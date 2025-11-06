/**
 * Desert Theme Navigation System - Minimal Navigation
 * Clean navigation without content filtering - filters will be on homepage
 */

// Make this a module
export {};

// Theme Management
function toggleTheme(): void {
    const currentTheme = document.documentElement.getAttribute('data-theme') || document.body.getAttribute('data-theme');
    const newTheme = currentTheme === 'dark' ? 'light' : 'dark';
    document.documentElement.setAttribute('data-theme', newTheme);
    document.body.setAttribute('data-theme', newTheme);
    
    const themeIcon = document.getElementById('theme-toggle-icon');
    if (themeIcon) {
        themeIcon.innerHTML = newTheme === 'dark' ? '🌙' : '☀️';
    }
    
    localStorage.setItem('theme', newTheme);
}

function applyTheme(theme: string): void {
    // Apply theme to both documentElement and body for broader compatibility
    document.documentElement.setAttribute('data-theme', theme);
    document.body.setAttribute('data-theme', theme);
    const themeIcon = document.getElementById('theme-toggle-icon');
    if (themeIcon) {
        themeIcon.innerHTML = theme === 'dark' ? '🌙' : '☀️';
    }
}

function initializeTheme(): void {
    const savedTheme = localStorage.getItem('theme');
    const systemTheme = window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    const theme = savedTheme || systemTheme;
    applyTheme(theme);
}

// Mobile Navigation
function toggleMobileNav(): void {
    const nav = document.getElementById('sidebar-menu');
    const overlay = document.getElementById('nav-overlay');
    const toggle = document.getElementById('mobile-nav-toggle');
    const hamburger = toggle?.querySelector('.hamburger');
    
    if (nav && overlay && toggle) {
        const isOpen = nav.classList.contains('active');
        
        if (isOpen) {
            nav.classList.remove('active');
            overlay.classList.remove('active');
            toggle.classList.remove('active');
            hamburger?.classList.remove('active');
            toggle.setAttribute('aria-expanded', 'false');
            document.body.style.overflow = '';
        } else {
            nav.classList.add('active');
            overlay.classList.add('active');
            toggle.classList.add('active');
            hamburger?.classList.add('active');
            toggle.setAttribute('aria-expanded', 'true');
            document.body.style.overflow = 'hidden';
        }
    }
}

// Navigation Dropdown Management
function toggleDropdown(dropdownId: string): void {
    const dropdown = document.getElementById(dropdownId);
    const toggle = document.querySelector<HTMLElement>(`[data-target="${dropdownId}"]`);
    
    if (dropdown && toggle) {
        const isOpen = dropdown.classList.contains('show');
        
        // Close all other dropdowns first
        document.querySelectorAll('.dropdown-menu.show').forEach(menu => {
            if (menu.id !== dropdownId) {
                menu.classList.remove('show');
                const otherToggle = document.querySelector<HTMLElement>(`[data-target="${menu.id}"]`);
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
}

function setupDropdownListeners(): void {
    // Collections dropdown
    const collectionsToggle = document.querySelector('[data-target="collections-dropdown"]');
    if (collectionsToggle) {
        collectionsToggle.addEventListener('click', (e) => {
            e.preventDefault();
            toggleDropdown('collections-dropdown');
        });
    }
    
    // Resources dropdown
    const resourcesToggle = document.querySelector('[data-target="resources-dropdown"]');
    if (resourcesToggle) {
        resourcesToggle.addEventListener('click', (e) => {
            e.preventDefault();
            toggleDropdown('resources-dropdown');
        });
    }
    
    // Close dropdowns when clicking outside
    document.addEventListener('click', (e) => {
        const target = e.target as HTMLElement;
        if (!target.closest('.nav-section.dropdown')) {
            document.querySelectorAll('.dropdown-menu.show').forEach(menu => {
                menu.classList.remove('show');
                const toggle = document.querySelector<HTMLElement>(`[data-target="${menu.id}"]`);
                if (toggle) {
                    toggle.setAttribute('aria-expanded', 'false');
                }
            });
        }
    });
    
    // Close mobile nav when clicking dropdown items (actual navigation links)
    const dropdownItems = document.querySelectorAll('.dropdown-item');
    dropdownItems.forEach(item => {
        item.addEventListener('click', () => {
            const nav = document.getElementById('sidebar-menu');
            if (nav?.classList.contains('active')) {
                toggleMobileNav();
            }
        });
    });
}

// Event Listeners Setup
function setupEventListeners(): void {
    // Theme toggle
    const themeToggle = document.getElementById('theme-toggle');
    if (themeToggle) {
        themeToggle.addEventListener('click', toggleTheme);
    }
    
    // Mobile navigation toggle
    const mobileToggle = document.getElementById('mobile-nav-toggle');
    if (mobileToggle) {
        mobileToggle.addEventListener('click', toggleMobileNav);
    }
    
    // Navigation overlay click to close
    const overlay = document.getElementById('nav-overlay');
    if (overlay) {
        overlay.addEventListener('click', toggleMobileNav);
    }
    
    // Keyboard navigation
    document.addEventListener('keydown', (e) => {
        // Escape key closes mobile nav
        if (e.key === 'Escape') {
            const nav = document.getElementById('sidebar-menu');
            if (nav?.classList.contains('active')) {
                toggleMobileNav();
            }
        }
        
        // Alt + T for theme toggle
        if (e.altKey && e.key === 't') {
            e.preventDefault();
            toggleTheme();
        }
    });
    
    // Focus management for accessibility
    const navLinks = document.querySelectorAll('.nav-link');
    navLinks.forEach(link => {
        link.addEventListener('focus', () => {
            link.classList.add('focused');
        });
        
        link.addEventListener('blur', () => {
            link.classList.remove('focused');
        });
        
        // Close nav when clicking regular nav links (but not dropdown toggles)
        link.addEventListener('click', (e) => {
            const target = e.target as HTMLElement;
            // Don't close nav if this is a dropdown toggle button
            // Check both the clicked element and its parent in case we clicked a child element (icon, text, etc.)
            if (link.classList.contains('dropdown-toggle') || target.closest('.dropdown-toggle')) {
                return;
            }
            
            const nav = document.getElementById('sidebar-menu');
            if (nav?.classList.contains('active')) {
                toggleMobileNav();
            }
        });
    });
}

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    setupEventListeners();
    setupDropdownListeners();
    initializeTheme();
    setupCopyToClipboard();
    setupWebShare();
});

// Handle theme changes from system preferences
window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', (e) => {
    if (!localStorage.getItem('theme')) {
        applyTheme(e.matches ? 'dark' : 'light');
    }
});

