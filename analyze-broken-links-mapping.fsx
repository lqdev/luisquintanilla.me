#!/usr/bin/env dotnet fsi

// Direct Link Replacement Analysis and Fix
// Map broken links to existing files and create direct replacements

open System
open System.IO

let sourceDirectory = "_src"
let publicDirectory = "_public"

printfn "🔍 DIRECT LINK REPLACEMENT ANALYSIS"
printfn "📊 Mapping broken links to existing files for direct replacement"
printfn ""

// 1. ANALYZE QR CODE ISSUE: Files exist but wrong paths
let analyzeQrCodePaths () =
    printfn "📝 Analyzing QR code path mappings..."
    
    let qrCodeMappings = [
        // Current broken links -> Existing file paths (need to fix references)
        ("/images/contact/qr-bluesky.png", "/assets/images/contact/qr-bluesky.png")
        ("/images/contact/qr-github.png", "/assets/images/contact/qr-github.png")
        ("/images/contact/qr-linkedin.png", "/assets/images/contact/qr-linkedin.png")
        ("/images/contact/qr-mastodon.png", "/assets/images/contact/qr-mastodon.png")
        ("/images/contact/qr-mecard.png", "/assets/images/contact/qr-mecard.png")
        ("/images/contact/qr-twitter.png", "/assets/images/contact/qr-twitter.png")
        ("/images/contact/qr-vcard.png", "/assets/images/contact/qr-vcard.png")
        ("/images/contact/qr-mecard.svg", "/assets/images/contact/qr-mecard.svg")
        ("../images/contact/qr-vcard.svg", "/assets/images/contact/qr-vcard.svg")
    ]
    
    // Verify files actually exist in _src
    let mutable existingFiles = 0
    for (brokenPath, expectedPath) in qrCodeMappings do
        let srcPath = Path.Combine(sourceDirectory, expectedPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar))
        if File.Exists(srcPath) then
            existingFiles <- existingFiles + 1
            printfn "  ✅ EXISTS: %s -> %s" brokenPath expectedPath
        else
            printfn "  ❌ MISSING: %s (expected at %s)" brokenPath srcPath
    
    printfn "📊 QR Code Analysis: %d/%d files exist and need path correction" existingFiles (List.length qrCodeMappings)
    qrCodeMappings

// 2. ANALYZE CONTENT TYPE MISMATCHES: responses vs bookmarks
let analyzeContentTypeMismatches () =
    printfn "📝 Analyzing content type mismatches..."
    
    // Check if files exist in responses/ but are referenced as bookmarks/
    let contentMismatches = [
        ("/bookmarks/pocket-shutting-down/", "/responses/pocket-shutting-down/")
        ("/bookmarks/resource-list-personal-web/", "/responses/resource-list-personal-web/")
    ]
    
    let mutable existingMismatches = 0
    for (brokenPath, correctPath) in contentMismatches do
        // Check if the source file exists in responses/
        let fileName = Path.GetFileName(correctPath.TrimEnd('/'))
        let srcPath = Path.Combine(sourceDirectory, "responses", fileName + ".md")
        if File.Exists(srcPath) then
            existingMismatches <- existingMismatches + 1
            printfn "  ✅ MISMATCH: %s -> %s (exists in responses/)" brokenPath correctPath
        else
            printfn "  ❌ NOT FOUND: %s (checked %s)" brokenPath srcPath
    
    printfn "📊 Content Mismatch Analysis: %d content type corrections needed" existingMismatches
    contentMismatches

// 3. ANALYZE MISSING IMAGE REFERENCES: notes images
let analyzeMissingImages () =
    printfn "📝 Analyzing missing image references..."
    
    let missingImages = [
        ("/assets/images/notes/finally-feels-like-fall.jpg", "notes/finally-feels-like-fall.md")
        ("/assets/images/notes/rss-community-calendars.png", "notes/rss-community-calendars.md") 
        ("/assets/images/notes/spotify-wrapped-2024-0.png", "notes/spotify-wrapped-2024.md")
        ("/assets/images/notes/spotify-wrapped-2024-1.png", "notes/spotify-wrapped-2024.md")
        ("../images/notes/surrender-quantum-conscience.jpg", "notes/surrender-quantum-conscience.md")
    ]
    
    let mutable existingImages = 0
    for (imagePath, referencingFile) in missingImages do
        let srcImagePath = Path.Combine(sourceDirectory, imagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar))
        let srcImagePath2 = srcImagePath.Replace("..\\", "") // Handle relative paths
        if File.Exists(srcImagePath) || File.Exists(srcImagePath2) then
            existingImages <- existingImages + 1
            printfn "  ✅ IMAGE EXISTS: %s (referenced in %s)" imagePath referencingFile
        else
            printfn "  ❌ IMAGE MISSING: %s (referenced in %s)" imagePath referencingFile
    
    printfn "📊 Missing Images Analysis: %d/%d images need attention" (List.length missingImages - existingImages) (List.length missingImages)
    missingImages

