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

    let outputDir = "_public"
    
    // docs
    // |> Array.iter(fun doc -> 
    //     let htmlFileName = sprintf "%s.html" doc.FileName
    //     let savePath = Path.Join(outputDir,htmlFileName)
    //     File.WriteAllText(savePath,doc.Content)
    // ) 

    File.WriteAllText(Path.Join("_public","index.html"),HomePage.generate)  

    0