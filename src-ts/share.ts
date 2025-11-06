/**
 * Web Share API - Native Content Sharing
 * Attaches functionality to footer share buttons
 * Progressive enhancement with feature detection
 */

interface ShareMetadata {
    title: string;
    text: string;
    url: string;
}

class ShareManager {
    constructor() {
        this.init();
    }

    private init(): void {
        // Check if Web Share API is available
        if (!navigator.share) {
            console.log('Web Share API not available - share buttons will still be visible but may not function');
            // Don't hide buttons, just log the info
            // Users can still see the button and get feedback if they try to use it
        }

        // Attach handlers to footer share buttons
        this.attachShareHandlers();
    }

    private attachShareHandlers(): void {
        // Find all footer share buttons (.web-share-btn)
        const shareButtons = document.querySelectorAll<HTMLButtonElement>('.web-share-btn');
        
        shareButtons.forEach((button) => {
            // Get URL from data attribute
            const url = button.getAttribute('data-url');
            if (!url) return;

            // Extract metadata from the page/article
            const article = button.closest('article.h-entry');
            const metadata = this.extractContentMetadata(article, url);
            
            // Add click handler
            button.addEventListener('click', async (e) => {
                e.preventDefault();
                await this.shareContent(metadata, button);
            });
        });

        console.log(`✅ Attached share handlers to ${shareButtons.length} buttons`);
    }

    private extractContentMetadata(article: Element | null, url: string): ShareMetadata {
        const metadata: ShareMetadata = {
            title: '',
            text: '',
            url: ''
        };

        // Get title
        if (article) {
            const titleElement = article.querySelector<HTMLElement>('.p-name, .post-title, h1, h2');
            if (titleElement) {
                metadata.title = titleElement.textContent?.trim() || '';
            }

            // Get summary/text
            const summaryElement = article.querySelector<HTMLElement>('.p-summary, .post-content, .e-content');
            if (summaryElement) {
                const text = summaryElement.textContent?.trim() || '';
                // Truncate to reasonable length for sharing
                metadata.text = text.length > 200 ? text.substring(0, 197) + '...' : text;
            }
        } else {
            // Use page title as fallback
            metadata.title = document.title;
        }

        // Use provided URL (ensure absolute)
        if (url) {
            metadata.url = new URL(url, window.location.origin).href;
        } else {
            metadata.url = window.location.href;
        }

        return metadata;
    }

    private async shareContent(metadata: ShareMetadata, button: HTMLButtonElement): Promise<void> {
        // Check if Web Share API is available at time of use
        if (!navigator.share) {
            console.warn('Web Share API not available');
            this.showShareError(button);
            return;
        }
        
        try {
            // Prepare share data
            const shareData: ShareData = {
                title: metadata.title,
                url: metadata.url
            };

            // Only include text if it's meaningful
            if (metadata.text && metadata.text.length > 20) {
                shareData.text = metadata.text;
            }

            // Use Web Share API
            await navigator.share(shareData);

            // Show success feedback
            this.showShareSuccess(button);

        } catch (err) {
            // User cancelled or error occurred
            if (err instanceof Error && err.name === 'AbortError') {
                // User cancelled - this is normal, don't show error
                console.log('Share cancelled by user');
            } else {
                console.error('Error sharing:', err);
                this.showShareError(button);
            }
        }
    }

    private showShareSuccess(button: HTMLButtonElement): void {
        const originalTitle = button.getAttribute('title') || '';
        const iconSpan = button.querySelector<HTMLSpanElement>('.button-icon');
        const labelSpan = button.querySelector<HTMLSpanElement>('.button-label');
        const originalIcon = iconSpan ? iconSpan.textContent : '🔗';
        const originalLabel = labelSpan ? labelSpan.textContent : 'Share';
        
        // Update button appearance
        button.classList.add('shared');
        button.setAttribute('title', 'Shared!');
        
        // Update icon and label
        if (iconSpan) iconSpan.textContent = '✓';
        if (labelSpan) labelSpan.textContent = 'Shared!';

        // Reset after 2 seconds
        setTimeout(() => {
            button.classList.remove('shared');
            button.setAttribute('title', originalTitle);
            if (iconSpan) iconSpan.textContent = originalIcon;
            if (labelSpan) labelSpan.textContent = originalLabel;
        }, 2000);
    }

    private showShareError(button: HTMLButtonElement): void {
        const originalTitle = button.getAttribute('title') || '';
        const iconSpan = button.querySelector<HTMLSpanElement>('.button-icon');
        const labelSpan = button.querySelector<HTMLSpanElement>('.button-label');
        const originalIcon = iconSpan ? iconSpan.textContent : '🔗';
        const originalLabel = labelSpan ? labelSpan.textContent : 'Share';
        
        // Update button to show error
        button.classList.add('share-error');
        button.setAttribute('title', 'Share failed');
        
        // Update icon and label
        if (iconSpan) iconSpan.textContent = '✗';
        if (labelSpan) labelSpan.textContent = 'Failed';

        // Reset after 2 seconds
        setTimeout(() => {
            button.classList.remove('share-error');
            button.setAttribute('title', originalTitle);
            if (iconSpan) iconSpan.textContent = originalIcon;
            if (labelSpan) labelSpan.textContent = originalLabel;
        }, 2000);
    }
}

// Initialize when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        new ShareManager();
    });
} else {
    // DOM already loaded
    new ShareManager();
}
