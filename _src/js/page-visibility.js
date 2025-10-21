/**
 * Page Visibility API - Resource Optimization
 * Pauses animations, reduces resource usage when tab is hidden
 * Progressive enhancement with feature detection
 */

class PageVisibilityManager {
    constructor() {
        this.isVisible = true;
        this.hiddenTime = null;
        this.visibilityChangeHandlers = [];
        this.init();
    }

    init() {
        // Check for Page Visibility API support
        if (!this.isSupported()) {
            console.log('[Page Visibility] API not supported in this browser');
            return;
        }

        // Setup visibility change listener
        this.setupVisibilityListener();
        
        // Setup default optimizations
        this.setupDefaultOptimizations();
        
        console.log('[Page Visibility] Initialized successfully');
    }

    isSupported() {
        return typeof document.hidden !== 'undefined' ||
               typeof document.msHidden !== 'undefined' ||
               typeof document.webkitHidden !== 'undefined';
    }

    getHiddenProperty() {
        if (typeof document.hidden !== 'undefined') {
            return 'hidden';
        } else if (typeof document.msHidden !== 'undefined') {
            return 'msHidden';
        } else if (typeof document.webkitHidden !== 'undefined') {
            return 'webkitHidden';
        }
        return null;
    }

    getVisibilityChangeEvent() {
        if (typeof document.hidden !== 'undefined') {
            return 'visibilitychange';
        } else if (typeof document.msHidden !== 'undefined') {
            return 'msvisibilitychange';
        } else if (typeof document.webkitHidden !== 'undefined') {
            return 'webkitvisibilitychange';
        }
        return null;
    }

    setupVisibilityListener() {
        const hiddenProperty = this.getHiddenProperty();
        const visibilityChangeEvent = this.getVisibilityChangeEvent();
        
        if (!hiddenProperty || !visibilityChangeEvent) {
            console.warn('[Page Visibility] Could not determine visibility properties');
            return;
        }

        document.addEventListener(visibilityChangeEvent, () => {
            const isHidden = document[hiddenProperty];
            this.handleVisibilityChange(!isHidden);
        });

        // Set initial state
        this.isVisible = !document[hiddenProperty];
    }

    handleVisibilityChange(isVisible) {
        this.isVisible = isVisible;
        
        if (isVisible) {
            console.log('[Page Visibility] Tab became visible');
            this.onVisible();
        } else {
            console.log('[Page Visibility] Tab became hidden');
            this.hiddenTime = Date.now();
            this.onHidden();
        }

        // Notify registered handlers
        this.visibilityChangeHandlers.forEach(handler => {
            try {
                handler(isVisible);
            } catch (error) {
                console.error('[Page Visibility] Handler error:', error);
            }
        });
    }

    onVisible() {
        // Resume animations
        this.resumeAnimations();
        
        // Resume video playback (if user wants)
        this.resumeMedia();
        
        // Check how long tab was hidden
        if (this.hiddenTime) {
            const hiddenDuration = Date.now() - this.hiddenTime;
            console.log(`[Page Visibility] Tab was hidden for ${Math.round(hiddenDuration / 1000)}s`);
            
            // If hidden for more than 5 minutes, consider refreshing content
            if (hiddenDuration > 5 * 60 * 1000) {
                this.considerContentRefresh();
            }
        }
    }

    onHidden() {
        // Pause animations
        this.pauseAnimations();
        
        // Pause video playback
        this.pauseMedia();
        
        // Reduce polling/timers
        this.reduceBackgroundActivity();
    }

    pauseAnimations() {
        // Pause CSS animations on specific elements
        const animatedElements = document.querySelectorAll('[data-pauseable-animation]');
        animatedElements.forEach(element => {
            element.style.animationPlayState = 'paused';
        });

        // Pause any JavaScript-based animations
        // (Custom implementation can add animation pause logic here)
        document.body.setAttribute('data-animations-paused', 'true');
    }

    resumeAnimations() {
        // Resume CSS animations
        const animatedElements = document.querySelectorAll('[data-pauseable-animation]');
        animatedElements.forEach(element => {
            element.style.animationPlayState = 'running';
        });

        // Resume JavaScript-based animations
        document.body.removeAttribute('data-animations-paused');
    }

    pauseMedia() {
        // Don't auto-pause videos - let browser handle it
        // But we can mark them for potential future behavior
        const videos = document.querySelectorAll('video[data-auto-pause]');
        videos.forEach(video => {
            if (!video.paused) {
                video.pause();
                video.setAttribute('data-paused-by-visibility', 'true');
            }
        });
    }

    resumeMedia() {
        // Only resume videos that were paused by visibility API
        const videos = document.querySelectorAll('video[data-paused-by-visibility]');
        videos.forEach(video => {
            video.play().catch(err => {
                console.log('[Page Visibility] Could not resume video:', err);
            });
            video.removeAttribute('data-paused-by-visibility');
        });
    }

    reduceBackgroundActivity() {
        // Dispatch event for other scripts to reduce activity
        document.dispatchEvent(new CustomEvent('pageHidden', {
            detail: { timestamp: this.hiddenTime }
        }));
    }

    considerContentRefresh() {
        // Dispatch event for potential content refresh
        document.dispatchEvent(new CustomEvent('pageVisibleAfterLongHidden', {
            detail: { hiddenDuration: Date.now() - this.hiddenTime }
        }));
    }

    setupDefaultOptimizations() {
        // Optimize timeline progressive loading
        document.addEventListener('pageHidden', () => {
            // Pause any ongoing progressive loading
            const timeline = document.querySelector('#timeline');
            if (timeline) {
                timeline.setAttribute('data-loading-paused', 'true');
            }
        });

        document.addEventListener('pageVisibleAfterLongHidden', () => {
            // Resume progressive loading
            const timeline = document.querySelector('#timeline');
            if (timeline) {
                timeline.removeAttribute('data-loading-paused');
            }
        });
    }

    /**
     * Register custom visibility change handler
     * @param {Function} handler - Function called with (isVisible) parameter
     */
    onVisibilityChange(handler) {
        if (typeof handler === 'function') {
            this.visibilityChangeHandlers.push(handler);
        }
    }

    /**
     * Get current visibility state
     * @returns {boolean} True if page is visible
     */
    isPageVisible() {
        return this.isVisible;
    }

    /**
     * Get time page has been hidden (if currently hidden)
     * @returns {number|null} Milliseconds hidden, or null if visible
     */
    getHiddenDuration() {
        if (this.isVisible || !this.hiddenTime) {
            return null;
        }
        return Date.now() - this.hiddenTime;
    }
}

// Initialize when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        const visibilityManager = new PageVisibilityManager();
        
        // Make available globally for other scripts
        window.visibilityManager = visibilityManager;
    });
} else {
    // DOM already loaded
    const visibilityManager = new PageVisibilityManager();
    window.visibilityManager = visibilityManager;
}

// Example usage for developers:
// 
// // Register custom handler
// if (window.visibilityManager) {
//     window.visibilityManager.onVisibilityChange((isVisible) => {
//         if (isVisible) {
//             console.log('Tab visible - resume operations');
//         } else {
//             console.log('Tab hidden - pause operations');
//         }
//     });
// }
//
// // Check current visibility
// if (window.visibilityManager && window.visibilityManager.isPageVisible()) {
//     // Page is currently visible
// }
//
// // Get hidden duration
// const hiddenMs = window.visibilityManager?.getHiddenDuration();
// if (hiddenMs) {
//     console.log(`Page has been hidden for ${hiddenMs}ms`);
// }
