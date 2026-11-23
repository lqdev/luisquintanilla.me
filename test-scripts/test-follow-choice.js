const assert = require('node:assert/strict');
const {
  buildCandidateComment,
  extractFollowFields,
  parseCandidateComment,
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

assert.deepEqual(parseCandidateComment(marker), [
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
  reason: 'No feed candidate comment is available.'
});
const formatted = buildCandidateComment([
  'https://example.test/feed.xml',
  'https://example.test/atom.xml'
]);
assert.deepEqual(parseCandidateComment(formatted).map((candidate) => candidate.url), [
  'https://example.test/feed.xml',
  'https://example.test/atom.xml'
]);
assert.match(formatted, /<!-- follow-candidates:v1 -->/);

console.log('Follow choice parser checks passed.');
