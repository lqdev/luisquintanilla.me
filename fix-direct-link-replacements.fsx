#!/usr/bin/env dotnet fsi

// Direct Link Replacement Implementation
// Fix broken links by replacing them with correct existing paths

open System
open System.IO

let sourceDirectory = "_src"

printfn "🔧 DIRECT LINK REPLACEMENT IMPLEMENTATION"
printfn "📊 Fixing broken links with direct path corrections"
printfn ""

// 1. FIX QR CODE PATHS (HIGH IMPACT - 9 links, files exist)
let fixQrCodePaths () =
    printfn "📝 Fixing QR code path references..."
    
    let qrCodeReplacements = [
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
    
    let processFile filePath =
        try
            let content = File.ReadAllText(filePath)
            let mutable updatedContent = content
            let mutable changesCount = 0
            
            for (wrongPath, correctPath) in qrCodeReplacements do
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
    
    let markdownFiles = 
        Directory.GetFiles(sourceDirectory, "*.md", SearchOption.AllDirectories)
        |> Array.filter (fun path -> not (path.Contains("\\node_modules\\") || path.Contains("\\.git\\")))
    
    let mutable totalChanges = 0
    let mutable processedFiles = 0
    
    for filePath in markdownFiles do
        let changes = processFile filePath
        if changes > 0 then
            processedFiles <- processedFiles + 1
            totalChanges <- totalChanges + changes
    
    printfn "📊 QR Code path fixes: %d files, %d total changes" processedFiles totalChanges
    totalChanges

// 2. FIX EXTERNAL DOMAIN PROTOCOLS (MEDIUM IMPACT - 4 links)
let fixExternalDomainProtocols () =
    printfn "📝 Fixing external domain protocol issues..."
    
    let protocolReplacements = [
        ("desertoracle.com/radio", "https://desertoracle.com/radio")
        ("radiobilingue.org/", "https://radiobilingue.org/")
        ("surf.social", "https://surf.social")
        ("Bounce", "https://bounce.so")
    ]
    
    let processFile filePath =
        try
            let content = File.ReadAllText(filePath)
            let mutable updatedContent = content
            let mutable changesCount = 0
            
            for (brokenDomain, correctUrl) in protocolReplacements do
                let beforeLength = updatedContent.Length
                updatedContent <- updatedContent.Replace(brokenDomain, correctUrl)
                let afterLength = updatedContent.Length
                
                if beforeLength <> afterLength then
                    changesCount <- changesCount + 1
                    printfn "  ✅ %s -> %s" brokenDomain correctUrl
            
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
    
    let markdownFiles = 
        Directory.GetFiles(sourceDirectory, "*.md", SearchOption.AllDirectories)
        |> Array.filter (fun path -> not (path.Contains("\\node_modules\\") || path.Contains("\\.git\\")))
    
    let mutable totalChanges = 0
    let mutable processedFiles = 0
    
    for filePath in markdownFiles do
        let changes = processFile filePath
        if changes > 0 then
            processedFiles <- processedFiles + 1
            totalChanges <- totalChanges + changes
    
    printfn "📊 External protocol fixes: %d files, %d total changes" processedFiles totalChanges
    totalChanges

// 3. FIX CONTENT TYPE MISMATCHES (ARCHITECTURE - 2 links)
let fixContentTypeMismatches () =
    printfn "📝 Fixing content type mismatch issues..."
    
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
    
    let markdownFiles = 
        Directory.GetFiles(sourceDirectory, "*.md", SearchOption.AllDirectories)
        |> Array.filter (fun path -> not (path.Contains("\\node_modules\\") || path.Contains("\\.git\\")))
    
    let mutable totalChanges = 0
    let mutable processedFiles = 0
    
    for filePath in markdownFiles do
        let changes = processFile filePath
        if changes > 0 then
            processedFiles <- processedFiles + 1
            totalChanges <- totalChanges + changes
    
    printfn "📊 Content type fixes: %d files, %d total changes" processedFiles totalChanges
    totalChanges

// 4. AUDIT AND FIX MISSING IMAGES (CONTENT AUDIT)
let auditMissingImages () =
    printfn "📝 Auditing missing image references..."
    
    let missingImageReferences = [
        ("/assets/images/notes/finally-feels-like-fall.jpg", "notes/finally-feels-like-fall.md")
        ("/assets/images/notes/rss-community-calendars.png", "notes/rss-community-calendars.md") 
        ("/assets/images/notes/spotify-wrapped-2024-0.png", "notes/spotify-wrapped-2024.md")
        ("/assets/images/notes/spotify-wrapped-2024-1.png", "notes/spotify-wrapped-2024.md")
        ("../images/notes/surrender-quantum-conscience.jpg", "notes/surrender-quantum-conscience.md")
    ]
    
    printfn "📊 Found %d missing image references:" (List.length missingImageReferences)
    for (imagePath, sourceFile) in missingImageReferences do
        let srcFilePath = Path.Combine(sourceDirectory, sourceFile)
        if File.Exists(srcFilePath) then
            printfn "  🔍 %s referenced in %s" imagePath sourceFile
        else
            printfn "  ❓ %s referenced in MISSING FILE %s" imagePath sourceFile
    
    printfn "ℹ️  Image audit complete - manual review needed for image creation/removal"
    0

// EXECUTE DIRECT REPLACEMENTS
printfn "🚀 EXECUTING DIRECT LINK REPLACEMENTS"
printfn ""

let qrCodeFixes = fixQrCodePaths()
printfn ""

let protocolFixes = fixExternalDomainProtocols()
printfn ""

let contentTypeFixes = fixContentTypeMismatches()
printfn ""

let imageAudit = auditMissingImages()
printfn ""

let totalFixes = qrCodeFixes + protocolFixes + contentTypeFixes

printfn "✅ DIRECT LINK REPLACEMENT COMPLETED"
printfn "📊 Summary:"
printfn "• QR code path corrections: %d changes" qrCodeFixes
printfn "• External protocol fixes: %d changes" protocolFixes
printfn "• Content type corrections: %d changes" contentTypeFixes
printfn "• Total direct replacements: %d" totalFixes
printfn ""

if totalFixes > 0 then
    printfn "🎯 Impact: Expected reduction of ~15 broken links through direct replacement"
    printfn "• Fixed QR code paths (files exist, now correct paths)"
    printfn "• Fixed external domain protocols"
    printfn "• Fixed content type mismatches"
    printfn ""
    printfn "🎯 Next Steps:"
    printfn "1. Build website: dotnet run"
    printfn "2. Run link analysis to verify reduction"
    printfn "3. Review missing images - create or remove references"
    printfn "4. Address remaining bootstrap cleanup and RSS issues"
else
    printfn "ℹ️  No direct replacements needed - all references already correct"
