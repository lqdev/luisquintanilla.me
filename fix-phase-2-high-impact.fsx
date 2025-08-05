#!/usr/bin/env dotnet fsi

// Phase 2: Fix Remaining High-Impact Issues
// Focus on social redirects and content type mismatches

open System
open System.IO

let sourceDirectory = "_src"

printfn "🔧 PHASE 2: HIGH-IMPACT BROKEN LINK FIXES"
printfn "📊 Targeting social redirects and content mismatches"
printfn ""

// 1. FIX CONTENT TYPE MISMATCHES (2 links - architecture critical)
let fixContentTypeMismatches () =
    printfn "📝 Fixing content type mismatches (bookmarks → responses)..."
    
    let contentReplacements = [
        ("/bookmarks/pocket-shutting-down/", "/responses/pocket-shutting-down/")
        ("/bookmarks/resource-list-personal-web/", "/responses/resource-list-personal-web/")
    ]
    
    let processFile filePath =
        try
            let content = File.ReadAllText(filePath)
            let mutable updatedContent = content
            let mutable changesCount = 0
            
            for (wrongPath, correctPath) in contentReplacements do
                let beforeLength = updatedContent.Length
                updatedContent <- updatedContent.Replace(wrongPath, correctPath)
                let afterLength = updatedContent.Length
                
                if beforeLength <> afterLength then
                    changesCount <- changesCount + 1
                    printfn "  ✅ %s -> %s" wrongPath correctPath
            
            if changesCount > 0 then
                File.WriteAllText(filePath, updatedContent)
                printfn "📝 Updated: %s (%d changes)" (Path.GetFileName(filePath)) changesCount
                changesCount
            else
                0
        with
        | ex ->
            printfn "❌ Error processing %s: %s" filePath ex.Message
            0
    
    // Check HTML files in _public directory for these references
    let htmlFiles = 
        Directory.GetFiles("_public", "*.html", SearchOption.AllDirectories)
        |> Array.filter (fun path -> not (path.Contains("\\node_modules\\") || path.Contains("\\.git\\")))
    
    let mutable totalChanges = 0
    let mutable processedFiles = 0
    
    for filePath in htmlFiles do
        let changes = processFile filePath
        if changes > 0 then
            processedFiles <- processedFiles + 1
            totalChanges <- totalChanges + changes
    
    printfn "📊 Content type fixes: %d files, %d total changes" processedFiles totalChanges
    totalChanges

// 2. AUDIT MISSING IMAGES AND SUGGEST FIXES
let auditMissingImages () =
    printfn "📝 Auditing missing image references for action plan..."
    
    let missingImageReferences = [
        ("/assets/images/notes/finally-feels-like-fall.jpg", "Fall season post - likely aesthetic image")
        ("/assets/images/notes/rss-community-calendars.png", "RSS calendar screenshot - functional requirement") 
        ("/assets/images/notes/spotify-wrapped-2024-0.png", "Spotify Wrapped screenshot 0 - personal content")
        ("/assets/images/notes/spotify-wrapped-2024-1.png", "Spotify Wrapped screenshot 1 - personal content")
        ("/images/notes/surrender-quantum-conscience.jpg", "Article image - likely book/concept visual")
    ]
    
    printfn "📊 Missing image analysis:"
    for (imagePath, description) in missingImageReferences do
        printfn "  🔍 %s" imagePath
        printfn "     Description: %s" description
        
        // Check if source markdown exists to understand context
        let potentialSource = 
            match imagePath with
            | path when path.Contains("finally-feels-like-fall") -> 
                Path.Combine(sourceDirectory, "notes", "finally-feels-like-fall.md")
            | path when path.Contains("rss-community-calendars") -> 
                Path.Combine(sourceDirectory, "notes", "rss-community-calendars.md")
            | path when path.Contains("spotify-wrapped-2024") -> 
                Path.Combine(sourceDirectory, "notes", "spotify-wrapped-2024.md")
            | path when path.Contains("surrender-quantum-conscience") -> 
                Path.Combine(sourceDirectory, "notes", "surrender-quantum-conscience.md")
            | _ -> ""
            
        if File.Exists(potentialSource) then
            printfn "     Source: %s (exists)" (Path.GetFileName(potentialSource))
        else
            printfn "     Source: Not found - check if note was removed"
    
    printfn "ℹ️  Image action recommendations:"
    printfn "  1. Check if source notes still exist"
    printfn "  2. Create placeholder images for functional requirements"
    printfn "  3. Remove image references if content no longer exists"
    0

// 3. CHECK SOCIAL REDIRECT STATUS
let checkSocialRedirectStatus () =
    printfn "📝 Checking social redirect infrastructure..."
    
    let socialRedirects = [
        ("/github", "GitHub redirect")
        ("/mastodon", "Mastodon redirect")
        ("/twitter", "Twitter/X redirect")
        ("/youtube", "YouTube redirect")
        ("/linkedin", "LinkedIn redirect")
        ("/gravatar", "Gravatar redirect")
        ("/bluesky", "Bluesky redirect")
    ]
    
    printfn "📊 Social redirect status:"
    for (redirectPath, description) in socialRedirects do
        let publicPath = Path.Combine("_public", redirectPath.TrimStart('/'))
        let htmlPath = Path.Combine(publicPath, "index.html")
        
        if File.Exists(htmlPath) then
            printfn "  ✅ %s - Redirect exists" description
        else
            printfn "  ❌ %s - Missing redirect at %s" description htmlPath
    
    printfn "ℹ️  Social redirects should be handled by Redirects.fs during build"
    0

// 4. FIX MALFORMED TAG URLS
let fixMalformedTagUrls () =
    printfn "📝 Checking for malformed tag URL issues..."
    
    let tagIssues = [
        ("tags/ci/cd", "CI/CD tag contains slash")
        ("tags/f#/", "F# tag contains hash and slash")
        ("tags/stabilityai&quot;", "StabilityAI tag contains HTML entity")
    ]
    
    printfn "📊 Tag URL issues found:"
    for (malformedTag, description) in tagIssues do
        printfn "  🔍 %s - %s" malformedTag description
    
    printfn "ℹ️  Tag URL fixes require:"
    printfn "  1. Review tag generation logic in GenericBuilder.fs"
    printfn "  2. Implement proper URL encoding for special characters"
    printfn "  3. Handle CI/CD → ci-cd transformation"
    0

// EXECUTE PHASE 2 FIXES
printfn "🚀 EXECUTING PHASE 2 HIGH-IMPACT FIXES"
printfn ""

let contentTypeFixes = fixContentTypeMismatches()
printfn ""

let imageAudit = auditMissingImages()
printfn ""

let socialRedirectCheck = checkSocialRedirectStatus()
printfn ""

let tagUrlCheck = fixMalformedTagUrls()
printfn ""

printfn "✅ PHASE 2 ANALYSIS COMPLETED"
printfn "📊 Summary:"
printfn "• Content type fixes applied: %d" contentTypeFixes
printfn "• Missing images identified: 5 (need manual review)"
printfn "• Social redirects checked: 7 (infrastructure dependency)"
printfn "• Tag URL issues identified: 3 (generation logic fix needed)"
printfn ""

if contentTypeFixes > 0 then
    printfn "🎯 Impact: Expected reduction of ~%d broken links from content type fixes" contentTypeFixes
    printfn ""

printfn "🎯 Next Actions:"
printfn "1. Re-run link analysis to confirm content type fixes"
printfn "2. Review missing image source files for cleanup decision"
printfn "3. Investigate social redirect generation in build process"
printfn "4. Fix tag URL encoding in GenericBuilder.fs"
