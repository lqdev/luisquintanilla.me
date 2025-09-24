#!/bin/bash

# Website Minification and Compression Script
# Minifies HTML, CSS, JS files and applies gzip compression to reduce site size
# Uses Unix built-in tools: sed, tr, awk, gzip, find

echo "🗜️ Starting website minification and compression..."
echo "=================================================="

# Check if _public directory exists
if [ ! -d "./_public" ]; then
    echo "❌ Error: _public directory not found. Please run 'dotnet run' to generate the site first."
    exit 1
fi

# Get initial size
INITIAL_SIZE=$(du -sh ./_public | cut -f1)
echo "📊 Initial site size: $INITIAL_SIZE"
echo ""

# Function to minify HTML files
minify_html() {
    echo "🔧 Minifying HTML files..."
    local count=0
    
    # Find all HTML files and minify them
    find ./_public -name "*.html" -type f | while read -r file; do
        # Create temp file for processing
        temp_file="${file}.tmp"
        
        # Minify HTML: remove extra whitespace, comments, and blank lines
        sed 's/<!--.*-->//g' "$file" | \
        sed 's/[[:space:]]*$//' | \
        sed '/^$/d' | \
        tr -s ' ' | \
        sed 's/> </></g' | \
        sed 's/[[:space:]]*>[[:space:]]*</></g' > "$temp_file"
        
        # Replace original file
        mv "$temp_file" "$file"
        
        count=$((count + 1))
        if [ $((count % 500)) -eq 0 ]; then
            echo "   Processed $count HTML files..."
        fi
    done
    
    total_html=$(find ./_public -name "*.html" -type f | wc -l)
    echo "✅ Minified $total_html HTML files"
}

# Function to minify CSS files
minify_css() {
    echo "🔧 Minifying CSS files..."
    local count=0
    
    find ./_public -name "*.css" -type f | while read -r file; do
        # Create temp file for processing
        temp_file="${file}.tmp"
        
        # Minify CSS: remove comments, extra whitespace, and blank lines
        sed 's|/\*.*\*/||g' "$file" | \
        sed 's/[[:space:]]*$//' | \
        sed '/^$/d' | \
        tr -d '\n' | \
        sed 's/[[:space:]]*{[[:space:]]*/{/g' | \
        sed 's/[[:space:]]*}[[:space:]]*/ }/g' | \
        sed 's/[[:space:]]*;[[:space:]]*/;/g' | \
        sed 's/[[:space:]]*:[[:space:]]*/:/g' | \
        sed 's/[[:space:]]*,[[:space:]]*/,/g' > "$temp_file"
        
        # Add newline at end and replace original
        echo "" >> "$temp_file"
        mv "$temp_file" "$file"
        
        count=$((count + 1))
    done
    
    total_css=$(find ./_public -name "*.css" -type f | wc -l)
    echo "✅ Minified $total_css CSS files"
}

# Function to minify JS files (excluding already minified files)
minify_js() {
    echo "🔧 Minifying JS files..."
    local count=0
    
    find ./_public -name "*.js" -type f ! -name "*.min.js" | while read -r file; do
        # Create temp file for processing
        temp_file="${file}.tmp"
        
        # Basic JS minification: remove comments and extra whitespace
        sed 's|//.*$||g' "$file" | \
        sed 's|/\*.*\*/||g' | \
        sed 's/[[:space:]]*$//' | \
        sed '/^$/d' | \
        tr -s ' ' | \
        sed 's/[[:space:]]*{[[:space:]]*/{/g' | \
        sed 's/[[:space:]]*}[[:space:]]*/}/g' | \
        sed 's/[[:space:]]*;[[:space:]]*/;/g' > "$temp_file"
        
        mv "$temp_file" "$file"
        count=$((count + 1))
    done
    
    total_js=$(find ./_public -name "*.js" -type f ! -name "*.min.js" | wc -l)
    echo "✅ Minified $total_js JS files (excluding *.min.js)"
}

