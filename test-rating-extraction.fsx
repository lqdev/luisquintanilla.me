#!/usr/bin/env dotnet fsi

// Test rating extraction function

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

// Test with actual content
let testContent = """---
title: "The Four Agreements"
author: "Don Miguel Ruiz"
isbn: "9781878424945"
status: "Read"
date_published: "08/30/2025 19:08 -05:00"
---

# The Four Agreements Review

:::review
item: "The Four Agreements"
itemType: "book"
rating: 4.4
scale: 5.0
summary: "I purchased the paperback version but ended up listening to the audiobook version while doing chores..."
itemUrl: "https://openlibrary.org/works/OL27203W/The_Four_Agreements?edition="
imageUrl: "https://covers.openlibrary.org/b/id/15101528-L.jpg"
additionalFields:
  author: "Don Miguel Ruiz"
  isbn: "9781878424945"
  status: "Read"
:::

## Description

> In The Four Agreements, don Miguel Ruiz reveals the source of self-limiting beliefs that rob us of joy and create needless suffering. Based on ancient Toltec wisdom, The Four Agreements offer a powerful code of conduct that can rapidly transform our lives to a new experience of freedom, true happiness, and love.

## Review

I purchased the paperback version but ended up listening to the audiobook version while doing chores over the weekend (~2.5 hours). This book gets straight to the point. That point being, throughout our lives we enter into agreements with society that enforce beliefs and influence our behavior, happiness, and inner peace. By speaking with truth and love, not suffering from main character syndrome (not everything is about you), seeking to understand rather than make assumptions, and not striving for perfection but constant improvement, you can slowly create new agreements that are more in line with your higher self."""

printfn "Testing rating extraction..."
match extractRatingFromContent testContent with
| Some rating -> printfn "Successfully extracted rating: %f" rating
| None -> printfn "Failed to extract rating"