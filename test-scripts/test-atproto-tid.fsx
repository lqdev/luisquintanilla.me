// Validates AtProtoBuilder's deterministic TID derivation (Part B Phase 1, issue #2574).
// Run: dotnet fsi test-scripts/test-atproto-tid.fsx
// Constants.fs is self-contained (no repo deps), so the #load chain is just Constants + AtProtoBuilder.

#load "../Constants.fs"
#load "../AtProtoBuilder.fs"

open System
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

printfn "AtProtoBuilder TID derivation tests"
printfn "-----------------------------------"

let date2026 = DateTimeOffset(2026, 1, 31, 22, 14, 0, TimeSpan.FromHours -5.0)
let slugA = "fosdem-2026-social-web-thoughts"
let tidA = deriveTid date2026 slugA

// 1. Determinism — same (date, slug) always yields the same TID.
check "determinism: same input -> same TID" (deriveTid date2026 slugA = tidA)

// 2. Length is always exactly 13 chars.
check "length is 13" (tidA.Length = 13)

// 3. Derived TID is syntactically valid per the spec regex.
check "isValidTid on derived TID" (isValidTid tidA)

// 4. encodeTid 0 is the canonical all-'2' TID.
check "encodeTid 0UL = 2222222222222" (encodeTid 0UL = "2222222222222")

// 5. Current-era (2026) TIDs start with '3'.
check "2026 TID first char is '3'" (tidA.[0] = '3')

// 6. Collision-resistance: different slug, same minute -> different TID.
let tidB = deriveTid date2026 "some-other-post-same-minute"
check "different slug (same minute) -> different TID" (tidA <> tidB)

// 7. Time monotonicity: same slug, later date -> lexicographically greater TID
//    (base32-sortable => string order == chronological order).
let dateLater = date2026.AddDays 1.0
check "later date sorts after earlier (same slug)" (deriveTid dateLater slugA > tidA)

// 8. isValidTid rejects malformed input.
check "isValidTid rejects too-short" (not (isValidTid "abc"))
check "isValidTid rejects bad first char" (not (isValidTid "z234567abcdef"))
check "isValidTid rejects null" (not (isValidTid null))

// 9. Real-world scale: 2000 distinct slugs sharing the exact same minute all derive distinct TIDs.
let bulkSlugs = [ for i in 1 .. 2000 -> sprintf "note-%04d" i ]
let bulkTids = bulkSlugs |> List.map (fun s -> deriveTid date2026 s)
check "2000 same-minute slugs -> all valid TIDs" (bulkTids |> List.forall isValidTid)
check "2000 same-minute slugs -> all distinct TIDs"
    (List.length (List.distinct bulkTids) = 2000)

// 10. assertNoTidCollisions passes on a distinct set...
let distinctItems = bulkSlugs |> List.map (fun s -> date2026, s)
let noCollisionOk =
    try assertNoTidCollisions distinctItems; true
    with _ -> false
check "assertNoTidCollisions passes on distinct set" noCollisionOk

// ...and fails loudly on an actual duplicate (identical date+slug).
let dupItems = [ (date2026, slugA); (date2026, slugA) ]
let collisionCaught =
    try assertNoTidCollisions dupItems; false
    with ex -> ex.Message.Contains "colliding rkeys"
check "assertNoTidCollisions throws on true duplicate" collisionCaught

// 11. generateHash is deterministic and hex-formatted (32 chars for MD5).
let h1 = generateHash "https://lqdev.me/posts/x/|content"
check "generateHash deterministic" (h1 = generateHash "https://lqdev.me/posts/x/|content")
check "generateHash is 32-char hex" (h1.Length = 32 && h1 |> Seq.forall (fun c -> "0123456789abcdef".Contains c))

// 12. Config wires to Constants single source of truth.
check "Config.canonicalUrl = Constants.Urls.canonical" (Config.canonicalUrl = Constants.Urls.canonical)
check "Config.publicationName = Constants.Site.title" (Config.publicationName = Constants.Site.title)
check "Config.publicationAtUri is a site.standard.publication AT-URI"
    (Config.publicationAtUri.StartsWith "at://did:plc:" && Config.publicationAtUri.Contains "/site.standard.publication/")

printfn "-----------------------------------"
printfn "Passed: %d   Failed: %d" passed failed
if failed > 0 then
    eprintfn "TID derivation tests FAILED"
    exit 1
else
    printfn "All TID derivation tests passed."
