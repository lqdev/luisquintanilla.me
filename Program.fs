// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open System
open System.IO
open MarkdownService
open PartialViews
open ViewGenerator

[<EntryPoint>]
let main argv =

    let filePaths = 
        Directory.GetFiles("_src/posts")
    
    let outputDir = "_public"
    
    let posts = 
        filePaths
        |> Array.map(parseMarkdown) 

    let aboutContent = convertFileToHtml @"_src\about.md" |> aboutView

    let homePage = generate homeView "default" "Luis Quintanilla"
    let aboutPage = generate aboutContent "default" "Luis Quintanilla - About"
    let postPages = 
        posts
        |> Array.map(fun post -> 
            let postView = post.Content |> ConvertMdToHtml |> postView
            post.FileName,generate postView "default" post.Metadata.Title)

    // Generate Home / About
    File.WriteAllText(Path.Join("_public","about.html"),aboutPage)
    File.WriteAllText(Path.Join("_public","index.html"),homePage)  
    postPages
    |> Array.iter(fun (fileName,html) ->
        let saveDir = Path.Join(outputDir,"posts")
        let saveFileName = sprintf "%s.html" fileName
        let savePath = Path.Join(saveDir,saveFileName)
        File.WriteAllText(savePath,html))

    0