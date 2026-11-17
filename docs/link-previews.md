# Link Previews

The site provides lightweight previews for first-party content links and a visual
indicator for external Markdown links. Previews are available on pointer hover
and keyboard focus; they are not triggered by touch.

## What is previewed

- Internal content links in `.post-content` and `.presentation-content` can show
  the target page's title, content type, publication date, and short summary.
- External Markdown links with visible text receive an external-link indicator.
- Site assets, fragment links, mailto links, downloads, navigation, and
  third-party destinations do not receive internal previews.

Authored descriptions are preferred when the source content type provides them.
Media pages use the first image's description or alt text. Other pages fall back
to the generated page's `.p-summary` or readable content text.

## Build-time data

After unified content has been generated, the build scans each content page's
`.e-content` once. It writes the link graph to `obj/links/registry.json` and
prints broken internal links as warnings without failing the build. Redirect
routes from `staticwebapp.config.json` are resolved before checking whether an
internal destination exists.

Preview records are written to 64 JSON buckets under
`_public/assets/previews/b00.json` through `b63.json`. The normalized path key
is lowercased, origin-free, and stripped of query strings, fragments,
`index.html`, and trailing slashes. FNV-1a over the UTF-8 path key selects the
bucket. Each file has this shape:

```json
{
  "version": 1,
  "bucketCount": 64,
  "entries": {
    "/posts/example": {
      "title": "Example",
      "type": "Post",
      "date": "2026-01-01",
      "summary": "A short summary.",
      "url": "https://www.luisquintanilla.me/posts/example/"
    }
  }
}
```

Internal redirect aliases are added as additional entries; they do not create
additional files. Keeping records in fixed buckets avoids a per-destination file
and request while allowing links that share a bucket to reuse its payload.

## Browser behavior

The client waits 300 ms before fetching a bucket on hover. Keyboard focus loads
the preview immediately. Each bucket is fetched at most once per page view and
is not precached, so pages without preview interactions incur no preview JSON
request. Tooltip content is inserted as text, the original link title and ARIA
description state are restored when the tooltip closes, Escape dismisses it,
and positioning is clamped to the viewport.

The external indicator is applied by the Markdown extension to text-bearing
external links. Raw HTML anchors are still part of the build-time link graph but
are not decorated by that Markdown AST extension.

## Validation

From the repository root:

```powershell
dotnet build -c Release
dotnet run
dotnet fsi test-scripts/test-link-preview.fsx
node --check _src/js/link-preview.js
```

`Scripts/check-site-sizes.sh` also reports the generated file count and warns at
14,000 files, before the Azure Static Web Apps 15,000-file limit.
