# Resume Feature Documentation

This comprehensive guide explains how to create, customize, and maintain your professional resume page using the custom resume system. The resume feature provides a clean, professional presentation with structured sections and support for rich content including work experience, projects, skills, education, and testimonials.

## Overview

The resume system is a custom implementation that:
- Uses **custom Markdig blocks** for structured content sections
- Supports **markdown content** within each section for rich formatting
- Generates a **professional, responsive layout** with Desert theme integration
- Provides **semantic HTML** with proper accessibility support
- Includes **print-friendly styling** for PDF generation
- Supports **availability status badges** (open to opportunities, not looking)
- Enables **easy updates** through markdown editing

## Quick Start

Your resume is defined in a single markdown file:
```
_src/resume/resume.md
```

The file structure consists of:
1. **YAML frontmatter** - Metadata and contact information
2. **Markdown content** - Introduction and summary sections
3. **Custom blocks** - Structured sections (experience, projects, skills, etc.)

### Minimal Example

```markdown
---
title: Professional Resume
status: open-to-opportunities
lastUpdated: 2025-10-25
currentRole: Software Engineer
contactLinks:
  Email: you@example.com
  LinkedIn: https://linkedin.com/in/yourprofile
  GitHub: https://github.com/yourusername
---

# Professional Resume

## About

I'm a passionate software engineer with expertise in building scalable applications. 
I specialize in making technology accessible and creating tools that empower others.

## Experience

:::experience
role: Senior Software Engineer
company: [Company Name](https://company.com)
start: 2020-01-15
end: current
---
- Led development of key features
- Mentored junior engineers
- Presented at conferences
:::

## Skills & Expertise

:::skills
category: Programming Languages
---
F#, C#, Python, JavaScript
:::

## Currently Interested In

I'm exploring advances in cloud architecture and distributed systems. Always excited 
to discuss software best practices and how technology can solve real-world problems.
```

## YAML Frontmatter Reference

### Required Fields

| Field | Type | Description | Example |
|-------|------|-------------|---------|
| `title` | string | Resume page title | `"Professional Resume"` |
| `lastUpdated` | date | Last update date (YYYY-MM-DD) | `2025-10-25` |
| `currentRole` | string | Current job title | `"Senior Software Engineer"` |
| `contactLinks` | object | Contact information | See below |

### Optional Fields

| Field | Type | Description | Example |
|-------|------|-------------|---------|
| `status` | string | Availability status | `open-to-opportunities` or `not-looking` |

### Contact Links Object

The `contactLinks` field is a dictionary of label-URL pairs:

```yaml
contactLinks:
  Email: hello@example.com
  LinkedIn: https://www.linkedin.com/in/yourprofile
  GitHub: https://github.com/yourusername
  Website: https://www.yoursite.com
  Twitter: https://twitter.com/yourhandle
```

**Guidelines**:
- Use descriptive labels (Email, LinkedIn, GitHub, etc.)
- Email addresses will be automatically converted to `mailto:` links
- URLs should be complete with protocol (`https://`)
- Order determines display order on the resume

### Status Badge

The `status` field controls the availability badge:
- `open-to-opportunities` - Green badge, shows "Open to Opportunities"
- `not-looking` - Gray badge, shows "Not Currently Looking"
- Omit field or leave empty for no badge

## Custom Block Syntax

The resume system uses five custom block types, all following the same pattern:

```
:::blocktype
field1: value1
field2: value2
---
Markdown content for this section
:::
```

### Block Structure
- **Opening marker**: `:::blocktype`
- **Field definitions**: `field: value` (one per line, before separator)
- **Content separator**: `---` (separates fields from content)
- **Content**: Markdown text (after separator, before closing marker)
- **Closing marker**: `:::`

### Experience Block

Represents work experience, positions, and job history.

**Syntax**:
```markdown
:::experience
role: Job Title
company: [Company Name](https://company.com)
start: 2020-01-15
end: current
---
- Key achievement or responsibility
- Another accomplishment with **markdown** support
- Use bullet points for highlights
:::
```

**Field Reference**:

