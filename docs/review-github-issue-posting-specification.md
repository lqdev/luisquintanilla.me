# Review GitHub Issue Forms & Actions Publishing Specification

## Overview

This specification details the implementation of GitHub Issue Forms and GitHub Actions workflow for review content creation, following the proven pattern established for notes, responses, bookmarks, and media content.

## Current System Analysis

### Proven Publishing Pattern ✅

The repository implements a successful **Issue Template → GitHub Action → F# Processing Script → Pull Request** pattern that:

1. **Issue Forms**: Collect structured user input with validation
2. **GitHub Actions**: Parse form data and trigger F# processing 
3. **F# Scripts**: Generate properly formatted markdown files with frontmatter
4. **Pull Requests**: Enable review and merge workflow for content publication
5. **Automated Cleanup**: Close issues and notify users on success

### Existing Review Architecture ✅

The review system already supports:
- **Multiple Review Types**: Books, Movies, Music, Business/Establishment, Product
- **Custom Block Format**: `:::review` YAML blocks with structured metadata
- **Flexible Schema**: Type-specific fields via `additionalFields` dictionary
- **Rich Metadata**: Ratings, pros/cons, images, external links
- **Backward Compatibility**: Works with existing book reviews

## Proposed Review Publishing Workflow

### 1. GitHub Issue Templates

Create issue templates for each supported review type following the established pattern:

#### A. Book Review Template
**File**: `.github/ISSUE_TEMPLATE/post-review-book.yml`

```yaml
name: 📚 Post a Book Review
description: Create a new book review post for the website
title: "[Book Review] "
labels: ["review", "book"]
body:
  - type: markdown
    attributes:
      value: |
        ## Create a New Book Review
        
        Use this form to create a book review that will automatically generate a pull request with properly formatted content including review metadata, rating, and custom blocks.
        
        **Note**: This will create a new review post in the `_src/reviews/` directory and generate a PR for review.

  - type: input
    id: book_title
    attributes:
      label: Book Title
      description: The title of the book you're reviewing
      placeholder: "e.g., The Four Agreements"
    validations:
      required: true

  - type: input
    id: author
    attributes:
      label: Author
      description: The book's author(s)
      placeholder: "e.g., Don Miguel Ruiz"
    validations:
      required: true

  - type: input
    id: isbn
    attributes:
      label: ISBN (Optional)
      description: The book's ISBN for reference
      placeholder: "e.g., 9781878424945"
    validations:
      required: false

  - type: dropdown
    id: rating
    attributes:
      label: Rating
      description: Your overall rating for this book
      options:
        - "5.0 - Excellent"
        - "4.5 - Very Good"
        - "4.0 - Good"
        - "3.5 - Above Average"
        - "3.0 - Average"
        - "2.5 - Below Average"
        - "2.0 - Poor"
        - "1.5 - Very Poor"
        - "1.0 - Terrible"
    validations:
      required: true

  - type: textarea
    id: summary
    attributes:
      label: Review Summary
      description: A brief summary of your review (will appear in the review block)
      placeholder: "A concise summary of your thoughts on this book..."
    validations:
      required: true

  - type: textarea
    id: content
    attributes:
      label: Detailed Review Content
      description: Your full review content (supports Markdown)
      placeholder: |
        Write your detailed review here. You can use Markdown formatting.
        
        ## What I Liked
        - Point 1
        - Point 2
        
        ## What Could Be Better
        - Point 1
        - Point 2
        
        ## Final Thoughts
        Your concluding thoughts...
    validations:
      required: true

  - type: textarea
    id: pros
    attributes:
      label: Pros (Optional)
      description: Positive aspects, one per line
      placeholder: |
        Clear writing style
        Practical advice
        Well-structured content
    validations:
      required: false

  - type: textarea
    id: cons
    attributes:
      label: Cons (Optional)  
      description: Areas for improvement, one per line
      placeholder: |
        Could be longer
        Limited examples
        Repetitive in places
    validations:
      required: false

  - type: input
    id: book_url
    attributes:
      label: Book URL (Optional)
      description: Link to the book (Open Library, Amazon, etc.)
      placeholder: "https://openlibrary.org/works/OL27203W/The_Four_Agreements"
    validations:
      required: false

  - type: input
    id: cover_image_url
    attributes:
      label: Cover Image URL (Optional)
      description: URL to book cover image
      placeholder: "https://covers.openlibrary.org/b/id/15101528-L.jpg"
    validations:
      required: false

  - type: input
    id: genre
    attributes:
      label: Genre (Optional)
      description: Book genre/category
      placeholder: "e.g., Philosophy, Self-Help, Fiction"
    validations:
      required: false

  - type: input
    id: tags
    attributes:
      label: Tags (Optional)
      description: Comma-separated tags for categorizing your review
      placeholder: "e.g., philosophy, personal-development, spirituality"
    validations:
      required: false

  - type: input
    id: slug
    attributes:
      label: Custom Slug (Optional)
      description: Custom URL slug for your review. If not provided, one will be generated from the book title.
      placeholder: "e.g., four-agreements-review (leave blank to auto-generate)"
    validations:
      required: false

  - type: markdown
    attributes:
      value: |
        ---
        
        **What happens next?**
        
        1. 🤖 A GitHub Action will process this issue
        2. 📄 Generate a properly formatted markdown file in `_src/reviews/`
        3. 🔀 Create a pull request with your review content
        4. ✅ The issue will be closed automatically
        5. 🚀 After PR review and merge, your book review will be live on the website
        
        The generated content will include proper frontmatter, structured review metadata, and a custom `:::review` block with your rating and feedback.
```

#### B. Movie Review Template
**File**: `.github/ISSUE_TEMPLATE/post-review-movie.yml`

