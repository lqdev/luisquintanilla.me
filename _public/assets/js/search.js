/**
 * Enhanced Content Discovery - Client-Side Search
 * Uses Fuse.js for fuzzy search with accessibility compliance
 */

class SearchManager {
    constructor() {
        this.searchIndex = null;
        this.tagIndex = null;
        this.fuse = null;
        this.currentQuery = '';
        this.activeFilters = new Set(['all']);
        
        // Search configuration based on research
        this.fuseOptions = {
            // Performance optimized
            includeScore: true,
            includeMatches: true,
            threshold: 0.4, // Balance between fuzzy and accuracy
            minMatchCharLength: 2,
            maxPatternLength: 100,
            
            // Weighted search fields
            keys: [
                { name: 'title', weight: 0.4 },
                { name: 'keywords', weight: 0.3 },
                { name: 'tags', weight: 0.2 },
                { name: 'summary', weight: 0.1 }
            ]
        };
        
        this.init();
    }
    
    async init() {
        this.setupEventListeners();
        this.showLoading(true);
        
        try {
            await this.loadSearchIndexes();
            this.initializeFuse();
            this.setupUrlSearchParams();
            this.showLoading(false);
        } catch (error) {
            console.error('Search initialization failed:', error);
            this.showError('Failed to load search. Please refresh the page.');
        }
    }
    
    async loadSearchIndexes() {
        try {
            const [contentResponse, tagResponse] = await Promise.all([
                fetch('/search/index.json'),
                fetch('/search/tags.json')
            ]);
            
            if (!contentResponse.ok || !tagResponse.ok) {
                throw new Error('Failed to fetch search indexes');
            }
            
            this.searchIndex = await contentResponse.json();
            this.tagIndex = await tagResponse.json();
            
            console.log(`Search index loaded: ${this.searchIndex.length} items`);
            console.log(`Tag index loaded: ${this.tagIndex.length} tags`);
            
        } catch (error) {
            throw new Error(`Failed to load search indexes: ${error.message}`);
        }
    }
    
    initializeFuse() {
        this.fuse = new Fuse(this.searchIndex, this.fuseOptions);
    }
    
