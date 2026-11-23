const FOLLOW_CANDIDATES_MARKER = '<!-- follow-candidates:v1 -->';

function extractFormValue(body, label) {
  const escapedLabel = label.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
  const match = String(body || '').match(
    new RegExp(`### (?:${escapedLabel})\\s*\\n\\s*\\n([\\s\\S]*?)(?=\\n\\n###|\\n\\n---|$)`, 'i')
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

function parseCandidateComment(body) {
  const text = String(body || '');
  if (!text.includes(FOLLOW_CANDIDATES_MARKER)) {
    return [];
  }

  return [...text.matchAll(/^\s*-\s+\[([ xX])\]\s+`([^`]+)`(?:\s|$)/gm)].map((match) => ({
    checked: match[1].toLowerCase() === 'x',
    url: match[2].trim()
  }));
}

function parseCheckedCandidate(body) {
  const candidates = parseCandidateComment(body);
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

function resolveFollowCommand(command, markedComments) {
  if (!command) {
    return { state: 'none' };
  }
  if (/^https?:\/\//i.test(command)) {
    return { state: 'selected', url: command };
  }

  const index = Number(command);
  for (let commentIndex = markedComments.length - 1; commentIndex >= 0; commentIndex -= 1) {
    const candidates = parseCandidateComment(markedComments[commentIndex]);
    if (candidates.length === 0) {
      continue;
    }
    if (!Number.isSafeInteger(index) || index < 1 || index > candidates.length) {
      return { state: 'invalid', reason: `Candidate ${command} is not available.` };
    }
    return { state: 'selected', url: candidates[index - 1].url };
  }
  return { state: 'invalid', reason: 'No feed candidate comment is available.' };
}

function buildCandidateComment(urls) {
  const checklist = urls
    .map((url, index) => `- [ ] \`${url}\` (candidate ${index + 1})`)
    .join('\n');
  return [
    FOLLOW_CANDIDATES_MARKER,
    'I found multiple feeds for this site. Select **exactly one**:',
    '',
    checklist,
    '',
    'Tick exactly one checkbox, or reply with `/follow N` or `/follow <url>`.'
  ].join('\n');
}

module.exports = {
  FOLLOW_CANDIDATES_MARKER,
  buildCandidateComment,
  extractFormValue,
  extractFollowFields,
  parseCandidateComment,
  parseCheckedCandidate,
  parseFollowCommand,
  resolveFollowCommand
};
