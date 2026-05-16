module Services.QRStyled

// =============================================================================
// Pure-F# styled QR code SVG generator.
//
// Emits an SVG that visually matches the runtime `qr-code-styling` modal
// (see `_src/js/qrcode.js`) — slate-blue rounded data modules, orange
// rounded finder patterns, and an embedded center avatar — without any
// JavaScript, headless browser, or third-party rendering library. The QR
// bit-matrix itself is produced by `Net.Codecrete.QrCodeGenerator`, the same
// library already used by `_src/resources/snippets/qr-code-generator.md` for
// the contact-page QR family.
//
// This exists because the homepage avatar flip-card needs a pre-rendered,
// brand-styled QR (avatar 100% on-brand, no CDN dependency on the homepage
// hot path), and the project ethos is: if we're emitting SVG, F# should be
// the thing emitting it. No Node, no Playwright, no runtime JS lib.
//
// The renderer is intentionally simpler than qr-code-styling internally but
// produces the same visual idiom:
//   - Data modules: rounded rectangles whose corner radii adapt to neighbors,
//     so isolated modules render as full circles, pairs render as pills,
//     and connected runs flow into smoothly merged shapes.
//   - Finder patterns (the 3 big corner squares): outer rounded-square frame
//     with a rounded inner block, both in the accent color.
//   - Center image: embedded as a base64 data URI so the SVG stays a single
//     self-contained file (works inside `<img src>` without external fetches
//     or sandbox issues). Modules under the image are cleared (the QR's
//     High ECC level recovers the lost data).
// =============================================================================

open System
open System.IO
open System.Text
open System.Globalization
open Net.Codecrete.QrCodeGenerator

/// Render styled QR options. All sizes are in the SVG's user-coordinate space,
/// not pixels — the SVG uses `viewBox` so the consumer scales it freely.
type Options = {
    /// Text/URL to encode in the QR.
    Data: string
    /// Total SVG width/height in user coords. 280 matches the runtime modal.
    Size: float
    /// Dot/module color (e.g., "#1a2332").
    DotColor: string
    /// Finder-pattern corner color (e.g., "#ff6b35").
    CornerColor: string
    /// SVG background fill (e.g., "#ffffff"). Use "none" to skip.
    BackgroundColor: string
    /// Optional path to a PNG to embed at center as a data URI.
    CenterImagePath: string option
    /// Fraction of `Size` the center image occupies (e.g., 0.25 = 25%).
    CenterImageRatio: float
    /// Quiet-zone margin in user coords between the image and surrounding
    /// modules. Modules whose centers fall within (imageBox + margin) are
    /// suppressed so the image sits in a clean rectangular hole.
    CenterImageMargin: float
    /// When true, render in module-grid coordinates (viewBox = "0 0 N N"
    /// where N is the QR's module count) and omit width/height attributes
    /// so the consumer scales via CSS. Slashes path data size by ~5x by
    /// turning float coords like "64.324" into integers like "8". Use for
    /// per-page QRs where the SVG is rendered via `<img src>` at any size.
    /// Leave false for the homepage hero where the existing modal sizes
    /// the SVG natively at 280 px.
    IntegerGrid: bool
}

/// Sensible defaults matching the runtime per-page modal.
let defaultOptions data centerImage = {
    Data = data
    Size = 280.0
    DotColor = "#1a2332"
    CornerColor = "#ff6b35"
    BackgroundColor = "#ffffff"
    CenterImagePath = centerImage
    CenterImageRatio = 0.25
    CenterImageMargin = 8.0
    IntegerGrid = false
}

/// Defaults for per-page QRs: integer-grid coords for compact path data,
/// downsized avatar still embedded. Consumer scales via CSS.
let perPageOptions data centerImage = {
    defaultOptions data centerImage with
        IntegerGrid = true
}

// ----- internal helpers -------------------------------------------------------

/// Invariant-culture formatter so we always emit `0.5` not `0,5` on
/// European-locale machines (which would silently break the SVG paths).
let private fmt (f: float) =
    f.ToString("0.###", CultureInfo.InvariantCulture)

