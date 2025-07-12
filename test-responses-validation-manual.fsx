open System
open System.IO

printfn "=== Response Migration Build Validation ==="
printfn "Testing both legacy and AST response processing systems"
printfn ""

// Test 1: Verify current build works with legacy system
printfn "=== Test 1: Legacy System Validation ==="
Environment.SetEnvironmentVariable("NEW_RESPONSES", "false")
printfn "Environment set: NEW_RESPONSES=false"
printfn "Run: dotnet run"
printfn "Expected: Build completes successfully with legacy response processing"
printfn ""

// Test 2: Verify new system works  
printfn "=== Test 2: AST System Validation ==="
printfn "Set environment: NEW_RESPONSES=true"
printfn "Run: dotnet run"
printfn "Expected: Build completes successfully with new ResponseProcessor"
printfn ""

// Test 3: Output comparison instructions
printfn "=== Test 3: Manual Output Comparison ==="
printfn "1. Build with NEW_RESPONSES=false, save _public/feed/responses/ as legacy_output"
printfn "2. Build with NEW_RESPONSES=true, save _public/feed/responses/ as ast_output"  
printfn "3. Compare files using diff tools:"
printfn "   - RSS feed: responses/index.xml"
printfn "   - Individual response pages: *.html files"
printfn "   - Directory structure and file count"
printfn ""

// Test 4: Microformat validation points
printfn "=== Test 4: IndieWeb Microformat Checklist ==="
printfn "Check AST-generated response pages for:"
printfn "✓ h-entry class on main article element"
printfn "✓ response-type indicator for bookmark/star/reply"
printfn "✓ u-repost-of, u-bookmark-of, or u-in-reply-to for target URLs"
printfn "✓ dt-published for publication dates"
printfn "✓ p-name for response titles"
printfn ""

// Test 5: Feature flag safety
printfn "=== Test 5: Feature Flag Safety Test ==="
printfn "Verify instant rollback capability:"
printfn "1. Deploy with NEW_RESPONSES=true (AST system)"
printfn "2. If issues found, set NEW_RESPONSES=false (instant legacy fallback)"
printfn "3. Rebuild and confirm legacy system restored"
printfn ""

// Get some stats
let responseDir = "_src/responses"
if Directory.Exists(responseDir) then
    let responseFiles = Directory.GetFiles(responseDir, "*.md")
    printfn $"Response files to process: {responseFiles.Length}"
    
    // Show sample of response types
    let sampleFiles = responseFiles |> Array.take (Math.Min(5, responseFiles.Length))
    printfn "Sample files for testing:"
    for file in sampleFiles do
        printfn $"  - {Path.GetFileName(file)}"
else
    printfn "❌ Response directory not found"

printfn ""
printfn "=== Ready for Manual Testing ==="
printfn "Follow the test steps above to validate both systems"

// Reset environment
Environment.SetEnvironmentVariable("NEW_RESPONSES", null)