```yaml
name: 🎬 Post a Movie Review
description: Create a new movie review post for the website
title: "[Movie Review] "
labels: ["review", "movie"]
body:
  - type: markdown
    attributes:
      value: |
        ## Create a New Movie Review
        
        Use this form to create a movie review that will automatically generate a pull request with properly formatted content.

  - type: input
    id: movie_title
    attributes:
      label: Movie Title
      description: The title of the movie you're reviewing
      placeholder: "e.g., Blade Runner 2049"
    validations:
      required: true

  - type: input
    id: director
    attributes:
      label: Director
      description: The movie's director(s)
      placeholder: "e.g., Denis Villeneuve"
    validations:
      required: true

  - type: input
    id: year
    attributes:
      label: Release Year
      description: Year the movie was released
      placeholder: "e.g., 2017"
    validations:
      required: true

  - type: dropdown
    id: rating
    attributes:
      label: Rating
      description: Your overall rating for this movie
      options:
        - "5.0 - Masterpiece"
        - "4.5 - Excellent"
        - "4.0 - Very Good"
        - "3.5 - Good"
        - "3.0 - Average"
        - "2.5 - Below Average"
        - "2.0 - Poor"
        - "1.5 - Very Poor"
        - "1.0 - Terrible"
    validations:
      required: true

  - type: textarea
    id: summary
    attributes:
      label: Review Summary
      description: A brief summary of your review
      placeholder: "A concise summary of your thoughts on this movie..."
    validations:
      required: true

  - type: textarea
    id: content
    attributes:
      label: Detailed Review Content
      description: Your full review content (supports Markdown)
      placeholder: |
        Write your detailed movie review here...
        
        ## Plot & Story
        Your thoughts on the plot...
        
        ## Performances
        Thoughts on acting...
        
        ## Direction & Cinematography
        Visual and directorial elements...
        
        ## Final Verdict
        Your concluding thoughts...
    validations:
      required: true

  - type: textarea
    id: pros
    attributes:
      label: What Worked Well
      description: Positive aspects, one per line
      placeholder: |
        Stunning visuals
        Strong performances
        Thought-provoking themes
    validations:
      required: false

  - type: textarea
    id: cons
    attributes:
      label: Areas for Improvement
      description: Weaker aspects, one per line
      placeholder: |
        Slow pacing in middle
        Overly long runtime
        Confusing subplot
    validations:
      required: false

  - type: input
    id: imdb_url
    attributes:
      label: IMDB URL (Optional)
      description: Link to the movie's IMDB page
      placeholder: "https://www.imdb.com/title/tt1856101/"
    validations:
      required: false

  - type: input
    id: poster_url
    attributes:
      label: Movie Poster URL (Optional)
      description: URL to movie poster image
      placeholder: "https://image.tmdb.org/t/p/w500/poster.jpg"
    validations:
      required: false

  - type: input
    id: genre
    attributes:
      label: Genre
      description: Movie genre(s)
      placeholder: "e.g., Sci-Fi, Drama, Thriller"
    validations:
      required: false

  - type: input
    id: tags
    attributes:
      label: Tags (Optional)
      description: Comma-separated tags
      placeholder: "e.g., sci-fi, cyberpunk, sequel"
    validations:
      required: false

  - type: input
    id: slug
    attributes:
      label: Custom Slug (Optional)
      description: Custom URL slug for your review
      placeholder: "e.g., blade-runner-2049-review"
    validations:
      required: false
```

#### C. Music Review Template
**File**: `.github/ISSUE_TEMPLATE/post-review-music.yml`

```yaml
name: 🎵 Post a Music Review
description: Create a new music review post for the website
title: "[Music Review] "
labels: ["review", "music"]
body:
  - type: markdown
    attributes:
      value: |
        ## Create a New Music Review
        
        Review albums, singles, or musical works with structured metadata and detailed analysis.

  - type: dropdown
    id: music_type
    attributes:
      label: Music Type
      description: What type of music are you reviewing?
      options:
        - "Album"
        - "Single"
        - "EP"
        - "Live Performance"
        - "Compilation"
    validations:
      required: true

  - type: input
    id: title
    attributes:
      label: Album/Song Title
      description: The name of the music you're reviewing
      placeholder: "e.g., OK Computer"
    validations:
      required: true

  - type: input
    id: artist
    attributes:
      label: Artist/Band
      description: The performing artist or band
      placeholder: "e.g., Radiohead"
    validations:
      required: true

  - type: input
    id: release_year
    attributes:
      label: Release Year
      description: Year of release
      placeholder: "e.g., 1997"
    validations:
      required: true

  - type: dropdown
    id: rating
    attributes:
      label: Rating
      description: Your overall rating
      options:
        - "5.0 - Masterpiece"
        - "4.5 - Excellent"
        - "4.0 - Very Good"
        - "3.5 - Good"
        - "3.0 - Average"
        - "2.5 - Below Average"
        - "2.0 - Poor"
        - "1.5 - Very Poor"
        - "1.0 - Terrible"
    validations:
      required: true

  - type: textarea
    id: summary
    attributes:
      label: Review Summary
      description: Brief summary of your review
      placeholder: "Your concise thoughts on this music..."
    validations:
      required: true

  - type: textarea
    id: content
    attributes:
      label: Detailed Review
      description: Full review content
      placeholder: |
        ## Musical Style & Genre
        Description of the musical approach...
        
        ## Standout Tracks
        Highlight notable songs...
        
        ## Production & Sound
        Thoughts on production quality...
        
        ## Overall Impact
        Your final assessment...
    validations:
      required: true

  - type: input
    id: genre
    attributes:
      label: Genre
      description: Musical genre(s)
      placeholder: "e.g., Alternative Rock, Electronic"
    validations:
      required: true

  - type: input
    id: label
    attributes:
      label: Record Label (Optional)
      description: The record label
      placeholder: "e.g., Parlophone"
    validations:
      required: false

  - type: input
    id: spotify_url
    attributes:
      label: Spotify/Music URL (Optional)
      description: Link to listen to the music
      placeholder: "https://open.spotify.com/album/..."
    validations:
      required: false

  - type: input
    id: tags
    attributes:
      label: Tags (Optional)
      description: Comma-separated tags
      placeholder: "e.g., experimental, ambient, guitar"
    validations:
      required: false

  - type: input
    id: slug
    attributes:
      label: Custom Slug (Optional)
      description: Custom URL slug
      placeholder: "e.g., ok-computer-review"
    validations:
      required: false
```