/// Build a rounded-rect SVG path whose four corner radii can each be 0 or `r`.
/// When all four are `r` and r = s/2 we get a circle; when only one pair is
/// rounded we get a pill end; when none are rounded we get a square. This is
/// how the data modules visually merge when adjacent.
let private dotPath (x: float) (y: float) (s: float)
                    (rTL: float) (rTR: float) (rBR: float) (rBL: float) =
    let sb = StringBuilder()
    let inline app (s: string) = sb.Append(s) |> ignore
    app (sprintf "M%s,%s" (fmt (x + rTL)) (fmt y))
    app (sprintf "H%s" (fmt (x + s - rTR)))
    if rTR > 0.0 then
        app (sprintf "A%s,%s 0 0 1 %s,%s" (fmt rTR) (fmt rTR) (fmt (x + s)) (fmt (y + rTR)))
    app (sprintf "V%s" (fmt (y + s - rBR)))
    if rBR > 0.0 then
        app (sprintf "A%s,%s 0 0 1 %s,%s" (fmt rBR) (fmt rBR) (fmt (x + s - rBR)) (fmt (y + s)))
    app (sprintf "H%s" (fmt (x + rBL)))
    if rBL > 0.0 then
        app (sprintf "A%s,%s 0 0 1 %s,%s" (fmt rBL) (fmt rBL) (fmt x) (fmt (y + s - rBL)))
    app (sprintf "V%s" (fmt (y + rTL)))
    if rTL > 0.0 then
        app (sprintf "A%s,%s 0 0 1 %s,%s" (fmt rTL) (fmt rTL) (fmt (x + rTL)) (fmt y))
    app "Z"
    sb.ToString()

/// A rounded-square frame: outer rounded rect with an inner rounded rect
/// "punched out" via the even-odd fill rule. Used for the outer ring of each
/// finder pattern (the 7x7 corner squares).
let private framePath (x: float) (y: float) (sOuter: float) (thickness: float)
                       (rOuter: float) (rInner: float) =
    let xi = x + thickness
    let yi = y + thickness
    let sInner = sOuter - 2.0 * thickness
    let outer =
        sprintf "M%s,%s H%s A%s,%s 0 0 1 %s,%s V%s A%s,%s 0 0 1 %s,%s H%s A%s,%s 0 0 1 %s,%s V%s A%s,%s 0 0 1 %s,%s Z"
            (fmt (x + rOuter)) (fmt y)
            (fmt (x + sOuter - rOuter))
            (fmt rOuter) (fmt rOuter) (fmt (x + sOuter)) (fmt (y + rOuter))
            (fmt (y + sOuter - rOuter))
            (fmt rOuter) (fmt rOuter) (fmt (x + sOuter - rOuter)) (fmt (y + sOuter))
            (fmt (x + rOuter))
            (fmt rOuter) (fmt rOuter) (fmt x) (fmt (y + sOuter - rOuter))
            (fmt (y + rOuter))
            (fmt rOuter) (fmt rOuter) (fmt (x + rOuter)) (fmt y)
    let inner =
        sprintf "M%s,%s H%s A%s,%s 0 0 1 %s,%s V%s A%s,%s 0 0 1 %s,%s H%s A%s,%s 0 0 1 %s,%s V%s A%s,%s 0 0 1 %s,%s Z"
            (fmt (xi + rInner)) (fmt yi)
            (fmt (xi + sInner - rInner))
            (fmt rInner) (fmt rInner) (fmt (xi + sInner)) (fmt (yi + rInner))
            (fmt (yi + sInner - rInner))
            (fmt rInner) (fmt rInner) (fmt (xi + sInner - rInner)) (fmt (yi + sInner))
            (fmt (xi + rInner))
            (fmt rInner) (fmt rInner) (fmt xi) (fmt (yi + sInner - rInner))
            (fmt (yi + rInner))
            (fmt rInner) (fmt rInner) (fmt (xi + rInner)) (fmt yi)
    outer + " " + inner

/// A plain rounded square — used for the inner 3x3 dot of each finder pattern.
let private roundedSquarePath (x: float) (y: float) (s: float) (r: float) =
    dotPath x y s r r r r

