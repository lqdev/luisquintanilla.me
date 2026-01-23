---
title: "What came first: the CNAME or the A record?"
targeturl: https://blog.cloudflare.com/cname-a-record-order-dns-standards/
response_type: reshare
dt_published: "2026-01-23 06:21 -05:00"
dt_updated: "2026-01-23 06:21 -05:00"
tags: ["internet","web","dns","networking"]
---

> On January 8, 2026, a routine update to 1.1.1.1 aimed at reducing memory usage accidentally triggered a wave of DNS resolution failures for users across the Internet. The root cause wasn't an attack or an outage, but a subtle shift in the order of records within our DNS responses.  
> <br>
> While most modern software treats the order of records in DNS responses as irrelevant, we discovered that some implementations expect CNAME records to appear before everything else. When that order changed, resolution started failing. This post explores the code change that caused the shift, why it broke specific DNS clients, and the 40-year-old protocol ambiguity that makes the "correct" order of a DNS response difficult to define.