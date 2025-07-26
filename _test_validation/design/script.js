// Function to toggle theme
function toggleTheme() {
    const currentTheme = document.body.getAttribute('data-theme');
    const newTheme = currentTheme === 'dark' ? 'light' : 'dark';
    document.body.setAttribute('data-theme', newTheme);
    document.getElementById('theme-toggle-icon').innerHTML = newTheme === 'dark' ? '&#x1F31A;' : '&#x1F31E;'; // 🌜 Moon with face, 🌞 Sun with face
    localStorage.setItem('theme', newTheme);
}

// Function to apply theme
function applyTheme(theme) {
    document.body.setAttribute('data-theme', theme);
    document.getElementById('theme-toggle-icon').innerHTML = theme === 'dark' ? '&#x1F31A;' : '&#x1F31E;'; // 🌜 Moon with face, 🌞 Sun with face
}

// Function to initialize theme
function initializeTheme() {
    const savedTheme = localStorage.getItem('theme');
    const theme = savedTheme || (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
    applyTheme(theme);
}

// Apply theme as soon as possible
initializeTheme();

// let hasScrolled = false;

// Function to filter posts
function filterPosts(type) {
    const posts = document.querySelectorAll('.post');
    posts.forEach(post => {
        const postType = post.getAttribute('data-type');
        post.style.display = (type === 'all' || type === postType) ? 'block' : 'none';
    });
}

// Function to scroll to top
function scrollToTop() {
    window.scrollTo({ top: 0, behavior: 'smooth' });
}

// Function to load posts on scroll
function loadPostsOnScroll() {
    const cards = document.querySelectorAll('.card[data-loaded="false"]');
    const windowHeight = window.innerHeight;
    cards.forEach(card => {
        if (card.getBoundingClientRect().top < windowHeight) {
            card.setAttribute('data-loaded', 'true');
            card.style.display = 'block';
        }
    });
}

// Event listener for scroll event
window.addEventListener('scroll', () => {
    // if (!hasScrolled) {
    //     hasScrolled = true;
    //     // loadPostsOnScroll();
    // }
    const backToTopButton = document.getElementById('back-to-top');
    if (window.scrollY > 200) {
        backToTopButton.style.display = 'block';
    } else {
        backToTopButton.style.display = 'none';
    }
});

// Function to open modal
function openModal(img) {
    const modal = document.getElementById('modal');
    const modalImg = document.getElementById('modal-img');
    modal.style.display = 'block';
    modalImg.src = img.src.replace('200', '2000');
}

// Function to close modal
function closeModal() {
    const modal = document.getElementById('modal');
    modal.style.display = 'none';
}

// Event listener for closing modal when clicking outside the image
window.addEventListener('click', (event) => {
    const modal = document.getElementById('modal');
    const modalImg = document.getElementById('modal-img');
    if (event.target === modal) {
        closeModal();
    }
});

// Function to toggle sidebar menu
function toggleMenu() {
    const menu = document.getElementById('sidebar-menu');
    if (menu.classList.contains('show')) {
        menu.classList.remove('show');
    } else {
        menu.classList.add('show');
    }
}

// Function to initialize audio and video players
function initializeMediaPlayers() {
    document.querySelectorAll('audio, video').forEach(media => {
        media.addEventListener('play', () => {
            document.querySelectorAll('audio, video').forEach(otherMedia => {
                if (otherMedia !== media) {
                    otherMedia.pause();
                }
            });
        });
    });
}

// Function to setup common event listeners
function setupEventListeners() {
    document.getElementById('theme-toggle').addEventListener('click', toggleTheme);
    document.getElementById('back-to-top').addEventListener('click', scrollToTop);
    document.querySelector('.hamburger').addEventListener('click', toggleMenu);

    // Apply white text style to all buttons
    document.querySelectorAll('button').forEach((button) => {
        button.classList.add('button-style-1');
    });

    // Apply white text style to all nav links
    document.querySelectorAll('nav a').forEach((link) => {
        link.classList.add('button-style-1');
    });

    // Remove event listener to card elements
    // document.querySelectorAll('.post.card').forEach(card => {
    //     card.addEventListener('click', (event) => {
    //         const sourceUrl = card.querySelector('.source-url a');
    //         if (sourceUrl && event.target === sourceUrl) {
    //             window.open(sourceUrl.href, '_blank');
    //         } else {
    //             const permalink = card.querySelector('.card-footer a')?.getAttribute('href') || card.querySelector('.card-footer span').textContent;
    //             window.location.href = permalink;
    //         }
    //     });
    // });

    // Set default filter to 'all'
    filterPosts('all');
}

// Event listener for DOMContentLoaded event
document.addEventListener('DOMContentLoaded', () => {
    setupEventListeners();
    initializeMediaPlayers();
});