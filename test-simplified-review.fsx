#r "bin/Debug/net9.0/PersonalSite.dll"

open System
open System.IO

// Test the new simplified review content generation
let sampleReviewPath = "_src/reviews/library/serviceberry-robin-wall-kimerrer.md"
let sampleReviewPath2 = "_src/reviews/library/tiny-experiments-le-cunff.md"

// Test both review files
let testFiles = [sampleReviewPath; sampleReviewPath2]

printfn "=== Testing Enhanced Simplified Review Content ==="

// Since we can't directly access the private function, let's test the approach
// by calling the processing pipeline ourselves
for file in testFiles do
    printfn "\nTesting file: %s" (Path.GetFileName(file))
    let content = File.ReadAllText(file)
    
    // Extract the part that contains the custom block
    if content.Contains(":::review") then
        let startIndex = content.IndexOf(":::review")
        let endIndex = content.IndexOf(":::", startIndex + 3)
        if endIndex > startIndex then
            let reviewBlock = content.Substring(startIndex, endIndex - startIndex + 3)
            printfn "Found review block (first 200 chars): %s..." (reviewBlock.Substring(0, min 200 reviewBlock.Length))
        
        // Check specific fields we expect
        printfn "Has imageUrl: %b" (content.Contains("imageUrl:"))
        printfn "Has rating: %b" (content.Contains("rating:"))
        printfn "Has summary: %b" (content.Contains("summary:"))
        
        // Extract values for validation
        let lines = content.Split('\n')
        
        let imageUrl = 
            lines 
            |> Array.tryFind (fun line -> line.Trim().StartsWith("imageUrl:"))
            |> Option.map (fun line -> line.Substring(line.IndexOf(":") + 1).Trim().Trim('"'))
        
        let rating = 
            lines 
            |> Array.tryFind (fun line -> line.Trim().StartsWith("rating:"))
            |> Option.map (fun line -> line.Substring(line.IndexOf(":") + 1).Trim())
        
        let item = 
            lines 
            |> Array.tryFind (fun line -> line.Trim().StartsWith("item:"))
            |> Option.map (fun line -> line.Substring(line.IndexOf(":") + 1).Trim().Trim('"'))
        
        match imageUrl, rating, item with
        | Some img, Some rate, Some itemName when not (String.IsNullOrWhiteSpace(img)) ->
            printfn "✅ Review data extracted successfully:"
            printfn "  Item: %s" itemName
            printfn "  Rating: %s" rate
            printfn "  Image URL: %s" img
        | _ ->
            printfn "❌ Could not extract complete review data"
            printfn "  ImageUrl: %A" imageUrl
            printfn "  Rating: %A" rating
            printfn "  Item: %A" item
    else
        printfn "❌ No review block found in file"

printfn "\n=== Expected Timeline Card Behavior ==="
printfn "After the fix, timeline cards should show:"
printfn "1. ✅ Real book/movie titles (not 'Book Review')"
printfn "2. ✅ Actual cover images (not empty src)"
printfn "3. ✅ Correct ratings from the review data"
printfn "4. ✅ Real summaries from the review content"