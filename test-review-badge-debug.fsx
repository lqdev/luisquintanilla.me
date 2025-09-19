#load "Views/LayoutViews.fs"

open LayoutViews

// Test the extractReviewItemType function with actual content
let sampleReviewContent = """
<div class="custom-review-block h-review">
    <div class="review-header">
        <h3 class="review-title p-name">The Serviceberry</h3>
        <span class="item-type-badge badge bg-secondary">BOOK</span>
    </div>
    <div class="review-image">
        <img src="https://example.com/image.jpg" alt="The Serviceberry" class="review-thumbnail img-fluid" />
    </div>
    <div class="review-rating p-rating">
        <strong>Rating:</strong> ★★★★ (4.8/5.0)
    </div>
</div>
"""

// Test the function
let result = LayoutViews.extractReviewItemType sampleReviewContent

printfn "Extracted review item type: %A" result

// Test with different item types
let movieContent = """<span class="item-type-badge badge bg-secondary">MOVIE</span>"""
let musicContent = """<span class="item-type-badge badge bg-secondary">MUSIC</span>"""
let businessContent = """<span class="item-type-badge badge bg-secondary">BUSINESS</span>"""

printfn "Movie: %A" (LayoutViews.extractReviewItemType movieContent)
printfn "Music: %A" (LayoutViews.extractReviewItemType musicContent)
printfn "Business: %A" (LayoutViews.extractReviewItemType businessContent)