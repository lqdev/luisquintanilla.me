/**
 * Service Worker - PWA Offline Support
 * Implements intelligent caching strategy for static site
 * Progressive enhancement with graceful degradation
 */

const CACHE_VERSION = 'v1.0.0';
const CACHE_NAME = `luisquintanilla-${CACHE_VERSION}`;

// Cache strategies
const CACHE_STRATEGIES = {
    // Static assets - cache first, network fallback
    STATIC: 'static',
    // Content pages - network first, cache fallback (for freshness)
    CONTENT: 'content',
    // Images - cache first with stale-while-revalidate
    IMAGES: 'images',
    // API/JSON - network first with timeout
    API: 'api'
};

// Files to cache immediately on install
const STATIC_CACHE_URLS = [
    '/',
    '/about',
    '/contact',
    '/search',
    '/feed',
    '/offline.html', // Fallback page for offline
    '/assets/css/main.css',
    '/assets/js/main.js',
    '/assets/js/timeline.js',
    '/assets/js/clipboard.js',
    '/assets/js/share.js',
    '/assets/js/lazy-images.js',
    '/avatar.png',
    '/manifest.json'
];

// URL patterns for different cache strategies
const URL_PATTERNS = {
    static: [
        /\/assets\/css\//,
        /\/assets\/js\//,
        /\/assets\/lib\//,
        /\.css$/,
        /\.js$/,
        /\.woff2?$/,
        /\.ttf$/,
        /\.otf$/
    ],
    images: [
        /\.png$/,
        /\.jpg$/,
        /\.jpeg$/,
        /\.gif$/,
        /\.svg$/,
        /\.webp$/,
        /\.ico$/
    ],
    api: [
        /\.json$/,
        /\/api\//
    ]
};

// Maximum cache sizes
const MAX_CACHE_SIZES = {
    static: 50,    // 50 static assets
    content: 100,  // 100 content pages
    images: 150,   // 150 images
    api: 25        // 25 API/JSON responses
};

/**
 * Install Event - Cache static assets
 */
self.addEventListener('install', (event) => {
    console.log('[Service Worker] Installing...');
    
    event.waitUntil(
        caches.open(CACHE_NAME)
            .then((cache) => {
                console.log('[Service Worker] Caching static assets');
                return cache.addAll(STATIC_CACHE_URLS);
            })
            .then(() => {
                console.log('[Service Worker] Installation complete');
                // Force activation immediately
                return self.skipWaiting();
            })
            .catch((error) => {
                console.error('[Service Worker] Installation failed:', error);
            })
    );
});

/**
 * Activate Event - Clean up old caches
 */
self.addEventListener('activate', (event) => {
    console.log('[Service Worker] Activating...');
    
    event.waitUntil(
        caches.keys()
            .then((cacheNames) => {
                return Promise.all(
                    cacheNames
                        .filter((cacheName) => {
                            // Delete old cache versions
                            return cacheName.startsWith('luisquintanilla-') && cacheName !== CACHE_NAME;
                        })
                        .map((cacheName) => {
                            console.log('[Service Worker] Deleting old cache:', cacheName);
                            return caches.delete(cacheName);
                        })
                );
            })
            .then(() => {
                console.log('[Service Worker] Activation complete');
                // Claim all clients immediately
                return self.clients.claim();
            })
    );
});

/**
 * Fetch Event - Intercept network requests
 */
self.addEventListener('fetch', (event) => {
    const { request } = event;
    const url = new URL(request.url);
    
    // Skip cross-origin requests
    if (url.origin !== location.origin) {
        return;
    }
    
    // Skip chrome-extension and other non-http(s) requests
    if (!url.protocol.startsWith('http')) {
        return;
    }
    
    // Determine cache strategy based on URL
    const strategy = determineStrategy(url);
    
    // Apply appropriate caching strategy
    switch (strategy) {
        case CACHE_STRATEGIES.STATIC:
            event.respondWith(cacheFirst(request));
            break;
        case CACHE_STRATEGIES.CONTENT:
            event.respondWith(networkFirst(request));
            break;
        case CACHE_STRATEGIES.IMAGES:
            event.respondWith(cacheFirstStaleWhileRevalidate(request));
            break;
        case CACHE_STRATEGIES.API:
            event.respondWith(networkFirstWithTimeout(request, 3000));
            break;
        default:
            event.respondWith(networkFirst(request));
    }
});

/**
 * Determine caching strategy based on URL
 */
function determineStrategy(url) {
    const pathname = url.pathname;
    
    // Check static assets
    if (URL_PATTERNS.static.some(pattern => pattern.test(pathname))) {
        return CACHE_STRATEGIES.STATIC;
    }
    
    // Check images
    if (URL_PATTERNS.images.some(pattern => pattern.test(pathname))) {
        return CACHE_STRATEGIES.IMAGES;
    }
    
    // Check API/JSON
    if (URL_PATTERNS.api.some(pattern => pattern.test(pathname))) {
        return CACHE_STRATEGIES.API;
    }
    
    // Default to content strategy for HTML pages
    return CACHE_STRATEGIES.CONTENT;
}

