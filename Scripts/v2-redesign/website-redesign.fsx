#r "nuget: YamlDotnet,12.0.0"
#r "nuget: Markdig, 0.30.3"

module Domain = 

    open System
    open YamlDotNet.Serialization

    // Reviews
    [<CLIMutable>]
    type BookReview = 
        {
            [<YamlMember(Alias="title")>] Title: string
            [<YamlMember(Alias="author")>] Author: string        
            [<YamlMember(Alias="isbn")>] Isbn: string option
            [<YamlMember(Alias="cover_image")>] CoverImage: string option
            [<YamlMember(Alias="publish_date")>] PublishDate: string option
            [<YamlMember(Alias="source")>] Source: string
        }

    [<CLIMutable>]
    type MovieReview =
        {
            [<YamlMember(Alias="title")>] Title : string
            [<YamlMember(Alias="director")>] Director : string option
            [<YamlMember(Alias="cover_image")>] CoverImage : string option
            [<YamlMember(Alias="release_date")>] ReleaseDate : DateTime option
            [<YamlMember(Alias="source")>] Source : string
        }

    [<CLIMutable>]
    type AlbumReview = 
        {
            [<YamlMember(Alias="title")>] Title : string
            [<YamlMember(Alias="artist")>] Artist : string option
            [<YamlMember(Alias="cover_image")>] CoverImage : string option
            [<YamlMember(Alias="release_date")>] ReleaseDate: DateTime option
            [<YamlMember(Alias="source")>] Source: string
        }

    // type ReviewType = 
    //     | BookReview of BookReview
    //     | MovieReview of MovieReview
    //     | AlbumReview of AlbumReview

    [<CLIMutable>]
    type ReviewDetails = 
        {
            [<YamlMember(Alias="review_type")>] ReviewType : string
            [<YamlMember(Alias="rating")>] Rating : float
            [<YamlMember(Alias="scale")>] Scale : float 
        }

    // Post Types
    [<CLIMutable>]
    type PostMedatadata = 
        {
            [<YamlMember(Alias="title")>] Title : string
            [<YamlMember(Alias="post_type")>] PostType: string
            [<YamlMember(Alias="date_published")>] DatePublished : DateTimeOffset
            [<YamlMember(Alias="date_updated")>] DateUpdated : DateTimeOffset
            [<YamlMember(Alias="tags")>] Tags : string array
        }

    [<CLIMutable>]
    type ReviewPost = 
        {
            [<YamlMember(Alias="metadata")>] Metadata : PostMedatadata
            [<YamlMember(Alias="review_details")>] ReviewDetails: ReviewDetails
        }

    type PostType = 
        | Review of ReviewPost


    type YamlResult<'a> = 
        {
            Yaml: 'a
            Content: string
        }

    type Post = 
        {
            FileName: string
            Post: PostType
        }

module ParseService = 

    open System.IO
    open System.Linq
        
    open YamlDotNet.Serialization
    open Markdig
    open Markdig.Parsers
    open Markdig.Syntax
    open Markdig.Extensions.Yaml
    open YamlDotNet.Serialization
    
    open Domain
    

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

        let details = getContentAndMetadata<Post>(filePath)

        let parsedPost = 
            match details.Yaml.Post with
            | Review x -> Review x

        let post = { FileName = Path.GetFileNameWithoutExtension(filePath); Post = parsedPost }

        post

module UI = 

    open Domain

    printfn "UI"
