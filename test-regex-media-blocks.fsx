#r "nuget: FSharp.Core"

open System.Text.RegularExpressions

let html = """<div class="media"><p>media_type: image
uri: /images/fall-mountains/sunrise.jpg
alt_text: Sunrise at mountain summit with mist and fall foliage
caption: Sunrise
aspect: ""</p>
</div>"""

let mediaBlockPattern = @"<div class=""media""><p>(.*?)</p></div>"
let regex = Regex(mediaBlockPattern, RegexOptions.Singleline)

let matches = regex.Matches(html)
printfn "Pattern: %s" mediaBlockPattern
printfn "HTML: %s" html
printfn "Matches found: %d" matches.Count

if matches.Count > 0 then
    for i = 0 to matches.Count - 1 do
        let m = matches.[i]
        printfn "Match %d: %s" i m.Value
        printfn "Group 1: %s" m.Groups.[1].Value
else
    printfn "No matches found"

// Test the YAML parsing part
let yamlContent = """media_type: image
uri: /images/fall-mountains/sunrise.jpg
alt_text: Sunrise at mountain summit with mist and fall foliage
caption: Sunrise
aspect: """"

let lines = yamlContent.Split([|'\n'|], System.StringSplitOptions.RemoveEmptyEntries)
let properties = 
    lines 
    |> Array.fold (fun acc line ->
        if line.Contains(":") then
            let parts = line.Split([|':'|], 2)
            if parts.Length = 2 then
                let key = parts.[0].Trim()
                let value = parts.[1].Trim().Trim('"')
                Map.add key value acc
            else acc
        else acc
    ) Map.empty

printfn "Parsed properties:"
properties |> Map.iter (fun k v -> printfn "  %s: %s" k v)
