// Reference DLL
#r "../bin/Debug/net8.0/PersonalSite.dll"

// Add modules
open Domain
open Builder
open System

// Load posts
let posts = loadPosts()
let notes = loadFeed ()
let responses = loadReponses ()

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
    |> Array.filter(fun x -> (DateTime.Parse(x.Metadata.DatePublished) |> _.Year) = 2023)
    |> Array.countBy(fun x -> x.Metadata.ResponseType)

// Organize responses by tag
let responsesByTag = 
    responses
    |> Array.filter(fun x -> (DateTime.Parse(x.Metadata.DatePublished) |> _.Year) = 2023)
    |> Array.collect(fun x -> 
            match x.Metadata.Tags with
            | null -> [|"untagged"|]
            | [||] -> [|"untagged"|]
            | _ -> x.Metadata.Tags
        )
    |> Array.countBy(fun x -> x)
    |> Array.sortByDescending(snd)

// Utility function to display counts
let printEntryCounts<'a> (title:string) (entryCounts:('a * int) array) (n:int) = 
    printfn $"{title}"

    match n with 
    | n when n = -1 -> 
        entryCounts
        |> Array.iter(fun x -> printfn $"{fst x} {snd x}")
        |> fun _ -> printfn $""
    | n when n >= 0 -> 
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