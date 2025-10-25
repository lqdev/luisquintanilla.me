#r "nuget: Markdig"

open Markdig

let content = """- .NET AI Libraries
  - Microsoft Agent Framework
  - ML.NET
  - Microsoft.Extensions.AI
  - Tokenizers
  - Tensors
- .NET Data
  - Microsoft.Extensions.VectorData
  - Entity Framework"""

printfn "Input content:"
printfn "%s" content
printfn ""
printfn "Markdig output:"
printfn "%s" (Markdown.ToHtml(content))