    setupEventListeners() {
        const searchInput = document.getElementById('search-input');
        const searchClear = document.getElementById('search-clear');
        const filterContainer = document.getElementById('search-filters');
        
        // Search input with debouncing
        let searchTimeout;
        searchInput.addEventListener('input', (e) => {
            clearTimeout(searchTimeout);
            searchTimeout = setTimeout(() => {
                this.handleSearch(e.target.value.trim());
            }, 300);
        });
        
        // Clear button
        searchClear.addEventListener('click', () => {
            searchInput.value = '';
            searchInput.focus();
            this.handleSearch('');
        });
        
        // Filter checkboxes
        filterContainer.addEventListener('change', (e) => {
            if (e.target.type === 'checkbox') {
                this.handleFilterChange(e.target);
            }
        });
        
        // Keyboard navigation
        searchInput.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') {
                searchInput.blur();
            }
        });
    }
    
    setupUrlSearchParams() {
        const urlParams = new URLSearchParams(window.location.search);
        const query = urlParams.get('q');
        
        if (query) {
            const searchInput = document.getElementById('search-input');
            searchInput.value = query;
            this.handleSearch(query);
        }
    }
    
    handleSearch(query) {
        this.currentQuery = query;
        
        // Update URL without refresh
        const url = new URL(window.location);
        if (query) {
            url.searchParams.set('q', query);
        } else {
            url.searchParams.delete('q');
        }
        window.history.replaceState({}, '', url);
        
        // Show/hide clear button
        const searchClear = document.getElementById('search-clear');
        searchClear.style.display = query ? 'block' : 'none';
        
        if (!query) {
            this.clearResults();
            return;
        }
        
        this.performSearch(query);
    }
    
    handleFilterChange(checkbox) {
        const value = checkbox.value;
        
        if (value === 'all') {
            if (checkbox.checked) {
                // Check all, uncheck others
                this.activeFilters.clear();
                this.activeFilters.add('all');
                this.uncheckOtherFilters('all');
            }
        } else {
            if (checkbox.checked) {
                // Uncheck 'all', add specific filter
                this.activeFilters.delete('all');
                this.activeFilters.add(value);
                this.uncheckFilter('all');
            } else {
                // Remove specific filter
                this.activeFilters.delete(value);
                // If no specific filters, check 'all'
                if (this.activeFilters.size === 0) {
                    this.activeFilters.add('all');
                    this.checkFilter('all');
                }
            }
        }
        
        // Re-run search with current query if exists
        if (this.currentQuery) {
            this.performSearch(this.currentQuery);
        }
    }
    
    checkFilter(value) {
        const checkbox = document.querySelector(`input[value="${value}"]`);
        if (checkbox) checkbox.checked = true;
    }
    
    uncheckFilter(value) {
        const checkbox = document.querySelector(`input[value="${value}"]`);
        if (checkbox) checkbox.checked = false;
    }
    
    uncheckOtherFilters(except) {
        const checkboxes = document.querySelectorAll('#search-filters input[type="checkbox"]');
        checkboxes.forEach(cb => {
            if (cb.value !== except) {
                cb.checked = false;
            }
        });
    }
    
    performSearch(query) {
        const startTime = performance.now();
        
        // Perform fuzzy search
        let results = this.fuse.search(query);
        
        // Apply content type filters
        if (!this.activeFilters.has('all')) {
            results = results.filter(result => 
                this.activeFilters.has(result.item.contentType)
            );
        }
        
        const endTime = performance.now();
        const searchTime = ((endTime - startTime) / 1000).toFixed(3);
        
        this.displayResults(results, query, searchTime);
    }
    
    displayResults(results, query, searchTime) {
        const resultsContainer = document.getElementById('results-list');
        const statsContainer = document.getElementById('search-stats');
        const noResultsContainer = document.getElementById('no-results');
        const filtersContainer = document.getElementById('search-filters');
        
        // Show filters and stats
        filtersContainer.style.display = 'block';
        statsContainer.style.display = 'block';
        
        // Update stats
        const resultsCount = document.getElementById('results-count');
        const searchTimeSpan = document.getElementById('search-time');
        resultsCount.textContent = results.length;
        searchTimeSpan.textContent = `in ${searchTime}s`;
        
        if (results.length === 0) {
            resultsContainer.innerHTML = '';
            noResultsContainer.style.display = 'block';
            return;
        }
        
        noResultsContainer.style.display = 'none';
        
        // Render results
        const resultsHtml = results.slice(0, 50).map(result => 
            this.renderSearchResult(result, query)
        ).join('');
        
        resultsContainer.innerHTML = resultsHtml;
    }
    
    renderSearchResult(result, query) {
        const item = result.item;
        const score = (1 - result.score).toFixed(2);
        
        // Highlight matches in title and summary
        const highlightedTitle = this.highlightMatches(item.title, result.matches, 'title');
        const highlightedSummary = this.highlightMatches(item.summary, result.matches, 'summary');
        
        // Format date
        const date = new Date(item.date);
        const formattedDate = date.toLocaleDateString('en-US', {
            year: 'numeric',
            month: 'short',
            day: 'numeric'
        });
        
        // Tags (limit to first 5)
        const tagsHtml = item.tags.slice(0, 5).map(tag => 
            `<span class="tag">#${tag}</span>`
        ).join(' ');
        
        return `
            <article class="search-result" data-score="${score}">
                <header class="search-result-header">
                    <h2 class="search-result-title">
                        <a href="${item.url}">${highlightedTitle}</a>
                    </h2>
                    <div class="search-result-meta">
                        <time datetime="${item.date}">${formattedDate}</time>
                        <span class="search-score" title="Relevance score">Score: ${score}</span>
                    </div>
                </header>
                
                <div class="search-result-summary">
                    <p>${highlightedSummary}</p>
                </div>
                
                ${tagsHtml ? `<div class="search-result-tags">${tagsHtml}</div>` : ''}
            </article>
        `;
    }
    
    highlightMatches(text, matches, field) {
        if (!matches || !text) return text;
        
        const fieldMatches = matches.filter(match => match.key === field);
        if (fieldMatches.length === 0) return text;
        
        let highlightedText = text;
        const ranges = [];
        
        fieldMatches.forEach(match => {
            match.indices.forEach(([start, end]) => {
                ranges.push([start, end]);
            });
        });
        
        // Sort ranges by start position (descending) to avoid index shifting
        ranges.sort((a, b) => b[0] - a[0]);
        
        ranges.forEach(([start, end]) => {
            const before = highlightedText.substring(0, start);
            const highlighted = highlightedText.substring(start, end + 1);
            const after = highlightedText.substring(end + 1);
            highlightedText = before + `<mark>${highlighted}</mark>` + after;
        });
        
        return highlightedText;
    }
    
    clearResults() {
        document.getElementById('results-list').innerHTML = '';
        document.getElementById('search-stats').style.display = 'none';
        document.getElementById('no-results').style.display = 'none';
        document.getElementById('search-filters').style.display = 'none';
    }
    
    showLoading(show) {
        const loading = document.getElementById('loading');
        loading.style.display = show ? 'block' : 'none';
    }
    
    showError(message) {
        const resultsContainer = document.getElementById('search-results');
        resultsContainer.innerHTML = `
            <div class="search-error">
                <h2>Search Error</h2>
                <p>${message}</p>
            </div>
        `;
    }
}

// Initialize search when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    // Only initialize on search page
    if (document.getElementById('search-container')) {
        new SearchManager();
    }
});

// Export for potential external use
window.SearchManager = SearchManager;
