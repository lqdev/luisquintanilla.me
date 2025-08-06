# Migration Guide: Snippets

## Prerequisites
- Core Infrastructure Phase 1 complete
- Feature Flag Infrastructure implemented
- New processor implementation ready

## Migration Steps

### 1. Enable Feature Flag
```bash
export NEW_SNIPPETS=true
```

### 2. Run Parallel Validation
```bash
dotnet run
```

### 3. Validate Output
Check `_public/snippets` for correct generation

### 4. Compare Results
Use OutputComparison module to validate identical output

### 5. Production Deployment
Set NEW_SNIPPETS=true in production environment

## Rollback Procedure
Set NEW_SNIPPETS=false to revert to old processor

## Troubleshooting
- Check feature flag validation messages
- Review output comparison differences
- Verify new processor implementation
