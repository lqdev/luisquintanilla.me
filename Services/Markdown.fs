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

    let getContentAndMetadata<'a> (filePath:string) : YamlResult<'a> = 
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

        let yaml = yamlSerializer.Deserialize<'a>(sanitizedYamlBlock)
        let mdBlock = 
            fileContents
                .Substring(yamlBlock.Span.Length)
                .Trim()
        
        { Yaml=yaml ; Content=mdBlock }

    let parsePost (filePath:string) : Post = 

        let postDetails = getContentAndMetadata<PostDetails>(filePath)

        { FileName = Path.GetFileNameWithoutExtension(filePath); Metadata = postDetails.Yaml; Content = postDetails.Content}

    let parsePresentation (filePath:string) : Presentation = 
        
        let presentationDetails = getContentAndMetadata<PresentationDetails>(filePath)

        { FileName = Path.GetFileNameWithoutExtension(filePath); Metadata = presentationDetails.Yaml; Content = presentationDetails.Content }

    let parseSnippet (filePath:string) : Snippet = 
        let snippetDetails = getContentAndMetadata<SnippetDetails>(filePath);

        { FileName = Path.GetFileNameWithoutExtension(filePath); Metadata = snippetDetails.Yaml; Content = snippetDetails.Content }

    let parseWiki (filePath:string) : Wiki = 
        let wikiDetails = getContentAndMetadata<WikiDetails>(filePath);

        { FileName = Path.GetFileNameWithoutExtension(filePath); Metadata = wikiDetails.Yaml; Content = wikiDetails.Content }