| Field | Required | Format | Description |
|-------|----------|--------|-------------|
| `role` | Yes | string | Job title or position |
| `company` | Yes | string | Company name (supports markdown links) |
| `start` | Yes | date | Start date (YYYY-MM-DD format) |
| `end` | No | date or "current" | End date or "current" for ongoing |

**Content**: List of highlights, achievements, or responsibilities (markdown supported)

**Tips**:
- Use markdown links for company names: `[Microsoft](https://microsoft.com)`
- Use "current" for `end` date if still in the position
- Order experiences from most recent to oldest
- Keep bullet points concise and achievement-focused
- Use markdown for emphasis: **bold**, *italic*, `code`, etc.

### Project Block

Showcases notable projects, contributions, and portfolio items.

**Syntax**:
```markdown
:::project
title: Project Name
url: https://github.com/username/project
tech: F#, Markdig, Giraffe, Static Site Generation
---
Detailed project description with markdown support.

Can include multiple paragraphs and **rich formatting**.
:::
```

**Field Reference**:

| Field | Required | Format | Description |
|-------|----------|--------|-------------|
| `title` | Yes | string | Project name |
| `url` | No | URL | Project link (GitHub, website, etc.) |
| `tech` | No | string | Technology stack (comma-separated) |

**Content**: Project description, impact, and details (markdown supported)

**Tips**:
- Include project URLs for GitHub repos, live demos, or documentation
- List relevant technologies in the `tech` field
- Describe both technical implementation and impact
- Use concrete metrics when possible (users, performance, etc.)

### Skills Block

Groups skills and expertise by category.

**Syntax**:
```markdown
:::skills
category: Programming Languages
---
F#, C#, Python, JavaScript, TypeScript
:::

:::skills
category: Frameworks & Tools
---
.NET, React, [ML.NET](https://dot.net/ml), Docker
:::
```

**Field Reference**:

| Field | Required | Format | Description |
|-------|----------|--------|-------------|
| `category` | Yes | string | Skill category name |

**Content**: Comma-separated list of skills (markdown links supported)

**Tips**:
- Create separate blocks for different skill categories
- Use markdown links for technologies: `[ML.NET](https://dot.net/ml)`
- Order categories by relevance to your target role
- Keep skill lists concise and relevant
- Skills display in a responsive grid layout

### Education Block

Documents educational background and achievements.

**Syntax**:
```markdown
:::education
degree: Bachelor of Science in Computer Science
institution: [University Name](https://university.edu)
year: 2016
---
Summa Cum Laude - Focus on Machine Learning and Software Engineering
:::
```

**Field Reference**:

| Field | Required | Format | Description |
|-------|----------|--------|-------------|
| `degree` | Yes | string | Degree name |
| `institution` | Yes | string | School name (supports markdown links) |
| `year` | No | integer | Graduation year |

**Content**: Additional details (honors, focus areas, achievements)

**Tips**:
- Use markdown links for institution: `[MIT](https://mit.edu)`
- Include relevant honors, GPA, or specializations
- Order from most recent to oldest
- Content section is optional

### Testimonial Block

Displays recommendations and endorsements.

**Syntax**:
```markdown
:::testimonial
author: Jane Doe, Engineering Manager at [Microsoft](https://microsoft.com)
---
Quote text with the person's recommendation or feedback.

Can include multiple paragraphs and **markdown formatting**.
:::
```

**Field Reference**:

| Field | Required | Format | Description |
|-------|----------|--------|-------------|
| `author` | Yes | string | Name, title, and affiliation (supports markdown) |

**Content**: The testimonial quote (markdown supported)

**Tips**:
- Include author's name, title, and company
- Use markdown links for company: `at [Microsoft](https://microsoft.com)`
- Keep testimonials specific and impactful
- Order by relevance or recency
- Testimonials display in an attractive card format

## Section Organization

The resume system recognizes standard markdown headings to organize sections:

```markdown
## About
Your professional summary and introduction written directly in markdown.
Supports standard markdown formatting including **bold**, *italic*, links, 
lists, code, and multiple paragraphs.

## Experience
:::experience blocks here:::

## Skills & Expertise
:::skills blocks here:::

## Notable Projects
:::project blocks here:::

## Education
:::education blocks here:::

## What People Say
:::testimonial blocks here:::

## Currently Interested In
Your current interests, career goals, and what you're looking for.
Supports standard markdown formatting including **bold**, *italic*, links, 
lists, code, and multiple paragraphs.
```

