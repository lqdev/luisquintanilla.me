module GeneratedConfig

// =============================================================================
// Build-time generation / verification of the site's static config documents,
// derived from the single source of truth in `Constants.fs`.
//
//   - manifest.json      : generated in full from Constants (Pwa/Theme/Avatar).
//   - service-worker.js  : `_src/service-worker.js` is a TEMPLATE with tokens
//                          (__CACHE_VERSION__, __PRECACHE_URLS__) injected from
//                          Constants.Pwa. All caching logic stays hand-written.
//   - api/data/actor.json: federation-critical and separately deployed with the
//                          `api/` Functions app, and byte-for-byte (CRLF, exact
//                          escaping) coupled to that deploy. Rather than mutate
//                          it from the site build, we VERIFY it stays consistent
//                          with Constants and fail loudly on drift — the same
//                          single-source guarantee without the coupling risk.
// =============================================================================

open System.IO
open System.Text.Json
open BuilderCommon

let private jsonOpts = JsonSerializerOptions(WriteIndented = true)

/// Generate `_public/manifest.json` from Constants. The manifest is a pure
/// generated artifact (nothing consumes the `_src` copy), so exact key order /
/// formatting is irrelevant — only the values must be correct.
let generateManifest () =
    let icon size purpose =
        match purpose with
        | Some p -> box {| src = Constants.Avatar.displayPath; sizes = size; ``type`` = "image/png"; purpose = p |}
        | None -> box {| src = Constants.Avatar.displayPath; sizes = size |}

    let shortcut name shortName description url =
        {| name = name
           short_name = shortName
           description = description
           url = url
           icons = [| {| src = Constants.Avatar.displayPath; sizes = "192x192" |} |] |}

    let manifest =
        {| name = Constants.Pwa.name
           short_name = Constants.Pwa.shortName
           description = Constants.Pwa.description
           start_url = "/"
           scope = "/"
           display = "standalone"
           background_color = Constants.Theme.background
           theme_color = Constants.Theme.color
           orientation = "portrait-primary"
           icons = [| icon "512x512" (Some "any maskable") |]
           categories = [| "blog"; "technology"; "education" |]
           lang = "en-US"
           dir = "ltr"
           shortcuts =
            [| shortcut "Blog Posts" "Posts" "Read latest blog posts" "/posts"
               shortcut "Search" "Search" "Search content" "/search"
               shortcut "Subscribe" "RSS" "View RSS feeds" "/feed" |] |}

    let json = JsonSerializer.Serialize(manifest, jsonOpts)
    File.WriteAllText(Path.Join(outputDir, "manifest.json"), json)

/// Generate `_public/service-worker.js` by injecting Constants values into the
/// `_src/service-worker.js` template. The template keeps all caching logic and
/// only exposes `__CACHE_VERSION__` and `__PRECACHE_URLS__` as tokens.
let generateServiceWorker () =
    let templatePath = Path.Join(srcDir, "service-worker.js")
    let template = File.ReadAllText(templatePath)

    let precacheJs =
        Constants.Pwa.precache
        |> List.map (fun u -> sprintf "    '%s'" u)
        |> String.concat ",\n"
        |> fun body -> "[\n" + body + "\n]"

    if not (template.Contains("__CACHE_VERSION__")) || not (template.Contains("__PRECACHE_URLS__")) then
        failwithf
            "service-worker.js template is missing injection tokens (__CACHE_VERSION__ / __PRECACHE_URLS__). Path: %s"
            templatePath

    let generated =
        template
            .Replace("__CACHE_VERSION__", Constants.Pwa.cacheVersion)
            .Replace("__PRECACHE_URLS__", precacheJs)

    File.WriteAllText(Path.Join(outputDir, "service-worker.js"), generated)

/// Generate `_public/robots.txt` from Constants.Crawlers. Classic search bots
/// and AI answer/retrieval bots are allowed (default); AI training crawlers are
/// disallowed. See Constants.Crawlers for the policy rationale.
let generateRobots () =
    let sb = System.Text.StringBuilder()
    let line (s: string) = sb.AppendLine(s) |> ignore

    line "# robots.txt — generated at build time from Constants.Crawlers."
    line "# Policy: allow classic search indexing + AI answer/retrieval (citation)"
    line "#         bots; disallow AI training / dataset-collection crawlers."
    line "# Blocking a training token (e.g. Google-Extended) does NOT affect that"
    line "# vendor's normal Search crawler (e.g. Googlebot)."
    line ""
    line "# Default: everything else may crawl the whole site."
    line "User-agent: *"
    line "Disallow:"
    line ""
    line "# --- Disallowed: AI training / dataset-collection crawlers ---"
    for bot in Constants.Crawlers.blockedAiTrainingBots do
        line (sprintf "User-agent: %s" bot)
        line "Disallow: /"
        line ""

    File.WriteAllText(Path.Join(outputDir, "robots.txt"), sb.ToString())

/// Verify `api/data/actor.json` stays consistent with Constants. This does NOT
/// rewrite the federation document (it is byte-coupled to the `api/` Functions
/// deploy); it fails the build if identity has drifted so the two are updated
/// together deliberately.
let verifyActor () =
    let actorPath = Path.Join("api", "data", "actor.json")
    if not (File.Exists actorPath) then
        failwithf "actor.json not found at %s" actorPath

    use doc = JsonDocument.Parse(File.ReadAllText actorPath)
    let root = doc.RootElement
    let str (name: string) = root.GetProperty(name).GetString()

    let mismatches = System.Collections.Generic.List<string>()
    let check label (expected: string) (actual: string) =
        if actual <> expected then
            mismatches.Add(sprintf "%s: expected \"%s\" but actor.json has \"%s\"" label expected actual)

    check "id" (Constants.Urls.canonical + "/api/activitypub/actor") (str "id")
    check "preferredUsername" Constants.Author.username (str "preferredUsername")
    check "name" Constants.Author.name (str "name")
    check "summary" Constants.Author.bio (str "summary")
    check "url" (Constants.Urls.canonical + "/") (str "url")
    check "inbox" (Constants.Urls.canonical + "/api/activitypub/inbox") (str "inbox")
    check "outbox" (Constants.Urls.canonical + "/api/activitypub/outbox") (str "outbox")
    check "icon.url" Constants.Avatar.displayUrl (root.GetProperty("icon").GetProperty("url").GetString())
    check "image.url" Constants.Avatar.displayUrl (root.GetProperty("image").GetProperty("url").GetString())
    check "publicKeyPem" Constants.ActivityPub.publicKeyPem (root.GetProperty("publicKey").GetProperty("publicKeyPem").GetString())

    let alsoKnownAs = root.GetProperty("alsoKnownAs")
    if alsoKnownAs.GetArrayLength() = 0 || alsoKnownAs.[0].GetString() <> Constants.Author.mastodonProfile then
        mismatches.Add(sprintf "alsoKnownAs: expected first entry \"%s\"" Constants.Author.mastodonProfile)

    let attachmentText =
        root.GetProperty("attachment").EnumerateArray()
        |> Seq.map (fun a -> a.GetProperty("value").GetString())
        |> String.concat "\n"
    if not (attachmentText.Contains(Constants.Author.github)) then
        mismatches.Add(sprintf "attachment: expected a link to \"%s\"" Constants.Author.github)

    if mismatches.Count > 0 then
        let details = mismatches |> Seq.map (sprintf "  - %s") |> String.concat "\n"
        failwithf
            "api/data/actor.json is out of sync with Constants (federation identity). Reconcile both deliberately:\n%s"
            details
