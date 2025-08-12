#!/bin/bash

# Check Site Sizes Script
# Shows total site size, main site size (excluding text-only), and text-only site size

echo "📊 Website Size Analysis"
echo "========================"

# Total site size
total_size=$(du -sh ./_public | cut -f1)
echo ""
echo "🚀 TOTAL WEBSITE SIZE: $total_size"
echo "================================="
echo ""
echo "🌐 Full Site: $total_size"

# Text-only site size (if it exists)
if [ -d "./_public/text" ]; then
    text_size=$(du -sh ./_public/text | cut -f1)
    echo "📝 Text-Only Site Size: $text_size"
    
    # Calculate main site size (total minus text-only)
    # Note: This is an approximation since we're working with human-readable sizes
    echo "🎨 Main Site Size: ~$(du -sh --exclude=text ./_public | cut -f1)"
else
    echo "📝 Text-Only Site: Not found"
    echo "🎨 Main Site Size: $total_size"
fi

echo ""
echo "📁 Directory Breakdown:"
echo "----------------------"

# Show breakdown of major directories
for dir in ./_public/*/; do
    if [ -d "$dir" ]; then
        dir_name=$(basename "$dir")
        dir_size=$(du -sh "$dir" | cut -f1)
        printf "%-15s %s\n" "$dir_name:" "$dir_size"
    fi
done

echo ""
echo "📈 Content Statistics:"
echo "---------------------"

# Count files in main areas
if [ -d "./_public/posts" ]; then
    post_count=$(find ./_public/posts -name "index.html" | wc -l)
    echo "📄 Posts: $post_count"
fi

if [ -d "./_public/notes" ]; then
    note_count=$(find ./_public/notes -name "index.html" | wc -l)
    echo "📝 Notes: $note_count"
fi

if [ -d "./_public/responses" ]; then
    response_count=$(find ./_public/responses -name "index.html" | wc -l)
    echo "💬 Responses: $response_count"
fi

if [ -d "./_public/text" ]; then
    text_page_count=$(find ./_public/text -name "*.html" | wc -l)
    echo "🔤 Text-Only Pages: $text_page_count"
fi

echo ""
