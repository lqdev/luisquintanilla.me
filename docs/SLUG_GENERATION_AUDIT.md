# Slug Generation Audit Report

## Executive Summary

This audit examines slug generation logic across all post types in the GitHub issue-based publishing workflow. The goal is to standardize slug generation so that when users leave the optional `slug` field blank, all post types consistently append the date (YYYY-MM-DD format) to avoid filename conflicts.

## Current State Analysis

### Post Types and Their Slug Generation Logic

#### ✅ **Note Posts** (CONSISTENT - HAS DATE APPENDING)
- **Script**: `Scripts/process-github-issue.fsx`
- **Lines**: 94-97
- **Current Logic**:
  ```fsharp
  let filename = 
      match customSlug with
      | Some _ -> sprintf "%s.md" finalSlug  // Use slug as-is when custom slug provided
      | None -> sprintf "%s-%s.md" finalSlug (now.ToString("yyyy-MM-dd"))  // Append date when no custom slug
  ```
- **Status**: ✅ **CORRECT** - Appends date when no custom slug provided

#### ❌ **Bookmark Posts** (INCONSISTENT - NO DATE APPENDING)
- **Script**: `Scripts/process-bookmark-issue.fsx`
- **Lines**: 98-101
- **Current Logic**:
  ```fsharp
  let filename = 
      match customSlug with
      | Some _ -> sprintf "%s.md" finalSlug  // Use slug as-is when custom slug provided
      | None -> sprintf "%s.md" finalSlug  // For bookmarks, don't append date to match existing pattern
  ```
- **Status**: ❌ **INCONSISTENT** - Does NOT append date, even with comment noting it doesn't match pattern

#### ❌ **Media Posts** (INCONSISTENT - NO DATE APPENDING)
- **Script**: `Scripts/process-media-issue.fsx`
- **Lines**: 92
- **Current Logic**:
  ```fsharp
  let filename = sprintf "%s.md" finalSlug
  ```
- **Status**: ❌ **INCONSISTENT** - Never appends date, no custom slug check

#### ❌ **Playlist Posts** (INCONSISTENT - NO DATE APPENDING)
- **Script**: `Scripts/process-playlist-issue.fsx`
- **Lines**: 160
- **Current Logic**:
  ```fsharp
  let filename = sprintf "%s.md" finalSlug
  ```
- **Status**: ❌ **INCONSISTENT** - Never appends date, no custom slug check

#### ✅ **Response Posts** (CONSISTENT - HAS DATE APPENDING)
- **Script**: `Scripts/process-response-issue.fsx`
- **Lines**: 169-172
- **Current Logic**:
  ```fsharp
  let filename = 
      match customSlug with
      | Some _ -> sprintf "%s.md" finalSlug  // Use slug as-is when custom slug provided
      | None -> sprintf "%s-%s.md" finalSlug (now.ToString("yyyy-MM-dd"))  // Append date when no custom slug
  ```
- **Status**: ✅ **CORRECT** - Appends date when no custom slug provided

#### ⚠️ **Review Posts** (MIXED - HAS DATE BUT ALWAYS APPENDED)
- **Script**: `Scripts/process-review-issue.fsx`
- **Lines**: 75-83
- **Current Logic**:
  ```fsharp
  let generateSlug (title: string) =
      let baseSlug = sanitizeSlug title
      let timestamp = DateTimeOffset.Now.ToString("yyyy-MM-dd")
      sprintf "%s-%s" baseSlug timestamp
  
  let slug = 
      match customSlug with
      | Some s when not (String.IsNullOrWhiteSpace(s)) -> sanitizeSlug s
      | _ -> generateSlug itemName
  ```
- **Status**: ⚠️ **PARTIALLY CORRECT** - Appends date when auto-generating, but NOT when custom slug provided
- **Issue**: Custom slugs should NOT get date appended (already correct), but the pattern differs from other scripts

## Inconsistencies Summary

| Post Type | Script | Date Appended? | Custom Slug Handling | Status |
|-----------|--------|----------------|---------------------|---------|
| **note** | process-github-issue.fsx | ✅ Yes (auto-generated only) | ✅ No date for custom | ✅ CORRECT |
| **bookmark** | process-bookmark-issue.fsx | ❌ No | ✅ No date for custom | ❌ NEEDS FIX |
| **media** | process-media-issue.fsx | ❌ No | ❌ No custom slug handling | ❌ NEEDS FIX |
| **playlist** | process-playlist-issue.fsx | ❌ No | ❌ No custom slug handling | ❌ NEEDS FIX |
| **response** | process-response-issue.fsx | ✅ Yes (auto-generated only) | ✅ No date for custom | ✅ CORRECT |
| **review** | process-review-issue.fsx | ✅ Yes (always in helper) | ✅ No date for custom | ⚠️ DIFFERENT PATTERN |

