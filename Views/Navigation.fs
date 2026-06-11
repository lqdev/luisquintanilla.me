module Navigation

// Single source of truth for site navigation.
//
// Two surfaces are modelled here, intentionally kept independent:
//   - the desktop "desert" sidebar nav (root URL space, icons + dropdowns)
//   - the text-only nav (the /text/ URL space, a curated flat list)
//
// They are NOT unified into one value: the text-only surface is a deliberately
// minimal, curated set that routes through aggregation pages (e.g. "All Content")
// rather than mirroring every collection, so it does not drift when collections
// are added on the desktop side. The data is co-located here for discoverability;
// each renderer owns its own markup.
//
// The desktop dropdown contents are the high-churn surface ("add a collection to
// the menu"): adding an item is now a one-line data edit below. This value is the
// seam the future content-type registry (bet B1) will feed.

open Giraffe.ViewEngine

// ---------------------------------------------------------------------------
// Icons (Bootstrap Icons paths). Bespoke per link; carried as pre-built nodes.
// ---------------------------------------------------------------------------

let private navIcon (paths: string list) =
    tag "svg" [_class "nav-icon"; attr "viewBox" "0 0 16 16"; attr "fill" "currentColor"]
        [ for d in paths -> tag "path" [attr "d" d] [] ]

let private homeIcon =
    navIcon [ "M8.354 1.146a.5.5 0 0 0-.708 0l-6 6A.5.5 0 0 0 1.5 7.5v7a.5.5 0 0 0 .5.5h4.5a.5.5 0 0 0 .5-.5v-4h2v4a.5.5 0 0 0 .5.5H14a.5.5 0 0 0 .5-.5v-7a.5.5 0 0 0-.146-.354L8.354 1.146zM2.5 14V7.707l5.5-5.5 5.5 5.5V14H10v-4a.5.5 0 0 0-.5-.5h-3a.5.5 0 0 0-.5.5v4H2.5z" ]

let private aboutIcon =
    navIcon [ "M8 15A7 7 0 1 1 8 1a7 7 0 0 1 0 14zm0 1A8 8 0 1 0 8 0a8 8 0 0 0 0 16z"
              "m8.93 6.588-2.29.287-.082.38.45.083c.294.07.352.176.288.469l-.738 3.468c-.194.897.105 1.319.808 1.319.545 0 1.178-.252 1.465-.598l.088-.416c-.2.176-.492.246-.686.246-.275 0-.375-.193-.304-.533L8.93 6.588zM9 4.5a1 1 0 1 1-2 0 1 1 0 0 1 2 0z" ]

let private contactIcon =
    navIcon [ "M0 4a2 2 0 0 1 2-2h12a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2H2a2 2 0 0 1-2-2V4Zm2-1a1 1 0 0 0-1 1v.217l7 4.2 7-4.2V4a1 1 0 0 0-1-1H2Zm13 2.383-4.708 2.825L15 11.105V5.383Zm-.034 6.876-5.64-3.471L8 9.583l-1.326-.795-5.64 3.47A1 1 0 0 0 2 13h12a1 1 0 0 0 .966-.741ZM1 11.105l4.708-2.897L1 5.383v5.722Z" ]

let private searchIcon =
    navIcon [ "M11.742 10.344a6.5 6.5 0 1 0-1.397 1.398h-.001c.03.04.062.078.098.115l3.85 3.85a1 1 0 0 0 1.415-1.414l-3.85-3.85a1.007 1.007 0 0 0-.115-.1zM12 6.5a5.5 5.5 0 1 1-11 0 5.5 5.5 0 0 1 11 0z" ]

let private subscribeIcon =
    navIcon [ "M5.5 12a1.5 1.5 0 1 1-3 0 1.5 1.5 0 0 1 3 0zm-3-8.5a1 1 0 0 1 1-1c5.523 0 10 4.477 10 10a1 1 0 1 1-2 0 8 8 0 0 0-8-8 1 1 0 0 1-1-1zm0 4a1 1 0 0 1 1-1 6 6 0 0 1 6 6 1 1 0 1 1-2 0 4 4 0 0 0-4-4 1 1 0 0 1-1-1z" ]

