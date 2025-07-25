open System.Text.RegularExpressions

let normalizeUrlsForRss (content: string) (baseUrl: string) =
    let baseUrl = baseUrl.TrimEnd('/')
    
    // Normalize relative image src attributes
    let imgPattern = @"src\s*=\s*""(/[^""]*)""|src\s*=\s*'(/[^']*)'"
    let normalizedImages = 
        Regex.Replace(content, imgPattern, fun m ->
            let quote = if m.Value.Contains("\"") then "\"" else "'"
            let path = if m.Groups.[1].Success then m.Groups.[1].Value else m.Groups.[2].Value
            sprintf "src=%s%s%s%s" quote baseUrl path quote)
    
    // Normalize relative link href attributes  
    let linkPattern = @"href\s*=\s*""(/[^""]*)""|href\s*=\s*'(/[^']*)'"
    let normalizedLinks =
        Regex.Replace(normalizedImages, linkPattern, fun m ->
            let quote = if m.Value.Contains("\"") then "\"" else "'"
            let path = if m.Groups.[1].Success then m.Groups.[1].Value else m.Groups.[2].Value
            sprintf "href=%s%s%s%s" quote baseUrl path quote)
    
    normalizedLinks

// Test cases
let testContent1 = "<p>Check out <a href='/posts/test'>this post</a> and <img src='/images/test.png' alt='test'></p>"
let testContent2 = "<p>Link to <a href=\"/wiki/example\">wiki</a> and image <img src=\"/assets/photo.jpg\" alt=\"photo\"></p>"
let baseUrl = "https://www.luisquintanilla.me"

printfn "=== URL Normalization Test ==="
printfn ""
printfn "Test 1:"
printfn "Original: %s" testContent1
let result1 = normalizeUrlsForRss testContent1 baseUrl
printfn "Normalized: %s" result1
printfn ""

printfn "Test 2:" 
printfn "Original: %s" testContent2
let result2 = normalizeUrlsForRss testContent2 baseUrl
printfn "Normalized: %s" result2
printfn ""

// Test edge cases
let testContent3 = "<p>Absolute URLs should not change: <a href='https://external.com/test'>external</a> and <img src='https://cdn.example.com/image.png'></p>"
printfn "Test 3 (should not change absolute URLs):"
printfn "Original: %s" testContent3
let result3 = normalizeUrlsForRss testContent3 baseUrl
printfn "Normalized: %s" result3
printfn ""

printfn "✅ URL normalization tests completed"
