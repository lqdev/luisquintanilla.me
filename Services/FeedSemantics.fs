module FeedSemantics

open System
open System.Xml.Linq

[<RequireQualifiedAccess>]
type EntryKind =
    | BlogPost
    | Note
    | Reply of target: string
    | Like of target: string
    | Bookmark of target: string

let responseKind (responseType: string) : (string -> EntryKind) option =
    if String.Equals(responseType, "reply", StringComparison.OrdinalIgnoreCase) then Some EntryKind.Reply
    elif String.Equals(responseType, "star", StringComparison.OrdinalIgnoreCase) then Some EntryKind.Like
    elif String.Equals(responseType, "bookmark", StringComparison.OrdinalIgnoreCase) then Some EntryKind.Bookmark
    else None

// Preserve the deployed reply validator's exact trimmed string, not Uri's
// reserialized form (which can change escaping and relationship identity).
let targetReference (targetUrl: string) : string option =
    if String.IsNullOrWhiteSpace(targetUrl) then None
    else
        let target = targetUrl.Trim()
        let mutable targetUri = Unchecked.defaultof<Uri>
        if Uri.TryCreate(target, UriKind.Absolute, &targetUri)
           && (targetUri.Scheme = Uri.UriSchemeHttp || targetUri.Scheme = Uri.UriSchemeHttps)
           && String.IsNullOrEmpty(targetUri.UserInfo)
           && Uri.IsWellFormedUriString(target, UriKind.Absolute) then
            Some target
        else
            None

let append (item: XElement) (kind: EntryKind) : unit =
    let term, profileRole, relationship =
        match kind with
        | EntryKind.BlogPost -> "BlogPosting", "author", None
        | EntryKind.Note -> "SocialMediaPosting", "author", None
        | EntryKind.Reply target -> "Comment", "author", Some("parentItem", target)
        | EntryKind.Like target -> "LikeAction", "agent", Some("object", target)
        | EntryKind.Bookmark target -> "BookmarkAction", "agent", Some("object", target)
    let atom = XNamespace.Get "http://www.w3.org/2005/Atom"
    let schema = "https://schema.org/"
    let link (role: string) (href: string) =
        XElement(atom + "link", XAttribute(XName.Get "rel", schema + role), XAttribute(XName.Get "href", href))
    item.Add(XElement(atom + "category", XAttribute(XName.Get "term", schema + term)))
    item.Add(link profileRole "https://lqdev.me/")
    match relationship with
    | Some(role, target) -> item.Add(link role target)
    | None -> ()
