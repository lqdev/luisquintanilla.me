/// AT Protocol POSSE mapping for Response content (bookmarks + reshares).
///
/// This is the PURE classification layer for issue #2574 / ADR-0009's response-POSSE scope. It
/// answers two questions and nothing else:
///   1. What does a response's `targeturl` point at? — an ordinary web URL, or a native
///      app.bsky.feed.post (recognised strictly from a `bsky.app/profile/{actor}/post/{rkey}`
///      permalink or a literal `at://{did}/app.bsky.feed.post/{rkey}` URI).
///   2. For an ATProto-targeted reshare, does the author add their own commentary? — decided by
///      scanning the response body's TOP-LEVEL blocks: any non-blockquote block is authored
///      commentary; a body made only of `>` blockquotes is a bare re-share.
///
/// The record-building + staging that consumes these decisions lives in `AtProtoBuilder.fs`, which
/// compiles AFTER this module and owns all the JSON/staging/hash helpers. Keeping this module free
/// of those helpers is deliberate: it depends only on `ASTParsing` (the canonical Markdown pipeline)
/// so the block-structure decision can never drift from how pages are actually rendered.
module AtProtoResponseMapping

open System
open System.Text
open System.Text.RegularExpressions
open Markdig.Syntax
open Markdig.Syntax.Inlines
open CustomBlocks

/// What a response's `targeturl` points at, for POSSE routing.
type TargetRef =
    /// An ordinary web URL — anything NOT recognised as a native Bluesky post reference.
    | OrdinaryUrl of url: string
    /// A native app.bsky.feed.post, identified by its actor (handle or DID) and record key.
    | AtProtoPost of actor: string * rkey: string

/// The native record a response is POSSE'd as.
type ResponsePostKind =
    /// app.bsky.feed.post carrying an external link card (bookmarks + ordinary-web reshares).
    | LinkPost
    /// app.bsky.feed.repost — a bare re-share of a native post, no authored commentary.
    | Repost
    /// app.bsky.feed.post quoting a native post via embed.record — a reshare WITH commentary.
    | QuotePost

// ---------------------------------------------------------------------------
// Target-URL parsing (strict)
// ---------------------------------------------------------------------------

/// Strict `https://bsky.app/profile/{actor}/post/{rkey}` permalink. `actor` is a handle or a DID
/// (both are a single non-delimiter path segment); `rkey` is the trailing record key. An optional
/// trailing slash is tolerated but query/fragment suffixes are not part of the identity.
let private bskyAppPostRegex =
    Regex(@"^https://bsky\.app/profile/([^/?#]+)/post/([^/?#]+)/?$",
          RegexOptions.Compiled ||| RegexOptions.IgnoreCase)

/// Literal `at://{did}/app.bsky.feed.post/{rkey}` AT-URI. The authority segment keeps its colons
/// (`did:plc:…`) because `[^/?#]+` excludes only path delimiters.
let private atUriPostRegex =
    Regex(@"^at://(did:[^/?#]+)/app\.bsky\.feed\.post/([^/?#]+)$",
          RegexOptions.Compiled ||| RegexOptions.IgnoreCase)

/// Classify a `targeturl` into an ordinary URL or a native ATProto post reference. Whitespace is
/// trimmed first (some frontmatter targeturls carry a trailing space). Anything that does not match
/// one of the two strict native-post shapes is treated as an ordinary web URL — the conservative
/// default, so we never mistake a non-post Bluesky link (profile, feed, RSS) for a quotable post.
let parseTargetRef (targetUrl: string) : TargetRef =
    let url = (if isNull targetUrl then "" else targetUrl).Trim()
    let m1 = bskyAppPostRegex.Match url
    if m1.Success then AtProtoPost(m1.Groups.[1].Value, m1.Groups.[2].Value)
    else
        let m2 = atUriPostRegex.Match url
        if m2.Success then AtProtoPost(m2.Groups.[1].Value, m2.Groups.[2].Value)
        else OrdinaryUrl url

// ---------------------------------------------------------------------------
// Body analysis — authored commentary vs. quoted material
// ---------------------------------------------------------------------------

