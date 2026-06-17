module ResumePageBuilder

    open System
    open System.IO
    open Domain
    open ViewGenerator
    open Markdig
    open Markdig.Parsers
    open BuilderCommon

    let private extractSectionContent (doc: Markdig.Syntax.MarkdownDocument) (headingText: string) =
        let headings = 
            Markdig.Syntax.MarkdownObjectExtensions.Descendants<Markdig.Syntax.HeadingBlock>(doc)
            |> Seq.toList
        
        // Helper to extract text from heading inline content
        let getHeadingText (h: Markdig.Syntax.HeadingBlock) =
            if h.Inline <> null then
                // Get first child literal if it exists
                let literals = 
                    Markdig.Syntax.MarkdownObjectExtensions.Descendants<Markdig.Syntax.Inlines.LiteralInline>(h.Inline)
                    |> Seq.toList
                if not literals.IsEmpty then
                    literals |> List.map (fun l -> l.Content.ToString()) |> String.concat ""
                else
                    ""
            else
                ""
        
        // Find the heading that matches the target text (case-insensitive)
        let targetHeadingOpt = 
            headings 
            |> List.tryFind (fun h -> 
                let text = getHeadingText h
                text.Trim().Equals(headingText, StringComparison.OrdinalIgnoreCase))
        
        match targetHeadingOpt with
        | None -> None
        | Some targetHeading ->
            // Find all blocks that come after this heading until the next heading of same or higher level
            let allBlocks = doc |> Seq.toList
            let targetIndex = allBlocks |> List.findIndex (fun b -> Object.ReferenceEquals(b, targetHeading))
            let targetLevel = targetHeading.Level
            
            // Get blocks until next heading of same or higher level
            let contentBlocks = 
                allBlocks
                |> List.skip (targetIndex + 1)
                |> List.takeWhile (fun block ->
                    match block with
                    | :? Markdig.Syntax.HeadingBlock as h -> h.Level > targetLevel
                    | _ -> true)
            
            if contentBlocks.IsEmpty then
                None
            else
                // Convert blocks back to markdown using Markdig renderer
                use writer = new System.IO.StringWriter()
                let renderer = Markdig.Renderers.HtmlRenderer(writer)
                for block in contentBlocks do
                    renderer.Write(block)
                let html = writer.ToString().Trim()
                if String.IsNullOrWhiteSpace(html) then None else Some html
    
    let buildResumePage () =
        let resumePath = Path.Join(srcDir, "resume", "resume.md")
        
        match Loaders.loadResume resumePath with
        | None ->
            printfn "⚠ No resume found at %s - skipping resume page" resumePath
        | Some baseResume ->
            try
                // Parse the markdown with resume block extensions
                let pipeline = 
                    MarkdownPipelineBuilder()
                        .UseYamlFrontMatter()
                        .UsePipeTables()
                        .UseTaskLists()
                        .UseDiagrams()
                        .UseMediaLinks()
                        .UseMathematics()
                        .UseEmojiAndSmiley()
                        .UseEmphasisExtras()
                        .UseBootstrap()
                        .UseFigures()
                        .Use(CustomBlocks.ResumeBlockExtension())
                        .Build()
                
                let doc = Markdown.Parse(baseResume.Content, pipeline)
                
                // Extract resume blocks from AST
                let experiences = 
                    Markdig.Syntax.MarkdownObjectExtensions.Descendants<CustomBlocks.ExperienceBlock>(doc)
                    |> Seq.map (fun (block: CustomBlocks.ExperienceBlock) ->
                        {
                            Role = block.Role
                            Company = block.Company
                            StartDate = DateTime.Parse(block.Start)
                            EndDate = 
                                match block.End with
                                | Some "current" -> None
                                | Some date -> Some (DateTime.Parse(date))
                                | None -> None
                            Highlights = 
                                if String.IsNullOrWhiteSpace(block.Content) || block.Content = " " then None
                                else 
                                    block.Content.Split('\n')
                                    |> Array.filter (fun s -> not (String.IsNullOrWhiteSpace s))
                                    |> Array.toList
                                    |> Some
                        } : Domain.Experience)
                    |> Seq.toList
                
                let projects = 
                    Markdig.Syntax.MarkdownObjectExtensions.Descendants<CustomBlocks.ProjectBlock>(doc)
                    |> Seq.map (fun (block: CustomBlocks.ProjectBlock) ->
                        let description = if String.IsNullOrWhiteSpace(block.Content) || block.Content = " " then "" else block.Content
                        let techs = 
                            match block.Tech with
                            | Some t when not (String.IsNullOrWhiteSpace t) ->
                                t.Split(',') |> Array.map (fun s -> s.Trim()) |> Array.toList |> Some
                            | _ -> None
                        {
                            Title = block.Title
                            Description = description
                            Url = block.Url
                            Technologies = techs
                            Highlights = None  // Not used in current design
                        } : Domain.Project)
                    |> Seq.toList
                
                let skills = 
                    Markdig.Syntax.MarkdownObjectExtensions.Descendants<CustomBlocks.SkillsBlock>(doc)
                    |> Seq.map (fun (block: CustomBlocks.SkillsBlock) ->
                        let skillList = 
                            if String.IsNullOrWhiteSpace(block.Content) || block.Content = " " then []
                            elif block.Content.Contains(",") && not (block.Content.Contains("\n-") || block.Content.Contains("\n*")) then
                                // Comma-separated
                                block.Content.Split(',') |> Array.map (fun s -> s.Trim()) |> Array.toList
                            else
                                // Bullet list
                                block.Content.Split('\n') 
                                |> Array.filter (fun s -> not (String.IsNullOrWhiteSpace s))
                                |> Array.map (fun s -> s.Trim().TrimStart('-', '*').Trim())
                                |> Array.toList
                        {
                            Category = block.Category
                            Skills = skillList
                        } : Domain.SkillCategory)
                    |> Seq.toList
                
                let testimonials = 
                    Markdig.Syntax.MarkdownObjectExtensions.Descendants<CustomBlocks.TestimonialBlock>(doc)
                    |> Seq.map (fun (block: CustomBlocks.TestimonialBlock) ->
                        {
                            Quote = if String.IsNullOrWhiteSpace(block.Content) || block.Content = " " then "" else block.Content
                            Author = block.Author
                        } : Domain.Testimonial)
                    |> Seq.toList
                
                let education = 
                    Markdig.Syntax.MarkdownObjectExtensions.Descendants<CustomBlocks.EducationBlock>(doc)
                    |> Seq.map (fun (block: CustomBlocks.EducationBlock) ->
                        let yearOpt = 
                            match block.Year with
                            | Some y when not (String.IsNullOrWhiteSpace y) ->
                                match Int32.TryParse(y) with
                                | (true, year) -> Some year
                                | _ -> None
                            | _ -> None
                        let details = 
                            if String.IsNullOrWhiteSpace(block.Content) || block.Content = " " then None
                            else Some block.Content
                        {
                            Degree = block.Degree
                            Institution = block.Institution
                            GraduationYear = yearOpt
                            Details = details
                        } : Domain.Education)
                    |> Seq.toList
                
                // Extract About and Interests sections from markdown
                let aboutSection = extractSectionContent doc "About"
                let interestsSection = 
                    match extractSectionContent doc "Currently Interested In" with
                    | Some content -> Some content
                    | None -> extractSectionContent doc "Interests"
                
                // Build complete resume with extracted data
                let completeResume = 
                    { baseResume with
                        AboutSection = aboutSection
                        InterestsSection = interestsSection
                        Experience = experiences
                        Projects = projects
                        Skills = skills
                        Testimonials = testimonials
                        Education = education
                    }
                
                // Generate HTML (will add view in next phase)
                let html = ContentViews.ResumeView.render completeResume
                let resumePage = generate html "default" "Resume - Luis Quintanilla"
                
                let saveDir = Path.Join(outputDir, "resume")
                Directory.CreateDirectory(saveDir) |> ignore
                File.WriteAllText(Path.Join(saveDir, "index.html"), resumePage)
                
                printfn "✅ Resume page built successfully"
            with
            | ex ->
                printfn "❌ Error building resume page: %s" ex.Message
