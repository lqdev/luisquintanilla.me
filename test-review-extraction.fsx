#!/usr/bin/env dotnet fsi

#load "Views/LayoutViews.fs"
open LayoutViews

// Test the extractReviewItemType function with sample content
let sampleContent = """<div class="custom-review-block h-review"><div class="review-header"><h3 class="review-title p-name">The Four Agreements</h3><span class="item-type-badge badge bg-secondary">BOOK</span></div><div class="review-image"><img src="https://covers.openlibrary.org/b/id/15101528-L.jpg" alt="The Four Agreements" class="review-thumbnail img-fluid" /></div><div class="review-rating p-rating"><strong>Rating:</strong> ★★★★ (4.4/5.0)</div></div>"""

// The function is private, so I need to copy the logic to test it
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
                    Some (itemType.Substring(0, 1).ToUpper() + itemType.Substring(1).ToLower())
                else None
            else None
        else None
    with
    | ex -> 
        None

let result = extractReviewItemType sampleContent
printfn "Extracted item type: %A" result