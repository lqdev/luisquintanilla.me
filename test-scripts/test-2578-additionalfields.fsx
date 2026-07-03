// Validation for issue #2578: nested additionalFields survive parsing; books unchanged.
#r "../bin/Debug/net10.0/PersonalSite.dll"

open ASTParsing
open CustomBlocks

let mutable failures = 0
let check name cond =
    if cond then printfn "PASS  %s" name
    else failures <- failures + 1; printfn "FAIL  %s" name

let fieldsOf (file: string) =
    match parseReviewFromFile file with
    | Ok doc ->
        match doc.CustomBlocks.TryGetValue("review") with
        | true, lst when lst.Length > 0 ->
            match lst.[0] with
            | :? ReviewData as rd -> Some (rd.GetAdditionalFields())
            | _ -> None
        | _ -> None
    | Error e -> printfn "PARSE ERROR %s: %A" file e; None

// Movie: nested additionalFields must now be present
match fieldsOf @"C:\Dev\website\_src\reviews\movies\hell-house-llc-lineage-2026-07-03.md" with
| Some f ->
    check "movie.director = Stephen Cognetti" (f.TryFind "director" = Some "Stephen Cognetti")
    check "movie.year = 2025"                 (f.TryFind "year"     = Some "2025")
    check "movie.genre = horror,mystery"      (f.TryFind "genre"    = Some "horror,mystery")
| None -> check "movie review block parsed" false

// Book: must still expose author/isbn (no regression)
match fieldsOf @"C:\Dev\website\_src\reviews\library\we-zamyatin.md" with
| Some f ->
    check "book.author = Yevgeny Zamyatin" (f.TryFind "author" = Some "Yevgeny Zamyatin")
    check "book.isbn = 9780140185850"      (f.TryFind "isbn"   = Some "9780140185850")
| None -> check "book review block parsed" false

printfn ""
if failures = 0 then printfn "ALL CHECKS PASSED"
else (printfn "%d CHECK(S) FAILED" failures; exit 1)
