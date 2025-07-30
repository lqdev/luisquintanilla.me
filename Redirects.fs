module PersonalSite.Redirects

open System
open System.IO
open System.Collections.Generic

type ContentType = 
    | Posts
    | Notes 
    | Responses
    | Reviews
    | Resources of string // wiki, snippets, presentations

type RedirectEntry = {
    Source: string
    Target: string
    StatusCode: int
    ContentType: ContentType option
}

/// Legacy /feed/ URL mappings to new architecture
let legacyFeedRedirects: Map<string, RedirectEntry> =
    let createRedirect source target contentType =
        {
            Source = source
            Target = target
            StatusCode = 301
            ContentType = Some contentType
        }
    
    Map [
        // Weekly summaries (all in /notes/)
        "/feed/2024-03-10-weekly-post-summary", createRedirect "/feed/2024-03-10-weekly-post-summary" "/notes/2024-03-10-weekly-post-summary/" Notes
        "/feed/2024-03-18-weekly-post-summary", createRedirect "/feed/2024-03-18-weekly-post-summary" "/notes/2024-03-18-weekly-post-summary/" Notes
        "/feed/2024-03-24-weekly-post-summary", createRedirect "/feed/2024-03-24-weekly-post-summary" "/notes/2024-03-24-weekly-post-summary/" Notes
        "/feed/2024-04-01-weekly-post-summary", createRedirect "/feed/2024-04-01-weekly-post-summary" "/notes/2024-04-01-weekly-post-summary/" Notes
        "/feed/2024-04-07-weekly-post-summary", createRedirect "/feed/2024-04-07-weekly-post-summary" "/notes/2024-04-07-weekly-post-summary/" Notes
        "/feed/2024-04-14-weekly-post-summary", createRedirect "/feed/2024-04-14-weekly-post-summary" "/notes/2024-04-14-weekly-post-summary/" Notes
        "/feed/2024-04-29-weekly-post-summary", createRedirect "/feed/2024-04-29-weekly-post-summary" "/notes/2024-04-29-weekly-post-summary/" Notes
        "/feed/2024-05-06-weekly-post-summary", createRedirect "/feed/2024-05-06-weekly-post-summary" "/notes/2024-05-06-weekly-post-summary/" Notes
        "/feed/2024-05-26-weekly-post-summary", createRedirect "/feed/2024-05-26-weekly-post-summary" "/notes/2024-05-26-weekly-post-summary/" Notes
        "/feed/2024-06-03-weekly-post-summary", createRedirect "/feed/2024-06-03-weekly-post-summary" "/notes/2024-06-03-weekly-post-summary/" Notes
        "/feed/2024-07-21-weekly-post-summary", createRedirect "/feed/2024-07-21-weekly-post-summary" "/notes/2024-07-21-weekly-post-summary/" Notes
        "/feed/2024-07-28-weekly-post-summary", createRedirect "/feed/2024-07-28-weekly-post-summary" "/notes/2024-07-28-weekly-post-summary/" Notes
        "/feed/2024-08-11-weekly-post-summary", createRedirect "/feed/2024-08-11-weekly-post-summary" "/notes/2024-08-11-weekly-post-summary/" Notes
        "/feed/2024-09-15-weekly-post-summary", createRedirect "/feed/2024-09-15-weekly-post-summary" "/notes/2024-09-15-weekly-post-summary/" Notes
        "/feed/2024-10-20-weekly-post-summary", createRedirect "/feed/2024-10-20-weekly-post-summary" "/notes/2024-10-20-weekly-post-summary/" Notes
        "/feed/2024-11-03-weekly-post-summary", createRedirect "/feed/2024-11-03-weekly-post-summary" "/notes/2024-11-03-weekly-post-summary/" Notes
        "/feed/2024-11-10-weekly-post-summary", createRedirect "/feed/2024-11-10-weekly-post-summary" "/notes/2024-11-10-weekly-post-summary/" Notes
        "/feed/2024-11-18-weekly-post-summary", createRedirect "/feed/2024-11-18-weekly-post-summary" "/notes/2024-11-18-weekly-post-summary/" Notes
        
        // Regular notes (miscellaneous items in /notes/)
        "/feed/2022-04-21-assorted-links", createRedirect "/feed/2022-04-21-assorted-links" "/notes/2022-04-21-assorted-links/" Notes
        "/feed/weblogging-rewind-2023", createRedirect "/feed/weblogging-rewind-2023" "/notes/weblogging-rewind-2023/" Notes
        "/feed/weblogging-rewind-2023-continued", createRedirect "/feed/weblogging-rewind-2023-continued" "/notes/weblogging-rewind-2023-continued/" Notes
        "/feed/webmentions-partially-implemented", createRedirect "/feed/webmentions-partially-implemented" "/notes/webmentions-partially-implemented/" Notes
        "/feed/website-feeds-opml", createRedirect "/feed/website-feeds-opml" "/notes/website-feeds-opml/" Notes
        "/feed/website-upgraded-dotnet-9", createRedirect "/feed/website-upgraded-dotnet-9" "/notes/website-upgraded-dotnet-9/" Notes
        "/feed/welcome-to-fediverse-tips", createRedirect "/feed/welcome-to-fediverse-tips" "/notes/welcome-to-fediverse-tips/" Notes
        "/feed/well-known-feeds", createRedirect "/feed/well-known-feeds" "/notes/well-known-feeds/" Notes
        "/feed/what-is-kick", createRedirect "/feed/what-is-kick" "/notes/what-is-kick/" Notes
        "/feed/marvin-gaye-whats-going-on", createRedirect "/feed/marvin-gaye-whats-going-on" "/notes/marvin-gaye-whats-going-on/" Notes
        
        // Response items (in /responses/)
        "/feed/1-percent-rule", createRedirect "/feed/1-percent-rule" "/responses/1-percent-rule/" Responses
        "/feed/1mb-club", createRedirect "/feed/1mb-club" "/responses/1mb-club/" Responses
        "/feed/2022-state-of-ai-report", createRedirect "/feed/2022-state-of-ai-report" "/responses/2022-state-of-ai-report/" Responses
        "/feed/2023-goals-accomplishments-purple-life", createRedirect "/feed/2023-goals-accomplishments-purple-life" "/responses/2023-goals-accomplishments-purple-life/" Responses
        "/feed/2023-social-media-case-fediverse", createRedirect "/feed/2023-social-media-case-fediverse" "/responses/2023-social-media-case-fediverse/" Responses
        "/feed/2023-state-of-ai-report", createRedirect "/feed/2023-state-of-ai-report" "/responses/2023-state-of-ai-report/" Responses
    ]

