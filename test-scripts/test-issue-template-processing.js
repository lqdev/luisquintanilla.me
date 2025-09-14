#!/usr/bin/env node

// Test script to validate GitHub Issue Template posting logic locally
// This simulates the GitHub Action workflow logic for testing

// Simulate issue body from GitHub Issue Form
const simulatedIssueBody = `### Title

Test Note from GitHub Issue Template

### Content

This is a test note created via GitHub Issue Template. 

The workflow should:
- Generate proper frontmatter
- Create a uniquely named file
- Format the content correctly

### Tags

github, automation, testing

---

**What happens next?**

1. 🤖 A GitHub Action will process this issue
2. 📄 Generate a properly formatted markdown file in \`_src/notes/\`
3. 🔀 Create a pull request with your content
4. ✅ The issue will be closed automatically
5. 🚀 After PR review and merge, your note will be live on the website

The generated content will include proper frontmatter with title, publication date, post type, and tags.`;

console.log('🧪 Testing GitHub Issue Template Processing Logic\n');

// Extract form responses using regex patterns (same as workflow)
function extractFormValue(body, label) {
  const regex = new RegExp(`### ${label}\\s*\\n\\s*\\n([\\s\\S]*?)(?=\\n\\n###|\\n\\n---|\$)`, 'i');
  const match = body.match(regex);
  const value = match ? match[1].trim() : '';
  
  // Remove common artifacts from GitHub issue forms
  return value.replace(/^_No response_$/i, '').trim();
}

const title = extractFormValue(simulatedIssueBody, 'Title');
const content = extractFormValue(simulatedIssueBody, 'Content');
const tagsInput = extractFormValue(simulatedIssueBody, 'Tags');

console.log('📝 Extracted Fields:');
console.log('Title:', title);
console.log('Content length:', content.length, 'characters');
console.log('Tags input:', tagsInput);
console.log();

// Generate slug from title (same as workflow)
function generateSlug(title) {
  let slug = title.toLowerCase()
    .replace(/[^a-z0-9\s-]/g, '') // Remove special chars
    .replace(/\s+/g, '-') // Replace spaces with hyphens
    .replace(/-+/g, '-') // Replace multiple hyphens with single
    .replace(/^-|-$/g, ''); // Remove leading/trailing hyphens
    
  // Ensure slug is not empty
  if (!slug || slug.length === 0) {
    slug = 'untitled-note';
  }
  
  // Truncate if too long
  if (slug.length > 50) {
    slug = slug.substring(0, 50).replace(/-$/, '');
  }
  
  return slug;
}

const slug = generateSlug(title);

// Process tags
const tags = tagsInput ? 
  tagsInput.split(',').map(tag => tag.trim().toLowerCase()).filter(tag => tag.length > 0) :
  [];

// Generate current timestamp in EST
const now = new Date();
const est = new Date(now.getTime() - (5 * 60 * 60 * 1000)); // EST is UTC-5
const timestamp = est.getFullYear() + '-' + 
  String(est.getMonth() + 1).padStart(2, '0') + '-' + 
  String(est.getDate()).padStart(2, '0') + ' ' +
  String(est.getHours()).padStart(2, '0') + ':' + 
  String(est.getMinutes()).padStart(2, '0') + ' -05:00';

// Generate frontmatter
const frontmatter = `---
title: ${title}
post_type: note
published_date: "${timestamp}"
tags: [${tags.map(tag => `"${tag}"`).join(', ')}]
---`;

// Combine frontmatter and content
const fullContent = frontmatter + '\n\n' + content;

// Generate filename
const filename = `${slug}-${est.getFullYear()}-${String(est.getMonth() + 1).padStart(2, '0')}-${String(est.getDate()).padStart(2, '0')}.md`;

console.log('🔧 Generated Values:');
console.log('Slug:', slug);
console.log('Timestamp:', timestamp);
console.log('Filename:', filename);
console.log('Tags array:', JSON.stringify(tags));
console.log();

console.log('📄 Generated Content:');
console.log('='.repeat(50));
console.log(fullContent);
console.log('='.repeat(50));
console.log();

console.log('✅ Test completed successfully!');
console.log('📊 Content Statistics:');
console.log('- Total content length:', fullContent.length, 'characters');
console.log('- Frontmatter lines:', frontmatter.split('\n').length);
console.log('- Content preview:', content.substring(0, 100) + '...');