#!/usr/bin/env dotnet fsi

// Test script to understand the CardHtml structure after markdown conversion
open System.Text.RegularExpressions

// Simulate what ResponseProcessor.RenderCard creates, but with converted markdown content
let sampleCardHtml = """<article class="response-card h-entry"><h2><a href="/responses/test/">Test Response Title</a></h2><div class="response-target">→ <a href="https://example.com">https://example.com</a></div><div class="response-content">This is a response with some content. 

<h2>Important Section</h2>

<p>This section should be preserved.</p>

<h3>Subsection</h3>

<p>More content here.</p></div></article>"""

printfn "Sample CardHtml structure (after markdown conversion):"
printfn "%s" sampleCardHtml

// Test different regex approaches
printfn "\n--- Testing regex patterns ---"

// Current approach (removes all h1/h2) - BREAKS #286
let currentPattern = @"<h[12][^>]*>.*?</h[12]>"
let currentResult = Regex.Replace(sampleCardHtml, currentPattern, "", RegexOptions.IgnoreCase)
printfn "\nCurrent pattern (removes all h1/h2) - BREAKS #286:"
printfn "%s" currentResult

// Previous approach (only removes h1) - DOESN'T FIX #288  
let previousPattern = @"<h1[^>]*>.*?</h1>"
let previousResult = Regex.Replace(sampleCardHtml, previousPattern, "", RegexOptions.IgnoreCase)
printfn "\nPrevious pattern (only removes h1) - DOESN'T FIX #288:"
printfn "%s" previousResult

// Targeted approach (remove only title h2 with links) - SHOULD FIX BOTH
let targetedPattern = @"<h2[^>]*><a[^>]*>.*?</a></h2>"
let targetedResult = Regex.Replace(sampleCardHtml, targetedPattern, "", RegexOptions.IgnoreCase)
printfn "\nTargeted pattern (removes only h2 with links) - SHOULD FIX BOTH:"
printfn "%s" targetedResult