## Required Changes

### Scripts Requiring Updates

1. **`Scripts/process-bookmark-issue.fsx`** (lines 98-101)
   - Change from: `| None -> sprintf "%s.md" finalSlug`
   - Change to: `| None -> sprintf "%s-%s.md" finalSlug (now.ToString("yyyy-MM-dd"))`

2. **`Scripts/process-media-issue.fsx`** (lines 92)
   - Add custom slug handling
   - Implement date appending for auto-generated slugs
   
3. **`Scripts/process-playlist-issue.fsx`** (lines 160)
   - Add custom slug handling
   - Implement date appending for auto-generated slugs

### Standard Pattern

All scripts should follow this pattern:

```fsharp
// Generate filename - only append date when no custom slug provided
let filename = 
    match customSlug with
    | Some _ -> sprintf "%s.md" finalSlug  // Use slug as-is when custom slug provided
    | None -> sprintf "%s-%s.md" finalSlug (now.ToString("yyyy-MM-dd"))  // Append date when no custom slug
```

## Benefits of Standardization

1. **Consistency**: All post types follow the same naming convention
2. **Uniqueness**: Date-stamped auto-generated filenames prevent conflicts
3. **Predictability**: Contributors know exactly what filename will be generated
4. **Flexibility**: Custom slugs still work without date suffixes when intentionally provided

## Implementation Status

### Changes Made

1. ✅ **`Scripts/process-bookmark-issue.fsx`** (line 101)
   - ✅ Updated to append date when no custom slug provided
   - ✅ Now matches standard pattern

2. ✅ **`Scripts/process-media-issue.fsx`** (lines 91-95)
   - ✅ Added custom slug handling
   - ✅ Implemented date appending for auto-generated slugs
   - ✅ Now matches standard pattern

3. ✅ **`Scripts/process-playlist-issue.fsx`** (lines 159-163)
   - ✅ Added custom slug handling
   - ✅ Implemented date appending for auto-generated slugs
   - ✅ Now matches standard pattern

### Verification

All scripts now follow the standardized pattern:

```fsharp
// Generate filename - only append date when no custom slug provided
let filename = 
    match customSlug with
    | Some _ -> sprintf "%s.md" finalSlug  // Use slug as-is when custom slug provided
    | None -> sprintf "%s-%s.md" finalSlug (now.ToString("yyyy-MM-dd"))  // Append date when no custom slug
```

**Review Script Note**: The review script (`process-review-issue.fsx`) uses a helper function approach but achieves the same result:
- Custom slugs: No date appended (sanitized only)
- Auto-generated: Date appended via `generateSlug` function
- This is functionally equivalent and doesn't require changes

## Final Status

| Post Type | Script | Date Appended? | Custom Slug Handling | Status |
|-----------|--------|----------------|---------------------|---------|
| **note** | process-github-issue.fsx | ✅ Yes (auto-generated only) | ✅ No date for custom | ✅ CORRECT |
| **bookmark** | process-bookmark-issue.fsx | ✅ Yes (auto-generated only) | ✅ No date for custom | ✅ **FIXED** |
| **media** | process-media-issue.fsx | ✅ Yes (auto-generated only) | ✅ No date for custom | ✅ **FIXED** |
| **playlist** | process-playlist-issue.fsx | ✅ Yes (auto-generated only) | ✅ No date for custom | ✅ **FIXED** |
| **response** | process-response-issue.fsx | ✅ Yes (auto-generated only) | ✅ No date for custom | ✅ CORRECT |
| **review** | process-review-issue.fsx | ✅ Yes (auto-generated only) | ✅ No date for custom | ✅ CORRECT |

## Next Steps

1. ✅ Complete audit (this document)
2. ✅ Update processing scripts with standardized logic
3. ✅ Add documentation for contributors (SLUG_GENERATION_GUIDE.md)
4. ⏳ Test changes with sample workflows
5. ⏳ Code review and security check
