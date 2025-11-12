// Test script to check additionalFields in review blocks
#r "../bin/Debug/net9.0/PersonalSite.dll"

open System
open System.IO
open Domain
open ASTParsing
open CustomBlocks

let testDir = "/home/runner/work/luisquintanilla.me/luisquintanilla.me/_src/reviews/library"

printfn "=== Checking Review Block AdditionalFields ==="
printfn ""

let files = Directory.GetFiles(testDir, "*.md") |> Array.take 5

for file in files do
    let fileName = Path.GetFileName(file)
    match parseBookFromFile file with
    | Ok parsedDoc ->
        match parsedDoc.CustomBlocks.TryGetValue("review") with
        | true, reviewList when reviewList.Length > 0 ->
            match reviewList.[0] with
            | :? ReviewData as reviewData ->
                printfn "File: %s" fileName
                printfn "  Item: %s" reviewData.item
                match reviewData.additional_fields with
                | Some fields when fields.Count > 0 ->
                    printfn "  Additional Fields:"
                    for kvp in fields do
                        printfn "    %s: %A" kvp.Key kvp.Value
                | _ -> printfn "  No additional fields"
                printfn ""
            | _ -> ()
        | _ -> ()
    | Error _ -> ()

printfn "=== Complete ===" 
