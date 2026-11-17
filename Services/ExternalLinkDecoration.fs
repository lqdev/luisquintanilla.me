module ExternalLinkDecoration

open System
open Markdig
open Markdig.Parsers
open Markdig.Renderers.Html
open Markdig.Syntax
open Markdig.Syntax.Inlines

let private hasVisibleLinkText (link: LinkInline) =
    let isInsideImage (inlineNode: Inline) =
        let rec loop (parent: ContainerInline) =
            match parent with
            | null -> false
            | :? LinkInline as parentLink when parentLink.IsImage -> true
            | _ -> loop parent.Parent
        loop inlineNode.Parent

    MarkdownObjectExtensions.Descendants<LiteralInline>(link :> MarkdownObject)
    |> Seq.exists (fun literal ->
        not (isInsideImage (literal :> Inline))
        && not (String.IsNullOrWhiteSpace(literal.Content.ToString())))

type private ExternalLinkExtension() =
    interface IMarkdownExtension with
        member _.Setup(pipeline: MarkdownPipelineBuilder) =
            pipeline.add_DocumentProcessed(
                ProcessDocumentDelegate(fun (document: MarkdownDocument) ->
                    MarkdownObjectExtensions.Descendants<LinkInline>(
                        document :> MarkdownObject
                    )
                    |> Seq.iter (fun link ->
                        if not link.IsImage
                           && hasVisibleLinkText link
                           && (LinkClassification.classify
                                   (Constants.Urls.canonical + "/")
                                   link.Url).Kind = LinkClassification.LinkKind.External then
                            HtmlAttributesExtensions.GetAttributes(link).AddClass("link-external"))
                )
            )

        member _.Setup(_pipeline: MarkdownPipeline, _renderer: Markdig.Renderers.IMarkdownRenderer) = ()

let addToPipeline (builder: MarkdownPipelineBuilder) =
    builder.Extensions.Add(ExternalLinkExtension() :> IMarkdownExtension)
