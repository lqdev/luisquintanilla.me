// Test script for send-webmentions.fsx
// Validates that the script can parse response files correctly

#r "nuget: YamlDotNet, 16.3.0"

open System
open System.IO
open System.Text.RegularExpressions
open YamlDotNet.Serialization

// Response metadata structure matching Domain.fs
[<CLIMutable>]
type ResponseDetails = {
    title: string
    targeturl: string
    response_type: string
    dt_published: string
    dt_updated: string
    tags: string array
}

// Response with filename
type Response = {
    FileName: string
    Metadata: ResponseDetails
}

// Parse a response markdown file to extract metadata
let parseResponseFile (filePath: string) : Response option =
    try
        let content = File.ReadAllText(filePath)
        let fileName = Path.GetFileNameWithoutExtension(filePath)
        
        // Extract YAML frontmatter using regex
        let yamlPattern = @"^---\s*\n(.*?)\n---"
        let yamlMatch = Regex.Match(content, yamlPattern, RegexOptions.Singleline)
        
        if yamlMatch.Success then
            let yamlContent = yamlMatch.Groups.[1].Value
            let deserializer = DeserializerBuilder().Build()
            let metadata = deserializer.Deserialize<ResponseDetails>(yamlContent)
            Some { FileName = fileName; Metadata = metadata }
        else
            None
    with
    | ex -> 
        printfn $"Error parsing {filePath}: {ex.Message}"
        None

// Test parsing responses
let testParsing () =
    printfn "Testing response file parsing..."
    
    let responsesDir = Path.Combine("_src", "responses")
    if not (Directory.Exists(responsesDir)) then
        printfn "❌ _src/responses directory not found"
        1
    else
        let files = Directory.GetFiles(responsesDir, "*.md")
        printfn $"Found {files.Length} response files"
        
        let parsed = files |> Array.choose parseResponseFile
        printfn $"Successfully parsed {parsed.Length} responses"
        
        if parsed.Length > 0 then
            let sample = parsed.[0]
            printfn "\nSample response:"
            printfn $"  FileName: {sample.FileName}"
            printfn $"  Title: {sample.Metadata.title}"
            printfn $"  Target URL: {sample.Metadata.targeturl}"
            printfn $"  Type: {sample.Metadata.response_type}"
            printfn $"  Published: {sample.Metadata.dt_published}"
            printfn $"  Updated: {sample.Metadata.dt_updated}"
            let tagsStr = 
                if isNull sample.Metadata.tags then 
                    "(none)"
                else 
                    String.Join(", ", sample.Metadata.tags)
            printfn $"  Tags: {tagsStr}"
        
        if parsed.Length = files.Length then
            printfn "\n✅ All response files parsed successfully"
            0
        else
            printfn $"\n⚠️ Warning: {files.Length - parsed.Length} files failed to parse"
            0

// Run tests
exit (testParsing())
