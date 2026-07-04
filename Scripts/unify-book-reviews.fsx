open System
open System.IO
open System.Text.RegularExpressions

let dir = Path.Combine(__SOURCE_DIRECTORY__, "..", "_src", "reviews", "library")
let moveKeys = set ["author"; "isbn"; "datePublished"]

for file in Directory.GetFiles(dir, "*.md") do
    let text = File.ReadAllText(file)
    let m = Regex.Match(text, @"(?s)(:::review\r?\n)(.*?)(\r?\n:::)" )
    if not m.Success then
        ()
    else
        let blockContent = m.Groups.[2].Value
        let lines = blockContent.Replace("\r\n", "\n").Split('\n') |> Array.toList
        let mutable moved = []
        let mutable cover = None
        let kept = ResizeArray<string>()

        for line in lines do
            let trimmed = line.TrimEnd()
            let topLevelMatch = Regex.Match(trimmed, @"^([A-Za-z0-9_]+):\s*(.*)$")
            if topLevelMatch.Success then
                let key = topLevelMatch.Groups.[1].Value
                let value = topLevelMatch.Groups.[2].Value.Trim()
                match key with
                | k when moveKeys.Contains k -> moved <- (k, value) :: moved
                | "cover" -> cover <- Some value
                | _ -> kept.Add line
            else
                kept.Add line

        let keptLines = kept |> Seq.toList

        let addImageUrl =
            let hasImageUrl = keptLines |> Seq.exists (fun line -> Regex.IsMatch(line, @"^imageUrl:\s*"))
            if cover.IsSome && not hasImageUrl then
                Some (sprintf "imageUrl: %s" cover.Value)
            else None

        let movedFields =
            moved
            |> List.rev
            |> List.map (fun (k, v) -> sprintf "  %s: %s" k v)

        let newBlockLines =
            let lines = ResizeArray<string>()
            for line in keptLines do
                lines.Add(line)
            match addImageUrl with
            | Some imageLine -> lines.Add(imageLine)
            | None -> ()
            if not (List.isEmpty movedFields) then
                if lines.Count = 0 || (lines.[lines.Count - 1] <> "additionalFields:" && (lines |> Seq.exists (fun line -> line = "additionalFields:")) = false) then
                    lines.Add("additionalFields:")
                for fieldLine in movedFields do
                    lines.Add(fieldLine)
            lines |> Seq.toList

        let newBlock = String.concat "\n" newBlockLines
        let newText =
            let prefix = m.Groups.[1].Value
            let suffix = m.Groups.[3].Value
            text.Substring(0, m.Index) + prefix + newBlock + suffix
        File.WriteAllText(file, newText)
        printfn "migrated %s" (Path.GetFileName file)
