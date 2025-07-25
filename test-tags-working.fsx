open System
open System.IO

printfn "=== Tag RSS Feeds Test ==="

// Test 1: Check if tag RSS feeds were generated
let tagDir = Path.Combine("_public", "tags")
let bashTagFeed = Path.Combine(tagDir, "bash", "feed.xml")
let fsharpTagFeed = Path.Combine(tagDir, "fsharp", "feed.xml")

printfn "Tag directory exists: %b" (Directory.Exists(tagDir))
printfn "Bash tag feed exists: %b" (File.Exists(bashTagFeed))
printfn "F# tag feed exists: %b" (File.Exists(fsharpTagFeed))

// Test 2: Count total tag feeds
if Directory.Exists(tagDir) then
    let tagDirs = Directory.GetDirectories(tagDir)
    let feedsCount = 
        tagDirs 
        |> Array.filter (fun dir -> File.Exists(Path.Combine(dir, "feed.xml")))
        |> Array.length
    
    printfn "Total tag directories: %d" tagDirs.Length
    printfn "Tag directories with RSS feeds: %d" feedsCount

// Test 3: Validate a specific feed contains categories
if File.Exists(bashTagFeed) then
    let content = File.ReadAllText(bashTagFeed)
    let hasCategories = content.Contains("<category>")
    printfn "Bash feed contains categories: %b" hasCategories
    
    if hasCategories then
        printfn "✅ Tag RSS feeds are working correctly!"
    else
        printfn "❌ Tag RSS feeds missing categories"
else
    printfn "❌ Test feed not found"

printfn "=== Test Complete ==="
