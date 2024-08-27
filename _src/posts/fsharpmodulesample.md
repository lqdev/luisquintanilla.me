---
post_type: "article" 
title: Organizing F# Modules Into Separate Files
published_date: 2018-06-05 20:56:52
tags: [dotnet,fsharp,programming,development,functionalprogramming,dotnetcore]
---

# Introduction

After watching [Jeff Fritz's](https://twitter.com/csharpfritz) F# Friday [Stream](https://www.twitch.tv/videos/268107540), I was inspired to do some tinkering of my own. I have looked at F# in the past, but often times have not done much with it. After the stream, I decided to keep plugging away at it and one of the first things I thought about doing was organizing my code into separate files. As simple as it may seem, it was no easy task and while there are many different ways of doing it, the method described in this writeup is one of the easiest I found. Keep in mind though that this may not be the appropriate way of doing it although it seems to work nicely for our purposes. In this solution I will create a set of mathematical operations that will be organized into a single module that will then be imported and used inside of my console application. Source code for this sample solution can be found at this [link](https://github.com/lqdev/fsharpmoduledemo).

## Prerequisites

This solution was built on a Mac, but should work on both Windows and Linux environments.

- [.NET Core SDK](https://www.microsoft.com/net/download/macos)
- [Ionide Extension - Option 1](https://fsharp.org/use/mac/)

## Create Project

Once you have everything installed, you can create a new F# project by entering the following command into the console.

```bash
dotnet new console -lang f#
```

## Create Module

The goal of this solution will be to create functions that add, subtract, multiply and divide two numbers. Since all of these functions are mathematical operations, we can organize them inside a single module. Therefore, we can create a new file inside of our project called `Math.fs` which will contain all of our functions.

```fsharp
module Math
    let add x y = x + y
    let subtract x y = x - y
    let multiply x y = x * y
    let divide x y = x / y
```

## Compile File

Now that our file has been created, we need to add it to our list of files to be compiled by our project. To do so, we can add an entry for our `Math.fs` file to the `ItemGroup` property inside our `fsproj` file. The contents of the file should look as follows after the addition:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp2.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include="Math.fs" />
    <Compile Include="Program.fs" />
  </ItemGroup>

</Project>
```

## Import Module Into Console Application

Once we have our module created and our `fsproj` file configured, we can call the module from our console application. To do so, we need to first import it into our `Program.fs` file.

```fsharp
open Math
```

Once imported, we can call the `Math` module functions from our `main` function in the `Program.fs` file.

```fsharp
    printfn "Add: %i" (add 1 2)
    printfn "Subtract: %i" (subtract 1 2)
    printfn "Multiply: %i" (multiply 1 2)
    printfn "Divide: %i" (divide 4 2)
```

## Run Application

Now that everything is set up, we can build and run our console application with the following commands:

```bash
dotnet build
dotnet run
```

The output should be the following:

```bash
Add: 3
Subtract: -1
Multiply: 2
Divide: 2
```

## Conclusion

In this writeup, I went over how to create an F# console application that organizes a set of mathematical functions into a module whose contents are stored in a separate file. This is good for organization and to reduce clutter. Happy coding!