# Function to compress files with gzip
compress_files() {
    echo "🔧 Compressing files with gzip..."
    
    # Compress HTML files
    echo "   Compressing HTML files..."
    find ./_public -name "*.html" -type f -exec gzip -9 -c {} \; > /dev/null 2>&1
    local html_compressed=$(find ./_public -name "*.html" -type f | wc -l)
    
    # Compress CSS files  
    echo "   Compressing CSS files..."
    find ./_public -name "*.css" -type f -exec gzip -9 -c {} \; > /dev/null 2>&1
    local css_compressed=$(find ./_public -name "*.css" -type f | wc -l)
    
    # Compress JS files
    echo "   Compressing JS files..."
    find ./_public -name "*.js" -type f -exec gzip -9 -c {} \; > /dev/null 2>&1
    local js_compressed=$(find ./_public -name "*.js" -type f | wc -l)
    
    # Create .gz versions alongside originals for web server compression
    echo "   Creating .gz versions for web server..."
    find ./_public -name "*.html" -type f -exec bash -c 'gzip -9 -c "$0" > "$0.gz"' {} \;
    find ./_public -name "*.css" -type f -exec bash -c 'gzip -9 -c "$0" > "$0.gz"' {} \;
    find ./_public -name "*.js" -type f -exec bash -c 'gzip -9 -c "$0" > "$0.gz"' {} \;
    find ./_public -name "*.xml" -type f -exec bash -c 'gzip -9 -c "$0" > "$0.gz"' {} \;
    find ./_public -name "*.rss" -type f -exec bash -c 'gzip -9 -c "$0" > "$0.gz"' {} \;
    
    local gz_files=$(find ./_public -name "*.gz" -type f | wc -l)
    echo "✅ Created $gz_files compressed .gz versions"
}

# Function to show compression statistics
show_statistics() {
    echo ""
    echo "📈 Compression Statistics:"
    echo "========================="
    
    # Calculate size without .gz files for accurate comparison
    FINAL_SIZE_NO_GZ=$(du -sh --exclude="*.gz" ./_public | cut -f1)
    FINAL_SIZE_WITH_GZ=$(du -sh ./_public | cut -f1)
    
    echo "📊 Site size after minification: $FINAL_SIZE_NO_GZ"
    echo "📊 Total size with .gz files: $FINAL_SIZE_WITH_GZ"
    
    # Count compressed files
    local gz_count=$(find ./_public -name "*.gz" | wc -l)
    echo "📦 Compressed files created: $gz_count"
    
    # Show compression savings examples
    echo ""
    echo "💡 Compression savings examples:"
    echo "--------------------------------"
    
    # Sample a few large HTML files to show compression ratio
    find ./_public -name "*.html" -size +10k | head -3 | while read -r file; do
        if [ -f "$file.gz" ]; then
            original_size=$(stat -c%s "$file")
            compressed_size=$(stat -c%s "$file.gz") 
            if [ $original_size -gt 0 ]; then
                savings=$((100 - (compressed_size * 100 / original_size)))
                echo "   $(basename "$file"): ${savings}% smaller when compressed"
            fi
        fi
    done
    
    # File type breakdown
    echo ""
    echo "📁 File type breakdown:"
    echo "----------------------"
    printf "%-15s %s\n" "HTML files:" "$(find ./_public -name "*.html" | wc -l)"
    printf "%-15s %s\n" "CSS files:" "$(find ./_public -name "*.css" | wc -l)"
    printf "%-15s %s\n" "JS files:" "$(find ./_public -name "*.js" | wc -l)"
    printf "%-15s %s\n" "XML/RSS files:" "$(find ./_public \( -name "*.xml" -o -name "*.rss" \) | wc -l)"
    printf "%-15s %s\n" "GZ files:" "$gz_count"
    
    echo ""
    echo "🎯 Optimization complete!"
    echo "Original: $INITIAL_SIZE → Minified: $FINAL_SIZE_NO_GZ → With compression files: $FINAL_SIZE_WITH_GZ"
    echo ""
    echo "💡 Web servers can automatically serve .gz versions for better performance"
}

# Main execution
main() {
    # Run minification
    minify_html
    echo ""
    minify_css  
    echo ""
    minify_js
    echo ""
    
    # Run compression
    compress_files
    
    # Show final statistics
    show_statistics
    
    echo ""
    echo "✅ Website minification and compression completed successfully!"
}

# Execute main function
main