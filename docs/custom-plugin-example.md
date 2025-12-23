# Custom Plugin Development Example

This document demonstrates how to extend the decoupled SSG.Core with custom plugins.

## Example: Creating a Tutorial Content Type Plugin

### 1. Define Domain Type

```fsharp
// MyPlugin.fs
module MyPlugin

open System
open System.IO
open System.Xml.Linq
open SSG.Core.PluginRegistry

// Custom domain type for tutorials
type Tutorial = {
    Title: string
    Content: string
    Author: string
    Date: DateTime
    Difficulty: string // "Beginner", "Intermediate", "Advanced"
    Tags: string array
    Slug: string
    Duration: int option // minutes
    Prerequisites: string array
}
```

### 2. Implement Content Processor Plugin

```fsharp
// Custom content processor for tutorials
type TutorialProcessor() =
    interface IContentProcessor<Tutorial> with
        member _.Name = "TutorialProcessor"
        
        member _.Parse(filePath: string) : Tutorial option =
            try
                if File.Exists(filePath) then
                    let content = File.ReadAllText(filePath)
                    // Parse YAML frontmatter and content
                    // This would use proper frontmatter parsing in reality
                    Some {
                        Title = "Sample Tutorial"
                        Content = content
                        Author = "Tutorial Author"
                        Date = DateTime.Now
                        Difficulty = "Intermediate"
                        Tags = [|"tutorial"; "learning"|]
                        Slug = Path.GetFileNameWithoutExtension(filePath)
                        Duration = Some 30
                        Prerequisites = [|"Basic F#"; "Git knowledge"|]
                    }
                else
                    None
            with
            | ex -> 
                printfn $"Failed to parse tutorial {filePath}: {ex.Message}"
                None
        
        member _.RenderHtml(tutorial: Tutorial) : string =
            $"""<article class="tutorial">
                <header>
                    <h1>{tutorial.Title}</h1>
                    <div class="tutorial-meta">
                        <p><strong>Author:</strong> {tutorial.Author}</p>
                        <p><strong>Difficulty:</strong> {tutorial.Difficulty}</p>
                        <p><strong>Duration:</strong> {tutorial.Duration |> Option.map string |> Option.defaultValue "N/A"} minutes</p>
                        <p><strong>Date:</strong> {tutorial.Date:MMMM dd, yyyy}</p>
                    </div>
                    {if tutorial.Prerequisites.Length > 0 then 
                        $"<div class=\"prerequisites\">
                            <h3>Prerequisites:</h3>
                            <ul>
                                {tutorial.Prerequisites |> Array.map (fun req -> $"<li>{req}</li>") |> String.concat ""}
                            </ul>
                        </div>"
                     else ""}
                </header>
                <div class="tutorial-content">
                    {tutorial.Content}
                </div>
                <footer>
                    <div class="tutorial-tags">
                        {tutorial.Tags |> Array.map (fun tag -> $"<span class=\"tag difficulty-{tutorial.Difficulty.ToLower()}\">{tag}</span>") |> String.concat " "}
                    </div>
                </footer>
            </article>"""
        
        member _.RenderCard(tutorial: Tutorial) : string =
            let difficultyClass = tutorial.Difficulty.ToLower()
            let durationText = tutorial.Duration |> Option.map (fun d -> $"{d} min") |> Option.defaultValue ""
            
            $"""<div class="card tutorial-card difficulty-{difficultyClass}">
                <div class="card-header">
                    <span class="difficulty-badge">{tutorial.Difficulty}</span>
                    <span class="duration-badge">{durationText}</span>
                </div>
                <div class="card-body">
                    <h5 class="card-title">{tutorial.Title}</h5>
                    <p class="card-text">{tutorial.Content.Substring(0, min 150 tutorial.Content.Length) + "..."}</p>
                    <p class="card-text"><small class="text-muted">By {tutorial.Author} • {tutorial.Date:MMMM dd, yyyy}</small></p>
                </div>
            </div>"""
        
        member _.GenerateRssItem(tutorial: Tutorial) : XElement option =
            try
                let item = XElement(XName.Get("item"),
                    XElement(XName.Get("title"), tutorial.Title),
                    XElement(XName.Get("description"), tutorial.Content),
                    XElement(XName.Get("pubDate"), tutorial.Date.ToString("R")),
                    XElement(XName.Get("guid"), $"https://example.com/tutorials/{tutorial.Slug}/"),
                    XElement(XName.Get("link"), $"https://example.com/tutorials/{tutorial.Slug}/"),
                    XElement(XName.Get("author"), tutorial.Author))
                
                // Add custom elements for tutorial metadata
                item.Add(XElement(XName.Get("difficulty"), tutorial.Difficulty))
                if tutorial.Duration.IsSome then
                    item.Add(XElement(XName.Get("duration"), tutorial.Duration.Value))
                
                // Add categories for tags
                for tag in tutorial.Tags do
                    item.Add(XElement(XName.Get("category"), tag))
                
                Some item
            with
            | ex -> 
                printfn $"Failed to generate RSS item for {tutorial.Title}: {ex.Message}"
                None
        
        member _.GetOutputPath(tutorial: Tutorial) : string =
            $"tutorials/{tutorial.Slug}/index.html"
        
        member _.GetSlug(tutorial: Tutorial) : string =
            tutorial.Slug
        
        member _.GetTags(tutorial: Tutorial) : string array =
            tutorial.Tags
        
        member _.GetDate(tutorial: Tutorial) : string =
            tutorial.Date.ToString("yyyy-MM-dd HH:mm zzz")
```

