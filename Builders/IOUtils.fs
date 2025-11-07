module IOUtils

open System.IO

/// Ensure directory exists, creating it if necessary
let ensureDirectory (path: string) =
    if not (Directory.Exists(path)) then
        Directory.CreateDirectory(path) |> ignore
    path

/// Write text content to a file, ensuring parent directory exists
let writeFile (path: string) (content: string) =
    let directory = Path.GetDirectoryName(path)
    ensureDirectory directory |> ignore
    File.WriteAllText(path, content)

/// Get all markdown files in a directory
let getMarkdownFiles (directory: string) =
    if Directory.Exists(directory) then
        Directory.GetFiles(directory)
        |> Array.filter (fun f -> f.EndsWith(".md"))
        |> Array.toList
    else
        []

/// Join path segments safely
let joinPath (segments: string list) =
    match segments with
    | [] -> ""
    | [single] -> single
    | head :: tail -> tail |> List.fold (fun acc seg -> Path.Join(acc, seg)) head
