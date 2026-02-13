#r "../bin/Debug/net9.0/PersonalSite.dll"

open System
open TagService

// Test cases for the enhanced tag processing
let testCases = [|
    // Issue-specific examples
    ("selfhosting", "selfhost")
    ("Selfhosting", "selfhost") 
    ("nationalparks", "nationalpark")
    ("NationalParks", "nationalpark")
    
    // High-impact plural consolidations
    ("websites", "website")
    ("Websites", "website")
    ("webmentions", "webmention")
    ("WebMentions", "webmention")
    ("transformers", "transformer") 
    ("Transformers", "transformer")
    ("agents", "agent")
    ("Agents", "agent")
    ("tokenizers", "tokenizer")
    ("videos", "video")
    ("tools", "tool")
    ("Tools", "tool")
    
    // Existing technology normalizations should still work
    (".NET", "dotnet")
    ("c#", "csharp")
    ("artificial intelligence", "artificial-intelligence")
    ("open source", "open-source")
    
    // Edge cases
    ("", "untagged")
    ("   ", "untagged")
    ("tool", "tool")  // Should remain singular
    ("website", "website")  // Should remain singular
|]

printfn "=== TAG PROCESSING VALIDATION ==="
printfn "Testing enhanced tag processing with duplicate consolidation..."
printfn ""

let mutable passed = 0
let mutable failed = 0

for (input, expected) in testCases do
    let result = processTagName input
    if result = expected then
        printfn "✅ PASS: \"%s\" -> \"%s\"" input result
        passed <- passed + 1
    else
        printfn "❌ FAIL: \"%s\" -> \"%s\" (expected: \"%s\")" input result expected
        failed <- failed + 1

printfn ""
printfn "=== TEST RESULTS ==="
printfn "Passed: %d" passed
printfn "Failed: %d" failed
printfn "Total: %d" (passed + failed)

if failed = 0 then
    printfn "🎉 All tests passed! Enhanced tag processing is working correctly."
else
    printfn "⚠️  Some tests failed. Please review the tag processing logic."

printfn ""
printfn "=== IMPACT ANALYSIS ==="

// Run the original analysis again to see the impact
let posts = Loaders.loadPosts "_src"
let notes = Loaders.loadFeed "_src" 
let responses = Loaders.loadReponses "_src"

let getEnhancedTagUsageCounts (posts: Domain.Post array) (notes: Domain.Post array) (responses: Domain.Response array) =
    let allTagUsages = ResizeArray<string>()
    
    for post in posts do
        if post.Metadata.Tags <> null then
            for tag in post.Metadata.Tags do
                allTagUsages.Add(processTagName tag)
    
    for note in notes do
        if note.Metadata.Tags <> null then
            for tag in note.Metadata.Tags do
                allTagUsages.Add(processTagName tag)
                
    for response in responses do
        if response.Metadata.Tags <> null then
            for tag in response.Metadata.Tags do
                allTagUsages.Add(processTagName tag)
    
    allTagUsages.ToArray()
    |> Array.groupBy id
    |> Array.map (fun (tag, occurrences) -> (tag, occurrences.Length))
    |> Array.sortByDescending snd

let enhancedTagCounts = getEnhancedTagUsageCounts posts notes responses

printfn "Enhanced tag processing results:"
printfn "Total unique processed tags: %d" enhancedTagCounts.Length

// Show the impact of consolidations
let consolidationImpact = [|
    ("website", ["websites"])
    ("webmention", ["webmentions"])
    ("transformer", ["transformers"])
    ("agent", ["agents"])
    ("tokenizer", ["tokenizers"])
    ("video", ["videos"])
    ("tool", ["tools"])
    ("selfhost", ["selfhosting"])
    ("nationalpark", ["nationalparks"])
|]

printfn ""
printfn "Consolidation Impact:"
for (primary, alts) in consolidationImpact do
    match enhancedTagCounts |> Array.tryFind (fun (tag, _) -> tag = primary) with
    | Some (_, count) -> printfn "  %s: %d uses (consolidated from %s)" primary count (String.Join(", ", alts))
    | None -> printfn "  %s: 0 uses (would consolidate %s)" primary (String.Join(", ", alts))

printfn ""
printfn "=== VALIDATION COMPLETE ==="