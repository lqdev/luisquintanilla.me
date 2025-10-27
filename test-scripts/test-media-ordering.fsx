#!/usr/bin/env dotnet fsi

// Test script to verify media landing page displays posts in descending date order
// This test validates the fix for GitHub issue: Fix media landing page post ordering

#r "nuget: FSharp.Data"

open System
open System.IO
open FSharp.Data

let publicDir = "_public"
let mediaIndexPath = Path.Join(publicDir, "media", "index.html")

// Function to extract dates from media landing page HTML
let extractMediaDates (html: string) =
    let datePattern = System.Text.RegularExpressions.Regex(@"<li>.*?• ([A-Za-z]{3} \d{1,2}, \d{4})</li>")
    datePattern.Matches(html)
    |> Seq.cast<System.Text.RegularExpressions.Match>
    |> Seq.map (fun m -> m.Groups.[1].Value)
    |> Seq.toList

// Function to parse date string to DateTimeOffset for comparison
let parseDate (dateStr: string) =
    DateTime.ParseExact(dateStr, "MMM dd, yyyy", System.Globalization.CultureInfo.InvariantCulture)

printfn "Testing media landing page date ordering..."
printfn "============================================\n"

if not (File.Exists(mediaIndexPath)) then
    printfn "❌ ERROR: Media index page not found at %s" mediaIndexPath
    printfn "   Please run 'dotnet run' first to generate the site."
    exit 1

let html = File.ReadAllText(mediaIndexPath)
let dates = extractMediaDates html

printfn "Found %d media posts on landing page:\n" dates.Length

dates
|> List.iteri (fun i date -> 
    printfn "  %d. %s" (i + 1) date)

printfn "\nChecking date ordering (should be descending)..."

// Convert to DateTimes and check if sorted in descending order
let dateTimes = dates |> List.map parseDate
let sortedDates = dateTimes |> List.sortByDescending id

if dateTimes = sortedDates then
    printfn "✅ SUCCESS: Media posts are correctly ordered by date (descending)"
    printfn "\n   Most recent: %s" (List.head dates)
    printfn "   Oldest: %s" (List.last dates)
    exit 0
else
    printfn "❌ FAILED: Media posts are NOT correctly ordered"
    printfn "\n   Expected order (descending):"
    sortedDates 
    |> List.iteri (fun i date -> 
        printfn "     %d. %s" (i + 1) (date.ToString("MMM dd, yyyy")))
    printfn "\n   Actual order:"
    dateTimes 
    |> List.iteri (fun i date -> 
        printfn "     %d. %s" (i + 1) (date.ToString("MMM dd, yyyy")))
    exit 1
