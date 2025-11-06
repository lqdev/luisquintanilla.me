/**
 * Enhanced Content Discovery - Client-Side Search
 * Uses Fuse.js for fuzzy search with accessibility compliance
 */

// Make this a module
export {};

// Minimal type definitions for Fuse.js (loaded from CDN)
interface FuseOptions<T> {
    includeScore?: boolean;
    includeMatches?: boolean;
    threshold?: number;
    minMatchCharLength?: number;
    maxPatternLength?: number;
    keys?: Array<{ name: keyof T; weight: number } | keyof T>;
}

interface FuseMatch {
    indices: [number, number][];
    key: string;
    value?: string;
}

interface FuseResult<T> {
    item: T;
    score?: number;
    matches?: FuseMatch[];
}

declare class Fuse<T> {
    constructor(list: T[], options?: FuseOptions<T>);
    search(pattern: string): FuseResult<T>[];
}

// Search item interface
interface SearchItem {
    title: string;
    url: string;
    date: string;
    contentType: string;
    keywords: string[];
    tags: string[];
    summary: string;
}

class SearchManager {
    private searchIndex: SearchItem[] | null = null;
    private tagIndex: any[] | null = null;
    private fuse: Fuse<SearchItem> | null = null;
    private currentQuery: string = '';
    private activeFilters: Set<string> = new Set(['all']);
    
    // Search configuration based on research
    private fuseOptions: FuseOptions<SearchItem> = {
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
    
    constructor() {
        this.init();
    }
    
    private async init(): Promise<void> {
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
    
    private async loadSearchIndexes(): Promise<void> {
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
            
            console.log(`Search index loaded: ${this.searchIndex?.length || 0} items`);
            console.log(`Tag index loaded: ${this.tagIndex?.length || 0} tags`);
            
        } catch (error) {
            const errorMessage = error instanceof Error ? error.message : 'Unknown error';
            throw new Error(`Failed to load search indexes: ${errorMessage}`);
        }
    }
    
    private initializeFuse(): void {
        if (!this.searchIndex) return;
        if (typeof Fuse === 'undefined') {
            throw new Error('Fuse.js library not loaded');
        }
        this.fuse = new Fuse(this.searchIndex, this.fuseOptions);
    }
    
    private setupEventListeners(): void {
        const searchInput = document.getElementById('search-input') as HTMLInputElement | null;
        const searchClear = document.getElementById('search-clear');
        const filterContainer = document.getElementById('search-filters');
        
        if (!searchInput || !searchClear || !filterContainer) return;
        
        // Search input with debouncing
        let searchTimeout: number;
        searchInput.addEventListener('input', (e) => {
            clearTimeout(searchTimeout);
            searchTimeout = window.setTimeout(() => {
                this.handleSearch((e.target as HTMLInputElement).value.trim());
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
            const target = e.target as HTMLInputElement;
            if (target.type === 'checkbox') {
                this.handleFilterChange(target);
            }
        });
        
        // Keyboard navigation
        searchInput.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') {
                searchInput.blur();
            }
        });
    }
    
    private setupUrlSearchParams(): void {
        const urlParams = new URLSearchParams(window.location.search);
        const query = urlParams.get('q');
        
        if (query) {
            const searchInput = document.getElementById('search-input') as HTMLInputElement | null;
            if (searchInput) {
                searchInput.value = query;
                this.handleSearch(query);
            }
        }
    }
    
    private handleSearch(query: string): void {
        this.currentQuery = query;
        
        // Update URL without refresh
        const url = new URL(window.location.href);
        if (query) {
            url.searchParams.set('q', query);
        } else {
            url.searchParams.delete('q');
        }
        window.history.replaceState({}, '', url);
        
        // Show/hide clear button
        const searchClear = document.getElementById('search-clear');
        if (searchClear) {
            searchClear.style.display = query ? 'block' : 'none';
        }
        
        if (!query) {
            this.clearResults();
            return;
        }
        
        this.performSearch(query);
    }
    
    private handleFilterChange(checkbox: HTMLInputElement): void {
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
    
    private checkFilter(value: string): void {
        const checkbox = document.querySelector<HTMLInputElement>(`input[value="${value}"]`);
        if (checkbox) checkbox.checked = true;
    }
    
    private uncheckFilter(value: string): void {
        const checkbox = document.querySelector<HTMLInputElement>(`input[value="${value}"]`);
        if (checkbox) checkbox.checked = false;
    }
    
    private uncheckOtherFilters(except: string): void {
        const checkboxes = document.querySelectorAll<HTMLInputElement>('#search-filters input[type="checkbox"]');
        checkboxes.forEach(cb => {
            if (cb.value !== except) {
                cb.checked = false;
            }
        });
    }
    
    private performSearch(query: string): void {
        if (!this.fuse) return;
        
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
    
    private displayResults(results: FuseResult<SearchItem>[], query: string, searchTime: string): void {
        const resultsContainer = document.getElementById('results-list');
        const statsContainer = document.getElementById('search-stats');
        const noResultsContainer = document.getElementById('no-results');
        const filtersContainer = document.getElementById('search-filters');
        
        if (!resultsContainer || !statsContainer || !noResultsContainer || !filtersContainer) return;
        
        // Show filters and stats
        filtersContainer.style.display = 'block';
        statsContainer.style.display = 'block';
        
        // Update stats
        const resultsCount = document.getElementById('results-count');
        const searchTimeSpan = document.getElementById('search-time');
        if (resultsCount) resultsCount.textContent = results.length.toString();
        if (searchTimeSpan) searchTimeSpan.textContent = `in ${searchTime}s`;
        
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
    
    private renderSearchResult(result: FuseResult<SearchItem>, _query: string): string {
        const item = result.item;
        const score = (1 - (result.score || 0)).toFixed(2);
        
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
    
    private highlightMatches(text: string, matches: FuseMatch[] | undefined, field: string): string {
        if (!matches || !text) return text;
        
        const fieldMatches = matches.filter(match => match.key === field);
        if (fieldMatches.length === 0) return text;
        
        let highlightedText = text;
        const ranges: [number, number][] = [];
        
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
    
    private clearResults(): void {
        const resultsContainer = document.getElementById('results-list');
        const statsContainer = document.getElementById('search-stats');
        const noResultsContainer = document.getElementById('no-results');
        const filtersContainer = document.getElementById('search-filters');
        
        if (resultsContainer) resultsContainer.innerHTML = '';
        if (statsContainer) statsContainer.style.display = 'none';
        if (noResultsContainer) noResultsContainer.style.display = 'none';
        if (filtersContainer) filtersContainer.style.display = 'none';
    }
    
    private showLoading(show: boolean): void {
        const loading = document.getElementById('loading');
        if (loading) {
            loading.style.display = show ? 'block' : 'none';
        }
    }
    
    private showError(message: string): void {
        const resultsContainer = document.getElementById('search-results');
        if (resultsContainer) {
            resultsContainer.innerHTML = `
                <div class="search-error">
                    <h2>Search Error</h2>
                    <p>${message}</p>
                </div>
            `;
        }
    }
}

// Extend Window interface
declare global {
    interface Window {
        SearchManager?: typeof SearchManager;
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
