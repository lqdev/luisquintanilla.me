// Validates the response-POSSE mapping + record builders (issue #2574 / ADR-0009 amendment):
//   * AtProtoResponseMapping.parseTargetRef  — strict native-post recognition vs ordinary URLs
//   * AtProtoResponseMapping.analyzeResponseBody / classify — quote-vs-repost decision
//   * AtProtoBuilder response record builders — bookmark/reshare link posts, quote posts, reposts
//   * rkey seed namespacing + repost collection routing
// Run: dotnet fsi test-scripts/test-atproto-response-mapping.fsx
// The #load chain mirrors the .fsproj compile order up to AtProtoBuilder (which now depends on
// AtProtoResponseMapping -> ASTParsing).

#r "nuget: YamlDotNet, 16.3.0"
#r "nuget: Giraffe.ViewEngine, 1.4.0"
#r "nuget: Markdig, 0.38.0"
#load "../Domain.fs"
#load "../Constants.fs"
#load "../StructuredData.fs"
#load "../ReviewSchema.fs"
#load "../ContentTypes.fs"
#load "../CustomBlocks.fs"
#load "../MediaTypes.fs"
#load "../ASTParsing.fs"
#load "../Services/Tag.fs"
#load "../AtProtoResponseMapping.fs"
#load "../AtProtoBuilder.fs"

open System
open Domain
open AtProtoResponseMapping
open AtProtoBuilder

let mutable passed = 0
let mutable failed = 0
let check name cond =
    if cond then passed <- passed + 1; printfn "  PASS  %s" name
    else failed <- failed + 1; printfn "  FAIL  %s" name

printfn "AtProto response-mapping + record-builder tests"
printfn "-----------------------------------------------"

// --- parseTargetRef ----------------------------------------------------------
check "bsky.app handle permalink -> AtProtoPost"
    (match parseTargetRef "https://bsky.app/profile/bsky.app/post/3kh5rjl6bgu2i" with
     | AtProtoPost("bsky.app", "3kh5rjl6bgu2i") -> true | _ -> false)

check "bsky.app did permalink -> AtProtoPost"
    (match parseTargetRef "https://bsky.app/profile/did:plc:abc123/post/3xyz" with
     | AtProtoPost("did:plc:abc123", "3xyz") -> true | _ -> false)

check "http bsky.app permalink stays an ordinary URL"
    (match parseTargetRef "http://bsky.app/profile/bsky.app/post/3xyz" with
     | OrdinaryUrl _ -> true | _ -> false)

check "literal at:// post URI -> AtProtoPost"
    (match parseTargetRef "at://did:plc:pme7qquljcdx6i4zyawoxypd/app.bsky.feed.post/3abc" with
     | AtProtoPost("did:plc:pme7qquljcdx6i4zyawoxypd", "3abc") -> true | _ -> false)

check "literal at:// URI with a non-DID authority stays ordinary"
    (match parseTargetRef "at://not-a-did/app.bsky.feed.post/3abc" with
     | OrdinaryUrl _ -> true | _ -> false)

check "trailing whitespace tolerated"
    (match parseTargetRef "https://bsky.app/profile/x.y/post/3abc  " with
     | AtProtoPost("x.y", "3abc") -> true | _ -> false)

check "ordinary web URL -> OrdinaryUrl"
    (match parseTargetRef "https://en.wikipedia.org/wiki/1%25_rule" with
     | OrdinaryUrl _ -> true | _ -> false)

// A non-post Bluesky link (profile RSS) is NOT a quotable post — must be ordinary.
check "bsky.app profile RSS (non-post) -> OrdinaryUrl"
    (match parseTargetRef "https://bsky.app/profile/did:plc:pme7qquljcdx6i4zyawoxypd/rss" with
     | OrdinaryUrl _ -> true | _ -> false)

check "at:// non-post collection -> OrdinaryUrl"
    (match parseTargetRef "at://did:plc:abc/app.bsky.feed.like/3abc" with
     | OrdinaryUrl _ -> true | _ -> false)

