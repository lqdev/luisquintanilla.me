(*
    Identify follow/unfollow webmentions from roll changes.

    Compares Data/blogroll.json and Data/podroll.json at a base commit against the working
    tree and emits one webmention per added or removed HtmlUrl:
        source = https://lqdev.me/collections/{roll}/
        target = HtmlUrl
    Removed entries are sent too: the receiver re-fetches the source, finds no link, and
    deletes the follow (this is how webmention "unfollow" works).

    Only HtmlUrl membership matters. Edits to other fields (Title, XmlUrl, Added) never
    produce a webmention, so metadata backfills do not notify anyone.

    Usage:
      dotnet fsi Scripts/identify-follow-webmentions.fsx -- --base <commit> [--no-removals]
      dotnet fsi Scripts/identify-follow-webmentions.fsx -- --all          # deliberate announce of every entry
      dotnet fsi Scripts/identify-follow-webmentions.fsx --                # no base: writes an empty list

    Output: _tmp/follow-webmentions.json (same shape as _tmp/webmentions.json)
*)

open System
open System.Diagnostics
open System.IO
open System.Text.Json
open System.Text.Json.Nodes

type WebmentionData = { Source: string; Target: string }

let args = fsi.CommandLineArgs |> Array.skip 1 |> Array.toList

let rec parseArgs (remaining: string list) (baseSha: string option, all: bool, removals: bool) =
    match remaining with
    | "--base" :: sha :: rest -> parseArgs rest (Some sha, all, removals)
    | "--all" :: rest -> parseArgs rest (baseSha, true, removals)
    | "--no-removals" :: rest -> parseArgs rest (baseSha, all, false)
    | other :: _ -> failwithf "Unknown argument: %s" other
    | [] -> (baseSha, all, removals)

let baseSha, announceAll, includeRemovals = parseArgs args (None, false, true)

let rolls =
    [ "blogroll", Path.Combine("Data", "blogroll.json")
      "podroll", Path.Combine("Data", "podroll.json") ]

let sourceFor (roll: string) = $"https://lqdev.me/collections/{roll}/"

let htmlUrls (json: string) : Set<string> =
    match JsonNode.Parse(json) with
    | :? JsonArray as entries ->
        entries
        |> Seq.choose (fun entry ->
            match entry.["HtmlUrl"] with
            | null -> None
            | node -> Some (node.GetValue<string>().Trim()))
        |> Set.ofSeq
    | _ -> failwith "Roll file is not a JSON array"

let gitShow (sha: string) (file: string) : string option =
    let psi = ProcessStartInfo("git", RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false)
    for arg in [ "show"; $"{sha}:{file}" ] do psi.ArgumentList.Add(arg)
    use proc = Process.Start(psi)
    let stdout = proc.StandardOutput.ReadToEnd()
    proc.StandardError.ReadToEnd() |> ignore
    proc.WaitForExit()
    if proc.ExitCode = 0 then Some stdout else None

let mentionsForRoll (roll: string, file: string) : WebmentionData list =
    let current = htmlUrls (File.ReadAllText(file))
    let source = sourceFor roll
    let toMentions (urls: Set<string>) = urls |> Set.toList |> List.map (fun url -> { Source = source; Target = url })

    if announceAll then
        printfn "📣 %s: announcing all %d entries" roll current.Count
        toMentions current
    else
        match baseSha with
        | None -> []
        | Some sha ->
            match gitShow sha file with
            | None ->
                // Missing base file would diff as "everything added"; refuse rather than mass-send.
                printfn "⚠️  %s: %s not found at %s; skipping to avoid a mass send" roll file sha
                []
            | Some baseJson ->
                let previous = htmlUrls baseJson
                let added = Set.difference current previous
                let removed = if includeRemovals then Set.difference previous current else Set.empty
                for url in added do printfn "➕ %s follow:   %s" roll url
                for url in removed do printfn "➖ %s unfollow: %s" roll url
                toMentions (Set.union added removed)

if baseSha.IsNone && not announceAll then
    printfn "No --base commit and no --all; nothing to compare."

let webmentions = rolls |> List.collect mentionsForRoll |> List.toArray

let outputDir = "_tmp"
Directory.CreateDirectory(outputDir) |> ignore
let outputPath = Path.Combine(outputDir, "follow-webmentions.json")
File.WriteAllText(outputPath, JsonSerializer.Serialize(webmentions, JsonSerializerOptions(WriteIndented = true)))

printfn "Wrote %d follow webmentions to %s" webmentions.Length outputPath
