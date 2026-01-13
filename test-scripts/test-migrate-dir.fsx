#r "../bin/Debug/net10.0/PersonalSite.dll"

open System
open System.IO
open System.Text.RegularExpressions
open Domain
open ASTParsing
open CustomBlocks

let reviewsDir = "/tmp/test-migration"
let reviewFiles = Directory.GetFiles(reviewsDir, "*.md")

printfn "Found %d files" reviewFiles.Length

for filePath in reviewFiles do
    let fileName = Path.GetFileName(filePath)
    printfn "\nProcessing: %s" fileName
    
    match parseBookFromFile filePath with
    | Ok parsedDoc ->
        match parsedDoc.Metadata with
        | Some metadata ->
            match parsedDoc.CustomBlocks.TryGetValue("review") with
            | true, reviewList when reviewList.Length > 0 ->
                match reviewList.[0] with
                | :? ReviewData as reviewData ->
                    if reviewData.author.IsSome then
                        printfn "  Already migrated"
                    else
                        printfn "  Needs migration"
                        printfn "    Old: Author in frontmatter = %s" metadata.Author
                        printfn "    Old: ISBN in frontmatter = %s" metadata.Isbn
                        
                        // Get content lines
                        let lines = File.ReadAllLines(filePath)
                        let contentStart = 
                            lines |> Array.skip 1 |> Array.tryFindIndex (fun line -> line.Trim() = "---")
                            |> Option.map (fun idx -> idx + 2) |> Option.defaultValue 0
                        
                        let reviewBlockStart = 
                            lines |> Array.skip contentStart 
                            |> Array.tryFindIndex (fun line -> line.Trim() = ":::review")
                            |> Option.map (fun idx -> contentStart + idx)
                            |> Option.defaultValue (lines.Length)
                        
                        let reviewBlockEnd =
                            lines |> Array.skip (reviewBlockStart + 1)
                            |> Array.tryFindIndex (fun line -> line.Trim() = ":::")
                            |> Option.map (fun idx -> reviewBlockStart + idx + 1)
                            |> Option.defaultValue (lines.Length - 1)
                        
                        let contentAfterReview = 
                            if reviewBlockEnd + 1 < lines.Length then
                                lines.[(reviewBlockEnd + 1)..] |> String.concat "\n"
                            else ""
                        
                        // Generate new frontmatter
                        let newFrontmatter = sprintf """---
title: "%s Review"
post_type: "review"
published_date: "%s"
tags: []
---""" (metadata.Title.Replace("\"", "\\\"")) metadata.DatePublished
                        
                        // Generate new review block
                        let newReviewLines = ResizeArray<string>()
                        newReviewLines.Add(":::review")
                        newReviewLines.Add(sprintf "item: \"%s\"" (reviewData.item.Replace("\"", "\\\"")))
                        newReviewLines.Add("itemType: \"book\"")
                        newReviewLines.Add(sprintf "author: \"%s\"" (metadata.Author.Replace("\"", "\\\"")))
                        if not (String.IsNullOrWhiteSpace(metadata.Isbn)) then
                            newReviewLines.Add(sprintf "isbn: \"%s\"" (metadata.Isbn.Replace("\"", "\\\"")))
                        if not (String.IsNullOrWhiteSpace(metadata.Cover)) then
                            newReviewLines.Add(sprintf "cover: \"%s\"" metadata.Cover)
                        newReviewLines.Add(sprintf "datePublished: \"%s\"" metadata.DatePublished)
                        newReviewLines.Add(sprintf "rating: %.1f" reviewData.rating)
                        newReviewLines.Add(sprintf "scale: %.1f" (reviewData.GetScale()))
                        
                        match reviewData.summary with
                        | Some summary -> newReviewLines.Add(sprintf "summary: \"%s\"" (summary.Replace("\"", "\\\"").Replace("\n", " ")))
                        | None -> ()
                        
                        match reviewData.item_url with
                        | Some url -> newReviewLines.Add(sprintf "itemUrl: \"%s\"" url)
                        | None -> ()
                        
                        match reviewData.image_url with
                        | Some url -> newReviewLines.Add(sprintf "imageUrl: \"%s\"" url)
                        | None -> ()
                        
                        newReviewLines.Add(":::")
                        let newReviewBlock = String.concat "\n" newReviewLines
                        
                        let heading = sprintf "# %s Review" metadata.Title
                        let newContent = sprintf "%s\n\n%s\n\n%s%s" newFrontmatter heading newReviewBlock contentAfterReview
                        
                        File.WriteAllText(filePath, newContent)
                        printfn "    ✅ Migrated!"
                | _ -> printfn "  No review data found"
            | _ -> printfn "  No review block found"
        | None -> printfn "  No metadata found"
    | Error err -> printfn "  Parse error: %A" err

printfn "\n=== Verifying Migration ==="
for filePath in reviewFiles do
    match parseBookFromFile filePath with
    | Ok parsedDoc ->
        match parsedDoc.CustomBlocks.TryGetValue("review") with
        | true, reviewList when reviewList.Length > 0 ->
            match reviewList.[0] with
            | :? ReviewData as reviewData ->
                printfn "File: %s" (Path.GetFileName(filePath))
                printfn "  Author in review block: %A" reviewData.author
                printfn "  ISBN in review block: %A" reviewData.isbn
            | _ -> ()
        | _ -> ()
    | Error _ -> ()

printfn "\n=== Test Complete ==="
0
