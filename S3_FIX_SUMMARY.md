# S3 Media Upload Fix - Implementation Summary

## Problem
Media uploads to Linode Object Storage were failing with error:
```
Connection was closed before we received a valid response from endpoint URL
```

## Root Cause
The boto3 S3 client configuration was missing critical timeout and retry settings, causing premature connection closure when uploading files to Linode Object Storage.

## Solution Applied

### 1. Enhanced boto3 Configuration
Added the following settings to the boto3 Config in `upload_media.py`:

```python
s3_config = Config(
    signature_version='s3v4',
    s3={'addressing_style': 'virtual'},
    connect_timeout=60,          # NEW: Prevents connection timeout
    read_timeout=60,             # NEW: Prevents timeout during large uploads
    retries={                    # NEW: Handles transient failures
        'max_attempts': 3,
        'mode': 'standard'
    }
)
```

#### Configuration Explanation:
- **signature_version='s3v4'**: Required for S3-compatible storage (already present)
- **addressing_style='virtual'**: Virtual-hosted style URLs for Linode (already present)
- **connect_timeout=60**: Gives 60 seconds to establish connection (prevents premature closure)
- **read_timeout=60**: Gives 60 seconds between read operations (handles large files)
- **retries**: Automatically retries failed requests up to 3 times with standard backoff

### 2. Created Testing Tools

#### A. Test S3 Connection Script (`test_s3_connection.py`)
- Safely tests S3 connectivity without exposing credentials in logs
- Tests multiple configuration options
- Performs actual upload/delete operations
- Provides clear diagnostic output

**Features:**
- ✅ Masks credentials in output
- ✅ Tests connection with current configuration
- ✅ Tests connection with original configuration
- ✅ Tests path-style addressing as fallback
- ✅ Performs real upload and delete operations
- ✅ Verbose mode for debugging (--verbose flag)

#### B. Manual Test Workflow (`test-s3-connection.yml`)
- Can be triggered manually from GitHub Actions
- Uses same credentials as media upload workflow
- Provides clear pass/fail status
- Optional verbose mode for detailed troubleshooting

**To run:**
1. Go to Actions tab in GitHub
2. Select "Test S3 Connection" workflow
3. Click "Run workflow"
4. Optionally enable verbose output
5. Review results

### 3. Updated Documentation
- Enhanced README with timeout/retry configuration details
- Improved troubleshooting section with specific solutions
- Added configuration explanation for clarity

## Testing Instructions

### Phase 1: Test S3 Connectivity (Manual)
1. Navigate to GitHub repository
2. Go to "Actions" tab
3. Select "Test S3 Connection" workflow from left sidebar
4. Click "Run workflow" button (top right)
5. Click green "Run workflow" button in dropdown
6. Wait for workflow to complete (30-60 seconds)
7. Review results:
   - ✅ Green checkmark = S3 connection works
   - ❌ Red X = S3 connection failed (check logs)

### Phase 2: Test Media Upload (Real)
1. Navigate to "Issues" tab in GitHub repository
2. Click "New issue"
3. Select "📷 Post Media" template
4. Fill in:
   - **Title**: "Test S3 Fix"
   - **Content**: Drag and drop a small image (< 1MB)
   - **Tags**: "test"
5. Click "Submit new issue"
6. Watch GitHub Actions run (should complete in 1-2 minutes)
7. Verify:
   - ✅ Issue is auto-closed
   - ✅ PR is created
   - ✅ No S3 connection errors in logs
   - ✅ File uploaded to S3 successfully

### Phase 3: Verify Upload Results
1. Check PR created by workflow
2. Review markdown file created in `_src/media/`
3. Verify :::media blocks contain permanent CDN URLs
4. Merge PR if everything looks good

## Expected Outcomes

### Before Fix
```
❌ Error processing attachment: Connection was closed before we received 
   a valid response from endpoint URL: "***/***/files/images/..."
```

### After Fix
```
✅ Uploaded successfully
🔗 Permanent URL: https://cdn.luisquintanilla.me/files/images/20251026_211117_...
✅ Media upload and transformation complete!
```

## Additional Considerations

### If Test Still Fails
Check these potential issues:

1. **Credentials**: Verify all secrets are set correctly in GitHub repository settings
   - LINODE_STORAGE_ACCESS_KEY_ID
   - LINODE_STORAGE_SECRET_ACCESS_KEY
   - LINODE_STORAGE_ENDPOINT_URL
   - LINODE_STORAGE_BUCKET_NAME
   - LINODE_STORAGE_CUSTOM_DOMAIN (optional)

2. **Endpoint URL Format**: Must be `https://REGION.linodeobjects.com`
   - Example: `https://us-east-1.linodeobjects.com`
   - No trailing slash
   - HTTPS (not HTTP)

3. **Bucket Permissions**: Bucket must allow:
   - ListBucket
   - GetObject
   - PutObject with public-read ACL

4. **Clock Skew**: GitHub Actions runner time must be within 15 minutes of S3 server time
   - This should not be an issue with GitHub-hosted runners
   - Could be an issue with self-hosted runners

5. **Network Restrictions**: Ensure GitHub Actions runner can reach Linode Object Storage
   - GitHub-hosted runners should have no restrictions
   - Self-hosted runners may need firewall rules

## Files Changed

### Modified Files:
1. `.github/scripts/upload_media.py`
   - Added timeout and retry configuration to boto3 Config

2. `.github/scripts/README.md`
   - Enhanced documentation with new configuration details
   - Improved troubleshooting section

### New Files:
1. `.github/scripts/test_s3_connection.py`
   - Test script for S3 connectivity

2. `.github/workflows/test-s3-connection.yml`
   - Manual workflow for testing S3 connection

## Success Criteria
- ✅ Timeout and retry configuration added
- ✅ Test script created
- ✅ Test workflow created
- ✅ Documentation updated
- ⏳ S3 connection test passes (user to verify)
- ⏳ Media upload completes successfully (user to verify)

## Next Steps
1. **User Action Required**: Run "Test S3 Connection" workflow to verify credentials work
2. If test passes: Try real media upload via issue form
3. If test fails: Check credentials and endpoint URL format
4. Report results back to Copilot for further debugging if needed
