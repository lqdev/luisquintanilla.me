# Audit Report Format

When presenting audit results to the user, use this structure:

## Summary Line
```
Memex Health: X entries | Y stale | Z drifted | W broken refs | V orphans
```

## Freshness Table
```
| Entry | Type | Last Updated | Age (days) | Status |
|-------|------|-------------|------------|--------|
| pattern-xyz | pattern | 2025-01-15 | 120 | ⚠️ STALE |
| research-abc | research | 2025-04-01 | 45 | ✅ Fresh |
```

## Spoke-Hub Drift
```
| Entry | Spoke Date | Hub Date | Source |
|-------|-----------|----------|--------|
| pattern-foo | 2025-05-01 | 2025-03-15 | copilot-sdk-elisp |
```

## Dependency Impact (for updates)
When a hub entry is about to be updated, show its impact:
```
Updating: pattern-generic-builder-content-processor (11 inbound refs)

Direct dependents:
  - pattern-custom-block-infrastructure
  - pattern-feed-architecture-consistency
  - reference-codebase-context
  ... (8 more)

Recommend: Review dependents after updating. Run audit again post-update.
```

## Action Recommendations
Prioritize by impact:
1. Fix broken references (data integrity)
2. Resolve spoke-hub drift (run import)
3. Review critical hubs before updating (propagation risk)
4. Consider updating stale project-reports (likely outdated)
5. Orphans are low priority (may gain connections after next build)