let private collectionsIcon =
    navIcon [ "M2.5 3A1.5 1.5 0 0 0 1 4.5v.793c.026.009.051.02.076.032L7.674 8.51c.206.1.446.1.652 0l6.598-3.185A.755.755 0 0 1 15 5.293V4.5A1.5 1.5 0 0 0 13.5 3h-11Z"
              "M15 6.954 8.978 9.86a2.25 2.25 0 0 1-1.956 0L1 6.954V11.5A1.5 1.5 0 0 0 2.5 13h11a1.5 1.5 0 0 0 1.5-1.5V6.954Z" ]

let private resourcesIcon =
    navIcon [ "M1 2.828c.885-.37 2.154-.769 3.388-.893 1.33-.134 2.458.063 3.112.752v9.746c-.935-.53-2.12-.603-3.213-.493-1.18.12-2.37.461-3.287.811V2.828zm7.5-.141c.654-.689 1.782-.886 3.112-.752 1.234.124 2.503.523 3.388.893v9.923c-.918-.35-2.107-.692-3.287-.81-1.094-.111-2.278-.039-3.213.492V2.687zM8 1.783C7.015.936 5.587.81 4.287.94c-1.514.153-3.042.672-3.994 1.105A.5.5 0 0 0 0 2.5v11a.5.5 0 0 0 .707.455c.882-.4 2.303-.881 3.68-1.02 1.409-.142 2.59.087 3.223.877a.5.5 0 0 0 .78 0c.633-.79 1.814-1.019 3.222-.877 1.378.139 2.8.62 3.681 1.02A.5.5 0 0 0 16 13.5v-11a.5.5 0 0 0-.293-.455c-.952-.433-2.48-.952-3.994-1.105C10.413.809 8.985.936 8 1.783z" ]

let private dropdownArrow =
    tag "svg" [_class "dropdown-arrow"; attr "viewBox" "0 0 16 16"; attr "fill" "currentColor"]
        [ tag "path" [attr "d" "M7.247 11.14 2.451 5.658C1.885 5.013 2.345 4 3.204 4h9.592a1 1 0 0 1 .753 1.659l-4.796 5.48a1 1 0 0 1-1.506 0z"] [] ]

// ---------------------------------------------------------------------------
// Navigation data model
// ---------------------------------------------------------------------------

/// A navigable link. Icon is present on desktop top-level links, absent on
/// dropdown items and text-only links.
type NavLink =
    { Href: string
      Label: string
      Icon: XmlNode option }

/// An entry inside a desktop dropdown menu: a link or a visual divider.
type NavMenuEntry =
    | MenuLink of NavLink
    | MenuDivider

/// A top-level section of the desktop sidebar nav.
type NavSection =
    /// A plain group of links (rendered as <div class="nav-section">).
    | LinkGroup of NavLink list
    /// A dropdown. `Id` is used for both the toggle's data-target and the menu's id.
    | DropdownGroup of Id: string * Label: string * Icon: XmlNode * Entries: NavMenuEntry list

let private link href label icon = { Href = href; Label = label; Icon = icon }
let private item href label = MenuLink { Href = href; Label = label; Icon = None }

// ---------------------------------------------------------------------------
// Desktop ("desert") navigation data
// ---------------------------------------------------------------------------

