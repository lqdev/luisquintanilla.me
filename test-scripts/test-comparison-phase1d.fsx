#r "nuget: Markdig"
#r "nuget: YamlDotNet" 
#load "../Domain.fs"
#load "../CustomBlocks.fs"
#load "../MediaTypes.fs"
#load "../ASTParsing.fs"
#load "../Services/Markdown.fs"

open System
open System.IO
open Domain
open MarkdownService
open ASTParsing

// Test comparison between new AST parsing and existing string-based parsing

let testFile = "test-content/simple-review-test.md"

printfn "=== Phase 1D Step 2: Comparison Testing ==="
printfn "Comparing parseDocumentFromAst vs getContentAndMetadata"
printfn ""

// Read the test file content for both approaches
let fileContent = File.ReadAllText(testFile)

printfn "Test file: %s" testFile
printfn "File size: %d characters" fileContent.Length
printfn ""

// Test 1: Existing string-based parsing (using PostDetails as example)
printfn "1. Testing existing getContentAndMetadata approach (PostDetails)..."
let existingResult : YamlResult<PostDetails> = getContentAndMetadata<PostDetails> testFile

printfn "Existing approach results:"
printfn "  Title: %s" existingResult.Yaml.Title
printfn "  Tags: %A" existingResult.Yaml.Tags
printfn "  Date: %A" existingResult.Yaml.Date
printfn "  Content length: %d characters" existingResult.Content.Length
printfn "  Content preview: %s..." (existingResult.Content.Substring(0, min 100 existingResult.Content.Length))
printfn ""

// Test 2: New AST-based parsing
printfn "2. Testing new parseDocumentFromAst approach (PostDetails)..."
let astResult = parseDocumentFromAst<PostDetails> fileContent

match astResult with
| Ok parsedDoc ->
    printfn "AST approach results:"
    match parsedDoc.Metadata with
    | Some metadata ->
        printfn "  Title: %s" metadata.Title
        printfn "  Tags: %A" metadata.Tags  
        printfn "  Date: %A" metadata.Date
    | None ->
        printfn "  No metadata found"
    
    printfn "  Text content length: %d characters" parsedDoc.TextContent.Length
    printfn "  Custom blocks: %d types" parsedDoc.CustomBlocks.Count
    printfn "  Raw markdown length: %d characters" parsedDoc.RawMarkdown.Length
    printfn ""
    
    // Test 3: Content comparison
    printfn "3. Testing content comparison..."
    printfn "Existing content length: %d characters" existingResult.Content.Length
    printfn "AST text content length: %d characters" parsedDoc.TextContent.Length
    printfn "AST raw markdown length: %d characters" parsedDoc.RawMarkdown.Length
    printfn ""

    // Test 4: Metadata equivalence check
    printfn "4. Testing metadata equivalence..."
    match parsedDoc.Metadata with
    | Some metadata ->
        let metadataMatch = 
            existingResult.Yaml.Title = metadata.Title &&
            existingResult.Yaml.Tags = metadata.Tags &&
            existingResult.Yaml.Date = metadata.Date

        printfn "Metadata matches: %b" metadataMatch
        if not metadataMatch then
            printfn "  Title match: %b ('%s' vs '%s')" (existingResult.Yaml.Title = metadata.Title) existingResult.Yaml.Title metadata.Title
            printfn "  Tags match: %b (%A vs %A)" (existingResult.Yaml.Tags = metadata.Tags) existingResult.Yaml.Tags metadata.Tags
            printfn "  Date match: %b (%A vs %A)" (existingResult.Yaml.Date = metadata.Date) existingResult.Yaml.Date metadata.Date
    | None ->
        printfn "No metadata in AST result to compare"

    printfn ""

    // Test 5: Custom blocks detection
    printfn "5. Testing custom blocks detection..."
    printfn "Custom block types found: %d" parsedDoc.CustomBlocks.Count
    parsedDoc.CustomBlocks |> Map.iter (fun blockType contentList ->
        printfn "  %s: %d instances" blockType contentList.Length)

    printfn ""

| Error parseError ->
    printfn "AST parsing failed: %A" parseError
    printfn ""

// Test 6: Using comprehensive test file
printfn "6. Testing with comprehensive blocks file..."
let comprehensiveFile = "test-content/comprehensive-blocks-test.md"

try
    let comprehensiveContent = File.ReadAllText(comprehensiveFile)
    let comprehensiveResult = parseDocumentFromAst<PostDetails> comprehensiveContent
    
    match comprehensiveResult with
    | Ok parsedDoc ->
        printfn "Comprehensive test results:"
        printfn "  Text content length: %d characters" parsedDoc.TextContent.Length
        printfn "  Custom block types: %d" parsedDoc.CustomBlocks.Count
        printfn "  Custom block types found:"
        
        parsedDoc.CustomBlocks |> Map.iter (fun blockType contentList ->
            printfn "    - %s: %d instances" blockType contentList.Length)
    | Error parseError ->
        printfn "Comprehensive test parsing failed: %A" parseError
        
with ex ->
    printfn "Error processing comprehensive file: %s" ex.Message

printfn ""
printfn "=== Comparison Test Complete ==="
printfn ""
