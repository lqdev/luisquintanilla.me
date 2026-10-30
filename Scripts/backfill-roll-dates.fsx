(*
    Backfill "Added" dates for roll entries from git history.

    For each entry in Data/blogroll.json and Data/podroll.json that lacks an "Added" field,
    finds the first commit whose diff introduced the entry's HtmlUrl into that file
    (git log -S) and records its author date (YYYY-MM-DD). Entries present since the file
    was created all receive the file-creation date, which is a floor, not a true follow date.

    Idempotent: entries that already have "Added" are left untouched.
    Requires full history (not a shallow clone): git fetch --unshallow

    Usage: dotnet fsi Scripts/backfill-roll-dates.fsx
*)

open System
open System.Diagnostics
open System.IO
open System.Text.Json
open System.Text.Json.Nodes

let rollFiles = [ Path.Combine("Data", "blogroll.json"); Path.Combine("Data", "podroll.json") ]

let runGit (arguments: string list) : string =
    let psi = ProcessStartInfo("git", RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false)
    for arg in arguments do psi.ArgumentList.Add(arg)
    use proc = Process.Start(psi)
    let stdout = proc.StandardOutput.ReadToEnd()
    let stderr = proc.StandardError.ReadToEnd()
    proc.WaitForExit()
    if proc.ExitCode <> 0 then failwithf "git %s failed: %s" (String.Join(" ", arguments)) stderr
    stdout

if (runGit [ "rev-parse"; "--is-shallow-repository" ]).Trim() = "true" then
    printfn "❌ Repository is shallow; run: git fetch --unshallow"
    exit 1

let firstIntroducedDate (file: string) (htmlUrl: string) : string option =
    runGit [ "log"; "--format=%as"; "--reverse"; "-S"; htmlUrl; "--"; file ]
    |> fun output -> output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
    |> Array.tryHead
    |> Option.map (fun line -> line.Trim())

let writeOptions =
    JsonSerializerOptions(
        WriteIndented = true,
        IndentSize = 4,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping)

for file in rollFiles do
    let entries =
        match JsonNode.Parse(File.ReadAllText(file)) with
        | :? JsonArray as arr -> arr
        | _ -> failwithf "%s is not a JSON array" file

    let mutable updated = 0
    for entry in entries do
        match entry.["Added"], entry.["HtmlUrl"] with
        | null, (:? JsonValue as urlNode) ->
            let htmlUrl = urlNode.GetValue<string>()
            match firstIntroducedDate file htmlUrl with
            | Some date ->
                entry.["Added"] <- JsonValue.Create(date)
                updated <- updated + 1
                printfn "  %s  %s" date htmlUrl
            | None -> printfn "  ⚠️  no commit found for %s" htmlUrl
        | _ -> ()

    File.WriteAllText(file, entries.ToJsonString(writeOptions) + "\n")
    printfn "✅ %s: %d entries backfilled, %d total" file updated entries.Count