/// Read an image and inline as a base64 data URI. Returns None if the path
/// is missing or unreadable so the SVG can still render (just without the
/// center avatar).
let private inlineImage (path: string) =
    try
        if not (File.Exists path) then None
        else
            let bytes = File.ReadAllBytes path
            let mime =
                let ext = Path.GetExtension(path).ToLowerInvariant()
                match ext with
                | ".png" -> "image/png"
                | ".jpg" | ".jpeg" -> "image/jpeg"
                | ".gif" -> "image/gif"
                | ".webp" -> "image/webp"
                | ".svg" -> "image/svg+xml"
                | _ -> "application/octet-stream"
            Some (sprintf "data:%s;base64,%s" mime (Convert.ToBase64String bytes))
    with _ -> None

// ----- main renderer ----------------------------------------------------------

/// Render the styled QR to an SVG string.
let render (opts: Options) : string =
    // Build the QR bit-matrix at the highest ECC so we can punch a hole for
    // the center image without losing scannability.
    let qr = QrCode.EncodeText(opts.Data, QrCode.Ecc.High)
    let count = qr.Size                       // number of modules per side (square)
    // In IntegerGrid mode, work in module units (cell = 1.0, viewBox = count).
    // Otherwise work in the caller-specified user-coordinate space (cell may
    // be fractional, viewBox = opts.Size).
    let canvasSize = if opts.IntegerGrid then float count else opts.Size
    let cell = canvasSize / float count

    // Finder pattern footprints: 7x7 modules in three corners.
    let finderTL = (0, 0)
    let finderTR = (count - 7, 0)
    let finderBL = (0, count - 7)
    let isInFinder x y =
        (x < 7 && y < 7) ||
        (x >= count - 7 && y < 7) ||
        (x < 7 && y >= count - 7)

    // Center image hole. Compute it in canvas coords, then derive which module
    // cells to suppress (any cell whose center falls inside the cleared zone).
    let imagePx = canvasSize * opts.CenterImageRatio
    // Margin is specified in user coords (pixels). Scale into canvas units in
    // IntegerGrid mode so a 8 px gap on a 280 px canvas stays proportional
    // when the canvas is renormalized to module units.
    let marginInCanvas =
        if opts.IntegerGrid then opts.CenterImageMargin * (canvasSize / opts.Size)
        else opts.CenterImageMargin
    let half = imagePx / 2.0 + marginInCanvas
    let centerPx = canvasSize / 2.0
    let imageHoleMinX = centerPx - half
    let imageHoleMaxX = centerPx + half
    let imageHoleMinY = centerPx - half
    let imageHoleMaxY = centerPx + half
    let isInImageHole x y =
        let cx = (float x + 0.5) * cell
        let cy = (float y + 0.5) * cell
        cx >= imageHoleMinX && cx <= imageHoleMaxX &&
        cy >= imageHoleMinY && cy <= imageHoleMaxY

    let isDataModule x y =
        x >= 0 && y >= 0 && x < count && y < count &&
        not (isInFinder x y) && not (isInImageHole x y) &&
        qr.GetModule(x, y)

    // ----- Background -----
    let bgEl =
        if opts.BackgroundColor = "none" || String.IsNullOrEmpty opts.BackgroundColor then
            ""
        else
            sprintf "<rect width=\"%s\" height=\"%s\" fill=\"%s\"/>"
                (fmt canvasSize) (fmt canvasSize) opts.BackgroundColor

    // ----- Data modules (rounded, neighbor-aware) -----
    // For each "on" data module, compute corner radii: a corner is rounded
    // iff neither of its two adjacent sides has an "on" data neighbor.
    //
    // IntegerGrid fast path: in module-grid coords each cell is 1 unit. When
    // this SVG is rendered via `<img>` at typical display sizes (~70-280 px),
    // sub-pixel corner radii are invisible — so we emit a plain square with
    // short relative commands (`h1v1h-1z`, ~12 chars vs ~60 for the rounded
    // arc form). For ~1,500 modules that's ~70 KB → ~18 KB of path data.
    let dotPaths = ResizeArray<string>()
    if opts.IntegerGrid then
        for y in 0 .. count - 1 do
            for x in 0 .. count - 1 do
                if isDataModule x y then
                    dotPaths.Add (sprintf "M%d,%dh1v1h-1z" x y)
    else
        let r = cell / 2.0
        for y in 0 .. count - 1 do
            for x in 0 .. count - 1 do
                if isDataModule x y then
                    let nN = isDataModule x (y - 1)
                    let nE = isDataModule (x + 1) y
                    let nS = isDataModule x (y + 1)
                    let nW = isDataModule (x - 1) y
                    let rTL = if not nN && not nW then r else 0.0
                    let rTR = if not nN && not nE then r else 0.0
                    let rBR = if not nS && not nE then r else 0.0
                    let rBL = if not nS && not nW then r else 0.0
                    dotPaths.Add (dotPath (float x * cell) (float y * cell) cell rTL rTR rBR rBL)
    let dotsEl =
        if dotPaths.Count = 0 then ""
        else
            sprintf "<path fill=\"%s\" d=\"%s\"/>"
                opts.DotColor
                (String.concat " " dotPaths)

    // ----- Finder patterns (extra-rounded) -----
    // Each finder is a 7x7 cell area: outer rounded frame (1 cell thick) + a
    // rounded 3x3 inner block, both in the corner color. The runtime modal's
    // "extra-rounded" type rounds the outer corners heavily — we use a radius
    // of 2 cells, which matches its visual signature closely.
    let finderPaths = ResizeArray<string>()
    for (fx, fy) in [ finderTL; finderTR; finderBL ] do
        let x = float fx * cell
        let y = float fy * cell
        let s = 7.0 * cell
        let thickness = cell
        let rOuter = cell * 2.0
        let rInner = cell * 1.0
        finderPaths.Add (framePath x y s thickness rOuter rInner)
        // Inner 3x3 dot, positioned 2 cells in from each side of the 7x7.
        let dx = x + 2.0 * cell
        let dy = y + 2.0 * cell
        let ds = 3.0 * cell
        finderPaths.Add (roundedSquarePath dx dy ds (cell * 0.75))
    let cornersEl =
        sprintf "<path fill=\"%s\" fill-rule=\"evenodd\" d=\"%s\"/>"
            opts.CornerColor
            (String.concat " " finderPaths)

    // ----- Center image -----
    let imageEl =
        match opts.CenterImagePath |> Option.bind inlineImage with
        | None -> ""
        | Some dataUri ->
            let x = centerPx - imagePx / 2.0
            let y = centerPx - imagePx / 2.0
            // In IntegerGrid (per-page) mode, emit only SVG2 `href` — every
            // browser that supports `<img src=...svg>` understands it, and
            // dropping the `xlink:href` duplicate halves the embedded
            // base64 payload (17 KB → 17 KB, not 34 KB). For the homepage
            // hero we keep both attributes for maximum compatibility.
            if opts.IntegerGrid then
                sprintf "<image x=\"%s\" y=\"%s\" width=\"%s\" height=\"%s\" href=\"%s\" preserveAspectRatio=\"xMidYMid meet\"/>"
                    (fmt x) (fmt y) (fmt imagePx) (fmt imagePx) dataUri
            else
                sprintf "<image x=\"%s\" y=\"%s\" width=\"%s\" height=\"%s\" href=\"%s\" xlink:href=\"%s\" preserveAspectRatio=\"xMidYMid meet\"/>"
                    (fmt x) (fmt y) (fmt imagePx) (fmt imagePx) dataUri dataUri

    // ----- Compose -----
    // `xmlns` and `xmlns:xlink` are both required so the file is valid as a
    // standalone SVG document (loaded via `<img src>`); without them browsers
    // refuse to render it.
    //
    // In IntegerGrid mode, omit `width`/`height` so the SVG scales to
    // whatever the consumer's CSS dictates (and the byte savings from
    // dropping them are non-trivial across 1,700+ files).
    let sb = StringBuilder()
    sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>") |> ignore
    if opts.IntegerGrid then
        sb.Append(
            sprintf "<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" viewBox=\"0 0 %s %s\">"
                (fmt canvasSize) (fmt canvasSize)) |> ignore
    else
        sb.Append(
            sprintf "<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" viewBox=\"0 0 %s %s\" width=\"%s\" height=\"%s\">"
                (fmt canvasSize) (fmt canvasSize) (fmt canvasSize) (fmt canvasSize)) |> ignore
    sb.Append(bgEl) |> ignore
    sb.Append(dotsEl) |> ignore
    sb.Append(cornersEl) |> ignore
    sb.Append(imageEl) |> ignore
    sb.Append("</svg>") |> ignore
    sb.ToString()

