// Regression guard for issue #2497.
// RSS <description> must contain a REAL CDATA section (not an XML-escaped literal)
// and must escape any embedded "]]>" so feeds stay well-formed.
//
// Run from the project ROOT, AFTER `dotnet build` (Debug):
//   dotnet fsi test-scripts\test-rss-cdata.fsx
// Exits 0 on success, 1 on any failure (CI-gateable).

#r "../bin/Debug/net10.0/PersonalSite.dll"

open System.Xml.Linq
open GenericBuilder

let mutable failures = 0
let check name cond =
    if cond then printfn "  PASS  %s" name
    else failures <- failures + 1; printfn "  FAIL  %s" name

printfn "=== Issue #2497 regression guard: GenericBuilder.rssDescriptionElement ==="

// 1. HTML content -> a real CDATA node, not escaped text.
let html = "<p>Hello & <b>world</b></p>"
let el = rssDescriptionElement html
check "content is emitted as an XCData node"        (el.FirstNode :? XCData)
check "serialized form has no escaped CDATA marker" (not ((el.ToString()).Contains "&lt;![CDATA["))
check "HTML round-trips verbatim through CDATA"     ((XElement.Parse (el.ToString())).Value = html)

// 2. Embedded "]]>" must remain well-formed and preserve the payload.
let evil = "danger ]]> here"
let evilXml = (rssDescriptionElement evil).ToString()
let reparsed = try Some (XElement.Parse evilXml) with _ -> None
check "embedded ]]> serializes to well-formed XML"  reparsed.IsSome
check "embedded ]]> preserves content"  (match reparsed with Some x -> x.Value = evil | None -> false)

// 3. null content is safe.
check "null content does not throw" (try (rssDescriptionElement null).ToString() |> ignore; true with _ -> false)

printfn ""
if failures = 0 then printfn "ALL CHECKS PASSED"; exit 0
else printfn "%d CHECK(S) FAILED" failures; exit 1
