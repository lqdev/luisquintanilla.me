---
title: Create an HTTP Trigger Azure Function using FSharp
date: 2019-11-16 17:33:59
tags: fsharp,serverless,azure-functions,azure,dotnet,dotnet-core,functional-programming
---

## Introduction

Since the upgrade to the 2.0 version of the Azure Functions runtime, .NET Core has been natively supported by the platform. As a result some changes took effect. Most notably, in version 1.0, a template for an F# HttpTrigger function was available. The template was removed in 2.0. However, that does not mean Azure Functions does not support F#. Azure Functions can be built in F# using a .NET Standard Class Library. This writeup provides a detailed walk-through of how to build an Azure Function that processes HTTP requests using F#. The complete code sample can be found on [GitHub](https://github.com/lqdev/FsHttpTriggerSample).

## Prerequisites

This solution was built using a Windows PC but should work on Mac and Linux.

- [.NET SDK (2.x or 3.x)](https://dotnet.microsoft.com/download/dotnet-core)
- [Node.js](https://nodejs.org/en/download/)
- [Azure Functions Core Tools](https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local#windows-npm)

## Create Solution

Open the command prompt and create a new directory for your solution called "FsHttpTriggerSample".

```bash
mkdir FsHttpTriggerSample
```

Navigate into the new directory and create a solution using the .NET CLI.

```bash
cd FsHttpTriggerSample
dotnet new sln
```

## Create Azure Functions Project

Inside the *FsHttpTriggerSample* solution directory, use the .NET CLI to create a new F# .NET Standard Class Library project.

```bash
dotnet new classlib -o FsHttpTriggerSample -lang f#
```

Add the project to the solution

```bash
dotnet sln add FsHttpTriggerSample
```

## Install NuGet Packages

To use Azure Functions, install the [**Microsoft.Net.Sdk.Functions** NuGet package](https://www.nuget.org/packages/Microsoft.NET.Sdk.Functions).

Inside the *FsHttpTriggerSample* project directory, enter the following command.

```bash
dotnet add package Microsoft.Net.Sdk.Functions
```

## Create the Azure Function

### Prepare Files

Delete the default *Library.fs* file inside the *FsHttpTriggerSample* project directory.

```bash
del Library.fs
```

Create a new file called *GreetFunction.fs*.

```bash
type nul > GreetFunction.fs
```

### Configure Files

Open the *FsHttpTriggerSample.fsproj* file and find the following snippet.

```xml
<Compile Include="Library.fs" />
```

Replace the snippet with the content below.

```xml
<Compile Include="GreetFunction.fs" />
```

### Configure Host

At a minimum, Azure Functions requires the runtime version to run. This information is provided by a file called *host.json*.

Create a new file called *host.json* inside the *FsHttpTriggerSample* project directory.

```bash
type nul > host.json
```

Open the *host.json* file and add the following content

```json
{
    "version": "2.0"
}
```

### Implement Azure Function

Open the *GreetFunction.fs* file and add the namespace and module for it.

```fsharp
namespace FsHttpTriggerSample

module GreetFunction = 
```

Below the module definition, add the following `open` statements:

```fsharp
open Microsoft.AspNetCore.Mvc
open Microsoft.Azure.WebJobs
open Microsoft.AspNetCore.Http
open Newtonsoft.Json
open System.IO
open Microsoft.Extensions.Logging
```

Define a `User` type containing a single property called `Name`.

```fsharp
type User = {
    Name: string
}
```

The entrypoint of an Azure Function is the `Run` function. Create a function called `Run`.

```fsharp
[<FunctionName("Greet")>]
let Run ([<HttpTrigger(Methods=[|"POST"|])>] req:HttpRequest) (log:ILogger) = 
```

To register an Azure Function, use the `FunctionName` attribute. In this case, the name of the function is `Greet`. The `Run` function takes two parameters, an `HttpRequest` and an `ILogger`. Since the binding used by HTTP Trigger functions is `HttpTrigger`, the request object is annotated with the `HttpTrigger` attribute. Options such as the accepted methods can be provided through the `HttpTrigger` attribute. In this case, only `POST` requests are accepted.

Create an `async` computation expression inside the `Run` function.

```fsharp
async {

}
```

Inside the `async` expression, add logging to indicate that the function has initialized. 

```fsharp
"Running function"
|> log.LogInformation
```

Below that, get the body of the request.

```fsharp
let! body = 
    new StreamReader(req.Body) 
    |> (fun stream -> stream.ReadToEndAsync()) 
    |> Async.AwaitTask
```

Then, deserialized the body into an instance of `User`.

```fsharp
let user = JsonConvert.DeserializeObject<User>(body)
```

Return a personalized greeting with the user's name.

```fsharp
return OkObjectResult(sprintf "Hello %s" user.Name)
```

Finally, use the `StartAsTask` function to start the `async` expression as a `Task`.

```fsharp
|> Async.StartAsTask
```

Once finished, the contents of the *GreetFunction.fs* should look similar to the following.

```fsharp
namespace FsHttpTriggerSample

module GreetFunction = 

    open Microsoft.AspNetCore.Mvc
    open Microsoft.Azure.WebJobs
    open Microsoft.AspNetCore.Http
    open Newtonsoft.Json
    open System.IO
    open Microsoft.Extensions.Logging

    type User = {
        Name: string
    }

    [<FunctionName("Greet")>]
    let Run ([<HttpTrigger(Methods=[|"POST"|])>] req:HttpRequest) (log:ILogger) = 
        async {
            "Runnning Function"
            |> log.LogInformation

            let! body = 
                new StreamReader(req.Body) 
                |> (fun stream -> stream.ReadToEndAsync()) 
                |> Async.AwaitTask

            let user = JsonConvert.DeserializeObject<User>(body)

            return OkObjectResult(sprintf "Hello %s" user.Name)
        } |> Async.StartAsTask
```

## Run the Function Locally

Build the project by using the `build` command inside the *FsHttpTriggerSample* project directory.

```bash
dotnet build
```

Then, navigate to the output directory

```bash
cd bin\Debug\netstandard2.0
```

Use the Azure Functions Core Tools to start the Azure Functions host locally.

```bash
func host start
```

Once the host is initialized, the function is available at the following endpoint `http://localhost:7071/api/Greet`.

## Test the function

Using a REST client like Postman or Insomnia, make a POST request to `http://localhost:7071/api/Greet` with the following body. Feel free to replace the name with your own.

```json
{
    "Name": "Luis"
}
```

If successful, the response should look similar to the following output.

```text
Hello Luis
```

## Conclusion

This writeup showed how to create an HTTP Trigger Azure Function using F#. Creating additional functions inside the same project is relatively trivial since the structure of the *GreetFunction.fs* file can be copied and the logic inside the `Run` function can be adapted to meet your requirements.

## Resources

- [Azure Functions F# Developer Reference](https://docs.microsoft.com/en-us/azure/azure-functions/functions-reference-fsharp)
- [Azure Functions Host 2.x Reference](https://docs.microsoft.com/en-us/azure/azure-functions/functions-host-json)
- [Azure Functions Zip Deployment](https://docs.microsoft.com/en-us/azure/azure-functions/deployment-zip-push)
