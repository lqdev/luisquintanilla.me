/**
 * Web Share API - Native Content Sharing
 * Attaches functionality to footer share buttons
 * Progressive enhancement with feature detection
 */

class ShareManager {
    constructor() {
        this.init();
    }

    init() {
        // Feature detection
        if (!navigator.share) {
            console.log('Web Share API not available - share buttons will be hidden');
            // Hide share buttons if they exist
            document.querySelectorAll('.web-share-btn').forEach(btn => {
                btn.style.display = 'none';
            });
            return;
        }

        // Attach handlers to footer share buttons
        this.attachShareHandlers();
    }

    attachShareHandlers() {
        // Find all footer share buttons (.web-share-btn)
        const shareButtons = document.querySelectorAll('.web-share-btn');
        
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

    extractContentMetadata(article, url) {
        const metadata = {
            title: '',
            text: '',
            url: ''
        };

        // Get title
        if (article) {
            const titleElement = article.querySelector('.p-name, .post-title, h1, h2');
            if (titleElement) {
                metadata.title = titleElement.textContent.trim();
            }

            // Get summary/text
            const summaryElement = article.querySelector('.p-summary, .post-content, .e-content');
            if (summaryElement) {
                const text = summaryElement.textContent.trim();
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

    async shareContent(metadata, button) {
        try {
            // Prepare share data
            const shareData = {
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
            if (err.name === 'AbortError') {
                // User cancelled - this is normal, don't show error
                console.log('Share cancelled by user');
            } else {
                console.error('Error sharing:', err);
                this.showShareError(button);
            }
        }
    }

    showShareSuccess(button) {
        const originalTitle = button.getAttribute('title');
        
        // Update button appearance
        button.classList.add('shared');
        button.setAttribute('title', 'Shared!');
        
        // Find icon element and update
        const icon = button.querySelector('.bi-share');
        if (icon) {
            icon.classList.remove('bi-share');
            icon.classList.add('bi-check');
        }

        // Reset after 2 seconds
        setTimeout(() => {
            button.classList.remove('shared');
            button.setAttribute('title', originalTitle);
            if (icon) {
                icon.classList.remove('bi-check');
                icon.classList.add('bi-share');
            }
        }, 2000);
    }

    showShareError(button) {
        const originalTitle = button.getAttribute('title');
        
        // Update button to show error
        button.classList.add('share-error');
        button.setAttribute('title', 'Share failed');
        
        // Find icon element and update
        const icon = button.querySelector('.bi-share');
        if (icon) {
            icon.classList.remove('bi-share');
            icon.classList.add('bi-x-circle');
        }

        // Reset after 2 seconds
        setTimeout(() => {
            button.classList.remove('share-error');
            button.setAttribute('title', originalTitle);
            if (icon) {
                icon.classList.remove('bi-x-circle');
                icon.classList.add('bi-share');
            }
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
