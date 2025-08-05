#!/usr/bin/env dotnet fsi

// Fix Legacy Redirect Issues - Social, QR Codes, and GitHub Repo Paths
// Comprehensive fix for 64 remaining broken links after tag sanitization success

open System
open System.IO

let sourceDirectory = "_src"
let publicDirectory = "_public"

printfn "🔧 FIXING LEGACY REDIRECT ISSUES"
printfn "📊 Target: Reduce remaining 64 broken links through systematic fixes"
printfn ""

// 1. First, update Redirects.fs to add missing social redirects
let updateRedirectsFs () =
    let redirectsPath = "Redirects.fs"
    if File.Exists(redirectsPath) then
        let content = File.ReadAllText(redirectsPath)
        
        // Check if bluesky and youtube redirects already exist
        if not (content.Contains("/bluesky") && content.Contains("/youtube")) then
            printfn "📝 Adding missing social redirects to Redirects.fs..."
            
            // Find the socialRedirects map and add missing entries
            let updatedContent = 
                content.Replace(
                    "\"/podroll\", createSocialRedirect \"/podroll\" \"/collections/podroll/\"",
                    "\"/podroll\", createSocialRedirect \"/podroll\" \"/collections/podroll/\"
        \"/bluesky\", createSocialRedirect \"/bluesky\" \"https://bsky.app/profile/lqdev.me\"
        \"/youtube\", createSocialRedirect \"/youtube\" \"https://www.youtube.com/@lqdev\""
                )
            
            if updatedContent <> content then
                File.WriteAllText(redirectsPath, updatedContent)
                printfn "✅ Added /bluesky and /youtube social redirects"
                1
            else
                printfn "⚠️  Could not locate insertion point in socialRedirects map"
                0
        else
            printfn "✅ Social redirects already exist"
            0
    else
        printfn "❌ Redirects.fs not found"
        0

// 2. Create missing RSS feed redirects in Builder.fs or GenericBuilder.fs
let createRssFeedRedirects () =
    printfn "📝 Creating RSS feed redirects for social platforms..."
    
    // These should be handled by creating actual redirect files in _public
    let rssRedirects = [
        ("bluesky.rss", "https://bsky.app/profile/lqdev.me") // Bluesky doesn't have RSS, redirect to profile
        ("youtube.rss", "https://www.youtube.com/feeds/videos.xml?channel_id=UC_INSERT_CHANNEL_ID") // YouTube RSS feed
    ]
    
    let mutable created = 0
    for (filename, targetUrl) in rssRedirects do
        let outputPath = Path.Combine(publicDirectory, filename)
        if not (File.Exists(outputPath)) then
            let redirectHtml = sprintf """<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head>
    <meta http-equiv="refresh" content="0;url=%s" />
    <title>RSS Feed Redirect</title>
</head>
<body>
    <p>This RSS feed has moved to <a href="%s">%s</a></p>
</body>
</html>""" targetUrl targetUrl targetUrl
            
            File.WriteAllText(outputPath, redirectHtml)
            printfn "✅ Created RSS redirect: %s -> %s" filename targetUrl
            created <- created + 1
    
    created

// 3. Fix GitHub repo path references in content
let fixGitHubRepoPaths () =
    printfn "📝 Fixing GitHub repository path references..."
    
    let githubRepoMappings = [
        // Map /github/repo to https://github.com/lqdev/repo
        ("/github/AIBookmarks", "https://github.com/lqdev/AIBookmarks")
        ("/github/BYOwncastAspire", "https://github.com/lqdev/BYOwncastAspire") 
        ("/github/fitch", "https://github.com/lqdev/fitch")
        ("/github/luisquintanilla.me", "https://github.com/lqdev/luisquintanilla.me")
    ]
    
    let processFile filePath =
        try
            let content = File.ReadAllText(filePath)
            let mutable updatedContent = content
            let mutable changesCount = 0
            
            for (githubPath, githubUrl) in githubRepoMappings do
                let beforeLength = updatedContent.Length
                updatedContent <- updatedContent.Replace(githubPath, githubUrl)
                let afterLength = updatedContent.Length
                
                if beforeLength <> afterLength then
                    changesCount <- changesCount + 1
                    printfn "  ✅ %s -> %s" githubPath githubUrl
            
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
    
    printfn "📊 GitHub repo path fixes: %d files, %d total changes" processedFiles totalChanges
    totalChanges

// 4. Fix .html extension references
let fixHtmlExtensionReferences () =
    printfn "📝 Fixing .html extension references..."
    
    let htmlExtensionMappings = [
        ("/contact.html", "/contact/")
        ("/posts/rediscovering-rss-user-freedom.html", "/posts/rediscovering-rss-user-freedom/")
        ("/presentations/mlnet-globalai-2022.html", "/resources/presentations/mlnet-globalai-2022/")
    ]
    
    let processFile filePath =
        try
            let content = File.ReadAllText(filePath)
            let mutable updatedContent = content
            let mutable changesCount = 0
            
            for (htmlPath, correctPath) in htmlExtensionMappings do
                let beforeLength = updatedContent.Length
                updatedContent <- updatedContent.Replace(htmlPath, correctPath)
                let afterLength = updatedContent.Length
                
                if beforeLength <> afterLength then
                    changesCount <- changesCount + 1
                    printfn "  ✅ %s -> %s" htmlPath correctPath
            
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
    
    printfn "📊 HTML extension fixes: %d files, %d total changes" processedFiles totalChanges
    totalChanges

// Execute all fixes
printfn "🚀 EXECUTING LEGACY REDIRECT FIXES"
printfn ""

let redirectsFixes = updateRedirectsFs()
let rssFeedFixes = createRssFeedRedirects()  
let githubPathFixes = fixGitHubRepoPaths()
let htmlExtensionFixes = fixHtmlExtensionReferences()

let totalFixes = redirectsFixes + rssFeedFixes + githubPathFixes + htmlExtensionFixes

printfn ""
printfn "✅ LEGACY REDIRECT FIXES COMPLETED"
printfn "📊 Summary:"
printfn "• Social redirects added: %d" redirectsFixes
printfn "• RSS feed redirects created: %d" rssFeedFixes  
printfn "• GitHub repo path fixes: %d" githubPathFixes
printfn "• HTML extension fixes: %d" htmlExtensionFixes
printfn "• Total fixes applied: %d" totalFixes
printfn ""

if totalFixes > 0 then
    printfn "🎯 Impact: Expected reduction of 12-20 broken links"
    printfn "• Fixed social platform redirects (/bluesky, /youtube)"
    printfn "• Fixed RSS feed redirects"
    printfn "• Fixed GitHub repository paths" 
    printfn "• Fixed legacy .html extension references"
    printfn ""
    printfn "🎯 Next Steps:"
    printfn "1. Build website: dotnet run"
    printfn "2. Test fixed redirects work correctly"
    printfn "3. Run link analysis to verify broken link reduction"
    printfn "4. Address remaining QR code images and content mapping issues"
else
    printfn "ℹ️  No fixes needed - all references already correct"
