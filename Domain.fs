module Domain

    open System

    [<CLIMutable>]
    type PostDetails = {
        Title: string
        Date: string
        Tags: string array
    }

    type Post = {
        Metadata: PostDetails
        Content: string
    }
