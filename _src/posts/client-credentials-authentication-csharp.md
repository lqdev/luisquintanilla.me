---
title: 'Client Credentials Authorization in C#'
tags: csharp, authentication, web applications, .net, api, .net core,
published_date: 2017-12-25 19:28:48
---


# Getting Started

For this writeup, I'm going to use the Spotify API. Spotify API supports different authorization flows. In this writeup, I will be using the client credentials authorization flow. Generally this works for server-to-server authentication. Because this does not allow users the ability to provide their own credentials, there is no access to endpoints that contain user data.

![](/images/client-credentials-authentication-csharp/clientcredentialsflowdiagram.png)

# Create a new .NET Core Console Application

```bash
dotnet new console -o authtest
```

# Add Dependencies

```bash
dotnet add package Newtonsoft.Json --version 10.0.3
```

# Access Token Model

When a request is made, it needs to be parsed. To better capture the data into a Plain Old CLR Object (POCO), a model can be created.

```csharp
using System;

namespace authtest
{
    class AccessToken
    {
        public string access_token { get; set; }
        public string token_type { get; set; }
        public long expires_in { get; set; }
    }
}
```

# Request Token

In order to extract our token, an HTTP Request needs to be made to the Spotify API in order to get an access token. To do so, we can leverage the `HTTPClient` functionalities.

```csharp
private static async Task<string> GetToken()
{
    string clientId = "YOUR CLIENT ID";
    string clientSecret = "YOUR CLIENT SECRET";
    string credentials = String.Format("{0}:{1}",clientId,clientSecret);

    using(var client = new HttpClient())
    {
        //Define Headers
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials)));

        //Prepare Request Body
        List<KeyValuePair<string,string>> requestData = new List<KeyValuePair<string,string>>();
        requestData.Add(new KeyValuePair<string,string>("grant_type","client_credentials"));

        FormUrlEncodedContent requestBody = new FormUrlEncodedContent(requestData);

        //Request Token
        var request = await client.PostAsync("https://accounts.spotify.com/api/token",requestBody);
        var response = await request.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<AccessToken>(response);   
    }
}
```


We can then use this function in our main method to request the token.

```csharp
static void Main(string[] args)
{
    Console.WriteLine("Spotify API");
    AccessToken token = GetToken().Result;
    Console.WriteLine(String.Format("Access Token: {0}",token.access_token));
}
```

This should produce the following output

```bash
Spotify API
Getting Token
Access Token: "YOUR ACCESS TOKEN"
```

The token can now be used to make requests to the Spotify API.

Source Code can be found here [Source Code](https://gist.github.com/lqdev/5e82a5c856fcf0818e0b5e002deb0c28)

###### Sources
[Spotify Authorization Guide](https://developer.spotify.com/web-api/authorization-guide/#client_credentials_flow)