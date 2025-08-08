#r "_bin/PersonalSite.dll"

open Views.TextOnlyViews
open Domain
open GenericBuilder.UnifiedFeeds

// Test HTML content with various formatting
let testHtml = """
<p>This paragraph has <strong>bold text</strong> and <em>italic text</em>.</p>
<p>Here's some <code>inline code</code> and more <strong>bold</strong> content.</p>
<p>Mixed formatting: <strong>bold with <em>nested italics</em></strong>.</p>
<p>Links should be preserved: <a href="https://example.com">example link</a>.</p>
"""

// Create a test content item
let testContent = {
    Title = "Test Formatting"
    Url = "/test/formatting/"
    Date = "2025-08-07 12:00 -05:00"
    ContentType = "posts"
    Tags = [|"test"|]
    CardHtml = ""
}

// Test the content processing
let result = textOnlyContentPage testContent testHtml

printfn "=== Testing Markdown Formatting in Text-Only Views ==="
printfn ""
printfn "Original HTML:"
printfn "%s" testHtml
printfn ""
printfn "Generated result contains the following formatting elements:"

if result.Contains("<strong>") then
    printfn "✅ Bold formatting (<strong>) working"
else
    printfn "❌ Bold formatting (<strong>) NOT working"

if result.Contains("<em>") then
    printfn "✅ Italic formatting (<em>) working"  
else
    printfn "❌ Italic formatting (<em>) NOT working"

if result.Contains("<code>") then
    printfn "✅ Code formatting (<code>) working"
else
    printfn "❌ Code formatting (<code>) NOT working"

if result.Contains("<a href=") then
    printfn "✅ Links preserved"
else
    printfn "❌ Links NOT preserved"

printfn ""
printfn "=== Test completed ==="
