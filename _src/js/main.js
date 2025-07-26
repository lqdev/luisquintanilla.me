// Desert Theme Navigation System - Minimal Navigation
// Clean navigation without content filtering - filters will be on homepage

// Theme Management
function toggleTheme() {
    const currentTheme = document.documentElement.getAttribute('data-theme');
    const newTheme = currentTheme === 'dark' ? 'light' : 'dark';
    document.documentElement.setAttribute('data-theme', newTheme);
    
    const themeIcon = document.getElementById('theme-toggle-icon');
    if (themeIcon) {
        themeIcon.innerHTML = newTheme === 'dark' ? '🌙' : '☀️';
    }
    
    localStorage.setItem('theme', newTheme);
}

function applyTheme(theme) {
    document.documentElement.setAttribute('data-theme', theme);
    const themeIcon = document.getElementById('theme-toggle-icon');
    if (themeIcon) {
        themeIcon.innerHTML = theme === 'dark' ? '🌙' : '☀️';
    }
}

function initializeTheme() {
    const savedTheme = localStorage.getItem('theme');
    const systemTheme = window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    const theme = savedTheme || systemTheme;
    applyTheme(theme);
}

// Mobile Navigation
function toggleMobileNav() {
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

// Event Listeners Setup
function setupEventListeners() {
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
        
        // Close nav when clicking regular nav links
        link.addEventListener('click', () => {
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
    initializeTheme();
});

// Handle theme changes from system preferences
window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', (e) => {
    if (!localStorage.getItem('theme')) {
        applyTheme(e.matches ? 'dark' : 'light');
    }
});

// Legacy function for backward compatibility (if needed)
function switchTheme() {
    toggleTheme();
}