module ReadingTimeService

open System
open System.Text.RegularExpressions

/// Calculate reading time in minutes based on word count
/// Uses standard 200 words per minute reading speed
let calculateReadingTime (content: string) : int option =
    if String.IsNullOrWhiteSpace(content) then
        None
    else
        // Remove markdown code blocks (they're typically skimmed rather than read)
        let withoutCodeBlocks = Regex.Replace(content, @"```[\s\S]*?```", "", RegexOptions.Multiline)
        
        // Remove inline code
        let withoutInlineCode = Regex.Replace(withoutCodeBlocks, @"`[^`]+`", "")
        
        // Remove markdown links but keep the text
        let withoutLinks = Regex.Replace(withoutInlineCode, @"\[([^\]]+)\]\([^\)]+\)", "$1")
        
        // Remove HTML tags
        let withoutHtml = Regex.Replace(withoutLinks, @"<[^>]+>", "")
        
        // Count words (split on whitespace and filter empty strings)
        let words = 
            withoutHtml.Split([|' '; '\t'; '\n'; '\r'|], StringSplitOptions.RemoveEmptyEntries)
            |> Array.filter (fun w -> not (String.IsNullOrWhiteSpace(w)))
        
        let wordCount = words.Length
        
        // Calculate reading time (minimum 1 minute for any content)
        if wordCount > 0 then
            let minutes = Math.Ceiling(float wordCount / 200.0) |> int
            Some (max 1 minutes)
        else
            None


