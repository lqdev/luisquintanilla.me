module MarkdownService

    open System
    open System.IO
    open System.Linq
    open Markdig
    open Markdig.Parsers
    open Markdig.Syntax
    open Markdig.Extensions.Yaml
    open YamlDotNet.Serialization
    open Domain

    let summarizePost (content:string) = 
        let doc = Markdown.Parse(content)
        let startP,endP = doc.Descendants<ParagraphBlock>().FirstOrDefault() |> fun x -> x.Span.Start,x.Span.End
        content.Substring(startP,endP).Trim()
        
    let convertFileToHtml (filePath:string) =
        filePath |> File.ReadAllText |> Markdown.ToHtml

    let ConvertMdToHtml (content:string) = 
        Markdown.ToHtml content

    let parseMarkdown (filePath:string) = 

        let yamlSerializer = 
            DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .Build()

        let mdPipeline = 
            MarkdownPipelineBuilder()
                .UseYamlFrontMatter()
                .Build()     
       
        let fileContents = filePath |> File.ReadAllText                
        
        let doc = Markdown.Parse(fileContents, mdPipeline)

        let yamlBlock = doc.Descendants<YamlFrontMatterBlock>().FirstOrDefault()

        let sanitizedYamlBlock = 
            fileContents
                .Substring(yamlBlock.Span.Start,yamlBlock.Span.Length)
                .Replace("---","")
                .Trim()

        let postDetails = yamlSerializer.Deserialize<PostDetails>(sanitizedYamlBlock)
        let mdBlock = 
            fileContents
                .Substring(yamlBlock.Span.Length)
                .Trim()

        { FileName = Path.GetFileNameWithoutExtension(filePath); Metadata = postDetails; Content = mdBlock}
        