/// Social media and contact shortcuts
let socialRedirects: Map<string, RedirectEntry> =
    let createSocialRedirect source target =
        {
            Source = source
            Target = target
            StatusCode = 301
            ContentType = None
        }
    
    Map [
        "/github", createSocialRedirect "/github" "https://github.com/lqdev"
        "/mastodon", createSocialRedirect "/mastodon" "https://toot.lqdev.tech/@lqdev"
        "/linkedin", createSocialRedirect "/linkedin" "https://www.linkedin.com/in/lquintanilla01/"
        "/twitter", createSocialRedirect "/twitter" "https://twitter.com/ljquintanilla"
        "/youtube", createSocialRedirect "/youtube" "https://www.youtube.com/@lqdev"
        "/gravatar", createSocialRedirect "/gravatar" "/contact/"
        "/blogroll", createSocialRedirect "/blogroll" "/collections/blogroll/"
        "/podroll", createSocialRedirect "/podroll" "/collections/podroll/"
    ]

/// Legacy post references with .html extensions
let legacyPostRedirects: Map<string, RedirectEntry> =
    let createPostRedirect source target =
        {
            Source = source
            Target = target
            StatusCode = 301
            ContentType = Some Posts
        }
    
    Map [
        "/posts/how-to-watch-twitch-using-vlc.html", createPostRedirect "/posts/how-to-watch-twitch-using-vlc.html" "/posts/how-to-listen-internet-radio-using-vlc/"
        "/posts/inspect-mlnet-models-netron.html", createPostRedirect "/posts/inspect-mlnet-models-netron.html" "/posts/inspect-mlnet-models-netron/"
        "/posts/rediscovering-rss-user-freedom.html", createPostRedirect "/posts/rediscovering-rss-user-freedom.html" "/posts/rediscovering-rss-user-freedom/"
        "/inspect-mlnet-models-netron.html", createPostRedirect "/inspect-mlnet-models-netron.html" "/posts/inspect-mlnet-models-netron/"
        "/vs-automate-mlnet-schema-generation.html", createPostRedirect "/vs-automate-mlnet-schema-generation.html" "/posts/vs-automate-mlnet-schema-generation/"
        "/presentations/mlnet-globalai-2022.html", createPostRedirect "/presentations/mlnet-globalai-2022.html" "/resources/presentations/mlnet-globalai-2022/"
    ]

