module Domain

    open System
    open YamlDotNet.Serialization

    type RedirectDetails = (string * string * string)

    type YamlResult<'a> = {
        Yaml: 'a
        Content: string
    }

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
        [<YamlMember(Alias="title")>] Title: string
        [<YamlMember(Alias="mainimage")>] MainImage: string
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