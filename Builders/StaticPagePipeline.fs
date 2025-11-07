module StaticPagePipeline

open System.IO
open IOUtils
open ViewGenerator
open MarkdownService
open LayoutViews  // Changed from ContentViews to LayoutViews

/// Configuration for building a static page
type StaticPageConfig = {
    /// Source markdown file relative to srcDir (e.g., "about.md")
    SourceFile: string
    /// Output directory relative to outputDir (e.g., "about")
    OutputDir: string
    /// Page title
    Title: string
    /// Layout template to use (e.g., "default", "defaultindex")
    Layout: string
}

/// Build a static page from markdown content
let buildStaticPage (srcDir: string) (outputDir: string) (config: StaticPageConfig) =
    let sourceFile = Path.Join(srcDir, config.SourceFile)
    let content = convertFileToHtml sourceFile |> contentView
    let page = generate content config.Layout config.Title
    
    let saveDir = Path.Join(outputDir, config.OutputDir)
    ensureDirectory saveDir |> ignore
    writeFile (Path.Join(saveDir, "index.html")) page

/// Build multiple static pages from a list of configurations
let buildStaticPages (srcDir: string) (outputDir: string) (configs: StaticPageConfig list) =
    configs
    |> List.iter (buildStaticPage srcDir outputDir)