// 4. ANALYZE BOOTSTRAP ISSUES: legacy CSS references
let analyzeBootstrapIssues () =
    printfn "📝 Analyzing Bootstrap legacy issues..."
    
    // The bootstrap-icons.css reference from legacy index.html
    let bootstrapIssues = [
        ("bootstrap-icons.css", "Should reference /assets/css/bootstrap-icons-1.5.0/bootstrap-icons.css")
        ("/css/bootstrap.min.css", "Should be removed - we don't use Bootstrap anymore")
    ]
    
    for (brokenRef, explanation) in bootstrapIssues do
        printfn "  🚨 LEGACY: %s -> %s" brokenRef explanation
    
    printfn "📊 Bootstrap Issues: %d legacy references need cleanup" (List.length bootstrapIssues)
    bootstrapIssues

// 5. ANALYZE RSS FEED ISSUES: missing feeds  
let analyzeRssFeedIssues () =
    printfn "📝 Analyzing RSS feed issues..."
    
    let missingFeeds = [
        ("/bluesky.rss", "Should redirect to Bluesky profile (no RSS available)")
        ("/youtube.rss", "Should redirect to YouTube RSS feed")
        ("/posts/index.xml", "Should redirect to /blog.rss")
    ]
    
    for (missingFeed, solution) in missingFeeds do
        printfn "  📡 MISSING RSS: %s -> %s" missingFeed solution
    
    printfn "📊 RSS Issues: %d missing feeds need creation" (List.length missingFeeds)
    missingFeeds

// 6. ANALYZE EXTERNAL DOMAIN ISSUES: missing protocols
let analyzeExternalDomainIssues () =
    printfn "📝 Analyzing external domain issues..."
    
    let externalDomainIssues = [
        ("desertoracle.com/radio", "https://desertoracle.com/radio")
        ("radiobilingue.org/", "https://radiobilingue.org/")
        ("surf.social", "https://surf.social")
        ("Bounce", "https://bounce.so") // Assuming this is the Bounce platform
    ]
    
    for (brokenDomain, correctUrl) in externalDomainIssues do
        printfn "  🌐 EXTERNAL: %s -> %s" brokenDomain correctUrl
    
    printfn "📊 External Domain Issues: %d missing protocols need fixing" (List.length externalDomainIssues)
    externalDomainIssues

// EXECUTE ANALYSIS
printfn "🚀 EXECUTING COMPREHENSIVE BROKEN LINK ANALYSIS"
printfn ""

let qrCodeMappings = analyzeQrCodePaths()
printfn ""

let contentMismatches = analyzeContentTypeMismatches() 
printfn ""

let missingImages = analyzeMissingImages()
printfn ""

let bootstrapIssues = analyzeBootstrapIssues()
printfn ""

let rssFeedIssues = analyzeRssFeedIssues()
printfn ""

let externalDomainIssues = analyzeExternalDomainIssues()
printfn ""

// SUMMARY AND RECOMMENDATIONS
printfn "✅ ANALYSIS COMPLETE - DIRECT REPLACEMENT STRATEGY"
printfn ""
printfn "📊 CATEGORIZED BROKEN LINKS (64 total):"
printfn "• QR Code path fixes: %d items (files exist, wrong paths)" (List.length qrCodeMappings)
printfn "• Content type mismatches: %d items (responses vs bookmarks)" (List.length contentMismatches)
printfn "• Missing images: %d items (need to check/create)" (List.length missingImages)
printfn "• Bootstrap legacy cleanup: %d items (post-Bootstrap elimination)" (List.length bootstrapIssues)
printfn "• RSS feed creation: %d items (missing feed redirects)" (List.length rssFeedIssues)
printfn "• External domain protocols: %d items (missing https://)" (List.length externalDomainIssues)
printfn ""
printfn "🎯 DIRECT REPLACEMENT PRIORITY:"
printfn "1. **HIGH IMPACT**: QR code path corrections (9 links) - files exist, just wrong paths"
printfn "2. **MEDIUM IMPACT**: External domain protocol fixes (4 links) - simple https:// additions"
printfn "3. **CONTENT AUDIT**: Missing images (5 links) - check if images should exist or references removed"
printfn "4. **ARCHITECTURE**: Content type mismatches (2 links) - verify content classification"
printfn "5. **CLEANUP**: Bootstrap legacy issues (2 links) - post-migration cleanup"
printfn ""
printfn "🔧 NEXT STEPS:"
printfn "1. Fix QR code paths in contact.md and related files"
printfn "2. Fix external domain protocols in content files"
printfn "3. Audit missing images - create or remove references"
printfn "4. Review content type classifications"
printfn "5. Clean up Bootstrap legacy references"
