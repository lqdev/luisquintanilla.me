/**
 * Reveal.js Initialization
 * Initialize Reveal.js when a presentation container is found on the page
 */

// Make this a module
export {};

// Type declarations for Reveal.js (loaded from CDN)
interface RevealOptions {
    width?: number;
    height?: number;
    minScale?: number;
    maxScale?: number;
    hash?: boolean;
    controls?: boolean;
    progress?: boolean;
    center?: boolean;
    transition?: string;
    embedded?: boolean;
    plugins?: any[];
}

interface Reveal {
    initialize(options: RevealOptions): void;
}

declare global {
    const Reveal: Reveal | undefined;
    const RevealMarkdown: any;
    const RevealHighlight: any;
}

// Initialize Reveal.js when a presentation container is found on the page
document.addEventListener('DOMContentLoaded', function() {
    const presentationContainer = document.querySelector('.presentation-container');
    if (presentationContainer && typeof Reveal !== 'undefined') {
        Reveal.initialize({
            width: 800,
            height: 600,
            minScale: 0.5,
            maxScale: 1.0,
            hash: true,
            controls: true,
            progress: true,
            center: false,
            transition: 'slide',
            embedded: true,
            plugins: [RevealMarkdown, RevealHighlight]
        });
    }
});
