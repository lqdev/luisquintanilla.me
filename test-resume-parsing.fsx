#r "nuget: Markdig"
#r "nuget: YamlDotNet"
#load "CustomBlocks.fs"
#load "Domain.fs"

open System
open System.IO
open Markdig
open Markdig.Parsers
open Markdig.Syntax

// Read the resume file
let resumeContent = File.ReadAllText("_src/resume/resume.md")

// Parse with resume extensions
let pipeline = 
    MarkdownPipelineBuilder()
        .UseYamlFrontMatter()
        .UsePipeTables()
        .Use(CustomBlocks.ResumeBlockExtension())
        .Build()

let doc = Markdown.Parse(resumeContent, pipeline)

printfn "Total blocks in document: %d" (Seq.length doc)

// Check for experience blocks
let experiences = 
    Markdig.Syntax.MarkdownObjectExtensions.Descendants<CustomBlocks.ExperienceBlock>(doc)
    |> Seq.toList

printfn "Experience blocks found: %d" experiences.Length
for exp in experiences do
    printfn "  - Role: %s, Company: %s" exp.Role exp.Company

// Check for project blocks
let projects = 
    Markdig.Syntax.MarkdownObjectExtensions.Descendants<CustomBlocks.ProjectBlock>(doc)
    |> Seq.toList

printfn "Project blocks found: %d" projects.Length
for proj in projects do
    printfn "  - Title: %s" proj.Title

// Check for skills blocks
let skills = 
    Markdig.Syntax.MarkdownObjectExtensions.Descendants<CustomBlocks.SkillsBlock>(doc)
    |> Seq.toList

printfn "Skills blocks found: %d" skills.Length
for skill in skills do
    printfn "  - Category: %s" skill.Category

// Check for education blocks
let education = 
    Markdig.Syntax.MarkdownObjectExtensions.Descendants<CustomBlocks.EducationBlock>(doc)
    |> Seq.toList

printfn "Education blocks found: %d" education.Length
for edu in education do
    printfn "  - Degree: %s, Institution: %s" edu.Degree edu.Institution

// Check for testimonial blocks
let testimonials = 
    Markdig.Syntax.MarkdownObjectExtensions.Descendants<CustomBlocks.TestimonialBlock>(doc)
    |> Seq.toList

printfn "Testimonial blocks found: %d" testimonials.Length
for test in testimonials do
    printfn "  - Author: %s" test.Author
