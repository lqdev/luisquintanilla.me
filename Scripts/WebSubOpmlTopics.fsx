module WebSubOpmlTopics

open System

type Topic =
    { Route: string
      Url: string }

let topics =
    [| "/.well-known/following.opml"
       "/collections/blogroll/index.opml"
       "/collections/podroll/index.opml"
       "/collections/youtube/index.opml"
       "/collections/forums/index.opml"
       "/collections/starter-packs/ai/index.opml" |]
    |> Array.map (fun route ->
        { Route = route
          Url = "https://lqdev.me" + route })

let loadHubUrl () =
    let value = Environment.GetEnvironmentVariable("WEBSUB_HUB_URL")

    if String.IsNullOrWhiteSpace(value) then
        invalidOp "Set WEBSUB_HUB_URL to the deployed WebSub hub URL."

    match Uri.TryCreate(value, UriKind.Absolute) with
    | true, uri
        when (uri.Scheme = Uri.UriSchemeHttps
              || (uri.Scheme = Uri.UriSchemeHttp && uri.IsLoopback))
             && String.IsNullOrEmpty(uri.UserInfo)
             && uri.AbsolutePath = "/"
             && String.IsNullOrEmpty(uri.Query)
             && String.IsNullOrEmpty(uri.Fragment) ->
        uri
    | _ ->
        invalidArg "WEBSUB_HUB_URL" "The hub URL must be an HTTPS root URL (or a loopback HTTP URL for local testing)."
