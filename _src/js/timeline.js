// Timeline Content System - Desert Theme with Stratified Progressive Loading
// Optimized for feed-as-homepage with type-aware chunked loading

// DEBUG - Immediate execution logging
console.log('🚀 Timeline.js loading - stratified version');

const TimelineFilter = {
    // Initialize filtering system
    init() {
        this.setupFilterButtons();
        this.restoreFilterState();
        this.setupKeyboardNavigation();
    },

    // Setup filter button event listeners
    setupFilterButtons() {
        const filterButtons = document.querySelectorAll('.filter-btn');
        
        filterButtons.forEach(button => {
            button.addEventListener('click', (e) => {
                e.preventDefault();
                const filterType = button.getAttribute('data-filter');
                this.filterContent(filterType);
                this.updateActiveButton(button);
                this.saveFilterState(filterType);
            });
        });
    },

    // Main content filtering function
    filterContent(contentType) {
        // Get all content cards (both initial and progressively loaded)
        const cards = document.querySelectorAll('.content-card');
        let visibleCount = 0;

        cards.forEach(card => {
            const cardType = card.getAttribute('data-type');
            
            // Special handling for response types
            let shouldShow = false;
            if (contentType === 'all') {
                shouldShow = true;
            } else if (contentType === 'responses') {
                // Show all response subtypes for "responses" filter
                shouldShow = ['star', 'reply', 'reshare', 'rsvp', 'responses'].includes(cardType);
            } else {
                // Exact match for other content types
                shouldShow = (contentType === cardType);
            }
            
            if (shouldShow) {
                // Show card with smooth transition
                card.classList.remove('filtered-out');
                card.classList.add('filtered-in');
                card.style.display = 'block';
                
                // Smooth fade in
                setTimeout(() => {
                    card.style.opacity = '1';
                    card.style.transform = 'translateY(0)';
                }, 10);
                
                visibleCount++;
            } else {
                // Hide card with smooth transition
                card.classList.remove('filtered-in');
                card.classList.add('filtered-out');
                card.style.opacity = '0';
                card.style.transform = 'translateY(-10px)';
                
                // Hide after transition
                setTimeout(() => {
                    card.style.display = 'none';
                }, 300);
            }
        });

        // Dispatch filter change event for progressive loader
        document.dispatchEvent(new CustomEvent('filterChanged', {
            detail: { filterType: contentType, visibleCount: visibleCount }
        }));
    },

    // Update active filter button
    updateActiveButton(activeButton) {
        // Remove active class from all buttons
        document.querySelectorAll('.filter-btn').forEach(btn => {
            btn.classList.remove('active');
            btn.setAttribute('aria-pressed', 'false');
        });
        
        // Add active class to clicked button
        activeButton.classList.add('active');
        activeButton.setAttribute('aria-pressed', 'true');
    },

    // Save filter state to localStorage
    saveFilterState(filterType) {
        try {
            localStorage.setItem('timelineFilter', filterType);
        } catch (e) {
            console.log('localStorage not available for filter state');
        }
    },

    // Restore filter state from localStorage
    restoreFilterState() {
        try {
            const savedFilter = localStorage.getItem('timelineFilter');
            if (savedFilter) {
                const button = document.querySelector(`[data-filter="${savedFilter}"]`);
                if (button) {
                    this.filterContent(savedFilter);
                    this.updateActiveButton(button);
                    return;
                }
            }
        } catch (e) {
            console.log('localStorage not available for filter restore');
        }
        
        // Default to 'all' if no saved state
        const allButton = document.querySelector('[data-filter="all"]');
        if (allButton) {
            this.updateActiveButton(allButton);
        }
    },

    // Keyboard navigation support
    setupKeyboardNavigation() {
        document.addEventListener('keydown', (e) => {
            // Alt + number keys for quick filtering
            if (e.altKey && e.key >= '1' && e.key <= '9') {
                e.preventDefault();
                const buttons = document.querySelectorAll('.filter-btn');
                const index = parseInt(e.key) - 1;
                if (buttons[index]) {
                    buttons[index].click();
                    buttons[index].focus();
                }
            }
        });
    },

    // Get current filter state
    getCurrentFilter() {
        const activeButton = document.querySelector('.filter-btn.active');
        return activeButton ? activeButton.getAttribute('data-filter') : 'all';
    }
};

