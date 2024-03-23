---
post_type: "note" 
title: "Use Azure CDN from Verizon Premium for Regex URL redirects"
published_date: "2022-12-21 15:09"
tags: ["azure","azurecdn","redirect","indieweb","selfhosting"]
---

Well, that's time that I'm never going to get back. 

Imagine that you wanted to redirect a URL,

From: `https://mydomain/github/repo-name`  
To: `https://github.com/username/repo-name`

You could use regular expressions to parse out the `repo-name` part of the URL path from the source URL to the destination URL. This is what it might look like using something like NGINX.

```bash
location ~ ^/github/(.*) {
     return 307 https://github.com/yourGHusername/$1;
}
```

Unless I wasn't looking at the right documentation, it's not obvious that the rules engine for most Azure CDN plans don't support this feature, except for the [Verizon Premium](https://learn.microsoft.com/azure/cdn/cdn-verizon-premium-rules-engine-reference-features). While the standard rules engine supports wildcards for source URLs, lack of regular expresssions support means there's no way to parameterize the values of the destination URL. If this is something  you need, make sure to use the Verizon Premium plan.