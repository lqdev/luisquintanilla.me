// Fix medium priority broken link issues identified during investigation
open System
open System.IO
open System.Text.RegularExpressions

let contentTypeMappings = [|
    // Missing bookmark pages that are actually responses
    ("/bookmarks/pocket-shutting-down/", "/responses/pocket-shutting-down/")
    ("/bookmarks/resource-list-personal-web/", "/responses/resource-list-personal-web/")
|]

let tagEncodingFixes = [|
    // Fix malformed tag with HTML entity quote
    ("/tags/stabilityai&quot;/", "/tags/stabilityai/")
    ("/tags/stabilityai&quot;", "/tags/stabilityai")
    ("stabilityai&quot;", "stabilityai")
|]

let externalDomainFixes = [|
    // Fix external links missing protocol schemes
    ("href=\"radiobilingue.org/\"", "href=\"https://radiobilingue.org/\"")
    ("htmlUrl=\"desertoracle.com/radio\"", "htmlUrl=\"https://desertoracle.com/radio\"")
    ("href=\"desertoracle.com/radio\"", "href=\"https://desertoracle.com/radio\"")
|]

let publicDir = @"C:\Dev\website\_public"

let applyFixes fixes description =
    printfn "Applying %s fixes..." description
    let mutable totalFixed = 0
    let mutable filesFixed = 0
    
    for (oldPattern, newPattern) in fixes do
        printfn "  Replacing: %s → %s" oldPattern newPattern
        let mutable fixCount = 0
        
        // Get all HTML, XML, and OPML files
        let searchPatterns = [| "*.html"; "*.xml"; "*.opml" |]
        let allFiles = 
            searchPatterns
            |> Array.collect (fun pattern -> 
                Directory.GetFiles(publicDir, pattern, SearchOption.AllDirectories))
            |> Array.distinct
        
        for filePath in allFiles do
            try
                let content = File.ReadAllText(filePath)
                if content.Contains(oldPattern) then
                    let newContent = content.Replace(oldPattern, newPattern)
                    File.WriteAllText(filePath, newContent)
                    let count = (content.Length - newContent.Length) / (oldPattern.Length - newPattern.Length)
                    fixCount <- fixCount + count
                    printfn "    Fixed %d occurrence(s) in: %s" count (Path.GetRelativePath(publicDir, filePath))
            with
            | ex -> printfn "    Error processing %s: %s" filePath ex.Message
        
        if fixCount > 0 then
            totalFixed <- totalFixed + fixCount
            filesFixed <- filesFixed + 1
            printfn "  Total fixed for this pattern: %d occurrences" fixCount
        else
            printfn "  No occurrences found for this pattern"
    
    printfn "%s fixes complete: %d total fixes across %d patterns\n" description totalFixed filesFixed

// Apply all fix categories
applyFixes contentTypeMappings "Content Type Mapping"
applyFixes tagEncodingFixes "Tag Encoding"
applyFixes externalDomainFixes "External Domain Protocol"

printfn "Medium priority broken link fixes completed successfully!"
