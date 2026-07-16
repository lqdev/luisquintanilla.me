module Constants

// =============================================================================
// Single source of truth for site-wide identity, URLs, theme and PWA config.
//
// This replaces the earlier `Avatars.fs` (its values now live under
// `Constants.Avatar`) and generalizes the "define it in one place" idea to the
// rest of the app's identity strings. F# consumers reference these values so
// there is ONE place to change site identity.
//
// The three static config documents are now DERIVED from these values at build
// time (see Builders/GeneratedConfig.fs):
//   - _public/manifest.json      (generated fully from Constants.Pwa/Site/Theme)
//   - _public/service-worker.js  (template `_src/service-worker.js` + tokens)
//   - api/data/actor.json        (verified consistent with Constants — see note)
// =============================================================================

module Urls =
    /// Canonical origin (feeds, canonical links, OG, ActivityPub, QR payloads).
    /// NOTE: `lqdev.me` (apex) is canonical. `www.lqdev.me` is the *legacy*
    /// form still present in many feed/processor literals (migrated in a later
    /// phase) and must keep redirecting so old links/feeds don't 404.
    let canonical = "https://lqdev.me"

    /// Legacy www origin — do not introduce new references; kept for the
    /// pending migration of existing feed/processor literals and documentation.
    let legacyWww = "https://www.lqdev.me"

    /// Webmention receiving endpoint (external service).
    let webmentionInbox = "https://webmentions.lqdev.tech/api/inbox"

module Author =
    let name = "Luis Quintanilla"
    let username = "lqdev"
    let shortName = "Luis Q"
    /// Bio / ActivityPub actor summary.
    let bio =
        "AI whisperer wandering the shifting sands of the desert of the real . "
        + "Semi-fluent in the language of machines, with an affinity for the F# dialect."
    /// Fediverse handle (used in `fediverse:creator`).
    let fediverseHandle = "@lqdev@toot.lqdev.tech"
    /// Mastodon profile (ActivityPub `alsoKnownAs`).
    let mastodonProfile = "https://toot.lqdev.tech/users/lqdev"
    let github = "https://github.com/lqdev"
    let twitter = "https://twitter.com/ljquintanilla"
    let linkedin = "https://www.linkedin.com/in/lquintanilla01/"
    let email = "mailto:lqdev@outlook.com"

    /// Job/role title used in schema.org `Person.jobTitle`.
    let jobTitle = "Software Engineer"

    /// Canonical external profiles for schema.org `Person.sameAs` (identity
    /// disambiguation for crawlers/LLMs). Mirrors the footer `rel="me"` links.
    let sameAs =
        [ github
          mastodonProfile
          twitter
          linkedin
          email ]

module Site =
    /// `og:site_name` / general site title.
    let title = "Luis Quintanilla Personal Website"

module Theme =
    /// Brand/theme color (PWA `theme_color`, `<meta name="theme-color">`).
    let color = "#2d4a5c"
    /// App background color (PWA `background_color`).
    let background = "#1a1a1a"

module Avatar =
    // Folded in from the former `Avatars.fs`.

    /// Bare filename of the canonical, full-color source avatar. Kept and
    /// served; never deleted. It is the input the retro avatar is generated
    /// from (see Services/RetroAvatar.fs).
    let sourceFileName = "avatar.png"

    /// Bare filename of the build-generated Doom-style avatar the site displays.
    let displayFileName = "avatar-doom.png"

    /// Site-root-relative URL for the displayed avatar (use in `img _src`).
    let displayPath = "/" + displayFileName

    /// Absolute (canonical) URL for the displayed avatar (og:image, actor icon).
    let displayUrl = Urls.canonical + "/" + displayFileName

