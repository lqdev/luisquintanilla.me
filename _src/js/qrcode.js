/**
 * QR Code Generator - Universal QR Code Button
 * Generates QR codes for page URLs with modal interface
 * Progressive enhancement with feature detection
 */

class QRCodeManager {
    constructor() {
        this.modal = null;
        this.qrCode = null;
        this.currentUrl = null;
        this.init();
    }

    init() {
        // Check if QRCodeStyling library is available
        if (typeof QRCodeStyling === 'undefined') {
            console.log('QRCodeStyling library not available - QR buttons will be hidden');
            this.hideQRButtons();
            return;
        }

        // Create modal structure
        this.createModal();

        // Attach handlers to QR code buttons
        this.attachQRHandlers();
    }

    hideQRButtons() {
        document.querySelectorAll('.qr-code-btn').forEach(btn => {
            btn.style.display = 'none';
        });
    }

    createModal() {
        // Check if modal already exists
        if (document.getElementById('qr-modal-overlay')) {
            this.modal = document.getElementById('qr-modal-overlay');
            return;
        }

        // Create modal HTML structure
        const modalHTML = `
            <div id="qr-modal-overlay" class="qr-modal-overlay">
                <div class="qr-modal">
                    <div class="qr-modal-header">
                        <h2 class="qr-modal-title">📱 Scan to Open</h2>
                        <button class="qr-modal-close" aria-label="Close modal" type="button">
                            <i class="bi bi-x"></i>
                        </button>
                    </div>
                    <div class="qr-url-display" id="qr-url-text"></div>
                    <div class="qr-code-container" id="qr-code-container">
                        <div class="qr-loading">
                            <div class="qr-loading-spinner"></div>
                            <p>Generating QR Code...</p>
                        </div>
                    </div>
                    <div class="qr-modal-actions">
                        <button class="qr-action-btn qr-copy-btn" id="qr-copy-url-btn" type="button">
                            <i class="bi bi-clipboard"></i>
                            Copy URL
                        </button>
                        <button class="qr-action-btn qr-download-btn" id="qr-download-btn" type="button">
                            <i class="bi bi-download"></i>
                            Download
                        </button>
                    </div>
                </div>
            </div>
        `;

        // Insert modal into document
        document.body.insertAdjacentHTML('beforeend', modalHTML);
        this.modal = document.getElementById('qr-modal-overlay');

        // Attach modal event listeners
        this.attachModalListeners();
    }

    attachModalListeners() {
        // Close button
        const closeBtn = this.modal.querySelector('.qr-modal-close');
        closeBtn.addEventListener('click', () => this.closeModal());

        // Click outside to close
        this.modal.addEventListener('click', (e) => {
            if (e.target === this.modal) {
                this.closeModal();
            }
        });

        // ESC key to close
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape' && this.modal.classList.contains('active')) {
                this.closeModal();
            }
        });

        // Copy URL button
        const copyBtn = document.getElementById('qr-copy-url-btn');
        copyBtn.addEventListener('click', () => this.copyURL());

        // Download button
        const downloadBtn = document.getElementById('qr-download-btn');
        downloadBtn.addEventListener('click', () => this.downloadQR());
    }

    attachQRHandlers() {
        // Find all QR code buttons
        const qrButtons = document.querySelectorAll('.qr-code-btn');
        
        qrButtons.forEach((button) => {
            // Get URL from data attribute
            const url = button.getAttribute('data-url');
            if (!url) return;

            // Add click handler
            button.addEventListener('click', (e) => {
                e.preventDefault();
                this.generateQRCode(url);
            });
        });

        console.log(`✅ Attached QR code handlers to ${qrButtons.length} buttons`);
    }

    async generateQRCode(relativeUrl) {
        // Convert relative URL to absolute
        const absoluteUrl = new URL(relativeUrl, window.location.origin).href;
        this.currentUrl = absoluteUrl;

        // Open modal
        this.openModal();

        // Update URL display
        document.getElementById('qr-url-text').textContent = absoluteUrl;

        // Clear previous QR code
        const container = document.getElementById('qr-code-container');
        container.innerHTML = `
            <div class="qr-loading">
                <div class="qr-loading-spinner"></div>
                <p>Generating QR Code...</p>
            </div>
        `;

        // Load avatar image with fallback
        const avatarImage = await this.loadAvatarImage();

        // Create QR code
        try {
            this.qrCode = new QRCodeStyling({
                width: 280,
                height: 280,
                data: absoluteUrl,
                image: avatarImage,
                dotsOptions: {
                    color: "#1a2332",
                    type: "rounded"
                },
                cornersSquareOptions: {
                    color: "#ff6b35",
                    type: "extra-rounded"
                },
                cornersDotOptions: {
                    color: "#ff6b35"
                },
                backgroundOptions: {
                    color: "#ffffff"
                },
                imageOptions: {
                    crossOrigin: "anonymous",
                    margin: 8,
                    imageSize: 0.25
                }
            });

            // Clear loading state and append QR code
            container.innerHTML = '';
            this.qrCode.append(container);

        } catch (error) {
            console.error('Error generating QR code:', error);
            container.innerHTML = '<p style="color: var(--text-color);">Error generating QR code</p>';
        }
    }

    async loadAvatarImage() {
        // Try to load avatar.png
        return new Promise((resolve) => {
            const img = new Image();
            img.crossOrigin = 'anonymous';
            
            img.onload = () => {
                resolve('/avatar.png');
            };

            img.onerror = () => {
                // Fallback to inline SVG
                console.log('Avatar image failed to load, using fallback SVG');
                resolve(this.generateFallbackSVG());
            };

            img.src = '/avatar.png';
        });
    }

    generateFallbackSVG(initial = 'L', bgColor = '#ff6b35') {
        const svg = `
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
                <circle cx="50" cy="50" r="45" fill="${bgColor}"/>
                <text x="50" y="68" font-family="Arial, sans-serif" font-size="45" font-weight="bold" fill="white" text-anchor="middle">${initial}</text>
            </svg>
        `;
        return 'data:image/svg+xml;base64,' + btoa(svg);
    }

    openModal() {
        this.modal.classList.add('active');
        document.body.style.overflow = 'hidden'; // Prevent background scrolling
    }

    closeModal() {
        this.modal.classList.remove('active');
        document.body.style.overflow = ''; // Restore scrolling
    }

    async copyURL() {
        const copyBtn = document.getElementById('qr-copy-url-btn');
        const originalHTML = copyBtn.innerHTML;

        try {
            await navigator.clipboard.writeText(this.currentUrl);
            
            // Show success feedback
            copyBtn.classList.add('copied');
            copyBtn.innerHTML = '<i class="bi bi-check"></i> Copied!';

            // Reset after 2 seconds
            setTimeout(() => {
                copyBtn.classList.remove('copied');
                copyBtn.innerHTML = originalHTML;
            }, 2000);

        } catch (error) {
            console.error('Failed to copy URL:', error);
            
            // Show error feedback
            copyBtn.innerHTML = '<i class="bi bi-x"></i> Failed';
            setTimeout(() => {
                copyBtn.innerHTML = originalHTML;
            }, 2000);
        }
    }

    downloadQR() {
        if (!this.qrCode) {
            console.error('No QR code to download');
            return;
        }

        try {
            this.qrCode.download({
                name: 'qr-code',
                extension: 'png'
            });
        } catch (error) {
            console.error('Failed to download QR code:', error);
        }
    }
}

// Initialize when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        new QRCodeManager();
    });
} else {
    // DOM already loaded
    new QRCodeManager();
}