### 3. Create Custom Block Plugin

```fsharp
// Custom block for code exercises within tutorials
type CodeExercise = {
    Title: string
    Instructions: string
    StarterCode: string option
    Solution: string option
    Hints: string array
}

type CodeExerciseBlockProcessor() =
    interface ICustomBlockProcessor with
        member _.Name = "code-exercise"
        
        member _.ParseBlock(content: string) : obj list =
            try
                // Parse YAML content for exercise data
                // Simplified parsing for demo
                let lines = content.Split([|'\n'; '\r'|], StringSplitOptions.RemoveEmptyEntries)
                let title = lines |> Array.tryHead |> Option.defaultValue "Exercise"
                
                let exercise = {
                    Title = title
                    Instructions = "Follow the instructions below..."
                    StarterCode = Some "// Your code here"
                    Solution = Some "// Solution code"
                    Hints = [|"Remember to check edge cases"; "Use pattern matching"|]
                }
                
                [exercise :> obj]
            with
            | ex ->
                printfn $"Failed to parse code exercise block: {ex.Message}"
                []
        
        member _.RenderHtml(obj: obj) : string =
            match obj with
            | :? CodeExercise as exercise ->
                let hintsHtml = 
                    if exercise.Hints.Length > 0 then
                        let hintsList = exercise.Hints |> Array.mapi (fun i hint -> $"<li>{hint}</li>") |> String.concat ""
                        $"""<div class="exercise-hints">
                            <h4>💡 Hints</h4>
                            <ol>{hintsList}</ol>
                        </div>"""
                    else ""
                
                let starterCodeHtml =
                    match exercise.StarterCode with
                    | Some code -> $"""<div class="starter-code">
                        <h4>🚀 Starting Code</h4>
                        <pre><code class="language-fsharp">{code}</code></pre>
                    </div>"""
                    | None -> ""
                
                let solutionHtml = 
                    match exercise.Solution with
                    | Some solution -> $"""<details class="exercise-solution">
                        <summary>🎯 Show Solution</summary>
                        <pre><code class="language-fsharp">{solution}</code></pre>
                    </details>"""
                    | None -> ""
                
                $"""<div class="code-exercise">
                    <div class="exercise-header">
                        <h3>🎯 {exercise.Title}</h3>
                    </div>
                    <div class="exercise-instructions">
                        <p>{exercise.Instructions}</p>
                    </div>
                    {starterCodeHtml}
                    {hintsHtml}
                    {solutionHtml}
                </div>"""
            | _ -> ""
        
        member _.GetBlockType() : Type =
            typeof<CodeExercise>
```

### 4. Create Build Plugin for Additional Processing

