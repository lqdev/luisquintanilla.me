module ViewGenerator

    open Giraffe.ViewEngine
    open Layouts

    let generateWithHead (extraHead:XmlNode list) (partialView:XmlNode) (layout:string) (title:string) =
        
        let viewLayout = 
            match layout with 
            | "default" -> defaultLayoutWithHead extraHead
            | "defaultindex" -> defaultIndexedLayoutWithHead extraHead
            | "presentation" -> presentationLayout
            | _ -> defaultLayoutWithHead extraHead
        
        partialView 
        |> RenderView.AsString.xmlNode 
        |> viewLayout title 
        |> RenderView.AsString.htmlDocument 

    let generate (partialView:XmlNode) (layout:string) (title:string) =
        generateWithHead [] partialView layout title
    
    let generatePartial (partialView:XmlNode) =
        partialView
        |> RenderView.AsString.xmlNode
