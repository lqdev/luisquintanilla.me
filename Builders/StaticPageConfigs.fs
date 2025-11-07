module StaticPageConfigs

open StaticPagePipeline

/// All static page configurations
let staticPageConfigs = [
    { SourceFile = "about.md"
      OutputDir = "about"
      Title = "About - Luis Quintanilla"
      Layout = "default" }
    
    { SourceFile = "collections.md"
      OutputDir = "collections"
      Title = "Collections - Luis Quintanilla"
      Layout = "default" }
    
    { SourceFile = "contact.md"
      OutputDir = "contact"
      Title = "Contact - Luis Quintanilla"
      Layout = "default" }
    
    { SourceFile = "search.md"
      OutputDir = "search"
      Title = "Search - Luis Quintanilla"
      Layout = "default" }
    
    { SourceFile = "starter-packs.md"
      OutputDir = "collections/starter-packs"
      Title = "Starter Packs - Luis Quintanilla"
      Layout = "default" }
    
    { SourceFile = "travel-guides.md"
      OutputDir = "collections/travel-guides"
      Title = "Travel Guides - Luis Quintanilla"
      Layout = "default" }
    
    { SourceFile = "uses.md"
      OutputDir = "uses"
      Title = "In Real Life Stack - Luis Quintanilla"
      Layout = "default" }
    
    { SourceFile = "colophon.md"
      OutputDir = "colophon"
      Title = "Colophon - Luis Quintanilla"
      Layout = "default" }
    
    { SourceFile = "radio.md"
      OutputDir = "online-radio"
      Title = "Online Radio - Luis Quintanilla"
      Layout = "default" }
]
