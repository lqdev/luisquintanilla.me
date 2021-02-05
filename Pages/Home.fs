module HomePage

    open Giraffe.ViewEngine

    let getView =
        let view = 
            html [] [
                body [] [
                    h1 [ ] [ Text "Hello" ]
                    p [ ] [ Text "This is my app"]
                ]
            ]
        
        view |> RenderView.AsString.htmlDocument
