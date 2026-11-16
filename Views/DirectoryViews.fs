module DirectoryViews

open Giraffe.ViewEngine
open Domain

let private collectionUrl (collection: Collection) =
    let path = collection.UrlPath.TrimEnd('/')
    if path.StartsWith("/", System.StringComparison.Ordinal) then path + "/"
    else "/" + path + "/"

let private browseCard (collection: Collection) =
    let pageUrl = collectionUrl collection
    let opmlUrl = pageUrl + "index.opml"

    article [_class "directory-browse-card"] [
        h3 [] [a [_href pageUrl] [Text collection.Title]]
        p [_class "directory-browse-description"] [Text collection.Description]
        div [_class "directory-browse-links"] [
            a [_href pageUrl] [Text "View list"]
            a [
                _href opmlUrl
                attr "aria-label" $"{collection.Title} OPML subscription"
            ] [Text "OPML"]
        ]
    ]

let private filterOption (id: string) (value: string) (labelText: string) (isChecked: bool) =
    tag "label" [] [
        tag "input" [
            attr "type" "checkbox"
            _id id
            attr "value" value
            if isChecked then attr "checked" "checked"
        ] []
        span [] [Text labelText]
    ]

let directoryPage (collections: Collection array) =
    div [_class "directory-page"; _id "directory-container"] [
        h1 [] [Text "Directory"]
        p [_class "directory-intro"] [
            Text "Sites and feeds from my curated follow lists."
        ]
        p [_class "directory-scope"] [
            Text "Search names, domains, descriptions, tags, or list names. This is a directory of websites and feeds, not a people or account lookup."
        ]
        p [_class "directory-provenance-note"] [
            Text "Being listed shows only that a site appears in one of my curated lists; it does not verify identity or imply endorsement."
        ]

        div [_class "directory-search-form"] [
            tag "label" [
                _class "visually-hidden"
                attr "for" "directory-search-input"
            ] [Text "Search directory"]
            tag "input" [
                attr "type" "search"
                _id "directory-search-input"
                attr "placeholder" "Search sites, topics, or tags..."
                attr "autocomplete" "off"
                attr "spellcheck" "false"
                attr "aria-describedby" "directory-help"
            ] []
            button [
                attr "type" "button"
                _class "directory-search-clear"
                _id "directory-search-clear"
                attr "aria-label" "Clear search"
                attr "hidden" "hidden"
            ] [span [attr "aria-hidden" "true"] [Text "×"]]
        ]

        p [_class "directory-help"; _id "directory-help"] [
            Text "Try a domain, topic, or source-list name."
        ]

        section [
            _class "directory-browse"
            _id "directory-browse"
            attr "aria-labelledby" "directory-browse-heading"
        ] [
            h2 [_id "directory-browse-heading"] [Text "Browse a source list"]
            div [_class "directory-browse-grid"] [
                for collection in collections do
                    browseCard collection
            ]
        ]

        fieldset [
            _class "directory-filters"
            _id "directory-filters"
            attr "aria-describedby" "directory-filter-help"
        ] [
            legend [] [Text "Filter by list"]
            p [_class "directory-filter-help"; _id "directory-filter-help"] [
                Text "Choose one or more lists to narrow results."
            ]
            div [_class "directory-filter-options"] [
                filterOption "directory-filter-all" "all" "All lists" true
                for collection in collections do
                    filterOption
                        $"directory-filter-{collection.Id}"
                        collection.Id
                        collection.Title
                        false
            ]
        ]

        p [
            _class "directory-loading"
            _id "directory-loading"
            attr "role" "status"
            attr "aria-live" "polite"
            attr "hidden" "hidden"
        ] [Text "Loading directory..."]

        p [
            _class "directory-error"
            _id "directory-error"
            attr "role" "alert"
            attr "hidden" "hidden"
        ] []

        section [
            _class "directory-results"
            _id "directory-results"
            attr "aria-labelledby" "directory-results-heading"
            attr "hidden" "hidden"
        ] [
            h2 [_id "directory-results-heading"] [Text "Results"]
            p [
                _class "directory-result-count"
                _id "directory-result-count"
                attr "role" "status"
                attr "aria-live" "polite"
            ] []
            div [_class "directory-result-list"; _id "directory-result-list"] []
            div [
                _class "directory-no-results"
                _id "directory-no-results"
                attr "hidden" "hidden"
            ] [
                h3 [] [Text "No matching sites"]
                p [] [Text "Try another search, clear the query, or broaden the selected lists."]
                button [
                    attr "type" "button"
                    _class "directory-reset"
                    _id "directory-reset"
                ] [Text "Clear search and filters"]
            ]
        ]

        script [_src "/assets/js/fuse.min.js"] []
        script [_src "/assets/js/directory.js"] []
    ]
