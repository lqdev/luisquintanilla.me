# ActivityPub Account Migration: toot.lqdev.tech → lqdev.me

**Created**: January 23, 2026  
**Status**: 📋 Planning Phase  
**Branch**: `project/activitypub-account-migration`  
**Priority**: High

---

## 🎯 Project Overview

### Objective
Migrate the Mastodon account `@lqdev@toot.lqdev.tech` to the self-hosted ActivityPub implementation at `@lqdev@lqdev.me`, ensuring:
1. Followers automatically transfer to the new account
2. The migration is properly recognized across the Fediverse
3. The old account correctly redirects to the new one
4. Full compliance with Mastodon's migration validation requirements

### Current State
- **Source Account**: `https://toot.lqdev.tech/@lqdev` (Mastodon instance)
- **Target Account**: `https://lqdev.me/api/activitypub/actor` (Custom F# + Azure Functions)
- **Current ActivityPub Status**: Phases 1-4 Complete (Discovery, Follow/Accept, Outbox, Delivery)

---

## 📚 Research Summary: Mastodon Account Migration

### How Mastodon Migration Works

#### The Migration Process (High-Level)
1. **Alias Creation on Target**: User creates an "account alias" on the NEW account pointing to the OLD account
2. **Validation**: Mastodon fetches the new account's actor and verifies `alsoKnownAs` contains the old account URI
3. **Migration Initiation**: User initiates migration from the OLD Mastodon account
4. **Move Activity**: Old account sends a `Move` activity to all followers
5. **Follower Re-follow**: Followers' instances automatically unfollow old → follow new
6. **Account Marking**: Old account gets `movedTo` property set, becomes read-only

#### The Move Activity Structure
```json
{
  "@context": "https://www.w3.org/ns/activitystreams",
  "id": "https://toot.lqdev.tech/users/lqdev#moves/1",
  "actor": "https://toot.lqdev.tech/users/lqdev",
  "type": "Move",
  "object": "https://toot.lqdev.tech/users/lqdev",
  "target": "https://lqdev.me/api/activitypub/actor",
  "to": "https://toot.lqdev.tech/users/lqdev/followers"
}
```

#### Critical Properties for Migration

1. **`alsoKnownAs`** (on NEW/target account)
   - Points BACKWARD to the old account URI
   - Must be a JSON-LD array of URIs
   - Mastodon validates this before allowing migration

2. **`movedTo`** (on OLD/source account - Mastodon handles this)
   - Points FORWARD to the new account URI
   - Automatically set by Mastodon when migration completes
   - Signals to federation that account has moved

#### Validation Steps Mastodon Performs
1. Fetch target actor via HTTP GET with `Accept: application/activity+json`
2. Verify `alsoKnownAs` array contains the old account's exact URI
3. Verify HTTP signature on any activities
4. Check actor type is compatible (Person)
5. Verify WebFinger resolves correctly

---

## 🔍 Gap Analysis: Current Implementation vs. Migration Requirements

### ✅ Already Implemented (No Changes Needed)

| Requirement | Current State | Notes |
|-------------|---------------|-------|
| Actor endpoint | ✅ `/api/activitypub/actor` | Returns valid Person actor |
| WebFinger | ✅ `/.well-known/webfinger` | Resolves `acct:lqdev@lqdev.me` |
| Inbox endpoint | ✅ `/api/activitypub/inbox` | Accepts POST activities |
| Followers collection | ✅ `/api/activitypub/followers` | Dynamic from Table Storage |
| Following collection | ✅ `/api/activitypub/following` | Static JSON file |
| Outbox collection | ✅ `/api/activitypub/outbox` | 1,547+ items |
| Public key | ✅ In actor.json | RSA key for signatures |
| HTTP signatures | ✅ Key Vault integration | Can sign outgoing activities |
| Follow/Accept workflow | ✅ Working | Table Storage + queued delivery |
| Actor type | ✅ `"type": "Person"` | Correct for user accounts |

### ❌ Missing (Must Implement)

| Requirement | Gap | Priority | Complexity |
|-------------|-----|----------|------------|
| **`alsoKnownAs` property** | Not present in actor.json | 🔴 Critical | Low |
| **Move activity handler** | Inbox doesn't process Move | 🟡 Medium | Medium |
| **Mastodon namespace in context** | Only has AS and security | 🟡 Medium | Low |
| **Migration collection support** | For future FEP-7628 | 🟢 Nice-to-have | Medium |

### ⚠️ Needs Verification/Enhancement

| Area | Current State | Required Enhancement |
|------|---------------|---------------------|
| **@context array** | Missing Mastodon extensions | Add Mastodon namespace for full compatibility |
| **WebFinger aliases** | Only has actor link | May need profile-page link (already present) |
| **Followers sync** | Basic implementation | May need Collection-Synchronization header |
| **Rate limiting** | None on inbox | Consider adding for Move processing |

---

## 📋 Implementation Plan

### Phase 1: Actor Enhancement (Critical Path)
**Estimated Effort**: 1-2 hours  
**Risk Level**: Low

#### 1.1 Add `alsoKnownAs` Property to Actor
Update `api/data/actor.json` to include the old Mastodon account:

```json
{
  "@context": [
    "https://www.w3.org/ns/activitystreams",
    "https://w3id.org/security/v1",
    {
      "toot": "http://joinmastodon.org/ns#",
      "discoverable": "toot:discoverable",
      "alsoKnownAs": {
        "@id": "as:alsoKnownAs",
        "@type": "@id"
      }
    }
  ],
  "id": "https://lqdev.me/api/activitypub/actor",
  "type": "Person",
  "preferredUsername": "lqdev",
  "alsoKnownAs": [
    "https://toot.lqdev.tech/users/lqdev"
  ],
  // ... rest of actor properties
}
```

**Key Points**:
- `alsoKnownAs` must be an **array** of URIs (even with single entry)
- URI must be the **exact** ActivityPub actor URI from Mastodon (not the profile URL)
- Must be in the `@context` or use full IRI

#### 1.2 Mastodon Actor URI (VERIFIED ✅)
Confirmed via curl on January 23, 2026:
```
Actor ID: https://toot.lqdev.tech/users/lqdev
```

This is the exact URI that must go in `alsoKnownAs`.

### Phase 2: Move Activity Handler (Recommended)
**Estimated Effort**: 2-3 hours  
**Risk Level**: Medium

While not strictly required for the migration to work (Mastodon sends Move to YOUR followers, not to you), implementing Move handling enables:
- Future migrations TO your account from other sources
- Proper logging of migration events
- Fediverse best practices compliance

#### 2.1 Add Move Activity Processing to Inbox

```javascript
// In api/inbox/index.js

if (activityType === 'Move') {
    // Log Move activity for audit
    const originActor = activityData.actor;
    const targetActor = activityData.target;
    
    context.log(`Move activity received: ${originActor} → ${targetActor}`);
    
    // Validate: actor and object should be the same (account moving itself)
    if (activityData.actor !== activityData.object) {
        context.log.warn('Invalid Move: actor !== object');
        context.res = {
            status: 400,
            body: { error: 'Invalid Move activity' }
        };
        return;
    }
    
    // Verify alsoKnownAs relationship (optional but recommended)
    // This would involve fetching target actor and checking alsoKnownAs
    
    // For now, log and accept
    context.res = {
        status: 202,
        headers: { 'Content-Type': 'application/json' },
        body: { message: 'Move activity received' }
    };
    return;
}
```

### Phase 3: Enhanced Context (Optional but Recommended)
**Estimated Effort**: 30 minutes  
**Risk Level**: Low

Add Mastodon-specific extensions for better federation compatibility:

```json
{
  "@context": [
    "https://www.w3.org/ns/activitystreams",
    "https://w3id.org/security/v1",
    {
      "toot": "http://joinmastodon.org/ns#",
      "discoverable": "toot:discoverable",
      "memorial": "toot:memorial",
      "indexable": "toot:indexable",
      "alsoKnownAs": {
        "@id": "as:alsoKnownAs",
        "@type": "@id"
      },
      "movedTo": {
        "@id": "as:movedTo",
        "@type": "@id"
      }
    }
  ]
}
```

---

## 🚀 Migration Execution Plan

### Pre-Migration Checklist

- [ ] **Verify Mastodon actor URI**: `curl -H "Accept: application/activity+json" https://toot.lqdev.tech/users/lqdev`
- [ ] **Deploy Phase 1 changes**: Update actor.json with `alsoKnownAs`
- [ ] **Verify deployment**: `curl -H "Accept: application/activity+json" https://lqdev.me/api/activitypub/actor`
- [ ] **Test WebFinger**: Verify `@lqdev@lqdev.me` resolves correctly
- [ ] **Note current follower count** on toot.lqdev.tech for comparison

### Migration Steps (On Mastodon)

1. **Log into toot.lqdev.tech** as @lqdev

2. **Go to**: Preferences → Account → Moving to a different account

3. **Enter new account handle**: `@lqdev@lqdev.me`

4. **Confirm with password** and click "Move Followers"

5. **Wait**: Migration can take minutes to hours depending on follower count

### Post-Migration Verification

- [ ] Old account shows "moved" notice
- [ ] New account receives Follow activities from old followers
- [ ] Check Table Storage for new follower entries
- [ ] Verify followers can find you via `@lqdev@lqdev.me`

---

## ⚠️ Important Considerations

### What Migrates
- ✅ **Followers** (automatic re-follow via Move activity)
- ✅ **Account identity** (old points to new)

### What Does NOT Migrate
- ❌ **Posts/Toots** - Remain on toot.lqdev.tech permanently
- ❌ **Following list** - Must export CSV and re-follow manually
- ❌ **Blocks/Mutes** - Must be recreated
- ❌ **Bookmarks** - Not transferable
- ❌ **Lists** - Must be recreated

### Cooldown Period
- 30-day cooldown before another migration can be performed
- Cannot migrate back immediately if something goes wrong

### Followers on Non-Supporting Instances
- Some ActivityPub implementations don't support Move
- Those followers will need to manually re-follow

---

## 🧪 Testing Strategy

### Before Migration (Validation)

1. **Actor Validation**
```bash
# Fetch actor and verify alsoKnownAs
curl -s -H "Accept: application/activity+json" \
  "https://lqdev.me/api/activitypub/actor" | jq '.alsoKnownAs'
# Expected: ["https://toot.lqdev.tech/users/lqdev"]
```

2. **WebFinger Test**
```bash
curl -s "https://lqdev.me/.well-known/webfinger?resource=acct:lqdev@lqdev.me" | jq
```

3. **Cross-Instance Discovery**
- Search for `@lqdev@lqdev.me` from another Mastodon instance
- Verify profile loads correctly

### After Migration

1. **Verify Move Received**
- Check inbox activity logs
- Verify new followers in Table Storage

2. **Federation Test**
- Post from new account
- Verify it federates to followers

---

## 📊 Success Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| Migration completion | 100% | Old account shows "moved" |
| Follower transfer rate | >90% | Compare before/after counts |
| Federation continuity | 100% | New posts reach followers |
| Discovery working | 100% | Can be found via @lqdev@lqdev.me |

---

## 🔮 Future Enhancements (Post-Migration)

1. **FEP-7628 Support**: Full Move activity compliance
2. **FEP-1580 Support**: Object migration collections
3. **Multi-Account Support**: Support multiple alsoKnownAs entries
4. **Migration Audit Log**: Track all migration-related activities

---

## 📚 References

- [Mastodon Account Migration Docs](https://docs.joinmastodon.org/user/moving/)
- [Mastodon ActivityPub Spec](https://docs.joinmastodon.org/spec/activitypub/)
- [FEP-7628: Move Actor](https://socialhub.activitypub.rocks/t/fep-7628-move-actor/3583)
- [GoToSocial Migration Docs](https://docs.gotosocial.org/en/latest/user_guide/migration/)
- [ActivityPub Data Portability](https://swicg.github.io/activitypub-data-portability/)

---

## 📝 Implementation Log

### January 23, 2026 - Project Initiation
- Created project plan and gap analysis
- Researched Mastodon migration requirements
- Identified critical path: `alsoKnownAs` property addition
- Branch created: `project/activitypub-account-migration`
