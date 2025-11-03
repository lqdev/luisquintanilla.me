(*
    Process GitHub Issue Template for Review Creation
    
    This script processes GitHub issue template data to create a review post.
    Usage: dotnet fsi process-review-issue.fsx -- "review_type" "item_name" "rating" "summary" "content" "pros" "cons" "item_url" "image_url" "additional_fields_json" "tags" "slug"
*)

#r "../bin/Debug/net9.0/PersonalSite.dll"

open System
open System.IO
open System.Text.RegularExpressions
open System.Text.Json

// Get command line arguments
let args = fsi.CommandLineArgs |> Array.skip 1

// Validate arguments
if args.Length < 5 then
    printfn "❌ Error: Missing required arguments"
    printfn "Usage: dotnet fsi process-review-issue.fsx -- \"review_type\" \"item_name\" \"rating\" \"summary\" \"content\" \"pros\" \"cons\" \"item_url\" \"image_url\" \"additional_fields_json\" \"tags\" \"slug\""
    exit 1

let reviewType = args.[0].Trim()
let itemName = args.[1].Trim()
let ratingStr = args.[2].Trim()
let summary = args.[3].Trim()
let content = args.[4].Trim()
let prosInput = if args.Length > 5 && not (String.IsNullOrWhiteSpace(args.[5])) then Some(args.[5].Trim()) else None
let consInput = if args.Length > 6 && not (String.IsNullOrWhiteSpace(args.[6])) then Some(args.[6].Trim()) else None
let itemUrl = if args.Length > 7 && not (String.IsNullOrWhiteSpace(args.[7])) then Some(args.[7].Trim()) else None
let imageUrl = if args.Length > 8 && not (String.IsNullOrWhiteSpace(args.[8])) then Some(args.[8].Trim()) else None
let additionalFieldsJson = if args.Length > 9 && not (String.IsNullOrWhiteSpace(args.[9])) then Some(args.[9].Trim()) else None
let tagsInput = if args.Length > 10 && not (String.IsNullOrWhiteSpace(args.[10])) then Some(args.[10].Trim()) else None
let customSlug = if args.Length > 11 && not (String.IsNullOrWhiteSpace(args.[11])) then Some(args.[11].Trim()) else None

// Validation
if String.IsNullOrWhiteSpace(reviewType) then
    printfn "❌ Error: Review type is required"
    exit 1

if String.IsNullOrWhiteSpace(itemName) then
    printfn "❌ Error: Item name is required"
    exit 1

if String.IsNullOrWhiteSpace(ratingStr) then
    printfn "❌ Error: Rating is required"
    exit 1

// Parse rating (should be a simple float like "4.5")
let rating = 
    match Double.TryParse(ratingStr) with
    | true, r -> r
    | false, _ -> 
        printfn "❌ Error: Invalid rating format: %s" ratingStr
        exit 1

// Validate rating range
if rating < 1.0 || rating > 5.0 then
    printfn "❌ Error: Rating must be between 1.0 and 5.0, got: %f" rating
    exit 1

// Content is now optional - can be added later
// If content is provided, it will be used; if not, we'll generate a minimal post

// Slug generation and sanitization
let sanitizeSlug (slug: string) =
    slug.ToLowerInvariant()
        .Replace(" ", "-")
        .Replace("_", "-")
    |> fun s -> Regex.Replace(s, @"[^a-z0-9\-]", "")
    |> fun s -> Regex.Replace(s, @"-+", "-")
    |> fun s -> s.Trim('-')

let generateSlug (title: string) =
    let baseSlug = sanitizeSlug title
    let timestamp = DateTimeOffset.Now.ToString("yyyy-MM-dd")
    sprintf "%s-%s" baseSlug timestamp

let slug = 
    match customSlug with
    | Some s when not (String.IsNullOrWhiteSpace(s)) -> sanitizeSlug s
    | _ -> generateSlug itemName

// Process tags
let tags = 
    match tagsInput with
    | Some t when not (String.IsNullOrWhiteSpace(t)) ->
        t.Split(',') 
        |> Array.map (fun tag -> tag.Trim().ToLowerInvariant())
        |> Array.filter (fun tag -> not (String.IsNullOrWhiteSpace(tag)))
    | _ -> [||]

// Process pros and cons
let pros = 
    match prosInput with
    | Some p -> 
        p.Split('\n') 
        |> Array.map (fun line -> line.Trim())
        |> Array.filter (fun line -> not (String.IsNullOrWhiteSpace(line)))
    | None -> [||]

let cons = 
    match consInput with
    | Some c -> 
        c.Split('\n') 
        |> Array.map (fun line -> line.Trim())
        |> Array.filter (fun line -> not (String.IsNullOrWhiteSpace(line)))
    | None -> [||]

// Parse additional fields from JSON
let additionalFields = 
    match additionalFieldsJson with
    | Some json when not (String.IsNullOrWhiteSpace(json)) ->
        try
            let doc = JsonDocument.Parse(json)
            let mutable fields = []
            for prop in doc.RootElement.EnumerateObject() do
                if not (String.IsNullOrWhiteSpace(prop.Value.ToString())) then
                    fields <- (prop.Name, prop.Value.ToString()) :: fields
            fields |> List.rev
        with
        | ex -> 
            printfn "⚠️ Warning: Could not parse additional fields JSON: %s" ex.Message
            []
    | _ -> []

// Generate current timestamp in EST
let now = DateTimeOffset.Now.ToOffset(TimeSpan.FromHours(-5.0))
let timestamp = now.ToString("M/d/yyyy h:mm tt zzz")

