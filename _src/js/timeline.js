// Timeline Content Filtering - Desert Theme Integration
// Feed-as-Homepage Phase 3 Implementation

// IMMEDIATE DEBUG - This should execute as soon as the script is parsed
console.log('🚀 IMMEDIATE: timeline.js parsing started');
console.log('🚀 IMMEDIATE: Current URL:', window.location.href);
console.log('🚀 IMMEDIATE: Document exists:', !!document);
console.log('🚀 IMMEDIATE: Window exists:', !!window);

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
            const shouldShow = (contentType === 'all' || contentType === cardType);
            
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

        // Update filter button text with count
        this.updateFilterButtonText(contentType, visibleCount);
        
        // Announce change for screen readers
        this.announceFilterChange(contentType, visibleCount);
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

    // Update filter button text with count (optional enhancement)
    updateFilterButtonText(filterType, count) {
        const button = document.querySelector(`[data-filter="${filterType}"]`);
        if (button) {
            const baseText = button.textContent.split(' (')[0]; // Remove existing count
            // Optionally show count: button.textContent = `${baseText} (${count})`;
        }
    },

    // Save filter state to localStorage
    saveFilterState(filterType) {
        try {
            localStorage.setItem('timelineFilter', filterType);
        } catch (e) {
            // Fail silently if localStorage not available
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
            // Fail silently if localStorage not available
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
            
            // Arrow keys for filter navigation when focused on filter buttons
            if (e.target.classList.contains('filter-btn')) {
                const buttons = Array.from(document.querySelectorAll('.filter-btn'));
                const currentIndex = buttons.indexOf(e.target);
                
                if (e.key === 'ArrowLeft' || e.key === 'ArrowUp') {
                    e.preventDefault();
                    const prevIndex = currentIndex > 0 ? currentIndex - 1 : buttons.length - 1;
                    buttons[prevIndex].focus();
                } else if (e.key === 'ArrowRight' || e.key === 'ArrowDown') {
                    e.preventDefault();
                    const nextIndex = currentIndex < buttons.length - 1 ? currentIndex + 1 : 0;
                    buttons[nextIndex].focus();
                }
            }
        });
    },

    // Accessibility announcement for filter changes
    announceFilterChange(filterType, count) {
        // Create or update live region for screen reader announcements
        let announcement = document.getElementById('filter-announcement');
        if (!announcement) {
            announcement = document.createElement('div');
            announcement.id = 'filter-announcement';
            announcement.setAttribute('aria-live', 'polite');
            announcement.setAttribute('aria-atomic', 'true');
            announcement.style.position = 'absolute';
            announcement.style.left = '-10000px';
            announcement.style.width = '1px';
            announcement.style.height = '1px';
            announcement.style.overflow = 'hidden';
            document.body.appendChild(announcement);
        }
        
        const typeLabel = filterType === 'all' ? 'all content' : filterType;
        announcement.textContent = `Showing ${count} ${typeLabel} items`;
    },

    // Get current filter state (for external use)
    getCurrentFilter() {
        const activeButton = document.querySelector('.filter-btn.active');
        return activeButton ? activeButton.getAttribute('data-filter') : 'all';
    },

    // Reset to show all content
    showAll() {
        const allButton = document.querySelector('[data-filter="all"]');
        if (allButton) {
            allButton.click();
        }
    }
};