// Copy to Clipboard Functionality
function setupCopyToClipboard(): void {
    document.querySelectorAll<HTMLButtonElement>('.copy-permalink-btn').forEach(button => {
        button.addEventListener('click', async (e) => {
            e.preventDefault();
            
            const relativeUrl = button.dataset.url;
            if (!relativeUrl) return;
            
            const url = window.location.origin + relativeUrl;
            const iconSpan = button.querySelector<HTMLSpanElement>('.button-icon');
            const labelSpan = button.querySelector<HTMLSpanElement>('.button-label');
            const originalIcon = iconSpan ? iconSpan.textContent : '📋';
            const originalLabel = labelSpan ? labelSpan.textContent : 'Copy';
            
            try {
                // Use modern Clipboard API if available
                if (navigator.clipboard && window.isSecureContext) {
                    await navigator.clipboard.writeText(url);
                } else {
                    // Fallback for older browsers
                    const textArea = document.createElement('textarea');
                    textArea.value = url;
                    textArea.style.position = 'fixed';
                    textArea.style.left = '-999999px';
                    textArea.style.top = '-999999px';
                    document.body.appendChild(textArea);
                    textArea.focus();
                    textArea.select();
                    document.execCommand('copy');
                    textArea.remove();
                }
                
                // Visual feedback - change icon and label temporarily
                button.classList.add('copied');
                if (iconSpan) iconSpan.textContent = '✓';
                if (labelSpan) labelSpan.textContent = 'Copied!';
                button.title = 'Copied!';
                
                // Reset after 2 seconds
                setTimeout(() => {
                    button.classList.remove('copied');
                    if (iconSpan) iconSpan.textContent = originalIcon;
                    if (labelSpan) labelSpan.textContent = originalLabel;
                    button.title = 'Copy to clipboard';
                }, 2000);
                
            } catch (err) {
                console.warn('Failed to copy to clipboard:', err);
                // Visual feedback for error
                button.classList.add('copy-error');
                if (iconSpan) iconSpan.textContent = '✗';
                if (labelSpan) labelSpan.textContent = 'Failed';
                button.title = 'Copy failed';
                
                setTimeout(() => {
                    button.classList.remove('copy-error');
                    if (iconSpan) iconSpan.textContent = originalIcon;
                    if (labelSpan) labelSpan.textContent = originalLabel;
                    button.title = 'Copy to clipboard';
                }, 2000);
            }
        });
    });
}

// Legacy function for backward compatibility (if needed)
function switchTheme(): void {
    toggleTheme();
}

// Extend Window interface for backward compatibility
declare global {
    interface Window {
        switchTheme?: () => void;
    }
}

// Export for backward compatibility if needed
window.switchTheme = switchTheme;

// Web Share API Functionality
function setupWebShare(): void {
    // Check if Web Share API is supported
    if (!navigator.share) {
        console.info('Web Share API not supported in this browser');
        return;
    }

    // Add keyboard shortcut for sharing (Ctrl/Cmd + Shift + S)
    document.addEventListener('keydown', async (e) => {
        if ((e.ctrlKey || e.metaKey) && e.shiftKey && e.key === 'S') {
            e.preventDefault();
            await shareCurrentPage();
        }
    });

    // Setup share buttons if they exist
    document.querySelectorAll<HTMLButtonElement>('.web-share-btn').forEach(button => {
        button.addEventListener('click', async (e) => {
            e.preventDefault();
            const relativeUrl = button.dataset.url;
            await shareContent(relativeUrl || null);
        });
    });
}

async function shareContent(relativeUrl: string | null): Promise<void> {
    try {
        // Get the page title
        const title = document.title;

        // Get selected text if any
        const selection = window.getSelection();
        const selectedText = selection?.toString().trim() || '';

        // Use selected text or default message
        const text = selectedText || 'I found this interesting';

        // Build full URL from relative URL
        const url = relativeUrl ? window.location.origin + relativeUrl : window.location.href;

        // Share using Web Share API
        await navigator.share({
            title: title,
            text: text,
            url: url
        });

        console.log('Content shared successfully');
    } catch (err) {
        // User cancelled or share failed
        if (err instanceof Error && err.name !== 'AbortError') {
            console.warn('Error sharing:', err);
        }
    }
}

async function shareCurrentPage(): Promise<void> {
    // Share the current page without a specific URL (uses window.location.href)
    await shareContent(null);
}
