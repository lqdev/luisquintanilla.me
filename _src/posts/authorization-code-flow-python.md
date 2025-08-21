---
post_type: "article" 
title: "Authorization Code Authentication Flow in Python"
published_date: 2017-12-29 18:45:19 -05:00
tags: [python,api,programming,security,authentication]
---

For the longest time, authentication had been my worst enemy. I took small steps towards becoming comfortable with it. At first I learned Implicit Authorization with AngularJS, then I learned Client Credentials Authorization with Python and C# and finally, I learned Authorization Code authentication with C#. The one that always gave me trouble was Authorization Code authentication because it requires user credentials. 

To help with that, I started with the `WebAuthenticationBroker` in C#. Although, most of it was done for me in terms of creating a `WebView` for users to authenticate with their credentials, I wanted to know whether I could do it myself without having to rely on a service to do it for me. 

First, I had to find an API to test this out with and in an attempt to reduce complexity, I decided to prototype it in Python. As an avid user of [Pocket](http://getpocket.com), I chose its API to learn Authorization Flow authentication. 

# Getting Started 

## Register An Application

In order to get started, we need to register an application with Pocket. This can be done by visiting the following [website](https://getpocket.com/developer/apps/new).

## Install Requests

In order to create HTTP requests, we need to install the `requests` module

```
pip install requests
```

# Get Request Token

## Import Modules

```python
import requests
import json
```

## Prepare Request

```python
consumer_key = "YOUR_CONSUMER_KEY"
request_url = "https://getpocket.com/v3/oauth/request"
headers = {"Content-Type":"application/json","X-Accept":"application/json"}
redirect_uri = "http://www.google.com"
payload = {"consumer_key":consumer_key,"redirect_uri":redirect_uri}
```

Notice how the `redirect_uri` is set to `http://www.google.com`. This, for the most part is irrelevant, especially for console/desktop applications. Since this is a console application and we are not hosting a server for it, the value assigned to this field is arbitrary.

## Make Request

```python
request = requests.post(request_url,headers=headers,data=json.dumps(payload))
code = request.json()['code']
code
```

# Redirect User to Pocket to Continue Authorization

## Import Modules

```python
import webbrowser
```

```python
webbrowser.open('https://getpocket.com/auth/authorize?request_token='+ code + '&redirect_uri=' + redirect_uri)
```

This will open your default browser and redirect you to an authorization page. After logging in, should you accept giving the application access to your account, it should redirect you to `http://www.google.com`. Once on the Google page, you can close out of it.

# Convert A Request Token Into A Pocket Access Token

After authorizing the application to access your account, you need to exchange the code you received for an access token.

## Prepare the Request

```python
access_token_url = "https://getpocket.com/v3/oauth/authorize"
payload = {"consumer_key":consumer_key,"code":code}
```

## Get Access Token

```python
access_token_request = requests.post(access_token_url,headers=headers,data=json.dumps(payload))
access_token = access_token_request.json()['access_token']
access_token
```

# Make Authenticated Request

In order to test whether the authentication was successful, try making an authenticated request.

## Prepare Request

```python
get_url = "https://getpocket.com/v3/get"
get_payload = {"consumer_key":consumer_key,"access_token":access_token,"contentType":"article","count":10}
get_request = requests.get(get_url,data=json.dumps(get_payload),headers=headers)
```

## Get Response
```python
response = get_request.json()
```

## Sample Response

```json
{'complete': 1,
 'error': None,
 'list': {'1519049132': {'excerpt': 'Heads up! As part of our efforts to improve security and standards-based interoperability, we have implemented several new features in our authentication flows and made changes to existing ones.',
   'favorite': '0',
   'given_title': '',
   'given_url': 'https://auth0.com/docs/api-auth/grant/authorization-code-pkce',
   'has_image': '0',
   'has_video': '0',
   'is_article': '1',
   'is_index': '0',
   'item_id': '1519049132',
   'resolved_id': '1519049132',
   'resolved_title': 'Calling APIs from Mobile Apps',
   'resolved_url': 'https://auth0.com/docs/api-auth/grant/authorization-code-pkce',
   'sort_id': 5,
   'status': '0',
   'time_added': '1514493110',
   'time_favorited': '0',
   'time_read': '0',
   'time_updated': '1514493110',
   'word_count': '445'},
  '1607678551': {'excerpt': 'What is the smallest number of Democrats that could have changed the outcome of the 2016 United States presidential election by relocating to another state? And where should they have moved?  It turns out this question is a variant of the knapsack problem, an NP-hard computer science classic.',
   'favorite': '0',
   'given_title': '',
   'given_url': 'https://mike.place/2017/ecknapsack/',
   'has_image': '0',
   'has_video': '0',
   'is_article': '1',
   'is_index': '0',
   'item_id': '1607678551',
   'resolved_id': '1607678551',
   'resolved_title': 'The Electoral College and the knapsack problem',
   'resolved_url': 'http://mike.place/2017/ecknapsack/',
   'sort_id': 7,
   'status': '0',
   'time_added': '1514478701',
   'time_favorited': '0',
   'time_read': '0',
   'time_updated': '1514478701',
   'word_count': '1564'},
  '1616406703': {'excerpt': 'OAuth 2.0 is a protocol that allows a user to grant limited access to their resources on one site, to another site. This is done without the users having to expose their credentials. According to OAuth‘s website the protocol is not unlike a valet key.  Many luxury cars today come with a valet key.',
   'favorite': '0',
   'given_title': '',
   'given_url': 'https://auth0.com/docs/protocols/oauth2',
   'has_image': '1',
   'has_video': '0',
   'is_article': '1',
   'is_index': '0',
   'item_id': '1616406703',
   'resolved_id': '1616406703',
   'resolved_title': 'OAuth 2.0',
   'resolved_url': 'https://auth0.com/docs/protocols/oauth2',
   'sort_id': 4,
   'status': '0',
   'time_added': '1514493120',
   'time_favorited': '0',
   'time_read': '0',
   'time_updated': '1514493120',
   'word_count': '823'},
  '1859670338': {'excerpt': 'Last Tuesday saw the official launch of Will Robots Take My Job? and 5 days later we have passed 500K visitors and 4M page views.  To say this surpassed any expectations we had would be a major understatement. This is a recap of how the project got started and the first 5 days after launch.',
   'favorite': '0',
   'given_title': '',
   'given_url': 'https://hackernoon.com/from-idea-to-4m-page-views-in-4-weeks-622aa194787d?ref=quuu',
   'has_image': '1',
   'has_video': '1',
   'is_article': '1',
   'is_index': '0',
   'item_id': '1859670338',
   'resolved_id': '1773505341',
   'resolved_title': 'From Idea to 4M Page Views in 4\xa0Weeks',
   'resolved_url': 'https://hackernoon.com/from-idea-to-4m-page-views-in-4-weeks-622aa194787d',
   'sort_id': 0,
   'status': '0',
   'time_added': '1514494606',
   'time_favorited': '0',
   'time_read': '0',
   'time_updated': '1514494606',
   'word_count': '1837'},
  '2001727306': {'amp_url': 'https://venturebeat.com/2017/12/22/how-ibm-builds-an-effective-data-science-team/amp/',
   'excerpt': 'Data science is a team sport. This sentiment rings true not only with our experiences within IBM, but with our enterprise customers, who often ask us for advice on how to structure data science teams within their own organizations.',
   'favorite': '0',
   'given_title': '',
   'given_url': 'https://venturebeat.com/2017/12/22/how-ibm-builds-an-effective-data-science-team/',
   'has_image': '1',
   'has_video': '0',
   'is_article': '1',
   'is_index': '0',
   'item_id': '2001727306',
   'resolved_id': '2001727306',
   'resolved_title': 'How IBM builds an effective data science team',
   'resolved_url': 'https://venturebeat.com/2017/12/22/how-ibm-builds-an-effective-data-science-team/',
   'sort_id': 1,
   'status': '0',
   'time_added': '1514493979',
   'time_favorited': '0',
   'time_read': '0',
   'time_updated': '1514493979',
   'word_count': '773'},
  '2006596547': {'excerpt': 'I ran, a lot, this year— 2017 miles over 280 hours, or just short of 12 full days. For many of you, that seems like a ridiculous amount of distance and time, but to put it in perspective, elite marathoners and ultrarunners easily run two or three times that distance in a year.',
   'favorite': '0',
   'given_title': '',
   'given_url': 'https://medium.com/run-ruminate/17-thoughts-on-running-life-and-startups-5d305223669b',
   'has_image': '1',
   'has_video': '0',
   'is_article': '1',
   'is_index': '0',
   'item_id': '2006596547',
   'resolved_id': '2006596547',
   'resolved_title': '17 thoughts on running, startups, and life while running 2017 miles in\xa02017',
   'resolved_url': 'https://medium.com/run-ruminate/17-thoughts-on-running-life-and-startups-5d305223669b',
   'sort_id': 6,
   'status': '0',
   'time_added': '1514479812',
   'time_favorited': '0',
   'time_read': '0',
   'time_updated': '1514479812',
   'word_count': '1561'},
  '2006920392': {'excerpt': 'What the heck is “work” anyway?  In the Information Age, the dictionary definition of the word just doesn’t cut it anymore. Skill sets, jobs, and entire companies are forming daily based on new technology, market demands, and trends that didn’t exist even just a few years ago.',
   'favorite': '0',
   'given_title': '',
   'given_url': 'https://www.hellosign.com/blog/the-future-of-work',
   'has_image': '1',
   'has_video': '0',
   'is_article': '1',
   'is_index': '0',
   'item_id': '2006920392',
   'resolved_id': '2006920392',
   'resolved_title': '4 Pillars Of The Future of Work',
   'resolved_url': 'https://www.hellosign.com/blog/the-future-of-work',
   'sort_id': 2,
   'status': '0',
   'time_added': '1514493449',
   'time_favorited': '0',
   'time_read': '0',
   'time_updated': '1514493449',
   'word_count': '1805'},
  '2007117419': {'excerpt': 'Finding a job is not an easy task. There are multiple ways to look for jobs. Multiple steps has to be completed before you are called for an interview. After completing all these the most annoying part is when your interview doesn’t even last for 10 minutes.',
   'favorite': '0',
   'given_title': '',
   'given_url': 'https://www.jobsstudios.com/blog/interview-tips',
   'has_image': '0',
   'has_video': '0',
   'is_article': '1',
   'is_index': '0',
   'item_id': '2007117419',
   'resolved_id': '2007117419',
   'resolved_title': '7 Tips to prepare for an Interview',
   'resolved_url': 'https://www.jobsstudios.com/blog/interview-tips',
   'sort_id': 8,
   'status': '0',
   'time_added': '1514477292',
   'time_favorited': '0',
   'time_read': '0',
   'time_updated': '1514477292',
   'word_count': '468'},
  '2007245672': {'excerpt': 'RESTful API Versioning, though a simple and elegant concept, is a LOT harder to enforce than it sounds. It’s hard to not break backward compatibility on a continually evolving API, and though API versioning is a great concept, it’s rarely followed without flaw.',
   'favorite': '0',
   'given_title': '',
   'given_url': 'https://shift.infinite.red/snapshot-testing-api-calls-the-right-way-58ef59b7f71b',
   'has_image': '1',
   'has_video': '1',
   'is_article': '1',
   'is_index': '0',
   'item_id': '2007245672',
   'resolved_id': '2007245672',
   'resolved_title': 'Snapshot Testing API Calls The Right\xa0Way',
   'resolved_url': 'https://shift.infinite.red/snapshot-testing-api-calls-the-right-way-58ef59b7f71b',
   'sort_id': 3,
   'status': '0',
   'time_added': '1514493189',
   'time_favorited': '0',
   'time_read': '0',
   'time_updated': '1514493189',
   'word_count': '1230'},
  '234097661': {'excerpt': 'Sanchez examines the question, concluding that intellectuals support government intervention because it makes their work have greater importance.',
   'favorite': '0',
   'given_title': '',
   'given_url': 'https://www.libertarianism.org/blog/why-do-intellecuals-support-government-solutions',
   'has_image': '0',
   'has_video': '0',
   'is_article': '1',
   'is_index': '0',
   'item_id': '234097661',
   'resolved_id': '234097661',
   'resolved_title': 'Why Do Intellectuals Support Government Solutions?',
   'resolved_url': 'https://www.libertarianism.org/blog/why-do-intellecuals-support-government-solutions',
   'sort_id': 9,
   'status': '0',
   'time_added': '1514477287',
   'time_favorited': '0',
   'time_read': '0',
   'time_updated': '1514477287',
   'word_count': '1423'}},
 'search_meta': {'search_type': 'normal'},
 'since': 1514514495,
 'status': 1}
```

Hope this helps!

###### Sources

[Pocket API](https://getpocket.com/developer/docs/authentication)  
[Requests](http://docs.python-requests.org/en/master/)