// Enhanced Theme Manager with timeline integration
const TimelineThemeManager = {
    init() {
        this.initializeTheme();
        this.setupThemeToggle();
    },

    initializeTheme() {
        // Get saved theme or use system preference
        const savedTheme = localStorage.getItem('theme');
        const systemPreference = window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
        const theme = savedTheme || systemPreference;
        
        // Apply theme to both elements for compatibility
        document.documentElement.setAttribute('data-theme', theme);
        document.body.setAttribute('data-theme', theme);
        this.updateThemeToggleIcon(theme);
        
        // Listen for system theme changes
        window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', (e) => {
            if (!localStorage.getItem('theme')) {
                const newTheme = e.matches ? 'dark' : 'light';
                document.documentElement.setAttribute('data-theme', newTheme);
                document.body.setAttribute('data-theme', newTheme);
                this.updateThemeToggleIcon(newTheme);
            }
        });
    },

    setupThemeToggle() {
        const themeToggle = document.getElementById('theme-toggle');
        if (themeToggle) {
            themeToggle.addEventListener('click', () => {
                this.toggleTheme();
            });
        }

        // Keyboard shortcut: Alt+T
        document.addEventListener('keydown', (e) => {
            if (e.altKey && e.key.toLowerCase() === 't') {
                e.preventDefault();
                this.toggleTheme();
            }
        });
    },

    toggleTheme() {
        const current = document.documentElement.getAttribute('data-theme') || document.body.getAttribute('data-theme') || 'light';
        const newTheme = current === 'dark' ? 'light' : 'dark';
        
        // Smooth transition
        document.body.style.transition = 'background-color 0.3s ease, color 0.3s ease';
        document.documentElement.setAttribute('data-theme', newTheme);
        document.body.setAttribute('data-theme', newTheme);
        
        // Update icon and save preference
        this.updateThemeToggleIcon(newTheme);
        localStorage.setItem('theme', newTheme);
        
        // Remove transition after animation
        setTimeout(() => {
            document.body.style.transition = '';
        }, 300);
    },

    updateThemeToggleIcon(theme) {
        const themeToggle = document.getElementById('theme-toggle-icon');
        if (themeToggle) {
            // Use proper emoji encoding to avoid corruption
            const icon = theme === 'dark' ? '\u{1F319}' : '\u{2600}\u{FE0F}'; // 🌙 : ☀️
            themeToggle.innerHTML = icon;
            
            const themeButton = document.getElementById('theme-toggle');
            if (themeButton) {
                themeButton.setAttribute('aria-label', `Switch to ${theme === 'dark' ? 'light' : 'dark'} theme`);
            }
        }
    }
};

// Mobile Navigation Manager (inherited from Phase 2)
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
        }
    },

    setupOverlayClose() {
        // Close on overlay click
        const overlay = document.getElementById('mobile-nav-overlay');
        if (overlay) {
            overlay.addEventListener('click', () => {
                this.closeMobileNav();
            });
        }

        // Close on escape key
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') {
                this.closeMobileNav();
            }
        });
    },

    toggleMobileNav() {
        const nav = document.querySelector('.desert-nav');
        const overlay = document.getElementById('mobile-nav-overlay');
        
        if (nav && overlay) {
            const isOpen = nav.classList.contains('mobile-open');
            
            if (isOpen) {
                this.closeMobileNav();
            } else {
                this.openMobileNav();
            }
        }
    },

    openMobileNav() {
        const nav = document.querySelector('.desert-nav');
        const overlay = document.getElementById('mobile-nav-overlay');
        
        if (nav && overlay) {
            nav.classList.add('mobile-open');
            overlay.classList.add('active');
            document.body.style.overflow = 'hidden';
            
            // Focus first nav item for accessibility
            const firstNavItem = nav.querySelector('.nav-link');
            if (firstNavItem) {
                firstNavItem.focus();
            }
        }
    },

    closeMobileNav() {
        const nav = document.querySelector('.desert-nav');
        const overlay = document.getElementById('mobile-nav-overlay');
        
        if (nav && overlay) {
            nav.classList.remove('mobile-open');
            overlay.classList.remove('active');
            document.body.style.overflow = '';
        }
    }
};

