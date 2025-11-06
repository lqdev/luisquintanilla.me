/**
 * UFO Cursor Enhancement - Dynamic Direction-Based Tilting
 * Phase 1: Movement-based cursor rotation
 *
 * Tracks horizontal mouse movement and applies appropriate UFO cursor tilt:
 * - Moving left: UFO tilts left (-25°)
 * - Moving right: UFO tilts right (+25°)
 *
 * Performance optimized with throttling to prevent excessive DOM updates
 */
class UfoCursorManager {
    constructor() {
        this.lastX = 0;
        this.tiltDirection = 'left'; // Default tilt direction
        this.isInitialized = false;
        // Performance optimization: throttle interval in ms
        this.throttleDelay = 50; // Update every 50ms max
        this.throttleTimeout = null;
        // Store bound event handler for proper cleanup
        this.boundHandleMouseMove = null;
    }
    /**
     * Initialize the UFO cursor tracking
     */
    init() {
        if (this.isInitialized) {
            return;
        }
        // Check if user prefers reduced motion
        const prefersReducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        if (prefersReducedMotion) {
            // Skip dynamic cursor for users who prefer reduced motion
            console.log('UFO Cursor: Reduced motion preference detected, skipping dynamic cursor');
            return;
        }
        // Check if device has a mouse pointer (not touch-only)
        const hasMousePointer = window.matchMedia('(pointer: fine)').matches;
        if (!hasMousePointer) {
            // Skip on touch-only devices
            console.log('UFO Cursor: Touch device detected, skipping dynamic cursor');
            return;
        }
        // Add event listener with throttling
        this.boundHandleMouseMove = (e) => this.handleMouseMove(e);
        document.addEventListener('mousemove', this.boundHandleMouseMove);
        this.isInitialized = true;
        console.log('UFO Cursor: Dynamic tilting initialized');
    }
    /**
     * Handle mouse movement with throttling
     * @param e - Mouse event
     */
    handleMouseMove(e) {
        // Throttle updates for performance
        if (this.throttleTimeout) {
            return;
        }
        this.throttleTimeout = window.setTimeout(() => {
            this.updateCursorTilt(e.clientX);
            this.throttleTimeout = null;
        }, this.throttleDelay);
    }
    /**
     * Update cursor tilt based on horizontal movement
     * @param currentX - Current mouse X position
     */
    updateCursorTilt(currentX) {
        // Determine direction based on horizontal movement
        if (currentX < this.lastX) {
            // Moving left
            if (this.tiltDirection !== 'left') {
                this.tiltDirection = 'left';
                document.body.classList.remove('ufo-tilt-right');
            }
        }
        else if (currentX > this.lastX) {
            // Moving right
            if (this.tiltDirection !== 'right') {
                this.tiltDirection = 'right';
                document.body.classList.add('ufo-tilt-right');
            }
        }
        // Update last position for next comparison
        this.lastX = currentX;
    }
    /**
     * Reset cursor to default state
     */
    reset() {
        this.tiltDirection = 'left';
        document.body.classList.remove('ufo-tilt-right');
        this.lastX = 0;
    }
    /**
     * Cleanup event listeners
     */
    destroy() {
        if (this.throttleTimeout) {
            clearTimeout(this.throttleTimeout);
        }
        if (this.boundHandleMouseMove) {
            document.removeEventListener('mousemove', this.boundHandleMouseMove);
            this.boundHandleMouseMove = null;
        }
        this.reset();
        this.isInitialized = false;
    }
}
// Initialize on DOM content loaded
if (typeof document !== 'undefined') {
    // Create global instance
    const ufoCursor = new UfoCursorManager();
    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', () => {
            ufoCursor.init();
        });
    }
    else {
        // DOM already loaded
        ufoCursor.init();
    }
    // Export for potential external use
    window.UfoCursorManager = UfoCursorManager;
    window.ufoCursor = ufoCursor;
}
export {};
//# sourceMappingURL=ufo-cursor.js.map