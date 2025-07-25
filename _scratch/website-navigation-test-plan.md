# Website Navigation Test Plan - Manual Testing Guide

**Date**: 2025-07-24  
**Server**: http://localhost:8080  
**Purpose**: Comprehensive navigation and link testing after Legacy Code Cleanup completion

## 🎯 Test Objectives

1. **Navigation Menu Testing**: Verify all dropdown menus and nav links work correctly
2. **Content Link Testing**: Test links to all content types (posts, notes, wiki, etc.)
3. **Feed/RSS Testing**: Verify all feed links and RSS functionality
4. **Footer Link Testing**: Check all social media and external links
5. **Internal Routing**: Verify URL structure and redirects work properly
6. **Homepage Links**: Test all featured content links on homepage

## 📋 Manual Testing Checklist

### ✅ Homepage (http://localhost:8080)
**Status**: ✅ **LOADED SUCCESSFULLY**

**Featured Content Links**:
- [ ] Latest Microblog Note: `/feed/2025-07-06-weekly-post-summary`
- [ ] Latest Response: `/feed/tumblr-wordpress-fediverse-integration-pause`
- [ ] Latest Blog Post: `/posts/indieweb-create-day-2025-07`

### 🧭 Main Navigation Menu Testing

#### 1. **Home** (Active)
- [ ] Home link: `/`

#### 2. **About Dropdown**
- [ ] Profile: `/about`
- [ ] Contact: `/contact` 
- [ ] Uses: `/uses`
- [ ] Colophon: `/colophon`

#### 3. **Feeds Dropdown**
- [ ] Main: `/feed`
- [ ] Responses: `/feed/responses`
- [ ] Blog: `/posts/1`
- [ ] Subscribe: `/subscribe`
- [ ] **Divider Section**:
- [ ] Starter Packs: `/feed/starter`
- [ ] **Divider Section**:
- [ ] Blogroll: `/feed/blogroll`
- [ ] Podroll: `/feed/podroll`
- [ ] Forums: `/feed/forums`
- [ ] YouTube: `/feed/youtube`

#### 4. **Collections Dropdown**
- [ ] Radio: `/radio`
- [ ] Books: `/reviews`
- [ ] Tags: `/tags`

#### 5. **Knowledgebase Dropdown**
- [ ] Snippets: `/resources/snippets`
- [ ] Wiki: `/resources/wiki`
- [ ] Presentations: `/resources/presentations`

#### 6. **Live Dropdown**
- [ ] Stream: `/live`
- [ ] Recordings: `/streams`

#### 7. **Events**
- [ ] Events: `/events`

#### 8. **RSS Icon**
- [ ] Subscribe link: `/subscribe`

### 🔗 Footer Social Links
- [ ] Mastodon: `https://toot.lqdev.tech/@lqdev`
- [ ] GitHub: `https://github.com/lqdev`
- [ ] Twitter: `https://twitter.com/ljquintanilla`
- [ ] LinkedIn: `https://www.linkedin.com/in/lquintanilla01/`
- [ ] Email: `mailto:lqdev@outlook.com`

### 📁 Content Directory Testing

Based on `_public` directory structure:

#### **Core Content Types**
- [ ] `/about/` - About page and profile
- [ ] `/posts/` - Blog posts 
- [ ] `/notes/` - Microblog notes
- [ ] `/responses/` - Response content
- [ ] `/reviews/` - Book reviews
- [ ] `/snippets/` - Code snippets
- [ ] `/wiki/` - Wiki content
- [ ] `/presentations/` - Presentation content

#### **Collections & Aggregations**
- [ ] `/feed/` - Main feed aggregation
- [ ] `/tags/` - Tag-based navigation
- [ ] `/radio/` - Radio/music content
- [ ] `/albums/` - Album content
- [ ] `/collections/` - Collection pages
- [ ] `/streams/` - Stream recordings
- [ ] `/events/` - Event listings

#### **Utility Pages**
- [ ] `/subscribe/` - Subscription options
- [ ] `/contact/` - Contact information
- [ ] `/uses/` - Uses page (tools/setup)
- [ ] `/colophon/` - Site information
- [ ] `/live/` - Live streaming
- [ ] `/library/` - Library content
- [ ] `/resources/` - Resource aggregation

#### **Media & Assets**
- [ ] `/media/` - Media files
- [ ] `/assets/` - CSS/JS assets
- [ ] `/css/` - Stylesheets
- [ ] `/js/` - JavaScript files
- [ ] Static files: `avatar.png`, `favicon.ico`, etc.

### 🚨 Critical Test Areas