// Generate frontmatter - use simplified Post schema for all review types (metadata goes in review block)
let frontmatter = 
    let tagsString = 
        if tags.Length = 0 then "[]"
        else sprintf "[%s]" (tags |> Array.map (sprintf "\"%s\"") |> String.concat ",")
    let titleEsc = itemName.Replace("\"", "\\\"")
    sprintf "---\ntitle: \"%s Review\"\npost_type: \"review\"\npublished_date: \"%s\"\ntags: %s\n---" titleEsc timestamp tagsString

// Generate review block YAML with top-level book fields
let generateReviewBlock () =
    let lines = ResizeArray<string>()
    
    lines.Add(":::review")
    lines.Add(sprintf "item: \"%s\"" (itemName.Replace("\"", "\\\"")))
    lines.Add(sprintf "itemType: \"%s\"" reviewType)
    
    // Add book-specific fields at top level (not in additionalFields)
    if reviewType.ToLower() = "book" then
        let author = 
            match additionalFields |> List.tryFind (fun (k, _) -> k.ToLower() = "author") with
            | Some (_, v) -> v
            | None -> "Unknown"
        let isbn = 
            match additionalFields |> List.tryFind (fun (k, _) -> k.ToLower() = "isbn") with
            | Some (_, v) -> v
            | None -> ""
        
        lines.Add(sprintf "author: \"%s\"" (author.Replace("\"", "\\\"")))
        if not (String.IsNullOrWhiteSpace(isbn)) then
            lines.Add(sprintf "isbn: \"%s\"" (isbn.Replace("\"", "\\\"")))
        
        // Add cover field if imageUrl provided for books
        match imageUrl with
        | Some url -> lines.Add(sprintf "cover: \"%s\"" url)
        | None -> ()
        
        // Add datePublished for books
        lines.Add(sprintf "datePublished: \"%s\"" timestamp)
    
    lines.Add(sprintf "rating: %.1f" rating)
    lines.Add("scale: 5.0")
    
    if not (String.IsNullOrWhiteSpace(summary)) then
        lines.Add(sprintf "summary: \"%s\"" (summary.Replace("\"", "\\\"").Replace("\n", " ")))
    
    if pros.Length > 0 then
        lines.Add("pros:")
        for pro in pros do
            lines.Add(sprintf "  - \"%s\"" (pro.Replace("\"", "\\\"")))
    
    if cons.Length > 0 then
        lines.Add("cons:")
        for con in cons do
            lines.Add(sprintf "  - \"%s\"" (con.Replace("\"", "\\\"")))
    
    match itemUrl with
    | Some url -> lines.Add(sprintf "itemUrl: \"%s\"" url)
    | None -> ()
    
    // Add imageUrl for non-book reviews
    if reviewType.ToLower() <> "book" then
        match imageUrl with
        | Some url -> lines.Add(sprintf "imageUrl: \"%s\"" url)
        | None -> ()
    
    // Keep non-book additionalFields for other review types
    if reviewType.ToLower() <> "book" && additionalFields.Length > 0 then
        lines.Add("additionalFields:")
        for (key, value) in additionalFields do
            lines.Add(sprintf "  %s: \"%s\"" key (value.Replace("\"", "\\\"")))
    
    lines.Add(":::")
    String.concat "\n" lines

// Generate the complete file content
let contentSection = 
    if String.IsNullOrWhiteSpace(content) then 
        "" // No content provided - just the review block is fine
    else 
        sprintf "\n\n%s" content

let fileContent = sprintf "%s\n\n# %s Review\n\n%s%s" frontmatter itemName (generateReviewBlock()) contentSection

// Determine output directory and filename based on review type
let subdirectory = 
    match reviewType.ToLower() with
    | "book" -> "library"
    | "movie" -> "movies"
    | "music" -> "music"
    | "business" -> "business"
    | "product" -> "products"
    | _ -> "" // fallback to root reviews directory

let outputDir = 
    if String.IsNullOrWhiteSpace(subdirectory) then
        Path.Join(Directory.GetCurrentDirectory(), "_src", "reviews")
    else
        Path.Join(Directory.GetCurrentDirectory(), "_src", "reviews", subdirectory)

let filename = sprintf "%s.md" slug
let fullPath = Path.Join(outputDir, filename)

// Ensure directory exists
if not (Directory.Exists(outputDir)) then
    Directory.CreateDirectory(outputDir) |> ignore

// Check if file already exists
if File.Exists(fullPath) then
    printfn "❌ Error: File already exists: %s" fullPath
    printfn "Try using a different slug or the file may have been created already."
    exit 1

// Write the file
try
    File.WriteAllText(fullPath, fileContent)
    
    // Success output
    printfn "✅ %s review created successfully!" (reviewType.Substring(0, 1).ToUpper() + reviewType.Substring(1))
    let displayPath = 
        if String.IsNullOrWhiteSpace(subdirectory) then
            Path.Combine("_src", "reviews", filename)
        else
            Path.Combine("_src", "reviews", subdirectory, filename)
    printfn "📁 File: %s" displayPath
    printfn "📖 Item: %s" itemName
    printfn "⭐ Rating: %.1f/5.0" rating
    printfn "🏷️ Tags: %s" (if tags.Length > 0 then String.concat ", " tags else "none")
    printfn "🔗 URL: %s" (match itemUrl with Some url -> url | None -> "none")
    
    if pros.Length > 0 then
        printfn "👍 Pros: %d items" pros.Length
    if cons.Length > 0 then
        printfn "👎 Cons: %d items" cons.Length
    if additionalFields.Length > 0 then
        printfn "📊 Additional fields: %d items" additionalFields.Length

with
| ex -> 
    printfn "❌ Error writing file: %s" ex.Message
    exit 1
