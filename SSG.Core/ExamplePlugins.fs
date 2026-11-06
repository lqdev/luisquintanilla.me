module SSG.Core.ExamplePlugins

open System
open System.IO
open System.Xml.Linq
open SSG.Core.Configuration
open SSG.Core.PluginRegistry

// =============================================================================
// Example Domain Types for Plugin Demonstration
// =============================================================================

/// Example blog post type
type BlogPost = {
    Title: string
    Content: string
    Date: DateTime
    Tags: string array
    Slug: string
    Author: string
    Description: string option
}

/// Example note type (microblog)
type Note = {
    Content: string
    Date: DateTime
    Tags: string array
    Slug: string
}

// =============================================================================
// Example Content Processor Plugins
// =============================================================================

/// Example blog post processor plugin
type BlogPostProcessor() =
    interface IContentProcessor<BlogPost> with
        member _.Name = "BlogPostProcessor"
        
        member _.Parse(filePath: string) : BlogPost option =
            try
                if File.Exists(filePath) then
                    let content = File.ReadAllText(filePath)
                    // Simple parsing example - in reality would use proper frontmatter parsing
                    Some {
                        Title = "Example Post"
                        Content = content
                        Date = DateTime.Now
                        Tags = [|"example"; "blog"|]
                        Slug = Path.GetFileNameWithoutExtension(filePath)
                        Author = "Example Author"
                        Description = None
                    }
                else
                    None
            with
            | ex -> 
                printfn $"Failed to parse blog post {filePath}: {ex.Message}"
                None
        
        member _.RenderHtml(post: BlogPost) : string =
            $"""<article>
                <header>
                    <h1>{post.Title}</h1>
                    <time datetime="{post.Date:yyyy-MM-dd}">{post.Date:MMMM dd, yyyy}</time>
                    <p>By {post.Author}</p>
                </header>
                <div class="content">
                    {post.Content}
                </div>
                <footer>
                    <div class="tags">
                        {post.Tags |> Array.map (fun tag -> $"<span class=\"tag\">{tag}</span>") |> String.concat " "}
                    </div>
                </footer>
            </article>"""
        
        member _.RenderCard(post: BlogPost) : string =
            $"""<div class="card post-card">
                <div class="card-body">
                    <h5 class="card-title">{post.Title}</h5>
                    <p class="card-text">{post.Description |> Option.defaultValue (post.Content.Substring(0, min 150 post.Content.Length) + "...")}</p>
                    <p class="card-text"><small class="text-muted">{post.Date:MMMM dd, yyyy}</small></p>
                </div>
            </div>"""
        
        member _.GenerateRssItem(post: BlogPost) : XElement option =
            try
                let item = XElement(XName.Get("item"),
                    XElement(XName.Get("title"), post.Title),
                    XElement(XName.Get("description"), post.Content),
                    XElement(XName.Get("pubDate"), post.Date.ToString("R")),
                    XElement(XName.Get("guid"), $"https://example.com/posts/{post.Slug}/"),
                    XElement(XName.Get("link"), $"https://example.com/posts/{post.Slug}/"),
                    XElement(XName.Get("author"), post.Author))
                
                // Add categories for tags
                for tag in post.Tags do
                    item.Add(XElement(XName.Get("category"), tag))
                
                Some item
            with
            | ex -> 
                printfn $"Failed to generate RSS item for {post.Title}: {ex.Message}"
                None
        
        member _.GetOutputPath(post: BlogPost) : string =
            $"posts/{post.Slug}/index.html"
        
        member _.GetSlug(post: BlogPost) : string =
            post.Slug
        
        member _.GetTags(post: BlogPost) : string array =
            post.Tags
        
        member _.GetDate(post: BlogPost) : string =
            post.Date.ToString("yyyy-MM-dd HH:mm zzz")

/// Example note processor plugin
type NoteProcessor() =
    interface IContentProcessor<Note> with
        member _.Name = "NoteProcessor"
        
        member _.Parse(filePath: string) : Note option =
            try
                if File.Exists(filePath) then
                    let content = File.ReadAllText(filePath)
                    Some {
                        Content = content
                        Date = DateTime.Now
                        Tags = [|"note"|]
                        Slug = Path.GetFileNameWithoutExtension(filePath)
                    }
                else
                    None
            with
            | ex -> 
                printfn $"Failed to parse note {filePath}: {ex.Message}"
                None
        
        member _.RenderHtml(note: Note) : string =
            $"""<article class="h-entry note">
                <div class="e-content">
                    {note.Content}
                </div>
                <footer>
                    <time class="dt-published" datetime="{note.Date:yyyy-MM-dd}">{note.Date:MMMM dd, yyyy}</time>
                </footer>
            </article>"""
        
        member _.RenderCard(note: Note) : string =
            $"""<div class="card note-card">
                <div class="card-body">
                    <p class="card-text">{note.Content.Substring(0, min 200 note.Content.Length) + if note.Content.Length > 200 then "..." else ""}</p>
                    <p class="card-text"><small class="text-muted">{note.Date:MMMM dd, yyyy}</small></p>
                </div>
            </div>"""
        
        member _.GenerateRssItem(note: Note) : XElement option =
            try
                let item = XElement(XName.Get("item"),
                    XElement(XName.Get("description"), note.Content),
                    XElement(XName.Get("pubDate"), note.Date.ToString("R")),
                    XElement(XName.Get("guid"), $"https://example.com/notes/{note.Slug}/"),
                    XElement(XName.Get("link"), $"https://example.com/notes/{note.Slug}/"))
                
                Some item
            with
            | ex -> 
                printfn $"Failed to generate RSS item for note {note.Slug}: {ex.Message}"
                None
        
        member _.GetOutputPath(note: Note) : string =
            $"notes/{note.Slug}/index.html"
        
        member _.GetSlug(note: Note) : string =
            note.Slug
        
        member _.GetTags(note: Note) : string array =
            note.Tags
        
        member _.GetDate(note: Note) : string =
            note.Date.ToString("yyyy-MM-dd HH:mm zzz")

