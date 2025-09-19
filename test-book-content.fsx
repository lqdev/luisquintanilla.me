#!/usr/bin/env dotnet fsi

// Simple test to see what book content looks like
open System.IO

let testFile = "_src/reviews/library/four-agreements-ruiz.md"
let content = File.ReadAllText(testFile)

printfn "=== FULL FILE CONTENT ==="
printfn "%s" content
printfn "=========================="

// Test my regex
let extractRatingFromContent (content: string) : float option =
    try
        // Use regex to find rating in :::review blocks - updated pattern to handle multi-line blocks
        let reviewBlockPattern = @":::review\s*\n(.*?)\n:::"
        let ratingPattern = @"rating:\s*([\d.]+)"
        
        let reviewMatches = System.Text.RegularExpressions.Regex.Matches(content, reviewBlockPattern, System.Text.RegularExpressions.RegexOptions.Singleline)
        
        printfn "Found %d review matches" reviewMatches.Count
        
        if reviewMatches.Count > 0 then
            let reviewContent = reviewMatches.[0].Groups.[1].Value
            printfn "Review content: %s" reviewContent
            let ratingMatch = System.Text.RegularExpressions.Regex.Match(reviewContent, ratingPattern)
            
            if ratingMatch.Success then
                printfn "Rating match: %s" ratingMatch.Groups.[1].Value
                match System.Double.TryParse(ratingMatch.Groups.[1].Value) with
                | (true, rating) -> Some rating
                | _ -> None
            else 
                printfn "No rating match found"
                None
        else 
            printfn "No review block found"
            None
    with
    | ex -> 
        printfn "Exception: %s" ex.Message
        None

printfn "\n=== TESTING REGEX ==="
match extractRatingFromContent content with
| Some rating -> printfn "Successfully extracted rating: %f" rating
| None -> printfn "Failed to extract rating"