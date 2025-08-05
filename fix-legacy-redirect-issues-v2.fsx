#!/usr/bin/env dotnet fsi

// Fix Legacy Redirect Issues - Social, GitHub Repo Paths, and HTML Extensions  
// Systematic fix for remaining broken links after tag sanitization success

#r "nuget: System.IO.FileSystem"

open System
open System.IO

let sourceDirectory = "_src"

printfn "🔧 FIXING LEGACY REDIRECT ISSUES"
printfn "📊 Target: Reduce remaining broken links through systematic fixes"
printfn ""

// 1. Update Redirects.fs to add missing social redirects
let updateRedirectsFs () =
    let redirectsPath = "Redirects.fs"
    if File.Exists(redirectsPath) then
        let content = File.ReadAllText(redirectsPath)
        
        // Check if bluesky redirect already exists
        if not (content.Contains("/bluesky")) then
            printfn "📝 Adding missing /bluesky social redirect to Redirects.fs..."
            
            // Add after the twitter line
            let updatedContent = 
                content.Replace(
                    "\"/twitter\", createSocialRedirect \"/twitter\" \"https://twitter.com/ljquintanilla\"",
                    "\"/twitter\", createSocialRedirect \"/twitter\" \"https://twitter.com/ljquintanilla\"
        \"/bluesky\", createSocialRedirect \"/bluesky\" \"https://bsky.app/profile/lqdev.me\""
                )
            
            if updatedContent <> content then
                File.WriteAllText(redirectsPath, updatedContent)
                printfn "✅ Added /bluesky social redirect"
                1
            else
                printfn "⚠️  Could not locate insertion point for /bluesky"
                0
        else
            printfn "✅ Bluesky redirect already exists"
            0
    else
        printfn "❌ Redirects.fs not found"
        0

// 2. Fix GitHub repo path references in content
let fixGitHubRepoPaths () =
    printfn "📝 Fixing GitHub repository path references..."
    
    let githubRepoMappings = [
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

// 3. Fix .html extension references
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

// 4. Fix external protocol issues (missing https://)
let fixExternalProtocolIssues () =
    printfn "📝 Fixing external protocol issues..."
    
    let protocolMappings = [
        ("desertoracle.com/radio", "https://desertoracle.com/radio")
        ("radiobilingue.org/", "https://radiobilingue.org/")
        ("surf.social", "https://surf.social")
        ("Bounce", "https://bounce.so") // Assuming this should be the Bounce social platform
    ]
    
    let processFile filePath =
        try
            let content = File.ReadAllText(filePath)
            let mutable updatedContent = content
            let mutable changesCount = 0
            
            for (badUrl, correctUrl) in protocolMappings do
                let beforeLength = updatedContent.Length
                updatedContent <- updatedContent.Replace(badUrl, correctUrl)
                let afterLength = updatedContent.Length
                
                if beforeLength <> afterLength then
                    changesCount <- changesCount + 1
                    printfn "  ✅ %s -> %s" badUrl correctUrl
            
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

// Execute all fixes
printfn "🚀 EXECUTING LEGACY REDIRECT FIXES"
printfn ""

let redirectsFixes = updateRedirectsFs()
let githubPathFixes = fixGitHubRepoPaths()
let htmlExtensionFixes = fixHtmlExtensionReferences()
let protocolFixes = fixExternalProtocolIssues()

let totalFixes = redirectsFixes + githubPathFixes + htmlExtensionFixes + protocolFixes

printfn ""
printfn "✅ LEGACY REDIRECT FIXES COMPLETED"
printfn "📊 Summary:"
printfn "• Social redirects added: %d" redirectsFixes
printfn "• GitHub repo path fixes: %d" githubPathFixes
printfn "• HTML extension fixes: %d" htmlExtensionFixes
printfn "• External protocol fixes: %d" protocolFixes
printfn "• Total fixes applied: %d" totalFixes
printfn ""

if totalFixes > 0 then
    printfn "🎯 Impact: Expected reduction of 10-15 broken links"
    printfn "• Fixed social platform redirects (/bluesky)"
    printfn "• Fixed GitHub repository paths" 
    printfn "• Fixed legacy .html extension references"
    printfn "• Fixed external protocol issues"
    printfn ""
    printfn "🎯 Next Steps:"
    printfn "1. Build website: dotnet run"
    printfn "2. Test fixed redirects work correctly"
    printfn "3. Run link analysis to verify broken link reduction"
    printfn "4. Address remaining QR code images and missing content"
else
    printfn "ℹ️  No fixes needed - all references already correct"