// Theme Manager
const TimelineThemeManager = {
    init() {
        this.initializeTheme();
        this.setupThemeToggle();
    },

    initializeTheme() {
        const savedTheme = localStorage.getItem('theme');
        const systemPreference = window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
        const theme = savedTheme || systemPreference;
        
        document.documentElement.setAttribute('data-theme', theme);
        document.body.setAttribute('data-theme', theme);
        this.updateThemeToggleIcon(theme);
    },

    setupThemeToggle() {
        const themeToggle = document.getElementById('theme-toggle');
        if (themeToggle) {
            themeToggle.addEventListener('click', () => {
                this.toggleTheme();
            });
        }
    },

    toggleTheme() {
        const current = document.documentElement.getAttribute('data-theme') || 'light';
        const newTheme = current === 'dark' ? 'light' : 'dark';
        
        document.body.style.transition = 'background-color 0.3s ease, color 0.3s ease';
        document.documentElement.setAttribute('data-theme', newTheme);
        document.body.setAttribute('data-theme', newTheme);
        
        this.updateThemeToggleIcon(newTheme);
        localStorage.setItem('theme', newTheme);
        
        setTimeout(() => {
            document.body.style.transition = '';
        }, 300);
    },

    updateThemeToggleIcon(theme) {
        const themeToggle = document.getElementById('theme-toggle-icon');
        if (themeToggle) {
            const icon = theme === 'dark' ? '🌙' : '☀️';
            themeToggle.innerHTML = icon;
            
            const themeButton = document.getElementById('theme-toggle');
            if (themeButton) {
                themeButton.setAttribute('aria-label', `Switch to ${theme === 'dark' ? 'light' : 'dark'} theme`);
            }
        }
    }
};

