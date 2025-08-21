---
post_type: "article" 
title: "Super simple captcha"
description: "Implement a simple captcha challenge without dependencies"
published_date: "10/24/2021 13:00 -05:00"
tags: [web development, html, javascript, webmentions]
---

## Introduction

I've been thinking about implementing webmentions on this site. According to the spec, "Webmention is a simple way to notify any URL when you mention it on your site. From the receiver's perspective, it's a way to request notifications when other sites mention it". I won't get into the technical details of webmentions in this post, but the simplest way to implement webmentions is to have a text input box on your site for each of your articles, kind of like a comment box. When someone wants to mention your post on their site, they can use the text input box on your site to add the URL from their site where they mention your post. When the user submits the webmention, there's some endpoint that listens for webmention submissions and processes them accordingly. If you're interested in learning more about webmentions, you can check out the following resources:

- [Webmentions spec](https://www.w3.org/TR/webmention/)
- [Webmention Rocks](https://webmention.rocks/)
- [Sending Your First Webmention - IndieWebCamp Düsseldorf 2019](https://yewtu.be/watch?v=ZOlkS6xP2Zk)

While it's great that you can mention and comment on other people's content from your site, like comments, writing a script to submit spam is relatively simple. Therefore, I want to create some sort of [CAPTCHA](https://en.wikipedia.org/wiki/CAPTCHA) challenge as an initial form of validation to prevent spam. At the same time, I don't want to have any external dependencies. In this post, I'll show how I went about implementing a captcha-like solution with zero dependencies to prevent spam submissions. 

## Simple captcha

The solution I came up with asks the users to add the day of the month (1-31) to a random number between 1-100. While not entirely foolproof, it's "complex" enough that it's not the same thing every time. 

Below is what the implementation looks like. 

![Captcha implementation asking for date](https://user-images.githubusercontent.com/11130940/138604591-dfe4c301-78fe-4338-a751-799b420a1791.png)

The HTML markup for the webpage looks like the following:

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta http-equiv="X-UA-Compatible" content="IE=edge">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Super Simple Captcha</title>
    <script src="main.js" type="text/javascript"></script>
</head>
<body onload="setQuery()">
    <div>
        <span id="query"></span>
        <input type="text" id="answerInput">
        <button onclick="displayResult()">Submit</button>
    </div>
    <h3 id="result"></h3>
</body>
</html>
```

When the `body` element of the page loads, it invokes the `setQuery` function, which displays the challenge the user is supposed to solve in the `query` span element. 

The user then submits their answer via the `answerInput` text input box. 

The answer is then checked by invoking the `displayResult` function. The `displayResult` function checks the user's answer against the expected answer. If the answer is correct, the text "OK" is displayed on the page's `result` H3 element. Otherwise, the text "Try again" displays on the webpage.

All of the code that handles this logic is in the `main.js` file.

```javascript
let date = new Date()
let day = date.getDate() // Day of the month
let randomNumber = Math.floor(Math.random() * 100) + 1
let answer = day + randomNumber // Expected answer

let setQuery = () => {
    let element = document.getElementById('query');
    element.innerText = `Enter the sum of ${day} + ${randomNumber}`;    
}

let checkAnswer = () => {
    let userAnswer = parseInt(document.getElementById("answerInput").value);
    return answer === userAnswer
}

let displayResult = () => {
    let result = checkAnswer() ? "OK" : "Try again" // Ternary function to check if answer is correct
    document.getElementById("result").innerText = result;
}
```

That's all there is to it!

## Conclusion

In this post, I showed how to implement a dependency-free solution to present a challenge to users submitting comments / webmentions to your site. Although the solution isn't foolproof, it's just complex enough any spammers would have to work a little harder. Happy coding!  