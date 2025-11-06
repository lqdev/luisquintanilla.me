/**
 * Image Lazy Loading Enhancement
 * Uses Intersection Observer API for performance optimization
 * Progressive enhancement for images in posts and media
 */

class LazyImageLoader {
    private images: HTMLImageElement[] = [];
    private observer: IntersectionObserver | null = null;

    constructor() {
        this.init();
    }

    private init(): void {
        // Check for Intersection Observer support
        if (!('IntersectionObserver' in window)) {
            console.log('IntersectionObserver not available - using native lazy loading fallback');
            this.useFallback();
            return;
        }

        // Setup intersection observer
        this.setupObserver();

        // Find and observe images
        this.observeImages();

        console.log(`✅ Lazy loading enabled for ${this.images.length} images`);
    }

    private setupObserver(): void {
        const options: IntersectionObserverInit = {
            root: null, // viewport
            rootMargin: '50px', // Load images 50px before they enter viewport
            threshold: 0.01
        };

        this.observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    this.loadImage(entry.target as HTMLImageElement);
                    this.observer?.unobserve(entry.target);
                }
            });
        }, options);
    }

    private observeImages(): void {
        // Find all images that don't already have loading="lazy"
        // Prioritize images without explicit loading attribute or with loading="eager"
        const imageSelectors = [
            'img:not([loading])',
            'img[loading="eager"]',
            'img.img-fluid',
            'picture img'
        ];

        imageSelectors.forEach(selector => {
            const images = document.querySelectorAll<HTMLImageElement>(selector);
            images.forEach(img => {
                // Skip if already processed or if it's above the fold
                if (img.dataset.lazy === 'processed' || this.isAboveFold(img)) {
                    return;
                }

                // Store original src if not already stored
                if (!img.dataset.src && img.src) {
                    img.dataset.src = img.src;
                    
                    // Add a placeholder or low-quality placeholder
                    // For now, we'll keep the original src but mark for optimization
                    img.dataset.lazy = 'pending';
                }

                this.images.push(img);
                this.observer?.observe(img);
            });
        });
    }

    private isAboveFold(img: HTMLImageElement): boolean {
        const rect = img.getBoundingClientRect();
        return rect.top < window.innerHeight && rect.top >= 0;
    }

    private loadImage(img: HTMLImageElement): void {
        // If image has data-src, load it
        if (img.dataset.src && img.dataset.src !== img.src) {
            img.src = img.dataset.src;
        }

        // Add loading attribute for browser-native lazy loading
        if (!img.hasAttribute('loading')) {
            img.setAttribute('loading', 'lazy');
        }

        // Mark as processed
        img.dataset.lazy = 'processed';

        // Add loaded class for any CSS transitions
        img.classList.add('lazy-loaded');

        // Handle image load success
        img.addEventListener('load', () => {
            img.classList.add('lazy-loaded-success');
        }, { once: true });

        // Handle image load error
        img.addEventListener('error', () => {
            console.warn('Failed to load image:', img.src);
            img.classList.add('lazy-loaded-error');
        }, { once: true });
    }

    private useFallback(): void {
        // For browsers without IntersectionObserver, use native loading attribute
        const images = document.querySelectorAll<HTMLImageElement>('img:not([loading])');
        images.forEach(img => {
            if (!this.isAboveFold(img)) {
                img.setAttribute('loading', 'lazy');
            }
        });
    }
}

// Initialize when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        new LazyImageLoader();
    });
} else {
    // DOM already loaded
    new LazyImageLoader();
}