// Progressive Loading Manager - Stratified Content Support
const TimelineProgressiveLoader = {
    // Configuration
    config: {
        chunkSize: 10,
        loadThreshold: 0.8
    },
    
    // State
    isLoading: false,
    observer: null,
    remainingContentByType: new Map(),
    loadedCountByType: new Map(),
    currentFilter: 'all',
    
    init() {
        console.log('🔄 Initializing Progressive Loader for Stratified Timeline...');
        this.loadRemainingContentByType();
        this.setupIntersectionObserver();
        this.setupLoadMoreButton();
        this.setupFilterListener();
        
        // Sync with current filter state from TimelineFilter to handle cached filters
        this.currentFilter = TimelineFilter.getCurrentFilter();
        console.log(`🔄 Progressive loader synced with current filter: ${this.currentFilter}`);
    },
    
    loadRemainingContentByType() {
        const contentTypes = ['posts', 'notes', 'responses', 'bookmarks', 'reviews', 'media', 'snippets', 'wiki', 'presentations'];
        
        console.log('📊 Loading remaining content by type for stratified timeline...');
        
        contentTypes.forEach(contentType => {
            const contentScript = document.getElementById(`remainingContentData-${contentType}`);
            if (contentScript) {
                try {
                    const contentItems = JSON.parse(contentScript.textContent);
                    this.remainingContentByType.set(contentType, contentItems);
                    this.loadedCountByType.set(contentType, 0);
                    console.log(`✅ Loaded ${contentItems.length} remaining ${contentType} items`);
                } catch (error) {
                    console.error(`❌ Error parsing ${contentType} content data:`, error);
                    this.remainingContentByType.set(contentType, []);
                    this.loadedCountByType.set(contentType, 0);
                }
            } else {
                this.remainingContentByType.set(contentType, []);
                this.loadedCountByType.set(contentType, 0);
                console.log(`📊 No remaining ${contentType} items found`);
            }
        });
        
        const totalRemaining = Array.from(this.remainingContentByType.values())
            .reduce((sum, items) => sum + items.length, 0);
        console.log(`🎯 Progressive loader initialized with ${totalRemaining} total remaining items`);
        
        this.updateLoadMoreButton();
    },
    
    setupFilterListener() {
        document.addEventListener('filterChanged', (event) => {
            this.currentFilter = event.detail.filterType;
            console.log(`🔄 Filter changed to: ${this.currentFilter}`);
            this.updateLoadMoreButton();
        });
    },
    
    setupIntersectionObserver() {
        this.observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting && !this.isLoading && this.hasMoreContent()) {
                    console.log('🔄 Intersection observer triggered - loading more content');
                    this.loadMoreContent();
                }
            });
        }, {
            rootMargin: '100px',
            threshold: 0.1
        });
        
        const loadMoreSection = document.getElementById('loadMoreSection');
        if (loadMoreSection && this.hasMoreContent()) {
            this.observer.observe(loadMoreSection);
            console.log('👀 Intersection observer setup');
        }
    },
    
    setupLoadMoreButton() {
        const loadMoreBtn = document.getElementById('loadMoreBtn');
        if (loadMoreBtn) {
            loadMoreBtn.addEventListener('click', (e) => {
                e.preventDefault();
                console.log('🖱️ Load more button clicked');
                this.loadMoreContent();
            });
            console.log('🖱️ Load more button event listener attached');
        } else {
            console.log('⚠️ Load more button not found');
        }
    },
    
    async loadMoreContent() {
        if (this.isLoading || !this.hasMoreContent()) {
            console.log('⏸️ Load more skipped - already loading or no content');
            return;
        }
        
        this.isLoading = true;
        this.updateLoadingState(true);
        
        try {
            console.log(`📥 Loading more content for filter: ${this.currentFilter}`);
            
            await new Promise(resolve => setTimeout(resolve, 300));
            
            const newContent = this.generateContentChunkByType();
            
            const progressiveContainer = document.getElementById('progressiveContent');
            if (progressiveContainer && newContent) {
                progressiveContainer.insertAdjacentHTML('beforeend', newContent);
                console.log('✅ New content added to progressive container');
                
                this.applyFilterToNewContent(this.currentFilter);
                this.animateNewContent();
            }
            
            this.updateLoadMoreButton();
            
        } catch (error) {
            console.error('❌ Error loading more content:', error);
        } finally {
            this.isLoading = false;
            this.updateLoadingState(false);
        }
    },
    
    generateContentChunkByType() {
        let html = '';
        let itemsLoaded = 0;
        
        if (this.currentFilter === 'all') {
            const contentTypes = Array.from(this.remainingContentByType.keys()).filter(type => {
                const items = this.remainingContentByType.get(type) || [];
                const loadedCount = this.loadedCountByType.get(type) || 0;
                return loadedCount < items.length;
            });
            
            if (contentTypes.length === 0) {
                console.log('📭 No more content available for "all" filter');
                return '';
            }
            
            const itemsPerType = Math.ceil(this.config.chunkSize / contentTypes.length);
            
            contentTypes.forEach(contentType => {
                const items = this.remainingContentByType.get(contentType) || [];
                const loadedCount = this.loadedCountByType.get(contentType) || 0;
                const availableItems = items.slice(loadedCount, loadedCount + itemsPerType);
                
                availableItems.forEach(item => {
                    if (itemsLoaded < this.config.chunkSize) {
                        html += this.generateContentCard(item);
                        itemsLoaded++;
                    }
                });
                
                this.loadedCountByType.set(contentType, loadedCount + availableItems.length);
            });
        } else {
            const items = this.remainingContentByType.get(this.currentFilter) || [];
            const loadedCount = this.loadedCountByType.get(this.currentFilter) || 0;
            const availableItems = items.slice(loadedCount, loadedCount + this.config.chunkSize);
            
            if (availableItems.length === 0) {
                console.log(`📭 No more content available for "${this.currentFilter}" filter`);
                return '';
            }
            
            availableItems.forEach(item => {
                html += this.generateContentCard(item);
                itemsLoaded++;
            });
            
            this.loadedCountByType.set(this.currentFilter, loadedCount + availableItems.length);
        }
        
        console.log(`✅ Generated ${itemsLoaded} content cards for filter: ${this.currentFilter}`);
        return html;
    },
    
    // Extract review item type from review content HTML (JavaScript version of F# extractReviewItemType)
    extractReviewItemType(content) {
        try {
            if (content && content.includes('item-type-badge')) {
                const startTag = 'item-type-badge badge bg-secondary">';
                const endTag = '</span>';
                const startIndex = content.indexOf(startTag);
                if (startIndex >= 0) {
                    const contentStart = startIndex + startTag.length;
                    const endIndex = content.indexOf(endTag, contentStart);
                    if (endIndex > contentStart) {
                        const itemType = content.substring(contentStart, endIndex).trim();
                        // Convert from uppercase back to proper case
                        return itemType.charAt(0).toUpperCase() + itemType.slice(1).toLowerCase();
                    }
                }
            }
            return null;
        } catch (ex) {
            return null;
        }
    },

    generateContentCard(item) {
        const date = new Date(item.date);
        const formattedDate = date.toLocaleDateString('en-US', { 
            year: 'numeric', 
            month: 'short', 
            day: 'numeric' 
        });
        
        const contentTypeBadge = (() => {
            if (item.contentType === 'reviews') {
                // For reviews, try to extract the specific item type (Book, Movie, etc.)
                const itemType = this.extractReviewItemType(item.content);
                return itemType || 'Review';  // Fallback to generic "Review"
            }
            
            // For other content types, use the standard mapping
            return {
                'posts': 'Blog Post',
                'notes': 'Note',
                'responses': 'Response',
                'bookmarks': 'Bookmark',
                'reviews': 'Review',
                'streams': 'Stream Recording',
                'media': 'Media',
                'snippets': 'Snippet',
                'wiki': 'Wiki',
                'presentations': 'Presentation',
                // Specific response types
                'star': 'Star',
                'reply': 'Reply',
                'reshare': 'Reshare',
                'bookmark': 'Bookmark'
            }[item.contentType] || item.contentType;
        })();
        
        const tagsHtml = item.tags && item.tags.length > 0 
            ? `<div class="p-category tags">
                   ${item.tags.map(tag => `<a class="tag-link" href="/tags/${tag}/">#${tag}</a>`).join('')}
               </div>`
            : '';
        
        return `
        <article class="h-entry content-card" data-type="${item.contentType}" data-date="${item.date}" style="opacity: 0; transform: translateY(10px);">
            <header class="card-header">
                <time class="dt-published publication-date" datetime="${item.date}">${formattedDate}</time>
                <div class="content-type-info">
                    <span class="content-type-badge" data-type="${item.contentType}">${contentTypeBadge}</span>
                </div>
            </header>
            <div class="card-body">
                <h2 class="p-name card-title">
                    <a class="u-url title-link" href="${item.url}">${item.title}</a>
                </h2>
                <div class="e-content card-content">
                    ${item.content}
                </div>
            </div>
            <footer class="card-footer">
                <div class="card-meta">
                    ${tagsHtml}
                </div>
            </footer>
        </article>`;
    },
    
    hasMoreContent() {
        if (this.currentFilter === 'all') {
            for (const [contentType, items] of this.remainingContentByType) {
                const loadedCount = this.loadedCountByType.get(contentType) || 0;
                if (loadedCount < items.length) {
                    return true;
                }
            }
            return false;
        } else {
            const items = this.remainingContentByType.get(this.currentFilter) || [];
            const loadedCount = this.loadedCountByType.get(this.currentFilter) || 0;
            return loadedCount < items.length;
        }
    },
    
    updateLoadMoreButton() {
        const loadMoreBtn = document.getElementById('loadMoreBtn');
        const loadMoreSection = document.getElementById('loadMoreSection');
        
        if (!loadMoreBtn || !loadMoreSection) {
            console.log('⚠️ Load more button or section not found');
            return;
        }
        
        const hasMore = this.hasMoreContent();
        const totalRemaining = this.getRemainingItemCount();
        
        console.log(`🔄 Updating load more button - hasMore: ${hasMore}, remaining: ${totalRemaining}`);
        
        if (hasMore && totalRemaining > 0) {
            loadMoreBtn.textContent = `Load More (${totalRemaining} items remaining)`;
            loadMoreSection.style.display = 'block';
            
            if (this.observer && !this.isObserving) {
                this.observer.observe(loadMoreSection);
                this.isObserving = true;
            }
        } else {
            loadMoreSection.style.display = 'none';
            
            if (this.observer) {
                this.observer.disconnect();
                this.isObserving = false;
            }
            
            console.log('🎉 All content loaded!');
        }
    },
    
    getRemainingItemCount() {
        if (this.currentFilter === 'all') {
            let total = 0;
            for (const [contentType, items] of this.remainingContentByType) {
                const loadedCount = this.loadedCountByType.get(contentType) || 0;
                total += Math.max(0, items.length - loadedCount);
            }
            return total;
        } else {
            const items = this.remainingContentByType.get(this.currentFilter) || [];
            const loadedCount = this.loadedCountByType.get(this.currentFilter) || 0;
            return Math.max(0, items.length - loadedCount);
        }
    },
    
    applyFilterToNewContent(filterType) {
        const newCards = document.querySelectorAll('.content-card[style*="opacity: 0"]');
        newCards.forEach(card => {
            const cardType = card.getAttribute('data-type');
            
            // Special handling for response types
            let shouldShow = false;
            if (filterType === 'all') {
                shouldShow = true;
            } else if (filterType === 'responses') {
                // Show all response subtypes for "responses" filter
                shouldShow = ['star', 'reply', 'reshare', 'rsvp', 'responses'].includes(cardType);
            } else {
                // Exact match for other content types
                shouldShow = (filterType === cardType);
            }
            
            if (!shouldShow) {
                card.style.display = 'none';
                card.classList.add('filtered-out');
            } else {
                card.classList.add('filtered-in');
            }
        });
    },
    
    animateNewContent() {
        const newCards = document.querySelectorAll('.content-card[style*="opacity: 0"]');
        newCards.forEach((card, index) => {
            setTimeout(() => {
                card.style.transition = 'opacity 0.4s ease, transform 0.4s ease';
                card.style.opacity = '1';
                card.style.transform = 'translateY(0)';
            }, index * 50);
        });
    },
    
    updateLoadingState(isLoading) {
        const loadMoreBtn = document.getElementById('loadMoreBtn');
        const loadingSpinner = document.getElementById('loadingIndicator');
        
        if (loadMoreBtn) {
            loadMoreBtn.disabled = isLoading;
        }
        
        if (loadingSpinner) {
            loadingSpinner.style.display = isLoading ? 'block' : 'none';
        }
    }
};

// Mobile Navigation Manager
const TimelineMobileNav = {
    init() {
        this.setupMobileToggle();
        this.setupOverlayClose();
    },

    setupMobileToggle() {
        const mobileToggle = document.getElementById('mobile-nav-toggle');
        if (mobileToggle) {
            mobileToggle.addEventListener('click', () => {
                this.toggleMobileNav();
            });
            console.log('📱 Mobile toggle event listener attached');
        } else {
            console.warn('⚠️ Mobile toggle button not found');
        }
    },

    setupOverlayClose() {
        const overlay = document.getElementById('nav-overlay');
        if (overlay) {
            overlay.addEventListener('click', () => {
                this.closeMobileNav();
            });
        }

        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') {
                this.closeMobileNav();
            }
        });
    },

    toggleMobileNav() {
        const nav = document.getElementById('sidebar-menu');
        const overlay = document.getElementById('nav-overlay');
        const toggle = document.getElementById('mobile-nav-toggle');
        
        if (nav && overlay && toggle) {
            const isOpen = nav.classList.contains('active');
            
            if (isOpen) {
                this.closeMobileNav();
            } else {
                this.openMobileNav();
            }
        } else {
            console.warn('⚠️ Navigation elements not found for mobile toggle');
        }
    },

    openMobileNav() {
        const nav = document.getElementById('sidebar-menu');
        const overlay = document.getElementById('nav-overlay');
        const toggle = document.getElementById('mobile-nav-toggle');
        
        if (nav && overlay && toggle) {
            nav.classList.add('active');
            overlay.classList.add('active');
            toggle.classList.add('active');
            toggle.setAttribute('aria-expanded', 'true');
            document.body.style.overflow = 'hidden';
            console.log('📱 Mobile navigation opened');
        }
    },

    closeMobileNav() {
        const nav = document.getElementById('sidebar-menu');
        const overlay = document.getElementById('nav-overlay');
        const toggle = document.getElementById('mobile-nav-toggle');
        
        if (nav && overlay && toggle) {
            nav.classList.remove('active');
            overlay.classList.remove('active');
            toggle.classList.remove('active');
            toggle.setAttribute('aria-expanded', 'false');
            document.body.style.overflow = '';
            console.log('📱 Mobile navigation closed');
        }
    }
};

// Navigation Dropdown Manager
const TimelineDropdownNav = {
    init() {
        this.setupDropdownListeners();
    },

    toggleDropdown(dropdownId) {
        const dropdown = document.getElementById(dropdownId);
        const toggle = document.querySelector(`[data-target="${dropdownId}"]`);
        
        if (dropdown && toggle) {
            const isOpen = dropdown.classList.contains('show');
            
            document.querySelectorAll('.dropdown-menu.show').forEach(menu => {
                if (menu.id !== dropdownId) {
                    menu.classList.remove('show');
                }
            });
            
            if (isOpen) {
                dropdown.classList.remove('show');
                toggle.setAttribute('aria-expanded', 'false');
            } else {
                dropdown.classList.add('show');
                toggle.setAttribute('aria-expanded', 'true');
            }
        }
    },

    setupDropdownListeners() {
        const collectionsToggle = document.querySelector('[data-target="collections-dropdown"]');
        if (collectionsToggle) {
            collectionsToggle.addEventListener('click', (e) => {
                e.preventDefault();
                this.toggleDropdown('collections-dropdown');
            });
        }
        
        const resourcesToggle = document.querySelector('[data-target="resources-dropdown"]');
        if (resourcesToggle) {
            resourcesToggle.addEventListener('click', (e) => {
                e.preventDefault();
                this.toggleDropdown('resources-dropdown');
            });
        }
        
        document.addEventListener('click', (e) => {
            if (!e.target.closest('.nav-section.dropdown')) {
                document.querySelectorAll('.dropdown-menu.show').forEach(menu => {
                    menu.classList.remove('show');
                });
            }
        });
    }
};

// Back to Top Button Manager - UX Best Practices Implementation
const BackToTopManager = {
    button: null,
    scrollThreshold: 200, // Pixels to scroll before showing button
    
    init() {
        this.button = document.getElementById('backToTopBtn');
        if (!this.button) {
            console.warn('⚠️ Back to top button not found');
            return;
        }
        
        this.setupScrollListener();
        this.setupClickHandler();
        this.setupKeyboardNavigation();
        console.log('✅ Back to top button initialized');
    },
    
    setupScrollListener() {
        // Throttle scroll events for performance
        let scrollTimeout;
        
        window.addEventListener('scroll', () => {
            if (scrollTimeout) {
                clearTimeout(scrollTimeout);
            }
            
            scrollTimeout = setTimeout(() => {
                this.handleScroll();
            }, 16); // ~60fps
        }, { passive: true });
    },
    
    handleScroll() {
        const scrollTop = window.pageYOffset || document.documentElement.scrollTop;
        
        if (scrollTop > this.scrollThreshold) {
            this.showButton();
        } else {
            this.hideButton();
        }
    },
    
    showButton() {
        if (this.button && !this.button.classList.contains('visible')) {
            this.button.classList.add('visible');
        }
    },
    
    hideButton() {
        if (this.button && this.button.classList.contains('visible')) {
            this.button.classList.remove('visible');
        }
    },
    
    setupClickHandler() {
        this.button.addEventListener('click', (e) => {
            e.preventDefault();
            this.scrollToTop();
        });
    },
    
    setupKeyboardNavigation() {
        this.button.addEventListener('keydown', (e) => {
            if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault();
                this.scrollToTop();
            }
        });
    },
    
    scrollToTop() {
        // Check if user prefers reduced motion
        const prefersReducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        
        if (prefersReducedMotion) {
            // Instant scroll for users with motion sensitivity
            window.scrollTo(0, 0);
        } else {
            // Smooth scroll for other users
            window.scrollTo({
                top: 0,
                behavior: 'smooth'
            });
        }
        
        // Focus management for accessibility - focus on main content area
        const timelineHeader = document.querySelector('.timeline-header h1');
        if (timelineHeader) {
            // Set focus to a logical element near the top
            timelineHeader.focus();
        }
    }
};

// Initialize everything when DOM is ready
function initializeTimeline() {
    console.log('🔧 DOMContentLoaded event fired or DOM ready');
    
    try {
        // NOTE: Theme, mobile nav, and dropdown nav are initialized by main.js
        // We only initialize timeline-specific functionality here to avoid conflicts
        
        const timelineElement = document.querySelector('.unified-timeline');
        console.log('🔧 Timeline element found:', !!timelineElement);
        
        if (timelineElement) {
            TimelineFilter.init();
            TimelineProgressiveLoader.init();
            console.log('🌵 Timeline filtering and progressive loading initialized');
        }
        
        console.log('🔧 Initializing back to top button...');
        BackToTopManager.init();
        console.log('✅ Back to top button initialized');
        
        console.log('🌵 Timeline interface initialized with desert theme');
        
    } catch (error) {
        console.error('❌ Error during timeline initialization:', error);
    }
}

// Setup DOM event listeners
try {
    if (document.readyState === 'loading') {
        console.log('🔧 DOM still loading, adding DOMContentLoaded listener');
        document.addEventListener('DOMContentLoaded', initializeTimeline);
    } else {
        console.log('🔧 DOM already loaded, initializing immediately');
        initializeTimeline();
    }
} catch (scriptError) {
    console.error('🚨 CRITICAL ERROR during script setup:', scriptError);
}

// Export for external use
window.TimelineInterface = {
    filter: TimelineFilter,
    theme: TimelineThemeManager,
    mobile: TimelineMobileNav,
    dropdown: TimelineDropdownNav,
    progressive: TimelineProgressiveLoader,
    backToTop: BackToTopManager
};

console.log('🏁 Timeline.js loaded successfully - stratified version');
