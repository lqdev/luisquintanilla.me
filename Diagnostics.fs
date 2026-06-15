module Diagnostics

// =============================================================================
// Structured content-build diagnostics (F8 — Phase 2.8; doctrine §8.3).
//
// `ASTParsing.fs` already returns `Result<ParsedDocument<'T>, ParseError>` with a
// typed error carrying real exception detail. That rail was SEVERED at 12
// `| Error _ -> None` sites in `GenericBuilder.fs`: the error value was built and
// then discarded, so a malformed file silently vanished from the site. This
// module is the reconnection — the failure track now has somewhere to go.
//
// Policy (decided in the plan): report loudly, keep building. One bad file must
// not block publishing the rest. `_public/` stays byte-identical when nothing
// fails (a failing file is dropped either way; only stdout differs). Opt-in
// strict mode (`--strict` / STRICT_CONTENT=1) turns any content error into a
// non-zero exit, for CI gating.
//
// Compiled after ASTParsing.fs (needs ParseError) and before GenericBuilder.fs
// (the consumer). No content-type dependency — valid sets are passed in by the
// caller, so this module stays a leaf.
// =============================================================================

open ASTParsing

/// A content-build failure carrying everything a human or agent needs to fix it
/// without re-deriving context: where (file), what (got), the valid set, and a
/// pointer to the authority to edit. Sketch from assessment §8.3.
[<RequireQualifiedAccess>]
type ContentError =
    | ParseFailure        of file: string * stage: string * detail: string
    | UnknownContentType  of file: string * got: string * valid: string list
    | UnknownResponseType of file: string * got: string * valid: string list
    | MissingField        of file: string * field: string * hint: string

/// Lift the parser's typed `ParseError` (already in hand at the severed sites)
/// into a `ContentError`, preserving the stage and the real exception detail.
let ofParseError (file: string) (err: ParseError) : ContentError =
    match err with
    | YamlParseError msg           -> ContentError.ParseFailure(file, "yaml-frontmatter", msg)
    | MarkdownParseError msg       -> ContentError.ParseFailure(file, "markdown", msg)
    | FileNotFound msg             -> ContentError.ParseFailure(file, "file", msg)
    | InvalidMarkdownStructure msg -> ContentError.ParseFailure(file, "structure", msg)
    | MissingRequiredField(f, ctx) -> ContentError.MissingField(file, f, ctx)

/// Render one error as a self-contained, copy-pasteable block: a status line, the
/// detail/expected, and a `fix:` pointer to the file:line that owns the rule.
let render (e: ContentError) : string =
    match e with
    | ContentError.ParseFailure(file, stage, detail) ->
        sprintf "\u2717 ParseFailure [%s]: %s\n    %s\n    fix: correct the %s in the file (rail: ASTParsing.fs:15)"
            stage file detail stage
    | ContentError.UnknownContentType(file, got, valid) ->
        sprintf "\u2717 UnknownContentType: %s\n    got '%s' \u2014 valid: %s\n    fix: correct the content type, or extend ContentType (ContentTypes.fs)"
            file got (String.concat " | " valid)
    | ContentError.UnknownResponseType(file, got, valid) ->
        sprintf "\u2717 UnknownResponseType: %s\n    got '%s' \u2014 valid: %s\n    fix: correct 'response_type' in the file, or extend ResponseType (Domain.fs:393)"
            file got (String.concat " | " valid)
    | ContentError.MissingField(file, field, hint) ->
        sprintf "\u2717 MissingField: %s\n    missing '%s' \u2014 %s\n    fix: add '%s' to the front-matter"
            file field hint field

// --- Build-wide accumulation (validation-style: collect all, report once) ---

let private sink = System.Collections.Generic.List<ContentError>()

/// Record an error and print its block immediately so it is visible inline with
/// the build it belongs to.
let report (e: ContentError) : unit =
    sink.Add e
    printfn "%s" (render e)

/// Total content errors accumulated across the whole build (for strict-mode exit).
let errorCount () = sink.Count

/// Strict mode requested via `--strict` argument or `STRICT_CONTENT=1` env var.
let isStrict (argv: string array) =
    (argv |> Array.contains "--strict")
    || (System.Environment.GetEnvironmentVariable("STRICT_CONTENT") = "1")

// =============================================================================
// RENDER_V2 flag (Phase 3 / B2 — structured render product, assessment F7).
//
// The timeline historically composed each card by re-parsing already-rendered
// HTML (`cleanCardHtml` regex-strips the redundant <article>/<h2><a> chrome that
// the standalone CardHtml baked in). B2 carries a chrome-free `BodyHtml` on each
// unified item so the timeline composes it structurally instead. The flag gates
// the new path so the old one stays wired until cutover: flag OFF reproduces the
// regex path byte-for-byte; flag ON is the reviewed (intentionally non-identical)
// structured path. Cutover (B2.cutover) flips the default and deletes the old path.
//
// View code has no access to argv, so the `--render-v2` answer is latched here at
// startup; `RENDER_V2=1` works without latching (handy for one-off generations).
// =============================================================================

let mutable private renderV2Latched = false

/// Latch the `--render-v2` argv answer at startup so argv-less view code can read it.
let useRenderV2From (argv: string array) =
    renderV2Latched <- argv |> Array.contains "--render-v2"

/// True when the structured render product (B2) is enabled.
let useRenderV2 () =
    renderV2Latched
    || (System.Environment.GetEnvironmentVariable("RENDER_V2") = "1")
