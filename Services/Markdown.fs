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

    let mdToHtmlPipeline = 
            MarkdownPipelineBuilder()
                .UsePipeTables()
                .UseTaskLists()
                .UseDiagrams()
                .UseMediaLinks()
                .UseMathematics()
                .UseMediaLinks()
                .UseEmojiAndSmiley()
                .UseEmphasisExtras()
                .UseBootstrap()
                .UseFigures()
                |> CustomBlocks.useCustomBlocks
                |> fun builder -> builder.Build()        

    let summarizePost (content:string) = 
        let doc = Markdown.Parse(content)
        let startP,endP = doc.Descendants<ParagraphBlock>().FirstOrDefault() |> fun x -> x.Span.Start,x.Span.End
        content.Substring(startP,endP).Trim()

    let convertMdToHtml (content:string) =
        Markdown.ToHtml(content,mdToHtmlPipeline)

    let convertFileToHtml (filePath:string) =
        filePath 
        |> File.ReadAllText 
        |> convertMdToHtml

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
                .Trim([|'-'|])

        let yaml = yamlSerializer.Deserialize<'a>(sanitizedYamlBlock)
        let mdBlock = 
            fileContents
                .Substring(yamlBlock.Span.Length)
                .Trim()
        
        { Yaml=yaml ; Content=mdBlock }

    let parsePost (filePath:string) : Post = 

        let postDetails = getContentAndMetadata<PostDetails>(filePath)

        { FileName = Path.GetFileNameWithoutExtension(filePath); Metadata = postDetails.Yaml; Content = postDetails.Content; MarkdownSource = Some postDetails.Content}

    let parseLivestream (filePath:string) : Livestream = 
        
        let livestreamDetails = getContentAndMetadata<LivestreamDetails>(filePath)

        { FileName = Path.GetFileNameWithoutExtension(filePath); Metadata = livestreamDetails.Yaml; Content = livestreamDetails.Content; MarkdownSource = Some livestreamDetails.Content }


    let parseSnippet (filePath:string) : Snippet = 
        let snippetDetails = getContentAndMetadata<SnippetDetails>(filePath);

        { FileName = Path.GetFileNameWithoutExtension(filePath); Metadata = snippetDetails.Yaml; Content = snippetDetails.Content; MarkdownSource = Some snippetDetails.Content }

    let parseWiki (filePath:string) : Wiki = 
        let wikiDetails = getContentAndMetadata<WikiDetails>(filePath);

        { FileName = Path.GetFileNameWithoutExtension(filePath); Metadata = wikiDetails.Yaml; Content = wikiDetails.Content; MarkdownSource = Some wikiDetails.Content }

    let parseBook (filePath:string) : Book = 
        let bookDetails: YamlResult<BookDetails> = getContentAndMetadata<BookDetails>(filePath);

        { FileName = Path.GetFileNameWithoutExtension(filePath); Metadata = bookDetails.Yaml; Content = bookDetails.Content; MarkdownSource = Some bookDetails.Content }

    let parseAlbum (filePath:string) : Album = 
        let albumDetails = getContentAndMetadata<AlbumDetails>(filePath);

        { FileName = Path.GetFileNameWithoutExtension(filePath); Metadata = albumDetails.Yaml; Content = albumDetails.Content; MarkdownSource = Some albumDetails.Content }

    let parseAlbumCollection (filePath:string) : AlbumCollection = 
        let albumCollectionDetails = getContentAndMetadata<AlbumCollectionDetails>(filePath);

        { FileName = Path.GetFileNameWithoutExtension(filePath); Metadata = albumCollectionDetails.Yaml; Content = albumCollectionDetails.Content; MarkdownSource = Some albumCollectionDetails.Content }

    let parseRsvp (filePath:string) : Rsvp = 
        let rsvpDetails = getContentAndMetadata<RsvpDetails>(filePath);

        { FileName = Path.GetFileNameWithoutExtension(filePath); Metadata = rsvpDetails.Yaml; Content = rsvpDetails.Content }

