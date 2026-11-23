const assert = require('node:assert/strict');
const http = require('node:http');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const { spawn } = require('node:child_process');

const projectRoot = path.resolve(__dirname, '..');
const fixtureRoot = path.join(__dirname, 'fixtures', 'follow');
const processorPath = path.join(projectRoot, 'Scripts', 'process-follow-issue.fsx');
const dotnetExe = process.env.DOTNET_EXE || 'dotnet';

function fixture(name) {
  return fs.readFileSync(path.join(fixtureRoot, name), 'utf8');
}

function response(body, contentType, extra = {}) {
  return { status: 200, body, contentType, ...extra };
}

function startServer(routes) {
  const requests = [];
  const server = http.createServer((request, serverResponse) => {
    requests.push(request.url);
    const route = routes[request.url || '/'];
    if (!route) {
      serverResponse.writeHead(404, { 'Content-Type': 'text/plain' });
      serverResponse.end('not found');
      return;
    }
    if (route.destroy) {
      request.socket.destroy();
      return;
    }
    serverResponse.writeHead(route.status || 200, {
      'Content-Type': route.contentType || 'text/html; charset=utf-8',
      ...(route.headers || {})
    });
    serverResponse.end(route.body);
  });

  return new Promise((resolve, reject) => {
    server.once('error', reject);
    server.listen(0, '127.0.0.1', () => {
      const address = server.address();
      resolve({
        origin: `http://127.0.0.1:${address.port}`,
        requests,
        close: () => new Promise((closeResolve) => server.close(closeResolve))
      });
    });
  });
}

function runProcessor(cwd, args) {
  return new Promise((resolve, reject) => {
    const child = spawn(dotnetExe, ['fsi', processorPath, '--', ...args], {
      cwd,
      env: { ...process.env, FOLLOW_SKIP_FEED_CHECK: '' },
      windowsHide: true
    });
    let stdout = '';
    let stderr = '';
    child.stdout.on('data', (chunk) => { stdout += chunk; });
    child.stderr.on('data', (chunk) => { stderr += chunk; });
    child.once('error', reject);
    child.once('close', (code) => resolve({ code, output: stdout + stderr }));
  });
}

async function runCase(name, options) {
  const tempRoot = fs.mkdtempSync(path.join(os.tmpdir(), 'follow-processing-'));
  const dataDir = path.join(tempRoot, 'Data');
  fs.mkdirSync(dataDir);
  const blogroll = JSON.stringify(options.blogroll || [], null, 4) + '\n';
  const podroll = JSON.stringify(options.podroll || [], null, 4) + '\n';
  fs.writeFileSync(path.join(dataDir, 'blogroll.json'), blogroll);
  fs.writeFileSync(path.join(dataDir, 'podroll.json'), podroll);
  const server = await startServer(options.routes);

  try {
    const siteUrl = options.siteUrl
      ? options.siteUrl(server.origin)
      : `${server.origin}/site`;
    const feedUrl = options.feedUrl ? options.feedUrl(server.origin) : '';
    const result = await runProcessor(tempRoot, [
      feedUrl,
      siteUrl,
      `Fixture ${name}`,
      'blogroll'
    ]);
    const afterBlogroll = fs.readFileSync(path.join(dataDir, 'blogroll.json'), 'utf8');
    const afterPodroll = fs.readFileSync(path.join(dataDir, 'podroll.json'), 'utf8');
    return {
      ...result,
      origin: server.origin,
      blogroll,
      podroll,
      afterBlogroll,
      afterPodroll,
      requests: server.requests
    };
  } finally {
    await server.close();
    fs.rmSync(tempRoot, { recursive: true, force: true });
  }
}

function candidates(result) {
  const line = result.output
    .split(/\r?\n/)
    .find((entry) => entry.startsWith('CANDIDATES_JSON='));
  assert.ok(line, `Missing CANDIDATES_JSON in output:\n${result.output}`);
  return JSON.parse(line.slice('CANDIDATES_JSON='.length));
}