// --- analyzeResponseBody / classify -----------------------------------------
let quoteOnly = "> just quoting the source, nothing of my own"
let withCommentary = "Here are my own thoughts.\n\n> and a quote from them"

check "blockquote-only body -> no authored commentary"
    (not (analyzeResponseBody quoteOnly).HasAuthoredCommentary)

check "paragraph + quote -> has authored commentary"
    ((analyzeResponseBody withCommentary).HasAuthoredCommentary)

check "classify: ATProto target + commentary -> QuotePost"
    (classify (AtProtoPost("a","b")) (analyzeResponseBody withCommentary) = QuotePost)

check "classify: ATProto target + no commentary -> Repost"
    (classify (AtProtoPost("a","b")) (analyzeResponseBody quoteOnly) = Repost)

check "classify: ordinary target -> LinkPost (no body needed)"
    (classify (OrdinaryUrl "https://x") (analyzeResponseBody quoteOnly) = LinkPost)

// A `>` inside a fenced code block must NOT read as a blockquote (authored commentary).
let codeFenceQuote = "```\n> not a real blockquote\n```"
check "fenced '>' is authored commentary, not a quote"
    ((analyzeResponseBody codeFenceQuote).HasAuthoredCommentary)

check "Markdown image is detected as local media"
    (containsLocalMedia "Commentary\n\n![Alt text](https://example.com/image.png)")

check "code-fenced image is not detected as local media"
    (not (containsLocalMedia "```\n![not an image](https://example.com/image.png)\n```"))

// --- record builders ---------------------------------------------------------
let mkResponse fileName title target rtype body : Response =
    { FileName = fileName
      Metadata =
        { Title = title
          TargetUrl = target
          ResponseType = rtype
          RsvpStatus = None
          DatePublished = "2026-09-01 12:00 -05:00"
          DateUpdated = "2026-09-01 12:00 -05:00"
          Tags = [| "bluesky" |]
          ReadingTimeMinutes = None }
      Content = body
      MarkdownSource = Some (sprintf "---\ntitle: \"%s\"\n---\n\n%s" title body) }

let published = DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.FromHours -5.0)

// Bookmark link post
let bm = mkResponse "one-percent-rule" "1% Rule" "https://en.wikipedia.org/wiki/1%25_rule" "bookmark" "> about 1% of Internet users create content"
let bmRec = buildBookmarkPostRecordJson bm published "one-percent-rule" "https://en.wikipedia.org/wiki/1%25_rule"
let bmText = bmRec.["text"].GetValue<string>()
check "bookmark: $type is app.bsky.feed.post" (bmRec.["$type"].GetValue<string>() = "app.bsky.feed.post")
check "bookmark: text starts with 'Bookmarked:'" (bmText.StartsWith("Bookmarked: 1% Rule"))
check "bookmark: canonical lqdev URL in text" (bmText.Contains "https://lqdev.me/bookmarks/one-percent-rule/")
check "bookmark: external card points at target URL"
    (bmRec.["embed"].["external"].["uri"].GetValue<string>() = "https://en.wikipedia.org/wiki/1%25_rule")
check "bookmark: has a link facet" (bmRec.["facets"].AsArray().Count = 1)
check "bookmark: normalized tags are included"
    (bmRec.["tags"].AsArray().Count = 1 && bmRec.["tags"].AsArray().[0].GetValue<string>() = "bluesky")
check "bookmark: carries a sourceHash" (not (isNull bmRec.["sourceHash"]))

// Ordinary-web reshare link post (commentary present)
let rs = mkResponse "dotnet-tensors" "Tensors in .NET" "https://www.youtube.com/watch?v=VOEeNffChSg" "reshare" "Great session on Tensors in .NET"
let rsRec = buildResharePostRecordJson rs published "dotnet-tensors" "https://www.youtube.com/watch?v=VOEeNffChSg"
let rsText = rsRec.["text"].GetValue<string>()
check "reshare: text starts with 'Shared:'" (rsText.StartsWith("Shared: Tensors in .NET"))
check "reshare: authored commentary used in text" (rsText.Contains "Great session on Tensors")
check "reshare: canonical response URL in text" (rsText.Contains "https://lqdev.me/responses/dotnet-tensors/")
check "reshare: external card points at target" (rsRec.["embed"].["external"].["uri"].GetValue<string>() = "https://www.youtube.com/watch?v=VOEeNffChSg")

