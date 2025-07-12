open System
open System.IO
open System.Security.Cryptography
open System.Text
open System.Diagnostics

// Test configuration
let responseDir = "_src/responses"
let sampleSize = 50 // Test with representative sample first, then full set
let outputDir = "_test_validation"
let legacyOutputDir = Path.Combine(outputDir, "legacy_responses")
let astOutputDir = Path.Combine(outputDir, "ast_responses")

// Create output directories
Directory.CreateDirectory(outputDir) |> ignore
Directory.CreateDirectory(legacyOutputDir) |> ignore  
Directory.CreateDirectory(astOutputDir) |> ignore

printfn "=== Response Migration Validation Test ==="
printfn "Testing legacy vs AST-based response processing"
printfn ""

// Get all response files
let allResponseFiles = 
    Directory.GetFiles(responseDir, "*.md")
    |> Array.sort

printfn $"Total response files found: {allResponseFiles.Length}"

// Select test sample (first N files for consistent testing)
let testFiles = 
    if sampleSize > 0 && sampleSize < allResponseFiles.Length then
        allResponseFiles.[..sampleSize-1]
    else
        allResponseFiles

printfn $"Testing with {testFiles.Length} files"
printfn ""

// Test 1: Legacy Response Processing
printfn "=== Test 1: Legacy Response Processing ==="
let mutable legacySuccessCount = 0
let mutable legacyErrorCount = 0

for responseFile in testFiles do
    try
        let fileName = Path.GetFileNameWithoutExtension(responseFile)
        let response = loadResponse responseFile
        
        // Generate legacy output using existing logic
        let legacyContent = responsePostView response false
        let outputPath = Path.Combine(legacyOutputDir, $"{fileName}.html")
        File.WriteAllText(outputPath, legacyContent)
        
        legacySuccessCount <- legacySuccessCount + 1
        if legacySuccessCount % 10 = 0 then
            printfn $"Legacy processing: {legacySuccessCount} files completed"
    with
    | ex -> 
        legacyErrorCount <- legacyErrorCount + 1
        printfn $"❌ Legacy error in {Path.GetFileName(responseFile)}: {ex.Message}"

printfn $"Legacy Results: {legacySuccessCount} success, {legacyErrorCount} errors"
printfn ""

// Test 2: AST Response Processing  
printfn "=== Test 2: AST Response Processing ==="
let mutable astSuccessCount = 0
let mutable astErrorCount = 0

for responseFile in testFiles do
    try
        let fileName = Path.GetFileNameWithoutExtension(responseFile)
        let response = ResponseProcessor.Parse responseFile
        
        // Generate AST output using new processor
        let astContent = ResponseProcessor.Render response false
        let outputPath = Path.Combine(astOutputDir, $"{fileName}.html")
        File.WriteAllText(outputPath, astContent)
        
        astSuccessCount <- astSuccessCount + 1
        if astSuccessCount % 10 = 0 then
            printfn $"AST processing: {astSuccessCount} files completed"
    with
    | ex -> 
        astErrorCount <- astErrorCount + 1
        printfn $"❌ AST error in {Path.GetFileName(responseFile)}: {ex.Message}"

printfn $"AST Results: {astSuccessCount} success, {astErrorCount} errors"
printfn ""

// Test 3: Content Comparison
printfn "=== Test 3: Content Comparison ==="
let mutable matchCount = 0
let mutable differenceCount = 0
let mutable comparisonErrors = 0

// Helper function to compute hash
let computeHash (content: string) =
    use sha256 = SHA256.Create()
    let bytes = Encoding.UTF8.GetBytes(content)
    let hashBytes = sha256.ComputeHash(bytes)
    Convert.ToHexString(hashBytes)

// Helper function to normalize content for comparison
let normalizeContent (content: string) =
    content
        .Replace("\r\n", "\n")  // Normalize line endings
        .Replace("\r", "\n")
        .Trim()

for responseFile in testFiles do
    try
        let fileName = Path.GetFileNameWithoutExtension(responseFile)
        let legacyPath = Path.Combine(legacyOutputDir, $"{fileName}.html")
        let astPath = Path.Combine(astOutputDir, $"{fileName}.html")
        
        if File.Exists(legacyPath) && File.Exists(astPath) then
            let legacyContent = File.ReadAllText(legacyPath) |> normalizeContent
            let astContent = File.ReadAllText(astPath) |> normalizeContent
            
            let legacyHash = computeHash legacyContent
            let astHash = computeHash astContent
            
            if legacyHash = astHash then
                matchCount <- matchCount + 1
            else
                differenceCount <- differenceCount + 1
                printfn $"❌ Content differs: {fileName}"
                
                // Save diff info for detailed analysis
                let diffPath = Path.Combine(outputDir, $"diff_{fileName}.txt")
                let diffContent = $"""=== LEGACY CONTENT ===
{legacyContent}

=== AST CONTENT ===  
{astContent}

=== HASHES ===
Legacy: {legacyHash}
AST: {astHash}
"""
                File.WriteAllText(diffPath, diffContent)
        else
            comparisonErrors <- comparisonErrors + 1
            printfn $"❌ Missing output files for {fileName}"
    with
    | ex -> 
        comparisonErrors <- comparisonErrors + 1
        printfn $"❌ Comparison error for {Path.GetFileName(responseFile)}: {ex.Message}"

