# Review Publishing Workflow Implementation Guide

## Quick Start Checklist

Based on the comprehensive [Review GitHub Issue Forms & Actions Publishing Specification](./review-github-issue-posting-specification.md), this guide provides actionable steps for implementation.

### Prerequisites ✅
- [x] **Existing Architecture**: GitHub Issue Forms + Actions + F# Scripts + PR workflow
- [x] **Review System**: Custom blocks with ReviewData schema supporting multiple types
- [x] **Domain Types**: Book, Movie, Music, Business, Product support confirmed
- [x] **Processing Pattern**: Proven workflow for notes, responses, bookmarks, media

## Implementation Steps

### Step 1: Create Issue Templates
Copy these files to `.github/ISSUE_TEMPLATE/`:

1. **`post-review-book.yml`** - Book review form (from specification)
2. **`post-review-movie.yml`** - Movie review form (from specification)  
3. **`post-review-music.yml`** - Music review form (from specification)
4. **`post-review-business.yml`** - Business review form (from specification)
5. **`post-review-product.yml`** - Product review form (from specification)

**Test**: Create a GitHub issue with each template to verify form rendering.

### Step 2: Extend GitHub Actions Workflow

Add the `process-review` job to `.github/workflows/process-content-issue.yml`:

```yaml
# Add after existing jobs (process-media, etc.)
  process-review:
    if: (contains(github.event.issue.labels.*.name, 'review') || contains(github.event.issue.labels.*.name, 'book') || contains(github.event.issue.labels.*.name, 'movie') || contains(github.event.issue.labels.*.name, 'music') || contains(github.event.issue.labels.*.name, 'business') || contains(github.event.issue.labels.*.name, 'product')) && github.event.issue.user.login == 'lqdev'
    runs-on: ubuntu-latest
    steps:
      # ... (complete job definition from specification)
```

**Key Integration Points**:
- **Label Detection**: Identifies review type from issue labels
- **Data Extraction**: Parses form fields based on review type
- **F# Script Invocation**: Calls `Scripts/process-review-issue.fsx`
- **PR Creation**: Generates review-specific pull request

### Step 3: Create F# Processing Script

Create `Scripts/process-review-issue.fsx` with:

```fsharp
(*
    Process GitHub Issue Template for Review Creation
    12 parameters: review_type, item_name, rating, summary, content, pros, cons, item_url, image_url, additional_fields_json, tags, slug
*)

#r "../bin/Debug/net9.0/PersonalSite.dll"

// Script implementation from specification...
```

**Key Features**:
- **Multi-Type Support**: Handles all 5 review types (book, movie, music, business, product)
- **Validation**: Rating range, required fields, content length
- **Metadata Processing**: Type-specific additional fields via JSON
- **File Generation**: Creates properly formatted markdown with review blocks

### Step 4: Testing Strategy

#### Unit Testing
Create test scripts for each review type:

```bash
# Test book review
dotnet fsi Scripts/process-review-issue.fsx -- \
  "book" "The Four Agreements" "4.5 - Excellent" \
  "Ancient Toltec wisdom" "Detailed review content..." \
  "Clear principles\nPractical advice" "Very brief" \
  "https://openlibrary.org/works/OL27203W" \
  "https://covers.openlibrary.org/b/id/15101528-L.jpg" \
  '{"author":"Don Miguel Ruiz","isbn":"9781878424945"}' \
  "philosophy,self-help" "four-agreements-test"
```

#### Integration Testing
1. **Form Submission**: Test each issue template creates proper issue
2. **Workflow Trigger**: Verify GitHub Action detects and processes review issues
3. **Content Generation**: Confirm F# script creates valid markdown files
4. **PR Creation**: Check pull requests contain correct metadata and content

#### End-to-End Testing
1. Submit review via GitHub Issue Form
2. Monitor GitHub Action execution logs
3. Verify generated file in `_src/reviews/`
4. Check pull request creation and content
5. Confirm issue closure and user notification