#### D. Business Review Template
**File**: `.github/ISSUE_TEMPLATE/post-review-business.yml`

```yaml
name: 🏢 Post a Business Review
description: Create a new business/establishment review post for the website
title: "[Business Review] "
labels: ["review", "business"]
body:
  - type: markdown
    attributes:
      value: |
        ## Create a New Business Review
        
        Review restaurants, coffee shops, stores, services, and other local businesses.

  - type: input
    id: business_name
    attributes:
      label: Business Name
      description: Name of the business you're reviewing
      placeholder: "e.g., Blue Bottle Coffee"
    validations:
      required: true

  - type: dropdown
    id: business_type
    attributes:
      label: Business Type
      description: What type of business is this?
      options:
        - "Restaurant"
        - "Coffee Shop"
        - "Retail Store"
        - "Service Provider"
        - "Entertainment Venue"
        - "Hotel/Accommodation"
        - "Other"
    validations:
      required: true

  - type: input
    id: location
    attributes:
      label: Location
      description: City, state/region where the business is located
      placeholder: "e.g., San Francisco, CA"
    validations:
      required: true

  - type: dropdown
    id: rating
    attributes:
      label: Rating
      description: Your overall rating
      options:
        - "5.0 - Outstanding"
        - "4.5 - Excellent"
        - "4.0 - Very Good"
        - "3.5 - Good"
        - "3.0 - Average"
        - "2.5 - Below Average"
        - "2.0 - Poor"
        - "1.5 - Very Poor"
        - "1.0 - Terrible"
    validations:
      required: true

  - type: textarea
    id: summary
    attributes:
      label: Review Summary
      description: Brief summary of your experience
      placeholder: "Quick summary of your visit and overall impression..."
    validations:
      required: true

  - type: textarea
    id: content
    attributes:
      label: Detailed Review
      description: Full review content
      placeholder: |
        ## The Experience
        Describe your visit...
        
        ## Service & Staff
        Quality of service...
        
        ## Atmosphere & Environment
        The setting and ambiance...
        
        ## Value & Pricing
        Cost vs. quality assessment...
        
        ## Would I Return?
        Final thoughts and recommendation...
    validations:
      required: true

  - type: input
    id: website_url
    attributes:
      label: Business Website (Optional)
      description: Link to the business website
      placeholder: "https://bluebottlecoffee.com"
    validations:
      required: false

  - type: input
    id: price_range
    attributes:
      label: Price Range (Optional)
      description: Approximate pricing level
      placeholder: "e.g., $$$ (Expensive)"
    validations:
      required: false

  - type: input
    id: tags
    attributes:
      label: Tags (Optional)
      description: Comma-separated tags
      placeholder: "e.g., coffee, local-favorite, wifi-friendly"
    validations:
      required: false

  - type: input
    id: slug
    attributes:
      label: Custom Slug (Optional)
      description: Custom URL slug
      placeholder: "e.g., blue-bottle-coffee-review"
    validations:
      required: false
```

#### E. Product Review Template
**File**: `.github/ISSUE_TEMPLATE/post-review-product.yml`

```yaml
name: 📦 Post a Product Review
description: Create a new product review post for the website
title: "[Product Review] "
labels: ["review", "product"]
body:
  - type: markdown
    attributes:
      value: |
        ## Create a New Product Review
        
        Review technology, books, tools, gadgets, or any other products you've used.

  - type: input
    id: product_name
    attributes:
      label: Product Name
      description: Name of the product you're reviewing
      placeholder: "e.g., MacBook Pro M2"
    validations:
      required: true

  - type: input
    id: manufacturer
    attributes:
      label: Manufacturer/Brand
      description: The company that makes this product
      placeholder: "e.g., Apple"
    validations:
      required: true

  - type: dropdown
    id: product_category
    attributes:
      label: Product Category
      description: What type of product is this?
      options:
        - "Technology/Electronics"
        - "Software/Apps"
        - "Books/Media"
        - "Tools/Equipment"
        - "Home/Lifestyle"
        - "Health/Fitness"
        - "Other"
    validations:
      required: true

  - type: dropdown
    id: rating
    attributes:
      label: Rating
      description: Your overall rating
      options:
        - "5.0 - Exceptional"
        - "4.5 - Excellent"
        - "4.0 - Very Good"
        - "3.5 - Good"
        - "3.0 - Average"
        - "2.5 - Below Average"
        - "2.0 - Poor"
        - "1.5 - Very Poor"
        - "1.0 - Terrible"
    validations:
      required: true

  - type: textarea
    id: summary
    attributes:
      label: Review Summary
      description: Brief summary of your experience with the product
      placeholder: "Quick overview of the product and your main impressions..."
    validations:
      required: true

  - type: textarea
    id: content
    attributes:
      label: Detailed Review
      description: Full product review
      placeholder: |
        ## First Impressions
        Initial thoughts and setup experience...
        
        ## Features & Performance
        How well does it work? Key features...
        
        ## Build Quality & Design
        Physical aspects and durability...
        
        ## Value for Money
        Price vs. features assessment...
        
        ## Who Should Buy This?
        Recommendations for potential buyers...
    validations:
      required: true

  - type: input
    id: model_version
    attributes:
      label: Model/Version (Optional)
      description: Specific model or version reviewed
      placeholder: "e.g., 13-inch M2, Version 2.0"
    validations:
      required: false

  - type: input
    id: price
    attributes:
      label: Price (Optional)
      description: Current or purchase price
      placeholder: "e.g., $1999"
    validations:
      required: false

  - type: input
    id: product_url
    attributes:
      label: Product URL (Optional)
      description: Link to product page (official site, Amazon, etc.)
      placeholder: "https://www.apple.com/macbook-pro/"
    validations:
      required: false

  - type: input
    id: tags
    attributes:
      label: Tags (Optional)
      description: Comma-separated tags
      placeholder: "e.g., laptop, development, m2-chip"
    validations:
      required: false

  - type: input
    id: slug
    attributes:
      label: Custom Slug (Optional)
      description: Custom URL slug
      placeholder: "e.g., macbook-pro-m2-review"
    validations:
      required: false
```