printfn $"Comparison Results: {matchCount} matches, {differenceCount} differences, {comparisonErrors} errors"
printfn ""

// Test 4: IndieWeb Microformat Validation
printfn "=== Test 4: IndieWeb Microformat Validation ==="
let mutable microformatCheckCount = 0
let mutable microformatIssues = 0

for responseFile in testFiles do
    try
        let fileName = Path.GetFileNameWithoutExtension(responseFile)
        let astPath = Path.Combine(astOutputDir, $"{fileName}.html")
        
        if File.Exists(astPath) then
            let content = File.ReadAllText(astPath)
            microformatCheckCount <- microformatCheckCount + 1
            
            // Check for required microformat classes
            let hasHEntry = content.Contains("h-entry")
            let hasResponseType = content.Contains("response-type")
            let hasTargetUrl = content.Contains("u-repost-of") || content.Contains("u-bookmark-of") || content.Contains("u-in-reply-to")
            
            if not hasHEntry then
                microformatIssues <- microformatIssues + 1
                printfn $"❌ Missing h-entry class: {fileName}"
            
            if not hasResponseType then
                microformatIssues <- microformatIssues + 1
                printfn $"❌ Missing response-type indicator: {fileName}"
                
            if not hasTargetUrl then
                microformatIssues <- microformatIssues + 1
                printfn $"❌ Missing target URL microformat: {fileName}"
    with
    | ex -> 
        printfn $"❌ Microformat check error for {Path.GetFileName(responseFile)}: {ex.Message}"

printfn $"Microformat Results: {microformatCheckCount} checked, {microformatIssues} issues found"
printfn ""

// Test 5: Response Type Classification
printfn "=== Test 5: Response Type Classification ==="
let mutable typeCheckCount = 0
let mutable typeErrors = 0

for responseFile in testFiles do
    try
        let response = ResponseProcessor.Parse responseFile
        let originalResponse = loadResponse responseFile
        
        typeCheckCount <- typeCheckCount + 1
        
        // Verify response type preservation
        if response.Metadata.ResponseType <> originalResponse.Metadata.ResponseType then
            typeErrors <- typeErrors + 1
            printfn $"❌ Response type mismatch in {Path.GetFileName(responseFile)}: {originalResponse.Metadata.ResponseType} vs {response.Metadata.ResponseType}"
    with
    | ex -> 
        typeErrors <- typeErrors + 1
        printfn $"❌ Type check error for {Path.GetFileName(responseFile)}: {ex.Message}"

printfn $"Type Classification Results: {typeCheckCount} checked, {typeErrors} errors"
printfn ""

// Final Summary
printfn "=== VALIDATION SUMMARY ==="
printfn $"Files Tested: {testFiles.Length}"
printfn $"Legacy Processing: {legacySuccessCount}/{testFiles.Length} success ({(float legacySuccessCount / float testFiles.Length * 100.0):F1}%)"
printfn $"AST Processing: {astSuccessCount}/{testFiles.Length} success ({(float astSuccessCount / float testFiles.Length * 100.0):F1}%)"
printfn $"Content Matches: {matchCount}/{testFiles.Length} identical ({(float matchCount / float testFiles.Length * 100.0):F1}%)"
printfn $"Microformat Issues: {microformatIssues} found"
printfn $"Type Classification Errors: {typeErrors} found"
printfn ""

let overallSuccess = 
    legacySuccessCount = testFiles.Length &&
    astSuccessCount = testFiles.Length &&
    matchCount = testFiles.Length &&
    microformatIssues = 0 &&
    typeErrors = 0

if overallSuccess then
    printfn "✅ ALL TESTS PASSED - Migration ready for production"
else
    printfn "❌ ISSUES FOUND - Review test results before proceeding"
    if differenceCount > 0 then
        printfn $"   📄 Check diff files in {outputDir} for content analysis"

printfn $"Detailed output saved to: {outputDir}"
