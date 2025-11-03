// Test script to validate review parsing behavior
#r "../bin/Debug/net9.0/PersonalSite.dll"

open System
open System.IO
open Domain
open ASTParsing
open CustomBlocks

// Test file paths
let testDir = "/home/runner/work/luisquintanilla.me/luisquintanilla.me/_src/reviews/library"
let testFile = Path.Combine(testDir, "project-hail-mary.md")

printfn "=== Testing Current Review Parsing Behavior ==="
printfn ""

// Test 1: Parse book with current system
printfn "Test 1: Parse book file with existing system"
printfn "File: %s" testFile
printfn ""

match parseBookFromFile testFile with
| Ok parsedDoc ->
    printfn "✅ Parse successful"
    match parsedDoc.Metadata with
    | Some metadata ->
        printfn "Frontmatter metadata:"
        printfn "  Title: %s" metadata.Title
        printfn "  Author: %s" metadata.Author
        printfn "  ISBN: %s" metadata.Isbn
        printfn "  Rating: %.1f" metadata.Rating
        printfn "  Date: %s" metadata.DatePublished
        printfn ""
    | None ->
        printfn "⚠️ No frontmatter metadata found"
        
    // Check for review blocks
    printfn "Custom blocks found: %d types" parsedDoc.CustomBlocks.Count
    match parsedDoc.CustomBlocks.TryGetValue("review") with
    | true, reviewList when reviewList.Length > 0 ->
        printfn "✅ Review block found"
        match reviewList.[0] with
        | :? ReviewData as reviewData ->
            printfn "Review block data:"
            printfn "  Item: %s" reviewData.item
            printfn "  Rating: %.1f" reviewData.rating
            printfn "  Scale: %.1f" (reviewData.GetScale())
            match reviewData.item_type with
            | Some itemType -> printfn "  Type: %s" itemType
            | None -> ()
            match reviewData.additional_fields with
            | Some fields ->
                printfn "  Additional fields: %d" fields.Count
                for kvp in fields do
                    printfn "    %s: %A" kvp.Key kvp.Value
            | None -> ()
            printfn ""
        | _ -> printfn "⚠️ Unexpected review data type"
    | _ -> 
        printfn "⚠️ No review block found"
        
| Error parseError ->
    printfn "❌ Parse failed: %A" parseError

printfn ""
printfn "=== Testing Additional Files ==="

// Test with a few more files
let sampleFiles = 
    Directory.GetFiles(testDir, "*.md")
    |> Array.take (min 3 (Directory.GetFiles(testDir, "*.md").Length))

for file in sampleFiles do
    let fileName = Path.GetFileName(file)
    printfn "\nFile: %s" fileName
    match parseBookFromFile file with
    | Ok parsedDoc ->
        match parsedDoc.Metadata with
        | Some metadata ->
            printfn "  FM: %s by %s (%.1f⭐)" metadata.Title metadata.Author metadata.Rating
        | None -> printfn "  No frontmatter"
        
        match parsedDoc.CustomBlocks.TryGetValue("review") with
        | true, reviewList when reviewList.Length > 0 ->
            match reviewList.[0] with
            | :? ReviewData as reviewData ->
                printfn "  RB: %s (%.1f⭐)" reviewData.item reviewData.rating
            | _ -> ()
        | _ -> printfn "  No review block"
    | Error err ->
        printfn "  ❌ Error: %A" err

printfn ""
printfn "=== Test Complete ==="