/// Result of scanning a response body's top-level blocks.
type ResponseBodyAnalysis =
    { /// Raw Markdown of the top-level NON-blockquote blocks (the author's own words), trimmed.
      AuthoredCommentaryMarkdown: string
      /// Raw Markdown of the top-level `>` blockquote blocks (quoted source material), trimmed.
      QuotedExcerptMarkdown: string
      /// True when at least one non-blockquote top-level block carries real (non-blank) content.
      HasAuthoredCommentary: bool }

/// Slice a block's source span back out of the original body. Markdig `SourceSpan.End` is the
/// INCLUSIVE index of the last character, so the length is `End - Start + 1`; bounds are clamped
/// defensively so a malformed span can never throw.
let private sliceSpan (body: string) (span: SourceSpan) : string =
    if span.IsEmpty || body.Length = 0 then ""
    else
        let start = max 0 span.Start
        let endInclusive = min (body.Length - 1) span.End
        if endInclusive < start then "" else body.Substring(start, endInclusive - start + 1)

/// Scan a response body (front matter already stripped) and partition its top-level blocks into
/// authored commentary (anything that is not a `>` blockquote) vs. quoted material. Parsing goes
/// through `ASTParsing.parseMarkdownAst` — the same canonical pipeline that renders pages — so a
/// `>` inside a fenced code block is correctly NOT treated as a blockquote.
let analyzeResponseBody (markdownBody: string) : ResponseBodyAnalysis =
    let body = if isNull markdownBody then "" else markdownBody
    let doc = ASTParsing.parseMarkdownAst body
    let authored = StringBuilder()
    let quoted = StringBuilder()
    for block in doc do
        let slice = (sliceSpan body block.Span).Trim()
        if slice.Length > 0 then
            match block with
            | :? QuoteBlock -> quoted.AppendLine slice |> ignore
            | _ -> authored.AppendLine slice |> ignore
    let authoredText = authored.ToString().Trim()
    let quotedText = quoted.ToString().Trim()
    { AuthoredCommentaryMarkdown = authoredText
      QuotedExcerptMarkdown = quotedText
      HasAuthoredCommentary = authoredText.Length > 0 }

/// Quote-posts currently use `app.bsky.embed.record` only. A response containing a
/// media block or Markdown image would require the unsupported recordWithMedia
/// union, so callers can reject it before staging a misleading text-only quote.
let containsUnsupportedQuoteMedia (markdownBody: string) : bool =
    let body = if isNull markdownBody then "" else markdownBody
    let doc = ASTParsing.parseMarkdownAst body
    let hasMediaBlock =
        Markdig.Syntax.MarkdownObjectExtensions.Descendants<MediaBlock>(doc)
        |> Seq.isEmpty
        |> not
    let hasImage =
        Markdig.Syntax.MarkdownObjectExtensions.Descendants<LinkInline>(doc)
        |> Seq.exists (fun link -> link.IsImage)
    hasMediaBlock || hasImage

// ---------------------------------------------------------------------------
// Classification
// ---------------------------------------------------------------------------

/// Decide the native record kind from a parsed target and a body analysis. Ordinary URLs always
/// become link posts. An ATProto-targeted reshare becomes a quote post when the author added
/// commentary, otherwise a bare repost.
let classify (targetRef: TargetRef) (analysis: ResponseBodyAnalysis) : ResponsePostKind =
    match targetRef with
    | OrdinaryUrl _ -> LinkPost
    | AtProtoPost _ -> if analysis.HasAuthoredCommentary then QuotePost else Repost

/// Convenience: parse the target and (only when it is a native post, where the distinction matters)
/// analyze the body, returning both the target reference and the chosen record kind. Ordinary-URL
/// targets skip body analysis entirely.
let classifyResponse (targetUrl: string) (markdownBody: string) : TargetRef * ResponsePostKind =
    let targetRef = parseTargetRef targetUrl
    match targetRef with
    | OrdinaryUrl _ -> targetRef, LinkPost
    | AtProtoPost _ -> targetRef, classify targetRef (analyzeResponseBody markdownBody)
