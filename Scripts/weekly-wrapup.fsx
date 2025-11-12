(*
    This script loads all content types from the unified feed system and creates a comprehensive weekly markdown summary.
*)

#r "../bin/Debug/net10.0/PersonalSite.dll"

open System
open System.IO
open Domain
open Loaders
open Builder
open GenericBuilder.UnifiedFeeds

// Load all content types using the unified feed system
let postsFeedData = buildPosts()
let notesFeedData = buildNotes()
let responsesFeedData = buildResponses()
let bookmarksFeedData = buildBookmarks()
let snippetsFeedData = buildSnippets()
let wikisFeedData = buildWikis()
let presentationsFeedData = buildPresentations()
let booksFeedData = buildBooks()
let mediaFeedData = buildMedia()

// Convert to unified feed items
let allUnifiedItems = [
    ("posts", convertPostsToUnified postsFeedData)
    ("notes", convertNotesToUnified notesFeedData)
    ("responses", convertResponsesToUnified responsesFeedData)
    ("bookmarks", convertBookmarkResponsesToUnified bookmarksFeedData)
    ("snippets", convertSnippetsToUnified snippetsFeedData)
    ("wiki", convertWikisToUnified wikisFeedData)
    ("presentations", convertPresentationsToUnified presentationsFeedData)
    ("reviews", convertBooksToUnified booksFeedData)
    ("media", convertAlbumsToUnified mediaFeedData)
]

// Filter for the last 7 days with proper timezone handling
let dateFilter = DateTimeOffset.Now.AddDays(-7.0).Date

let recentContent = 
    allUnifiedItems
    |> List.collect snd
    |> List.filter (fun item -> 
        try
            let itemDate = DateTimeOffset.Parse(item.Date).Date
            itemDate >= dateFilter
        with
        | _ -> false)
    |> List.sortByDescending (fun item -> item.Date)

// Helper function for string capitalization
module String =
    let capitalize (s: string) =
        if String.IsNullOrEmpty(s) then s
        else Char.ToUpper(s.[0]).ToString() + s.Substring(1).ToLower()

// Group content by type and generate markdown sections
let generateContentSection (contentType: string) (items: UnifiedFeedItem list) =
    let sectionTitle = 
        match contentType with
        | "posts" -> "Posts"
        | "notes" -> "Notes"
        | "bookmarks" -> "Bookmarks"
        | "snippets" -> "Snippets"
        | "wiki" -> "Wiki"
        | "presentations" -> "Presentations"
        | "reviews" -> "Reviews"
        | "media" -> "Media"
        | "reply" -> "Replies"
        | "reshare" -> "Reshares"
        | "star" -> "Stars"
        | _ -> contentType

    let urlPrefix = 
        match contentType with
        | "posts" -> "/posts/"
        | "notes" -> "/notes/"
        | "bookmarks" -> "/bookmarks/"  // bookmarks get their own routing
        | "snippets" -> "/resources/snippets/"
        | "wiki" -> "/resources/wiki/"
        | "presentations" -> "/resources/presentations/"
        | "reviews" -> "/reviews/"
        | "media" -> "/media/"
        | "reply" | "reshare" | "star" -> "/responses/"
        | _ -> sprintf "/%s/" contentType
    
    if items.IsEmpty then
        sprintf "## %s\n\nNo %s this week.\n" sectionTitle (sectionTitle.ToLower())
    else
        let itemStrings = 
            items
            |> List.map (fun item -> 
                let fileName = 
                    if item.Url.Contains("/") then
                        // Extract filename from URL
                        let segments = item.Url.Split('/')
                        let lastSegment = segments.[segments.Length - 1]
                        if lastSegment = "" then segments.[segments.Length - 2] else lastSegment
                    else item.Url
                sprintf "- [%s](%s%s)" item.Title urlPrefix fileName
            )
            |> String.concat "\n"

        sprintf "## %s\n\n%s\n" sectionTitle itemStrings

// Group recent content by type and sort types logically
let groupedContent = 
    recentContent
    |> List.groupBy (fun item -> item.ContentType)
    |> List.map (fun (contentType, items) -> (contentType, items |> List.sortByDescending (fun i -> i.Date)))
    |> List.sortBy (fun (contentType, _) -> 
        // Sort content types in logical order
        match contentType with
        | "posts" -> 1
        | "notes" -> 2
        | "bookmarks" -> 3
        | "reply" -> 4
        | "reshare" -> 5
        | "star" -> 6
        | "reviews" -> 7
        | "snippets" -> 8
        | "wiki" -> 9
        | "presentations" -> 10
        | "media" -> 11
        | _ -> 99
    )

// Generate all content sections
let contentSections = 
    groupedContent
    |> List.map (fun (contentType, items) -> generateContentSection contentType items)
    |> String.concat "\n"

let weeklyReviewPartial (title: string) (contentSections: string) = 
    let pubDate = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm zzz")
    sprintf "---\npost_type: \"note\"\ntitle: \"%s\"\npublished_date: \"%s\"\ntags: [\"weeklysummary\",\"blogging\",\"website\",\"indieweb\"]\n---\n\n%s" title pubDate contentSections

let currentDate = DateTime.Now.Date
let title = sprintf "Week of %s, %d - Post Summary" (currentDate.ToString("MMMM dd")) currentDate.Year

let content = weeklyReviewPartial title contentSections

let currentDateString = currentDate.ToString("yyyy-MM-dd")

// This is the root directory of the repo
let rootDir = Directory.GetCurrentDirectory()

let savePath = Path.Join(rootDir,"_src","notes")
let saveFileName = currentDateString + "-weekly-post-summary.md"
let fullPath = Path.Join(savePath,saveFileName)

File.WriteAllText(fullPath,content)
