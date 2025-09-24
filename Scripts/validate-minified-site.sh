#!/bin/bash

# Quick validation script to ensure minified website functions correctly
# Validates HTML structure, CSS functionality, and JS functionality

echo "🔍 Validating minified website functionality..."
echo "==============================================="

# Check if essential files exist and are not empty
validate_file() {
    local file="$1"
    local description="$2"
    
    if [ ! -f "$file" ]; then
        echo "❌ $description: File missing ($file)"
        return 1
    elif [ ! -s "$file" ]; then
        echo "❌ $description: File is empty ($file)"
        return 1
    else
        echo "✅ $description: File exists and has content"
        return 0
    fi
}

# Validate HTML structure
validate_html() {
    echo ""
    echo "🔧 Validating HTML structure..."
    
    # Check main page
    validate_file "_public/index.html" "Homepage"
    
    # Check for essential HTML elements in homepage
    if grep -q "<!DOCTYPE html>" "_public/index.html" && \
       grep -q "<html" "_public/index.html" && \
       grep -q "</html>" "_public/index.html" && \
       grep -q "<head>" "_public/index.html" && \
       grep -q "</head>" "_public/index.html" && \
       grep -q "<body>" "_public/index.html" && \
       grep -q "</body>" "_public/index.html"; then
        echo "✅ HTML structure: Valid HTML5 structure preserved"
    else
        echo "❌ HTML structure: Basic HTML elements missing or malformed"
    fi
    
    # Check a sample post page
    sample_post=$(find _public/posts -name "index.html" | head -1)
    if [ -n "$sample_post" ] && [ -f "$sample_post" ]; then
        validate_file "$sample_post" "Sample blog post"
    fi
}

# Validate CSS functionality
validate_css() {
    echo ""
    echo "🔧 Validating CSS files..."
    
    # Check main CSS files
    validate_file "_public/assets/css/custom/main.css" "Main CSS"
    validate_file "_public/assets/css/custom/timeline.css" "Timeline CSS"
    
    # Check that CSS contains basic selectors (not completely broken)
    if grep -q "body\|html\|\.main\|\.timeline" "_public/assets/css/custom/main.css" 2>/dev/null; then
        echo "✅ CSS functionality: Basic CSS selectors found"
    else
        echo "⚠️  CSS functionality: Could not verify CSS selectors"
    fi
}

# Validate JS functionality
validate_js() {
    echo ""
    echo "🔧 Validating JavaScript files..."
    
    # Check main JS files
    validate_file "_public/assets/js/main.js" "Main JavaScript"
    validate_file "_public/assets/js/search.js" "Search JavaScript"
    validate_file "_public/assets/js/timeline.js" "Timeline JavaScript"
    
    # Check for basic JS syntax (functions, variables)
    if grep -q "function\|var\|let\|const" "_public/assets/js/main.js" 2>/dev/null; then
        echo "✅ JS functionality: Basic JavaScript syntax found"
    else
        echo "⚠️  JS functionality: Could not verify JavaScript syntax"  
    fi
}

# Check compression results
validate_compression() {
    echo ""
    echo "🔧 Validating compression results..."
    
    # Check that .gz files were created
    gz_count=$(find _public -name "*.gz" | wc -l)
    if [ $gz_count -gt 0 ]; then
        echo "✅ Compression: $gz_count compressed files created"
    else
        echo "❌ Compression: No compressed files found"
    fi
    
    # Check compression ratio on homepage
    if [ -f "_public/index.html" ] && [ -f "_public/index.html.gz" ]; then
        original_size=$(stat -c%s "_public/index.html")
        compressed_size=$(stat -c%s "_public/index.html.gz")
        if [ $original_size -gt 0 ]; then
            savings=$((100 - (compressed_size * 100 / original_size)))
            echo "✅ Homepage compression: ${savings}% reduction"
        fi
    fi
}

# Check file count integrity
validate_file_counts() {
    echo ""
    echo "🔧 Validating file count integrity..."
    
    html_count=$(find _public -name "*.html" | wc -l)
    css_count=$(find _public -name "*.css" | wc -l)
    js_count=$(find _public -name "*.js" | wc -l)
    
    echo "📊 File counts after processing:"
    echo "   HTML files: $html_count"
    echo "   CSS files:  $css_count" 
    echo "   JS files:   $js_count"
    
    if [ $html_count -gt 4000 ] && [ $css_count -gt 40 ] && [ $js_count -gt 40 ]; then
        echo "✅ File counts: Expected number of files present"
    else
        echo "⚠️  File counts: Unexpected file counts, possible processing issues"
    fi
}

# Main validation
main() {
    validate_html
    validate_css  
    validate_js
    validate_compression
    validate_file_counts
    
    echo ""
    echo "✅ Website validation completed!"
    echo ""
    echo "💡 To verify full functionality:"
    echo "   1. Serve the _public directory with a web server"
    echo "   2. Test navigation, search, and interactive features"
    echo "   3. Verify responsive design on mobile devices"
    echo ""
}

# Execute validation
if [ ! -d "_public" ]; then
    echo "❌ Error: _public directory not found"
    echo "Please run 'dotnet run' and './Scripts/minify-and-compress.sh' first"
    exit 1
fi

main