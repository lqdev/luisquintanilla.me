# Feature Flag Pattern for Phase 2 Content Migrations

Based on `website-upgrade.md` specification for gradual content type migration.

## Pattern Overview

The feature flag pattern enables safe migration of content types from the existing string-based system to the new AST-based infrastructure without breaking existing functionality.

## Implementation Pattern

### 1. Keep Existing Function (Unchanged)
```fsharp
let buildSnippetPages(snippets) = 
    // Existing snippet processing (unchanged)
    ()
```

### 2. Add New Function Using New Infrastructure
```fsharp
let buildSnippetsNew() = 
    let snippetData = GenericBuilder.buildContentWithFeeds 
        createSnippetProcessor 
        renderSnippetCard 
        renderSnippetRssItem 
        "snippets" 
        (Path.Join(srcDir, "snippets"))
    snippetData
```

### 3. Feature Flag in Program.fs
```fsharp
let useNewSnippets = Environment.GetEnvironmentVariable("NEW_SNIPPETS") = "true"
```

### 4. Conditional Execution
```fsharp
if useNewSnippets then 
    let _ = buildSnippetsNew()
    ()
else 
    buildSnippetPages(snippets)
```

## Environment Variables for Content Types

- `NEW_SNIPPETS=true` - Use new snippet processing
- `NEW_POSTS=true` - Use new post processing  
- `NEW_WIKI=true` - Use new wiki processing
- `NEW_PRESENTATIONS=true` - Use new presentation processing

## Benefits

1. **Parallel Development**: Both systems coexist safely
2. **Gradual Migration**: Migrate content types individually  
3. **Easy Rollback**: Set environment variable to false
4. **Output Validation**: Can compare old vs new output
5. **No Breaking Changes**: Existing functionality preserved

## Phase 2 Usage Workflow

1. **Implement**: Create new content processor for target type using `GenericBuilder.buildContentWithFeeds`
2. **Integrate**: Add feature flag check in `Program.fs`
3. **Test**: Enable with `NEW_[TYPE]=true` environment variable
4. **Validate**: Compare output with existing system
5. **Switch**: Change default when confident in new system
6. **Cleanup**: Remove old system when fully migrated

## Reference

See `website-upgrade.md` for complete migration strategy and examples.
