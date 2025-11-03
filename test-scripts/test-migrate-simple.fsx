#r "../bin/Debug/net9.0/PersonalSite.dll"

open System.IO

let testFile = "/tmp/test-migrate.md"

printfn "=== Testing Migration on Single File ==="
printfn "File: %s" testFile
printfn ""

// Show first 400 chars
let before = File.ReadAllText(testFile)
printfn "Before (first 400 chars):"
printfn "%s" (before.Substring(0, min 400 before.Length))
printfn "..."
printfn ""

// Now we'll manually run the migration script
// (In real usage, we'd call it directly)

printfn "Manual migration would go here..."
printfn "For now, let's just run the full migration script"
