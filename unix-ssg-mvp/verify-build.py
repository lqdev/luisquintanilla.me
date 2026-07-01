#!/usr/bin/env python3
"""
Quick verification script to test the Unix SSG MVP build output
"""

import os
import sys
from pathlib import Path

BUILD_DIR = Path("build")

def check_structure():
    """Verify the build directory structure"""
    print("=" * 60)
    print("Unix SSG MVP - Build Verification")
    print("=" * 60)
    print()
    
    # Check root index
    root_index = BUILD_DIR / "index.html"
    if root_index.exists():
        print("✅ Root index.html found")
        print(f"   Size: {root_index.stat().st_size} bytes")
    else:
        print("❌ Root index.html missing")
    
    print()
    
    # Check subdirectories
    categories = {
        "posts": "📝 Posts",
        "notes": "📓 Notes", 
        "playlists": "🎵 Playlists",
        "reviews": "⭐ Reviews",
        "tags": "🏷️ Tags",
        "feed": "📡 Feed",
    }
    
    for category, label in categories.items():
        cat_dir = BUILD_DIR / category
        if cat_dir.exists():
            html_files = list(cat_dir.glob("**/index.html"))
            if html_files:
                print(f"✅ {label}: {len(html_files)} page(s)")
                for f in sorted(html_files)[:3]:  # Show first 3
                    rel_path = f.relative_to(BUILD_DIR)
                    print(f"   - {rel_path}")
                if len(html_files) > 3:
                    print(f"   ... and {len(html_files) - 3} more")
            else:
                print(f"⚠️  {label}: directory exists but empty")
        else:
            print(f"❌ {label}: directory not found")
    
    print()
    print("=" * 60)
    print("To view the site locally:")
    print("  python3 -m http.server 8000")
    print("")
    print("Then visit: http://localhost:8000")
    print("=" * 60)

if __name__ == "__main__":
    if not BUILD_DIR.exists():
        print(f"❌ Build directory not found at {BUILD_DIR}")
        sys.exit(1)
    
    check_structure()
