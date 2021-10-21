module Domain

    open System
    open YamlDotNet.Serialization

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
        [<YamlMember(Alias="tags")>] Tags: string
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