### 2. GitHub Action Workflow Enhancement

Extend the existing `.github/workflows/process-content-issue.yml` with review processing jobs:

```yaml
  process-review:
    # Only run if issue has any review label AND issue author is @lqdev
    if: (contains(github.event.issue.labels.*.name, 'review') || contains(github.event.issue.labels.*.name, 'book') || contains(github.event.issue.labels.*.name, 'movie') || contains(github.event.issue.labels.*.name, 'music') || contains(github.event.issue.labels.*.name, 'business') || contains(github.event.issue.labels.*.name, 'product')) && github.event.issue.user.login == 'lqdev'
    runs-on: ubuntu-latest
    
    steps:
      - name: Checkout repository
        uses: actions/checkout@v4
        with:
          token: ${{ secrets.GITHUB_TOKEN }}
          
      - name: Setup .NET 9
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'
          
      - name: Determine review type
        id: determine-type
        uses: actions/github-script@v7
        with:
          script: |
            const labels = context.payload.issue.labels.map(label => label.name);
            let reviewType = '';
            
            if (labels.includes('book')) reviewType = 'book';
            else if (labels.includes('movie')) reviewType = 'movie';
            else if (labels.includes('music')) reviewType = 'music';
            else if (labels.includes('business')) reviewType = 'business';
            else if (labels.includes('product')) reviewType = 'product';
            else reviewType = 'unknown';
            
            console.log('Detected review type:', reviewType);
            core.setOutput('review_type', reviewType);
          
      - name: Parse issue and extract form data
        id: extract-data
        uses: actions/github-script@v7
        with:
          script: |
            try {
              function extractFormValue(body, label) {
                const regex = new RegExp(`### ${label}\\s*\\n\\s*\\n([\\s\\S]*?)(?=\\n\\n###|\\n\\n---|\$)`, 'i');
                const match = body.match(regex);
                const value = match ? match[1].trim() : '';
                return value.replace(/^_No response_$/i, '').trim();
              }
              
              const issueBody = context.payload.issue.body;
              const reviewType = '${{ steps.determine-type.outputs.review_type }}';
              
              // Common fields
              const rating = extractFormValue(issueBody, 'Rating');
              const summary = extractFormValue(issueBody, '(Review )?Summary');
              const content = extractFormValue(issueBody, '(Detailed )?(Review )?Content');
              const pros = extractFormValue(issueBody, 'Pros|What Worked Well');
              const cons = extractFormValue(issueBody, 'Cons|Areas for Improvement');
              const tags = extractFormValue(issueBody, 'Tags \\(Optional\\)');
              const slug = extractFormValue(issueBody, '(Custom )?Slug \\(Optional\\)');
              
              // Type-specific fields
              let itemName = '';
              let itemUrl = '';
              let imageUrl = '';
              let additionalFields = {};
              
              switch(reviewType) {
                case 'book':
                  itemName = extractFormValue(issueBody, 'Book Title');
                  itemUrl = extractFormValue(issueBody, 'Book URL \\(Optional\\)');
                  imageUrl = extractFormValue(issueBody, 'Cover Image URL \\(Optional\\)');
                  additionalFields = {
                    author: extractFormValue(issueBody, 'Author'),
                    isbn: extractFormValue(issueBody, 'ISBN \\(Optional\\)'),
                    genre: extractFormValue(issueBody, 'Genre \\(Optional\\)')
                  };
                  break;
                case 'movie':
                  itemName = extractFormValue(issueBody, 'Movie Title');
                  itemUrl = extractFormValue(issueBody, 'IMDB URL \\(Optional\\)');
                  imageUrl = extractFormValue(issueBody, 'Movie Poster URL \\(Optional\\)');
                  additionalFields = {
                    director: extractFormValue(issueBody, 'Director'),
                    year: extractFormValue(issueBody, 'Release Year'),
                    genre: extractFormValue(issueBody, 'Genre')
                  };
                  break;
                case 'music':
                  itemName = extractFormValue(issueBody, 'Album/Song Title');
                  itemUrl = extractFormValue(issueBody, 'Spotify/Music URL \\(Optional\\)');
                  additionalFields = {
                    artist: extractFormValue(issueBody, 'Artist/Band'),
                    music_type: extractFormValue(issueBody, 'Music Type'),
                    release_year: extractFormValue(issueBody, 'Release Year'),
                    genre: extractFormValue(issueBody, 'Genre'),
                    label: extractFormValue(issueBody, 'Record Label \\(Optional\\)')
                  };
                  break;
                case 'business':
                  itemName = extractFormValue(issueBody, 'Business Name');
                  itemUrl = extractFormValue(issueBody, 'Business Website \\(Optional\\)');
                  additionalFields = {
                    business_type: extractFormValue(issueBody, 'Business Type'),
                    location: extractFormValue(issueBody, 'Location'),
                    price_range: extractFormValue(issueBody, 'Price Range \\(Optional\\)')
                  };
                  break;
                case 'product':
                  itemName = extractFormValue(issueBody, 'Product Name');
                  itemUrl = extractFormValue(issueBody, 'Product URL \\(Optional\\)');
                  additionalFields = {
                    manufacturer: extractFormValue(issueBody, 'Manufacturer/Brand'),
                    product_category: extractFormValue(issueBody, 'Product Category'),
                    model_version: extractFormValue(issueBody, 'Model/Version \\(Optional\\)'),
                    price: extractFormValue(issueBody, 'Price \\(Optional\\)')
                  };
                  break;
              }
              
              console.log('Extracted data:', { reviewType, itemName, rating, summary });
              
              // Set outputs for F# script
              core.setOutput('review_type', reviewType);
              core.setOutput('item_name', itemName);
              core.setOutput('rating', rating);
              core.setOutput('summary', summary);
              core.setOutput('content', content);
              core.setOutput('pros', pros);
              core.setOutput('cons', cons);
              core.setOutput('item_url', itemUrl);
              core.setOutput('image_url', imageUrl);
              core.setOutput('additional_fields', JSON.stringify(additionalFields));
              core.setOutput('tags', tags);
              core.setOutput('slug', slug);
              
            } catch (error) {
              console.error('Error extracting issue data:', error);
              throw error;
            }
          
      - name: Build F# project
        run: dotnet build
        
      - name: Process issue with F# script
        id: process-issue
        env:
          ISSUE_REVIEW_TYPE: ${{ steps.extract-data.outputs.review_type }}
          ISSUE_ITEM_NAME: ${{ steps.extract-data.outputs.item_name }}
          ISSUE_RATING: ${{ steps.extract-data.outputs.rating }}
          ISSUE_SUMMARY: ${{ steps.extract-data.outputs.summary }}
          ISSUE_CONTENT: ${{ steps.extract-data.outputs.content }}
          ISSUE_PROS: ${{ steps.extract-data.outputs.pros }}
          ISSUE_CONS: ${{ steps.extract-data.outputs.cons }}
          ISSUE_ITEM_URL: ${{ steps.extract-data.outputs.item_url }}
          ISSUE_IMAGE_URL: ${{ steps.extract-data.outputs.image_url }}
          ISSUE_ADDITIONAL_FIELDS: ${{ steps.extract-data.outputs.additional_fields }}
          ISSUE_TAGS: ${{ steps.extract-data.outputs.tags }}
          ISSUE_SLUG: ${{ steps.extract-data.outputs.slug }}
        run: |
          # Create temporary files for safe parameter passing
          echo "$ISSUE_REVIEW_TYPE" > /tmp/review_type.txt
          echo "$ISSUE_ITEM_NAME" > /tmp/item_name.txt
          echo "$ISSUE_RATING" > /tmp/rating.txt
          echo "$ISSUE_SUMMARY" > /tmp/summary.txt
          echo "$ISSUE_CONTENT" > /tmp/content.txt
          echo "$ISSUE_PROS" > /tmp/pros.txt
          echo "$ISSUE_CONS" > /tmp/cons.txt
          echo "$ISSUE_ITEM_URL" > /tmp/item_url.txt
          echo "$ISSUE_IMAGE_URL" > /tmp/image_url.txt
          echo "$ISSUE_ADDITIONAL_FIELDS" > /tmp/additional_fields.txt
          echo "$ISSUE_TAGS" > /tmp/tags.txt
          echo "$ISSUE_SLUG" > /tmp/slug.txt
          
          # Run F# script with file-based parameters
          OUTPUT=$(dotnet fsi Scripts/process-review-issue.fsx -- \
            "$(cat /tmp/review_type.txt)" \
            "$(cat /tmp/item_name.txt)" \
            "$(cat /tmp/rating.txt)" \
            "$(cat /tmp/summary.txt)" \
            "$(cat /tmp/content.txt)" \
            "$(cat /tmp/pros.txt)" \
            "$(cat /tmp/cons.txt)" \
            "$(cat /tmp/item_url.txt)" \
            "$(cat /tmp/image_url.txt)" \
            "$(cat /tmp/additional_fields.txt)" \
            "$(cat /tmp/tags.txt)" \
            "$(cat /tmp/slug.txt)" 2>&1)
          EXIT_CODE=$?
          
          # Clean up temporary files
          rm -f /tmp/review_type.txt /tmp/item_name.txt /tmp/rating.txt /tmp/summary.txt /tmp/content.txt /tmp/pros.txt /tmp/cons.txt /tmp/item_url.txt /tmp/image_url.txt /tmp/additional_fields.txt /tmp/tags.txt /tmp/slug.txt
          
          echo "Script output:"
          echo "$OUTPUT"
          
          if [ $EXIT_CODE -eq 0 ]; then
            echo "success=true" >> $GITHUB_OUTPUT
            # Extract filename from output
            FILENAME=$(echo "$OUTPUT" | grep "📁 File:" | sed 's/.*📁 File: _src\/reviews\///' | sed 's/ .*//')
            echo "filename=$FILENAME" >> $GITHUB_OUTPUT
          else
            echo "success=false" >> $GITHUB_OUTPUT
            echo "error=$OUTPUT" >> $GITHUB_OUTPUT
            exit 1
          fi
          
      - name: Create Pull Request
        if: steps.process-issue.outputs.success == 'true'
        uses: peter-evans/create-pull-request@v5
        with:
          token: ${{ secrets.GITHUB_TOKEN }}
          commit-message: "Add ${{ steps.extract-data.outputs.review_type }} review: ${{ steps.extract-data.outputs.item_name }}"
          title: "Add ${{ steps.extract-data.outputs.review_type }} review: ${{ steps.extract-data.outputs.item_name }}"
          body: |
            ## New ${{ steps.extract-data.outputs.review_type }} Review
            
            **Item:** ${{ steps.extract-data.outputs.item_name }}
            **Type:** ${{ steps.extract-data.outputs.review_type }}
            **Rating:** ${{ steps.extract-data.outputs.rating }}
            **File:** `_src/reviews/${{ steps.process-issue.outputs.filename }}`
            
            ### Review Summary
            ${{ steps.extract-data.outputs.summary }}
            
            ### Frontmatter Validation
            - ✅ Item Name: ${{ steps.extract-data.outputs.item_name }}
            - ✅ Review Type: ${{ steps.extract-data.outputs.review_type }}
            - ✅ Rating: ${{ steps.extract-data.outputs.rating }}
            - ✅ Tags: ${{ steps.extract-data.outputs.tags }}
            - ✅ Custom Slug: ${{ steps.extract-data.outputs.slug }}
            
            **Created via GitHub Issue Template #${{ github.event.issue.number }}**
            **Processed by F# script using .NET 9**
          branch: content/issue-${{ github.event.issue.number }}/review/${{ steps.extract-data.outputs.review_type }}-processed
          delete-branch: true
          
      - name: Close issue on success
        if: steps.process-issue.outputs.success == 'true'
        uses: actions/github-script@v7
        with:
          script: |
            github.rest.issues.update({
              owner: context.repo.owner,
              repo: context.repo.repo,
              issue_number: context.payload.issue.number,
              state: 'closed'
            });
            
            github.rest.issues.createComment({
              owner: context.repo.owner,
              repo: context.repo.repo,
              issue_number: context.payload.issue.number,
              body: '🎉 Your ${{ steps.extract-data.outputs.review_type }} review has been processed using F# and .NET 9! A pull request has been created with your content. You can track the progress in the pull requests tab.'
            });
            
      - name: Handle processing errors
        if: steps.process-issue.outputs.success == 'false'
        uses: actions/github-script@v7
        with:
          script: |
            github.rest.issues.createComment({
              owner: context.repo.owner,
              repo: context.repo.repo,
              issue_number: context.payload.issue.number,
              body: `❌ **Error processing review request with F# script**\n\n${steps.process-issue.outputs.error}\n\nPlease check your issue format and try again. The issue will remain open for you to edit and resubmit.`
            });
