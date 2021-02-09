// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open System
open System.IO
open MarkdownService
open PartialViews
open ViewGenerator
open RssService
open Orchestrator

[<EntryPoint>]
let main argv =
   
    let srcDir = "_src"
    let outputDir = "_public"

    // Prep work
    cleanOutputDirectory outputDir
    copyStaticFiles srcDir outputDir

    // Data
    let posts = loadPosts() 

    // Write Home / About / RSS Pages    
    buildHomePage()
    buildAboutPage()
    buildRssFeed posts
    
    // Write Post / Archive Pages
    buildPostPages posts
    buildPostArchive posts

    0