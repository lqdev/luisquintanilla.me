// test-scripts/test-garmin-gpx.fsx
//
// Validates the Garmin-compatible GPX output produced by Collections.fs against
// the contract from gist 0d7af14d39a05a8219faa7af86bc5ced (Garmin fenix 6X
// Saved Locations / Waypoints).
//
// Run from the repo root after `dotnet run` has produced _public/:
//   dotnet fsi test-scripts/test-garmin-gpx.fsx

open System
open System.IO
open System.Xml.Linq

let repoRoot =
    let here = __SOURCE_DIRECTORY__
    Path.GetFullPath(Path.Combine(here, ".."))

let gpxPath =
    Path.Combine(repoRoot, "_public", "collections", "travel",
                 "chicago-favorites", "chicago-favorites-garmin.gpx")

if not (File.Exists gpxPath) then
    eprintfn "❌ Garmin GPX not found at %s — run `dotnet run` first." gpxPath
    exit 1

let mutable failures = 0
let assertTrue (label: string) (cond: bool) =
    if cond then printfn "✅ %s" label
    else
        printfn "❌ %s" label
        failures <- failures + 1

let gpxNs = XNamespace.Get "http://www.topografix.com/GPX/1/1"

// 1. Parses as XML
let doc =
    try Some (XDocument.Load gpxPath)
    with ex ->
        printfn "❌ Output XML did not parse: %s" ex.Message
        None

match doc with
| None -> exit 1
| Some doc ->
    let root = doc.Root
    assertTrue "Root is <gpx> in GPX 1.1 namespace" (root.Name = (gpxNs + "gpx"))
    assertTrue "version=\"1.1\"" (root.Attribute(XName.Get "version").Value = "1.1")
    assertTrue "creator attribute is non-empty"
        (let a = root.Attribute(XName.Get "creator") in a <> null && not (String.IsNullOrWhiteSpace a.Value))

    let waypoints = root.Elements(gpxNs + "wpt") |> Seq.toList
    assertTrue "Chicago favorites produces 18 waypoints (per spec DoD)"
        (waypoints.Length = 18)

    // 2. Forbidden output elements absent
    let forbidden = ["metadata"; "desc"; "cmt"; "sym"; "type"; "extensions";
                     "rte"; "rtept"; "trk"; "trkseg"; "trkpt"]
    for name in forbidden do
        let found = doc.Descendants() |> Seq.exists (fun e -> e.Name.LocalName = name)
        assertTrue (sprintf "Forbidden element <%s> absent" name) (not found)

    // 3. Each <wpt> has exactly one <name> and no other children
    let mutable allWptShapeOk = true
    for wpt in waypoints do
        let children = wpt.Elements() |> Seq.toList
        if children.Length <> 1 || children.[0].Name <> (gpxNs + "name") then
            allWptShapeOk <- false
    assertTrue "Every <wpt> has exactly one <name> child" allWptShapeOk

    // 4. Lat/lon valid
    let mutable allCoordsValid = true
    for wpt in waypoints do
        let lat = float (wpt.Attribute(XName.Get "lat").Value)
        let lon = float (wpt.Attribute(XName.Get "lon").Value)
        if lat < -90.0 || lat > 90.0 || lon < -180.0 || lon > 180.0 then
            allCoordsValid <- false
    assertTrue "All waypoint coordinates within valid ranges" allCoordsValid

    // 5. Order matches data file (just check the first and known-special waypoint)
    let firstName = waypoints.[0].Element(gpxNs + "name").Value
    assertTrue "First waypoint is Chicago Pizza and Oven Grinder Company"
        (firstName = "Chicago Pizza and Oven Grinder Company")

    // 6. Ampersand correctly escaped — Longman & Eagle is in the source data
    let raw = File.ReadAllText gpxPath
    assertTrue "Raw \"& \" (unescaped ampersand) absent from output"
        (not (raw.Contains("Longman & Eagle")))
    assertTrue "Properly escaped &amp; present"
        (raw.Contains("Longman &amp; Eagle"))

    // 7. XML declaration encoding matches actual file encoding (UTF-8)
    assertTrue "XML declaration encoding=\"utf-8\""
        (raw.IndexOf("encoding=\"utf-8\"", StringComparison.OrdinalIgnoreCase) > 0)

    // 8. Original rich GPX still exists (we did not regress it)
    let richPath = Path.Combine(repoRoot, "_public", "collections", "travel",
                                 "chicago-favorites", "chicago-favorites.gpx")
    assertTrue "Original rich GPX still present alongside Garmin variant"
        (File.Exists richPath)

if failures = 0 then
    printfn ""
    printfn "🎉 All Garmin GPX assertions passed."
    exit 0
else
    printfn ""
    printfn "❌ %d assertion(s) failed." failures
    exit 1