```

### 3. F# Processing Script

Create **`Scripts/process-review-issue.fsx`** following the established pattern:

```fsharp
(*
    Process GitHub Issue Template for Review Creation
    
    This script processes GitHub issue template data to create a review post.
    Usage: dotnet fsi process-review-issue.fsx -- "review_type" "item_name" "rating" "summary" "content" "pros" "cons" "item_url" "image_url" "additional_fields_json" "tags" "slug"
*)

#r "../bin/Debug/net9.0/PersonalSite.dll"

open System
open System.IO
open System.Text.RegularExpressions
open System.Text.Json

// Get command line arguments
let args = fsi.CommandLineArgs |> Array.skip 1

// Validate arguments
if args.Length < 5 then
    printfn "❌ Error: Missing required arguments"
    printfn "Usage: dotnet fsi process-review-issue.fsx -- \"review_type\" \"item_name\" \"rating\" \"summary\" \"content\" \"pros\" \"cons\" \"item_url\" \"image_url\" \"additional_fields_json\" \"tags\" \"slug\""
    exit 1

let reviewType = args.[0].Trim()
let itemName = args.[1].Trim()
let ratingStr = args.[2].Trim()
let summary = args.[3].Trim()
let content = args.[4].Trim()
let prosInput = if args.Length > 5 && not (String.IsNullOrWhiteSpace(args.[5])) then Some(args.[5].Trim()) else None
let consInput = if args.Length > 6 && not (String.IsNullOrWhiteSpace(args.[6])) then Some(args.[6].Trim()) else None
let itemUrl = if args.Length > 7 && not (String.IsNullOrWhiteSpace(args.[7])) then Some(args.[7].Trim()) else None
let imageUrl = if args.Length > 8 && not (String.IsNullOrWhiteSpace(args.[8])) then Some(args.[8].Trim()) else None
let additionalFieldsJson = if args.Length > 9 && not (String.IsNullOrWhiteSpace(args.[9])) then Some(args.[9].Trim()) else None
let tagsInput = if args.Length > 10 && not (String.IsNullOrWhiteSpace(args.[10])) then Some(args.[10].Trim()) else None
let customSlug = if args.Length > 11 && not (String.IsNullOrWhiteSpace(args.[11])) then Some(args.[11].Trim()) else None

