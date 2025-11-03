#r "../bin/Debug/net9.0/PersonalSite.dll"

open System.IO
open Domain
open ASTParsing
open CustomBlocks

let testFile = "/tmp/test-migrate.md"

printfn "=== Before Migration ==="
let beforeContent = File.ReadAllText(testFile)
printfn "%s" (beforeContent.Substring(0, min 500 beforeContent.Length))
printfn "..."

// Parse before
match parseBookFromFile testFile with
| Ok parsedDoc ->
    match parsedDoc.Metadata with
    | Some metadata ->
        printfn "\nFrontmatter Author: %s" metadata.Author
        printfn "Frontmatter ISBN: %s" metadata.Isbn
    | None -> ()
    
    match parsedDoc.CustomBlocks.TryGetValue("review") with
    | true, reviewList when reviewList.Length > 0 ->
        match reviewList.[0] with
        | :? ReviewData as reviewData ->
            printfn "Review Block Author: %A" reviewData.author
            printfn "Review Block ISBN: %A" reviewData.isbn
        | _ -> ()
    | _ -> ()
| Error _ -> ()

printfn "\n=== Running Migration ==="
// Run migration logic inline
let filePath = testFile
let lines = File.ReadAllLines(filePath)
match parseBookFromFile filePath with
| Ok parsedDoc ->
    match parsedDoc.Metadata with
    | Some metadata ->
        match parsedDoc.CustomBlocks.TryGetValue("review") with
        | true, reviewList when reviewList.Length > 0 ->
            match reviewList.[0] with
            | :? ReviewData as reviewData ->
                let contentStart = 
                    lines 
                    |> Array.skip 1
                    |> Array.tryFindIndex (fun line -> line.Trim() = "---")
                    |> Option.map (fun idx -> idx + 2)
                    |> Option.defaultValue 0
                
                let reviewBlockStart = 
                    lines 
                    |> Array.skip contentStart
                    |> Array.tryFindIndex (fun line -> line.Trim() = ":::review")
                    |> Option.map (fun idx -> contentStart + idx)
                    |> Option.defaultValue (lines.Length)
                
                let reviewBlockEnd =
                    lines
                    |> Array.skip (reviewBlockStart + 1)
                    |> Array.tryFindIndex (fun line -> line.Trim() = ":::")
                    |> Option.map (fun idx -> reviewBlockStart + idx + 1)
                    |> Option.defaultValue (lines.Length - 1)
                
                let contentAfterReview = 
                    if reviewBlockEnd + 1 < lines.Length then
                        lines.[(reviewBlockEnd + 1)..]
                        |> String.concat "\n"
                    else
                        ""
                
                let newFrontmatter = sprintf """---
title: "%s Review"
post_type: "review"
published_date: "%s"
tags: []
---""" (metadata.Title.Replace("\"", "\\\"")) metadata.DatePublished
                
                let newReviewBlock = 
                    let lines = ResizeArray<string>()
                    lines.Add(":::review")
                    lines.Add(sprintf "item: \"%s\"" (reviewData.item.Replace("\"", "\\\"")))
                    lines.Add("itemType: \"book\"")
                    lines.Add(sprintf "author: \"%s\"" (metadata.Author.Replace("\"", "\\\"")))
                    if not (System.String.IsNullOrWhiteSpace(metadata.Isbn)) then
                        lines.Add(sprintf "isbn: \"%s\"" (metadata.Isbn.Replace("\"", "\\\"")))
                    if not (System.String.IsNullOrWhiteSpace(metadata.Cover)) then
                        lines.Add(sprintf "cover: \"%s\"" metadata.Cover)
                    lines.Add(sprintf "datePublished: \"%s\"" metadata.DatePublished)
                    lines.Add(sprintf "rating: %.1f" reviewData.rating)
                    lines.Add(sprintf "scale: %.1f" (reviewData.GetScale()))
                    
                    match reviewData.summary with
                    | Some summary -> lines.Add(sprintf "summary: \"%s\"" (summary.Replace("\"", "\\\"").Replace("\n", " ")))
                    | None -> ()
                    
                    match reviewData.item_url with
                    | Some url -> lines.Add(sprintf "itemUrl: \"%s\"" url)
                    | None -> ()
                    
                    match reviewData.image_url with
                    | Some url -> lines.Add(sprintf "imageUrl: \"%s\"" url)
                    | None -> ()
                    
                    lines.Add(":::")
                    System.String.concat "\n" lines
                
                let heading = sprintf "# %s Review" metadata.Title
                let newContent = sprintf "%s\n\n%s\n\n%s%s" newFrontmatter heading newReviewBlock contentAfterReview
                
                File.WriteAllText(filePath, newContent)
                printfn "✅ Migration completed"
            | _ -> printfn "❌ No review data"
        | _ -> printfn "❌ No review block"
    | None -> printfn "❌ No metadata"
| Error _ -> printfn "❌ Parse error"

printfn "\n=== After Migration ==="
let afterContent = File.ReadAllText(testFile)
printfn "%s" (afterContent.Substring(0, min 500 afterContent.Length))
printfn "..."

// Parse after
match parseBookFromFile testFile with
| Ok parsedDoc ->
    match parsedDoc.CustomBlocks.TryGetValue("review") with
    | true, reviewList when reviewList.Length > 0 ->
        match reviewList.[0] with
        | :? ReviewData as reviewData ->
            printfn "\nAfter Migration - Review Block Author: %A" reviewData.author
            printfn "After Migration - Review Block ISBN: %A" reviewData.isbn
        | _ -> ()
    | _ -> ()
| Error _ -> ()