/// Collection shortcuts and legacy paths
let collectionRedirects: Map<string, RedirectEntry> =
    let createCollectionRedirect source target =
        {
            Source = source
            Target = target
            StatusCode = 301
            ContentType = None
        }
    
    Map [
        "/posts/1", createCollectionRedirect "/posts/1" "/posts/"
        "/posts/1/", createCollectionRedirect "/posts/1/" "/posts/"
    ]

/// Combined redirect map for all legacy URLs
let allRedirects: Map<string, RedirectEntry> =
    [ legacyFeedRedirects; socialRedirects; legacyPostRedirects; collectionRedirects ]
    |> List.fold (fun acc map -> 
        Map.fold (fun acc key value -> Map.add key value acc) acc map) Map.empty

/// Try to find a redirect for the given path
let tryGetRedirect (path: string) : RedirectEntry option =
    allRedirects |> Map.tryFind path

/// Get all redirect entries for debugging/validation
let getAllRedirects () : RedirectEntry list =
    allRedirects |> Map.toList |> List.map snd

/// Generate a .htaccess file for Apache servers (if needed for static hosting)
let generateHtaccessRedirects () : string =
    let redirectLines = 
        allRedirects
        |> Map.toList
        |> List.map (fun (source, redirect) ->
            sprintf "Redirect %d %s %s" redirect.StatusCode source redirect.Target)
    
    String.Join("\n", redirectLines)

/// Generate nginx redirect configuration (if needed for nginx hosting)
let generateNginxRedirects () : string =
    let redirectLines = 
        allRedirects
        |> Map.toList
        |> List.map (fun (source, redirect) ->
            let statusCode = if redirect.StatusCode = 301 then "permanent" else "temporary"
            sprintf "rewrite ^%s$ %s %s;" source redirect.Target statusCode)
    
    String.Join("\n", redirectLines)

/// Generate HTML redirect page content
let generateHtmlRedirectPage (targetUrl: string) (delaySeconds: int) : string =
    sprintf """<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <meta http-equiv="refresh" content="%d;url=%s">
    <link rel="canonical" href="%s">
    <title>Redirecting...</title>
    <style>
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; text-align: center; padding: 2rem; }
        .redirect-message { margin: 2rem 0; }
        .redirect-link { color: #0066cc; text-decoration: none; }
        .redirect-link:hover { text-decoration: underline; }
    </style>
</head>
<body>
    <div class="redirect-message">
        <h1>Page Moved</h1>
        <p>This page has moved to <a href="%s" class="redirect-link">%s</a></p>
        <p>You will be redirected automatically in %d seconds.</p>
        <script>
            setTimeout(function() {
                window.location.href = '%s';
            }, %d);
        </script>
    </div>
</body>
</html>""" delaySeconds targetUrl targetUrl targetUrl targetUrl delaySeconds targetUrl (delaySeconds * 1000)

/// Create redirect pages in the output directory
let createRedirectPages (outputDir: string) : unit =
    let redirectsCreated = ref 0
    
    allRedirects
    |> Map.iter (fun source redirect ->
        try
            // Create directory structure for the source path
            let sourcePath = source.TrimStart('/')
            let fullOutputPath = Path.Join(outputDir, sourcePath)
            
            // Determine if this should be a file or directory-based redirect
            let (targetPath, isDirectory) = 
                if source.EndsWith("/") then
                    (Path.Join(fullOutputPath, "index.html"), true)
                else 
                    (fullOutputPath + ".html", false)
            
            // Create directory if needed
            let targetDir = Path.GetDirectoryName(targetPath)
            if not (Directory.Exists(targetDir)) then
                Directory.CreateDirectory(targetDir) |> ignore
            
            // Generate and write HTML redirect page
            let htmlContent = generateHtmlRedirectPage redirect.Target 0 // 0 second delay for immediate redirect
            File.WriteAllText(targetPath, htmlContent)
            
            incr redirectsCreated
            printfn "Created redirect: %s -> %s" source redirect.Target
            
        with
        | ex -> 
            printfn "Error creating redirect for %s: %s" source ex.Message
    )
    
    printfn "Generated %d redirect pages" !redirectsCreated
