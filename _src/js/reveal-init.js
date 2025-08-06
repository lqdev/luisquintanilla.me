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
