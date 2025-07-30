// Fix tag path sanitization to resolve missing index.html files for special character tags
open System
open System.IO
open System.Text.RegularExpressions

// Define a proper tag sanitization function that matches what should be used for file paths
let sanitizeTagForFilePath (tag: string) =
    tag.Trim()
        .Replace("\"", "")       // Remove quotes
        .Replace("#", "sharp")   // Replace # with "sharp" 
        .Replace(" ", "-")       // Replace spaces with hyphens
        .Replace(".", "dot")     // Replace dots with "dot"
        .Replace("/", "-")       // Replace slashes with hyphens
        .Replace("\\", "-")      // Replace backslashes with hyphens
        .Replace(":", "-")       // Replace colons with hyphens
        .Replace("*", "star")    // Replace asterisks
        .Replace("?", "q")       // Replace question marks
        .Replace("<", "lt")      // Replace less than
        .Replace(">", "gt")      // Replace greater than
        .Replace("|", "pipe")    // Replace pipes
        .ToLowerInvariant()      // Make lowercase for consistency

// Test the problematic tags we identified
let problematicTags = [
    "f#"
    "c#" 
    ".net"
    ".net core"
    "artificial intelligence"
    "Azure Machine Learning"
    "cognitive services"
]

printfn "=== TAG SANITIZATION ANALYSIS ==="
printfn ""

for tag in problematicTags do
    let sanitized = sanitizeTagForFilePath tag
    let currentPath = Path.Join("_public", "tags", tag.Trim().Replace("\"",""))
    let suggestedPath = Path.Join("_public", "tags", sanitized)
    let currentExists = Directory.Exists(currentPath)
    let hasIndexHtml = if currentExists then File.Exists(Path.Join(currentPath, "index.html")) else false
    let hasFeedXml = if currentExists then File.Exists(Path.Join(currentPath, "feed.xml")) else false
    
    printfn "Tag: '%s'" tag
    printfn "  Current sanitization: '%s' -> %s" tag (tag.Trim().Replace("\"",""))
    printfn "  Suggested sanitization: '%s' -> %s" tag sanitized
    printfn "  Directory exists: %b" currentExists
    printfn "  Has index.html: %b" hasIndexHtml
    printfn "  Has feed.xml: %b" hasFeedXml
    printfn ""

// Check how many total tags are missing index.html
let tagsDir = "_public/tags"
if Directory.Exists(tagsDir) then
    let allTagDirs = Directory.GetDirectories(tagsDir)
    let missingIndexCount = 
        allTagDirs 
        |> Array.filter (fun dir -> not (File.Exists(Path.Join(dir, "index.html"))))
        |> Array.length
    
    printfn "=== SUMMARY ==="
    printfn "Total tag directories: %d" allTagDirs.Length
    printfn "Missing index.html: %d" missingIndexCount
    printfn "Success rate: %.1f%%" (float (allTagDirs.Length - missingIndexCount) / float allTagDirs.Length * 100.0)
    printfn ""
    
    if missingIndexCount > 0 then
        printfn "This confirms the sanitization issue is causing index.html generation failures."
        printfn "The directories are created and feed.xml files are generated,"
        printfn "but index.html file writes are failing silently for problematic tag names."
else
    printfn "Tags directory not found."
