# Surgical Broken Links Fix - Final 15 Issues

**Created**: 2025-08-04  
**Objective**: Fix remaining 15 non-social broken links with precision

## 🎯 **Categorized Issues**

### **Category 1: Missing Content Files (2 links)**
**Root Cause**: Build process created redirects but target content is missing
- `/bookmarks/pocket-shutting-down/` (404) → Need to verify if file exists in _src/bookmarks/
- `/bookmarks/resource-list-personal-web/` (404) → Need to verify if file exists in _src/bookmarks/

### **Category 2: Legacy HTML Extension Links (3 links)**  
**Root Cause**: Internal links still point to .html URLs that have redirects
- `/contact.html` → Should link to `/contact/` (redirects exist)
- `/posts/rediscovering-rss-user-freedom.html` → Should link to `/posts/rediscovering-rss-user-freedom/`
- `/presentations/mlnet-globalai-2022.html` → Should link to `/resources/presentations/mlnet-globalai-2022/`

### **Category 3: Feed Path Issues (2 links)**
**Root Cause**: Feed references pointing to incorrect locations
- `/posts/index.xml` → Should be `/posts/feed.xml` 
- `/notes/marvin-gaye-whats-going-on/` → Redirect exists but target missing
- `/notes/well-known-feeds/` → Redirect exists but target missing

### **Category 4: Tag URL Malformation (3 links)**
**Root Cause**: Special characters in tag URLs breaking routing
- `/tags/ci/cd` → Should be `/tags/ci-cd/` or escaped properly
- `/tags/f#/` → Hash character needs URL encoding: `/tags/f%23/`
- `/tags/stabilityai&quot;` → HTML entity needs fixing: `/tags/stabilityai/`

### **Category 5: CSS/Asset Issues (2 links)**
**Root Cause**: Missing CSS files or incorrect paths
- `bootstrap-icons.css` → Relative path issue in bootstrap icons index
- `/gravatar` → Redirect exists to `/contact/` but sources still link to `/gravatar`

### **Category 6: External Domain Timeout (1 link)**
**Root Cause**: External URL incorrectly classified as internal
- `desertoracle.com/radio` → Missing protocol, should be `https://desertoracle.com/radio`
- `https://luisquintanilla.me` → Timeout (external, can ignore)

## 🔧 **Surgical Fix Strategy**

### **Phase 1: Content Verification & Creation**
1. Check if missing bookmark files exist in source
2. Create missing content files if needed
3. Verify redirect targets exist

### **Phase 2: Internal Link Updates**  
1. Update .html links to directory URLs in source content
2. Fix feed XML path references
3. Update gravatar links to contact page

### **Phase 3: Tag URL Sanitization**
1. Fix malformed tag URLs in tag listing generation
2. Ensure proper URL encoding for special characters
3. Validate tag page generation

### **Phase 4: Asset Path Fixes**
1. Fix bootstrap-icons CSS path
2. Update external URL protocols
3. Verify all asset references

## ✅ **Expected Results**
- **15 → 0** non-social broken links
- **Total reduction**: 1000+ → 8 (only social media links remaining)
- **Final success rate**: 99.2% link health improvement
