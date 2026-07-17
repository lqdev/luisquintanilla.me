// Wire-contract test for AtProtoBuilder.buildDocumentRecordJson (issue #2639, Part B).
// Run: dotnet fsi test-scripts/test-atproto-document-json.fsx
//
// Purpose: lock the on-the-wire shape of the site.standard.document record that the (hand-built)
// JsonObject serializer emits — BEFORE any live PDS write. Standard.site verification fetches
// {publication.url}{path} and looks for a matching <link>, and the sync script upserts exactly this
// JSON, so the contract asserted here (discriminator, required fields, optional-omission, the `path`
// shape, tag normalization, and the `sourceHash` write-scope formula) must not silently drift.
//
// This is the resolution of #2639 (Option B): the record types stay documentation-only domain models
// and this test — not JsonSerializer round-tripping — guards the wire format. #load chain mirrors the
// .fsproj compile order: Domain -> Constants -> ContentTypes -> Services/Tag -> AtProtoBuilder.

#r "nuget: YamlDotNet, 16.3.0"
#r "nuget: Giraffe.ViewEngine, 1.4.0"
#load "../Domain.fs"
#load "../Constants.fs"
#load "../ContentTypes.fs"
#load "../Services/Tag.fs"
#load "../AtProtoBuilder.fs"

open System
open System.Text.Json
open System.Text.Json.Nodes
open System.Text.RegularExpressions
open AtProtoBuilder

let mutable passed = 0
let mutable failed = 0

let check name cond =
    if cond then
        passed <- passed + 1
        printfn "  PASS  %s" name
    else
        failed <- failed + 1
        printfn "  FAIL  %s" name

// --- fixture + JsonObject inspection helpers ------------------------------------------------

let makePost (title: string) (desc: string) (content: string) (tags: string array) : Domain.Post =
    { FileName = "test-post"
      Metadata =
        { PostType = "article"
          Title = title
          Description = desc
          Date = "2026-01-31T22:14:00-05:00"
          Tags = tags
          ReadingTimeMinutes = None }
      Content = content
      MarkdownSource = None }

let published = DateTimeOffset(2026, 1, 31, 22, 14, 0, TimeSpan.FromHours -5.0)
let slug = "fosdem-2026-social-web-thoughts"

let has (o: JsonObject) (k: string) = o.ContainsKey k
let str (o: JsonObject) (k: string) = o.[k].GetValue<string>()
let tagList (o: JsonObject) =
    match o.[ "tags" ] with
    | :? JsonArray as a -> a |> Seq.map (fun n -> n.GetValue<string>()) |> List.ofSeq
    | _ -> []
// Any property whose value node is a JSON null. The whole point of "omit, don't null" — an absent
// optional must be a MISSING KEY, never "key": null (Standard.site/atproto reject null-for-absent).
let hasAnyJsonNull (o: JsonObject) =
    o |> Seq.exists (fun kv -> isNull kv.Value || kv.Value.GetValueKind() = JsonValueKind.Null)

let iso8601 = Regex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}[+-]\d{2}:\d{2}$")

printfn "AtProtoBuilder site.standard.document wire-contract tests"
printfn "--------------------------------------------------------"

// === Case 1: fully-populated post (description, content, messy tags) ========================
let full =
    buildDocumentRecordJson
        (makePost "FOSDEM 2026 Social Web Thoughts"
                  "  Reflections on the social web at FOSDEM.  "
                  "# Heading\n\nSome **bold** body text with a [link](https://example.com)."
                  [| ".net"; ".net core"; "C#"; "machine learning"; "machinelearning"; "untagged"; "" |])
        published slug

check "case1: $type == site.standard.document"
    (has full "$type" && str full "$type" = "site.standard.document")
check "case1: site == publication AT-URI (no trailing slash)"
    (has full "site" && str full "site" = Config.publicationAtUri && not ((str full "site").EndsWith "/"))
check "case1: title present and preserved"
    (has full "title" && str full "title" = "FOSDEM 2026 Social Web Thoughts")
