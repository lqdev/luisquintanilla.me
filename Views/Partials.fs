module PartialViews

    open Giraffe.ViewEngine

    let homeView = 
        div [_class "mr-auto"] [
            h1 [ ] [ Text "Hello" ]
            p [ ] [ Text "This is my app"]    
        ]

    let aboutView content =  
        div [_class "mr-auto"] [
            rawText content
        ]

    let postView content =     
        div [_class "mr-auto"] [
            rawText content
        ]
