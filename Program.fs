// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open System
open System.IO
open MarkdownService
open HomePage

[<EntryPoint>]
let main argv =

    let filePaths = 
        Directory.GetFiles("_src")
        |> Array.filter(fun path -> Path.GetExtension(path) = ".md")
    
    let docs = 
        filePaths
        |> Array.map(convertToHtml)
        |> Array.iter(printfn "%A")

    printfn "%s" HomePage.getView
    0