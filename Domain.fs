module Domain

    open System
    open YamlDotNet.Serialization

    type RedirectDetails = (string * string * string)

    type YamlResult<'a> = {
        Yaml: 'a
        Content: string
    }

    // Unified content interface for tag processing
    type ITaggable = 
        abstract member Tags: string array
        abstract member Title: string
        abstract member Date: string
        abstract member FileName: string
        abstract member ContentType: string

    [<CLIMutable>]
    type PostDetails = {
        [<YamlMember(Alias="post_type")>] PostType: string
        [<YamlMember(Alias="title")>] Title: string
        [<YamlMember(Alias="description")>] Description: string
        [<YamlMember(Alias="published_date")>] Date: string
        [<YamlMember(Alias="tags")>] Tags: string array
    }

    type Post = {
        FileName: string
        Metadata: PostDetails
        Content: string
    }

    type Event = {
        Name: string
        Date: string
        Url: string
    }

    type Link = {
        Title: string
        Url: string
        Tags: string array
        DateAdded: string
    }

    type FeedPost = {
        Title: string
        PostType: string
        PublishedDate: string
        Source: string
        Content: string
    }

    [<CLIMutable>]
    type PresentationResource = {
        [<YamlMember(Alias="text")>] Text: string
        [<YamlMember(Alias="url")>] Url: string
    }

    [<CLIMutable>]
    type PresentationDetails = {
        [<YamlMember(Alias="title")>] Title: string
        [<YamlMember(Alias="resources")>] Resources: PresentationResource array
    }

    type Presentation = {
        FileName: string
        Metadata: PresentationDetails
        Content: string
    }

    [<CLIMutable>]
    type LivestreamResource = {
        [<YamlMember(Alias="text")>] Text: string
        [<YamlMember(Alias="url")>] Url: string
    }

    [<CLIMutable>]
    type LivestreamDetails = {
        [<YamlMember(Alias="title")>] Title: string
        [<YamlMember(Alias="video_url")>] VideoUrl: string
        [<YamlMember(Alias="published_date")>] Date: string
        [<YamlMember(Alias="resources")>] Resources: LivestreamResource array
    }

    type Livestream = {
        FileName: string
        Metadata: LivestreamDetails
        Content: string
    }

    [<CLIMutable>]
    type SnippetDetails = {
        [<YamlMember(Alias="title")>] Title: string
        [<YamlMember(Alias="language")>] Language: string        
        [<YamlMember(Alias="tags")>] Tags: string
    }

    type Snippet = {
        FileName: string
        Metadata: SnippetDetails
        Content: string
    }

    [<CLIMutable>]
    type WikiDetails = {
        [<YamlMember(Alias="title")>] Title: string
        [<YamlMember(Alias="last_updated_date")>] LastUpdatedDate: string
        [<YamlMember(Alias="tags")>] Tags: string        
    }

    type Wiki = {
        FileName: string
        Metadata: WikiDetails
        Content: string
    }

    type OpmlMetadata = 
        {
            Title: string
            OwnerId: string
        }

    type Outline = 
        {
            Title: string
            Type: string
            HtmlUrl: string
            XmlUrl: string
        }    

    [<CLIMutable>]
    type BookDetails = {
        [<YamlMember(Alias="title")>] Title: string
        [<YamlMember(Alias="author")>] Author: string        
        [<YamlMember(Alias="isbn")>] Isbn: string
        [<YamlMember(Alias="cover")>] Cover: string
        [<YamlMember(Alias="status")>] Status: string
        [<YamlMember(Alias="rating")>] Rating: float
        [<YamlMember(Alias="source")>] Source: string
    }

    type Book = {
        FileName: string
        Metadata: BookDetails
        Content: string
    }

    [<CLIMutable>]
    type AlbumImage = {
        [<YamlMember(Alias="imagepath")>] ImagePath: string
        [<YamlMember(Alias="description")>] Description: string
        [<YamlMember(Alias="alttext")>] AltText: string
    }

    [<CLIMutable>]
    type AlbumDetails = {
        [<YamlMember(Alias="post_type")>] PostType: string
        [<YamlMember(Alias="title")>] Title: string
        [<YamlMember(Alias="mainimage")>] MainImage: string
        [<YamlMember(Alias="published_date")>] Date: string
        [<YamlMember(Alias="images")>] Images: AlbumImage array                
    }

    type Album = {
        FileName: string
        Metadata: AlbumDetails
    }

    type ResponseType = 
        | Reply
        | Star // Like / Favorite
        | Share // Repost / Retweet
        | Bookmark

    [<CLIMutable>]
    type ResponseDetails = {
        [<YamlMember(Alias="title")>] Title: string
        [<YamlMember(Alias="targeturl")>] TargetUrl: string
        [<YamlMember(Alias="response_type")>] ResponseType: string
        [<YamlMember(Alias="dt_published")>] DatePublished: string        
        [<YamlMember(Alias="dt_updated")>] DateUpdated: string
        [<YamlMember(Alias="tags")>] Tags: string array
    }

    type Response = {
        FileName: string
        Metadata: ResponseDetails
        Content: string
    }

    type TaggedPosts = { Posts:Post array; Notes:Post array; Responses:Response array }

    // ITaggable helper functions for unified tag processing
    module ITaggableHelpers =
        
        let getPostTags (post: Post) = post.Metadata.Tags
        let getPostTitle (post: Post) = post.Metadata.Title
        let getPostDate (post: Post) = post.Metadata.Date
        let getPostFileName (post: Post) = post.FileName
        let getPostContentType (_: Post) = "post"
        
        let getSnippetTags (snippet: Snippet) = 
            if String.IsNullOrEmpty(snippet.Metadata.Tags) then [||]
            else snippet.Metadata.Tags.Split(',') |> Array.map (fun s -> s.Trim())
        let getSnippetTitle (snippet: Snippet) = snippet.Metadata.Title
        let getSnippetDate (_: Snippet) = "" // Snippets don't have dates
        let getSnippetFileName (snippet: Snippet) = snippet.FileName
        let getSnippetContentType (_: Snippet) = "snippet"
        
        let getWikiTags (wiki: Wiki) = 
            if String.IsNullOrEmpty(wiki.Metadata.Tags) then [||]
            else wiki.Metadata.Tags.Split(',') |> Array.map (fun s -> s.Trim())
        let getWikiTitle (wiki: Wiki) = wiki.Metadata.Title
        let getWikiDate (wiki: Wiki) = wiki.Metadata.LastUpdatedDate
        let getWikiFileName (wiki: Wiki) = wiki.FileName
        let getWikiContentType (_: Wiki) = "wiki"
        
        let getResponseTags (response: Response) = response.Metadata.Tags
        let getResponseTitle (response: Response) = response.Metadata.Title
        let getResponseDate (response: Response) = response.Metadata.DatePublished
        let getResponseFileName (response: Response) = response.FileName
        let getResponseContentType (_: Response) = "response"
        
        // Generic function to work with any ITaggable-like object
        let createTaggableRecord (tags: string array) (title: string) (date: string) (fileName: string) (contentType: string) =
            { new ITaggable with
                member _.Tags = tags
                member _.Title = title
                member _.Date = date
                member _.FileName = fileName
                member _.ContentType = contentType }
        
        let postAsTaggable (post: Post) = 
            createTaggableRecord (getPostTags post) (getPostTitle post) (getPostDate post) (getPostFileName post) (getPostContentType post)
            
        let snippetAsTaggable (snippet: Snippet) = 
            createTaggableRecord (getSnippetTags snippet) (getSnippetTitle snippet) (getSnippetDate snippet) (getSnippetFileName snippet) (getSnippetContentType snippet)
            
        let wikiAsTaggable (wiki: Wiki) = 
            createTaggableRecord (getWikiTags wiki) (getWikiTitle wiki) (getWikiDate wiki) (getWikiFileName wiki) (getWikiContentType wiki)
            
        let responseAsTaggable (response: Response) = 
            createTaggableRecord (getResponseTags response) (getResponseTitle response) (getResponseDate response) (getResponseFileName response) (getResponseContentType response)