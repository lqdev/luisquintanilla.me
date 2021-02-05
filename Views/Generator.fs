module ViewGenerator

    open Giraffe.ViewEngine
    open Layouts

    let generate (partialView:XmlNode) (layout:string) (title:string) =
        
        let viewLayout = 
            match layout with 
            | "default" -> defaultLayout
            | _ -> defaultLayout
        
        partialView 
        |> RenderView.AsString.xmlNode 
        |> viewLayout title 
        |> RenderView.AsString.htmlDocument 
