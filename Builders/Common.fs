module BuilderCommon

    open System.IO

    let srcDir = "_src"
    let outputDir = "_public"

    /// Sanitize tag names for safe file system paths while preserving readability
    let sanitizeTagForPath (tag: string) =
        tag.Trim()
            .Replace("\"", "")       // Remove quotes
            .Replace("#", "sharp")   // Replace # with "sharp" (f# -> fsharp, c# -> csharp)
            .Replace(" ", "-")       // Replace spaces with hyphens
            .Replace(".", "dot")     // Replace dots with "dot" (.net -> dotnet)
            .Replace("/", "-")       // Replace slashes with hyphens
            .Replace("\\", "-")      // Replace backslashes with hyphens
            .Replace(":", "-")       // Replace colons with hyphens
            .Replace("*", "star")    // Replace asterisks
            .Replace("?", "q")       // Replace question marks
            .Replace("<", "lt")      // Replace less than
            .Replace(">", "gt")      // Replace greater than
            .Replace("|", "pipe")    // Replace pipes
            .ToLowerInvariant()      // Make lowercase for consistency

    // =====================================================================
    // Common Build Helper Functions - Reduce Duplication
    // =====================================================================

    /// Helper: Write HTML page to a directory, ensuring directory exists
    let writePageToDir (dir: string) (filename: string) (content: string) =
        Directory.CreateDirectory(dir) |> ignore
        File.WriteAllText(Path.Join(dir, filename), content)

    /// Helper: Write file content to a directory, ensuring directory exists
    let writeFileToDir (dir: string) (filename: string) (content: string) =
        Directory.CreateDirectory(dir) |> ignore
        File.WriteAllText(Path.Join(dir, filename), content)

    /// Helper: Get markdown files from a source directory
    let getContentFiles (relativePath: string) =
        Directory.GetFiles(Path.Join(srcDir, relativePath))
        |> Array.filter (fun f -> f.EndsWith(".md"))
        |> Array.toList
