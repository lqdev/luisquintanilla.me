#!/usr/bin/env dotnet fsi

#r "nuget: YamlDotNet, 16.3.0"

open System
open System.IO
open System.Text.RegularExpressions

// Copy of ReadingTimeService logic for testing
let calculateReadingTime (content: string) : int option =
    if String.IsNullOrWhiteSpace(content) then
        None
    else
        // Remove markdown code blocks (they're typically skimmed rather than read)
        let withoutCodeBlocks = Regex.Replace(content, @"```[\s\S]*?```", "", RegexOptions.Multiline)
        
        // Remove inline code
        let withoutInlineCode = Regex.Replace(withoutCodeBlocks, @"`[^`]+`", "")
        
        // Remove markdown links but keep the text
        let withoutLinks = Regex.Replace(withoutInlineCode, @"\[([^\]]+)\]\([^\)]+\)", "$1")
        
        // Remove HTML tags
        let withoutHtml = Regex.Replace(withoutLinks, @"<[^>]+>", "")
        
        // Count words (split on whitespace and filter empty strings)
        let words = 
            withoutHtml.Split([|' '; '\t'; '\n'; '\r'|], StringSplitOptions.RemoveEmptyEntries)
            |> Array.filter (fun w -> not (String.IsNullOrWhiteSpace(w)))
        
        let wordCount = words.Length
        
        // Calculate reading time (minimum 1 minute for any content)
        if wordCount > 0 then
            let minutes = Math.Ceiling(float wordCount / 200.0) |> int
            Some (max 1 minutes)
        else
            None

// Test with sample posts
let testContent1 = """
This is a short post with about 50 words. It should take less than 1 minute to read.
We want to ensure that even short content gets at least 1 minute reading time.
This helps provide consistent user experience across all content.
Testing testing testing more words here.
"""

let testContent2 = String.replicate 250 "word "  // 250 words = ~2 minutes

let testContent3 = """
# Long Post Test

This is a longer post with multiple paragraphs. We need to ensure the reading time
calculation is accurate for longer content. The standard reading speed is 200 words
per minute for average readers.

Here's a code block that should be excluded:
```fsharp
let add x y = x + y
let result = add 5 10
printfn "Result: %d" result
```

And here's more content with [links](http://example.com) that should have their
markdown syntax removed but text preserved.

This paragraph continues with more text to increase the word count. We want to test
that the calculation is accurate across various content types including technical
writing, blog posts, and documentation.
"""

printfn "Test 1 - Short content (~50 words):"
match calculateReadingTime testContent1 with
| Some minutes -> printfn "  Reading time: %d min" minutes
| None -> printfn "  No reading time calculated"

printfn "\nTest 2 - Medium content (250 words):"
match calculateReadingTime testContent2 with
| Some minutes -> printfn "  Reading time: %d min" minutes
| None -> printfn "  No reading time calculated"

printfn "\nTest 3 - Long content with code blocks and links:"
match calculateReadingTime testContent3 with
| Some minutes -> printfn "  Reading time: %d min" minutes
| None -> printfn "  No reading time calculated"

// Test with actual post file if available
let postsDir = "_src/posts"
if Directory.Exists(postsDir) then
    let postFiles = Directory.GetFiles(postsDir, "*.md") |> Array.take (min 3 (Directory.GetFiles(postsDir, "*.md").Length))
    
    printfn "\nTesting with actual post files:"
    for postFile in postFiles do
        let content = File.ReadAllText(postFile)
        let fileName = Path.GetFileName(postFile)
        match calculateReadingTime content with
        | Some minutes -> printfn "  %s: %d min" fileName minutes
        | None -> printfn "  %s: No reading time calculated" fileName

printfn "\nReading time calculation tests completed!"
