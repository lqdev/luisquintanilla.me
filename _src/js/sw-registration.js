/**
 * Service Worker Registration
 * Progressive Web App (PWA) capabilities
 * Progressive enhancement with feature detection
 */

class ServiceWorkerManager {
    constructor() {
        this.registration = null;
        this.updateAvailable = false;
        this.init();
    }

    async init() {
        // Check if service workers are supported
        if (!('serviceWorker' in navigator)) {
            console.log('[PWA] Service Workers not supported in this browser');
            return;
        }

        try {
            // Register service worker
            await this.registerServiceWorker();
            
            // Setup update handling
            this.setupUpdateHandling();
            
            // Check for updates periodically
            this.checkForUpdates();
            
            console.log('[PWA] Service Worker initialization complete');
        } catch (error) {
            console.error('[PWA] Service Worker initialization failed:', error);
        }
    }

    async registerServiceWorker() {
        try {
            this.registration = await navigator.serviceWorker.register('/service-worker.js', {
                scope: '/'
            });
            
            console.log('[PWA] Service Worker registered successfully:', this.registration.scope);
            
            // Log registration state
            if (this.registration.installing) {
                console.log('[PWA] Service Worker installing');
            } else if (this.registration.waiting) {
                console.log('[PWA] Service Worker waiting');
            } else if (this.registration.active) {
                console.log('[PWA] Service Worker active');
            }
            
            return this.registration;
        } catch (error) {
            console.error('[PWA] Service Worker registration failed:', error);
            throw error;
        }
    }

    setupUpdateHandling() {
        if (!this.registration) return;

        // Handle updates
        this.registration.addEventListener('updatefound', () => {
            console.log('[PWA] Service Worker update found');
            const newWorker = this.registration.installing;
            
            newWorker.addEventListener('statechange', () => {
                if (newWorker.state === 'installed' && navigator.serviceWorker.controller) {
                    // New service worker is ready
                    console.log('[PWA] New Service Worker ready');
                    this.updateAvailable = true;
                    this.showUpdateNotification();
                }
            });
        });

        // Handle controller change (after skipWaiting)
        navigator.serviceWorker.addEventListener('controllerchange', () => {
            console.log('[PWA] Service Worker controller changed');
            if (this.updateAvailable) {
                // Reload page to use new service worker
                window.location.reload();
            }
        });
    }

    showUpdateNotification() {
        // Check if user wants to see update notifications
        const hideNotifications = localStorage.getItem('pwa-hide-update-notifications') === 'true';
        if (hideNotifications) return;

        // Create update notification
        const notification = document.createElement('div');
        notification.className = 'pwa-update-notification';
        notification.innerHTML = `
            <div class="pwa-update-content">
                <p>A new version of this site is available.</p>
                <div class="pwa-update-actions">
                    <button class="pwa-update-btn" id="pwa-update-now">Update Now</button>
                    <button class="pwa-update-btn secondary" id="pwa-update-later">Later</button>
                </div>
            </div>
        `;

        // Add styles
        const styles = document.createElement('style');
        styles.textContent = `
            .pwa-update-notification {
                position: fixed;
                bottom: 20px;
                right: 20px;
                background: var(--bg-color, #2d4a5c);
                color: var(--text-color, #ffffff);
                padding: 16px 20px;
                border-radius: 8px;
                box-shadow: 0 4px 12px rgba(0,0,0,0.3);
                z-index: 10000;
                max-width: 400px;
                animation: slideIn 0.3s ease-out;
            }
            
            @keyframes slideIn {
                from {
                    transform: translateX(100%);
                    opacity: 0;
                }
                to {
                    transform: translateX(0);
                    opacity: 1;
                }
            }
            
            .pwa-update-content p {
                margin: 0 0 12px 0;
                font-size: 14px;
            }
            
            .pwa-update-actions {
                display: flex;
                gap: 8px;
            }
            
            .pwa-update-btn {
                flex: 1;
                padding: 8px 16px;
                border: none;
                border-radius: 4px;
                cursor: pointer;
                font-size: 14px;
                font-weight: 500;
                transition: all 0.2s;
            }
            
            .pwa-update-btn:not(.secondary) {
                background: #4a90e2;
                color: white;
            }
            
            .pwa-update-btn.secondary {
                background: rgba(255,255,255,0.1);
                color: var(--text-color, #ffffff);
            }
            
            .pwa-update-btn:hover {
                opacity: 0.9;
                transform: translateY(-1px);
            }
            
            @media (max-width: 480px) {
                .pwa-update-notification {
                    left: 20px;
                    right: 20px;
                    bottom: 20px;
                }
            }
        `;

        document.head.appendChild(styles);
        document.body.appendChild(notification);

        // Handle update button
        document.getElementById('pwa-update-now').addEventListener('click', () => {
            this.applyUpdate();
            notification.remove();
        });

        // Handle later button
        document.getElementById('pwa-update-later').addEventListener('click', () => {
            notification.remove();
        });

        // Auto-hide after 30 seconds
        setTimeout(() => {
            if (notification.parentElement) {
                notification.remove();
            }
        }, 30000);
    }

    applyUpdate() {
        if (!this.registration || !this.registration.waiting) {
            console.warn('[PWA] No service worker waiting to activate');
            return;
        }

        // Tell service worker to skip waiting and activate immediately
        this.registration.waiting.postMessage({ type: 'SKIP_WAITING' });
        
        console.log('[PWA] Applying update...');
    }

    checkForUpdates() {
        if (!this.registration) return;

        // Check for updates every hour
        setInterval(() => {
            console.log('[PWA] Checking for Service Worker updates...');
            this.registration.update().catch(error => {
                console.error('[PWA] Update check failed:', error);
            });
        }, 60 * 60 * 1000); // 1 hour
    }

    /**
     * Prefetch specific URLs for offline access
     */
    async prefetchUrls(urls) {
        if (!this.registration || !this.registration.active) {
            console.warn('[PWA] Service Worker not active, cannot prefetch');
            return;
        }

        // Send message to service worker to cache these URLs
        this.registration.active.postMessage({
            type: 'CACHE_URLS',
            urls: urls
        });

        console.log(`[PWA] Requested prefetch of ${urls.length} URLs`);
    }

    /**
     * Check if app is running in standalone mode (installed PWA)
     */
    isStandalone() {
        return (
            window.matchMedia('(display-mode: standalone)').matches ||
            window.navigator.standalone === true
        );
    }

    /**
     * Show install prompt if available
     * DISABLED: Custom install banner removed per user preference
     * Browsers provide native install prompts which are sufficient
     */
    setupInstallPrompt() {
        // Install banner functionality has been disabled
        // Users can still install the PWA via browser's native install prompt
        
        let deferredPrompt = null;

        window.addEventListener('beforeinstallprompt', (e) => {
            // Prevent default install prompt - but don't show custom banner
            e.preventDefault();
            deferredPrompt = e;

            console.log('[PWA] Install prompt available via browser');
        });

        // Track if app was installed
        window.addEventListener('appinstalled', () => {
            console.log('[PWA] App installed successfully');
            deferredPrompt = null;
        });
    }

    // showInstallPromotion method removed - custom install banner disabled
}

// Initialize when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        const swManager = new ServiceWorkerManager();
        // Install banner disabled - browser's native install prompt is sufficient
        // swManager.setupInstallPrompt();
        
        // Make available globally if needed
        window.swManager = swManager;
    });
} else {
    // DOM already loaded
    const swManager = new ServiceWorkerManager();
    // Install banner disabled - browser's native install prompt is sufficient
    // swManager.setupInstallPrompt();
    window.swManager = swManager;
}
