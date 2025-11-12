#r "../bin/Debug/net9.0/PersonalSite.dll"

open OutputComparison
open System.IO

printfn "=== Output Comparison Module Test ==="
printfn ""

// Test 1: Create test directories and files
let testOldDir = "_test_old"
let testNewDir = "_test_new"

printfn "Test 1: Setting up test directories"
if Directory.Exists(testOldDir) then Directory.Delete(testOldDir, true)
if Directory.Exists(testNewDir) then Directory.Delete(testNewDir, true)

Directory.CreateDirectory(testOldDir) |> ignore
Directory.CreateDirectory(testNewDir) |> ignore

// Create test files
File.WriteAllText(Path.Combine(testOldDir, "identical.txt"), "This content is the same")
File.WriteAllText(Path.Combine(testNewDir, "identical.txt"), "This content is the same")

File.WriteAllText(Path.Combine(testOldDir, "different.txt"), "Old content\nLine 2\nLine 3")
File.WriteAllText(Path.Combine(testNewDir, "different.txt"), "New content\nLine 2\nLine 4")

File.WriteAllText(Path.Combine(testOldDir, "only-in-old.txt"), "Only in old directory")
File.WriteAllText(Path.Combine(testNewDir, "only-in-new.txt"), "Only in new directory")

printfn "✅ Test directories and files created"
printfn ""

// Test 2: Compare outputs
printfn "Test 2: Running output comparison"
let summary = OutputComparison.compareOutputs testOldDir testNewDir
printfn ""

// Test 3: Print summary and validate
printfn "Test 3: Validation summary"
let isValid = OutputComparison.printSummary summary

// Test 4: Expected results validation
printfn "Test 4: Expected results validation"
let expectedResults = [
    ("Should have 4 total files", summary.TotalFiles = 4)
    ("Should have 1 matching file", summary.MatchingFiles = 1)
    ("Should have 1 different file", summary.DifferentFiles = 1)
    ("Should have 1 missing in old", summary.MissingInOld = 1)
    ("Should have 1 missing in new", summary.MissingInNew = 1)
    ("Overall validation should fail", not isValid)
]

expectedResults |> List.iter (fun (description, result) ->
    let status = if result then "✅ PASS" else "❌ FAIL"
    printfn $"  {status}: {description}")

printfn ""

// Test 5: Test identical outputs
printfn "Test 5: Testing identical outputs"
let testIdenticalDir = "_test_identical"
if Directory.Exists(testIdenticalDir) then Directory.Delete(testIdenticalDir, true)
Directory.CreateDirectory(testIdenticalDir) |> ignore

File.WriteAllText(Path.Combine(testIdenticalDir, "file1.txt"), "Same content")
File.WriteAllText(Path.Combine(testOldDir, "file1.txt"), "Same content")

let identicalSummary = OutputComparison.compareOutputs testOldDir testIdenticalDir
let identicalFiles = identicalSummary.Results |> List.filter (fun r -> r.FilePath = "file1.txt" && r.ContentsMatch)
let hasIdenticalFile = identicalFiles.Length > 0

let identicalResult = if hasIdenticalFile then "PASS" else "FAIL"
printfn $"✅ Identical file test: {identicalResult}"
printfn ""

// Cleanup
printfn "Cleaning up test directories"
Directory.Delete(testOldDir, true)
Directory.Delete(testNewDir, true)
Directory.Delete(testIdenticalDir, true)

printfn "✅ Cleanup complete"
printfn ""
printfn "=== Output Comparison Module Test Complete ==="