**Section Rendering**:
- Sections appear only if they contain content
- The "About" section extracts content from the `## About` markdown heading
- The "Interests" section extracts content from `## Currently Interested In` heading (or `## Interests` as a fallback)
- Experience, Skills, Projects, Education, and Testimonials are populated from their custom blocks
- Sections can use any markdown heading text
- Order sections as you prefer in the markdown file
- Write About and Interests content directly in markdown for easy editing

**Content vs. Metadata**:
- **Frontmatter (metadata)**: Resume metadata like title, current role, status, contact links
- **Markdown content**: Your professional summary, interests, and all other content
- This separation keeps metadata clean and content easy to edit

## Styling and Customization

### CSS Styling

The resume page uses dedicated styling in `_src/css/custom/resume.css`:

**Key Style Features**:
- Clean, professional card-based layout
- Desert theme variable integration
- Responsive design with mobile breakpoints
- Print-friendly styles for PDF generation
- Dark theme support via CSS variables
- Consistent spacing and typography

**CSS Variables Used**:
- `--card-bg` - Card background color
- `--border-color` - Border and divider colors
- `--text-primary` - Primary text color
- `--text-secondary` - Secondary/meta text color
- `--link-color` - Link color
- `--accent-color` - Accent highlights

### Mobile Responsive Design

The resume automatically adapts to different screen sizes:

**Desktop (> 768px)**:
- 800px maximum width
- Two-column skill grid
- Side-by-side experience headers
- Full contact link layout

**Mobile (≤ 768px)**:
- Single column layout
- Stacked contact links
- Reduced padding and font sizes
- Simplified navigation

### Dark Theme Support

Dark theme automatically applies based on system preference:
```css
@media (prefers-color-scheme: dark) {
  /* Dark theme styles */
}
```

## Implementation Architecture

### System Components

The resume system consists of several integrated components:

#### 1. Domain Model (Domain.fs)

Defines the data structures:

```fsharp
// Main resume type
type Resume = {
    FileName: string
    Metadata: ResumeMetadata
    Content: string
    Experience: Experience list
    Skills: SkillCategory list
    Projects: Project list
    Education: Education list
    Testimonials: Testimonial list
}

// Experience entry
type Experience = {
    Role: string
    Company: string
    StartDate: DateTime
    EndDate: DateTime option
    Highlights: string list option
}

// Similar types for Project, SkillCategory, Education, Testimonial
```

#### 2. Custom Block Parsers (CustomBlocks.fs)

Implements Markdig parsers for each block type:

- `ExperienceBlockParser` - Parses `:::experience` blocks
- `ProjectBlockParser` - Parses `:::project` blocks
- `SkillsBlockParser` - Parses `:::skills` blocks
- `EducationBlockParser` - Parses `:::education` blocks
- `TestimonialBlockParser` - Parses `:::testimonial` blocks

Each parser:
1. Recognizes the opening marker (`:::blocktype`)
2. Extracts field definitions (before `---`)
3. Captures content (after `---`)
4. Handles the closing marker (`:::`)

#### 3. Block Renderers (BlockRenderers.fs)

Converts parsed blocks to HTML:

- `ExperienceRenderer.render` - Generates experience HTML
- `ProjectRenderer.render` - Generates project HTML
- `SkillsRenderer.render` - Generates skills HTML
- `EducationRenderer.render` - Generates education HTML
- `TestimonialRenderer.render` - Generates testimonial HTML

#### 4. Content Loader (Loaders.fs)

```fsharp
let loadResume (filePath: string) : Resume option
```

Loads the resume markdown file and parses YAML frontmatter.

#### 5. Builder (Builder.fs)

```fsharp
let buildResumePage () =
    // 1. Load resume markdown
    // 2. Parse with Markdig + custom extensions
    // 3. Extract custom blocks from AST
    // 4. Generate complete Resume type
    // 5. Render to HTML
    // 6. Write to output
```

