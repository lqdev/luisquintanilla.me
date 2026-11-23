const FOLLOW_CANDIDATES_MARKER = '<!-- follow-candidates:v1 -->';
const FOLLOW_CANDIDATES_END = '<!-- follow-candidates:end -->';

function extractFormValue(body, label) {
  const escapedLabel = label.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
  const match = String(body || '').match(
    new RegExp(`### (?:${escapedLabel})\\s*\\n\\s*\\n([\\s\\S]*?)(?=\\n\\n###|\\n\\n---|\\n\\n${FOLLOW_CANDIDATES_MARKER}|$)`, 'i')
  );
  const value = match && match[1] ? match[1].trim() : '';
  return value.replace(/^_No response_$/i, '').trim();
}

function extractFollowFields(body) {
  return {
    feedUrl: extractFormValue(body, 'Feed URL'),
    siteUrl: extractFormValue(body, 'Site URL'),
    title: extractFormValue(body, 'Title'),
    roll: extractFormValue(body, 'Roll').toLowerCase()
  };
}

function parseCandidateChecklist(body) {
  const text = String(body || '');
  const start = text.indexOf(FOLLOW_CANDIDATES_MARKER);
  if (start === -1) {
    return [];
  }
  const end = text.indexOf(FOLLOW_CANDIDATES_END, start);
  const block = end === -1 ? text.slice(start) : text.slice(start, end + FOLLOW_CANDIDATES_END.length);

  return [...block.matchAll(/^\s*-\s+\[([ xX])\]\s+`([^`]+)`(?:\s|$)/gm)].map((match) => ({
    checked: match[1].toLowerCase() === 'x',
    url: match[2].trim()
  }));
}

function parseCheckedCandidate(body) {
  const candidates = parseCandidateChecklist(body);
  const checked = candidates.filter((candidate) => candidate.checked);
  if (checked.length === 0) {
    return { state: 'none' };
  }
  if (checked.length !== 1) {
    return { state: 'invalid', count: checked.length };
  }
  return { state: 'selected', url: checked[0].url };
}

function parseFollowCommand(body) {
  const match = String(body || '').match(/^\s*\/follow\s+(\d+|https?:\/\/\S+)\s*$/i);
  return match ? match[1] : null;
}

function resolveFollowCommand(command, markedBodies) {
  if (!command) {
    return { state: 'none' };
  }
  if (/^https?:\/\//i.test(command)) {
    return { state: 'selected', url: command };
  }

  const index = Number(command);
  for (let bodyIndex = markedBodies.length - 1; bodyIndex >= 0; bodyIndex -= 1) {
    const candidates = parseCandidateChecklist(markedBodies[bodyIndex]);
    if (candidates.length === 0) {
      continue;
    }
    if (!Number.isSafeInteger(index) || index < 1 || index > candidates.length) {
      return { state: 'invalid', reason: `Candidate ${command} is not available.` };
    }
    return { state: 'selected', url: candidates[index - 1].url };
  }
  return { state: 'invalid', reason: 'No feed candidate checklist is available in the issue description.' };
}

function buildCandidateIssueBody(issueBody, urls) {
  const checklist = urls
    .map((url, index) => `- [ ] \`${url}\` (candidate ${index + 1})`)
    .join('\n');
  const block = [
    '---',
    '',
    FOLLOW_CANDIDATES_MARKER,
    '### Choose a feed',
    '',
    'Select **exactly one** candidate:',
    '',
    checklist,
    '',
    'Then update this issue description. As a fallback, comment with `/follow N` or `/follow <url>`.',
    FOLLOW_CANDIDATES_END
  ].join('\n');

  const text = String(issueBody || '').trimEnd();
  const start = text.indexOf(FOLLOW_CANDIDATES_MARKER);
  const base =
    start === -1
      ? text
      : (() => {
          const end = text.indexOf(FOLLOW_CANDIDATES_END, start);
          const prefix = text.slice(0, start).replace(/\n\n---\s*$/, '');
          return (end === -1 ? prefix : prefix + text.slice(end + FOLLOW_CANDIDATES_END.length)).trimEnd();
        })();
  return `${base}\n\n${block}`;
}

module.exports = {
  FOLLOW_CANDIDATES_END,
  FOLLOW_CANDIDATES_MARKER,
  buildCandidateIssueBody,
  extractFormValue,
  extractFollowFields,
  parseCandidateChecklist,
  parseCheckedCandidate,
  parseFollowCommand,
  resolveFollowCommand
};
