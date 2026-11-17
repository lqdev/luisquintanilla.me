module OpmlService

    open System
    open System.ComponentModel
    open System.Diagnostics
    open System.Globalization
    open System.IO
    open System.Linq
    open System.Text.Json
    open System.Xml.Linq
    open Domain

    let private opmlDocument (head: XElement) (outlines: XElement array) =
        XElement(XName.Get "opml",
            XAttribute(XName.Get "version", "2.0"),
                head,
                XElement(XName.Get "body", outlines |> Array.map (fun outline -> outline :> obj)))

    let private headElement (metadata: OpmlMetadata) =
        let head = XElement(XName.Get "head")
        head.Add(XElement(XName.Get "title", metadata.Title))
        head.Add(XElement(XName.Get "ownerId", metadata.OwnerId))

        match metadata.DocsUrl with
        | Some docsUrl when not (String.IsNullOrWhiteSpace(docsUrl)) ->
            head.Add(XElement(XName.Get "docs", docsUrl))
        | _ ->
            ()

        match metadata.DateModified with
        | Some dateModified ->
            head.Add(XElement(XName.Get "dateModified", dateModified.ToString("r", CultureInfo.InvariantCulture)))
        | None ->
            ()

        head

    let private tryGitOutput (arguments: string list) =
        try
            let startInfo = ProcessStartInfo()
            startInfo.FileName <- "git"
            startInfo.UseShellExecute <- false
            startInfo.RedirectStandardOutput <- true

            for argument in arguments do
                startInfo.ArgumentList.Add(argument)

            use gitProcess = Process.Start(startInfo)

            if isNull gitProcess then
                None
            else
                let output = gitProcess.StandardOutput.ReadToEnd().Trim()
                gitProcess.WaitForExit()
                if gitProcess.ExitCode = 0 then Some output else None
        with
        | :? Win32Exception
        | :? InvalidOperationException
        | :? IOException ->
            None

    /// Prefer the last committed source change; use the file timestamp for uncommitted or unavailable history.
    let sourceModified (path: string) =
        let isClean =
            match tryGitOutput [ "diff"; "--quiet"; "HEAD"; "--"; path ] with
            | Some output -> String.IsNullOrEmpty(output)
            | None -> false

        let committedTimestamp =
            if isClean then
                tryGitOutput [ "log"; "-1"; "--format=%cI"; "--"; path ]
                |> Option.bind (fun value ->
                    let mutable timestamp = Unchecked.defaultof<DateTimeOffset>
                    if DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, &timestamp) then
                        Some timestamp
                    else
                        None)
            else
                None

        committedTimestamp
        |> Option.defaultWith (fun () -> DateTimeOffset(File.GetLastWriteTimeUtc(path)))

    let private feedOutlineElement (data: Outline) =
        XElement(XName.Get "outline",
            XAttribute(XName.Get "title", data.Title),
            XAttribute(XName.Get "text", data.Title),        
            XAttribute(XName.Get "type", data.Type),
            XAttribute(XName.Get "htmlUrl", data.HtmlUrl),
            XAttribute(XName.Get "xmlUrl", data.XmlUrl))

    let private includeOutlineElement (data: OpmlInclude) =
        if String.IsNullOrWhiteSpace(data.Title) then
            invalidArg (nameof data.Title) "OPML include title cannot be blank."

        if String.IsNullOrWhiteSpace(data.Url) then
            invalidArg (nameof data.Url) "OPML include URL cannot be blank."

        XElement(XName.Get "outline",
            XAttribute(XName.Get "title", data.Title),
            XAttribute(XName.Get "text", data.Title),
            XAttribute(XName.Get "type", "include"),
            XAttribute(XName.Get "url", data.Url))

    let private outlineElement (outline: OpmlOutline) =
        match outline with
        | FeedOutline data -> feedOutlineElement data
        | IncludeOutline data -> includeOutlineElement data

    let buildOpmlDocument (metadata: OpmlMetadata) (outlines: OpmlOutline array) =
        let head = metadata |> headElement
        let outlineElements = outlines |> Array.map outlineElement
        opmlDocument head outlineElements

    let buildOpmlFeedWithDateModified
        (title: string)
        (ownerId: string)
        (dateModified: DateTimeOffset option)
        (links: Outline array)
        =
        let metadata =
            {
                Title = title
                OwnerId = ownerId
                DocsUrl = None
                DateModified = dateModified
            }

        links
        |> Array.map FeedOutline
        |> buildOpmlDocument metadata

    let buildOpmlFeed (title: string) (ownerId: string) (links: Outline array) =
        buildOpmlFeedWithDateModified title ownerId None links
