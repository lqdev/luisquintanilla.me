module Domain

    open System
    open YamlDotNet.Serialization

    [<CLIMutable>]
    type PostDetails = {
        [<YamlMember(Alias="title")>] Title: string
        [<YamlMember(Alias="date")>] Date: string
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
