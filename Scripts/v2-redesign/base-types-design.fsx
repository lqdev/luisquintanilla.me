#r "nuget: YamlDotnet,12.0.0"

module Domain = 

    open System
    open YamlDotNet.Serialization

    [<CLIMutable>]
    type Location = {
        [<YamlMember(Alias="latitude")>] Latitude: float
        [<YamlMember(Alias="longitude")>] Longitude: float
    }

    [<CLIMutable>]
    type BusinessDetails = {
        [<YamlMember(Alias="name")>] Name: string
        [<YamlMember(Alias="location")>] Location: Location
    }

    [<CLIMutable>]
    type BusinessReview = {
        [<YamlMember(Alias="rating")>] Rating: float
        [<YamlMember(Alias="details")>] BusinessDetails: BusinessDetails
    }

    // Book Review
    [<CLIMutable>]
    type BookReview = 
        {
            [<YamlMember(Alias="title")>] Title: string
            [<YamlMember(Alias="author")>] Author: string
            [<YamlMember(Alias="rating")>] Rating: float
        }

    // Discriminated Unions don't work
    type Review = 
        | Business of BusinessReview
        | Book of BookReview

    [<CLIMutable>]
    type Post = {
        [<YamlMember(Alias="title")>] Title: string
        [<YamlMember(Alias="postType")>] PostType: Review
        [<YamlMember(Alias="date")>] Date: DateTime
        [<YamlMember(Alias="tags")>] Tags: string array
        [<YamlMember(Alias="categories")>] Categories : string array
    }

open YamlDotNet.Serialization
open System.IO
open Domain

let content = File.ReadAllText("/workspaces/luisquintanilla.me/Scripts/v2-redesign/posts/business-review.yml")

let deserializer = DeserializerBuilder().Build()
deserializer.Deserialize<Post>(content)