module RollsBuilder

    open System.IO
    open Domain
    open OpmlService
    open ViewGenerator
    open PartialViews
    open BuilderCommon

    let buildBlogrollPage (links:Outline array) = 
        let blogRollContent = links |> blogRollView
        let blogRollPage = generate blogRollContent "default" "Blogroll - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"collections","blogroll")
        writePageToDir saveDir "index.html" blogRollPage

    let buildPodrollPage (links:Outline array) = 
        let podrollContent = links |> podRollView
        let podrollPage = generate podrollContent "default" "Podroll - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"collections","podroll")
        writePageToDir saveDir "index.html" podrollPage

    let buildForumsPage (links:Outline array) = 
        let forumContent = links |> forumsView
        let forumsPage = generate forumContent "default" "Forums - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"collections","forums")
        writePageToDir saveDir "index.html" forumsPage

    let buildYouTubeChannelsPage (links:Outline array) = 
        let ytContent = links |> youTubeFeedView
        let ytFeedPage = generate ytContent "default" "YouTube Channels - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"collections","youtube")
        writePageToDir saveDir "index.html" ytFeedPage

    let buildAIStarterPackPage (links:Outline array) = 
        let aiStarterPackContent = links |> aiStarterPackFeedView
        let ytFeedPage = generate aiStarterPackContent "default" "AI Starter Pack - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"collections","starter-packs","ai")
        writePageToDir saveDir "index.html" ytFeedPage

    let buildReadLaterPage (links:ReadLaterLink array) = 
        let readLaterContent = links |> readLaterView
        let readLaterPage = generate readLaterContent "default" "Read Later - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"resources","read-later")
        writePageToDir saveDir "index.html" readLaterPage

    let buildFeedsOpml (links:Outline array) = 
        let feed = buildOpmlFeed "Luis Quintanilla Feeds" "https://www.lqdev.me" links
        let saveDir = Path.Join(outputDir,"feed")
        writeFileToDir saveDir "index.opml" (feed.ToString())

    let buildBlogrollOpml (links:Outline array) = 
        let feed = buildOpmlFeed "Luis Quintanilla Blogroll" "https://www.lqdev.me" links
        let saveDir = Path.Join(outputDir,"collections","blogroll")
        let content = feed.ToString()
        writeFileToDir saveDir "index.xml" content
        writeFileToDir saveDir "index.opml" content

    let buildPodrollOpml (links:Outline array) = 
        let feed = buildOpmlFeed "Luis Quintanilla Podroll" "https://www.lqdev.me" links
        let saveDir = Path.Join(outputDir,"collections","podroll")
        let content = feed.ToString()
        writeFileToDir saveDir "index.xml" content
        writeFileToDir saveDir "index.opml" content

    let buildForumsOpml (links:Outline array) = 
        let feed = buildOpmlFeed "Luis Quintanilla Forums" "https://www.lqdev.me" links
        let saveDir = Path.Join(outputDir,"collections","forums")
        let content = feed.ToString()
        writeFileToDir saveDir "index.xml" content
        writeFileToDir saveDir "index.opml" content

    let buildYouTubeOpml (links:Outline array) = 
        let feed = buildOpmlFeed "Luis Quintanilla YouTube Channels" "https://www.lqdev.me" links
        let saveDir = Path.Join(outputDir,"collections","youtube")
        let content = feed.ToString()
        writeFileToDir saveDir "index.xml" content
        writeFileToDir saveDir "index.opml" content

    let buildAIStarterPackOpml (links:Outline array) = 
        let feed = buildOpmlFeed "Luis Quintanilla AI Starter Pack" "https://www.lqdev.me" links
        let saveDir = Path.Join(outputDir,"collections","starter-packs","ai")
        let content = feed.ToString()
        writeFileToDir saveDir "index.xml" content
        writeFileToDir saveDir "index.opml" content