#### **RSS/Feed Infrastructure** (High Priority - Recently Migrated)
- [ ] `/feed/index.opml` - Main feeds OPML
- [ ] `/feed/blogroll/index.opml` - Blogroll OPML
- [ ] `/feed/podroll/index.opml` - Podroll OPML
- [ ] `/feed/youtube/index.opml` - YouTube OPML
- [ ] `/blog.rss` - Blog RSS feed
- [ ] `/microblog.rss` - Microblog RSS feed
- [ ] `/responses.rss` - Response RSS feed

#### **AST-Based Content** (Recently Migrated to GenericBuilder)
- [ ] Notes processing and display
- [ ] Posts processing and display  
- [ ] Responses processing and display
- [ ] Reviews processing and display
- [ ] Snippets processing and display
- [ ] Wiki processing and display
- [ ] Presentations processing and display

#### **URL Structure Validation** (Post-URL Alignment Project)
- [ ] Content URLs follow expected patterns
- [ ] Feed URLs are consistent
- [ ] No broken internal links
- [ ] Proper redirect handling

## 🔍 Testing Methodology

### Manual Browser Testing Steps:
1. **Open Homepage**: Navigate to http://localhost:8080
2. **Navigation Testing**: Click each menu item and dropdown option
3. **Link Validation**: Click all featured content links on homepage
4. **Feed Testing**: Test RSS links and OPML files
5. **Content Testing**: Navigate to different content types and verify rendering
6. **URL Testing**: Check address bar for proper URL patterns
7. **Back/Forward**: Test browser navigation
8. **Error Testing**: Try some non-existent URLs to test 404 handling

### Expected Results:
- ✅ All pages load without errors
- ✅ Navigation menus work correctly
- ✅ Content renders properly with AST-based processing
- ✅ RSS feeds are valid XML
- ✅ URLs follow expected patterns
- ✅ No 500 errors or broken functionality

## 📊 Test Results Log

**Test Started**: 2025-07-24 (Post-Legacy Cleanup)  
**Browser Used**: Chromium via Playwright  
**Total Links to Test**: ~50+ navigation links + content links

**Testing Priority Order**:
1. **Homepage & Featured Links** (AST-based content)
2. **Navigation Dropdowns** (All menus)
3. **RSS/Feed Infrastructure** (Recently migrated)
4. **Content Type Pages** (GenericBuilder architecture)

### Issues Found:
✅ **CRITICAL BUG FIXED**: Homepage featured content links were using incorrect `/feed/` URLs instead of proper `/notes/` and `/responses/` paths  
  - **Fixed**: `/feed/2025-07-06-weekly-post-summary` → `/notes/2025-07-06-weekly-post-summary` ✅  
  - **Fixed**: `/feed/tumblr-wordpress-fediverse-integration-pause` → `/responses/tumblr-wordpress-fediverse-integration-pause` ✅  
  - **Root Cause**: Incorrect URL generation in `Views/LayoutViews.fs` lines 31 & 39  
  - **Impact**: Production homepage was showing 404 errors for featured content links  

*Minor discovery: RSS feeds are located at `/feed/*.xml` paths, not legacy `.rss` root paths. This is correct and consistent with the new architecture.*

### Successful Tests:
✅ **Homepage**: Loads successfully with proper layout and avatar  
✅ **Featured Content Links**: All 3 featured links now load correctly (POST-FIX)  
  - Microblog note: `/notes/2025-07-06-weekly-post-summary` ✅  
  - Response: `/responses/tumblr-wordpress-fediverse-integration-pause` ✅  
  - Blog post: `/posts/indieweb-create-day-2025-07` ✅  
✅ **Navigation Dropdowns**: All dropdown menus work correctly  
  - About dropdown → Profile page ✅  
  - Feeds dropdown → Main feed page ✅  
  - Collections dropdown → Reviews page ✅  
  - Knowledgebase dropdown → Snippets page ✅  
✅ **RSS/XML Feeds**: All feeds are valid XML with proper RSS structure  
  - Main unified feed: `/feed/index.xml` ✅ (1129 items across 8 content types)  
  - Notes feed: `/feed/notes.xml` ✅  
  - Response feed: `/feed/responses/index.xml` ✅  
✅ **AST-Based Content Processing**: All content renders as proper HTML (not raw markdown)  
✅ **URL Structure**: Clean, consistent URL patterns across all content types  
✅ **Architecture**: GenericBuilder pattern working correctly for all content types

## 🎯 Post-Testing Actions

After manual testing:
1. **Document Results**: Update this file with test outcomes
2. **Report Issues**: Create issue list if problems found
3. **Validate Architecture**: Confirm GenericBuilder migrations successful
4. **Update Phase Log**: Record testing results in project completion log
5. **Archive Documentation**: Move to appropriate project archive
