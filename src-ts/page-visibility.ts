/**
 * Page Visibility API - Resource Optimization
 * Pauses animations, reduces resource usage when tab is hidden
 * Progressive enhancement with feature detection
 */

// Make this a module
export {};

// Extend Document interface for vendor-specific visibility properties
declare global {
    interface Document {
        msHidden?: boolean;
        webkitHidden?: boolean;
    }
}

type VisibilityHandler = (isVisible: boolean) => void;

class PageVisibilityManager {
    private isVisible: boolean = true;
    private hiddenTime: number | null = null;
    private visibilityChangeHandlers: VisibilityHandler[] = [];

    constructor() {
        this.init();
    }

    private init(): void {
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

    private isSupported(): boolean {
        return typeof document.hidden !== 'undefined' ||
               typeof document.msHidden !== 'undefined' ||
               typeof document.webkitHidden !== 'undefined';
    }

    private getHiddenProperty(): string | null {
        if (typeof document.hidden !== 'undefined') {
            return 'hidden';
        } else if (typeof document.msHidden !== 'undefined') {
            return 'msHidden';
        } else if (typeof document.webkitHidden !== 'undefined') {
            return 'webkitHidden';
        }
        return null;
    }

    private getVisibilityChangeEvent(): string | null {
        if (typeof document.hidden !== 'undefined') {
            return 'visibilitychange';
        } else if (typeof document.msHidden !== 'undefined') {
            return 'msvisibilitychange';
        } else if (typeof document.webkitHidden !== 'undefined') {
            return 'webkitvisibilitychange';
        }
        return null;
    }

    private setupVisibilityListener(): void {
        const hiddenProperty = this.getHiddenProperty();
        const visibilityChangeEvent = this.getVisibilityChangeEvent();
        
        if (!hiddenProperty || !visibilityChangeEvent) {
            console.warn('[Page Visibility] Could not determine visibility properties');
            return;
        }

        document.addEventListener(visibilityChangeEvent, () => {
            const isHidden = (document as any)[hiddenProperty];
            this.handleVisibilityChange(!isHidden);
        });

        // Set initial state
        this.isVisible = !(document as any)[hiddenProperty];
    }

    private handleVisibilityChange(isVisible: boolean): void {
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

    private onVisible(): void {
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

    private onHidden(): void {
        // Pause animations
        this.pauseAnimations();
        
        // Pause video playback
        this.pauseMedia();
        
        // Reduce polling/timers
        this.reduceBackgroundActivity();
    }

    private pauseAnimations(): void {
        // Pause CSS animations on specific elements
        const animatedElements = document.querySelectorAll<HTMLElement>('[data-pauseable-animation]');
        animatedElements.forEach(element => {
            element.style.animationPlayState = 'paused';
        });

        // Pause any JavaScript-based animations
        // (Custom implementation can add animation pause logic here)
        document.body.setAttribute('data-animations-paused', 'true');
    }

    private resumeAnimations(): void {
        // Resume CSS animations
        const animatedElements = document.querySelectorAll<HTMLElement>('[data-pauseable-animation]');
        animatedElements.forEach(element => {
            element.style.animationPlayState = 'running';
        });

        // Resume JavaScript-based animations
        document.body.removeAttribute('data-animations-paused');
    }

    private pauseMedia(): void {
        // Don't auto-pause videos - let browser handle it
        // But we can mark them for potential future behavior
        const videos = document.querySelectorAll<HTMLVideoElement>('video[data-auto-pause]');
        videos.forEach(video => {
            if (!video.paused) {
                video.pause();
                video.setAttribute('data-paused-by-visibility', 'true');
            }
        });
    }

    private resumeMedia(): void {
        // Only resume videos that were paused by visibility API
        const videos = document.querySelectorAll<HTMLVideoElement>('video[data-paused-by-visibility]');
        videos.forEach(video => {
            video.play().catch(err => {
                console.log('[Page Visibility] Could not resume video:', err);
            });
            video.removeAttribute('data-paused-by-visibility');
        });
    }

    private reduceBackgroundActivity(): void {
        // Dispatch event for other scripts to reduce activity
        document.dispatchEvent(new CustomEvent('pageHidden', {
            detail: { timestamp: this.hiddenTime }
        }));
    }

    private considerContentRefresh(): void {
        // Dispatch event for potential content refresh
        document.dispatchEvent(new CustomEvent('pageVisibleAfterLongHidden', {
            detail: { hiddenDuration: Date.now() - (this.hiddenTime || 0) }
        }));
    }

    private setupDefaultOptimizations(): void {
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
     * @param handler - Function called with (isVisible) parameter
     */
    public onVisibilityChange(handler: VisibilityHandler): void {
        if (typeof handler === 'function') {
            this.visibilityChangeHandlers.push(handler);
        }
    }

    /**
     * Get current visibility state
     * @returns True if page is visible
     */
    public isPageVisible(): boolean {
        return this.isVisible;
    }

    /**
     * Get time page has been hidden (if currently hidden)
     * @returns Milliseconds hidden, or null if visible
     */
    public getHiddenDuration(): number | null {
        if (this.isVisible || !this.hiddenTime) {
            return null;
        }
        return Date.now() - this.hiddenTime;
    }
}

// Extend Window interface for global visibility manager
declare global {
    interface Window {
        visibilityManager?: PageVisibilityManager;
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
