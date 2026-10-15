module ViewGenerator

    open Giraffe.ViewEngine
    open Layouts

    let pageMetadata (canonicalPath: string) (description: string) (ogType: string) : PageMetadata =
        { CanonicalPath = canonicalPath
          Description = description
          OgType = ogType }

    let generateWithPageMetadata
        (extraHead: XmlNode list)
        (metadata: PageMetadata)
        (partialView: XmlNode)
        (layout: string)
        (title: string) =

        let viewLayout =
            match layout with
            | "default" -> defaultLayoutWithHeadAt extraHead metadata
            | "defaultindex" -> defaultIndexedLayoutWithHeadAt extraHead metadata
            | "presentation" -> presentationLayout
            | _ -> defaultLayoutWithHeadAt extraHead metadata

        partialView
        |> RenderView.AsString.xmlNode
        |> viewLayout title
        |> RenderView.AsString.htmlDocument

    let generateWithHead (extraHead:XmlNode list) (partialView:XmlNode) (layout:string) (title:string) =
        generateWithPageMetadata extraHead (defaultPageMetadata title) partialView layout title

    let generate (partialView:XmlNode) (layout:string) (title:string) =
        generateWithHead [] partialView layout title

    let generatePage (metadata: PageMetadata) (partialView: XmlNode) (layout: string) (title: string) =
        generateWithPageMetadata [] metadata partialView layout title

    let generatePartial (partialView:XmlNode) =
        partialView
        |> RenderView.AsString.xmlNode
