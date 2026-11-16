(() => {
    const root = document.getElementById("directory-container");
    if (!root) return;

    const searchInput = document.getElementById("directory-search-input");
    const clearButton = document.getElementById("directory-search-clear");
    const browseSection = document.getElementById("directory-browse");
    const filterFieldset = document.getElementById("directory-filters");
    const allListsCheckbox = document.getElementById("directory-filter-all");
    const listCheckboxes = Array.from(
        filterFieldset.querySelectorAll('input[type="checkbox"]:not([value="all"])')
    );
    const loadingMessage = document.getElementById("directory-loading");
    const errorMessage = document.getElementById("directory-error");
    const resultsSection = document.getElementById("directory-results");
    const resultCount = document.getElementById("directory-result-count");
    const resultList = document.getElementById("directory-result-list");
    const noResults = document.getElementById("directory-no-results");
    const resetButton = document.getElementById("directory-reset");
    const validCollectionIds = new Set(listCheckboxes.map((checkbox) => checkbox.value));

    let entries = [];
    let fuse = null;
    let searchTimeout = null;

    const searchOptions = {
        includeScore: true,
        threshold: 0.35,
        minMatchCharLength: 2,
        ignoreLocation: true,
        keys: [
            { name: "title", weight: 0.35 },
            { name: "homepageUrl", weight: 0.25 },
            { name: "description", weight: 0.15 },
            { name: "tags", weight: 0.1 },
            { name: "collections.title", weight: 0.1 },
            { name: "feedUrls", weight: 0.05 }
        ]
    };

    function isHttpUrl(value) {
        if (typeof value !== "string") return false;
        try {
            const url = new URL(value);
            return (url.protocol === "http:" || url.protocol === "https:") &&
                url.username === "" &&
                url.password === "";
        } catch {
            return false;
        }
    }

    function isLocalPath(value) {
        return typeof value === "string" && value.startsWith("/") && !value.startsWith("//");
    }

    function validateIndex(index) {
        if (!index || index.version !== "1.0" || !Array.isArray(index.entries)) {
            throw new Error("Directory index has an unsupported shape or version.");
        }

        for (const entry of index.entries) {
            if (
                !entry ||
                typeof entry.title !== "string" ||
                !isHttpUrl(entry.homepageUrl) ||
                !Array.isArray(entry.feedUrls) ||
                !entry.feedUrls.every(isHttpUrl) ||
                !Array.isArray(entry.tags) ||
                !Array.isArray(entry.collections) ||
                !entry.collections.every((source) =>
                    source &&
                    validCollectionIds.has(source.id) &&
                    typeof source.title === "string" &&
                    isLocalPath(source.url) &&
                    isLocalPath(source.opmlUrl)
                )
            ) {
                throw new Error("Directory index contains an invalid entry.");
            }
        }
    }

    function selectedCollectionIds() {
        if (allListsCheckbox.checked) return [];
        return listCheckboxes
            .filter((checkbox) => checkbox.checked)
            .map((checkbox) => checkbox.value);
    }

    function synchronizeFilters(selectedIds) {
        const validIds = new Set(selectedIds.filter((id) => validCollectionIds.has(id)));
        allListsCheckbox.checked = validIds.size === 0;
        for (const checkbox of listCheckboxes) {
            checkbox.checked = validIds.has(checkbox.value);
        }
    }

    function synchronizeStateFromUrl() {
        const params = new URLSearchParams(window.location.search);
        searchInput.value = params.get("q") || "";
        synchronizeFilters(params.getAll("collection"));
    }

    function updateUrlState() {
        const url = new URL(window.location.href);
        const query = searchInput.value.trim();
        const selected = selectedCollectionIds();

        if (query) url.searchParams.set("q", query);
        else url.searchParams.delete("q");

        url.searchParams.delete("collection");
        for (const id of selected) {
            url.searchParams.append("collection", id);
        }

        window.history.replaceState({}, "", url);
    }

    function makeElement(tagName, className, text) {
        const element = document.createElement(tagName);
        if (className) element.className = className;
        if (text !== undefined) element.textContent = text;
        return element;
    }

    function makeHttpLink(url, text, className, ariaLabel) {
        if (!isHttpUrl(url)) return null;
        const link = makeElement("a", className, text);
        link.href = url;
        if (ariaLabel) link.setAttribute("aria-label", ariaLabel);
        return link;
    }

    function makeLocalLink(url, text, ariaLabel) {
        if (!isLocalPath(url)) return null;
        const link = makeElement("a", "", text);
        link.href = url;
        if (ariaLabel) link.setAttribute("aria-label", ariaLabel);
        return link;
    }

    function appendSectionHeading(parent, className, title) {
        const section = makeElement("div", className);
        section.append(makeElement("h4", "", title));
        parent.append(section);
        return section;
    }

    function renderResult(entry) {
        const card = makeElement("article", "directory-result-card");
        const heading = makeElement("h3", "");
        const titleLink = makeHttpLink(entry.homepageUrl, entry.title, "directory-result-title");
        if (titleLink) heading.append(titleLink);
        card.append(heading);

        const host = new URL(entry.homepageUrl).host;
        card.append(makeElement("p", "directory-result-hostname", host));

        if (typeof entry.description === "string" && entry.description.trim()) {
            card.append(makeElement("p", "directory-result-description", entry.description));
        }

        if (entry.tags.length > 0) {
            const tagsSection = appendSectionHeading(card, "directory-result-tags", "Topics");
            const tags = makeElement("ul", "");
            for (const tag of entry.tags) {
                tags.append(makeElement("li", "", tag));
            }
            tagsSection.append(tags);
        }

        if (entry.feedUrls.length > 0) {
            const feedsSection = appendSectionHeading(card, "directory-result-feeds", "Site feeds");
            const feeds = makeElement("ul", "");
            entry.feedUrls.forEach((url, index) => {
                const item = makeElement("li", "");
                const label = entry.feedUrls.length === 1 ? "Feed" : `Feed ${index + 1}`;
                const link = makeHttpLink(url, label);
                if (link) item.append(link);
                feeds.append(item);
            });
            feedsSection.append(feeds);
        }

        if (entry.collections.length > 0) {
            const sourcesSection = appendSectionHeading(card, "directory-result-sources", "Listed in");
            const sources = makeElement("ul", "");
            for (const source of entry.collections) {
                const item = makeElement("li", "");
                item.append(makeElement("span", "", `${source.title}: `));
                const pageLink = makeLocalLink(source.url, "View list");
                const opmlLink = makeLocalLink(
                    source.opmlUrl,
                    "OPML",
                    `${source.title} OPML subscription`
                );
                if (pageLink) item.append(pageLink);
                if (opmlLink) {
                    item.append(document.createTextNode(" ("));
                    opmlLink.setAttribute("class", "directory-opml-link");
                    item.append(opmlLink, document.createTextNode(")"));
                }
                sources.append(item);
            }
            sourcesSection.append(sources);
        }

        return card;
    }

    function render() {
        const query = searchInput.value.trim();
        const selected = selectedCollectionIds();
        const browseMode = query.length === 0 && selected.length === 0;

        browseSection.hidden = !browseMode;
        resultsSection.hidden = browseMode;
        clearButton.hidden = query.length === 0;
        resultList.replaceChildren();

        if (browseMode) {
            resultCount.textContent = "";
            noResults.hidden = true;
            return;
        }

        let matches;
        if (query) {
            matches = fuse
                .search(query)
                .map((result) => result.item)
                .filter((entry) =>
                    selected.length === 0 ||
                    entry.collections.some((source) => selected.includes(source.id))
                );
        } else {
            matches = entries
                .filter((entry) =>
                    entry.collections.some((source) => selected.includes(source.id))
                )
                .sort((left, right) => {
                    const byTitle = left.title.localeCompare(right.title, undefined, { sensitivity: "base" });
                    return byTitle || left.homepageUrl.localeCompare(right.homepageUrl);
                });
        }

        resultCount.textContent = `${matches.length} site${matches.length === 1 ? "" : "s"} found`;
        noResults.hidden = matches.length > 0;
        for (const entry of matches) {
            resultList.append(renderResult(entry));
        }
    }

    function resetSearchAndFilters() {
        clearTimeout(searchTimeout);
        searchInput.value = "";
        synchronizeFilters([]);
        searchInput.focus();
        updateUrlState();
        render();
    }

    function handleFilterChange(event) {
        if (event.target === allListsCheckbox) {
            if (!allListsCheckbox.checked && selectedCollectionIds().length === 0) {
                allListsCheckbox.checked = true;
            }
            if (allListsCheckbox.checked) {
                for (const checkbox of listCheckboxes) checkbox.checked = false;
            }
        } else {
            if (event.target.checked) allListsCheckbox.checked = false;
            if (selectedCollectionIds().length === 0) allListsCheckbox.checked = true;
        }

        updateUrlState();
        render();
    }

    searchInput.addEventListener("input", () => {
        clearTimeout(searchTimeout);
        searchTimeout = window.setTimeout(() => {
            updateUrlState();
            render();
        }, 300);
    });

    searchInput.addEventListener("keydown", (event) => {
        if (event.key === "Escape") resetSearchAndFilters();
    });

    clearButton.addEventListener("click", () => {
        clearTimeout(searchTimeout);
        searchInput.value = "";
        searchInput.focus();
        updateUrlState();
        render();
    });

    filterFieldset.addEventListener("change", handleFilterChange);
    resetButton.addEventListener("click", resetSearchAndFilters);
    synchronizeStateFromUrl();

    async function initialize() {
        try {
            if (typeof Fuse !== "function") {
                throw new Error("Fuse.js did not load.");
            }

            loadingMessage.hidden = false;
            const response = await fetch("/directory/index.json");
            if (!response.ok) {
                throw new Error(`Directory index request failed with status ${response.status}.`);
            }

            const index = await response.json();
            validateIndex(index);
            entries = index.entries;
            fuse = new Fuse(entries, searchOptions);
            loadingMessage.hidden = true;
            render();
        } catch (error) {
            console.error("Directory initialization failed:", error);
            loadingMessage.hidden = true;
            errorMessage.textContent = "The directory index could not be loaded. Please try again later.";
            errorMessage.hidden = false;
            searchInput.disabled = true;
            clearButton.disabled = true;
            allListsCheckbox.disabled = true;
            for (const checkbox of listCheckboxes) checkbox.disabled = true;
        }
    }

    initialize();
})();