The builder orchestrates the entire process:
1. Loads `_src/resume/resume.md`
2. Sets up Markdig pipeline with custom extensions
3. Parses markdown to AST (Abstract Syntax Tree)
4. Extracts custom blocks from AST using `Descendants<BlockType>`
5. Constructs the complete `Resume` domain object
6. Renders to HTML using view functions
7. Writes to `_public/resume/index.html`

#### 6. View Rendering (Views/ContentViews.fs)

```fsharp
module ResumeViews =
    let render (resume: Domain.Resume) =
        // Generate complete HTML page
```

Creates the final HTML output with:
- Header section (title, role, status badge, contact links)
- Summary section
- Dynamic sections (experience, skills, projects, education, testimonials)
- Interests section
- Footer with last updated date

### Build Process Integration

The resume page is built during the main site generation in `Program.fs`:

```fsharp
[<EntryPoint>]
let main argv =
    // ... other build steps ...
    buildResumePage ()
    // ... continue build ...
```

**Build Steps**:
1. Clean output directory
2. Copy static files (including `resume.css`)
3. Build resume page
4. Generate other pages
5. Output to `_public/resume/index.html`

**Build Output**:
```
_public/
  resume/
    index.html         # Resume page
  css/
    custom/
      resume.css      # Resume styles
```

### Markdig Pipeline Configuration

The resume builder configures Markdig with:

```fsharp
let pipeline = 
    MarkdownPipelineBuilder()
        .UseYamlFrontMatter()
        .UsePipeTables()
        .UseTaskLists()
        .UseDiagrams()
        .UseMediaLinks()
        .UseMathematics()
        .UseEmojiAndSmiley()
        .UseEmphasisExtras()
        .UseBootstrap()
        .UseFigures()
        .Use(CustomBlocks.ResumeBlockExtension())  // Custom blocks
        .Build()
```

The `ResumeBlockExtension` registers all custom block parsers with the pipeline.

## How to Update Your Resume

### 1. Edit Content

Open `_src/resume/resume.md` in your editor:

```bash
# Using VS Code
code _src/resume/resume.md

# Or any text editor
vim _src/resume/resume.md
```

### 2. Make Your Changes

**Update Frontmatter**:
```yaml
---
lastUpdated: 2025-10-26  # Update date
currentRole: New Title    # Update if changed
status: open-to-opportunities  # Update status
---
```

**Update About Section**:
```markdown
## About

I'm a passionate software engineer specializing in distributed systems and cloud 
architecture. With 10+ years of experience, I focus on building scalable solutions 
that solve real-world problems.

I believe in continuous learning and sharing knowledge with the developer community.
```

**Add New Experience**:
```markdown
## Experience

:::experience
role: New Role
company: [New Company](https://newco.com)
start: 2025-01-15
end: current
---
- Achievement one
- Achievement two
:::

:::experience
role: Previous Role
company: [Previous Company](https://oldco.com)
start: 2020-01-15
end: 2024-12-31
---
- Past achievements
:::
```

**Update Skills**:
```markdown
:::skills
category: New Technology Area
---
Technology 1, Technology 2, Technology 3
:::
```

**Add Projects**:
```markdown
:::project
title: New Project Name
url: https://github.com/username/project
tech: F#, .NET, Azure
---
Project description with impact and outcomes.
:::
```

**Update Interests Section**:
```markdown
## Currently Interested In

I'm exploring advances in AI/ML and how they can enhance developer productivity. 
Always excited to discuss cloud architecture, functional programming, and building 
resilient distributed systems.

Open to opportunities where I can lead technical teams and drive innovation.
```

### 3. Preview Locally

Build and preview your changes:

```bash
# Build the site
dotnet run

# Start a local server (if available)
cd _public
python -m http.server 8000

# Open browser to http://localhost:8000/resume/
```

### 4. Validate Changes

Check for:
- ✅ All custom blocks have proper opening/closing markers
- ✅ Field names are correct (role, company, start, end, etc.)
- ✅ Date formats are YYYY-MM-DD
- ✅ Markdown links are properly formatted
- ✅ No missing separators (`---`)
- ✅ About and Interests sections use markdown headings (not frontmatter)
- ✅ Content renders correctly with proper formatting

### 5. Deploy

Once validated, commit and push your changes:

```bash
git add _src/resume/resume.md
git commit -m "Update resume with recent experience"
git push
```

