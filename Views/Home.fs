module HomePage

    open Giraffe.ViewEngine
    open SharedViews

    let generate =
        let view = 
            div [] [
                h1 [ ] [ Text "Hello" ]
                p [ ] [ Text "This is my app"]    
            ]

        view |> RenderView.AsString.xmlNode |> defaultLayout "Luis Quintanilla" |> RenderView.AsString.htmlDocument