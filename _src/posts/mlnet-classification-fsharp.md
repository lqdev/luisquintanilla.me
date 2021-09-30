---
title: Classification with F# ML.NET Models 
published_date: 2018-06-13 18:19:05
tags: fsharp,dotnet,dotnetcore,programming,development,mlnet,machinelearning,artificialintelligence,functionalprogramming
---

# Introduction

In a previous [post](http://luisquintanilla.me/2018/05/11/deploy-netml-docker-aci/), I detailed how to build and deploy C# `ML.NET` models with `Docker` and `ASP.NET Core`. With inspiration from [Jeff Fritz](https://twitter.com/csharpfritz), I have been learning F# for the past week and a half or so. When trying to think of projects to start practicing my F#, porting over the code I had built in C# naturally came to mind. After overcoming many obstacles and with much guidance from [Alan Ball](https://github.com/voronoipotato) and [Isaac Abraham](https://twitter.com/isaac_abraham) whose F# [book](https://www.amazon.com/Get-Programming-guide-NET-developers/dp/1617293997/ref=sr_1_1?ie=UTF8&qid=1528929802&sr=8-1&keywords=get+programming+with+F%23) I highly recommend, I was able to successfully port over the main parts of my code which highlight `ML.NET` functionality. In this writeup, I will port a C# `ML.NET` classification model to F# which predicts the type of flower based on four numerical measurement inputs. I tried to keep the organization of this post nearly identical to that of the C# article where possible. Sample code for this project can be found at the following [link](https://github.com/lqdev/fsmlnetdemo).

## Prerequisites

This project was built on a Linux PC but should work cross-platform on Mac and Windows.

- [.NET Core SDK 2.0+](https://www.microsoft.com/net/download/linux)
- [Ionide Extension - Option 1](https://fsharp.org/use/linux/)
- [ML.NET v 0.2.0](https://www.nuget.org/packages/Microsoft.ML/)

## Setting Up The Project

The first thing we want to do is create a folder for our solution.

```bash
mkdir fsharpmlnetdemo
```

Then, we want to create a solution inside our newly created folder.

```bash
cd fsharpmlnetdemo
dotnet new sln
```

## Building The Model

### Setting Up The Model Project

First, we want to create the project. From the solution folder enter:

```bash
dotnet new console -o model -lang f#
```

Now we want to add this new project to our solution.

```bash
dotnet sln add model/model.fsproj
```

### Adding Dependencies

Since we’ll be using the `ML.NET` framework, we need to add it to our `model` project.

```bash
dotnet add model/model.fsproj package Microsoft.ML
```

### Download The Data

Before we start training the model, we need to download the data we’ll be using to train. We do so by downloading the data file into our root solution directory.

```bash
curl -o iris-data.txt https://archive.ics.uci.edu/ml/machine-learning-databases/iris/iris.data
```

If we take a look at the data file, it should look something like this:

```bash
5.1,3.5,1.4,0.2,Iris-setosa
4.9,3.0,1.4,0.2,Iris-setosa
4.7,3.2,1.3,0.2,Iris-setosa
4.6,3.1,1.5,0.2,Iris-setosa
5.0,3.6,1.4,0.2,Iris-setosa
5.4,3.9,1.7,0.4,Iris-setosa
4.6,3.4,1.4,0.3,Iris-setosa
5.0,3.4,1.5,0.2,Iris-setosa
4.4,2.9,1.4,0.2,Iris-setosa
4.9,3.1,1.5,0.1,Iris-setosa
```

### Train Model

Now that we have all our dependencies set up, it’s time to build our model. I leveraged the demo that is used on the `ML.NET` Getting-Started [website](https://www.microsoft.com/net/learn/apps/machine-learning-and-ai/ml-dotnet/get-started/linux/ubuntu16-04).

#### Defining Data Structures

At the time of this writing, `ML.NET` version `0.2.0` does not fully support F# Records. A workaround for this are mutable classes. Not really inline with F# paradigms, but it should be good enough.

In the `Program.fs` file of our `model` project directory, let’s create two mutable classes called `IrisData` and `IrisPrediction` which will define our features and predicted attribute respectively. Both of them will use `Microsoft.ML.Runtime.Api` to add the property attributes.

Here is what our `IrisData` class looks like:

```fsharp
type IrisData() =
    [<Column(ordinal = "0");DefaultValue>]
    val mutable public SepalLength: float32

    [<Column(ordinal = "1");DefaultValue>]
    val mutable public SepalWidth: float32

    [<Column(ordinal = "2");DefaultValue>]
    val mutable public PetalLength:float32

    [<Column(ordinal = "3");DefaultValue>]
    val mutable public PetalWidth:float32

    [<Column(ordinal = "4",name="Label");DefaultValue>]
    val mutable public Label: string
```

Similarly, here is the `IrisPrediction` class:

```fsharp
type IrisPrediction() =
    [<ColumnName "PredictedLabel";DefaultValue>] val mutable public PredictedLabel : string
```

#### Building Training Pipeline

The way the `ML.NET` computations process is via a sequential pipeline of steps that are performed eventually leading up to the training of the model. We can add that logic inside the `main` function of our `Program.fs` file.

```fsharp
let dataPath = "./iris-data.txt"

// Initialize Compute Graph
let pipeline = new LearningPipeline()

// Load Data
pipeline.Add((new TextLoader(dataPath).CreateFrom<IrisData>separator=','))

// Transform Data
// Assign numeric values to text in the "Label" column, because
// only numbers can be processed during model training
pipeline.Add(new Transforms.Dictionarizer("Label"))

// Vectorize Features
pipeline.Add(new ColumnConcatenator("Features","SepalLength", "SepalWidth", "PetalLength", "PetalWidth"))

// Add Learner
pipeline.Add(new StochasticDualCoordinateAscentClassifier())

// Convert Label back to text
pipeline.Add(new ransforms.PredictedLabelColumnOriginalValueConverterPredictedLabelColumn = "PredictedLabel"))

//Train the model
let model = pipeline.Train<IrisData, IrisPrediction>()
```

#### Testing Our Model

Now that we have our data structures and model trained, it’s time to test it to make sure it's working. Following our training operation, we can add the following code.

```fsharp
// Test data for prediction
let testInstance = IrisData()
testInstance.SepalLength <- 3.3f
testInstance.SepalWidth <- 1.6f
testInstance.PetalLength <- 0.2f
testInstance.PetalWidth <- 5.1f

//Get Prediction
let prediction = model.Predict(testInstance)

//Output Prediction
printfn "Predicted flower type is: %s" prediction.PredictedLabel
```

Our final `Program.fs` file should contain content similar to that below:

```fsharp
open System
open Microsoft.ML
open Microsoft.ML.Runtime
open Microsoft.ML.Runtime.Api
open Microsoft.ML.Data
open Microsoft.ML.Transforms
open Microsoft.ML.Trainers

type IrisData() =

    [<Column(ordinal = "0");DefaultValue>] val mutable public SepalLength: float32
    [<Column(ordinal = "1");DefaultValue>] val mutable public SepalWidth: float32
    [<Column(ordinal = "2");DefaultValue>] val mutable public PetalLength:float32
    [<Column(ordinal = "3");DefaultValue>] val mutable public PetalWidth:float32
    [<Column(ordinal = "4",name="Label");DefaultValue>] val mutable public Label: string


type IrisPrediction() =

    [<ColumnName "PredictedLabel";DefaultValue>] val mutable public PredictedLabel : string

[<EntryPoint>]
let main argv =

    let dataPath = "./iris-data.txt"

    // Initialize Compute Graph
    let pipeline = new LearningPipeline()

    // Load Data
    pipeline.Add((new TextLoader(dataPath)).CreateFrom<IrisData>(separator=','))

    // Transform Data
    // Assign numeric values to text in the "Label" column, because
    // only numbers can be processed during model training
    pipeline.Add(new Transforms.Dictionarizer("Label"))

    // Vectorize Features
    pipeline.Add(new ColumnConcatenator("Features","SepalLength", "SepalWidth", "PetalLength", "PetalWidth"))

    // Add Learner
    pipeline.Add(new StochasticDualCoordinateAscentClassifier())

    // Convert Label back to text
    pipeline.Add(new Transforms.PredictedLabelColumnOriginalValueConverter(PredictedLabelColumn = "PredictedLabel"))

    //Train the model
    let model = pipeline.Train<IrisData, IrisPrediction>()

    // Test data for prediction
    let testInstance = IrisData()
    testInstance.SepalLength <- 3.3f
    testInstance.SepalWidth <- 1.6f
    testInstance.PetalLength <- 0.2f
    testInstance.PetalWidth <- 5.1f

    //Get Prediction
    let prediction = model.Predict(testInstance)

    //Output Prediction
    printfn "Predicted flower type is: %s" prediction.PredictedLabel
    0 // return an integer exit code
```

All set to run. We can do so by entering the following command from our solution directory:

```fsharp
dotnet run -p model/model.fsproj
```

Once the application has been run, the following output should display on the console.

```bash
Automatically adding a MinMax normalization transform, use 'norm=Warn' or 'norm=No' to turn this behavior off.
Using 2 threads to train.
Automatically choosing a check frequency of 2.
Auto-tuning parameters: maxIterations = 9998.
Auto-tuning parameters: L2 = 2.667734E-05.Auto-tuning parameters: L1Threshold (L1/L2) = 0.
Using best model from iteration 1066.Not training a calibrator because it is not needed.
Predicted flower type is: Iris-virginica
```

## Conclusion

In this post, we ported over a C# `ML.NET` classification model to F# which predicts the class of flower based on numerical measurement inputs. While several workarounds needed to be made, `ML.NET` is still in its infancy. As more people become involved and provide feedback hopefully in the near future, F# support and functionality will become more stable. Happy coding!
