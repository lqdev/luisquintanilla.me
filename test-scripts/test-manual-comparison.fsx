// Compare snippet outputs between old and new systems
#r "../bin/Debug/net9.0/PersonalSite.dll"

open System
open System.IO
open OutputComparison

printfn "=== Snippet Output Comparison ==="
printfn ""

let oldDir = "_test_validation\\old_snippets"
let newDir = "_test_validation\\new_snippets"

if not (Directory.Exists(oldDir)) then
    printfn "ERROR: Old snippets directory not found: %s" oldDir
    exit 1

if not (Directory.Exists(newDir)) then
    printfn "ERROR: New snippets directory not found: %s" newDir
    exit 1

printfn "Comparing snippet outputs..."
printfn "Old directory: %s" oldDir
printfn "New directory: %s" newDir
printfn ""

// Use the OutputComparison module to validate outputs
let isValid = validateOutputs oldDir newDir

if isValid then
    printfn "🎉 SUCCESS: All snippet outputs are IDENTICAL!"
    printfn "✅ Migration validation PASSED"
    printfn ""
    printfn "The new AST-based snippet processor produces exactly the same output"
    printfn "as the existing string-based processor. Migration is safe to proceed."
else
    printfn "❌ FAILURE: Output differences detected"
    printfn "❌ Migration validation FAILED"
    printfn ""
    printfn "Manual review is required before proceeding with migration."