module ActivityPub =
    /// Public signing key for the fediverse actor. A PUBLIC key is safe to keep
    /// in source. Kept here so the actor document stays consistent with a
    /// single source of truth (verified at build time).
    let publicKeyPem =
        "-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAv3tqkIX9FECRyvdTmpa3\nCGAJrmup3X9w0cWJPNbK92sE89EV7FsBoQLPKs4ap1+ObBvmU2udtrk4Uk2PE2Bw\n8PyxMtIPFUq+qmUB1Bemett3gBXtLK8ravDsH4nB8wDk3O9GD6SozPPZzJSuV7MP\n8L2zwBkBScRsiqjalkOfmMikO473Ts2IaDDcJv5gWv+04pA8E+RbSIl+SryforUU\nU3yzGS2afRt+lCDePMHNkF2FBqPpjKxmtlk6Ivo6GBKT9ye5UP1bvheXN03kez2j\ngK0Nm9juHsER23gfReQdMC7n25GnnL2GCzP3uiiBKFMPGe5lZUHObPjP5y9+LYma\nxQIDAQAB\n-----END PUBLIC KEY-----"

module Pwa =
    /// PWA install name (manifest `name`).
    let name = "Luis Quintanilla - Tech Blog"
    /// PWA short name (manifest `short_name`).
    let shortName = "Luis Q"
    /// PWA description (manifest `description`).
    let description =
        "Personal website and blog by Luis Quintanilla - Software Engineer, "
        + "ML Enthusiast, and Tech Content Creator"

    /// Service-worker cache version. Bump deliberately when the precache set or
    /// cached asset contents change so clients evict stale entries.
    let cacheVersion = "v1.0.3"

    /// Files precached by the service worker on install (`STATIC_CACHE_URLS`)
    /// and the source of truth for the injected list.
    let precache =
        [ "/"
          "/about"
          "/contact"
          "/search"
          "/feed"
          "/offline.html"
          "/assets/css/main.css"
          "/assets/js/main.js"
          "/assets/js/timeline.js"
          "/assets/js/clipboard.js"
          "/assets/js/share.js"
          "/assets/js/lazy-images.js"
          Avatar.displayPath
          "/manifest.json" ]

module Crawlers =
    // Crawler policy (single source of truth for the generated robots.txt).
    //
    // Goal: keep the site fully indexable by classic search engines and
    // citable by AI *answer/retrieval* bots, while opting OUT of AI *training*
    // / dataset-collection crawlers. Blocking the "-Extended"/training tokens
    // does NOT affect the vendors' normal search indexing (e.g. blocking
    // `Google-Extended` does not affect Googlebot). robots.txt is honored by
    // reputable/documented bots only — it is a policy signal, not enforcement.

    /// AI training / dataset-collection user agents to `Disallow: /`.
    let blockedAiTrainingBots =
        [ "GPTBot"              // OpenAI model training
          "Google-Extended"    // Google Gemini/Vertex model training (not Search)
          "anthropic-ai"       // Anthropic (legacy token)
          "ClaudeBot"          // Anthropic crawler
          "Claude-Web"         // Anthropic (legacy token)
          "CCBot"              // Common Crawl (feeds many training corpora)
          "Bytespider"         // ByteDance/TikTok training
          "Applebot-Extended"  // Apple AI training opt-out (not Applebot Search)
          "Meta-ExternalAgent" // Meta AI training
          "meta-externalagent"
          "FacebookBot"        // Meta training
          "Amazonbot"          // Amazon (commonly training/corpus)
          "Google-CloudVertexBot"  // Vertex AI enterprise fetch
          "Diffbot"            // Knowledge-graph/dataset scraper
          "Omgilibot"          // Dataset resale
          "ImagesiftBot"       // The Hive dataset crawler
          "PanguBot"
          "Timpibot"
          "cohere-ai"
          "cohere-training-data-crawler"
          "Kangaroo Bot"
          "PetalBot"           // Huawei/Petal training
          "YouBot"             // You.com crawler (training)
          "Scrapy" ]

    /// AI answer / live-retrieval bots we intentionally ALLOW (so the site can
    /// be cited in AI answers). Documented here for intent; not emitted as
    /// rules since the default is allow.
    let allowedAiAnswerBots =
        [ "OAI-SearchBot"; "ChatGPT-User"; "PerplexityBot"; "Perplexity-User" ]