/// Render and write to disk, creating the directory if needed.
// Short content hash of the most-recently-generated QR SVG. Used by the view
// layer as a cache-buster (`?v=<hash>`) so the PWA service worker (which uses
// a `cacheFirstStaleWhileRevalidate` strategy for SVGs) serves the fresh
// version immediately on first load rather than after the next refresh.
let mutable HomeCacheKey : string = ""

/// Compute a short, URL-safe content hash of arbitrary text. Used to derive
/// a cache-busting token from the generated SVG bytes.
let private shortHash (content: string) =
    use sha = System.Security.Cryptography.SHA1.Create()
    let bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(content))
    // First 8 hex chars is plenty of entropy for cache invalidation.
    (System.Convert.ToHexString(bytes)).Substring(0, 8).ToLowerInvariant()

let renderToFile (opts: Options) (outputPath: string) =
    let dir = Path.GetDirectoryName(outputPath)
    if not (String.IsNullOrEmpty dir) && not (Directory.Exists dir) then
        Directory.CreateDirectory(dir) |> ignore
    let svg = render opts
    File.WriteAllText(outputPath, svg)
    let hash = shortHash svg
    HomeCacheKey <- hash
    hash

// =============================================================================
// Per-page QR support: downsized avatar + per-URL cache-busting keys.
// =============================================================================

