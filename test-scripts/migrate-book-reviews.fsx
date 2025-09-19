#!/usr/bin/env -S dotnet fsi

// Book Review Migration Script - Convert existing reviews to new custom blocks format
// This script will migrate all book reviews in _src/reviews/library to use the new ReviewData schema

#r "nuget: YamlDotNet, 13.1.1"

open System
open System.IO
open System.Text.RegularExpressions
open YamlDotNet.Serialization
open YamlDotNet.Serialization.NamingConventions

// Book frontmatter structure
[<CLIMutable>]
type BookMetadata = {
    title: string
    author: string
    isbn: string
    cover: string
    status: string
    rating: string
    source: string
    date_published: string
}

// Parse existing book review file
let parseBookReview (filePath: string) =
    if File.Exists(filePath) then
        let content = File.ReadAllText(filePath)
        
        // Extract frontmatter and content
        let frontmatterPattern = @"^---\s*\n(.*?)\n---\s*\n(.*)$"
        let frontmatterMatch = Regex.Match(content, frontmatterPattern, RegexOptions.Singleline)
        
        if frontmatterMatch.Success then
            let yamlContent = frontmatterMatch.Groups.[1].Value
            let markdownContent = frontmatterMatch.Groups.[2].Value
            
            let deserializer = 
                DeserializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .IgnoreUnmatchedProperties()
                    .Build()
            
            try
                let metadata = deserializer.Deserialize<BookMetadata>(yamlContent)
                Some (metadata, markdownContent)
            with
            | ex ->
                printfn "Failed to parse metadata for %s: %s" filePath ex.Message
                None
        else
            printfn "No frontmatter found in %s" filePath
            None
    else
        printfn "File not found: %s" filePath
        None

// Generate migrated review with custom blocks
let generateMigratedReview (metadata: BookMetadata) (content: string) =
    // Parse rating as float
    let rating = 
        match Double.TryParse(metadata.rating) with
        | (true, value) -> value
        | _ -> 0.0
    
    // Extract the review section from content if it exists
    let reviewPattern = @"##\s*Review\s*\n(.*?)(?=##|\Z)"
    let reviewMatch = Regex.Match(content, reviewPattern, RegexOptions.Singleline)
    let reviewText = 
        if reviewMatch.Success then
            reviewMatch.Groups.[1].Value.Trim()
        else
            // If no review section, use a default summary
            "Personal review of " + metadata.title
    
    // Generate summary (first sentence or up to 100 chars)
    let summary = 
        let firstSentence = reviewText.Split([|'.'|], 2).[0].Trim()
        if firstSentence.Length > 100 then
            firstSentence.Substring(0, 100).Trim() + "..."
        else
            firstSentence + if not (firstSentence.EndsWith(".")) then "." else ""
    
    // Generate the migrated format
    sprintf """---
title: "%s"
author: "%s"
isbn: "%s"
status: "%s"
date_published: "%s"
---

# %s Review

:::review
item: "%s"
itemType: "book"
rating: %.1f
scale: 5.0
summary: "%s"
itemUrl: "%s"
imageUrl: "%s"
additionalFields:
  author: "%s"
  isbn: "%s"
  status: "%s"
:::

%s""" 
        metadata.title metadata.author metadata.isbn metadata.status metadata.date_published
        metadata.title metadata.title rating summary metadata.source metadata.cover
        metadata.author metadata.isbn metadata.status
        content

// Migrate a single book review file
let migrateBookReview (inputPath: string) (outputPath: string) =
    match parseBookReview inputPath with
    | Some (metadata, content) ->
        let migratedContent = generateMigratedReview metadata content
        File.WriteAllText(outputPath, migratedContent)
        printfn "✅ Migrated: %s -> %s" (Path.GetFileName(inputPath)) (Path.GetFileName(outputPath))
        true
    | None ->
        printfn "❌ Failed to migrate: %s" (Path.GetFileName(inputPath))
        false

// Main migration function
let runMigration () =
    let sourceDir = "/home/runner/work/luisquintanilla.me/luisquintanilla.me/_src/reviews/library"
    let backupDir = "/home/runner/work/luisquintanilla.me/luisquintanilla.me/_src/reviews/library_backup"
    
    printfn "=== Book Review Migration Script ==="
    printfn ""
    
    if not (Directory.Exists(sourceDir)) then
        printfn "❌ Source directory not found: %s" sourceDir
        false
    else
        // Create backup directory
        if not (Directory.Exists(backupDir)) then
            Directory.CreateDirectory(backupDir) |> ignore
            printfn "📁 Created backup directory: %s" backupDir
        
        let reviewFiles = Directory.GetFiles(sourceDir, "*.md")
        printfn "📚 Found %d book reviews to migrate" reviewFiles.Length
        printfn ""
        
        let mutable successCount = 0
        let mutable failureCount = 0
        
        for filePath in reviewFiles do
            let fileName = Path.GetFileName(filePath)
            let backupPath = Path.Combine(backupDir, fileName)
            let outputPath = filePath // Overwrite original file
            
            // Backup original file
            File.Copy(filePath, backupPath, true)
            
            // Migrate the file
            if migrateBookReview filePath outputPath then
                successCount <- successCount + 1
            else
                failureCount <- failureCount + 1
                // Restore from backup if migration failed
                File.Copy(backupPath, outputPath, true)
        
        printfn ""
        printfn "=== Migration Results ==="
        printfn "✅ Successfully migrated: %d files" successCount
        printfn "❌ Failed migrations: %d files" failureCount
        printfn "📁 Backup files stored in: %s" backupDir
        printfn ""
        
        if failureCount = 0 then
            printfn "🎉 All book reviews successfully migrated to new custom blocks format!"
            true
        else
            printfn "⚠️ Some migrations failed. Check backup files for recovery."
            false

// Run the migration
let success = runMigration ()

if success then
    printfn ""
    printfn "Next steps:"
    printfn "1. ✅ Migration completed"
    printfn "2. 🔨 Build the website to test rendering"
    printfn "3. 🔍 Verify the review blocks display correctly"
    printfn "4. 🗑️ Remove backup directory if satisfied with results"
else
    printfn ""
    printfn "Migration incomplete. Please review errors and try again."