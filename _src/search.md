# Search

Search across all content on this site, including posts, notes, responses, bookmarks, and more.

<link rel="stylesheet" href="/assets/css/search.css">

<div id="search-container">
    <div class="search-form">
        <label for="search-input" class="visually-hidden">Search content</label>
        <input type="search" id="search-input" placeholder="Search posts, notes, bookmarks, and more..." autocomplete="off" spellcheck="false" aria-describedby="search-help">
        <button type="button" id="search-clear" class="search-clear" aria-label="Clear search" style="display: none;">
            <span aria-hidden="true">×</span>
        </button>
    </div>
    <div id="search-help" class="search-help">
        <p>Search tips: Use quotes for exact phrases, try different terms, or browse by content type.</p>
    </div>
    <div class="search-filters" id="search-filters" style="display: none;">
        <fieldset>
            <legend class="visually-hidden">Filter by content type</legend>
            <div class="filter-options">
                <label><input type="checkbox" value="all" checked> All content</label>
                <label><input type="checkbox" value="posts"> Posts</label>
                <label><input type="checkbox" value="notes"> Notes</label>
                <label><input type="checkbox" value="responses"> Responses</label>
                <label><input type="checkbox" value="bookmarks"> Bookmarks</label>
                <label><input type="checkbox" value="wiki"> Wiki</label>
                <label><input type="checkbox" value="reviews"> Reviews</label>
            </div>
        </fieldset>
    </div>
    <div id="search-stats" class="search-stats" style="display: none;">
        <p><span id="results-count">0</span> results found <span id="search-time"></span></p>
    </div>
    <div id="search-results" class="search-results">
        <div id="loading" class="search-loading" style="display: none;">
            <p>Loading search index...</p>
        </div>
        <div id="no-results" class="search-no-results" style="display: none;">
            <h2>No results found</h2>
            <p>Try different keywords, check spelling, or browse content by:</p>
            <ul>
                <li><a href="/posts/">All Posts</a></li>
                <li><a href="/notes/">All Notes</a></li>
                <li><a href="/responses/">All Responses</a></li>
                <li><a href="/bookmarks/">All Bookmarks</a></li>
                <li><a href="/tags/">Browse by Tags</a></li>
            </ul>
        </div>
        <div id="results-list" class="results-list">
            <!-- Search results will be populated here -->
        </div>
    </div>
</div>

<script src="/assets/js/fuse.min.js"></script>
<script src="/assets/js/search.js"></script>
