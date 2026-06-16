module StaticPagesBuilder

    open System
    open System.IO
    open System.Text.Json
    open Domain
    open MarkdownService
    open ViewGenerator
    open PartialViews
    open BuilderCommon

    let buildAboutPage () = 
        let aboutContent = convertFileToHtml (Path.Join(srcDir,"about.md")) |> contentView
        let aboutPage = generate aboutContent "default" "About - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"about")
        writePageToDir saveDir "index.html" aboutPage

    let buildCollectionsPage () = 
        let collectionsContent = convertFileToHtml (Path.Join(srcDir,"collections.md")) |> contentView
        let collectionsPage = generate collectionsContent "default" "Collections - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"collections")
        writePageToDir saveDir "index.html" collectionsPage

    let buildStarterPackPage () = 
        let starterContent = convertFileToHtml (Path.Join(srcDir,"starter-packs.md")) |> contentView
        let starterPage = generate starterContent "default" "Starter Packs - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"collections","starter-packs")
        writePageToDir saveDir "index.html" starterPage

    let buildTravelGuidesPage () = 
        let travelContent = convertFileToHtml (Path.Join(srcDir,"travel-guides.md")) |> contentView
        let travelPage = generate travelContent "default" "Travel Guides - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"collections","travel-guides")
        writePageToDir saveDir "index.html" travelPage

    let buildIRLStackPage () = 
        let irlStackContent = Path.Join(srcDir,"uses.md") |> convertFileToHtml |> contentView
        let irlStackPage = generate irlStackContent "default" "In Real Life Stack - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"uses")
        writePageToDir saveDir "index.html" irlStackPage

    let buildColophonPage () = 
        let colophonContent = Path.Join(srcDir,"colophon.md") |> convertFileToHtml |> contentView
        let colophonPage = generate colophonContent "default" "Colophon - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"colophon")
        writePageToDir saveDir "index.html" colophonPage

    let buildToolsPage () = 
        let toolsContent = Path.Join(srcDir,"tools.md") |> convertFileToHtml |> contentView
        let toolsPage = generate toolsContent "default" "Tools - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"tools")
        writePageToDir saveDir "index.html" toolsPage

    let buildContactPage () = 
        let contactContent = convertFileToHtml (Path.Join(srcDir,"contact.md")) |> contentView
        let contactPage = generate contactContent "default" "Contact - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"contact")
        writePageToDir saveDir "index.html" contactPage

    let buildSearchPage () = 
        let searchContent = convertFileToHtml (Path.Join(srcDir,"search.md")) |> contentView
        let searchPage = generate searchContent "default" "Search - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"search")
        writePageToDir saveDir "index.html" searchPage

    let buildOnlineRadioPage () = 
        let onlineRadioContent = convertFileToHtml (Path.Join(srcDir,"radio.md")) |> contentView
        let onlineRadioPage = generate onlineRadioContent "default" "Online Radio - Luis Quintanilla"
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

        let eventPage = generate (eventView events) "default" "Events - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"events")
        Directory.CreateDirectory(saveDir) |> ignore
        File.WriteAllText(Path.Join(saveDir,"index.html"),eventPage)
