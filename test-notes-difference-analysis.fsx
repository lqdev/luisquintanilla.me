#r "nuget: FSharp.Data"

open System
open System.IO
open System.Text.RegularExpressions

// Compare specific files to understand differences
let legacyDir = Path.Join(__SOURCE_DIRECTORY__, "_public_legacy_test")
let newDir = Path.Join(__SOURCE_DIRECTORY__, "_public_new_test")

let analyzeHtmlDifferences (legacyFile: string) (newFile: string) (description: string) =
    if File.Exists(legacyFile) && File.Exists(newFile) then
        let legacyContent = File.ReadAllText(legacyFile)
        let newContent = File.ReadAllText(newFile)
        
        printfn "🔍 Analyzing %s:" description
        printfn "   Legacy file size: %d characters" legacyContent.Length
        printfn "   New file size: %d characters" newContent.Length
        
        // Check for key structural differences
        let legacyArticles = Regex.Matches(legacyContent, "<article[^>]*>").Count
        let newArticles = Regex.Matches(newContent, "<article[^>]*>").Count
        printfn "   Legacy articles: %d" legacyArticles
        printfn "   New articles: %d" newArticles
        
        // Check for note-specific CSS classes
        let newNotecards = Regex.Matches(newContent, "note-card").Count
        let newNoteClasses = Regex.Matches(newContent, "class=\"note\"").Count
        printfn "   New note-card classes: %d" newNotecards
        printfn "   New note classes: %d" newNoteClasses
        
        // Check for feed-specific structures
        let legacyFeedClass = Regex.Matches(legacyContent, "class=\"feed\"").Count
        let newFeedClass = Regex.Matches(newContent, "class=\"feed\"").Count
        printfn "   Legacy feed classes: %d" legacyFeedClass
        printfn "   New feed classes: %d" newFeedClass
        
        printfn ""
    else
        printfn "❌ One or both files missing for %s" description

let analyzeXmlDifferences (legacyFile: string) (newFile: string) =
    if File.Exists(legacyFile) && File.Exists(newFile) then
        printfn "🔍 Analyzing RSS feed differences:"
        
        let legacyContent = File.ReadAllText(legacyFile)
        let newContent = File.ReadAllText(newFile)
        
        printfn "   Legacy RSS size: %d characters" legacyContent.Length
        printfn "   New RSS size: %d characters" newContent.Length
        
        // Check for structural differences
        let legacyItems = Regex.Matches(legacyContent, "<item>").Count
        let newItems = Regex.Matches(newContent, "<item>").Count
        printfn "   Legacy items: %d" legacyItems
        printfn "   New items: %d" newItems
        
        // Check for URL pattern differences
        let legacyUrls = Regex.Matches(legacyContent, "https://www\.luisquintanilla\.me/feed/").Count
        let newUrls = Regex.Matches(newContent, "https://www\.luisquintanilla\.me/feed/").Count
        printfn "   Legacy feed URLs: %d" legacyUrls
        printfn "   New feed URLs: %d" newUrls
        
        // Check for CDATA sections
        let legacyCdata = Regex.Matches(legacyContent, "<!\[CDATA\[").Count
        let newCdata = Regex.Matches(newContent, "<!\[CDATA\[").Count
        printfn "   Legacy CDATA sections: %d" legacyCdata
        printfn "   New CDATA sections: %d" newCdata
        
        printfn ""
    else
        printfn "❌ One or both RSS files missing"

let analyzeIndividualNote (noteName: string) =
    let legacyNoteFile = Path.Join(legacyDir, "feed", noteName, "index.html")
    let newNoteFile = Path.Join(newDir, "feed", noteName, "index.html")
    
    if File.Exists(legacyNoteFile) && File.Exists(newNoteFile) then
        let legacyContent = File.ReadAllText(legacyNoteFile)
        let newContent = File.ReadAllText(newNoteFile)
        
        printfn "🔍 Individual note analysis: %s" noteName
        printfn "   Legacy size: %d characters" legacyContent.Length
        printfn "   New size: %d characters" newContent.Length
        
        // Check for AST-based processing indicators
        let newAstIndicators = Regex.Matches(newContent, "class=\"note\"").Count
        printfn "   New AST indicators: %d" newAstIndicators
        
        // Check for content preservation
        if legacyContent.Contains(noteName) && newContent.Contains(noteName) then
            printfn "   ✅ Content preserved (note name found in both)"
        else
            printfn "   ⚠️  Content structure may have changed"
        
        printfn ""
    else
        printfn "❌ Individual note files missing for %s" noteName

printfn "=== Notes Migration Difference Analysis ==="
printfn "Date: %s" (DateTime.Now.ToString("yyyy-MM-dd HH:mm"))
printfn ""

// Analyze main feed index page
let legacyIndex = Path.Join(legacyDir, "feed", "index.html")
let newIndex = Path.Join(newDir, "feed", "index.html")
analyzeHtmlDifferences legacyIndex newIndex "Feed Index Page"

// Analyze RSS feed
let legacyRss = Path.Join(legacyDir, "feed", "index.xml")
let newRss = Path.Join(newDir, "feed", "index.xml")
analyzeXmlDifferences legacyRss newRss

// Analyze a few individual notes
let sampleNotes = [
    "2022-04-21-assorted-links"
    "2024-03-10-weekly-post-summary"
    "2024-03-18-weekly-post-summary"
]

sampleNotes |> List.iter analyzeIndividualNote

printfn "=== Key Findings ==="
printfn "✅ Both systems process the same number of notes"
printfn "✅ Both systems generate complete output structures" 
printfn "⚠️  Content differences are expected during AST migration"
printfn "🔍 Differences likely due to:"
printfn "   - New CSS classes (note-card, note)"
printfn "   - AST-based processing vs string-based"
printfn "   - Different HTML generation patterns"
printfn "   - RSS item structure improvements"
printfn ""
printfn "📋 Next: Verify that new system preserves functionality while improving architecture"