/**
 * Cache First Strategy
 * Try cache first, fall back to network
 */
async function cacheFirst(request) {
    const cache = await caches.open(CACHE_NAME);
    const cached = await cache.match(request);
    
    if (cached) {
        console.log('[Service Worker] Cache hit:', request.url);
        return cached;
    }
    
    console.log('[Service Worker] Cache miss, fetching:', request.url);
    try {
        const response = await fetch(request);
        
        // Cache successful responses
        if (response.ok) {
            cache.put(request, response.clone());
        }
        
        return response;
    } catch (error) {
        console.error('[Service Worker] Fetch failed:', error);
        return new Response('Offline - content not available', {
            status: 503,
            statusText: 'Service Unavailable'
        });
    }
}

/**
 * Network First Strategy
 * Try network first, fall back to cache
 */
async function networkFirst(request) {
    const cache = await caches.open(CACHE_NAME);
    
    try {
        const response = await fetch(request);
        
        // Cache successful responses
        if (response.ok) {
            console.log('[Service Worker] Network success, caching:', request.url);
            cache.put(request, response.clone());
        }
        
        return response;
    } catch (error) {
        console.log('[Service Worker] Network failed, trying cache:', request.url);
        const cached = await cache.match(request);
        
        if (cached) {
            console.log('[Service Worker] Serving from cache:', request.url);
            return cached;
        }
        
        // Return offline page for HTML requests
        if (request.headers.get('accept').includes('text/html')) {
            const offlinePage = await cache.match('/offline.html');
            if (offlinePage) {
                return offlinePage;
            }
        }
        
        return new Response('Offline - content not available', {
            status: 503,
            statusText: 'Service Unavailable',
            headers: { 'Content-Type': 'text/plain' }
        });
    }
}

/**
 * Cache First with Stale-While-Revalidate
 * Return cached version immediately, update cache in background
 */
async function cacheFirstStaleWhileRevalidate(request) {
    const cache = await caches.open(CACHE_NAME);
    const cached = await cache.match(request);
    
    // Start network fetch in background
    const fetchPromise = fetch(request).then((response) => {
        if (response.ok) {
            cache.put(request, response.clone());
        }
        return response;
    });
    
    // Return cached version immediately if available
    if (cached) {
        console.log('[Service Worker] Serving cached, revalidating:', request.url);
        return cached;
    }
    
    // Otherwise wait for network
    console.log('[Service Worker] No cache, waiting for network:', request.url);
    return fetchPromise;
}

/**
 * Network First with Timeout
 * Try network with timeout, fall back to cache
 */
async function networkFirstWithTimeout(request, timeout = 3000) {
    const cache = await caches.open(CACHE_NAME);
    
    try {
        // Race between fetch and timeout
        const controller = new AbortController();
        const timeoutId = setTimeout(() => controller.abort(), timeout);
        
        const response = await fetch(request, { signal: controller.signal });
        clearTimeout(timeoutId);
        
        if (response.ok) {
            cache.put(request, response.clone());
        }
        
        return response;
    } catch (error) {
        console.log('[Service Worker] Network timeout or failed, trying cache:', request.url);
        const cached = await cache.match(request);
        
        if (cached) {
            return cached;
        }
        
        return new Response(JSON.stringify({ error: 'Offline - data not available' }), {
            status: 503,
            statusText: 'Service Unavailable',
            headers: { 'Content-Type': 'application/json' }
        });
    }
}

/**
 * Clean up old cache entries
 * Called periodically to manage cache size
 */
async function cleanupCache(cacheName, maxSize) {
    const cache = await caches.open(cacheName);
    const keys = await cache.keys();
    
    if (keys.length > maxSize) {
        // Remove oldest entries (FIFO)
        const keysToDelete = keys.slice(0, keys.length - maxSize);
        await Promise.all(keysToDelete.map(key => cache.delete(key)));
        console.log(`[Service Worker] Cleaned up ${keysToDelete.length} old entries from ${cacheName}`);
    }
}

/**
 * Message Event - Handle messages from clients
 */
self.addEventListener('message', (event) => {
    if (event.data && event.data.type === 'SKIP_WAITING') {
        self.skipWaiting();
    }
    
    if (event.data && event.data.type === 'CACHE_URLS') {
        // Allow clients to request specific URLs to be cached
        const urls = event.data.urls || [];
        event.waitUntil(
            caches.open(CACHE_NAME)
                .then(cache => cache.addAll(urls))
                .then(() => {
                    console.log('[Service Worker] Cached requested URLs:', urls);
                })
        );
    }
});

console.log('[Service Worker] Loaded successfully');