let desktopSections : NavSection list =
    [
        // Main navigation with explicit Home button
        LinkGroup [
            link "/" "Home" (Some homeIcon)
            link "/about" "About" (Some aboutIcon)
            link "/contact" "Contact" (Some contactIcon)
            link "/search" "Search" (Some searchIcon)
            link "/feed" "Subscribe" (Some subscribeIcon)
        ]

        // Collections dropdown
        DropdownGroup("collections-dropdown", "Collections", collectionsIcon, [
            // Rolls (content-type)
            item "/collections/blogroll" "Blogroll"
            item "/collections/podroll" "Podroll"
            item "/collections/youtube" "YouTube"
            item "/collections/forums" "Forums"
            MenuDivider
            // Starter packs / travel guides
            item "/collections/starter-packs" "Starter Packs"
            item "/collections/travel-guides" "Travel Guides"
            MenuDivider
            // Media collections
            item "/collections/albums" "Albums"
            item "/collections/playlists" "Playlists"
            MenuDivider
            item "/radio" "Radio"
            item "/tags" "Tags"
        ])

        // Resources dropdown
        DropdownGroup("resources-dropdown", "Resources", resourcesIcon, [
            item "/resources/snippets" "Snippets"
            item "/resources/wiki" "Wiki"
            item "/resources/presentations" "Presentations"
            item "/resources/read-later" "Read Later"
            item "/tools" "Tools"
            MenuDivider
            item "/resources/ai-memex/" "AI Memex"
        ])
    ]

// ---------------------------------------------------------------------------
// Desktop navigation rendering
// ---------------------------------------------------------------------------

let private renderNavLink (l: NavLink) =
    a [_class "nav-link"; _href l.Href]
        (match l.Icon with
         | Some icon -> [ icon; Text l.Label ]
         | None -> [ Text l.Label ])

let private renderMenuEntry (e: NavMenuEntry) =
    match e with
    | MenuLink l -> a [_class "dropdown-item"; _href l.Href] [Text l.Label]
    | MenuDivider -> div [_class "dropdown-divider"] []

let private renderSection (section: NavSection) =
    match section with
    | LinkGroup links ->
        div [_class "nav-section"] (links |> List.map renderNavLink)
    | DropdownGroup(id, label, icon, entries) ->
        div [_class "nav-section dropdown"] [
            button [_class "nav-link dropdown-toggle"; attr "data-target" id] [
                icon
                Text label
                dropdownArrow
            ]
            div [_class "dropdown-menu"; _id id] (entries |> List.map renderMenuEntry)
        ]

/// The desktop sidebar navigation, rendered as the same XmlNode list the layout
/// spreads into the page body.
let desertNavigation : XmlNode list =
    [
        // Mobile toggle button (hidden on desktop)
        button [
            _class "mobile-toggle"
            _id "mobile-nav-toggle"
            attr "aria-label" "Toggle navigation menu"
            attr "aria-expanded" "false"
        ] [
            div [_class "hamburger"] [
                span [] []
                span [] []
                span [] []
            ]
        ]

        // Navigation overlay for mobile
        div [_class "nav-overlay"; _id "nav-overlay"] []

        // Always-visible navigation sidebar (desktop) / slide-out (mobile)
        nav [_class "desert-nav"; _id "sidebar-menu"; attr "role" "navigation"; attr "aria-label" "Main navigation"]
            ([
                // Brand/Header
                div [_class "nav-brand"] [
                    a [_href "/"; _class "brand-text"] [
                        img [_src "/avatar.png"; _alt "Luis Quintanilla avatar"; attr "loading" "lazy"]
                        Text "Luis Quintanilla"
                    ]
                ]
             ]
             @ (desktopSections |> List.map renderSection)
             @ [
                // Theme Toggle
                div [_class "theme-toggle"] [
                    button [
                        _class "theme-toggle-btn"
                        _id "theme-toggle"
                        attr "aria-label" "Toggle dark mode"
                    ] [
                        span [_id "theme-toggle-icon"] [Text "☀️"]
                        span [] [Text "Theme"]
                    ]
                ]
             ])
    ]

// ---------------------------------------------------------------------------
// Text-only navigation (separate curated surface over the /text/ URL space)
// ---------------------------------------------------------------------------

let textOnlyNav : (string * string) list =
    [
        "/text/", "Home"
        "/text/about/", "About"
        "/text/contact/", "Contact"
        "/text/content/", "All Content"
        "/text/feeds/", "RSS Feeds"
        "/text/help/", "Help"
    ]

/// The text-only main navigation block.
let renderTextOnlyNav : XmlNode =
    nav [attr "role" "navigation"; attr "aria-label" "Main navigation"] [
        ul [] [
            for (href, label) in textOnlyNav ->
                li [] [a [_href href] [Text label]]
        ]
    ]
