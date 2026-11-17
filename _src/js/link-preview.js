(function () {
  "use strict";

  const bucketCount = 64;
  const siteHosts = new Set([
    "lqdev.me",
    "www.lqdev.me",
    "luisquintanilla.me",
    "www.luisquintanilla.me",
  ]);
  const assetExtensions = /\.(activitypub|bar|css|csv|gif|gpx|ico|ics|jpe?g|js|json|m3u|md|mp3|mp4|opml|pdf|png|svg|txt|vcf|webmanifest|webp|xml|zip|woff2?|ttf|eot)$/i;
  const bucketCache = new Map();
  const linkState = new WeakMap();
  let tooltip;
  let activeLink = null;
  let pendingLink = null;
  let hoverTimer = null;
  let hideTimer = null;
  let operation = 0;
  let lastPointerType = null;
  let lastPointerAt = 0;

  function normalizeKey(href) {
    let url;
    try {
      url = new URL(href, window.location.href);
    } catch {
      return null;
    }

    if (!siteHosts.has(url.hostname.toLowerCase())) return null;

    const segments = url.pathname
      .split("/")
      .filter(Boolean)
      .filter((segment) => segment.toLowerCase() !== "index.html");
    if (segments.length === 0) return null;
    return `/${segments.join("/")}`.toLowerCase();
  }

  function fnv1a(value) {
    let hash = 2166136261;
    const bytes = new TextEncoder().encode(value);
    for (const byte of bytes) {
      hash ^= byte;
      hash = Math.imul(hash, 16777619) >>> 0;
    }
    return hash >>> 0;
  }

  function bucketFor(pathKey) {
    return fnv1a(pathKey) % bucketCount;
  }

  function isAsset(url) {
    return (
      url.pathname.toLowerCase().startsWith("/assets/") ||
      url.pathname.toLowerCase().startsWith("/images/") ||
      assetExtensions.test(url.pathname)
    );
  }

  function isEligible(link) {
    if (!link || link.hasAttribute("download")) return false;
    if (!link.closest(".post-content, .presentation-content")) return false;
    if (link.closest("nav, header, footer, .post-tags, button")) return false;
    if (link.querySelector("img, svg") && !link.textContent.trim()) return false;

    let url;
    try {
      url = new URL(link.href, window.location.href);
    } catch {
      return false;
    }

    if (!siteHosts.has(url.hostname.toLowerCase()) || isAsset(url)) return false;
    const key = normalizeKey(url.href);
    if (!key) return false;
    const currentKey = normalizeKey(window.location.href);
    return !(url.hash && key === currentKey);
  }

  function setLinkState(link) {
    if (linkState.has(link)) return;
    const state = {
      hadTitle: link.hasAttribute("title"),
      title: link.getAttribute("title"),
      describedBy: link.getAttribute("aria-describedby"),
    };
    linkState.set(link, state);
    link.removeAttribute("title");
    const ids = (state.describedBy || "").split(/\s+/).filter(Boolean);
    if (!ids.includes("link-preview")) ids.push("link-preview");
    link.setAttribute("aria-describedby", ids.join(" "));
  }

  function restoreLinkState(link) {
    const state = linkState.get(link);
    if (!state) return;
    if (state.hadTitle) link.setAttribute("title", state.title);
    else link.removeAttribute("title");
    if (state.describedBy === null) link.removeAttribute("aria-describedby");
    else link.setAttribute("aria-describedby", state.describedBy);
    linkState.delete(link);
  }

  function clearTooltip() {
    tooltip.replaceChildren();
    tooltip.hidden = true;
    tooltip.removeAttribute("style");
  }

  function hidePreview() {
    operation++;
    window.clearTimeout(hoverTimer);
    window.clearTimeout(hideTimer);
    pendingLink = null;
    if (activeLink) restoreLinkState(activeLink);
    activeLink = null;
    clearTooltip();
  }

  function scheduleHide() {
    window.clearTimeout(hideTimer);
    hideTimer = window.setTimeout(() => {
      if (
        activeLink &&
        (activeLink.matches(":focus") ||
          activeLink.matches(":hover") ||
          tooltip.matches(":hover"))
      ) {
        return;
      }
      hidePreview();
    }, 160);
  }

  function positionTooltip(link) {
    const rect = link.getBoundingClientRect();
    const margin = 12;
    const gap = 8;
    tooltip.hidden = false;
    const width = tooltip.offsetWidth;
    const height = tooltip.offsetHeight;
    const left = Math.max(
      margin,
      Math.min(rect.left, window.innerWidth - width - margin),
    );
    const below = rect.bottom + gap;
    const top =
      below + height <= window.innerHeight - margin
        ? below
        : Math.max(margin, rect.top - height - gap);
    tooltip.style.left = `${left}px`;
    tooltip.style.top = `${top}px`;
  }

  function renderPreview(link, record) {
    const heading = document.createElement("strong");
    heading.className = "link-preview-title";
    heading.textContent = record.title || "Untitled";

    const metadata = document.createElement("span");
    metadata.className = "link-preview-meta";
    metadata.textContent = [record.type, record.date].filter(Boolean).join(" · ");

    const summary = document.createElement("p");
    summary.className = "link-preview-summary";
    summary.textContent = record.summary || "";

    tooltip.replaceChildren(heading, metadata);
    if (summary.textContent) tooltip.append(summary);
    positionTooltip(link);
  }

  async function loadRecord(pathKey) {
    const index = bucketFor(pathKey);
    if (!bucketCache.has(index)) {
      const fileName = `b${String(index).padStart(2, "0")}.json`;
      const request = fetch(`/assets/previews/${fileName}`, {
        credentials: "same-origin",
      })
        .then((response) => {
          if (!response.ok) return null;
          return response.json();
        })
        .then((payload) => {
          if (
            !payload ||
            payload.version !== 1 ||
            payload.bucketCount !== bucketCount ||
            !payload.entries
          ) {
            return null;
          }
          return payload;
        })
        .catch(() => null);
      bucketCache.set(index, request);
    }
    const bucket = await bucketCache.get(index);
    return bucket ? bucket.entries[pathKey] || null : null;
  }

  async function showPreview(link) {
    window.clearTimeout(hideTimer);
    if (activeLink && activeLink !== link) restoreLinkState(activeLink);
    activeLink = link;
    pendingLink = null;
    setLinkState(link);
    const key = normalizeKey(link.href);
    if (!key) {
      hidePreview();
      return;
    }

    const thisOperation = ++operation;
    const record = await loadRecord(key);
    if (thisOperation !== operation || activeLink !== link) return;
    if (!record) {
      hidePreview();
      return;
    }
    renderPreview(link, record);
  }

  function initialize() {
    if (!document.body) return;

    tooltip = document.createElement("div");
    tooltip.id = "link-preview";
    tooltip.className = "link-preview";
    tooltip.setAttribute("role", "tooltip");
    tooltip.hidden = true;
    document.body.append(tooltip);

    document.addEventListener("pointerdown", (event) => {
      lastPointerType = event.pointerType;
      lastPointerAt = performance.now();
    });

    document.addEventListener("pointerover", (event) => {
      if (event.pointerType === "touch") return;
      const link = event.target.closest && event.target.closest("a[href]");
      if (!isEligible(link) || link.contains(event.relatedTarget)) return;
      window.clearTimeout(hideTimer);
      window.clearTimeout(hoverTimer);
      pendingLink = link;
      hoverTimer = window.setTimeout(() => {
        if (pendingLink === link) showPreview(link);
      }, 300);
    });

    document.addEventListener("pointerout", (event) => {
      const link = event.target.closest && event.target.closest("a[href]");
      if (!link || link.contains(event.relatedTarget)) return;
      if (pendingLink === link) {
        window.clearTimeout(hoverTimer);
        pendingLink = null;
      }
      if (activeLink === link) scheduleHide();
    });

    document.addEventListener("focusin", (event) => {
      const link = event.target.closest && event.target.closest("a[href]");
      if (!isEligible(link)) return;
      if (
        lastPointerType === "touch" &&
        performance.now() - lastPointerAt < 1000
      ) {
        return;
      }
      window.clearTimeout(hoverTimer);
      pendingLink = null;
      showPreview(link);
    });

    document.addEventListener("focusout", (event) => {
      const link = event.target.closest && event.target.closest("a[href]");
      if (activeLink === link && !link.contains(event.relatedTarget)) scheduleHide();
    });

    tooltip.addEventListener("pointerenter", () => window.clearTimeout(hideTimer));
    tooltip.addEventListener("pointerleave", scheduleHide);
    document.addEventListener("keydown", (event) => {
      if (event.key === "Escape" && activeLink) hidePreview();
    });
    window.addEventListener("resize", () => {
      if (activeLink && !tooltip.hidden) positionTooltip(activeLink);
    });
    window.addEventListener(
      "scroll",
      () => {
        if (activeLink && !tooltip.hidden) positionTooltip(activeLink);
      },
      true,
    );
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", initialize, { once: true });
  } else {
    initialize();
  }
})();
