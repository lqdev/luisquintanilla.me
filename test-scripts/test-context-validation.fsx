#r "nuget: Markdig"
#r "nuget: YamlDotNet" 
#load "../Domain.fs"
#load "../CustomBlocks.fs"
#load "../MediaTypes.fs"
#load "../ASTParsing.fs"

open System
open System.IO
open Domain
open ASTParsing
open CustomBlocks

// Test custom block parsing and basic validation in different contexts

let testFile = "test-content/simple-review-test.md"

printfn "=== Phase 1D Step 3: Context Validation Testing ==="
printfn "Testing custom block parsing and validation in different contexts"
printfn ""

// Test 1: Parse document with custom blocks
printfn "1. Parsing document with custom blocks..."
let fileContent = File.ReadAllText(testFile)
let astResult = parseDocumentFromAst<PostDetails> fileContent

match astResult with
| Ok parsedDoc ->
    printfn "Document parsed successfully"
    printfn "Custom blocks found: %d types" parsedDoc.CustomBlocks.Count
    
    // Test 2: Validate custom block content structure
    printfn ""
    printfn "2. Validating custom block content structure..."
    
    for (blockType, objList) in parsedDoc.CustomBlocks |> Map.toSeq do
        printfn "Block type: %s (%d instances)" blockType objList.Length
        
        objList |> List.iteri (fun i obj ->
            printfn "  Instance %d: %s" (i+1) (obj.GetType().Name)
            
            // Try to cast to known types and validate structure
            match blockType with
            | "review" ->
                try
                    let review = obj :?> ReviewData
                    printfn "    Review: %s (Rating: %.1f/%.1f)" review.item_title review.rating review.max_rating
                with ex ->
                    printfn "    Failed to cast to ReviewData: %s" ex.Message
            | "media" ->
                try
                    let media = obj :?> MediaItem
                    printfn "    Media: %s (Type: %s)" media.uri media.media_type
                with ex ->
                    printfn "    Failed to cast to MediaItem: %s" ex.Message
            | _ ->
                printfn "    Object type: %s" (obj.GetType().Name))
    
    // Test 3: Test content processing pipeline integration
    printfn ""
    printfn "3. Testing content processing pipeline integration..."
    
    // Verify that AST parsing preserves metadata correctly
    match parsedDoc.Metadata with
    | Some metadata ->
        printfn "Metadata validation:"
        printfn "  Title: %s" metadata.Title
        printfn "  Tags: %A" metadata.Tags
        printfn "  Date: %s" metadata.Date
        printfn "  Post Type: %s" metadata.PostType
    | None ->
        printfn "No metadata found - this should not happen"
    
    // Test 4: Verify text content extraction
    printfn ""
    printfn "4. Testing text content extraction..."
    printfn "Text content length: %d characters" parsedDoc.TextContent.Length
    printfn "Raw markdown length: %d characters" parsedDoc.RawMarkdown.Length
    
    // Verify that custom blocks are excluded from text content but present in raw markdown
    let hasCustomBlockInText = parsedDoc.TextContent.Contains(":::review")
    let hasCustomBlockInRaw = parsedDoc.RawMarkdown.Contains(":::review")
    
    printfn "Custom block in text content: %b (should be false)" hasCustomBlockInText
    printfn "Custom block in raw markdown: %b (should be true)" hasCustomBlockInRaw
    
    // Test 5: Test different context scenarios
    printfn ""
    printfn "5. Testing different context scenarios..."
    
    // Scenario 1: Card rendering context (short excerpt)
    let cardContent = 
        if parsedDoc.TextContent.Length > 150 then
            parsedDoc.TextContent.Substring(0, 150) + "..."
        else
            parsedDoc.TextContent
    printfn "Card context (excerpt): %s" cardContent
    
    // Scenario 2: Feed rendering context (structured data)
    printfn "Feed context blocks: %d custom blocks to include" (parsedDoc.CustomBlocks |> Map.toSeq |> Seq.sumBy (snd >> List.length))
    
    // Scenario 3: Full page rendering context (all content)
    printfn "Full page context: %d chars text + %d custom blocks" parsedDoc.TextContent.Length (parsedDoc.CustomBlocks |> Map.toSeq |> Seq.sumBy (snd >> List.length))

| Error parseError ->
    printfn "Document parsing failed: %A" parseError

printfn ""

// Test 6: Test with comprehensive blocks file
printfn "6. Testing with comprehensive blocks file..."
let comprehensiveFile = "test-content/comprehensive-blocks-test.md"

try
    let comprehensiveContent = File.ReadAllText(comprehensiveFile)
    let comprehensiveResult = parseDocumentFromAst<PostDetails> comprehensiveContent
    
    match comprehensiveResult with
    | Ok parsedDoc ->
        printfn "Comprehensive test results:"
        printfn "  Custom block types: %d" parsedDoc.CustomBlocks.Count
        printfn "  Total custom blocks: %d" (parsedDoc.CustomBlocks |> Map.toSeq |> Seq.sumBy (snd >> List.length))
        
        // Validate all block types are present
        let expectedTypes = ["media"; "review"; "venue"; "rsvp"]
        let foundTypes = parsedDoc.CustomBlocks |> Map.toSeq |> Seq.map fst |> Set.ofSeq
        
        expectedTypes |> List.iter (fun expectedType ->
            if foundTypes.Contains(expectedType) then
                printfn "  ✓ %s blocks found" expectedType
            else
                printfn "  ✗ %s blocks missing" expectedType)
                
    | Error parseError ->
        printfn "Comprehensive test parsing failed: %A" parseError
        
with ex ->
    printfn "Error processing comprehensive file: %s" ex.Message

printfn ""
printfn "=== Context Validation Testing Complete ==="
