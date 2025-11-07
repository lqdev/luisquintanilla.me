// Test script to validate static page pipeline produces identical output
#r "nuget: FSharp.Data, 5.0.2"
#r "nuget: Giraffe.ViewEngine, 1.4.0"
#r "nuget: Markdig, 0.38.0"
#r "nuget: YamlDotNet, 16.3.0"

#load "Domain.fs"
#load "CustomBlocks.fs"
#load "MediaTypes.fs"
#load "ASTParsing.fs"
#load "BlockRenderers.fs"
#load "Services/Markdown.fs"
#load "Services/Tag.fs"
#load "Views/ComponentViews.fs"
#load "Views/Layouts.fs"
#load "Views/LayoutViews.fs"
#load "Views/Generator.fs"
#load "Builders/IOUtils.fs"
#load "Builders/StaticPagePipeline.fs"
#load "Builders/StaticPageConfigs.fs"

open System.IO

let srcDir = "_src"
let testOutputDir = "_test_validation/static_pages_new"

// Clean test output directory
if Directory.Exists(testOutputDir) then
    Directory.Delete(testOutputDir, true)
Directory.CreateDirectory(testOutputDir) |> ignore

// Build static pages using new pipeline
StaticPagePipeline.buildStaticPages srcDir testOutputDir StaticPageConfigs.staticPageConfigs

printfn "✅ Static pages built successfully to %s" testOutputDir
printfn "Compare with legacy output in _public:"
printfn "  diff -r _public/about _test_validation/static_pages_new/about"
printfn "  diff -r _public/contact _test_validation/static_pages_new/contact"
printfn "  etc."
