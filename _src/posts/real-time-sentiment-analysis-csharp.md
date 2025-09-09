---
post_type: "article" 
title: Real-Time Sentiment Analysis with C#
published_date: 2018-01-18 03:41:47 -05:00
tags: [data analysis, sentiment analysis, nlp, machine learning, csharp, c#, twitter, api, .net, dotnet]
---

###### This is strictly for use with the .NET Framework. With Mono it might be able to work on other platforms. `SimpleNetNlp` does not currently work with .NET Core/Standard

In this project, I will demonstate how to perform sentiment analysis on tweets using various C# libraries.

## Dependencies

- TweetInviAPI
- SimpleNetNlp
- SimpleNetNlp.Models.LexParser
- SimpleNetNlP.Models.Sentiment

## Create Console Application

In Visual Studio, Click File > New > New Project > Console Application

All of the code below will be placed in the `Program` class.

## Creating A Stream

### Authenticate

Thanks to the `Tweetinvi` library, the authentication with the Twitter API is a breeze. Assuming that an application has been registered at [http://apps.twitter.com](http://apps.twitter.com), the `SetUserCredentials` method can be used and the `Consumer Key`, `Consumer Secret`,`Access Token` and `Access Token Secret` can be passed into it. This type of global authentication makes it easy to perform authenticated calls throughout the entire application.

```csharp
Auth.SetUserCredentials("consumer-key","consumer-secret","access-token","access-token-secret");
```

### Build Stream

Like the authentication, creating a stream is seamless.

We can create a stream by calling the `CreateFilteredStream` method.

```csharp
var stream = Stream.CreateFilteredStream();
```

We can then add conditions to filter on using the `AddTrack` method. In this case, I will be filtering for cryptocurrencies, ether, bitcoin, and litecoin.

```csharp
stream.AddTrack("cryptocurrencies");
stream.AddTrack("bitcoin");
stream.AddTrack("ether");
stream.AddTrack("Litecoin");
```

Additionally, we can filter by language. In my case, I will only be filtering on English. This can be done by using the `AddTweetLanguageFilter` method.

```csharp
stream.AddTweetLanguageFilter("en");
```

Once we have all the filters set up, we need to handle what will happen when a matching tweet is detected. This will be handled by an `EventHandler` called `MatchingTweetReceived`.

```csharp
stream.MatchingTweetReceived += OnMatchedTweet;
```

`MatchingTweetReceived` will be bound to the `OnMatchedTweet` method which I created.

```csharp
private static void OnMatchedTweet(object sender, MatchedTweetReceivedEventArgs args)
{
  //Do Stuff
}
```

The logic inside of this method will perform sentiment analysis and output the sentiment as well as the full text of the tweet.

## Data Cleaning

Tweets can contain many non-ascii characters. Therefore, we need to sanitize it as best as possible so that it can be processed by the sentiment analyzer. To help with that, I used regular expresions to replace non-ascii characters inside of the `sanitize` method.

```csharp
private static string sanitize(string raw)
{
  return Regex.Replace(raw, @"(@[A-Za-z0-9]+)|([^0-9A-Za-z \t])|(\w+:\/\/\S+)", " ").ToString();
}
```

## Sentiment Analysis

In order to perform sentiment analysis, we will be using the `SimpleNetNlp` library. This library is built on top of the Stanford CoreNLP library. In order to get the sentiment of a piece of text, we need to create a `Sentence` object which takes a string as a parameter and then get the `Sentiment` property. In our case, the parameter that will be used to instantiate a new `Sentence` object will be the sanitized text of a tweet.

```csharp
var sanitized = sanitize(args.Tweet.FullText);
string sentence = new Sentence(sanitized);
```

The code above will be placed inside of the `OnMatchedTweet` method.

## Produce Output

Now that we have everything set up, we can just output to the console the sentiment and raw text of the tweet. To do that, we can place the code below inside the `OnMatchedTweet` method.

```csharp
Console.WriteLine(sentence.Sentiment + "|" + args.Tweet);
```

The final `OnMatchedTweet` method looks as follows:

```csharp
private static void OnMatchedTweet(object sender, MatchedTweetReceivedEventArgs args)
{
    var sanitized = sanitize(args.Tweet.FullText); //Sanitize Tweet
    var sentence = new Sentence(sanitized); //Get Sentiment

    //Output Tweet and Sentiment
    Console.WriteLine(sentence.Sentiment + "|" + args.Tweet);
}
```
## Run

Once we run the application, our console application should look something like this:

![](https://cdn.lqdev.tech/files/images/sentiment-analysis-1.png)


## Conclusion

C# is not always the first language that comes to mind when doing analytics and machine learning. However, tasks such as sentiment analysis can be trivially performed thanks to libraries such as `Tweetinvi` and `SimpleNetNlp`. In its current state, this application is not very useful because it just outputs to the console sentiments and the respective tweets. In order to make it more useful, we can collect and aggregate the data someplace for more robust analysis. 
