module FeedViews

open Giraffe.ViewEngine
open Domain

let rollLinkView (links:Outline array) = 
    ul [] [
        for link in links do
            li [] [
                strong [] [
                    str $"{link.Title} - "
                ]
                a [ _href link.HtmlUrl ] [ Text "Website"]
                str " / "
                a [ _href link.XmlUrl ] [ Text "RSS Feed"]                    
            ]
    ]

let blogRollView (links:Outline array) = 
    let linkContent = rollLinkView links 

    div [ _class "mr-auto" ] [
        h2 [] [ Text "Blogroll" ]
        p [] [ Text "What is a blogroll you ask? At a high level, it's a list of links to blogs I find interesting."]
        p [] [
            str "Check out the article "
            a [_href "https://blogroll.org/what-are-blogrolls/"] [Text "What are blogrolls?"]
            str " for more details."
        ]
        p [] [
            str "You can subscribe to any of the individual feeds in your preferred RSS reader using the RSS feed links below. Want to subscribe to all of them? Use the "
            a [ _href "/feed/blogroll/index.opml"] [Text "OPML file"]
            str " if your RSS reader client supports "
            a [_href "http://opml.org/"] [Text "OPML."]
        ]        
        linkContent
    ]

let podRollView (links:Outline array) = 
    let linkContent = rollLinkView links

    div [ _class "mr-auto" ] [
        h2 [] [ Text "Podroll" ]
        p [] [ 
            str "I took the podroll concept from blogrolls. In short, this list of podcasts I find interesting. If you're interested in the blogroll, you can find it "
            a [_href "/feed/blogroll"] [Text "here"]            
            str "."
        ]
        p [] [
            str "You can subscribe to any of the individual feeds in your preferred RSS reader or podcast client using the RSS feed links below. Want to subscribe to all of them? Use the "
            a [ _href "/feed/podroll/index.opml"] [Text "OPML file"]
            str " if your RSS reader or podcast client supports "
            a [_href "http://opml.org/"] [Text "OPML."]
        ]
        linkContent
    ]

let forumsView (links:Outline array) = 
    let linkContent = rollLinkView links

    div [ _class "mr-auto" ] [
        h2 [] [ Text "Forums" ]
        p [] [ 
            str "This is a list of forums I find interesting. If you're interested, you can also check out my "
            a [_href "/feed/blogroll"] [Text "blogroll"]
            str " and "
            a [_href "/feed/podroll"] [Text "podroll"]            
            str "."
        ]
        p [] [
            str "You can subscribe to any of the individual forums in your preferred RSS reader using the RSS feed links below. Want to subscribe to all of them? Use the "
            a [ _href "/feed/forums/index.opml"] [Text "OPML file"]
            str " if your RSS reader or podcast client supports "
            a [_href "http://opml.org/"] [Text "OPML."]
        ]
        linkContent
    ]

let youTubeFeedView (links:Outline array) = 
    let linkContent = rollLinkView links

    div [ _class "mr-auto" ] [
        h2 [] [ Text "YouTube" ]
        p [] [ 
            str "This is a list of YouTube channels I find interesting. If you're interested, you can also check out my "
            a [_href "/feed/blogroll"] [Text "blogroll"]
            str ", "
            a [_href "/feed/podroll"] [Text "podroll"]            
            str ", and "
            a [_href "/feed/forums"] [Text "forums"]
            str "."
        ]
        p [] [
            str "You can subscribe to any of the individual channels in your preferred RSS reader using the RSS feed links below. Want to subscribe to all of them? Use the "
            a [ _href "/feed/youtube/index.opml"] [Text "OPML file"]
            str " if your RSS reader or podcast client supports "
            a [_href "http://opml.org/"] [Text "OPML."]
        ]
        linkContent
    ]

let aiStarterPackFeedView (links:Outline array) = 
    let linkContent = rollLinkView links

    div [ _class "mr-auto" ] [
        h2 [] [ Text "AI Starter Pack" ]
        p [] [ 
            str "This is a list of AI resources I use to stay on top of AI news."
        ]
        p [] [
            str "You can subscribe to any of the individual feeds in your preferred RSS reader using the RSS feed links below. Want to subscribe to all of them? Use the "
            a [ _href "/feed/starter/ai/index.opml"] [Text "OPML file"]
            str " if your RSS reader or podcast client supports "
            a [_href "http://opml.org/"] [Text "OPML."]
        ]
        linkContent
    ]