// Validation
if String.IsNullOrWhiteSpace(reviewType) then
    printfn "❌ Error: Review type is required"
    exit 1

if String.IsNullOrWhiteSpace(itemName) then
    printfn "❌ Error: Item name is required"
    exit 1

if String.IsNullOrWhiteSpace(ratingStr) then
    printfn "❌ Error: Rating is required"
    exit 1

// Parse rating from dropdown format "4.5 - Very Good" -> 4.5
let rating = 
    let ratingMatch = Regex.Match(ratingStr, @"^(\d+(?:\.\d+)?)")
    if ratingMatch.Success then
        match Double.TryParse(ratingMatch.Groups.[1].Value) with
        | true, r -> r
        | false, _ -> 
            printfn "❌ Error: Invalid rating format: %s" ratingStr
            exit 1
    else
        printfn "❌ Error: Could not parse rating from: %s" ratingStr
        exit 1

// Validate rating range
if rating < 1.0 || rating > 5.0 then
    printfn "❌ Error: Rating must be between 1.0 and 5.0, got: %f" rating
    exit 1

// Validate content
if String.IsNullOrWhiteSpace(content) then
    printfn "❌ Error: Review content is required"
    exit 1

if content.Replace(" ", "").Replace("\n", "").Replace("\t", "").Length < 10 then
    printfn "❌ Error: Review content must have at least 10 non-whitespace characters"
    exit 1

// Slug generation and sanitization
let sanitizeSlug (slug: string) =
    slug.ToLowerInvariant()
        .Replace(" ", "-")
        .Replace("_", "-")
    |> fun s -> Regex.Replace(s, @"[^a-z0-9\-]", "")
    |> fun s -> Regex.Replace(s, @"-+", "-")
    |> fun s -> s.Trim('-')

let generateSlug (title: string) =
    let baseSlug = sanitizeSlug title
    let timestamp = DateTimeOffset.Now.ToString("yyyy-MM-dd")
    sprintf "%s-%s" baseSlug timestamp

let slug = 
    match customSlug with
    | Some s when not (String.IsNullOrWhiteSpace(s)) -> sanitizeSlug s
    | _ -> generateSlug itemName

// Process tags
let tags = 
    match tagsInput with
    | Some t when not (String.IsNullOrWhiteSpace(t)) ->
        t.Split(',') 
        |> Array.map (fun tag -> tag.Trim().ToLowerInvariant())
        |> Array.filter (fun tag -> not (String.IsNullOrWhiteSpace(tag)))
    | _ -> [||]

