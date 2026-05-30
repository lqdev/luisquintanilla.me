# Tools

Small, single-purpose tools I build for myself and share. They're the moving parts behind how I read, publish, and listen on the open web — capture what's interesting, publish to my own site first, and keep my media habits in formats I control.

Everything here is open source and self-hostable or installable from source. No accounts, no tracking, your data stays yours.

## Read It Later

A minimal Chrome extension that saves the current page to a local-first reading list with zero organizational overhead — no folders, no tags, just a chronological list. Quick-capture via right-click or `Alt+Shift+R`, with a badge counter on the toolbar. Everything is stored locally in the browser and exports cleanly to JSON or Markdown, so it feeds straight into my [linkblog](/bookmarks/) workflow without any cloud middleman.

- **Platform**: Chromium browsers (Manifest V3) · **Language**: JavaScript
- **Solves**: Saving things to read later without handing my reading habits to a third party.
- [github.com/lqdev/read-it-later-browser-extension](https://github.com/lqdev/read-it-later-browser-extension)

## RSS Feed Detector

A lightweight browser extension that automatically detects RSS and Atom feeds on any page and lets me subscribe in one click through my reader of choice (NewsBlur, Feedly, Inoreader, The Old Reader, or a custom URL pattern). The toolbar icon changes color and shows a badge when feeds are found, and feed URLs are one click to copy.

- **Platform**: Chromium browsers (Manifest V3) · **Language**: JavaScript
- **Solves**: Bringing feed discovery back to the browser now that sites have buried their RSS links.
- [github.com/lqdev/rss-browser-extension](https://github.com/lqdev/rss-browser-extension)

## GitHub Post Creator

A small Progressive Web App that turns my phone's native share sheet into a publishing endpoint for this site. Share a page from any app and it drafts a bookmark or response as a commit to my GitHub repo — [POSSE](https://indieweb.org/POSSE) from anywhere, no native app required. It installs as a share target and works offline.

- **Platform**: Installable PWA (share-target) · **Language**: HTML / JavaScript
- **Solves**: Mobile publishing to my IndieWeb site straight from the OS share menu.
- [github.com/lqdev/github-post-pwa](https://github.com/lqdev/github-post-pwa)

## Playlist Creator

A Python tool that converts Spotify playlists into portable formats — beautifully formatted Markdown documentation and M3U playlists, with optional YouTube links for VLC-compatible playback. It can also convert existing Markdown playlist files back to M3U, so a playlist becomes a durable, plain-text artifact instead of something locked inside one service.

- **Platform**: CLI (`uv` / `pip`) · **Language**: Python
- **Solves**: Getting playlists out of a walled garden and into formats I can archive and share.
- [github.com/lqdev/playlist-creator](https://github.com/lqdev/playlist-creator)

## Podcast TUI

A cross-platform terminal user interface for managing podcasts — subscriptions with OPML import/export, parallel downloads, episode browsing, device sync, playlists, audio playback, discovery, tagging, and optional scrobbling. Keyboard-driven and fast, it's how I keep up with podcasts without a heavyweight app.

- **Platform**: Terminal (cross-platform) · **Language**: Rust
- **Solves**: A fast, ownable podcast client that lives in the terminal and speaks open formats.
- [github.com/lqdev/podcast-tui](https://github.com/lqdev/podcast-tui)

## Podcast Scrobbler

A self-hosted, [ListenBrainz](https://listenbrainz.readthedocs.io/)-compatible scrobble server optimized for podcasts. It's a lightweight REST API that records what I listen to — episodes, timestamps, progress — and exposes aggregation endpoints (top podcasts, weekly stats). Built as a companion to Podcast TUI, but any ListenBrainz-compatible client can submit listens. This is the engine that will power a future `/now-playing` view here.

- **Platform**: Self-hosted (Docker) · **Language**: C# / .NET 10 + DuckDB
- **Solves**: Owning my listening history instead of renting it from a streaming platform.
- [github.com/lqdev/podcast-scrobbler](https://github.com/lqdev/podcast-scrobbler)