// Progressive Loading Manager - Handles chunked content loading for large content volumes
const TimelineProgressiveLoader = {
    // Configuration
    config: {
        chunkSize: 25,
        loadThreshold: 0.8 // Trigger load when 80% of current content is visible
    },
    
    // State
    currentPage: 1,
    totalItems: 0,
    loadedItems: 50, // Start with 50 items already loaded
    isLoading: false,
    observer: null,
    remainingContent: [], // Array to store remaining content from server
    
    init() {
        this.setupProgressiveLoading();
        this.loadRemainingContent();
        this.setupIntersectionObserver();
        this.setupLoadMoreButton();
    },
    
    setupProgressiveLoading() {
        // Get total items from data attribute
        const progressiveContainer = document.getElementById('progressiveContent');
        if (progressiveContainer) {
            this.totalItems = parseInt(progressiveContainer.getAttribute('data-total-items')) || 0;
            console.log(`📊 Progressive loader initialized: ${this.loadedItems}/${this.totalItems} items`);
        }
    },
    
    loadRemainingContent() {
        // Load remaining content data from JSON script tag
        const contentScript = document.getElementById('remainingContentData');
        if (contentScript) {
            try {
                this.remainingContent = JSON.parse(contentScript.textContent);
                console.log(`📦 Loaded ${this.remainingContent.length} remaining content items`);
            } catch (error) {
                console.error('❌ Error parsing remaining content data:', error);
                this.remainingContent = [];
            }
        }
    },
    
    setupIntersectionObserver() {
        // Create intersection observer for automatic loading
        this.observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting && !this.isLoading && this.hasMoreContent()) {
                    console.log('🔄 Intersection observer triggered - loading more content');
                    this.loadMoreContent();
                }
            });
        }, {
            rootMargin: '100px', // Start loading 100px before the sentinel comes into view
            threshold: 0.1
        });
        
        // Observe the load more section
        const loadMoreSection = document.getElementById('loadMoreSection');
        if (loadMoreSection && this.hasMoreContent()) {
            this.observer.observe(loadMoreSection);
        }
    },
    
    setupLoadMoreButton() {
        const loadMoreBtn = document.getElementById('loadMoreBtn');
        if (loadMoreBtn) {
            loadMoreBtn.addEventListener('click', (e) => {
                e.preventDefault();
                this.loadMoreContent();
            });
        }
    },
    
    async loadMoreContent() {
        if (this.isLoading || !this.hasMoreContent()) {
            return;
        }
        
        this.isLoading = true;
        this.updateLoadingState(true);
        
        try {
            // Calculate items to load
            const remainingItems = this.totalItems - this.loadedItems;
            const itemsToLoad = Math.min(this.config.chunkSize, remainingItems);
            
            console.log(`📥 Loading ${itemsToLoad} more items (${this.loadedItems + itemsToLoad}/${this.totalItems})`);
            
            // Simulate loading delay for better UX
            await new Promise(resolve => setTimeout(resolve, 300));
            
            // Generate new content cards
            const newContent = this.generateContentChunk(itemsToLoad);
            
            // Add new content to progressive container
            const progressiveContainer = document.getElementById('progressiveContent');
            if (progressiveContainer) {
                progressiveContainer.insertAdjacentHTML('beforeend', newContent);
                
                // Update state
                this.loadedItems += itemsToLoad;
                this.currentPage++;
                
                // Apply current filter to new content
                const currentFilter = window.TimelineInterface?.filter?.getCurrentFilter() || 'all';
                if (currentFilter !== 'all') {
                    this.applyFilterToNewContent(currentFilter);
                }
                
                // Smooth reveal of new content
                this.animateNewContent();
            }
            
            // Update UI state
            this.updateLoadMoreButton();
            this.updateProgressIndicator();
            
            console.log(`✅ Loaded ${itemsToLoad} items. Total loaded: ${this.loadedItems}/${this.totalItems}`);
            
        } catch (error) {
            console.error('❌ Error loading more content:', error);
        } finally {
            this.isLoading = false;
            this.updateLoadingState(false);
        }
    },
    
    generateContentChunk(itemCount) {
        // Use actual remaining content instead of placeholder data
        let html = '';
        const startIndex = this.loadedItems - 50; // Offset for remaining content array
        const endIndex = Math.min(startIndex + itemCount, this.remainingContent.length);
        
        for (let i = startIndex; i < endIndex; i++) {
            const item = this.remainingContent[i];
            if (!item) continue;
            
            // Format date
            const date = new Date(item.date);
            const formattedDate = date.toLocaleDateString('en-US', { 
                year: 'numeric', 
                month: 'short', 
                day: 'numeric' 
            });
            
            // Content type badge text
            const contentTypeBadge = {
                'posts': 'Blog Post',
                'notes': 'Note',
                'responses': 'Response',
                'bookmarks': 'Bookmark',
                'reviews': 'Review',
                'streams': 'Stream Recording',
                'media': 'Media'
            }[item.contentType] || item.contentType;
            
            // Generate tags HTML
            const tagsHtml = item.tags && item.tags.length > 0 
                ? `<div class="p-category tags">
                     ${item.tags.map(tag => `<a class="tag-link" href="/tags/${tag}/">#${tag}</a>`).join('')}
                   </div>`
                : '';
            
            html += `
                <article class="h-entry content-card" data-type="${item.contentType}" data-date="${item.date}" style="opacity: 0; transform: translateY(20px);">
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
                </article>
            `;
        }
        
        return html;
    },
    
    applyFilterToNewContent(filterType) {
        // Apply current filter to newly loaded content
        const newCards = document.querySelectorAll('.content-card[style*="opacity: 0"]');
        newCards.forEach(card => {
            const cardType = card.getAttribute('data-type');
            const shouldShow = (filterType === 'all' || filterType === cardType);
            
            if (!shouldShow) {
                card.style.display = 'none';
                card.classList.add('filtered-out');
            }
        });
    },
    
    animateNewContent() {
        // Smooth reveal animation for new content
        const newCards = document.querySelectorAll('.content-card[style*="opacity: 0"]');
        newCards.forEach((card, index) => {
            setTimeout(() => {
                card.style.transition = 'opacity 0.4s ease, transform 0.4s ease';
                card.style.opacity = '1';
                card.style.transform = 'translateY(0)';
            }, index * 50); // Stagger the animations
        });
    },
    
    updateLoadingState(isLoading) {
        const loadMoreBtn = document.getElementById('loadMoreBtn');
        const loadingSpinner = document.getElementById('loadingIndicator');
        
        if (loadMoreBtn) {
            loadMoreBtn.disabled = isLoading;
            loadMoreBtn.textContent = isLoading ? 'Loading...' : 'Load More Posts';
        }
        
        if (loadingSpinner) {
            loadingSpinner.style.display = isLoading ? 'block' : 'none';
        }
    },
    
    updateLoadMoreButton() {
        const loadMoreSection = document.getElementById('loadMoreSection');
        
        if (!this.hasMoreContent()) {
            // Hide load more section when all content is loaded
            if (loadMoreSection) {
                loadMoreSection.style.display = 'none';
            }
            
            // Stop observing
            if (this.observer) {
                this.observer.disconnect();
            }
            
            console.log('🎉 All content loaded!');
        }
    },
    
    updateProgressIndicator() {
        const progressBar = document.querySelector('.progress-bar');
        if (progressBar) {
            const percentage = (this.loadedItems / this.totalItems) * 100;
            progressBar.style.width = `${percentage}%`;
        }
        
        const progressText = document.querySelector('.progress-text');
        if (progressText) {
            progressText.textContent = `${this.loadedItems} of ${this.totalItems} items loaded`;
        }
    },
    
    hasMoreContent() {
        return this.loadedItems < this.totalItems;
    },
    
    // Reset loader state (useful for filter changes)
    reset() {
        this.currentPage = 1;
        this.loadedItems = 50;
        this.isLoading = false;
        
        // Clear progressive content
        const progressiveContainer = document.getElementById('progressiveContent');
        if (progressiveContainer) {
            progressiveContainer.innerHTML = '';
        }
        
        // Reset load more button
        const loadMoreSection = document.getElementById('loadMoreSection');
        if (loadMoreSection && this.hasMoreContent()) {
            loadMoreSection.style.display = 'block';
            if (this.observer) {
                this.observer.observe(loadMoreSection);
            }
        }
        
        this.updateProgressIndicator();
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
            
            // Close all other dropdowns first
            document.querySelectorAll('.dropdown-menu.show').forEach(menu => {
                if (menu.id !== dropdownId) {
                    menu.classList.remove('show');
                    const otherToggle = document.querySelector(`[data-target="${menu.id}"]`);
                    if (otherToggle) {
                        otherToggle.setAttribute('aria-expanded', 'false');
                    }
                }
            });
            
            // Toggle current dropdown
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
        // Collections dropdown
        const collectionsToggle = document.querySelector('[data-target="collections-dropdown"]');
        if (collectionsToggle) {
            collectionsToggle.addEventListener('click', (e) => {
                e.preventDefault();
                this.toggleDropdown('collections-dropdown');
            });
        }
        
        // Resources dropdown
        const resourcesToggle = document.querySelector('[data-target="resources-dropdown"]');
        if (resourcesToggle) {
            resourcesToggle.addEventListener('click', (e) => {
                e.preventDefault();
                this.toggleDropdown('resources-dropdown');
            });
        }
        
        // Close dropdowns when clicking outside
        document.addEventListener('click', (e) => {
            if (!e.target.closest('.nav-section.dropdown')) {
                document.querySelectorAll('.dropdown-menu.show').forEach(menu => {
                    menu.classList.remove('show');
                    const toggle = document.querySelector(`[data-target="${menu.id}"]`);
                    if (toggle) {
                        toggle.setAttribute('aria-expanded', 'false');
                    }
                });
            }
        });
    }
};

// Debug: Script loaded
console.log('🔧 timeline.js script loaded - BEFORE DOM checks');
console.log('🔧 Document ready state:', document.readyState);
console.log('🔧 DOM loaded:', document.readyState === 'complete');

// More immediate debugging
console.log('🚀 SCRIPT PARSING: About to set up DOM event listeners');

try {
    // Check if DOM is already loaded
    if (document.readyState === 'loading') {
        console.log('🔧 DOM still loading, adding DOMContentLoaded listener');
        document.addEventListener('DOMContentLoaded', initializeTimeline);
    } else {
        console.log('🔧 DOM already loaded, initializing immediately');
        initializeTimeline();
    }
} catch (scriptError) {
    console.error('🚨 CRITICAL ERROR during script setup:', scriptError);
    console.error('🚨 Script error stack:', scriptError.stack);
}

function initializeTimeline() {
    console.log('🔧 DOMContentLoaded event fired or DOM ready');
    console.log('🔧 Current readyState:', document.readyState);
    
    try {
        // Always initialize theme management first (works on all pages)
        console.log('🔧 Initializing theme manager...');
        TimelineThemeManager.init();
        console.log('✅ Theme manager initialized');
        
        // Only initialize timeline-specific features if timeline elements exist
        const timelineElement = document.querySelector('.unified-timeline');
        console.log('🔧 Timeline element found:', !!timelineElement);
        
        if (timelineElement) {
            TimelineFilter.init();
            TimelineProgressiveLoader.init();
            console.log('🌵 Timeline filtering and progressive loading initialized');
        }
        
        // Always initialize mobile navigation (works on all pages)
        console.log('🔧 Initializing mobile nav...');
        TimelineMobileNav.init();
        console.log('✅ Mobile nav initialized');
        
        // Always initialize dropdown navigation (works on all pages)
        console.log('🔧 Initializing dropdown nav...');
        TimelineDropdownNav.init();
        console.log('✅ Dropdown nav initialized');
        
        console.log('🌵 Timeline interface initialized with desert theme');
        
    } catch (error) {
        console.error('❌ Error during timeline initialization:', error);
        console.error('❌ Error stack:', error.stack);
    }
}

// Export for external use
window.TimelineInterface = {
    filter: TimelineFilter,
    theme: TimelineThemeManager, // Theme management now handled by timeline.js
    mobile: TimelineMobileNav,
    dropdown: TimelineDropdownNav, // Navigation dropdown management
    progressive: TimelineProgressiveLoader // Progressive loading management
};

// FINAL DEBUG - This should be the last thing executed
console.log('🏁 SCRIPT END: timeline.js parsing completed successfully');
console.log('🏁 TimelineInterface exported:', !!window.TimelineInterface);