```fsharp
// Build plugin for tutorial-specific processing
type TutorialBuildPlugin() =
    interface IBuildPlugin with
        member _.Name = "TutorialBuildPlugin"
        
        member _.PreBuild(config: SiteConfiguration) : unit =
            printfn "🎓 TutorialBuildPlugin: Setting up tutorial environment"
            
            // Create tutorial-specific directories
            let tutorialDir = Path.Join(config.Directories.Output, "tutorials")
            let resourcesDir = Path.Join(tutorialDir, "resources")
            let exercisesDir = Path.Join(tutorialDir, "exercises")
            
            [tutorialDir; resourcesDir; exercisesDir]
            |> List.iter (fun dir -> 
                if not (Directory.Exists(dir)) then
                    Directory.CreateDirectory(dir) |> ignore
                    printfn $"  Created directory: {dir}")
        
        member _.PostBuild(config: SiteConfiguration) : unit =
            printfn "🎓 TutorialBuildPlugin: Generating tutorial index and navigation"
            
            // Generate tutorial difficulty index
            let tutorialIndexPath = Path.Join(config.Directories.Output, "tutorials", "by-difficulty.html")
            let indexHtml = """<html>
                <head><title>Tutorials by Difficulty</title></head>
                <body>
                    <h1>Browse Tutorials by Difficulty</h1>
                    <div class="difficulty-filters">
                        <a href="#beginner" class="filter-btn">Beginner</a>
                        <a href="#intermediate" class="filter-btn">Intermediate</a>
                        <a href="#advanced" class="filter-btn">Advanced</a>
                    </div>
                    <!-- Tutorial list would be generated here -->
                </body>
            </html>"""
            
            File.WriteAllText(tutorialIndexPath, indexHtml)
            printfn $"  Generated tutorial index: {tutorialIndexPath}"
        
        member _.ProcessContent(contentType: string) (content: obj) : obj =
            if contentType = "tutorials" then
                match content with
                | :? Tutorial as tutorial ->
                    // Add estimated reading time based on content length
                    let wordCount = tutorial.Content.Split([|' '; '\n'; '\r'|], StringSplitOptions.RemoveEmptyEntries).Length
                    let readingTime = max 1 (wordCount / 200) // ~200 words per minute
                    
                    // Add enhanced tutorial with reading time
                    { tutorial with Duration = Some (tutorial.Duration |> Option.defaultValue 0 |> (+) readingTime) } :> obj
                | _ -> content
            else
                content

// Register all tutorial plugins
let registerTutorialPlugins (registry: PluginRegistry) : unit =
    registry.RegisterContentProcessor(TutorialProcessor())
    registry.RegisterCustomBlockProcessor(CodeExerciseBlockProcessor())
    registry.RegisterBuildPlugin(TutorialBuildPlugin())
    
    printfn "✅ Tutorial plugins registered successfully"
```

### 5. Update Configuration to Use Custom Plugins

```json
{
  "contentTypes": [
    {
      "name": "tutorials",
      "sourceDirectory": "tutorials",
      "processor": "TutorialProcessor",
      "enabled": true,
      "urlPattern": "/tutorials/{slug}/",
      "feedEnabled": true,
      "archiveEnabled": true
    }
  ],
  "plugins": {
    "customBlocks": [
      {
        "name": "code-exercise",
        "enabled": true,
        "parser": "CodeExerciseBlockProcessor",
        "renderer": "CodeExerciseBlockHtmlRenderer"
      }
    ]
  }
}
```

### 6. Use in Content

Create `_src/tutorials/fsharp-basics.md`:

```markdown
---
title: "F# Pattern Matching Tutorial"
author: "Tutorial Author"
date: "2024-01-15 10:00 -05:00"
difficulty: "Intermediate"
duration: 45
tags: ["fsharp", "pattern-matching", "functional-programming"]
prerequisites: ["Basic F# syntax", "Understanding of types"]
---

# F# Pattern Matching Tutorial

Learn the power of pattern matching in F#...

:::code-exercise
title: "Basic Pattern Matching"
instructions: "Implement a function that matches different list patterns"
starter-code: |
  let processList items =
    match items with
    | [] -> // Your code here
    | [single] -> // Your code here
    | head::tail -> // Your code here

hints:
  - "Empty lists are represented as []"  
  - "Single item lists can be matched with [item]"
  - "Use :: to deconstruct head and tail"

solution: |
  let processList items =
    match items with
    | [] -> "Empty list"
    | [single] -> $"Single item: {single}"
    | head::tail -> $"Head: {head}, Tail length: {tail.Length}"
:::

More tutorial content here...
```

## Benefits Achieved

### ✅ Zero Code Changes to Framework
- Add new content types through configuration
- Custom processing through plugin interfaces
- No modifications to SSG.Core required

### ✅ Rich Extensibility
- Custom domain types with specialized rendering
- Custom Markdown blocks for interactive content  
- Build pipeline hooks for additional processing
- RSS feed extensions with custom metadata

### ✅ Plugin Ecosystem Ready
- Shareable plugin assemblies
- Configuration-based plugin discovery
- Type-safe plugin interfaces
- Comprehensive plugin lifecycle management

This example demonstrates how the decoupled SSG.Core enables powerful extensibility while maintaining clean separation between framework and customization code.