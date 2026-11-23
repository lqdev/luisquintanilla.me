# Azure Functions API

This directory contains Azure Functions for:
1. **ActivityPub Federation**: Endpoints for Fediverse integration (webfinger, actor, inbox, outbox, followers, following)
2. **File Redirects**: Wildcard redirect handling for blob storage files

## ActivityPub Federation

**📋 Documentation Home**: [`/docs/activitypub/`](../docs/activitypub/)

See the dedicated [ActivityPub README](ACTIVITYPUB.md) for complete documentation on:
- Endpoint structure and usage
- WebFinger discovery
- Actor profiles and collections
- Follow/Accept workflow
- HTTP signature verification
- Azure Key Vault integration
- Testing and troubleshooting
- Future enhancements (Phase 3-4)

For implementation status and roadmap, see [`/docs/activitypub/implementation-status.md`](../docs/activitypub/implementation-status.md).

Quick test:
```bash
./Scripts/test-activitypub.sh
```

---

## File Redirects

## Installation

Use Node.js 22 for local development and the managed Static Web Apps API
(`platform.apiRuntime: node:22`). The Azure SDK dependency tree requires Node 22;
`.npmrc` enforces engine compatibility. The Functions host and extension bundle
remain on v4.

1. Install the locked Node.js dependencies:
```bash
cd api
npm ci
```

2. Install Azure Functions Core Tools (if not already installed):
```bash
npm install -g azure-functions-core-tools@4 --unsafe-perm true
```

## Local Development

Start the Functions runtime locally:
```bash
cd api
func start
```

The API will be available at `http://localhost:7071/api/files/{filePath}`

## Testing

Run the offline SDK compatibility tests on Node 22:

```bash
cd api
npm ci
npm audit
npm test
```

These tests use in-memory transports and generated test keys, not Azure credentials
or live federation endpoints. They check request serialization, signing, table and
queue operations. They do not verify an Azure deployment; the maintenance scripts
and other integration tests may contact live services and must not be run as smoke tests.

Test the redirect functionality:

### Basic file redirect:
```bash
curl -I "http://localhost:7071/api/files/document.pdf"
```

Expected response:
```
HTTP/1.1 301 Moved Permanently
Location: https://luisquintanillame.blob.core.windows.net/files/document.pdf
Cache-Control: public, max-age=3600
X-Robots-Tag: noindex
```

### Nested path redirect:
```bash
curl -I "http://localhost:7071/api/files/reports/2024/summary.pdf"
```

Expected response:
```
HTTP/1.1 301 Moved Permanently
Location: https://luisquintanillame.blob.core.windows.net/files/reports/2024/summary.pdf
Cache-Control: public, max-age=3600
X-Robots-Tag: noindex
```

### With query parameters:
```bash
curl -I "http://localhost:7071/api/files/document.pdf?download=true"
```

Expected response:
```
HTTP/1.1 301 Moved Permanently
Location: https://luisquintanillame.blob.core.windows.net/files/document.pdf?download=true
Cache-Control: public, max-age=3600
X-Robots-Tag: noindex
```

## Security Features

- Path validation prevents directory traversal attacks
- Query parameter preservation maintains functionality
- Logging for monitoring and debugging
- Error handling for malformed requests

## Performance Optimizations

- HTTP 301 permanent redirects for SEO benefits
- Cache-Control headers reduce repeat requests
- Minimal execution time for cost efficiency
- X-Robots-Tag prevents search engine indexing of redirect URLs

## Integration with Azure Static Web Apps

The function automatically integrates with Azure Static Web Apps when deployed. Requests to `/files/*` will be handled by the Azure Functions runtime at `/api/files/*`.

URLs that previously would have been:
- `https://yoursite.com/files/document.pdf`

Are now handled through:
- `https://yoursite.com/files/document.pdf` → redirects via Azure Functions

The user experience remains identical while gaining proper wildcard redirect functionality.