check "case1: path == /posts/{slug}/ (leading + trailing slash)"
    (has full "path" && str full "path" = sprintf "/posts/%s/" slug)
check "case1: path matches postPath (verification-critical contract)"
    (str full "path" = postPath slug)
check "case1: description present and trimmed"
    (has full "description" && str full "description" = "Reflections on the social web at FOSDEM.")
check "case1: textContent present (non-empty plaintext)"
    (has full "textContent" && (str full "textContent").Length > 0)
check "case1: textContent is stripped to plaintext (no markdown markers)"
    (let t = str full "textContent" in not (t.Contains "#") && not (t.Contains "**") && not (t.Contains "]("))
check "case1: publishedAt present and ISO-8601 with offset"
    (has full "publishedAt" && iso8601.IsMatch(str full "publishedAt"))
check "case1: sourceHash present, 32-char lowercase hex"
    (has full "sourceHash"
     && (str full "sourceHash").Length = 32
     && (str full "sourceHash") |> Seq.forall (fun c -> "0123456789abcdef".Contains c))
check "case1: sourceHash equals the documented write-scope formula"
    (str full "sourceHash"
        = generateHash (Config.canonicalUrl + postPath slug + "\u0000"
                        + "# Heading\n\nSome **bold** body text with a [link](https://example.com)."))
check "case1: tags is a JSON array" (match full.[ "tags" ] with :? JsonArray -> true | _ -> false)
check "case1: tags normalized (.net/.net core -> dotnet, C# -> csharp)"
    (let t = tagList full in List.contains "dotnet" t && List.contains "csharp" t)
check "case1: tags carry no raw variants (.net / C# / spaced)"
    (let t = tagList full in
     not (List.contains ".net" t) && not (List.contains "C#" t) && not (List.contains "machine learning" t))
check "case1: tags de-duplicated (no repeats after normalization)"
    (let t = tagList full in List.length t = List.length (List.distinct t))
check "case1: 'untagged' sentinel and blanks dropped from tags"
    (let t = tagList full in not (List.contains "untagged" t) && not (List.contains "" t))
check "case1: no property is JSON null (omit-don't-null invariant)" (not (hasAnyJsonNull full))

// === Case 2: minimal post — blank description, blank content, empty tags =====================
let minimal =
    buildDocumentRecordJson (makePost "Bare Post" "   " "   " [||]) published slug

check "case2: required fields always present"
    ([ "$type"; "site"; "title"; "path"; "publishedAt"; "sourceHash" ] |> List.forall (has minimal))
check "case2: description OMITTED (key absent, not null) when blank" (not (has minimal "description"))
check "case2: textContent OMITTED when content is blank" (not (has minimal "textContent"))
check "case2: tags OMITTED when frontmatter tags are empty" (not (has minimal "tags"))
check "case2: no property is JSON null" (not (hasAnyJsonNull minimal))

// === Case 3: tags that normalize to ONLY the 'untagged' sentinel => tags omitted ============
let onlyUntagged =
    buildDocumentRecordJson (makePost "Untagged Only" "d" "body" [| "untagged"; " " |]) published slug
check "case3: tags OMITTED when everything collapses to the untagged sentinel"
    (not (has onlyUntagged "tags"))

// === Case 4: determinism — identical inputs produce byte-identical JSON ======================
let a = (buildDocumentRecordJson (makePost "T" "d" "c" [| "fsharp" |]) published slug).ToJsonString()
let b = (buildDocumentRecordJson (makePost "T" "d" "c" [| "fsharp" |]) published slug).ToJsonString()
check "case4: buildDocumentRecordJson is deterministic (identical input -> identical JSON)" (a = b)

printfn "--------------------------------------------------------"
printfn "Passed: %d   Failed: %d" passed failed
if failed > 0 then
    eprintfn "site.standard.document wire-contract tests FAILED"
    exit 1
else
    printfn "All site.standard.document wire-contract tests passed."
