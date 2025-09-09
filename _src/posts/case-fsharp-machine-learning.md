---
post_type: "article" 
title: The Case for Doing Machine Learning with F#
published_date: 2018-12-14 23:34:50 -05:00
tags: [mlnet,machine learning,fsharp,dotnet,programming,python,ai,dotnetcore,artificial intelligence,fsadvent,artificialintelligence,machinelearning,data science,functional programming]
---

## Introduction

This post is part of the 2018 [FsAdvent](https://sergeytihon.com/tag/fsadvent/) series organized by [Sergey Tihon](https://twitter.com/sergey_tihon).

When searching for tools and languages to implement machine learning applications, there are numerous options to choose from each with their own set of advantages and disadvantages. Out of all, however, Python seems to be the most salient. Not only is it a popular language but also many of the tools available for machine learning are either implemented in Python or support it in some capacity whether it's native or through community libraries. Very rarely though is F# mentioned in these discussions despite having many of the features that make languages like Python so loved and extending them to empower users. In this writeup, I will do a short review of many of the advantages of Python such as succinctness, platform support, library availability as well as many others and compare it to F#'s capabilities.

![2017 Top Languages](https://cdn.lqdev.tech/files/images/case-fsharp-ml-0.PNG)

## Python's Advantages

### Learning Curve

One of the reasons why Python is so widely adopted is its learning curve. Whether an individual knows how to program or not, at times, Python can look like pseudocode making it accessible to not only readers but also writers. As with anything the more complex the task, the steeper the learning curve. However, at a simpler level, Python makes it as easy as possible to get started. 

Although at first it may not appear to be the case with F#, the learning curve is not much steeper than that of Python. The syntax can sometimes look intimidating to individuals, but the steepest part of the learning curve doesn't necessarily come from the language itself but rather from the way of reasoning about the logic of the programs. As a functional language, there is somewhat of a paradigm shift from that of a procedural execution model. Below is an example that defines a function that doubles an integer in both languages.  

##### Python

```python
def double(x):
    return x * 2
```

##### FSharp

```fsharp
let double x = 
    x * 2
```

As it can be seen, despite some minor syntax and character differences the functions are essentially the same.  

### Intended Purpose

Depending on the task at hand, some languages are more adept for handling respective tasks. For example, R and Julia are excellent languages when performing statistical tasks. However, outside of those types of tasks their abilities are more limited. Python, being a general-purpose language means that not only can you use it for machine learning tasks but also to build n-tier applications entirely in Python without having to worry about integrations, plugins or having to learn an entirely different language to perform such actions. 

Similarly, F# is a general-purpose language which allows you to build web and console applications as well as machine learning applications all from the comfort of the same ecosystem.

### Strong Library Support

When performing data analysis and machine learning, practicioners use a variety of libraries for their development such as [NumPy](http://www.numpy.org/) and [Pandas](https://pandas.pydata.org/) for data wrangling, [scikit-learn](https://scikit-learn.org/stable/index.html) for machine learning algorithms and [matplotlib](https://matplotlib.org/) for creating visualizations. Although all of these tasks could most certainly be implemented from scratch, libraries speed up the development process allowing practitioners to focus more on the domain and experiment with the models that best solve the respective problem they are facing.  

F#, like Python has exceptional library support, specifically as it regards data science and machine learning. [FsLab](https://fslab.org/) is a collection of open source F# packages for machine learning that contain libraries such as [FSharp.Data](https://fsharp.github.io/FSharp.Data/) and [Deedle](https://fslab.org/Deedle/) for data wrangling, [Math.NET Numerics](https://numerics.mathdotnet.com/) for machine learning algorithms and [XPlot](https://fslab.org/XPlot/) to help with data visualization. Furthermore, at Build 2018, Microsoft released [ML.NET](https://dotnet.microsoft.com/apps/machinelearning-ai/ml-dotnet), an open-source, cross-platform machine learning framework for .NET which allows .NET users to not only perform machine learning tasks within .NET but also allows extensibility via Tensorflow, ONNX and Python extensions. For a more detailed writup on using ML.NET with F#, check out the post [A Christmas Classifier](https://towardsdatascience.com/f-advent-calendar-a-christmas-classifier-72fb9e9d8f23) by Willie Tetlow or my post [Classification with F# ML.NET Models](http://luisquintanilla.me/2018/06/13/mlnet-classification-fsharp/).

Libraries for F# are also not confined only to those written in F#. In many instances, because F# is a .NET language, there is interoperability with C# libraries further extending the capabilities of F#.

### Platform Support

One of the things that makes Python so attractive is that it runs cross-platform. It does not matter whether you're on Windows, Mac or Linux; Python code runs the same. That being said though, not all platforms are created equal and although it is possible to run Python code on all platforms, essential libraries such as NumPy, Pandas and scikit-learn run best on Unix environments. It is possible to run them on Windows but the set up is not as straightforward. 

As a .NET Language, F# runs on Windows. With the help of the Mono runtime and most recently .NET Core, it also runs on Mac and Linux. Like Python, depending on the runtime and dependencies used by the respective software packages there may be limitations as to which platform code can be run on. However, from my experience most of the FsLab set of packages work cross-platform. 

### Succintness

Productivity is an important measure of a language. One way to achieve it is to write logic using the least number of characters. This is where Python shines. As a dynamically typed, whitespace ruled language, Python does not require developers to declare types associated with the objects and variables defined nor does it require the use of brackets and any other special characters. As expected, this allows for developers to write the same logic with less characters much faster. 

Unlike Python, F# is a statically typed language. However, thanks to type inference and the help of the compiler, writing programs often does not require developers to explicitly define what types objects and variables are. Additionally, it is a whitespace ruled language therefore removing the need for brackets and additional characters further speeding up the development process in a safe manner.

### Immediate Feedback

Writing safe and effective code takes time and experience. However, even the most experienced developers often makes mistakes. Therefore, getting immediate feedback before adding certain logic to programs goes a long way to making code that is safe, efficient and tested.  

#### Read-Evaluate-Print Loop (REPL)

One way in which both Python and F# provide immediate feedback is via the command line using the Read-Evaluate-Print Loop (REPL). The REPL is a programming environment that reads the user input, evaluates it and prints out the results, hence the name. Below are screenshots of what that environment looks like using both Python and F#.

##### Python

![](https://cdn.lqdev.tech/files/images/case-fsharp-ml-1.png)

##### FSharp

![](https://cdn.lqdev.tech/files/images/case-fsharp-ml-2.png)

As it can be seen, getting that immediate feedback makes it easier to see whether the code is behaving the way it should. Although useful, this environment is not ideal when experimenting and making tweaks to the code. Additionally, because of the lack of a graphical user interface, the navigation can be less than ideal. Fortunately there is a solution out there that provides the same level of interactivity along with a graphical user interface that allows for ad-hoc experimentation and re-running of code at different points in time which is essential when developing machine learning applications.

#### Jupyter Notebooks

Much of machine learning deals with experimentation. Experimentation involves having a way to tweak parameters and evaluate the results. In addition, experiments should have a way of being documented and reproduced. As alluded to previously, such development environment and capabilities can be found in [Jupyter Notebook](https://jupyter.org/). 

As mentioned on the project's website: 

> The Jupyter Notebook is an open-source web application that allows you to create and share documents that contain live code, equations, visualizations and narrative text. Uses include: data cleaning and transformation, numerical simulation, statistical modeling, data visualization, machine learning, and much more.

Jupyter Notebooks work with various languages which include but are not limited to Python and F# and can be run both on local machines as well as via a hosted service. One such service is Microsoft's [Azure Notebooks](https://notebooks.azure.com/) which allows you to use Jupyter Notebooks in the cloud for free. All you need is to have a Microsoft account. Below are screenshots of what that environment looks like in both Python and F#.   

##### Python

![](https://cdn.lqdev.tech/files/images/case-fsharp-ml-3.png)

##### FSharp

![](https://cdn.lqdev.tech/files/images/case-fsharp-ml-4.png)

## Beyond Python

F#, is comparable to Python on many of the features that make Python a great language for both development and machine learning. However, there are some areas where F# provides additional safety and functionality that greatly improve the way in which machine learning applications are built. 

### Typing

As previously mentioned, F# is a statically typed language. However, it also makes use of type inference which means that declaring types is not always required. With the help of the compiler, based on the structure of functions and variables it is possible to determine which type an object or variable is. This has several advantages. One advantage of being strongly typed is that when the types are declared the code becomes self-documented because it is easier to deduce what functions are doing based on the types being passed in and returned. Another advantage is that it makes it harder to write bad code. Having the compiler help you when you write your code allows you to find errors prior to compilation or running the code. This along with the REPL gives you additional reassurance that your code is executing the intended logic. 

Python is making strides in acquiring some of that functionality with the introduction of type hints in version 3.6. However, this has not always been a core feature of the language and is only in its nascent stages. 

### Immutability

As a functional language, immutability is something that is a native part of the language. While in some cases it can change the way in which code is written, immutability has one advantage, especially when it comes to machine learning. With immutability, parallelization can be fully exploited for those algorithms which take advantage of it. 

## Conclusion

In this writeup, I went over how F# is comparable to Python on many of the features that make it such a popular language such as library support, succinctness, general purpose and interactivity. However, F# has additional capabilities such as static typing and immutability that further enhance its capabilities as a language for machine learning. That is not to say that one is better than the other as they both are more adept for performing certain tasks. When it comes to machine learning it does become a matter of choice as they are both robust, powerful and strongly supported languages. Therefore next time you're looking to build a machine learning application, hopefully you give F# a try. Happy coding!

###### Resources

[2018 Top Programming Languages](https://spectrum.ieee.org/at-work/innovation/the-2018-top-programming-languages)  
[Get Programming with F#](https://www.manning.com/books/get-programming-with-f-sharp)  
[ML.NET Machine Learning Samples](https://github.com/dotnet/machinelearning-samples)