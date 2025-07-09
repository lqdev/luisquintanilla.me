#r "../bin/Debug/net9.0/PersonalSite.dll"

open System
open FeatureFlags

printfn "=== Snippet Migration Test ==="
printfn ""

// Test feature flag status
printfn "Testing feature flag functionality:"
printfn "Current NEW_SNIPPETS status: %b" (isEnabled ContentType.Snippets)
printfn ""

// Test validation
printfn "Testing feature flag validation:"
let validation = validateConfiguration() : Result<string, string>
printfn "Validation result: %A" validation
printfn ""

// Test with feature flag disabled (default)
Environment.SetEnvironmentVariable("NEW_SNIPPETS", "false")
printfn "After setting NEW_SNIPPETS=false: %b" (isEnabled ContentType.Snippets)

// Test with feature flag enabled
Environment.SetEnvironmentVariable("NEW_SNIPPETS", "true")
printfn "After setting NEW_SNIPPETS=true: %b" (isEnabled ContentType.Snippets)
printfn ""

// Test snippet processor creation
open GenericBuilder

printfn "Testing snippet processor creation:"
try
    let processor = SnippetProcessor.create()
    printfn "✅ Snippet processor created successfully"
    
    // Test with a real snippet file
    let snippetFiles = 
        System.IO.Directory.GetFiles("_src/snippets")
        |> Array.filter (fun f -> f.EndsWith(".md"))
        |> Array.take 1  // Just test with one file
        |> Array.toList
    
    if not (List.isEmpty snippetFiles) then
        let testFile = List.head snippetFiles
        printfn "Testing with file: %s" testFile
        
        match processor.Parse testFile with
        | Some snippet ->
            printfn "✅ Successfully parsed snippet: %s" snippet.Metadata.Title
            printfn "   Language: %s" snippet.Metadata.Language
            printfn "   Tags: %s" snippet.Metadata.Tags
        | None ->
            printfn "❌ Failed to parse snippet"
    else
        printfn "⚠️ No snippet files found in _src/snippets"
        
with
| ex -> printfn "❌ Error creating snippet processor: %s" ex.Message

printfn ""
printfn "=== Test Complete ==="