The GitHub Actions workflow will automatically build and deploy your updated resume.

## Common Use Cases

### Adding Work Experience

```markdown
:::experience
role: Software Engineer II
company: [Tech Corp](https://techcorp.com)
start: 2023-06-01
end: current
---
- Developed microservices architecture using .NET Core
- Reduced API latency by 40% through optimization
- Mentored 3 junior developers on best practices
:::
```

### Showcasing Open Source Contributions

```markdown
:::project
title: ML.NET Documentation
url: https://github.com/dotnet/docs
tech: C#, Markdown, Machine Learning
---
Contributed comprehensive tutorials for ML.NET, helping thousands of 
developers get started with machine learning in .NET.

**Impact**: 50+ merged PRs, 100K+ documentation views.
:::
```

### Highlighting Skills with Links

```markdown
:::skills
category: Machine Learning & AI
---
[ML.NET](https://dot.net/ml), [TensorFlow](https://tensorflow.org), 
PyTorch, Scikit-learn, RAG Systems
:::
```

### Including Certifications in Education

```markdown
:::education
degree: AWS Certified Solutions Architect
institution: [Amazon Web Services](https://aws.amazon.com)
year: 2024
---
Professional level certification - Score: 890/1000
:::
```

### Adding Testimonials

```markdown
:::testimonial
author: Sarah Johnson, CTO at [StartupCo](https://startup.co)
---
An exceptional engineer who consistently delivers high-quality work. 
Their expertise in machine learning and ability to explain complex 
concepts made them invaluable to our team's success.
:::
```

## Troubleshooting

### Issue: Custom Blocks Not Rendering

**Symptoms**: Resume blocks appear as raw text instead of formatted sections.

**Solutions**:
1. Check opening marker format: `:::experience` (no spaces)
2. Verify closing marker: `:::` (exactly three colons)
3. Ensure content separator is present: `---`
4. Check that field names match expected names (case-insensitive)

**Example of Correct Format**:
```markdown
:::experience
role: Title
company: Company
start: 2020-01-01
end: current
---
Content here
:::
```

### Issue: Dates Not Parsing

**Symptoms**: Build errors or missing date display.

**Solutions**:
1. Use YYYY-MM-DD format: `2020-01-15` ✅
2. Not: `01/15/2020` ❌ or `Jan 15, 2020` ❌
3. For current position, use string: `end: current`
4. Not: `end: present` or `end: ongoing`

### Issue: Markdown Links Not Working

**Symptoms**: Links appear as plain text in company/institution names.

**Solutions**:
1. Use proper markdown link syntax: `[Text](URL)`
2. Example: `company: [Microsoft](https://microsoft.com)` ✅
3. Not: `company: Microsoft (https://microsoft.com)` ❌
4. Ensure URLs include protocol: `https://`

### Issue: Build Errors

**Common Build Errors**:

```
Warning: Failed to load resume from _src/resume/resume.md
```

**Solutions**:
1. Verify file exists at correct path
2. Check YAML frontmatter is valid
3. Ensure required fields are present
4. Look for YAML indentation errors

**Debugging Steps**:
```bash
# Check file exists
ls -la _src/resume/resume.md

# Validate YAML frontmatter
# Remove resume content temporarily, test with minimal frontmatter

# Build with verbose output
dotnet run 2>&1 | grep -i resume
```

### Issue: Styling Not Applied

**Symptoms**: Resume appears unstyled or with wrong colors.

**Solutions**:
1. Verify `_src/css/custom/resume.css` exists
2. Check that file is copied during build
3. Clear browser cache
4. Check for CSS syntax errors in resume.css

**Verify CSS Loading**:
```bash
# Check CSS file in output
ls -la _public/css/custom/resume.css

# Verify file size (should be ~10KB)
wc -c _public/css/custom/resume.css
```

### Issue: Mobile Layout Problems

**Symptoms**: Resume doesn't display well on mobile devices.

**Solutions**:
1. The responsive CSS should handle this automatically
2. Test at viewport width 768px and below
3. Check for overriding styles in other CSS files
4. Verify CSS media queries are not commented out

