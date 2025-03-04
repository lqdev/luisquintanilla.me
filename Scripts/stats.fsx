// Reference DLL
#r "../bin/Debug/net9.0/PersonalSite.dll"

// Add modules
open Domain
open Loaders
open System

// Load posts
let posts = loadPosts "_src"
let notes = loadFeed "_src"
let responses = loadReponses "_src"

// Organize posts by year
let postCountsByYear = 
    posts
    |> Array.countBy (fun (x:Post) -> DateTime.Parse(x.Metadata.Date) |> _.Year)
    |> Array.sortByDescending fst 

let noteCountsByYear = 
    notes
    |> Array.countBy (fun (x:Post) -> DateTime.Parse(x.Metadata.Date) |> _.Year)
    |> Array.sortByDescending fst

let responseCountsByYear = 
    responses
    |> Array.countBy (fun (x:Response) -> DateTime.Parse(x.Metadata.DatePublished) |> _.Year)
    |> Array.sortByDescending fst

// Organize responses by type
let responsesByType = 
    responses
    |> Array.filter(fun x -> (DateTime.Parse(x.Metadata.DatePublished) |> _.Year) = DateTime.UtcNow.Year)
    |> Array.countBy(fun x -> x.Metadata.ResponseType)
    |> Array.sortByDescending(snd)

// Organize responses by tag
let responsesByTag = 
    responses
    |> Array.filter(fun x -> (DateTime.Parse(x.Metadata.DatePublished) |> _.Year) = DateTime.UtcNow.Year)
    |> Array.collect(fun x -> 
            match x.Metadata.Tags with
            | null -> [|"untagged"|]
            | [||] -> [|"untagged"|]
            | _ -> x.Metadata.Tags
        )
    |> Array.countBy(fun x -> x)
    |> Array.sortByDescending(snd)

// Organize responses by host name (domain)
let responsesByDomain = 
    responses
    |> Array.filter(fun x -> (DateTime.Parse(x.Metadata.DatePublished) |> _.Year) = DateTime.UtcNow.Year)
    |> Array.countBy(fun x -> Uri(x.Metadata.TargetUrl).Host)
    |> Array.sortByDescending(snd)

// Utility function to display counts
let printEntryCounts<'a> (title:string) (entryCounts:('a * int) array) (n:int) = 
    printfn $"{title}"

    match entryCounts.Length with
    | 0 -> 
        printfn $"No entries"
        printfn $""
    | a when a > 0 -> 
        match n with 
        | n when n = -1 || n > entryCounts.Length -> 
            entryCounts
            |> Array.iter(fun x -> printfn $"{fst x} {snd x}")
            |> fun _ -> printfn $""
        | n when n > 0 -> 
            entryCounts
            |> Array.take n
            |> Array.iter(fun x -> printfn $"{fst x} {snd x}")
            |> fun _ -> printfn $""

// Print yearly counts
printEntryCounts "Blogs" postCountsByYear 2

printEntryCounts "Notes" noteCountsByYear 2

printEntryCounts "Responses" responseCountsByYear 2

// Print response types
printEntryCounts "Response Types" responsesByType -1

// Print response tag counts
printEntryCounts "Response Tags" responsesByTag 5

// Print response by host name
printEntryCounts "Domains" responsesByDomain 5