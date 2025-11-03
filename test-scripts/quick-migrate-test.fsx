#r "../bin/Debug/net9.0/PersonalSite.dll"

open System
open System.IO
open Domain
open ASTParsing
open CustomBlocks

let testFile = "/tmp/test-migration/project-hail-mary.md"

printfn "Testing migration..."

match parseBookFromFile testFile with
| Ok parsedDoc ->
    match parsedDoc.Metadata with
    | Some metadata ->
        match parsedDoc.CustomBlocks.TryGetValue("review") with
        | true, reviewList when reviewList.Length > 0 ->
            match reviewList.[0] with
            | :? ReviewData as reviewData ->
                let newContent = sprintf """---
title: "%s Review"
post_type: "review"
published_date: "%s"
tags: []
---

# %s Review

:::review
item: "%s"
itemType: "book"
author: "%s"
isbn: "%s"
cover: "%s"
datePublished: "%s"
rating: %.1f
scale: 5.0
summary: "%s"
itemUrl: "%s"
imageUrl: "%s"
:::

## Description

Test content after review block.
""" 
                    (metadata.Title.Replace("\"", "\\\""))
                    metadata.DatePublished
                    metadata.Title
                    (reviewData.item.Replace("\"", "\\\""))
                    (metadata.Author.Replace("\"", "\\\""))
                    metadata.Isbn
                    metadata.Cover
                    metadata.DatePublished
                    reviewData.rating
                    (reviewData.summary |> Option.defaultValue "" |> fun s -> s.Replace("\"", "\\\""))
                    (reviewData.item_url |> Option.defaultValue "")
                    (reviewData.image_url |> Option.defaultValue "")
                
                File.WriteAllText(testFile, newContent)
                printfn "✅ Migrated"
            | _ -> printfn "No review data"
        | _ -> printfn "No review block"
    | None -> printfn "No metadata"
| Error err -> printfn "Error: %A" err

// Verify
printfn "\nVerifying..."
match parseBookFromFile testFile with
| Ok pd ->
    match pd.CustomBlocks.TryGetValue("review") with
    | true, rl when rl.Length > 0 ->
        match rl.[0] with
        | :? ReviewData as rd ->
            printfn "Author: %A" rd.author
            printfn "ISBN: %A" rd.isbn
        | _ -> ()
    | _ -> ()
| Error _ -> ()

printfn "\nDone"
0
