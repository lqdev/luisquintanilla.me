module LivestreamBuilder

    open System.IO
    open Domain
    open MarkdownService
    open ViewGenerator
    open PartialViews
    open BuilderCommon

    let filterFeedByPostType (posts: Post array) (postType: string) = 
        posts |> Array.filter(fun post -> post.Metadata.PostType = postType)

    let buildLiveStreamPage () = 
        let title = "Live Stream - Luis Quintanilla"
        let page = generate (liveStreamView title) "default" title
        let saveDir = Path.Join(outputDir,"live")
        Directory.CreateDirectory(saveDir) |> ignore

        File.WriteAllText(Path.Join(saveDir,"index.html"), page)

    let buildLiveStreamsPage (streams: Livestream array) = 
        let liveStreamsPage = generate (liveStreamsView streams) "defaultindex" "Live Stream Recordings - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"streams")
        Directory.CreateDirectory(saveDir) |> ignore
        File.WriteAllText(Path.Join(saveDir,"index.html"),liveStreamsPage)

    let buildLiveStreamPages (streams:Livestream array) = 
        streams
        |> Array.iter(fun stream ->
            let rootSaveDir = Path.Join(outputDir,"streams")
            let html = liveStreamPageView {stream with Content = stream.Content |> convertMdToHtml}
            let streamView = generate  html "defaultindex" $"Live Stream Recording | {stream.Metadata.Title} | Luis Quintanilla"
            let saveDir = Path.Join(rootSaveDir,$"{stream.FileName}")
            Directory.CreateDirectory(saveDir) |> ignore
            File.WriteAllText(Path.Join(saveDir,"index.html"),streamView))
