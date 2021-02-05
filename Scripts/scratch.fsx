#I @"C:\Users\lqdev\.nuget\packages"
#r @"markdig\0.23.0\lib\netstandard2.0\Markdig.dll"

open System.IO
open Markdig

let convertToHtml (filePath:string) = 
    filePath
    |> (File.ReadAllText >> Markdown.ToHtml)     

Directory.GetFiles "."
|> Array.filter(fun x -> Path.GetExtension(x) = ".md")
|> Array.iter(convertToHtml >> printfn "%s")