async function main() {
  const mainFeed = response(fixture('main.xml'), 'application/rss+xml');
  const atomFeed = response(fixture('secondary.xml'), 'application/atom+xml');
  const jsonFeed = response(fixture('index.json'), 'application/feed+json');
  const htmlOnly = response(fixture('html-only.html'), 'text/html; charset=utf-8');

  const one = await runCase('one feed', {
    routes: {
      '/site': response(fixture('one-feed.html'), 'text/html'),
      '/main.xml': mainFeed
    }
  });
  assert.equal(one.code, 0, one.output);
  assert.equal(JSON.parse(one.afterBlogroll)[0].XmlUrl, `${one.origin}/main.xml`);

  const many = await runCase('multiple feeds', {
    routes: {
      '/site': response(fixture('many-feeds.html'), 'text/html'),
      '/main.xml': mainFeed,
      '/secondary.xml': atomFeed,
      '/index.json': jsonFeed,
      '/comments/feed': mainFeed
    }
  });
  assert.equal(many.code, 2, many.output);
  const manyCandidates = candidates(many);
  assert.equal(manyCandidates.length, 4);
  assert.equal(new Set(manyCandidates).size, 4);
  assert.match(manyCandidates.at(-1), /\/comments\/feed$/);
  assert.equal(many.afterBlogroll, many.blogroll);

  const noFeed = await runCase('no feed', {
    routes: { '/site': htmlOnly }
  });
  assert.equal(noFeed.code, 3, noFeed.output);
  assert.equal(noFeed.afterBlogroll, noFeed.blogroll);
  assert.equal(noFeed.afterPodroll, noFeed.podroll);

  const confirmedNonFeed = await runCase('confirmed non-feed candidate', {
    routes: {
      '/site': response(
        '<!doctype html><a type="application/rss+xml" href="/not-a-feed">Feed</a>',
        'text/html'
      ),
      '/not-a-feed': htmlOnly
    }
  });
  assert.equal(confirmedNonFeed.code, 3, confirmedNonFeed.output);
  assert.equal(confirmedNonFeed.afterBlogroll, confirmedNonFeed.blogroll);

  const explicit = await runCase('explicit feed', {
    routes: { '/site': htmlOnly, '/manual.xml': mainFeed },
    feedUrl: (origin) => `${origin}/manual.xml`
  });
  assert.equal(explicit.code, 0, explicit.output);
  assert.equal(JSON.parse(explicit.afterBlogroll)[0].XmlUrl, `${explicit.origin}/manual.xml`);

  for (const status of [401, 403, 429, 503]) {
    const blockedExplicit = await runCase(`explicit blocked feed ${status}`, {
      routes: {
        '/site': htmlOnly,
        '/manual.xml': response('', 'text/plain', { status })
      },
      feedUrl: (origin) => `${origin}/manual.xml`
    });
    assert.equal(blockedExplicit.code, 0, blockedExplicit.output);
    assert.equal(JSON.parse(blockedExplicit.afterBlogroll)[0].XmlUrl, `${blockedExplicit.origin}/manual.xml`);
  }

  const probed = await runCase('path probe', {
    routes: {
      '/site': htmlOnly,
      '/feed': mainFeed
    }
  });
  assert.equal(probed.code, 0, probed.output);
  assert.match(JSON.parse(probed.afterBlogroll)[0].XmlUrl, /\/feed$/);

  const siteFeed = await runCase('feed URL as site URL', {
    routes: { '/site-feed.xml': atomFeed },
    siteUrl: (origin) => `${origin}/site-feed.xml`
  });
  assert.equal(siteFeed.code, 0, siteFeed.output);
  assert.match(JSON.parse(siteFeed.afterBlogroll)[0].XmlUrl, /\/site-feed\.xml$/);

  const relative = await runCase('relative base URL', {
    routes: {
      '/base/page': response(fixture('relative-base.html'), 'text/html'),
      '/alternate/relative.json': jsonFeed
    },
    siteUrl: (origin) => `${origin}/base/page`
  });
  assert.equal(relative.code, 0, relative.output);
  assert.match(JSON.parse(relative.afterBlogroll)[0].XmlUrl, /\/alternate\/relative\.json$/);

  const redirectedSite = await runCase('redirect-relative discovery', {
    routes: {
      '/blog': response('', 'text/html', {
        status: 302,
        headers: { Location: '/blog/' }
      }),
      '/blog/': response(
        '<!doctype html><a type="application/rss+xml" href="updates.rss">Feed</a>',
        'text/html'
      ),
      '/blog/updates.rss': mainFeed
    },
    siteUrl: (origin) => `${origin}/blog`
  });
  assert.equal(redirectedSite.code, 0, redirectedSite.output);
  assert.equal(
    JSON.parse(redirectedSite.afterBlogroll)[0].XmlUrl,
    `${redirectedSite.origin}/blog/updates.rss`
  );

  const redirectedFeed = await runCase('redirected discovered feed', {
    routes: {
      '/site': response(
        '<!doctype html><a type="application/rss+xml" href="/temporary.xml">Feed</a>',
        'text/html'
      ),
      '/temporary.xml': response('', 'text/plain', {
        status: 302,
        headers: { Location: '/actual.xml' }
      }),
      '/actual.xml': mainFeed
    }
  });
  assert.equal(redirectedFeed.code, 0, redirectedFeed.output);
  assert.equal(JSON.parse(redirectedFeed.afterBlogroll)[0].XmlUrl, `${redirectedFeed.origin}/actual.xml`);

  const lateHtmlLink = await runCase('large HTML feed link', {
    routes: {
      '/site': response(
        `<!doctype html><style>${'x'.repeat(24 * 1024)}</style><link rel="alternate" type="application/rss+xml" href="/late.xml">`,
        'text/html'
      ),
      '/late.xml': mainFeed
    }
  });
  assert.equal(lateHtmlLink.code, 0, lateHtmlLink.output);
  assert.equal(JSON.parse(lateHtmlLink.afterBlogroll)[0].XmlUrl, `${lateHtmlLink.origin}/late.xml`);

  const truncatedHtml = await runCase('truncated HTML', {
    routes: {
      '/site': response(
        `<!doctype html>${'x'.repeat((1024 * 1024) + 1)}<link rel="alternate" type="application/rss+xml" href="/late.xml">`,
        'text/html'
      ),
      '/late.xml': mainFeed
    }
  });
  assert.equal(truncatedHtml.code, 1, truncatedHtml.output);
  assert.match(truncatedHtml.output, /discovery could not be completed/i);
  assert.equal(truncatedHtml.requests.length, 1);
  assert.equal(truncatedHtml.afterBlogroll, truncatedHtml.blogroll);

  const blockedDiscovered = await runCase('blocked discovered feed', {
    routes: {
      '/site': htmlOnly,
      '/feed': response('', 'text/plain', { status: 503 })
    }
  });
  assert.equal(blockedDiscovered.code, 1, blockedDiscovered.output);
  assert.match(blockedDiscovered.output, /discovery could not be completed/i);
  assert.equal(blockedDiscovered.afterBlogroll, blockedDiscovered.blogroll);

  const validWithBlockedProbe = await runCase('valid feed with blocked speculative probe', {
    routes: {
      '/site': response(fixture('one-feed.html'), 'text/html'),
      '/main.xml': mainFeed,
      '/feed': response('', 'text/plain', { status: 503 })
    }
  });
  assert.equal(validWithBlockedProbe.code, 0, validWithBlockedProbe.output);
  assert.equal(
    JSON.parse(validWithBlockedProbe.afterBlogroll)[0].XmlUrl,
    `${validWithBlockedProbe.origin}/main.xml`
  );

  const unreachableSite = await runCase('unreachable site', {
    routes: { '/site': { destroy: true } }
  });
  assert.equal(unreachableSite.code, 1, unreachableSite.output);
  assert.match(unreachableSite.output, /discovery could not be completed/i);
  assert.equal(unreachableSite.afterBlogroll, unreachableSite.blogroll);

  const truncatedCandidate = await runCase('truncated discovered feed response', {
    routes: {
      '/site': response(
        '<!doctype html><a type="application/rss+xml" href="/candidate.xml">Feed</a>',
        'text/html'
      ),
      '/candidate.xml': response('x'.repeat(8193), 'text/plain')
    }
  });
  assert.equal(truncatedCandidate.code, 1, truncatedCandidate.output);
  assert.match(truncatedCandidate.output, /truncated/i);
  assert.equal(truncatedCandidate.afterBlogroll, truncatedCandidate.blogroll);

  const htmlAsFeed = await runCase('HTML passed as explicit feed', {
    routes: { '/site': htmlOnly },
    feedUrl: (origin) => `${origin}/site`
  });
  assert.equal(htmlAsFeed.code, 1, htmlAsFeed.output);
  assert.equal(htmlAsFeed.afterBlogroll, htmlAsFeed.blogroll);
  assert.equal(htmlAsFeed.afterPodroll, htmlAsFeed.podroll);

  const duplicateUrl = 'https://existing.test/';
  const duplicate = await runCase('duplicate site', {
    routes: { '/site': mainFeed },
    siteUrl: () => duplicateUrl,
    blogroll: [{
      Title: 'Existing',
      Type: 'rss',
      HtmlUrl: duplicateUrl,
      XmlUrl: 'https://existing.test/feed.xml',
      Added: '2026-01-01'
    }]
  });
  assert.equal(duplicate.code, 1, duplicate.output);
  assert.equal(duplicate.requests.length, 0);
  assert.equal(duplicate.afterBlogroll, duplicate.blogroll);

  const capRoutes = { '/site': { body: '', contentType: 'text/html' } };
  const capLinks = [];
  for (let index = 1; index <= 16; index += 1) {
    capLinks.push(`<a type="application/rss+xml" href="/candidate-${index}.xml">Feed ${index}</a>`);
    capRoutes[`/candidate-${index}.xml`] = mainFeed;
  }
  capRoutes['/site'] = response(`<!doctype html><html>${capLinks.join('')}</html>`, 'text/html');
  const capped = await runCase('candidate cap', { routes: capRoutes });
  assert.equal(capped.code, 1, capped.output);
  assert.match(capped.output, /validation budget/i);
  assert.equal(capped.requests.length, 1);
  assert.equal(capped.afterBlogroll, capped.blogroll);

  const ordinalCandidates = [
    '/Feed',
    '/feed',
    '/feed/',
    '/same?version=1',
    '/same?version=2'
  ];
  const ordinalRoutes = {
    '/site': response(
      `<!doctype html>${ordinalCandidates.map((href) => `<a type="application/rss+xml" href="${href}">Feed</a>`).join('')}`,
      'text/html'
    )
  };
  for (const candidate of ordinalCandidates) {
    ordinalRoutes[candidate] = mainFeed;
  }
  const ordinal = await runCase('ordinal URL dedupe', { routes: ordinalRoutes });
  assert.equal(ordinal.code, 2, ordinal.output);
  const ordinalUrls = candidates(ordinal);
  assert.equal(ordinalUrls.length, 5);
  for (const candidate of ordinalCandidates) {
    assert.ok(ordinalUrls.includes(`${ordinal.origin}${candidate}`), candidate);
  }

  console.log('Follow processor fixture checks passed.');
}

main().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});
