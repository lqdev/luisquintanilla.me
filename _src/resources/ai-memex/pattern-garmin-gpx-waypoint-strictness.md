---
title: "Garmin GPX Waypoint Strictness — Dual-Output Pattern"
description: "Garmin fēnix watches require strict GPX 1.1 namespaced, waypoint-only files. Pattern for shipping a parallel Garmin variant alongside a richer GPX without regressing existing consumers."
entry_type: pattern
published_date: "2026-05-10 08:30 -05:00"
last_updated_date: "2026-05-10 08:30 -05:00"
tags: gpx, garmin, fenix, indieweb, travel-guides, fsharp, xmlwriter, static-site
related_skill: write-ai-memex
source_project: lqdev-me
---

## Context

Travel guides on lqdev.me generate GPX files so readers can take recommendations
offline in apps like OsmAnd or Maps.me. The original generator emitted a "rich"
GPX with `<metadata>`, `<wpt>` containing `<desc>` and `<type>`, and a bare
root `<gpx version="1.1" creator="...">` (no XML namespace).

This worked fine for phone apps. **It did not work for a Garmin fēnix 6X.**

## The gotcha

The fēnix 6X waypoint importer (Saved Locations) is far stricter than phone
apps. It rejects or silently drops files that:

1. **Lack the GPX 1.1 namespace** on the root element. The official namespace
   `xmlns="http://www.topografix.com/GPX/1/1"` must be present.
2. Contain elements outside the minimal waypoint shape. The fēnix happiest path
   is literally `<gpx>` → `<wpt lat lon>` → `<name>`. Anything else
   (`<desc>`, `<sym>`, `<type>`, `<extensions>`, `<metadata>`, routes, tracks)
   risks rejection.
3. Have raw, unescaped ampersands in names (e.g., `Longman & Eagle`) — which
   are an XML well-formedness violation regardless of device.

## The pattern

**Don't refactor the existing rich GPX. Ship a second, minimal variant beside
it.** Different consumers need different fidelity; a static site can produce
both for free.

For each travel collection:

```
/collections/travel/<id>/
├── <id>.gpx           ← rich (OsmAnd/Maps.me)
└── <id>-garmin.gpx    ← minimal, namespaced (Garmin Saved Locations)
```

Surface both in the UI with distinct buttons. Make the broad-app one primary,
the Garmin one secondary, and add literal copy/paste guidance for the watch:
*"Copy to `GARMIN/NewFiles/`, then START → Navigate → Saved Locations."*

## Implementation notes (F# / .NET)

- **Generate via `XmlWriter`, not string concatenation.** XmlWriter handles all
  XML escaping automatically (`&` → `&amp;`, etc.), so you get the spec's
  "ampersand repair" requirement for free, with zero parsing.
- **Watch the encoding.** `XmlWriter.Create(StringBuilder, …)` will emit
  `<?xml version="1.0" encoding="utf-16"?>` because StringBuilder is UTF-16
  internally — a mismatch with the UTF-8 file `File.WriteAllText` then writes.
  Write to a `MemoryStream` with `XmlWriterSettings(Encoding = UTF8Encoding(false))`
  and decode the bytes back to a string.
- **Validate lat/lon at generation time** (`-90..90`, `-180..180`) and skip
  invalid points with a build-time warning rather than emitting bad waypoints.
- **Fallback for empty names.** Spec convention is `Waypoint NNN` (1-based,
  zero-padded). The fēnix needs *something* in `<name>`; an empty element will
  drop the waypoint silently.
- **Use `CultureInfo.InvariantCulture`** when formatting coordinates to "F6" so
  locales with comma decimal separators don't break the file.

## Validation

A reparse-and-assert test script (`dotnet fsi`) covers the spec's Definition of
Done:

- Root namespace = `http://www.topografix.com/GPX/1/1`
- Forbidden elements absent (`metadata`, `desc`, `cmt`, `sym`, `type`,
  `extensions`, `rte`, `rtept`, `trk`, `trkseg`, `trkpt`)
- Every `<wpt>` has exactly one `<name>` child and no others
- Lat/lon in valid ranges, input order preserved
- `&` round-trips to `&amp;`
- Declared encoding matches actual file encoding
- Original rich GPX still present (no regression)

## Why ship both instead of replacing the rich GPX

OsmAnd and Maps.me users *benefit* from the descriptions and category metadata
in the rich variant. Stripping the file down to satisfy Garmin would degrade
the experience for the majority of users. Generation cost is negligible on a
static build, so dual output is the right trade.

## When this generalizes

Any time a "lowest-common-denominator" device sits in the consumer set for a
generated artifact (GPX, RSS, ICS, OPML, etc.), ask whether you should:

1. Constrain the rich format to the strict subset, or
2. Ship a second, minimal artifact alongside it.

Option 2 is almost always right when the rich consumers outnumber the strict
ones and generation is cheap. The cost is one extra button and one extra file
per artifact.

## References

- Spec gist: <https://gist.github.com/lqdev/0d7af14d39a05a8219faa7af86bc5ced>
- Files in this repo:
  - `Collections.fs` — `generateCollectionGarminGpx`
  - `Views/TravelViews.fs` — dual download buttons
  - `test-scripts/test-garmin-gpx.fsx` — reparse + assertions
- Related: [[pattern-content-volume-html-parsing]] (similar "different consumers,
  different output" reasoning for HTML)