### Step 5: Rollout Plan

#### Phase 1: Single Type Validation (Week 1)
- Implement book review workflow only
- Test with 2-3 book reviews
- Validate complete pipeline functionality
- Fix any integration issues

#### Phase 2: Multi-Type Implementation (Week 2)
- Add remaining review types (movie, music, business, product)
- Test each type individually
- Verify type-specific field handling
- Confirm metadata preservation

#### Phase 3: Production Deployment (Week 3)
- Enable for authorized users
- Create user documentation
- Monitor initial usage
- Address any user feedback

## Type-Specific Implementation Notes

### Books ✅
- **Existing Content**: 39 book reviews in `_src/reviews/library/`
- **Metadata Fields**: author, isbn, genre
- **External Links**: Open Library, Amazon
- **Image Support**: Book covers via Open Library API

### Movies
- **Metadata Fields**: director, year, genre, runtime
- **External Links**: IMDB, Rotten Tomatoes
- **Image Support**: Movie posters via TMDB API
- **Rating Integration**: Optional external ratings

### Music
- **Metadata Fields**: artist, music_type, release_year, genre, label
- **External Links**: Spotify, Apple Music, Bandcamp
- **Types**: Album, Single, EP, Live Performance
- **Genre Support**: Multiple genre handling

### Business/Establishments
- **Metadata Fields**: business_type, location, price_range
- **External Links**: Yelp, Google Business, website
- **Location**: City, state/region format
- **Categories**: Restaurant, Coffee Shop, Retail, Service, etc.

### Products
- **Metadata Fields**: manufacturer, product_category, model_version, price
- **External Links**: Amazon, manufacturer website
- **Categories**: Technology, Software, Tools, Lifestyle, etc.
- **Pricing**: Optional current/purchase price

## Integration with Existing Architecture

### Custom Blocks Compatibility ✅
- **ReviewData Schema**: Already supports all review types
- **Additional Fields**: Flexible dictionary for type-specific metadata
- **Backward Compatibility**: Existing reviews continue working
- **Rendering**: Enhanced display with pros/cons, ratings, images

### Content Directory Structure
```
_src/reviews/
├── book-title-review-2025-01-15.md
├── movie-title-review-2025-01-15.md
├── album-title-review-2025-01-15.md
├── business-name-review-2025-01-15.md
└── product-name-review-2025-01-15.md
```

### Build System Integration
- **No Changes Required**: Existing review processing handles new content
- **RSS Feeds**: Reviews automatically included in content feeds
- **Search Integration**: Review metadata indexed for search
- **Tag System**: Review tags included in site-wide tag navigation

## Success Metrics

### Usage Metrics
- **Submission Rate**: Target 2+ reviews per week
- **Success Rate**: 95%+ successful processing
- **User Satisfaction**: Positive feedback on ease of use
- **Content Quality**: Consistent formatting and metadata

### Technical Metrics
- **Processing Time**: <2 minutes from submission to PR
- **Error Rate**: <5% processing errors
- **Uptime**: 99%+ GitHub Actions availability
- **Data Quality**: 100% required field completion

### Content Metrics
- **Review Coverage**: All 5 types represented
- **Metadata Completeness**: 90%+ additional fields populated
- **External Links**: 80%+ reviews include item URLs
- **Image Integration**: 70%+ reviews include images

## Maintenance Considerations

### Regular Updates
- **Form Field Updates**: Quarterly review of field requirements
- **Validation Updates**: Adjust based on user feedback
- **Documentation**: Keep examples current with latest submissions
- **Error Monitoring**: Weekly review of failed processing attempts

### Future Enhancements
- **Additional Review Types**: Framework supports new types easily
- **Enhanced Validation**: More sophisticated content quality checks
- **Integration Improvements**: Better external API integration
- **Analytics**: Enhanced reporting on review trends and quality

This implementation guide provides clear, actionable steps for implementing the review publishing workflow while leveraging the existing proven architecture and maintaining high quality standards.