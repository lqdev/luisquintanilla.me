module Domain

    open System

    [<CLIMutable>]
    type PostDetails = {
        Title: string
        Date: string
        Tags: string array
    }

    type Post = {
        FileName: string
        Metadata: PostDetails
        Content: string
    }
