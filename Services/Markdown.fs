module MarkdownService

    open System
    open System.IO
    open System.Linq
    open Markdig
    open Markdig.Syntax
    open Markdig.Extensions.Yaml
    open YamlDotNet.Serialization
    open Domain

    let convertToHtml (filePath:string) = 

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

        { Metadata = postDetails; Content = mdBlock }
        