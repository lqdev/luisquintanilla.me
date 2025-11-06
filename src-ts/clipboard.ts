/**
 * Clipboard API - Code Snippet Copy Enhancement
 * Adds "Copy to Clipboard" buttons to all code blocks
 * Progressive enhancement with graceful degradation
 */

class ClipboardManager {
    private copiedTimeout: number | null = null;

    constructor() {
        this.init();
    }

    private init(): void {
        // Feature detection
        if (!navigator.clipboard) {
            console.log('Clipboard API not available - skipping code copy buttons');
            return;
        }

        // Add copy buttons to all code blocks
        this.addCopyButtonsToCodeBlocks();
    }

    private addCopyButtonsToCodeBlocks(): void {
        // Find all pre > code blocks (the pattern used in the site)
        const codeBlocks = document.querySelectorAll<HTMLElement>('pre > code');
        
        codeBlocks.forEach((codeElement) => {
            const preElement = codeElement.parentElement as HTMLPreElement;
            
            // Skip if already has a copy button
            if (preElement.querySelector('.code-copy-btn')) {
                return;
            }

            // Create wrapper div for positioning
            const wrapper = document.createElement('div');
            wrapper.className = 'code-block-wrapper';
            
            // Wrap the pre element
            preElement.parentNode?.insertBefore(wrapper, preElement);
            wrapper.appendChild(preElement);

            // Create copy button
            const copyButton = this.createCopyButton(codeElement);
            wrapper.appendChild(copyButton);
        });

        console.log(`✅ Added copy buttons to ${codeBlocks.length} code blocks`);
    }

    private createCopyButton(codeElement: HTMLElement): HTMLButtonElement {
        const button = document.createElement('button');
        button.className = 'code-copy-btn';
        button.setAttribute('aria-label', 'Copy code to clipboard');
        button.setAttribute('type', 'button');
        
        // Use SVG icon for copy
        button.innerHTML = `
            <svg class="copy-icon" viewBox="0 0 16 16" fill="currentColor" aria-hidden="true">
                <path d="M4 1.5H3a2 2 0 0 0-2 2V14a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V3.5a2 2 0 0 0-2-2h-1v1h1a1 1 0 0 1 1 1V14a1 1 0 0 1-1 1H3a1 1 0 0 1-1-1V3.5a1 1 0 0 1 1-1h1v-1z"/>
                <path d="M9.5 1a.5.5 0 0 1 .5.5v1a.5.5 0 0 1-.5.5h-3a.5.5 0 0 1-.5-.5v-1a.5.5 0 0 1 .5-.5h3zm-3-1A1.5 1.5 0 0 0 5 1.5v1A1.5 1.5 0 0 0 6.5 4h3A1.5 1.5 0 0 0 11 2.5v-1A1.5 1.5 0 0 0 9.5 0h-3z"/>
            </svg>
            <span class="copy-text">Copy</span>
        `;

        // Add click handler
        button.addEventListener('click', async (e) => {
            e.preventDefault();
            await this.copyCode(button, codeElement);
        });

        return button;
    }

    private async copyCode(button: HTMLButtonElement, codeElement: HTMLElement): Promise<void> {
        try {
            // Get the code text
            const code = codeElement.textContent || '';

            // Copy to clipboard
            await navigator.clipboard.writeText(code);

            // Show success feedback
            this.showCopySuccess(button);

        } catch (err) {
            console.error('Failed to copy code:', err);
            this.showCopyError(button);
        }
    }

    private showCopySuccess(button: HTMLButtonElement): void {
        // Update button appearance
        button.classList.add('copied');
        
        // Change icon to checkmark
        button.innerHTML = `
            <svg class="copy-icon" viewBox="0 0 16 16" fill="currentColor" aria-hidden="true">
                <path d="M13.854 3.646a.5.5 0 0 1 0 .708l-7 7a.5.5 0 0 1-.708 0l-3.5-3.5a.5.5 0 1 1 .708-.708L6.5 10.293l6.646-6.647a.5.5 0 0 1 .708 0z"/>
            </svg>
            <span class="copy-text">Copied!</span>
        `;

        // Update aria-label
        button.setAttribute('aria-label', 'Code copied to clipboard');

        // Clear any existing timeout
        if (this.copiedTimeout !== null) {
            clearTimeout(this.copiedTimeout);
        }

        // Reset after 2 seconds
        this.copiedTimeout = window.setTimeout(() => {
            this.resetCopyButton(button);
        }, 2000);
    }

    private showCopyError(button: HTMLButtonElement): void {
        // Update button to show error
        button.classList.add('copy-error');
        
        const originalHTML = button.innerHTML;
        button.innerHTML = `
            <svg class="copy-icon" viewBox="0 0 16 16" fill="currentColor" aria-hidden="true">
                <path d="M8 15A7 7 0 1 1 8 1a7 7 0 0 1 0 14zm0 1A8 8 0 1 0 8 0a8 8 0 0 0 0 16z"/>
                <path d="M4.646 4.646a.5.5 0 0 1 .708 0L8 7.293l2.646-2.647a.5.5 0 0 1 .708.708L8.707 8l2.647 2.646a.5.5 0 0 1-.708.708L8 8.707l-2.646 2.647a.5.5 0 0 1-.708-.708L7.293 8 4.646 5.354a.5.5 0 0 1 0-.708z"/>
            </svg>
            <span class="copy-text">Failed</span>
        `;

        // Reset after 2 seconds
        setTimeout(() => {
            button.classList.remove('copy-error');
            button.innerHTML = originalHTML;
            button.setAttribute('aria-label', 'Copy code to clipboard');
        }, 2000);
    }

    private resetCopyButton(button: HTMLButtonElement): void {
        button.classList.remove('copied');
        
        // Reset to original icon
        button.innerHTML = `
            <svg class="copy-icon" viewBox="0 0 16 16" fill="currentColor" aria-hidden="true">
                <path d="M4 1.5H3a2 2 0 0 0-2 2V14a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V3.5a2 2 0 0 0-2-2h-1v1h1a1 1 0 0 1 1 1V14a1 1 0 0 1-1 1H3a1 1 0 0 1-1-1V3.5a1 1 0 0 1 1-1h1v-1z"/>
                <path d="M9.5 1a.5.5 0 0 1 .5.5v1a.5.5 0 0 1-.5.5h-3a.5.5 0 0 1-.5-.5v-1a.5.5 0 0 1 .5-.5h3zm-3-1A1.5 1.5 0 0 0 5 1.5v1A1.5 1.5 0 0 0 6.5 4h3A1.5 1.5 0 0 0 11 2.5v-1A1.5 1.5 0 0 0 9.5 0h-3z"/>
            </svg>
            <span class="copy-text">Copy</span>
        `;

        // Reset aria-label
        button.setAttribute('aria-label', 'Copy code to clipboard');
    }
}

// Initialize when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        new ClipboardManager();
    });
} else {
    // DOM already loaded
    new ClipboardManager();
}
