#!/bin/bash
# Comprehensive validation script using Playwright

cd /workspaces/luisquintanilla.me/unix-ssg-mvp

cat > validate-build.js << 'EOF'
const { chromium } = require('playwright');

(async () => {
    const browser = await chromium.launch();
    const page = await browser.newPage();
    
    const baseUrl = 'http://localhost:8000';
    const issues = [];
    
    // Test routes
    const routes = [
        { path: '/', name: 'Home' },
        { path: '/posts/', name: 'Posts Collection' },
        { path: '/playlists/', name: 'Playlists Collection' },
        { path: '/notes/', name: 'Notes Collection' },
        { path: '/reviews/', name: 'Reviews Collection' },
        { path: '/tags/', name: 'Tags Collection' },
        { path: '/posts/test-review/', name: 'Individual Post' },
        { path: '/playlists/crate-finds-example/', name: 'Individual Playlist' },
    ];
    
    for (const route of routes) {
        try {
            const response = await page.goto(`${baseUrl}${route.path}`, { waitUntil: 'networkidle' });
            
            if (response.status() === 404) {
                issues.push(`❌ ${route.name} (${route.path}): 404 Not Found`);
            } else if (response.status() === 200) {
                // Check if it's a directory listing (bad) vs proper HTML
                const title = await page.title();
                const hasContent = await page.evaluate(() => document.documentElement.innerHTML.length > 200);
                
                if (!hasContent) {
                    issues.push(`⚠️  ${route.name} (${route.path}): Page has minimal content`);
                } else {
                    console.log(`✅ ${route.name} (${route.path}): OK`);
                }
            } else {
                issues.push(`⚠️  ${route.name} (${route.path}): HTTP ${response.status()}`);
            }
        } catch (error) {
            issues.push(`❌ ${route.name} (${route.path}): ${error.message}`);
        }
    }
    
    // Report issues
    if (issues.length > 0) {
        console.log('\n🔍 Issues Found:');
        issues.forEach(issue => console.log(issue));
    } else {
        console.log('\n✅ All routes validated successfully!');
    }
    
    await browser.close();
})();
EOF

echo "Created validation script. Running checks..."
npm install -D playwright 2>/dev/null || true

# Start server in background
python3 -m http.server 8000 --directory build &
SERVER_PID=$!
sleep 2

# Run validation
node validate-build.js

# Kill server
kill $SERVER_PID 2>/dev/null || true

echo ""
echo "🔍 Checking directory structure..."
echo ""
echo "Directories with missing index.html files:"
find build -type d ! -path '*/\.*' | while read dir; do
    if [ ! -f "$dir/index.html" ]; then
        rel_path="${dir#build/}"
        if [ "$rel_path" != "build" ] && [ "$rel_path" != "." ]; then
            echo "  ❌ /$rel_path/"
        fi
    fi
done

echo ""
echo "All directories in build:"
find build -type d ! -path '*/\.*' -print | sort
