# Phase 4D: Shared Inbox Optimization - Research Summary

**Date**: January 31, 2026  
**Issue**: #2038  
**Status**: Research Complete

---

## Executive Summary

This document captures the research findings for implementing ActivityPub shared inbox optimization. The shared inbox mechanism allows batching one POST per instance rather than one POST per follower, dramatically reducing outgoing HTTP requests for activity delivery.

## Key Findings from ActivityPub Specification & Major Implementations

### 1. Actor Profile Structure for SharedInbox

The `endpoints` object in an ActivityPub actor profile contains the `sharedInbox` endpoint:

```json
{
  "@context": [
    "https://www.w3.org/ns/activitystreams",
    "https://w3id.org/security/v1"
  ],
  "id": "https://example.org/users/alice",
  "type": "Person",
  "preferredUsername": "alice",
  "name": "Alice",
  "inbox": "https://example.org/users/alice/inbox",
  "outbox": "https://example.org/users/alice/outbox",
  "followers": "https://example.org/users/alice/followers",
  "following": "https://example.org/users/alice/following",
  "endpoints": {
    "sharedInbox": "https://example.org/inbox"
  }
}
```

**Key Points:**
- The `endpoints` object is **optional** in the ActivityPub spec
- The `sharedInbox` property is a string containing an absolute HTTPS URI
- Mastodon uses domain-wide endpoints like `https://mastodon.example/inbox`
- Pixelfed uses patterns like `https://example.org/f/inbox`

### 2. How Shared Inbox Delivery Works

**Sender Side:**
1. Group followers by their `endpoints.sharedInbox` (or fall back to personal `inbox`)
2. Send one POST request per unique inbox/sharedInbox URL
3. Include `to` or `cc` addressing referencing the followers collection

**Receiver Side:**
1. Receive activity at shared inbox endpoint
2. Examine addressing fields (`to`, `cc`, `bto`, `bcc`, `audience`)
3. Identify which local users follow the activity's actor
4. Deliver the activity to each identified local follower's timeline

**Delegation Model**: The receiving server determines final recipients, not the sender. This is why we include `https://lqdev.me/api/activitypub/actor/followers` in the addressing.

### 3. Edge Cases to Handle

#### Actors Without SharedInbox
- The `endpoints` object might be completely absent
- The `sharedInbox` property might be `null` or empty string
- **Solution**: Fall back to personal `inbox` delivery

#### Implementation Logic:
```javascript
const sharedInbox = actorProfile.endpoints?.sharedInbox || null;
const targetInbox = sharedInbox || actorProfile.inbox;
```

#### Follower Collection Synchronization
- Mastodon implements `Collection-Synchronization` header (optional, not required for MVP)
- XOR-based digest of follower IDs for quick sync validation

#### Dead Instances
- Already handled with exponential backoff in current implementation
- Continue with existing retry logic

### 4. Delivery Status Tracking

**Recommendation**: Track delivery status **per inbox URL** (not per follower).

**Rationale:**
- One POST to shared inbox delivers to all followers on that instance
- If POST succeeds, all followers on that instance should be marked as delivered
- If POST fails, all followers on that instance are affected equally

**Implementation:**
```javascript
// Group followers by target inbox
const inboxGroups = new Map();
for (const follower of followers) {
    const targetInbox = follower.sharedInbox || follower.inbox;
    if (!inboxGroups.has(targetInbox)) {
        inboxGroups.set(targetInbox, []);
    }
    inboxGroups.get(targetInbox).push(follower.actorUrl);
}

// Queue one task per inbox, tracking all covered actors
for (const [inboxUrl, actors] of inboxGroups.entries()) {
    queueTask({ 
        inboxUrl, 
        coveredActors: actors,  // All followers delivered via this inbox
        activityId, 
        activityJson 
    });
}
```

### 5. Best Practices from Major Implementations

#### Mastodon
- Extracts `sharedInbox` from actor profiles when available
- Groups followers by server and sends one activity to each server's shared inbox
- Includes followers collection reference in `to`/`cc` fields
- Implements optional `Collection-Synchronization` header

#### Pixelfed, PeerTube, GoToSocial
- All follow the same shared inbox pattern
- Shared inbox URIs vary by implementation but behavior is consistent

### 6. Idempotency Strategy

**Recommendation**: Use per-inbox idempotency.

The same activity ID should be processed once per inbox endpoint. This prevents:
- Duplicate delivery if retry happens
- Wasted processing when same activity arrives via different paths

**Cache Key Format:**
```
{origin}|{activity_id}|{inbox_endpoint}
```

---

## Proposal Validation & Corrections

### Original Issue Analysis

The issue proposal (#2038) is **architecturally sound**. Minor clarifications:

1. **Field Naming**: Use `sharedInbox` (camelCase) to match ActivityPub spec exactly
   - Proposal uses `SharedInbox` in some places (OK for Table Storage properties)
   - JavaScript should use `sharedInbox` when extracting from actor profiles

2. **Fallback Logic**: The proposal correctly identifies fallback to personal inbox when sharedInbox is not present

3. **Grouping Logic**: The grouping approach in QueueDeliveryTasks is correct

4. **Migration Script**: Approach is sound - fetch actor profiles, extract endpoints.sharedInbox

### Implementation Order (Validated)

1. ✅ **Table Storage Schema Update** - Add `sharedInbox` field to follower entities
2. ✅ **Inbox Handler Update** - Extract and store sharedInbox on new follows
3. ✅ **Migration Script** - Backfill existing followers
4. ✅ **QueueDeliveryTasks Update** - Group by shared inbox
5. ✅ **ProcessDelivery Update** - Track delivery for grouped followers

---

## Technical References

- [W3C ActivityPub Spec - Shared Inbox](https://www.w3.org/TR/activitypub/#shared-inbox)
- [Mastodon ActivityPub Implementation](https://docs.joinmastodon.org/spec/activitypub/)
- [FEP-8fcf: Followers Collection Synchronization](https://socialhub.activitypub.rocks/t/fep-8fcf-followers-collection-synchronization-across-servers/1172)
- [Guide for New ActivityPub Implementers](https://socialhub.activitypub.rocks/t/guide-for-new-activitypub-implementers/479)

---

## Files to Modify

| File | Change Type | Description |
|------|-------------|-------------|
| `api/utils/tableStorage.js` | Modify | Add `sharedInbox` field to follower entity schema |
| `api/inbox/index.js` | Modify | Extract `endpoints.sharedInbox` when processing Follow |
| `api/QueueDeliveryTasks/index.js` | Modify | Group followers by shared inbox before queuing |
| `api/scripts/backfill-shared-inbox.js` | Create | Migration script for existing followers |
| `docs/activitypub/ARCHITECTURE-OVERVIEW.md` | Update | Document shared inbox optimization |

---

## Success Criteria

1. All followers in Table Storage have `sharedInbox` field populated (if their instance supports it)
2. Delivery groups followers by shared inbox, reducing POST requests
3. Delivery status correctly tracks success/failure for all affected followers
4. Zero regression in existing functionality
5. Application Insights shows reduction in HTTP requests per delivery cycle
