---
title: "A Proposal for News Organization Mastodon Servers and More"
targeturl: https://newsletter.danhon.com/archive/4230/ 
response_type: star
dt_published: "2022-10-28 21:07"
dt_updated: "2022-10-28 21:07 -05:00"
---

> Here’s how it works in practice:
> 
> 1. A news organization (or any organization, let’s just start with news) already asserts ownership of its domain e.g. via its certificate, so we piggyback trust off its domain.
> 2. It stands up a Mastodon or other social server at a standard address. I’d propose follow.washingtonpost.com but there’s a bunch of reasons why you might do something else, see below, and uses the agreed well-known autodiscovery protocol to return the address for its Mastodon server (but I don’t see an entry for activitypub or Mastodon yet).
> 3. It creates accounts for its staff on its Mastodon server. Nobody else gets an account; the public can’t sign up.

> What you get: 
> 
> 1. Verified accounts. Instead of relying on a third party to “verify” your account by sticking a blue check against your display name, account provenance and “verification” is inherited as a property of the Mastodon server....
> 2. Ease of discovery...all a user would have to do, to find Washington Post accounts to follow, would be to know the washingtonpost.com domain. Autodiscovery would let your Mastodon client point itself to the appropriate server. 

> Not just news organizations...anyone can set up a Mastodon server...the federation means that “official” accounts become “more official” when their server home is hung off the domain of the actual organization. 

> You wouldn’t need Twitter (or anyone else, really) to verify that the UK Prime Minister’s account is official, because you’d have following.gov.uk as the Mastodon server, which means you can trust that server as much as you trust .gov.uk domains.

>  Your university or college wants you to have a social media account? Sure, you can have it hosted at following.ucla.edu.

> And yes, brands can get in on it. Sure. That way there’s a tiny chance you’re following the Proper Brand Account rather than a Parody Brand Account, which… is probably for the best. Or it’s easier to see that a Parody Account is a Parody Account because you can look at the parent server.