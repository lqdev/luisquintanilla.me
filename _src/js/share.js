/**
 * Web Share API - Native Content Sharing
 * Adds share buttons to posts, notes, and other content
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
            document.querySelectorAll('.share-btn').forEach(btn => {
                btn.style.display = 'none';
            });
            return;
        }

        // Add share buttons to content
        this.addShareButtons();
    }

    addShareButtons() {
        // Find all article elements (posts, notes, etc.)
        const articles = document.querySelectorAll('article.h-entry');
        
        articles.forEach((article) => {
            // Skip if already has a share button
            if (article.querySelector('.share-btn')) {
                return;
            }

            // Extract content metadata
            const metadata = this.extractContentMetadata(article);
            
            if (metadata.url) {
                const shareButton = this.createShareButton(metadata);
                
                // Find appropriate place to insert button
                const header = article.querySelector('.post-header, .card-header');
                if (header) {
                    // Create share button container if it doesn't exist
                    let shareContainer = header.querySelector('.share-container');
                    if (!shareContainer) {
                        shareContainer = document.createElement('div');
                        shareContainer.className = 'share-container';
                        header.appendChild(shareContainer);
                    }
                    shareContainer.appendChild(shareButton);
                }
            }
        });

        console.log(`✅ Added share buttons to ${articles.length} articles`);
    }

    extractContentMetadata(article) {
        const metadata = {
            title: '',
            text: '',
            url: ''
        };

        // Get title
        const titleElement = article.querySelector('.p-name, .card-title, h1, h2');
        if (titleElement) {
            metadata.title = titleElement.textContent.trim();
        }

        // Get URL
        const urlElement = article.querySelector('.u-url, a[href]');
        if (urlElement) {
            const href = urlElement.getAttribute('href');
            // Ensure absolute URL
            if (href) {
                metadata.url = new URL(href, window.location.origin).href;
            }
        } else {
            // Use current page URL as fallback
            metadata.url = window.location.href;
        }

        // Get summary/text
        const summaryElement = article.querySelector('.p-summary, .card-content, .e-content');
        if (summaryElement) {
            const text = summaryElement.textContent.trim();
            // Truncate to reasonable length for sharing
            metadata.text = text.length > 200 ? text.substring(0, 197) + '...' : text;
        }

        return metadata;
    }

    createShareButton(metadata) {
        const button = document.createElement('button');
        button.className = 'share-btn';
        button.setAttribute('aria-label', `Share: ${metadata.title}`);
        button.setAttribute('type', 'button');
        button.setAttribute('title', 'Share this content');
        
        // Use SVG icon for share
        button.innerHTML = `
            <svg class="share-icon" viewBox="0 0 16 16" fill="currentColor" aria-hidden="true">
                <path d="M13.5 1a1.5 1.5 0 1 0 0 3 1.5 1.5 0 0 0 0-3zM11 2.5a2.5 2.5 0 1 1 .603 1.628l-6.718 3.12a2.499 2.499 0 0 1 0 1.504l6.718 3.12a2.5 2.5 0 1 1-.488.876l-6.718-3.12a2.5 2.5 0 1 1 0-3.256l6.718-3.12A2.5 2.5 0 0 1 11 2.5zm-8.5 4a1.5 1.5 0 1 0 0 3 1.5 1.5 0 0 0 0-3zm11 5.5a1.5 1.5 0 1 0 0 3 1.5 1.5 0 0 0 0-3z"/>
            </svg>
            <span class="share-text">Share</span>
        `;

        // Add click handler
        button.addEventListener('click', async (e) => {
            e.preventDefault();
            await this.shareContent(metadata, button);
        });

        return button;
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
        const originalHTML = button.innerHTML;
        const originalClass = button.className;
        
        // Update button appearance
        button.classList.add('shared');
        
        // Change icon to checkmark
        button.innerHTML = `
            <svg class="share-icon" viewBox="0 0 16 16" fill="currentColor" aria-hidden="true">
                <path d="M13.854 3.646a.5.5 0 0 1 0 .708l-7 7a.5.5 0 0 1-.708 0l-3.5-3.5a.5.5 0 1 1 .708-.708L6.5 10.293l6.646-6.647a.5.5 0 0 1 .708 0z"/>
            </svg>
            <span class="share-text">Shared!</span>
        `;

        // Reset after 2 seconds
        setTimeout(() => {
            button.className = originalClass;
            button.innerHTML = originalHTML;
        }, 2000);
    }

    showShareError(button) {
        const originalHTML = button.innerHTML;
        const originalClass = button.className;
        
        // Update button to show error
        button.classList.add('share-error');
        
        button.innerHTML = `
            <svg class="share-icon" viewBox="0 0 16 16" fill="currentColor" aria-hidden="true">
                <path d="M8 15A7 7 0 1 1 8 1a7 7 0 0 1 0 14zm0 1A8 8 0 1 0 8 0a8 8 0 0 0 0 16z"/>
                <path d="M4.646 4.646a.5.5 0 0 1 .708 0L8 7.293l2.646-2.647a.5.5 0 0 1 .708.708L8.707 8l2.647 2.646a.5.5 0 0 1-.708.708L8 8.707l-2.646 2.647a.5.5 0 0 1-.708-.708L7.293 8 4.646 5.354a.5.5 0 0 1 0-.708z"/>
            </svg>
            <span class="share-text">Failed</span>
        `;

        // Reset after 2 seconds
        setTimeout(() => {
            button.className = originalClass;
            button.innerHTML = originalHTML;
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
