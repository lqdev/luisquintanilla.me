# Content Processor Pattern Optimization Specification

## Overview

This specification defines the consistent application of the data extraction and caching pattern demonstrated in `BookProcessor` across all content processors to optimize performance and maintain clear separation of concerns.

## Current Issues

### 1. Redundant File Operations
- **PresentationProcessor**: Performs redundant `File.ReadAllText(filePath)` when `parsedDoc.RawMarkdown` is already available
- Impact: Unnecessary I/O operations during parsing phase

### 2. Complex Processing in Render Methods  
- **AlbumProcessor**: Complex regex parsing in `RenderCard` method to extract media information
- Impact: Expensive processing repeated during card generation, violates separation of concerns

### 3. Inconsistent Data Extraction Patterns
- **BookProcessor**: Uses proper `ReviewDataExtractor` module and caching
- **Other processors**: Mix parsing logic with rendering logic

## Proposed Pattern

### Data Extractor Module Pattern
```fsharp
module [ContentType]DataExtractor =
    /// Extract complex data from raw markdown content during parsing
    let extract[DataType]Data (rawMarkdown: string) : [ExtractedDataType] =
        // Perform expensive parsing operations once
        // Return structured data for caching
```

### Processor Caching Pattern
```fsharp
module [ContentType]Processor =
    // Cache for extracted data to avoid recomputation
    let private [dataType]Cache = ConcurrentDictionary<string, [ExtractedDataType]>()
    
    let create() : ContentProcessor<[ContentType]> = {
        Parse = fun filePath ->
            // Extract and cache complex data once during parsing
            let extractedData = [ContentType]DataExtractor.extract[DataType]Data parsedDoc.RawMarkdown
            [dataType]Cache.[fileName] <- extractedData
            
        RenderCard = fun content ->
            // Use cached data only, no file operations or complex parsing
            let cachedData = [dataType]Cache.TryGetValue(content.FileName)
            // Generate output using cached data
    }
```

## Implementation Plan

### Phase 1: PresentationProcessor Optimization
- Remove redundant `File.ReadAllText` operation
- Use `parsedDoc.RawMarkdown` directly for frontmatter extraction

### Phase 2: AlbumProcessor Optimization  
- Create `MediaDataExtractor` module
- Extract media block parsing from `RenderCard` to Parse phase
- Implement caching for extracted media data

### Phase 3: Validation
- Verify performance improvements
- Ensure consistent behavior across all processors
- Validate proper separation of concerns

## Success Criteria

1. **Performance**: Eliminate redundant file I/O operations
2. **Consistency**: All processors follow the same data extraction pattern
3. **Maintainability**: Clear separation between parsing and rendering phases
4. **Functionality**: No regression in output or behavior

## Benefits

- **Improved Performance**: Expensive operations performed once during parsing
- **Better Architecture**: Clear separation of concerns between Parse and Render methods
- **Consistency**: Uniform pattern across all content processors
- **Maintainability**: Easier to debug and modify data extraction logic