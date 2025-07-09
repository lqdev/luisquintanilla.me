#r "../bin/Debug/net9.0/PersonalSite.dll"

open System
open System.IO
open FeatureFlags
open OutputComparison

printfn "=== Snippet Output Validation Test ==="
printfn ""

// Create test directories
let oldOutputDir = Path.Join(Directory.GetCurrentDirectory(), "_test_output_old")
let newOutputDir = Path.Join(Directory.GetCurrentDirectory(), "_test_output_new")

printfn "Setting up test directories:"
printfn "Old output: %s" oldOutputDir
printfn "New output: %s" newOutputDir
printfn ""

// Clean and create directories
if Directory.Exists(oldOutputDir) then Directory.Delete(oldOutputDir, true)
if Directory.Exists(newOutputDir) then Directory.Delete(newOutputDir, true)
Directory.CreateDirectory(oldOutputDir) |> ignore
Directory.CreateDirectory(newOutputDir) |> ignore

// Save original output directory
let originalOutputDir = System.Environment.GetEnvironmentVariable("OUTPUT_DIR") 

try
    printfn "=== Step 1: Generate Old System Output ==="
    
    // Disable feature flag and build with old system
    Environment.SetEnvironmentVariable("NEW_SNIPPETS", "false")
    Environment.SetEnvironmentVariable("OUTPUT_DIR", oldOutputDir)
    
    printfn "Feature flag status: %b" (isEnabled ContentType.Snippets)
    printfn "Building with old snippet processor..."
    
    // We'll need to run the build manually since we can't call Program.main directly
    printfn "⚠️ Manual step required: Run 'dotnet run' with NEW_SNIPPETS=false and OUTPUT_DIR=%s" oldOutputDir
    printfn "Press Enter when old system build is complete..."
    Console.ReadLine() |> ignore
    
    printfn "=== Step 2: Generate New System Output ==="
    
    // Enable feature flag and build with new system
    Environment.SetEnvironmentVariable("NEW_SNIPPETS", "true")
    Environment.SetEnvironmentVariable("OUTPUT_DIR", newOutputDir)
    
    printfn "Feature flag status: %b" (isEnabled ContentType.Snippets)
    printfn "Building with new snippet processor..."
    
    printfn "⚠️ Manual step required: Run 'dotnet run' with NEW_SNIPPETS=true and OUTPUT_DIR=%s" newOutputDir
    printfn "Press Enter when new system build is complete..."
    Console.ReadLine() |> ignore
    
    printfn "=== Step 3: Compare Outputs ==="
    
    // Use the comprehensive output comparison from OutputComparison module
    printfn "Running complete output comparison between old and new snippet systems..."
    
    let isValid = validateOutputs oldOutputDir newOutputDir
    
    if isValid then
        printfn "🎉 SUCCESS: All snippet outputs are identical between old and new systems!"
        printfn ""
        printfn "✅ Migration validation PASSED - New snippet processor produces identical results"
    else
        printfn "❌ VALIDATION FAILED: Differences found between old and new systems"
        printfn ""
        printfn "Next steps to fix issues:"
        printfn "1. Review the differences listed above"
        printfn "2. Fix the new snippet processor implementation"
        printfn "3. Re-run this validation test"

finally
    // Restore original environment
    if not (String.IsNullOrEmpty(originalOutputDir)) then
        Environment.SetEnvironmentVariable("OUTPUT_DIR", originalOutputDir)
    else
        Environment.SetEnvironmentVariable("OUTPUT_DIR", null)
    
    Environment.SetEnvironmentVariable("NEW_SNIPPETS", "false")
    
    printfn ""
    printfn "Environment restored to defaults"
    printfn ""
    printfn "=== Test Complete ==="
    printfn ""
    printfn "Next Steps:"
    printfn "1. Review any differences found above"
    printfn "2. Fix issues in new snippet processor if needed"
    printfn "3. Re-run test until all outputs are identical"
    printfn "4. Proceed with custom block testing"