// =============================================================================
// Example Custom Block Processors
// =============================================================================

/// Example quote block data
type QuoteBlock = {
    Quote: string
    Author: string option
    Source: string option
    Date: string option
}

/// Example quote block processor
type QuoteBlockProcessor() =
    interface ICustomBlockProcessor with
        member _.Name = "quote"
        
        member _.ParseBlock(content: string) : obj list =
            try
                // Simple parsing - in reality would use proper YAML parsing
                let lines = content.Split([|'\n'; '\r'|], StringSplitOptions.RemoveEmptyEntries)
                let quote = lines |> Array.tryHead |> Option.defaultValue ""
                
                let quoteBlock = {
                    Quote = quote
                    Author = None
                    Source = None
                    Date = None
                }
                
                [quoteBlock :> obj]
            with
            | ex ->
                printfn $"Failed to parse quote block: {ex.Message}"
                []
        
        member _.RenderHtml(obj: obj) : string =
            match obj with
            | :? QuoteBlock as quote ->
                let authorHtml = 
                    match quote.Author with
                    | Some author -> $"<cite>— {author}</cite>"
                    | None -> ""
                
                $"""<blockquote class="quote-block">
                    <p>{quote.Quote}</p>
                    {authorHtml}
                </blockquote>"""
            | _ -> ""
        
        member _.GetBlockType() : Type =
            typeof<QuoteBlock>

// =============================================================================
// Example View Plugin
// =============================================================================

/// Example view plugin that provides additional layout options
type ExampleViewPlugin() =
    interface IViewPlugin with
        member _.Name = "ExampleViewPlugin"
        
        member _.RenderLayout(content: string) (metadata: Map<string, obj>) : string =
            let title = metadata.TryFind("title") |> Option.map string |> Option.defaultValue "Untitled"
            let description = metadata.TryFind("description") |> Option.map string |> Option.defaultValue ""
            
            $"""<!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="UTF-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>{title}</title>
                <meta name="description" content="{description}">
            </head>
            <body>
                <header>
                    <nav>
                        <h1>Example Site</h1>
                    </nav>
                </header>
                <main>
                    {content}
                </main>
                <footer>
                    <p>&copy; 2024 Example Site</p>
                </footer>
            </body>
            </html>"""
        
        member _.SupportedLayouts = [|"default"; "post"; "page"|]

// =============================================================================
// Example Build Plugin
// =============================================================================

/// Example build plugin that runs additional processing
type ExampleBuildPlugin() =
    interface IBuildPlugin with
        member _.Name = "ExampleBuildPlugin"
        
        member _.PreBuild(config: SiteConfiguration) : unit =
            printfn $"ExampleBuildPlugin: Pre-build processing for {config.Site.Title}"
            // Could perform tasks like:
            // - Validating content
            // - Preprocessing assets
            // - Setting up external integrations
        
        member _.PostBuild(config: SiteConfiguration) : unit =
            printfn $"ExampleBuildPlugin: Post-build processing for {config.Site.Title}"
            // Could perform tasks like:
            // - Generating sitemaps
            // - Optimizing images
            // - Running deployment tasks
            // - Sending notifications
        
        member _.ProcessContent(contentType: string) (content: obj) : obj =
            // Could modify content before rendering
            // For example: add read time estimates, process shortcodes, etc.
            content

// =============================================================================
// Plugin Registration Helper
// =============================================================================

/// Register all example plugins with the registry
let registerExamplePlugins (registry: PluginRegistry) : unit =
    // Register content processors
    registry.RegisterContentProcessor(BlogPostProcessor())
    registry.RegisterContentProcessor(NoteProcessor())
    
    // Register custom block processors
    registry.RegisterCustomBlockProcessor(QuoteBlockProcessor())
    
    // Register view plugins
    registry.RegisterViewPlugin(ExampleViewPlugin())
    
    // Register build plugins
    registry.RegisterBuildPlugin(ExampleBuildPlugin())
    
    printfn "✅ Example plugins registered successfully"