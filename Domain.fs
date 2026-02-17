module Domain

    open System
    open System.Text.Json.Serialization
    open YamlDotNet.Serialization

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
        [<YamlMember(Alias="reading_time_minutes")>] ReadingTimeMinutes: int option
    }

    type Post = {
        FileName: string
        Metadata: PostDetails
        Content: string
        MarkdownSource: string option
    }
    with
        interface ITaggable with
            member this.Tags = 
                if isNull this.Metadata.Tags then [||]
                else this.Metadata.Tags
            member this.Title = this.Metadata.Title
            member this.Date = this.Metadata.Date
            member this.FileName = this.FileName
            member this.ContentType = "post"

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

    [<CLIMutable>]
    type ReadLaterLink = {
        [<JsonPropertyName("url")>] Url: string
        [<JsonPropertyName("title")>] Title: string
        [<JsonPropertyName("dateAdded")>] DateAdded: string
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
        [<YamlMember(Alias="tags")>] Tags: string
        [<YamlMember(Alias="date")>] Date: string
    }

    type Presentation = {
        FileName: string
        Metadata: PresentationDetails
        Content: string
        MarkdownSource: string option
    }
    with
        interface ITaggable with
            member this.Tags = 
                if String.IsNullOrEmpty(this.Metadata.Tags) then [||]
                else this.Metadata.Tags.Split(',') |> Array.map (fun s -> s.Trim())
            member this.Title = this.Metadata.Title
            member this.Date = this.Metadata.Date
            member this.FileName = this.FileName
            member this.ContentType = "presentation"

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
        MarkdownSource: string option
    }

    [<CLIMutable>]
    type SnippetDetails = {
        [<YamlMember(Alias="title")>] Title: string
        [<YamlMember(Alias="language")>] Language: string        
        [<YamlMember(Alias="tags")>] Tags: string
        [<YamlMember(Alias="created_date")>] CreatedDate: string
    }

    type Snippet = {
        FileName: string
        Metadata: SnippetDetails
        Content: string
        MarkdownSource: string option
    }
    with
        interface ITaggable with
            member this.Tags = 
                if String.IsNullOrEmpty(this.Metadata.Tags) then [||]
                else this.Metadata.Tags.Split(',') |> Array.map (fun s -> s.Trim())
            member this.Title = this.Metadata.Title
            member this.Date = 
                if String.IsNullOrEmpty(this.Metadata.CreatedDate) then ""
                else this.Metadata.CreatedDate
            member this.FileName = this.FileName
            member this.ContentType = "snippet"

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
        MarkdownSource: string option
    }
    with
        interface ITaggable with
            member this.Tags = 
                if String.IsNullOrEmpty(this.Metadata.Tags) then [||]
                else this.Metadata.Tags.Split(',') |> Array.map (fun s -> s.Trim())
            member this.Title = this.Metadata.Title
            member this.Date = 
                if String.IsNullOrEmpty(this.Metadata.LastUpdatedDate) then ""
                else this.Metadata.LastUpdatedDate
            member this.FileName = this.FileName
            member this.ContentType = "wiki"

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
    type PinnedPost = {
        [<JsonPropertyName("fileName")>] FileName: string
        [<JsonPropertyName("contentType")>] ContentType: string
        [<JsonPropertyName("order")>] Order: int
        [<JsonPropertyName("note")>] Note: string option
    }

    // Collection organizational approach
    type CollectionType = 
        | MediumFocused of medium: string  // "blogs", "podcasts", "youtube", "forums"
        | TopicFocused of topic: string    // "ai", "privacy", "fsharp", "indieweb"
        | Travel of category: string       // "city-guide", "national-parks", "restaurants", "bars"
        | Other of category: string        // "tags", "radio", etc.

    // Individual collection item (RSS feed source) - enhanced Outline
    [<CLIMutable>]
    type CollectionItem = {
        [<YamlMember(Alias="Title")>] Title: string
        [<YamlMember(Alias="Type")>] Type: string                 // "rss"
        [<YamlMember(Alias="HtmlUrl")>] HtmlUrl: string
        [<YamlMember(Alias="XmlUrl")>] XmlUrl: string
        [<YamlMember(Alias="Description")>] Description: string option   // Enhanced metadata
        [<YamlMember(Alias="Tags")>] Tags: string array option    // Topic tagging
        [<YamlMember(Alias="Added")>] Added: string option // When added to collection
    }

    // Unified collection metadata
    [<CLIMutable>]
    type Collection = {
        [<YamlMember(Alias="id")>] Id: string                    // "blogroll", "ai-starter-pack"
        [<YamlMember(Alias="title")>] Title: string                 // "Blogroll", "AI Starter Pack"
        [<YamlMember(Alias="description")>] Description: string           // User-facing description
        [<YamlMember(Alias="collectionType")>] CollectionType: CollectionType
        [<YamlMember(Alias="urlPath")>] UrlPath: string              // "/collections/blogroll/", "/collections/starter-packs/ai/"
        [<YamlMember(Alias="dataFile")>] DataFile: string             // "blogroll.json", "ai-starter-pack.json"
        [<YamlMember(Alias="tags")>] Tags: string array           // For cross-collection relationships
        [<YamlMember(Alias="lastUpdated")>] LastUpdated: string
        [<YamlMember(Alias="itemCount")>] ItemCount: int option        // Calculated during build
    }

    // Collection data structure
    type CollectionData = {
        Metadata: Collection
        Items: CollectionItem array
    }

    // Collection paths for output generation
    type CollectionPaths = {
        HtmlPath: string        // "/collections/blogroll/index.html"
        RssPath: string         // "/collections/blogroll/index.rss"
        OpmlPath: string        // "/collections/blogroll/index.opml"
        GpxPath: string option  // "/collections/travel/rome-favorites/rome-favorites.gpx"
        DataPath: string        // "/Data/blogroll.json"
    }

    // Navigation organization reflecting user mental models
    type NavigationSection = {
        Title: string                    // "Content Types", "Topic Guides"
        Description: string              // Section explanation
        Collections: Collection list     // Collections in this section
        Icon: string option             // Navigation icon
    }

    type NavigationStructure = {
        ContentTypes: NavigationSection      // Medium-focused collections
        TopicGuides: NavigationSection       // Topic-focused collections
        OtherCollections: NavigationSection  // Radio, Reviews, Tags
    }    

    [<CLIMutable>]
    type BookDetails = {
        [<YamlMember(Alias="title")>] Title: string
        [<YamlMember(Alias="author")>] Author: string        
        [<YamlMember(Alias="isbn")>] Isbn: string
        [<YamlMember(Alias="cover")>] Cover: string
        [<YamlMember(Alias="rating")>] Rating: float
        [<YamlMember(Alias="source")>] Source: string
        [<YamlMember(Alias="date_published")>] DatePublished: string
    }

    type Book = {
        FileName: string
        Metadata: BookDetails
        Content: string
        MarkdownSource: string option
    }
    with
        interface ITaggable with
            member this.Tags = [||] // Books don't have explicit tags
            member this.Title = this.Metadata.Title
            member this.Date = this.Metadata.DatePublished
            member this.FileName = this.FileName
            member this.ContentType = "book"

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
        [<YamlMember(Alias="published_date")>] Date: string
        [<YamlMember(Alias="tags")>] Tags: string array
        [<YamlMember(Alias="images")>] Images: AlbumImage array                
    }

    type Album = {
        FileName: string
        Metadata: AlbumDetails
        Content: string
        MarkdownSource: string option
    }
    with
        interface ITaggable with
            member this.Tags = 
                if isNull this.Metadata.Tags then [||]
                else this.Metadata.Tags
            member this.Title = this.Metadata.Title
            member this.Date = this.Metadata.Date
            member this.FileName = this.FileName
            member this.ContentType = "album"

    // Album Collection - Curated media groupings (events, themes, projects)
    [<CLIMutable>]
    type AlbumCollectionLocation = {
        [<YamlMember(Alias="lat")>] Latitude: float
        [<YamlMember(Alias="lon")>] Longitude: float
    }

    [<CLIMutable>]
    type AlbumCollectionDetails = {
        [<YamlMember(Alias="title")>] Title: string
        [<YamlMember(Alias="description")>] Description: string
        [<YamlMember(Alias="date")>] Date: string
        [<YamlMember(Alias="location")>] Location: AlbumCollectionLocation option
        [<YamlMember(Alias="tags")>] Tags: string array
    }

    type AlbumCollection = {
        FileName: string
        Metadata: AlbumCollectionDetails
        Content: string
        MarkdownSource: string option
    }
    with
        interface ITaggable with
            member this.Tags = 
                if isNull this.Metadata.Tags then [||]
                else this.Metadata.Tags
            member this.Title = this.Metadata.Title
            member this.Date = this.Metadata.Date
            member this.FileName = this.FileName
            member this.ContentType = "album-collection"

    // Playlist Collection - Curated music playlists (monthly discoveries, themed mixes)
    [<CLIMutable>]
    type PlaylistDetails = {
        [<YamlMember(Alias="title")>] Title: string
        [<YamlMember(Alias="description")>] Description: string option
        [<YamlMember(Alias="date")>] Date: string
        [<YamlMember(Alias="tags")>] Tags: string array
    }

    type PlaylistCollection = {
        FileName: string
        Metadata: PlaylistDetails
        Content: string  // Raw markdown content with track lists
        MarkdownSource: string option
    }
    with
        interface ITaggable with
            member this.Tags = 
                if isNull this.Metadata.Tags then [||]
                else this.Metadata.Tags
            member this.Title = this.Metadata.Title
            member this.Date = this.Metadata.Date
            member this.FileName = this.FileName
            member this.ContentType = "playlist-collection"

    type ResponseType = 
        | Reply
        | Star // Like / Favorite
        | Share // Repost / Retweet
        | Bookmark
        | Rsvp // RSVP to an event (yes, no, maybe, interested)

    [<CLIMutable>]
    type ResponseDetails = {
        [<YamlMember(Alias="title")>] Title: string
        [<YamlMember(Alias="targeturl")>] TargetUrl: string
        [<YamlMember(Alias="response_type")>] ResponseType: string
        [<YamlMember(Alias="rsvp_status")>] RsvpStatus: string option
        [<YamlMember(Alias="dt_published")>] DatePublished: string        
        [<YamlMember(Alias="dt_updated")>] DateUpdated: string
        [<YamlMember(Alias="tags")>] Tags: string array
        [<YamlMember(Alias="reading_time_minutes")>] ReadingTimeMinutes: int option
    }

    type Response = {
        FileName: string
        Metadata: ResponseDetails
        Content: string
        MarkdownSource: string option
    }
    with
        interface ITaggable with
            member this.Tags = this.Metadata.Tags
            member this.Title = this.Metadata.Title
            member this.Date = this.Metadata.DatePublished
            member this.FileName = this.FileName
            member this.ContentType = "response"

    [<CLIMutable>]
    type BookmarkDetails = {
        [<YamlMember(Alias="title")>] Title: string
        [<YamlMember(Alias="bookmark_of")>] BookmarkOf: string
        [<YamlMember(Alias="description")>] Description: string
        [<YamlMember(Alias="dt_published")>] DatePublished: string        
        [<YamlMember(Alias="dt_updated")>] DateUpdated: string
        [<YamlMember(Alias="tags")>] Tags: string array
    }

    type Bookmark = {
        FileName: string
        Metadata: BookmarkDetails
        Content: string
        MarkdownSource: string option
    }

    [<CLIMutable>]
    type RsvpDetails = {
        [<YamlMember(Alias="title")>] Title: string
        [<YamlMember(Alias="dt_published")>] DatePublished: string        
        [<YamlMember(Alias="dt_updated")>] DateUpdated: string
        [<YamlMember(Alias="tags")>] Tags: string array
    }

    type Rsvp = {
        FileName: string
        Metadata: RsvpDetails
        Content: string
    }
    with
        interface ITaggable with
            member this.Tags = 
                if isNull this.Metadata.Tags then [||]
                else this.Metadata.Tags
            member this.Title = this.Metadata.Title
            member this.Date = this.Metadata.DatePublished
            member this.FileName = this.FileName
            member this.ContentType = "rsvp"

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
        let getSnippetDate (snippet: Snippet) = snippet.Metadata.CreatedDate
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
        
        let getBookTags (_: Book) = [||] // Books don't have explicit tags
        let getBookTitle (book: Book) = book.Metadata.Title
        let getBookDate (book: Book) = book.Metadata.DatePublished
        let getBookFileName (book: Book) = book.FileName
        let getBookContentType (_: Book) = "book"
        
        let getPresentationTags (presentation: Presentation) = 
            if String.IsNullOrEmpty(presentation.Metadata.Tags) then [||]
            else presentation.Metadata.Tags.Split(',') |> Array.map (fun s -> s.Trim())
        let getPresentationTitle (presentation: Presentation) = presentation.Metadata.Title
        let getPresentationDate (presentation: Presentation) = presentation.Metadata.Date
        let getPresentationFileName (presentation: Presentation) = presentation.FileName
        let getPresentationContentType (_: Presentation) = "presentation"
        
        let getAlbumTags (album: Album) = 
            if isNull album.Metadata.Tags then [||] else album.Metadata.Tags
        let getAlbumTitle (album: Album) = album.Metadata.Title
        let getAlbumDate (album: Album) = album.Metadata.Date
        let getAlbumFileName (album: Album) = album.FileName
        let getAlbumContentType (_: Album) = "album"
        
        let getAlbumCollectionTags (albumCollection: AlbumCollection) = 
            if isNull albumCollection.Metadata.Tags then [||] else albumCollection.Metadata.Tags
        let getAlbumCollectionTitle (albumCollection: AlbumCollection) = albumCollection.Metadata.Title
        let getAlbumCollectionDate (albumCollection: AlbumCollection) = albumCollection.Metadata.Date
        let getAlbumCollectionFileName (albumCollection: AlbumCollection) = albumCollection.FileName
        let getAlbumCollectionContentType (_: AlbumCollection) = "album-collection"
        
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
            
        let bookAsTaggable (book: Book) = 
            createTaggableRecord (getBookTags book) (getBookTitle book) (getBookDate book) (getBookFileName book) (getBookContentType book)
            
        let presentationAsTaggable (presentation: Presentation) = 
            createTaggableRecord (getPresentationTags presentation) (getPresentationTitle presentation) (getPresentationDate presentation) (getPresentationFileName presentation) (getPresentationContentType presentation)
            
        let albumAsTaggable (album: Album) = 
            createTaggableRecord (getAlbumTags album) (getAlbumTitle album) (getAlbumDate album) (getAlbumFileName album) (getAlbumContentType album)
            
        let albumCollectionAsTaggable (albumCollection: AlbumCollection) = 
            createTaggableRecord (getAlbumCollectionTags albumCollection) (getAlbumCollectionTitle albumCollection) (getAlbumCollectionDate albumCollection) (getAlbumCollectionFileName albumCollection) (getAlbumCollectionContentType albumCollection)
            
        // Note: Notes are processed as Post types through NoteProcessor, so noteAsTaggable = postAsTaggable

    // GPX and Travel Recommendation Types
    [<CLIMutable>]
    type GpxPracticalInfo = {
        [<JsonPropertyName("price")>] Price: string option
        [<JsonPropertyName("hours")>] Hours: string option
        [<JsonPropertyName("phone")>] Phone: string option
        [<JsonPropertyName("website")>] Website: string option
    }

    type GpxCategory = 
        | Restaurant
        | Attraction  
        | Shopping
        | Hidden
        | Practical
        | GpxOther of string

    [<CLIMutable>]
    type GpxPlace = {
        [<JsonPropertyName("id")>] Id: string
        [<JsonPropertyName("name")>] Name: string
        [<JsonPropertyName("lat")>] Latitude: float
        [<JsonPropertyName("lon")>] Longitude: float
        [<JsonPropertyName("category")>] Category: string
        [<JsonPropertyName("description")>] Description: string
        [<JsonPropertyName("personalNote")>] PersonalNote: string option
        [<JsonPropertyName("practicalInfo")>] PracticalInfo: GpxPracticalInfo option
    }

    [<CLIMutable>]
    type GpxRoute = {
        [<JsonPropertyName("name")>] Name: string
        [<JsonPropertyName("description")>] Description: string
        [<JsonPropertyName("sequence")>] Sequence: string array
    }

    [<CLIMutable>]
    type TravelRecommendationData = {
        [<JsonPropertyName("title")>] Title: string
        [<JsonPropertyName("description")>] Description: string
        [<JsonPropertyName("places")>] Places: GpxPlace array
        [<JsonPropertyName("routes")>] Routes: GpxRoute array option
    }

    // Enhanced CollectionItem for travel recommendations
    [<CLIMutable>]
    type TravelCollectionItem = {
        [<YamlMember(Alias="Title")>] Title: string
        [<YamlMember(Alias="Type")>] Type: string                 // "travel"
        [<YamlMember(Alias="HtmlUrl")>] HtmlUrl: string
        [<YamlMember(Alias="XmlUrl")>] XmlUrl: string            // RSS feed
        [<YamlMember(Alias="GpxUrl")>] GpxUrl: string            // GPX download
        [<YamlMember(Alias="Description")>] Description: string option
        [<YamlMember(Alias="Tags")>] Tags: string array option
        [<YamlMember(Alias="Added")>] Added: string option
        [<YamlMember(Alias="TravelData")>] TravelData: TravelRecommendationData option
    }

    // =====================================================================
    // Resume Domain Model
    // =====================================================================

    /// Availability status for resume
    type AvailabilityStatus =
        | OpenToOpportunities
        | NotLooking
        | NotSpecified

    /// Work experience entry
    type Experience = {
        Role: string
        Company: string  // Can contain markdown links
        StartDate: DateTime
        EndDate: DateTime option  // None = current position
        Highlights: string list option
    }

    /// Project entry
    type Project = {
        Title: string
        Description: string
        Url: string option
        Technologies: string list option
        Highlights: string list option
    }

    /// Skill category grouping
    type SkillCategory = {
        Category: string
        Skills: string list  // Can contain markdown links
    }

    /// Education entry
    type Education = {
        Degree: string
        Institution: string  // Can contain markdown links
        GraduationYear: int option
        Details: string option
    }

    /// Testimonial/recommendation
    type Testimonial = {
        Quote: string
        Author: string  // Can contain markdown links
    }

    /// Complete resume data
    [<CLIMutable>]
    type ResumeMetadata = {
        [<YamlMember(Alias="title")>] Title: string
        [<YamlMember(Alias="lastUpdated")>] LastUpdated: string
        [<YamlMember(Alias="status")>] Status: string option
        [<YamlMember(Alias="summary")>] Summary: string option
        [<YamlMember(Alias="currentRole")>] CurrentRole: string
        [<YamlMember(Alias="contactLinks")>] ContactLinks: System.Collections.Generic.Dictionary<string, string>
        [<YamlMember(Alias="interests")>] Interests: string option
    }

    type Resume = {
        FileName: string
        Metadata: ResumeMetadata
        Content: string  // Full markdown content
        AboutSection: string option  // Extracted from ## About heading
        InterestsSection: string option  // Extracted from ## Interests or ## Currently Interested In heading
        Experience: Experience list
        Skills: SkillCategory list
        Projects: Project list
        Education: Education list
        Testimonials: Testimonial list
    }
