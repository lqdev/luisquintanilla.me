module ReviewDataExtractor

    open Domain
    open ASTParsing
    open CustomBlocks
    open BlockRenderers
    open TagService
    open MarkdownService
    open ReadingTimeService
    open System.Xml.Linq
    open System
    open System.IO
    open System.Text.Json
    open System.Text.Json.Nodes
    open Giraffe.ViewEngine
    open Giraffe.ViewEngine.HtmlElements
    open Markdig
    open Markdig.Syntax
    open GenericBuilder

    /// Extract review data from raw markdown content including item type for badge display
    let extractReviewData (rawMarkdown: string) : (string option * float * float * string option) =
        if not (String.IsNullOrWhiteSpace(rawMarkdown)) && rawMarkdown.Contains(":::review") then
            try
                let pipeline = 
                    MarkdownPipelineBuilder()
                        |> useCustomBlocks
                        |> fun builder -> builder.Build()
                let document = Markdown.Parse(rawMarkdown, pipeline)
                let customBlocks = extractCustomBlocks document
                
                match customBlocks.TryGetValue("review") with
                | true, reviewList when reviewList.Length > 0 ->
                    match reviewList.[0] with
                    | :? CustomBlocks.ReviewData as reviewData -> 
                        (reviewData.ImageUrl, reviewData.Rating, reviewData.Scale, Some reviewData.ItemType)
                    | _ -> (None, 0.0, 5.0, None)
                | _ -> (None, 0.0, 5.0, None)
            with
            | _ -> (None, 0.0, 5.0, None)
        else
            (None, 0.0, 5.0, None)
