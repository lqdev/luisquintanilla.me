(*
    Process GitHub Issue Template for Marketplace ("For Sale") Listing

    Mirrors process-media-issue.fsx. The content should already contain
    :::media::: blocks with permanent CDN URLs from the Python upload script
    (via the shared upload-to-cdn composite action).

    Usage:
      dotnet fsi process-marketplace-issue.fsx -- \
        "title" "description" "price" "currency" "price_note" "condition" \
        "status" "category" "location" "content_with_media_blocks" "optional-slug" "optional,tags"

    Unlike the media script, the generated filename is always "{slug}.md" with NO
    date appended — marketplace URLs must stay descriptive and status/price/date-free
    (Cool-URI discipline: a sold item keeps its permalink forever).
*)

#r "../bin/Debug/net10.0/PersonalSite.dll"

open System
open System.IO
open System.Globalization
open System.Text.RegularExpressions

// Get command line arguments
let args = fsi.CommandLineArgs |> Array.skip 1

// Helper: read an optional positional arg as Some/None
let argOpt i =
    if args.Length > i && not (String.IsNullOrWhiteSpace(args.[i])) then Some(args.[i].Trim())
    else None

// Validate arguments (title, description, price, content are required => indices 0,1,2,9)
if args.Length < 10 then
    printfn "❌ Error: Missing required arguments"
    printfn "Usage: dotnet fsi process-marketplace-issue.fsx -- \"title\" \"description\" \"price\" \"currency\" \"price_note\" \"condition\" \"status\" \"category\" \"location\" \"content_with_media_blocks\" \"optional-slug\" \"optional,tags\""
    exit 1

let title = args.[0]
let description = args.[1]
let priceInput = args.[2]
let currencyInput = argOpt 3
let priceNoteInput = argOpt 4
let conditionInput = argOpt 5
let statusInput = argOpt 6
let categoryInput = argOpt 7
let locationInput = argOpt 8
let contentWithMediaBlocks = args.[9]
let customSlug = argOpt 10
let tagsInput = argOpt 11

// Validate required fields
if String.IsNullOrWhiteSpace(title) then
    printfn "❌ Error: Title is required and cannot be empty"
    exit 1

if String.IsNullOrWhiteSpace(description) then
    printfn "❌ Error: Description is required and cannot be empty"
    exit 1

if String.IsNullOrWhiteSpace(contentWithMediaBlocks) then
    printfn "❌ Error: Content is required and cannot be empty"
    exit 1

// Parse and validate price: strip currency symbols / thousands separators, keep digits + dot
let price =
    let cleaned = Regex.Replace(priceInput, @"[^0-9.]", "")
    match Decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture) with
    | true, value when value >= 0m -> value
    | _ ->
        printfn "❌ Error: Price '%s' is not a valid non-negative number" priceInput
        exit 1

// Normalize currency (default USD)
let currency =
    match currencyInput with
    | Some c -> c.ToUpperInvariant()
    | None -> "USD"

// Normalize status (default available); validate enum
let validStatuses = [ "available"; "pending"; "sold" ]
let status =
    match statusInput with
    | Some s ->
        let v = s.ToLowerInvariant()
        if List.contains v validStatuses then v
        else
            printfn "⚠️  Warning: status '%s' not recognized; defaulting to 'available'" s
            "available"
    | None -> "available"

// Normalize condition (optional); validate enum if present
let validConditions = [ "new"; "like-new"; "good"; "fair"; "for-parts" ]
let condition =
    match conditionInput with
    | Some c ->
        let v = c.ToLowerInvariant()
        if List.contains v validConditions then Some v
        else
            printfn "⚠️  Warning: condition '%s' not recognized; omitting" c
            None
    | None -> None

let priceNote = priceNoteInput |> Option.map (fun s -> s.ToLowerInvariant())
let category = categoryInput
let location = locationInput

// Slug generation and sanitization functions (descriptive, status/price-free)
let sanitizeSlug (slug: string) =
    slug.ToLowerInvariant()
        .Replace(" ", "-")
        .Replace("_", "-")
    |> fun s -> Regex.Replace(s, @"[^a-z0-9\-]", "")
    |> fun s -> Regex.Replace(s, @"-+", "-")
    |> fun s -> s.Trim('-')

let generateSlugFromTitle (title: string) =
    let slug = sanitizeSlug title
    if String.IsNullOrWhiteSpace(slug) then "untitled-listing"
    elif slug.Length > 60 then slug.Substring(0, 60).TrimEnd('-')
    else slug

