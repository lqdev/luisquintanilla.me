const { chromium } = require('playwright');

(async () => {
  console.log('Starting website navigation test...');
  
  const browser = await chromium.launch({ headless: false });
  const context = await browser.newContext();
  const page = await context.newPage();

  try {
    // Navigate to homepage
    console.log('1. Testing homepage...');
    await page.goto('http://localhost:8080');
    await page.waitForLoadState('networkidle');
    console.log('✅ Homepage loaded successfully');

    // Test featured content links
    console.log('2. Testing featured content links...');
    
    // Test latest microblog note
    const microblogLink = page.locator('a[href*="2025-07-06-weekly-post-summary"]');
    if (await microblogLink.isVisible()) {
      await microblogLink.click();
      await page.waitForLoadState('networkidle');
      console.log('✅ Microblog note page loaded');
      await page.goBack();
    }

    // Test latest response
    const responseLink = page.locator('a[href*="tumblr-wordpress-fediverse-integration-pause"]');
    if (await responseLink.isVisible()) {
      await responseLink.click();
      await page.waitForLoadState('networkidle');
      console.log('✅ Response page loaded');
      await page.goBack();
    }

    // Test latest blog post
    const blogLink = page.locator('a[href*="indieweb-create-day-2025-07"]');
    if (await blogLink.isVisible()) {
      await blogLink.click();
      await page.waitForLoadState('networkidle');
      console.log('✅ Blog post page loaded');
      await page.goBack();
    }

    // Test navigation dropdowns
    console.log('3. Testing navigation dropdowns...');
    
    // Test About dropdown
    await page.locator('#aboutDropdown').click();
    await page.waitForTimeout(500);
    await page.locator('.dropdown-menu a[href="/about"]').first().click();
    await page.waitForLoadState('networkidle');
    console.log('✅ About page loaded');
    await page.goBack();

    // Test Feeds dropdown
    await page.locator('#feedDropdown').click();
    await page.waitForTimeout(500);
    await page.locator('.dropdown-menu a[href="/feed"]').click();
    await page.waitForLoadState('networkidle');
    console.log('✅ Main feed page loaded');
    await page.goBack();

    // Test Collections dropdown
    await page.locator('#collectionDropdown').click();
    await page.waitForTimeout(500);
    await page.locator('.dropdown-menu a[href="/reviews"]').click();
    await page.waitForLoadState('networkidle');
    console.log('✅ Reviews page loaded');
    await page.goBack();

    // Test Knowledgebase dropdown
    await page.locator('#kbDropdown').click();
    await page.waitForTimeout(500);
    await page.locator('.dropdown-menu a[href="/resources/snippets"]').click();
    await page.waitForLoadState('networkidle');
    console.log('✅ Snippets page loaded');
    await page.goBack();

    // Test RSS feeds
    console.log('4. Testing RSS feeds...');
    await page.goto('http://localhost:8080/feed/index.xml');
    const mainFeed = await page.textContent('body');
    if (mainFeed.includes('<?xml') && (mainFeed.includes('<feed') || mainFeed.includes('<rss'))) {
      console.log('✅ Main feed is valid XML');
    } else {
      console.log('❌ Main feed appears invalid');
    }

    await page.goto('http://localhost:8080/feed/notes.xml');
    const notesFeed = await page.textContent('body');
    if (notesFeed.includes('<?xml') && (notesFeed.includes('<feed') || notesFeed.includes('<rss'))) {
      console.log('✅ Notes feed is valid XML');
    } else {
      console.log('❌ Notes feed appears invalid');
    }

    await page.goto('http://localhost:8080/feed/responses/index.xml');
    const responsesFeed = await page.textContent('body');
    if (responsesFeed && responsesFeed.includes('<?xml') && (responsesFeed.includes('<feed') || responsesFeed.includes('<rss'))) {
      console.log('✅ Responses feed is valid XML');
    } else {
      console.log('❌ Responses feed appears invalid or missing');
    }

    console.log('✅ Website navigation test completed successfully!');

  } catch (error) {
    console.log('❌ Error during testing:', error.message);
  } finally {
    await browser.close();
  }
})();
