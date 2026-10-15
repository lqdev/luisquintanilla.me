module RollsBuilder

    open System.IO
    open Domain
    open OpmlService
    open ViewGenerator
    open PartialViews
    open BuilderCommon

    let buildBlogrollPage (links:Outline array) = 
        let blogRollContent = links |> blogRollView
        let blogRollPage =
            generatePage
                (pageMetadata "/collections/blogroll/" "Blogs followed by Luis Quintanilla." "website")
                blogRollContent
                "default"
                "Blogroll - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"collections","blogroll")
        writePageToDir saveDir "index.html" blogRollPage

    let buildPodrollPage (links:Outline array) = 
        let podrollContent = links |> podRollView
        let podrollPage =
            generatePage
                (pageMetadata "/collections/podroll/" "Podcasts followed by Luis Quintanilla." "website")
                podrollContent
                "default"
                "Podroll - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"collections","podroll")
        writePageToDir saveDir "index.html" podrollPage

    let buildForumsPage (links:Outline array) = 
        let forumContent = links |> forumsView
        let forumsPage =
            generatePage
                (pageMetadata "/collections/forums/" "Forums followed by Luis Quintanilla." "website")
                forumContent
                "default"
                "Forums - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"collections","forums")
        writePageToDir saveDir "index.html" forumsPage

    let buildYouTubeChannelsPage (links:Outline array) = 
        let ytContent = links |> youTubeFeedView
        let ytFeedPage =
            generatePage
                (pageMetadata "/collections/youtube/" "YouTube channels followed by Luis Quintanilla." "website")
                ytContent
                "default"
                "YouTube Channels - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"collections","youtube")
        writePageToDir saveDir "index.html" ytFeedPage

    let buildAIStarterPackPage (links:Outline array) = 
        let aiStarterPackContent = links |> aiStarterPackFeedView
        let ytFeedPage =
            generatePage
                (pageMetadata "/collections/starter-packs/ai/" "An AI-focused starter pack curated by Luis Quintanilla." "website")
                aiStarterPackContent
                "default"
                "AI Starter Pack - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"collections","starter-packs","ai")
        writePageToDir saveDir "index.html" ytFeedPage

    let buildReadLaterPage (links:ReadLaterLink array) = 
        let readLaterContent = links |> readLaterView
        let readLaterPage =
            generatePage
                (pageMetadata "/resources/read-later/" "Articles and resources Luis Quintanilla plans to read later." "website")
                readLaterContent
                "default"
                "Read Later - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"resources","read-later")
        writePageToDir saveDir "index.html" readLaterPage

    let buildFeedsOpml (links:Outline array) = 
        let feed = buildOpmlFeed "Luis Quintanilla Feeds" Constants.Urls.canonical links
        let saveDir = Path.Join(outputDir,"feed")
        writeFileToDir saveDir "index.opml" (feed.ToString())

    let buildBlogrollOpml (links:Outline array) = 
        let feed = buildOpmlFeed "Luis Quintanilla Blogroll" Constants.Urls.canonical links
        let saveDir = Path.Join(outputDir,"collections","blogroll")
        let content = feed.ToString()
        writeFileToDir saveDir "index.xml" content
        writeFileToDir saveDir "index.opml" content

    let buildPodrollOpml (links:Outline array) = 
        let feed = buildOpmlFeed "Luis Quintanilla Podroll" Constants.Urls.canonical links
        let saveDir = Path.Join(outputDir,"collections","podroll")
        let content = feed.ToString()
        writeFileToDir saveDir "index.xml" content
        writeFileToDir saveDir "index.opml" content

    let buildForumsOpml (links:Outline array) = 
        let feed = buildOpmlFeed "Luis Quintanilla Forums" Constants.Urls.canonical links
        let saveDir = Path.Join(outputDir,"collections","forums")
        let content = feed.ToString()
        writeFileToDir saveDir "index.xml" content
        writeFileToDir saveDir "index.opml" content

    let buildYouTubeOpml (links:Outline array) = 
        let feed = buildOpmlFeed "Luis Quintanilla YouTube Channels" Constants.Urls.canonical links
        let saveDir = Path.Join(outputDir,"collections","youtube")
        let content = feed.ToString()
        writeFileToDir saveDir "index.xml" content
        writeFileToDir saveDir "index.opml" content

    let buildAIStarterPackOpml (links:Outline array) = 
        let feed = buildOpmlFeed "Luis Quintanilla AI Starter Pack" Constants.Urls.canonical links
        let saveDir = Path.Join(outputDir,"collections","starter-packs","ai")
        let content = feed.ToString()
        writeFileToDir saveDir "index.xml" content
        writeFileToDir saveDir "index.opml" content