// Determine final slug (custom slug wins if valid, else generated from title)
let finalSlug =
    match customSlug with
    | Some slug ->
        let sanitized = sanitizeSlug slug
        if String.IsNullOrWhiteSpace(sanitized) then generateSlugFromTitle title
        else sanitized
    | None -> generateSlugFromTitle title

// Process tags
let tags =
    match tagsInput with
    | Some input ->
        input.Split(',')
        |> Array.map (fun tag -> tag.Trim().ToLowerInvariant())
        |> Array.filter (fun tag -> not (String.IsNullOrWhiteSpace(tag)))
        |> Array.distinct
    | None -> [||]

// Generate timestamp in EST (-05:00)
let now = DateTimeOffset.Now.ToOffset(TimeSpan.FromHours(-5.0))
let timestamp = now.ToString("yyyy-MM-dd HH:mm zzz")

// YAML double-quoted scalar escaping
let yamlEscape (s: string) = s.Replace("\\", "\\\\").Replace("\"", "\\\"")

// Build frontmatter, omitting optional fields when absent so YamlDotNet defaults apply
let sb = System.Text.StringBuilder()
sb.AppendLine("---") |> ignore
sb.AppendLine(sprintf "title: \"%s\"" (yamlEscape title)) |> ignore
sb.AppendLine(sprintf "description: \"%s\"" (yamlEscape description)) |> ignore
sb.AppendLine(sprintf "published_date: \"%s\"" timestamp) |> ignore
sb.AppendLine(sprintf "price: %s" (price.ToString(CultureInfo.InvariantCulture))) |> ignore
sb.AppendLine(sprintf "currency: \"%s\"" (yamlEscape currency)) |> ignore
priceNote |> Option.iter (fun pn -> sb.AppendLine(sprintf "price_note: \"%s\"" (yamlEscape pn)) |> ignore)
sb.AppendLine(sprintf "status: \"%s\"" status) |> ignore
condition |> Option.iter (fun c -> sb.AppendLine(sprintf "condition: \"%s\"" c) |> ignore)
category |> Option.iter (fun c -> sb.AppendLine(sprintf "category: \"%s\"" (yamlEscape c)) |> ignore)
location |> Option.iter (fun l -> sb.AppendLine(sprintf "location: \"%s\"" (yamlEscape l)) |> ignore)
if tags.Length > 0 then
    let tagsString = tags |> Array.map (fun t -> sprintf "\"%s\"" (yamlEscape t)) |> String.concat ", "
    sb.AppendLine(sprintf "tags: [%s]" tagsString) |> ignore
sb.Append("---") |> ignore
let frontmatter = sb.ToString()

// Filename: always "{slug}.md" — NO date (URL permanence for sold items)
let filename = sprintf "%s.md" finalSlug

// Combine frontmatter and content (content already has :::media blocks from Python script)
let fullContent = sprintf "%s\n\n%s" frontmatter contentWithMediaBlocks

// Ensure _src/marketplace directory exists
let marketplaceDir = Path.Combine("_src", "marketplace")
if not (Directory.Exists(marketplaceDir)) then
    Directory.CreateDirectory(marketplaceDir) |> ignore

// Write file
let filePath = Path.Combine(marketplaceDir, filename)
File.WriteAllText(filePath, fullContent)

// Output success information
printfn "✅ Marketplace listing created successfully!"
printfn "📁 File: %s" filePath
printfn "🏷️  Title: %s" title
printfn "💵 Price: %s %s%s" currency (price.ToString(CultureInfo.InvariantCulture)) (match priceNote with Some pn -> sprintf " (%s)" pn | None -> "")
printfn "📦 Status: %s" status
printfn "🔧 Condition: %s" (match condition with Some c -> c | None -> "(unspecified)")
printfn "🔗 Slug: %s" finalSlug
printfn "📅 Date: %s" timestamp
printfn "🏷️  Tags: %s" (if tags.Length = 0 then "none" else String.concat ", " tags)
printfn ""
printfn "📄 Generated markdown file content:"
printfn "==========================================="
printfn "%s" fullContent
printfn "==========================================="
printfn ""
printfn "💾 File has been persisted to: %s" (Path.GetFullPath(filePath))
printfn "📊 File size: %d bytes" (FileInfo(filePath).Length)
