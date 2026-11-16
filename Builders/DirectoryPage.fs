module DirectoryPageBuilder

open System.IO
open Giraffe.ViewEngine
open Domain
open BuilderCommon
open ViewGenerator

let buildDirectoryPage () =
    let collections = Collections.FollowingDirectory.getIncludedCollections ()
    let extraHead = [
        link [_rel "stylesheet"; _href "/assets/css/directory.css"]
    ]
    let page =
        generateWithPageMetadata
            extraHead
            (pageMetadata "/directory/" "Search websites and feeds in Luis Quintanilla's curated follow lists." "website")
            (DirectoryViews.directoryPage collections)
            "default"
            "Directory - Luis Quintanilla"

    writePageToDir (Path.Join(outputDir, "directory")) "index.html" page

let buildDirectoryIndex (loadedCollections: (Collection * CollectionData) array) =
    let includedCollections = Collections.FollowingDirectory.getIncludedCollections ()
    let index = DirectoryIndexBuilder.buildIndex includedCollections loadedCollections
    let directoryPath = Path.Join(outputDir, "directory")
    Directory.CreateDirectory(directoryPath) |> ignore
    File.WriteAllText(Path.Join(directoryPath, "index.json"), DirectoryIndexBuilder.serialize index)
    printfn "✅ Generated directory index (%d sites)" index.Entries.Length
