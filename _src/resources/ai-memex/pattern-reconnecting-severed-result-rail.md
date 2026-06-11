---
title: "Reconnecting a Severed Result Rail at the Consolidated Consumer"
description: "When a railway-oriented bottom layer returns Result but consumers discard it with `| Error _ -> None`, reconnect once at the single consolidated consumer — and for batch/build tools, accumulate-and-report rather than short-circuit."
entry_type: pattern
published_date: "2026-06-11 15:05 -05:00"
last_updated_date: "2026-06-11 15:05 -05:00"
tags: "fsharp, dotnet, architecture, patterns, error-handling, devops"
related_skill: write-ai-memex
source_project: lqdev-me
related_entries: pattern-closed-du-identity-vs-wire-boundary, pattern-false-unification
---

## Discovery

The site generator's parsing layer (`ASTParsing.fs`) was built correctly railway-style: every
parse function returned `Result<ParsedDocument<'T>, ParseError>` with a typed error carrying real
exception detail. But every one of the **12** content processors immediately collapsed that
`Result` to an `Option` with `| Error _ -> None`. The error value — file, stage, exception message —
was *constructed and then thrown away*. Net effect: a single malformed front-matter date didn't
fail the build; the post just **silently vanished from the site**, and the author might not notice
for days. For a slow-web, daily-publishing site, silent loss is worse than a loud failure.

## Root Cause

This is not missing error *handling* — it's missing error *transport*. The failure track existed
at the bottom of the stack; it was simply never connected to a reporter. The consumers chose
`List.choose`/`Option` convenience over carrying the error one layer up.

A second, subtler cause: there was no *single* place to reconnect. Before the build-driver
consolidation (a prior refactor step), the parse-consume logic was duplicated across ~11 hand-written
`buildX` functions, so reconnecting would have meant 11 edits. **Order matters:** consolidate the
consumer first, then reconnect the rail once.

## Solution

**1. Give the error somewhere to go.** A leaf `Diagnostics.fs` module (no heavy dependencies, compiled
early) with a structured, self-contained error type — each case carries *where* (file), *what* (got),
*the valid set*, and *the authority to edit* (file:line):

```fsharp
[<RequireQualifiedAccess>]
type ContentError =
    | ParseFailure        of file: string * stage: string * detail: string
    | UnknownContentType  of file: string * got: string * valid: string list
    | UnknownResponseType of file: string * got: string * valid: string list
    | MissingField        of file: string * field: string * hint: string

let ofParseError (file: string) (err: ParseError) : ContentError = (* lift the error already in hand *)
let render (e: ContentError) : string = (* one self-contained, copy-pasteable block *)
```

**2. Stop discarding the error.** Change the shared contract from `Parse : string -> 'T option` to
`Parse : string -> Result<'T, ContentError>`. The 12 `| Error _ -> None` become
`| Error e -> Error (Diagnostics.ofParseError filePath e)`; the companion `| None -> None`
(missing front-matter) becomes a `ParseFailure` too. The compiler finds every site for you.

**3. Reconnect once, at the consolidated consumer.** Because all parsing now flows through one
function, the partition lives in exactly one place:

```fsharp
let parsed = filePaths |> List.map processor.Parse
parsed |> List.choose (function Error e -> Some e | Ok _ -> None) |> List.iter Diagnostics.report
parsed |> List.choose (function Ok c -> Some (render c) | Error _ -> None)   // same success set as before
```

**4. Accumulate, don't short-circuit.** A build over ~1,200 files wants *validation-style*
accumulation (parse everything, collect every failure, report once) — **not** monadic `result { }`
bind, which aborts on the first error. The success track proceeds to render; the failure track
feeds the report.

**5. Policy: report loudly, keep building.** Default exit 0 — one bad file must not block publishing
the other 1,199. An opt-in strict mode (`--strict` arg / `STRICT_CONTENT=1` env) gates the exit
code non-zero for CI when wanted. For a daily-publishing site, a blocked deploy is a second disease,
not a cure for the first.

## Prevention

- **Build the rail *and* connect it.** A bottom layer returning `Result` is worthless if every
  consumer does `| Error _ -> None`. From now on, `| Error _ -> ...` that *discards* the error
  value is a review-blocker — the error was constructed for a reason; transport it.
- **Consolidate before you reconnect.** Reconnecting a rail across N duplicated consumers is N
  edits and N chances to miss one. Collapse to one consumer first; then the fix is a single,
  obvious diff.
- **Batch tools accumulate; request handlers short-circuit.** Pick the ROP flavour by context:
  first-error `bind` for a single request, error-collecting `partition` for a batch/build.
- **Errors as values with fix pointers.** The block an agent or human reads should contain
  *everything* needed to act — file, got, valid set, and the file:line that owns the rule — so no
  search loop is spent reconstructing what the build already knew.
- **Verify it's inert when clean.** This kind of change must be byte-identical when nothing fails
  (a failed parse is dropped either way; only stdout differs). Prove it with a hash-manifest diff,
  and prove the rail *works* with a temporary broken fixture (block + exit 0 default, exit 1 strict).
