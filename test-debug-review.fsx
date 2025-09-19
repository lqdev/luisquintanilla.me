#r "bin/Debug/net9.0/PersonalSite.dll"

open System
open System.IO

// Test our functions directly on sample content

let sampleContent = File.ReadAllText("_src/reviews/library/serviceberry-robin-wall-kimerrer.md")

printfn "=== Testing Direct Function Calls ==="
printfn "Sample content has custom block: %b" (sampleContent.Contains(":::review"))

// Test if we can parse the content with Markdig and custom blocks
// Since we can't directly access the private functions, let's test the pattern
// that the functions should follow

// Test extracting the review block manually
let lines = sampleContent.Split('\n')
let reviewStartIndex = 
    lines 
    |> Array.findIndex (fun line -> line.Trim() = ":::review")

let reviewEndIndex = 
    lines 
    |> Array.findIndexBack (fun line -> line.Trim() = ":::")

if reviewStartIndex >= 0 && reviewEndIndex > reviewStartIndex then
    let reviewLines = lines.[reviewStartIndex+1..reviewEndIndex-1]
    printfn "Found review YAML content:"
    for line in reviewLines do
        printfn "  %s" line
    
    // Check if we can find the key fields
    let hasItem = reviewLines |> Array.exists (fun line -> line.Trim().StartsWith("item:"))
    let hasImageUrl = reviewLines |> Array.exists (fun line -> line.Trim().StartsWith("imageUrl:"))
    let hasRating = reviewLines |> Array.exists (fun line -> line.Trim().StartsWith("rating:"))
    
    printfn "\nKey fields found:"
    printfn "  item: %b" hasItem
    printfn "  imageUrl: %b" hasImageUrl  
    printfn "  rating: %b" hasRating
    
    if hasItem then
        let itemLine = reviewLines |> Array.find (fun line -> line.Trim().StartsWith("item:"))
        let itemValue = itemLine.Substring(itemLine.IndexOf(":") + 1).Trim().Trim('"')
        printfn "  Item value: %s" itemValue
        
    if hasImageUrl then
        let imageLine = reviewLines |> Array.find (fun line -> line.Trim().StartsWith("imageUrl:"))
        let imageValue = imageLine.Substring(imageLine.IndexOf(":") + 1).Trim().Trim('"')
        printfn "  Image URL: %s" imageValue
else
    printfn "❌ Could not find complete review block"

// Test the issue - maybe the content is being processed differently in timeline?
printfn "\n=== Checking if issue is in content processing ==="
printfn "Likely issues:"
printfn "1. Content passed to functions might not include the custom blocks"
printfn "2. The custom block parsing might fail silently"  
printfn "3. The extractReviewItemType function might not be called correctly"
printfn "4. There might be compilation errors that don't show in build"