// Process pros and cons
let pros = 
    match prosInput with
    | Some p -> 
        p.Split('\n') 
        |> Array.map (fun line -> line.Trim())
        |> Array.filter (fun line -> not (String.IsNullOrWhiteSpace(line)))
    | None -> [||]

let cons = 
    match consInput with
    | Some c -> 
        c.Split('\n') 
        |> Array.map (fun line -> line.Trim())
        |> Array.filter (fun line -> not (String.IsNullOrWhiteSpace(line)))
    | None -> [||]

// Parse additional fields from JSON
let additionalFields = 
    match additionalFieldsJson with
    | Some json when not (String.IsNullOrWhiteSpace(json)) ->
        try
            let doc = JsonDocument.Parse(json)
            let mutable fields = []
            for prop in doc.RootElement.EnumerateObject() do
                if not (String.IsNullOrWhiteSpace(prop.Value.GetString())) then
                    fields <- (prop.Name, prop.Value.GetString()) :: fields
            fields |> List.rev
        with
        | ex -> 
            printfn "⚠️ Warning: Could not parse additional fields JSON: %s" ex.Message
            []
    | _ -> []

// Generate current timestamp in EST
let currentTime = DateTimeOffset.Now.ToString("MM/dd/yyyy HH:mm zzz")

// Generate frontmatter
let frontmatter = 
    let baseFields = [
        sprintf "title: \"%s Review\"" itemName
        sprintf "post_type: \"review\""
        sprintf "published_date: \"%s\"" currentTime
    ]
    
    let tagsField = 
        if tags.Length > 0 then
            let tagsList = tags |> Array.map (sprintf "\"%s\"") |> String.concat ", "
            [sprintf "tags: [%s]" tagsList]
        else
            ["tags: []"]
    
    String.concat "\n" (baseFields @ tagsField)

// Generate review block YAML
let generateReviewBlock () =
    let baseFields = [
        sprintf "item: \"%s\"" (itemName.Replace("\"", "\\\""))
        sprintf "itemType: \"%s\"" reviewType
        sprintf "rating: %.1f" rating
        "scale: 5.0"
    ]
    
    let summaryField = 
        if not (String.IsNullOrWhiteSpace(summary)) then
            [sprintf "summary: \"%s\"" (summary.Replace("\"", "\\\"").Replace("\n", "\\n"))]
        else
            []
    
    let prosField = 
        if pros.Length > 0 then
            let prosList = pros |> Array.map (fun p -> sprintf "  - \"%s\"" (p.Replace("\"", "\\\""))) |> String.concat "\n"
            ["pros:"; prosList]
        else
            []
    
    let consField = 
        if cons.Length > 0 then
            let consList = cons |> Array.map (fun c -> sprintf "  - \"%s\"" (c.Replace("\"", "\\\""))) |> String.concat "\n"
            ["cons:"; consList]
        else
            []
    
    let urlField = 
        match itemUrl with
        | Some url -> [sprintf "itemUrl: \"%s\"" url]
        | None -> []
    
    let imageField = 
        match imageUrl with
        | Some url -> [sprintf "imageUrl: \"%s\"" url]
        | None -> []
    
    let additionalFieldsYaml = 
        if additionalFields.Length > 0 then
            let fieldsYaml = 
                additionalFields 
                |> List.map (fun (key, value) -> sprintf "  %s: \"%s\"" key (value.Replace("\"", "\\\"")))
                |> String.concat "\n"
            ["additionalFields:"; fieldsYaml]
        else
            []
    
    let allFields = baseFields @ summaryField @ prosField @ consField @ urlField @ imageField @ additionalFieldsYaml
    String.concat "\n" allFields

// Generate the complete file content
let fileContent = sprintf """---%s
---

# %s Review

:::review
%s
:::

%s""" frontmatter itemName (generateReviewBlock()) content

// Determine output directory and filename
let outputDir = Path.Join(Directory.GetCurrentDirectory(), "_src", "reviews")
let filename = sprintf "%s.md" slug
let fullPath = Path.Join(outputDir, filename)

// Ensure directory exists
if not (Directory.Exists(outputDir)) then
    Directory.CreateDirectory(outputDir) |> ignore

// Check if file already exists
if File.Exists(fullPath) then
    printfn "❌ Error: File already exists: %s" fullPath
    printfn "Try using a different slug or the file may have been created already."
    exit 1

// Write the file
try
    File.WriteAllText(fullPath, fileContent)
    
    // Success output
    printfn "✅ %s review created successfully!" (reviewType.Substring(0, 1).ToUpper() + reviewType.Substring(1))
    printfn "📁 File: _src/reviews/%s" filename
    printfn "📖 Item: %s" itemName
    printfn "⭐ Rating: %.1f/5.0" rating
    printfn "🏷️ Tags: %s" (if tags.Length > 0 then String.concat ", " tags else "none")
    printfn "🔗 URL: %s" (match itemUrl with Some url -> url | None -> "none")
    
    if pros.Length > 0 then
        printfn "👍 Pros: %d items" pros.Length
    if cons.Length > 0 then
        printfn "👎 Cons: %d items" cons.Length
    if additionalFields.Length > 0 then
        printfn "📊 Additional fields: %d items" additionalFields.Length

with
| ex -> 
    printfn "❌ Error writing file: %s" ex.Message
    exit 1
```

### 4. Content Generation Rules

#### File Structure
- **Target Directory**: `_src/reviews/`
- **File Pattern**: `{slug}.md` (e.g., `four-agreements-review-2025-01-15.md`)
- **Content Format**: Frontmatter + Review Block + Detailed Content

#### Frontmatter Standards
```yaml
---
title: "{Item Name} Review"
post_type: "review"
published_date: "MM/dd/yyyy HH:mm zzz"
tags: ["tag1", "tag2", "tag3"]
---
```

#### Review Block Format
```yaml
:::review
item: "Item Name"
itemType: "book|movie|music|business|product"
rating: 4.5
scale: 5.0
summary: "Brief review summary"
pros:
  - "Positive aspect 1"
  - "Positive aspect 2"
