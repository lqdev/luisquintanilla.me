#!/usr/bin/env dotnet fsi

// Test the extractReviewItemType logic
let extractReviewItemType (content: string) =
    try
        // The content is already HTML, so parse the rendered custom-review-block
        if content.Contains("item-type-badge") then
            // Extract the item type from the rendered HTML badge
            let startTag = "item-type-badge badge bg-secondary\">"
            let endTag = "</span>"
            let startIndex = content.IndexOf(startTag)
            if startIndex >= 0 then
                let startIndex = startIndex + startTag.Length
                let endIndex = content.IndexOf(endTag, startIndex)
                if endIndex > startIndex then
                    let itemType = content.Substring(startIndex, endIndex - startIndex).Trim()
                    // Convert from uppercase back to proper case
                    printfn "Extracted itemType: '%s'" itemType
                    Some (itemType.Substring(0, 1).ToUpper() + itemType.Substring(1).ToLower())
                else None
            else None
        else None
    with
    | ex -> 
        printfn "Exception: %s" ex.Message
        None

// Test with sample content from the serviceberry review
let sampleContent = """<span class="item-type-badge badge bg-secondary">BOOK</span>"""

match extractReviewItemType sampleContent with
| Some result -> printfn "Result: %s" result
| None -> printfn "No result found"

// Test with other item types
let movieContent = """<span class="item-type-badge badge bg-secondary">MOVIE</span>"""
match extractReviewItemType movieContent with
| Some result -> printfn "Movie result: %s" result  
| None -> printfn "No movie result found"