/// Cache-bust keys for per-page QR SVGs, keyed by absolute encoded URL.
/// Populated by `Builder.buildPerPageQRs`. The view layer reads this to
/// append `?v=<hash>` to the per-page QR `<img src>`.
let mutable PageCacheKeys : System.Collections.Generic.IDictionary<string,string> =
    System.Collections.Generic.Dictionary<string,string>()
    :> System.Collections.Generic.IDictionary<string,string>

/// Resize `srcPath` (typically the full-res 71 KB site avatar) to a small
/// PNG sized for the per-page QR center slot (~120 px square covers the
/// 70 px display target at 2x density). Writes once to `outputPath` and
/// returns the path so callers can use it as `CenterImagePath` for every
/// per-page render — disk cache absorbs repeated reads.
///
/// Why this exists: the full-res avatar inflates a single QR SVG to ~217 KB
/// (base64 dominates). Multiplied across ~1,735 content pages that's ~376
/// MB of deployed static assets. Downsizing to ~120 px brings each SVG
/// under 20 KB, total ~26 MB.
let ensureSmallAvatar (srcPath: string) (outputPath: string) =
    let outDir = Path.GetDirectoryName(outputPath)
    if not (String.IsNullOrEmpty outDir) && not (Directory.Exists outDir) then
        Directory.CreateDirectory(outDir) |> ignore
    use src = SkiaSharp.SKBitmap.Decode(srcPath)
    if isNull src then
        failwithf "QRStyled.ensureSmallAvatar: failed to decode '%s'" srcPath
    let targetSize = 64
    let info =
        SkiaSharp.SKImageInfo(
            targetSize, targetSize,
            SkiaSharp.SKColorType.Rgba8888,
            SkiaSharp.SKAlphaType.Premul)
    use resized = new SkiaSharp.SKBitmap(info)
    let sampling = SkiaSharp.SKSamplingOptions(SkiaSharp.SKCubicResampler.Mitchell)
    if not (src.ScalePixels(resized, sampling)) then
        failwith "QRStyled.ensureSmallAvatar: SKBitmap.ScalePixels failed"
    use image = SkiaSharp.SKImage.FromBitmap(resized)
    use data = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100)
    use fs = File.Create(outputPath)
    data.SaveTo(fs)
    outputPath

/// Same as `renderToFile` but records the resulting hash into
/// `PageCacheKeys[urlKey]` instead of `HomeCacheKey`. Designed for the
/// per-page QR fan-out in `Builder.buildPerPageQRs`.
let renderPageQR (opts: Options) (outputPath: string) (urlKey: string) =
    let dir = Path.GetDirectoryName(outputPath)
    if not (String.IsNullOrEmpty dir) && not (Directory.Exists dir) then
        Directory.CreateDirectory(dir) |> ignore
    let svg = render opts
    File.WriteAllText(outputPath, svg)
    let hash = shortHash svg
    PageCacheKeys.[urlKey] <- hash
    hash
