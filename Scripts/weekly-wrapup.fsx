(*
    This script loads all blog posts, notes, and responses and creates a markdown file with the contents.
*)

#r "../bin/Debug/net8.0/PersonalSite.dll"

open System
open System.IO
open Domain
open Loaders

let blogs = loadPosts "_src"
let notes = loadFeed "_src"
let responses = loadReponses "_src"

let filteredBlogs = 
    blogs
    |> Array.filter(fun x -> 
        let postDate = DateTime.Parse(x.Metadata.Date).Date
        let dateFilter = DateTimeOffset(DateTime.Now - TimeSpan.FromDays(7)).Date
        postDate >= dateFilter)

let filteredNotes = 
    notes
    |> Array.filter(fun x -> 
        let postDate = DateTime.Parse(x.Metadata.Date).Date
        let dateFilter = DateTimeOffset(DateTime.Now - TimeSpan.FromDays(7)).Date
        postDate >= dateFilter)

let filteredResponses = 
    responses
    |> Array.filter(fun x -> 
    let postDate = DateTime.Parse(x.Metadata.DatePublished).Date
    let dateFilter = DateTimeOffset(DateTime.Now - TimeSpan.FromDays(7)).Date
    postDate >= dateFilter)

let blogPartial (posts: Post array) = 
    match posts with
    | [||] -> 
        "## Blogs\n"
    | _ -> 

        let postStrings = 
            posts
            |> Array.map(fun post -> 
                $"- [{post.Metadata.Title}](/posts/{post.FileName})"
            )
            |> fun x -> String.Join('\n',x)

        $"""
        ## Blogs

        {postStrings}
        """

let notesPartial (posts: Post array) = 
    match posts with
    | [||] -> 
        "## Notes\n"
    | _ -> 

        let postStrings = 
            posts
            |> Array.map(fun post -> 
                $"- [{post.Metadata.Title}](/notes/{post.FileName})"
            )
            |> fun x -> String.Join('\n',x)

        $"""
        ## Notes

        {postStrings}
        """

let responsesPartial (posts: Response array) = 
    match posts with
    | [||] -> 
        "## Responses"
    | _ -> 

        let postStrings = 
            posts
            |> Array.map(fun post -> 
                $"- [{post.Metadata.Title}](/responses/{post.FileName})"
            )
            |> fun x -> String.Join('\n',x)

        $"""
        ## Responses

        {postStrings}
        """

let weeklyReviewPartial (title) (blogs:string) (notes:string) (responses:string) = 
    let pubDate = DateTimeOffset(DateTime.Now.Subtract(TimeSpan(5,0,0)).Ticks,TimeSpan(-5,0,0)).ToString("yyyy-MM-dd HH:mm zzz")
    
    $"""
    ---
    post_type: "note" 
    title: "{title}"
    published_date: "{pubDate}"
    tags: ["weeklysummary","blogging","website","indieweb"]
    ---

    {blogs}

    {notes}

    {responses}
    """

let currentDate = DateTime.Now.Date

let title = $"""Week of {currentDate.ToString("m")}, {currentDate.Year} - Post Summary"""

let content = weeklyReviewPartial title (blogPartial filteredBlogs) (notesPartial filteredNotes) (responsesPartial filteredResponses)

let currentDateString = currentDate.ToString("yyyy-MM-dd")

// This is the root directory of the repo
let rootDir = Directory.GetCurrentDirectory()

let savePath = Path.Join($"{rootDir}","_src","notes")
let saveFileName = $"{currentDateString}-weekly-post-summary.md"

File.WriteAllText(Path.Join(savePath,saveFileName),content)
