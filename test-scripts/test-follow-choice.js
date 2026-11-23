const assert = require('node:assert/strict');
const {
  buildCandidateIssueBody,
  extractFollowFields,
  parseCandidateChecklist,
  parseCheckedCandidate,
  parseFollowCommand,
  resolveFollowCommand
} = require('../.github/scripts/follow-choice.js');

const issueBody = `### Feed URL

_No response_

### Site URL

https://example.test/

### Title

Example

### Roll

blogroll`;

assert.deepEqual(extractFollowFields(issueBody), {
  feedUrl: '',
  siteUrl: 'https://example.test/',
  title: 'Example',
  roll: 'blogroll'
});

const marker = `<!-- follow-candidates:v1 -->
I found feeds.

- [ ] \`https://example.test/feed.xml\` (candidate 1)
- [x] \`https://example.test/atom.xml\` (candidate 2)`;

assert.deepEqual(parseCandidateChecklist(marker), [
  { checked: false, url: 'https://example.test/feed.xml' },
  { checked: true, url: 'https://example.test/atom.xml' }
]);
assert.deepEqual(parseCheckedCandidate(marker), {
  state: 'selected',
  url: 'https://example.test/atom.xml'
});
assert.deepEqual(parseCheckedCandidate(marker.replace('[x]', '[ ]')), { state: 'none' });
assert.deepEqual(
  parseCheckedCandidate(marker.replace('- [ ]', '- [x]')),
  { state: 'invalid', count: 2 }
);
assert.equal(parseFollowCommand('/follow 2'), '2');
assert.equal(
  parseFollowCommand('/follow https://example.test/feed.xml'),
  'https://example.test/feed.xml'
);
assert.equal(parseFollowCommand('/follow two'), null);
assert.equal(parseFollowCommand('please /follow 1'), null);
assert.deepEqual(resolveFollowCommand('2', [marker]), {
  state: 'selected',
  url: 'https://example.test/atom.xml'
});
assert.deepEqual(
  resolveFollowCommand('3', [marker]),
  { state: 'invalid', reason: 'Candidate 3 is not available.' }
);
assert.deepEqual(resolveFollowCommand('1', []), {
  state: 'invalid',
  reason: 'No feed candidate checklist is available in the issue description.'
});
const issueWithChecklist = buildCandidateIssueBody(issueBody, [
  'https://example.test/feed.xml',
  'https://example.test/atom.xml'
]);
assert.ok(issueWithChecklist.startsWith(issueBody));
assert.deepEqual(extractFollowFields(issueWithChecklist), extractFollowFields(issueBody));
assert.deepEqual(parseCandidateChecklist(issueWithChecklist).map((candidate) => candidate.url), [
  'https://example.test/feed.xml',
  'https://example.test/atom.xml'
]);
assert.match(issueWithChecklist, /<!-- follow-candidates:v1 -->/);
assert.deepEqual(parseCheckedCandidate(issueWithChecklist), { state: 'none' });
const refreshedChecklist = buildCandidateIssueBody(issueWithChecklist, [
  'https://example.test/replacement.xml'
]);
assert.equal((refreshedChecklist.match(/<!-- follow-candidates:v1 -->/g) || []).length, 1);
assert.deepEqual(extractFollowFields(refreshedChecklist), extractFollowFields(issueBody));
assert.deepEqual(parseCandidateChecklist(refreshedChecklist), [{
  checked: false,
  url: 'https://example.test/replacement.xml'
}]);
const oneChecked = issueWithChecklist.replace('[ ] `https://example.test/feed.xml`', '[x] `https://example.test/feed.xml`');
assert.deepEqual(parseCheckedCandidate(oneChecked), {
  state: 'selected',
  url: 'https://example.test/feed.xml'
});
const severalChecked = oneChecked.replace('[ ] `https://example.test/atom.xml`', '[x] `https://example.test/atom.xml`');
assert.deepEqual(parseCheckedCandidate(severalChecked), { state: 'invalid', count: 2 });

console.log('Follow choice parser checks passed.');