cons:
  - "Improvement area 1"
  - "Improvement area 2"
itemUrl: "https://example.com/item"
imageUrl: "https://example.com/image.jpg"
additionalFields:
  author: "Author Name"
  genre: "Genre"
  year: "2024"
:::
```

#### Type-Specific Additional Fields

**Books**:
- `author`: Author name(s)
- `isbn`: ISBN number
- `genre`: Book genre/category
- `pages`: Page count (optional)

**Movies**:
- `director`: Director name(s)
- `year`: Release year
- `genre`: Movie genre(s)
- `runtime`: Duration (optional)
- `imdb_rating`: IMDB score (optional)

**Music**:
- `artist`: Artist/band name
- `music_type`: Album, Single, EP, etc.
- `release_year`: Release year
- `genre`: Musical genre
- `label`: Record label
- `tracks`: Number of tracks (optional)

**Business**:
- `business_type`: Restaurant, Coffee Shop, etc.
- `location`: City, state/region
- `price_range`: $, $$, $$$, etc.
- `yelp_rating`: Yelp score (optional)
- `phone`: Contact number (optional)

**Product**:
- `manufacturer`: Brand/company name
- `product_category`: Electronics, Software, etc.
- `model_version`: Specific model/version
- `price`: Current/purchase price
- `amazon_rating`: Amazon score (optional)

### 5. Validation and Error Handling

#### Content Validation
- **Item Name**: Required, 1-100 characters
- **Rating**: Required, must be 1.0-5.0 with 0.5 increments
- **Review Type**: Must be one of: book, movie, music, business, product
- **Content**: Required, minimum 10 non-whitespace characters
- **URLs**: Must be valid HTTP/HTTPS if provided

#### Error Scenarios
- **Invalid rating range** → Comment with valid range, keep issue open
- **Missing required fields** → Comment with missing fields list, keep issue open
- **Duplicate review** → Comment with existing review link, keep issue open
- **Processing failure** → Comment with error details, label for manual review

#### Success Validation
- File created in correct directory structure
- Frontmatter format matches standards
- Review block YAML is valid
- All provided data preserved correctly
- Pull request created with proper metadata

### 6. Security Considerations

#### Input Sanitization
- **XSS Prevention**: All user input escaped in YAML and HTML output
- **Path Traversal**: Slug generation restricted to safe characters
- **File System**: Output path validation prevents directory traversal
- **YAML Injection**: Content properly escaped for YAML format

#### Access Control
- **User Restriction**: Only authorized users (@lqdev) can trigger workflows
- **Label Validation**: Workflows only trigger on correct issue labels
- **Content Review**: All content goes through PR review before publication

#### Data Validation
- **Input Length**: Reasonable limits on all text fields
- **URL Validation**: External URLs validated for proper format
- **JSON Parsing**: Safe parsing of additional fields with error handling

## Implementation Plan

### Phase 1: Core Review Templates ✅ (Week 1)
- [x] Create GitHub Issue Form templates for all 5 review types
- [x] Design comprehensive form validation and user experience
- [x] Document field specifications and validation rules

### Phase 2: GitHub Action Integration (Week 2)
- [ ] Extend existing workflow with review processing job
- [ ] Implement type detection and data extraction logic
- [ ] Add review-specific error handling and validation
- [ ] Test with all review types to ensure reliable processing

### Phase 3: F# Processing Script (Week 3)
- [ ] Create `Scripts/process-review-issue.fsx` following established patterns
- [ ] Implement type-specific field handling and validation
- [ ] Add proper error handling and user-friendly output
- [ ] Test script with comprehensive validation scenarios

### Phase 4: Integration Testing (Week 4)
- [ ] Test complete workflow with all review types
- [ ] Validate generated content format and structure
- [ ] Ensure proper frontmatter and review block generation
- [ ] Verify pull request creation and issue closure

### Phase 5: Documentation & Launch (Week 5)
- [ ] Create user guide for review submission process
- [ ] Document troubleshooting and common issues
- [ ] Update repository documentation with new capabilities
- [ ] Announce new review publishing workflow

## Success Criteria

### Functional Requirements ✅
- [x] **Forms Support All Review Types**: Book, Movie, Music, Business, Product templates created
- [x] **Structured Data Collection**: Comprehensive field validation and type-specific metadata
- [x] **Integration with Existing System**: Follows proven Issue → Action → Script → PR pattern

### Technical Requirements
- [ ] **Reliable Processing**: 99%+ success rate for valid submissions
- [ ] **Error Handling**: Clear error messages and recovery instructions
- [ ] **Content Quality**: Generated files match existing review format standards
- [ ] **Security**: Safe handling of user input with proper validation

### User Experience Requirements
- [ ] **Intuitive Forms**: Easy-to-use issue templates with helpful guidance
- [ ] **Fast Processing**: Reviews processed and PR created within 2 minutes
- [ ] **Clear Feedback**: Users receive immediate confirmation and status updates
- [ ] **Consistent Output**: All review types follow same formatting standards

## Benefits Over Manual Process

### Efficiency Gains
- **Reduced Creation Time**: 5-10 minutes vs 20-30 minutes manual
- **Consistent Format**: Automatic frontmatter and review block generation
- **Type Safety**: Form validation prevents common formatting errors
- **Automated Workflow**: No manual file creation or PR setup required

### Quality Improvements
- **Structured Metadata**: Consistent review data across all types
- **Validation**: Required fields and format checking prevent incomplete reviews
- **Review Process**: All content goes through PR review before publication
- **Documentation**: Built-in examples and guidance in issue forms

### Scalability Benefits
- **Multiple Contributors**: Easy for authorized users to submit reviews
- **Type Extensibility**: Framework supports future review types easily
- **Maintenance**: Centralized processing logic easier to update and maintain
- **Analytics**: Structured data enables better content analysis and reporting

This specification provides a comprehensive foundation for implementing GitHub Issue Forms and Actions workflow for review content, leveraging the existing proven architecture while extending it for the rich metadata and validation requirements of review content.