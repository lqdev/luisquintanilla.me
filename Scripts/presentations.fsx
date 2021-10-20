#r @"C:\Users\lqdev\Development\PersonalSite\bin\Debug\net5.0\PersonalSite.dll"
#r @"C:\Users\lqdev\Development\PersonalSite\bin\Debug\net5.0\Giraffe.ViewEngine.dll"

open Giraffe.ViewEngine
open System.IO

let samplePresentation = 
    html [] [
        head [] [
            meta [_name "viewport" ; _content "width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no"]
            link [_rel "stylesheet"; _href "scripts/revealjs/dist/reveal.css"]
            link [_rel "stylesheet"; _href "scripts/revealjs/dist/theme/black.css"]
        ]
        body [] [
            div [_class "reveal"; _style "height:50vh;width:50vw"] [
                div [_class "slides"] [
                    section [ flag "data-markdown" ] [
                        textarea [ flag "data-template"] [
                            rawText """
                            ## Slide 1
                            A paragraph with some text and a [link](http://hakim.se).
                            ---
                            ## Slide 2
                            ---
                            ## Slide 3
                            """
                        ]
                    ]
                ]
            ]
            ul [] [
                li [] [
                    a [_href "https://luisquintanilla.me"] [Text "First Link"]
                ]
                li [] [
                    a [_href "https://luisquintanilla.me"] [Text "Second Link"]
                ]
                li [] [
                    a [_href "https://luisquintanilla.me"] [Text "Third Link"]
                ]                                
            ]

            
        ]
    ]

let presentation = samplePresentation |> RenderView.AsString.htmlDocument 

File.WriteAllText(Path.Join(__SOURCE_DIRECTORY__,"samplePresentation.html"), presentation)