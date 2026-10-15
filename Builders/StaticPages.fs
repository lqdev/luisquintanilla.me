module StaticPagesBuilder

    open System
    open System.IO
    open System.Text.Json
    open Giraffe.ViewEngine
    open Domain
    open MarkdownService
    open ViewGenerator
    open PartialViews
    open BuilderCommon

    let buildAboutPage () = 
        let aboutPath = Path.Join(srcDir,"about.md")
        let about = parseAbout aboutPath
        let aboutHtml = convertMdToHtml about.Content
        // Wrap the standard content view and append a ProfilePage @graph whose
        // mainEntity is the shared #person node (site-wide identity graph).
        let aboutContent =
            div [ _class "mr-auto" ] [
                rawText aboutHtml
                script [ _type "application/ld+json" ] [
                    rawText (StructuredData.profilePageJson "/about/")
                ]
            ]
        let aboutPage =
            generatePage
                (pageMetadata "/about/" Constants.Author.bio "profile")
                aboutContent
                "default"
                "About - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"about")
        writePageToDir saveDir "index.html" aboutPage

        let discoveryDir = Path.Join(outputDir, ".well-known")
        Directory.CreateDirectory(discoveryDir) |> ignore
        File.Copy(aboutPath, Path.Join(discoveryDir, "about.md"), true)

    let buildCollectionsPage () = 
        let collectionsContent = convertFileToHtml (Path.Join(srcDir,"collections.md")) |> contentView
        let collectionsPage =
            generatePage
                (pageMetadata "/collections/" "Collections of Luis Quintanilla's writing, media, and curated resources." "website")
                collectionsContent
                "default"
                "Collections - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"collections")
        writePageToDir saveDir "index.html" collectionsPage

    let buildStarterPackPage () = 
        let starterContent = convertFileToHtml (Path.Join(srcDir,"starter-packs.md")) |> contentView
        let starterPage =
            generatePage
                (pageMetadata "/collections/starter-packs/" "Curated starter packs from Luis Quintanilla." "website")
                starterContent
                "default"
                "Starter Packs - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"collections","starter-packs")
        writePageToDir saveDir "index.html" starterPage

    let buildTravelGuidesPage () = 
        let travelContent = convertFileToHtml (Path.Join(srcDir,"travel-guides.md")) |> contentView
        let travelPage =
            generatePage
                (pageMetadata "/collections/travel-guides/" "Travel guides and recommendations from Luis Quintanilla." "website")
                travelContent
                "default"
                "Travel Guides - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"collections","travel-guides")
        writePageToDir saveDir "index.html" travelPage

    let buildIRLStackPage () = 
        let irlStackContent = Path.Join(srcDir,"uses.md") |> convertFileToHtml |> contentView
        let irlStackPage =
            generatePage
                (pageMetadata "/uses/" "The tools, services, and practices Luis Quintanilla uses." "website")
                irlStackContent
                "default"
                "In Real Life Stack - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"uses")
        writePageToDir saveDir "index.html" irlStackPage

    let buildColophonPage () = 
        let colophonContent = Path.Join(srcDir,"colophon.md") |> convertFileToHtml |> contentView
        let colophonPage =
            generatePage
                (pageMetadata "/colophon/" "How Luis Quintanilla's website is built and maintained." "website")
                colophonContent
                "default"
                "Colophon - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"colophon")
        writePageToDir saveDir "index.html" colophonPage

    let buildToolsPage () = 
        let toolsContent = Path.Join(srcDir,"tools.md") |> convertFileToHtml |> contentView
        let toolsPage =
            generatePage
                (pageMetadata "/tools/" "Tools and utilities created or maintained by Luis Quintanilla." "website")
                toolsContent
                "default"
                "Tools - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"tools")
        writePageToDir saveDir "index.html" toolsPage

    let buildContactPage () = 
        let contactContent = convertFileToHtml (Path.Join(srcDir,"contact.md")) |> contentView
        let contactPage =
            generatePage
                (pageMetadata "/contact/" "Ways to contact Luis Quintanilla across the open web." "website")
                contactContent
                "default"
                "Contact - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"contact")
        writePageToDir saveDir "index.html" contactPage

    let buildSearchPage () = 
        let searchContent = convertFileToHtml (Path.Join(srcDir,"search.md")) |> contentView
        let searchPage =
            generatePage
                (pageMetadata "/search/" "Search Luis Quintanilla's website." "website")
                searchContent
                "default"
                "Search - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"search")
        writePageToDir saveDir "index.html" searchPage

    let buildOnlineRadioPage () = 
        let onlineRadioContent = convertFileToHtml (Path.Join(srcDir,"radio.md")) |> contentView
        let onlineRadioPage =
            generatePage
                (pageMetadata "/radio/" "Listen to Luis Quintanilla's online radio stream." "website")
                onlineRadioContent
                "default"
                "Online Radio - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"radio")
        Directory.CreateDirectory(saveDir) |> ignore

        // Copy playlist file
        File.Copy(Path.Join(srcDir,"OnlineRadioPlaylist.m3u"),Path.Join(saveDir,"OnlineRadioPlaylist.m3u"),true)
        
        // Write out page
        writePageToDir saveDir "index.html" onlineRadioPage        

    let buildEventPage () = 
        let events =  
            File.ReadAllText(Path.Join("Data","events.json"))
            |> JsonSerializer.Deserialize<Event array>
            |> Array.sortByDescending(fun x -> DateTimeOffset.Parse(x.Date))

        let eventPage =
            generatePage
                (pageMetadata "/events/" "Events and appearances from Luis Quintanilla." "website")
                (eventView events)
                "default"
                "Events - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"events")
        Directory.CreateDirectory(saveDir) |> ignore
        File.WriteAllText(Path.Join(saveDir,"index.html"),eventPage)
