(*
    Migrate Book Reviews to New Format
    
    This script migrates existing book review files from the old format
    (metadata in frontmatter) to the new format (metadata in review block).
    
    Old format:
    ---
    title: "Book Title"
    author: "Author Name"
    isbn: "123"
    cover: "url"
    rating: 4.5
    date_published: "date"
    ---
    
    :::review
    item: "Book Title"
    itemType: "book"
    rating: 4.5
    additionalFields:
      author: "Author Name"
      isbn: "123"
    :::
    
    New format:
    ---
    title: "Book Title Review"
    post_type: "review"
    published_date: "date"
    tags: []
    ---
    
    :::review
    item: "Book Title"
    itemType: "book"
    author: "Author Name"
    isbn: "123"
    cover: "url"
    datePublished: "date"
    rating: 4.5
    :::
*)

#r "../bin/Debug/net10.0/PersonalSite.dll"

open System
open System.IO
open System.Text.RegularExpressions
open Domain
open ASTParsing

// Directory containing book reviews
let reviewsDir = Path.Combine(Directory.GetCurrentDirectory(), "_src", "reviews", "library")

printfn "=== Book Review Migration Script ==="
printfn "Directory: %s" reviewsDir
printfn ""

// Get all book review files
let reviewFiles = Directory.GetFiles(reviewsDir, "*.md")
printfn "Found %d files to process" reviewFiles.Length
printfn ""

// Function to extract review block content
let extractReviewBlock (content: string) : string option =
    let pattern = @":::review\s*\n(.*?)\n:::"
    let match' = Regex.Match(content, pattern, RegexOptions.Singleline)
    if match'.Success then
        Some match'.Groups.[1].Value
    else
        None

// Function to migrate a single file
let migrateFile (filePath: string) : bool =
    try
        let fileName = Path.GetFileName(filePath)
        
        // Parse the file to extract metadata
        match parseBookFromFile filePath with
        | Ok parsedDoc ->
            match parsedDoc.Metadata with
            | Some metadata ->
                // Extract review block data
                let reviewBlockOpt = 
                    match parsedDoc.CustomBlocks.TryGetValue("review") with
                    | true, reviewList when reviewList.Length > 0 ->
                        match reviewList.[0] with
                        | :? CustomBlocks.ReviewData as reviewData -> Some reviewData
                        | _ -> None
                    | _ -> None
                
                match reviewBlockOpt with
                | Some reviewData ->
                    // Check if already in new format by looking at raw content
                    let rawContent = File.ReadAllText(filePath)
                    let hasTopLevelAuthor = 
                        let reviewBlockPattern = @":::review\s*\n(.*?)\n:::"
                        let match' = Regex.Match(rawContent, reviewBlockPattern, RegexOptions.Singleline)
                        if match'.Success then
                            let blockContent = match'.Groups.[1].Value
                            // Check if author is a top-level field (not under additionalFields)
                            blockContent.Contains("author:") && not (blockContent.Contains("additionalFields:"))
                        else
                            false
                    
                    if hasTopLevelAuthor then
                        printfn "  ⏭️  %s - Already in new format" fileName
                        false  // Don't need to migrate
                    else
                        // Extract content after frontmatter and heading
                        let lines = File.ReadAllLines(filePath)
                        let contentStart = 
                            lines 
                            |> Array.skip 1
                            |> Array.tryFindIndex (fun line -> line.Trim() = "---")
                            |> Option.map (fun idx -> idx + 2)  // Skip closing --- and next line
                            |> Option.defaultValue 0
                        
                        // Find where review block starts
                        let reviewBlockStart = 
                            lines 
                            |> Array.skip contentStart
                            |> Array.tryFindIndex (fun line -> line.Trim() = ":::review")
                            |> Option.map (fun idx -> contentStart + idx)
                            |> Option.defaultValue (lines.Length)
                        
                        // Find where review block ends
                        let reviewBlockEnd =
                            lines
                            |> Array.skip (reviewBlockStart + 1)
                            |> Array.tryFindIndex (fun line -> line.Trim() = ":::")
                            |> Option.map (fun idx -> reviewBlockStart + idx + 1)
                            |> Option.defaultValue (lines.Length - 1)
                        
                        // Get content after review block
                        let contentAfterReview = 
                            if reviewBlockEnd + 1 < lines.Length then
                                lines.[(reviewBlockEnd + 1)..]
                                |> String.concat "\n"
                            else
                                ""
                        
                        // Generate new frontmatter
                        let newFrontmatter = 
                            let title = metadata.Title.Replace("\"", "\\\"")
                            let publishedDate = metadata.DatePublished
                            sprintf "---\ntitle: \"%s Review\"\npost_type: \"review\"\npublished_date: \"%s\"\ntags: []\n---" title publishedDate
                        
                        // Generate new review block with top-level fields
                        let newReviewBlock = 
                            let lines = ResizeArray<string>()
                            lines.Add(":::review")
                            lines.Add(sprintf "item: \"%s\"" (reviewData.Item.Replace("\"", "\\\"")))
                            lines.Add("itemType: \"book\"")
                            lines.Add(sprintf "author: \"%s\"" (metadata.Author.Replace("\"", "\\\"")))
                            if not (String.IsNullOrWhiteSpace(metadata.Isbn)) then
                                lines.Add(sprintf "isbn: \"%s\"" (metadata.Isbn.Replace("\"", "\\\"")))
                            if not (String.IsNullOrWhiteSpace(metadata.Cover)) then
                                lines.Add(sprintf "cover: \"%s\"" metadata.Cover)
                            lines.Add(sprintf "datePublished: \"%s\"" metadata.DatePublished)
                            lines.Add(sprintf "rating: %.1f" reviewData.Rating)
                            lines.Add(sprintf "scale: %.1f" reviewData.Scale)
                            
                            let summary = reviewData.Summary
                            if not (String.IsNullOrWhiteSpace(summary)) then
                                lines.Add(sprintf "summary: \"%s\"" (summary.Replace("\"", "\\\"").Replace("\n", " ")))
                            
                            match reviewData.ItemUrl with
                            | Some url -> lines.Add(sprintf "itemUrl: \"%s\"" url)
                            | None -> ()
                            
                            match reviewData.ImageUrl with
                            | Some url -> lines.Add(sprintf "imageUrl: \"%s\"" url)
                            | None -> ()
                            
                            lines.Add(":::")
                            String.concat "\n" lines
                        
                        // Generate heading
                        let heading = sprintf "# %s Review" metadata.Title
                        
                        // Combine everything
                        let newContent = sprintf "%s\n\n%s\n\n%s%s" newFrontmatter heading newReviewBlock contentAfterReview
                        
                        // Write the migrated file
                        File.WriteAllText(filePath, newContent)
                        printfn "  ✅ %s - Migrated successfully" fileName
                        true
                | None ->
                    printfn "  ⚠️  %s - No review block found" fileName
                    false
            | None ->
                printfn "  ⚠️  %s - No frontmatter found" fileName
                false
        | Error err ->
            printfn "  ❌ %s - Parse error: %A" fileName err
            false
    with
    | ex ->
        printfn "  ❌ %s - Exception: %s" (Path.GetFileName(filePath)) ex.Message
        false

// Migrate all files
let migratedCount = 
    reviewFiles
    |> Array.filter migrateFile
    |> Array.length

printfn ""
printfn "=== Migration Complete ==="
printfn "Total files: %d" reviewFiles.Length
printfn "Migrated: %d" migratedCount
printfn "Skipped: %d" (reviewFiles.Length - migratedCount)
