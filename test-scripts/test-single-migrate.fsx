#r "../bin/Debug/net9.0/PersonalSite.dll"

open System
open System.IO
open Domain
open ASTParsing
open CustomBlocks

let testFile = "/tmp/test-migration/project-hail-mary.md"

printfn "=== Before Migration ==="
let beforeLines = File.ReadAllLines(testFile)
printfn "%s" (String.concat "\n" (beforeLines |> Array.take (min 20 beforeLines.Length)))

printfn "\n=== Performing Migration ==="

match parseBookFromFile testFile with
| Ok parsedDoc ->
    match parsedDoc.Metadata, parsedDoc.CustomBlocks.TryGetValue("review") with
    | Some metadata, (true, reviewList) when reviewList.Length > 0 ->
        match reviewList.[0] with
        | :? ReviewData as reviewData ->
            let lines = File.ReadAllLines(testFile)
            
            // Find content sections
            let contentStart = 
                lines |> Array.skip 1 |> Array.tryFindIndex (fun line -> line.Trim() = "---")
                |> Option.map (fun idx -> idx + 2) |> Option.defaultValue 0
            
            let reviewBlockEnd =
                lines |> Array.tryFindIndex (fun line -> line.Trim().StartsWith("::") && not (line.Trim() = ":::review"))
                |> Option.defaultValue (lines.Length - 1)
            
            let contentAfter = 
                if reviewBlockEnd + 1 < lines.Length then
                    lines.[(reviewBlockEnd + 1)..] |> String.concat "\n"
                else ""
            
            // Build new content
            let newFrontmatter = sprintf """---
title: "%s Review"
post_type: "review"
published_date: "%s"
tags: []
---""" (metadata.Title.Replace("\"", "\\\"")) metadata.DatePublished
            
            let reviewLines = [
                ":::review"
                sprintf "item: \"%s\"" (reviewData.item.Replace("\"", "\\\""))
                "itemType: \"book\""
                sprintf "author: \"%s\"" (metadata.Author.Replace("\"", "\\\""))
                if not (String.IsNullOrWhiteSpace(metadata.Isbn)) then sprintf "isbn: \"%s\"" metadata.Isbn
                if not (String.IsNullOrWhiteSpace(metadata.Cover)) then sprintf "cover: \"%s\"" metadata.Cover
                sprintf "datePublished: \"%s\"" metadata.DatePublished
                sprintf "rating: %.1f" reviewData.rating
                sprintf "scale: %.1f" (reviewData.GetScale())
                match reviewData.summary with | Some s -> sprintf "summary: \"%s\"" (s.Replace("\"", "\\\"")) | None -> ""
                match reviewData.item_url with | Some u -> sprintf "itemUrl: \"%s\"" u | None -> ""
                match reviewData.image_url with | Some u -> sprintf "imageUrl: \"%s\"" u | None -> ""
                ":::"
            ] |> List.filter (fun s -> not (String.IsNullOrWhiteSpace(s)))
            
            let newReviewBlock = String.concat "\n" reviewLines
            let heading = sprintf "# %s Review" metadata.Title
            let newContent = sprintf "%s\n\n%s\n\n%s%s" newFrontmatter heading newReviewBlock contentAfter
            
            File.WriteAllText(testFile, newContent)
            printfn "✅ Migration completed"
        | _ -> printfn "❌ Invalid review data"
    | _ -> printfn "❌ Missing metadata or review block"
| Error err -> printfn "❌ Parse error: %A" err

printfn "\n=== After Migration ==="
let afterLines = File.ReadAllLines(testFile)
printfn "%s" (String.concat "\n" (afterLines |> Array.take (min 25 afterLines.Length)))

printfn "\n=== Verification ==="
match parseBookFromFile testFile with
| Ok parsedDoc ->
    match parsedDoc.CustomBlocks.TryGetValue("review") with
    | true, reviewList when reviewList.Length > 0 ->
        match reviewList.[0] with
        | :? ReviewData as rd ->
            printfn "Author in review block: %A" rd.author
            printfn "ISBN in review block: %A" rd.isbn
        | _ -> ()
    | _ -> ()
| Error _ -> ()

0