**Test Responsive Design**:
1. Open resume in browser
2. Open DevTools (F12)
3. Toggle device toolbar (Ctrl+Shift+M)
4. Test at various screen sizes

## Best Practices

### Content Guidelines

**Writing Effective Descriptions**:
- Use action verbs (Led, Developed, Implemented, Designed)
- Quantify achievements with metrics when possible
- Focus on impact and outcomes, not just duties
- Keep bullet points concise (1-2 lines)
- Tailor content to target roles

**Organizing Experience**:
- List positions in reverse chronological order (most recent first)
- Use "current" for ongoing positions
- Include all relevant positions, but prioritize recent ones
- Group multiple roles at same company as separate blocks

**Showcasing Skills**:
- Group related skills into logical categories
- Prioritize skills relevant to target roles
- Use industry-standard technology names
- Link to technology documentation when helpful
- Avoid subjective skill ratings (expert, intermediate, etc.)

### Maintenance Tips

**Regular Updates**:
- Update `lastUpdated` field with each change
- Add new experiences, projects, and skills as they occur
- Remove or archive very old content
- Keep testimonials current and relevant
- Review and refresh content annually

**Version Control**:
- Commit resume changes with descriptive messages
- Create backup branches before major redesigns
- Tag versions for specific applications: `git tag resume-v1.0`
- Keep resume history for reference

**Content Management**:
- Use consistent formatting across all sections
- Maintain parallel structure in bullet points
- Keep technology names and capitalizations consistent
- Update company links if they change
- Remove broken links promptly

### Professional Presentation

**Formatting Consistency**:
- Use consistent date formats throughout
- Maintain consistent tense (past for previous, present for current)
- Apply consistent punctuation (periods vs. no periods in bullets)
- Keep line lengths readable (wrap at ~80-100 characters)

**Content Balance**:
- Include 3-5 bullets per experience
- Showcase 3-6 notable projects
- List 3-5 skill categories
- Include 2-4 impactful testimonials
- Keep overall page length to 2-3 screen heights

**Link Strategy**:
- Link company names to their websites
- Link projects to GitHub, demos, or documentation
- Link technologies to official documentation
- Ensure all links are current and functional
- Test all links regularly

## Advanced Customization

### Custom CSS Modifications

To customize the resume appearance, edit `_src/css/custom/resume.css`:

**Change Color Scheme**:
```css
.resume-header {
    background: #your-color;
}

.experience-item {
    border-left-color: #your-accent;
}
```

**Modify Layout**:
```css
.resume-container {
    max-width: 900px;  /* Wider layout */
}

.skills-grid {
    grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
}
```

**Add Custom Sections**:
You can add custom sections with standard markdown:

```markdown
## Certifications

- AWS Certified Solutions Architect (2024)
- Microsoft Certified: Azure Developer (2023)
- Certified Kubernetes Administrator (2023)

## Languages

- English (Native)
- Spanish (Professional Working Proficiency)
- French (Elementary)
```

These appear as regular content sections without custom block styling.

### Extending Block Types

To add new custom block types (advanced):

1. Define data structure in `Domain.fs`
2. Create block type in `CustomBlocks.fs`
3. Implement parser class
4. Add renderer in `BlockRenderers.fs`
5. Register in `ResumeBlockExtension`
6. Update builder to extract and process new blocks

Refer to existing block implementations as templates.

## Related Documentation

- **[How to Create Starter Packs](how-to-create-starter-packs.md)** - Creating curated content collections
- **[Custom Presentation Layouts](custom-presentation-layouts.md)** - Presentation system with custom blocks
- **[Travel Guide How-To](travel-guide-howto.md)** - Location-based content with GPS
- **[VS Code Snippets](vs-code-snippets-modernization.md)** - Content creation shortcuts
- **[Core Infrastructure Architecture](core-infrastructure-architecture.md)** - System overview

## Support and Feedback

For issues, questions, or suggestions:
- Open a GitHub issue with the `resume` label
- Check existing documentation in `/docs` directory
- Review the source code implementation
- Test changes locally before deploying

## Credits

The resume feature was implemented in PR #664 with:
- Custom Markdig block parsers for structured content
- Responsive Desert theme integration
- Professional layout with accessibility support
- Support for rich markdown content in all sections
