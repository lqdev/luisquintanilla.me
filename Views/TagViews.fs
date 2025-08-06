module TagViews

open Giraffe.ViewEngine
open Domain

/// Sanitize tag names for URL usage while preserving display text
let private sanitizeTagForUrl (tag: string) =
    tag.Replace("#", "sharp").Replace("/", "-").Replace(" ", "-").Replace("\"", "")

let tagLinkView (tags: string array) = 
    ul [] [
        for tag in tags do
            li [] [
                a [_href $"/tags/{sanitizeTagForUrl tag}"; _rel "tag"] [Text $"#{tag}"]
            ]
    ]

let tagPostLinkView (posts: Post array) (prefix:string) = 
    ul [] [
        for post in posts do
            li [] [
                a [_href $"/{prefix}/{post.FileName}"] [Text $"{post.Metadata.Title}"]
            ]
    ]

let tagResponseLinkView (responses: Response array) (prefix: string) = 
    ul [] [
        for post in responses do
            li [] [
                a [_href $"/{prefix}/{post.FileName}"] [Text $"{post.Metadata.Title}"]
            ]
    ]    

let allTagsView (tags: string array) = 
    let tagLinks = tagLinkView tags
    
    div [ _class "mr-auto" ] [ 
        h2 [] [Text "Tags"]
        p [] [Text "A list of tags for posts on this page"]
        tagLinks
    ]

let individualTagView (tagName:string) (posts:Post array) (notes:Post array) (responses:Response array) = 
    let postLinks = tagPostLinkView posts "posts"
    let noteLinks = tagPostLinkView notes "notes"
    let responseLinks = tagResponseLinkView responses "responses"

    div [ _class "mr-auto" ] [ 
        h2 [] [Text $"{tagName}"]
        p [] [Text $"A list of posts tagged {tagName}"]

        h3 [] [Text "Blogs"]
        postLinks

        h3 [] [Text "Notes"]
        noteLinks

        h3 [] [Text "Responses"]
        responseLinks
    ]