// Quote post (ATProto target, commentary)
let qp = mkResponse "bsky-rss" "Bluesky now supports RSS" "https://bsky.app/profile/bsky.app/post/3kh5rjl6bgu2i" "reshare" "Feel free to subscribe to my feed.\n\n> RSS feeds for profiles!"
let qpTarget = parseTargetRef qp.Metadata.TargetUrl
let qpRec = buildQuotePostRecordJson qp published "bsky-rss" qpTarget
let qpText = qpRec.["text"].GetValue<string>()
check "quote: $type app.bsky.feed.post" (qpRec.["$type"].GetValue<string>() = "app.bsky.feed.post")
check "quote: commentary present in text" (qpText.Contains "Feel free to subscribe")
check "quote: quoted source NOT duplicated in text" (not (qpText.Contains "RSS feeds for profiles"))
check "quote: canonical response URL in text" (qpText.Contains "https://lqdev.me/responses/bsky-rss/")
check "quote: embed is app.bsky.embed.record" (qpRec.["embed"].["$type"].GetValue<string>() = "app.bsky.embed.record")
check "quote: embed.record has placeholder uri" (qpRec.["embed"].["record"].["uri"].GetValue<string>() = "")
check "quote: normalized tags are included"
    (qpRec.["tags"].AsArray().Count = 1 && qpRec.["tags"].AsArray().[0].GetValue<string>() = "bluesky")
check "quote: carries a sourceHash" (not (isNull qpRec.["sourceHash"]))
let qpWithAtUriTarget =
    { qp with Metadata = { qp.Metadata with TargetUrl = "at://did:plc:abc123/app.bsky.feed.post/3kh5rjl6bgu2i" } }
let qpAtUriRec =
    buildQuotePostRecordJson qpWithAtUriTarget published "bsky-rss" (parseTargetRef qpWithAtUriTarget.Metadata.TargetUrl)
check "quote: sourceHash includes the original target URL"
    (qpRec.["sourceHash"].GetValue<string>() <> qpAtUriRec.["sourceHash"].GetValue<string>())

// Repost (ATProto target, no commentary)
let rpRec = buildRepostRecordJson published
check "repost: $type app.bsky.feed.repost" (rpRec.["$type"].GetValue<string>() = "app.bsky.feed.repost")
check "repost: has subject placeholder" (rpRec.["subject"].["uri"].GetValue<string>() = "")
check "repost: NO text field" (isNull rpRec.["text"])
check "repost: NO sourceHash (guarded by subject instead)" (isNull rpRec.["sourceHash"])
check "repost collection routing" (responseCollection Repost = "app.bsky.feed.repost")
check "link/quote collection routing" (responseCollection LinkPost = "app.bsky.feed.post" && responseCollection QuotePost = "app.bsky.feed.post")

// rkey seeds are namespaced (disjoint from a bare deriveTid on the same slug + from each other)
let bare = deriveTid published "bsky-rss"
check "responseRkey seeds differ from bare deriveTid"
    (responseRkey "quote" published "bsky-rss" <> bare && responseRkey "repost" published "bsky-rss" <> bare)
check "responseRkey seeds differ across kinds"
    (responseRkey "quote" published "bsky-rss" <> responseRkey "repost" published "bsky-rss"
     && responseRkey "bookmark" published "x" <> responseRkey "reshare-link" published "x")
check "responseRkey is a valid TID" (isValidTid (responseRkey "bookmark" published "one-percent-rule"))

printfn "-----------------------------------------------"
printfn "Passed: %d   Failed: %d" passed failed
if failed > 0 then exit 1
