---
title: Use machine learning to categorize web links with F# and ML.NET
date: 2019-12-17 19:59:06
tags: dotnet,dotnet-core,machine-learning,ai,artificial-intelligence,ml,fsharp,functional-programming
---

## Introduction

This post is part of [F# Advent 2019](https://sergeytihon.com/2019/11/05/f-advent-calendar-in-english-2019/). Thank you [Sergey Tihon](https://twitter.com/sergey_tihon) for organizing this and the rest of the contributors for producing interesting, high-quality content.

I scan and read articles on a constant basis, such as those published as part of F# Advent. Those that I find interesting, or I want to save for later, I bookmark using [Pocket](https://getpocket.com/). One of the neat features it provides is tagging. You can add as many tags as you want to organize the bookmarked links. When I first started using the service, I was fairly good at adding tags. However, I've gotten lazy and don't do it as much. It would be nice if bookmarked links could automatically be categorized for me without having to provide the tags manually. Using machine learning, this task can be automated. In this writeup, I will show how to build a machine learning model using [ML.NET](https://dotnet.microsoft.com/apps/machinelearning-ai/ml-dotnet), a .NET, open-source, cross-platform machine learning framework to automatically categorize web links / articles.

## Prerequisites

This application was built on a Windows 10 PC, but should work cross-platform.

- [.NET Core SDK 2.1+](https://dotnet.microsoft.com/download)
- [Visual Studio Code](https://code.visualstudio.com/Download)
- [Ionide](http://ionide.io/)
- [Microsoft.ML NuGet package](https://www.nuget.org/packages/Microsoft.ML/)

## Create the solution

Make a new directory and create a solution by using the .NET CLI.

```powershell
mkdir FsAdvent2019
cd FsAdvent2019
dotnet new sln
```

Then, create an F# Console application.

```powershell
dotnet new console -o FsAdvent2019 -lang f#
```

Navigate to the console application directory and install the Microsoft.ML NuGet package.

```powershell
cd FsAdvent2019
dotnet add package Microsoft.ML -v 1.4.0
```

## Get the data

[Click on this link to download and unzip the data anywhere on your PC](https://archive.ics.uci.edu/ml/machine-learning-databases/00359/NewsAggregatorDataset.zip).

The data contains information about several articles that are separated into four categories: business (b), science and technology (t), entertainment (e) and health (h). Visit the [UCI Machine Learning repository website](https://archive.ics.uci.edu/ml/datasets/News+Aggregator) to learn more about the dataset.

Below is a sample of the data.

```text
ID    Title    Url    Publisher    Category    Story    Hostname    Timestamp
2	Fed's Charles Plosser sees high bar for change in pace of tapering	http://www.livemint.com/Politics/H2EvwJSK2VE6OF7iK1g3PP/Feds-Charles-Plosser-sees-high-bar-for-change-in-pace-of-ta.html	Livemint	b	ddUyU0VZz0BRneMioxUPQVP6sIxvM	www.livemint.com	1394470371207
3	US open: Stocks fall after Fed official hints at accelerated tapering	http://www.ifamagazine.com/news/us-open-stocks-fall-after-fed-official-hints-at-accelerated-tapering-294436	IFA Magazine	b	ddUyU0VZz0BRneMioxUPQVP6sIxvM	www.ifamagazine.com	1394470371550
4	Fed risks falling 'behind the curve', Charles Plosser says	http://www.ifamagazine.com/news/fed-risks-falling-behind-the-curve-charles-plosser-says-294430	IFA Magazine	b	ddUyU0VZz0BRneMioxUPQVP6sIxvM	www.ifamagazine.com	1394470371793
```

Inside the console application directory, create a new directory called *data* and copy the *newsCorpora.csv* file to it.

```powershell
mkdir data
```

## Define the schema

Open the *Program.fs* file and add the following `open` statements at the top.

```fsharp
open Microsoft.ML
open Microsoft.ML.Data
```

Directly below the `open` statements, define the data schema of the input and output of the machine learning model as records called `ModelInput` and `ModelOutput` respectively.

```fsharp
[<CLIMutable>]
type ModelInput = {
    [<LoadColumn(1)>]
    Title:string
    [<LoadColumn(2)>]
    Url:string
    [<LoadColumn(3)>]
    Publisher:string
    [<LoadColumn(4)>]
    Category:string
    [<LoadColumn(6)>]
    Hostname:string
}

[<CLIMutable>]
type ModelOutput = {
    PredictedLabel: string
}
```

As input, only the **Title**, **Url**, **Publisher** and **Hostname** columns are used to train the machine learning model and make predictions. The label or value to predict in this case is the **Category**. When a prediction is output by the model, its value is stored in a column called **PredictedLabel**.

## Create the application entry point

The `MLContext` is the entry point of all ML.NET applications which binds all tasks like data loading, data transformations, model training, model evaluation, and model saving/loading.

Inside of the `main` function, create an instance of `MLContext`.

```fsharp
let mlContext = MLContext()
```

## Load the data

Once the `MLContext` is initialized, use the `LoadFromTextFile` function and provide the path to the file containing the data.

```fsharp
let data = mlContext.Data.LoadFromTextFile<ModelInput>("data/newsCorpora.csv")
```

## Create training and test datasets

It's often good practice to split the data into train and test sets. The goal of a machine learning model is to accurately make predictions on data it has not seen before. Therefore, making predictions using inputs that are the same as those it was trained on may provide misleading accuracy metrics.

Use the `TrainTestSplit` to split the data into train / test sets with 90% of the data used for training and 10% used for testing.

```fsharp
let datasets = mlContext.Data.TrainTestSplit(data,testFraction=0.1)
```

## Define the transformation and algorithm pipelines

Now that the data is split, define the set of transformations to be applied to the data. The purpose of transforming the data is to convert it into numbers which are more easily processed by machine learning algorithms.

### Preprocessing pipeline

The preprocessing pipeline contains the series of transformations that take place before training the model. To create a pipeline, initialize an `EstimatorChain` and append the desired transformations to it.

```fsharp
let preProcessingPipeline = 
    EstimatorChain()
        .Append(mlContext.Transforms.Text.FeaturizeText("FeaturizedTitle","Title"))
        .Append(mlContext.Transforms.Text.FeaturizeText("FeaturizedUrl","Url"))
        .Append(mlContext.Transforms.Text.FeaturizeText("FeaturizedPublisher","Publisher"))
        .Append(mlContext.Transforms.Text.FeaturizeText("FeaturizedHost","Hostname"))
        .Append(mlContext.Transforms.Concatenate("Features",[|"FeaturizedTitle"; "FeaturizedUrl" ;"FeaturizedPublisher"; "FeaturizedHost"|]))
        .Append(mlContext.Transforms.Conversion.MapValueToKey("Label","Category"))
```

In this preprocessing pipeline, the following transformations are taking place:

1. Convert the *Title*, *Url*, *Publisher* and *Hostname* columns into numbers and store the transformed value into the *FeaturizedTitle*, *FeaturizedUrl*, *FeaturizedPublisher* and *FeaturizedHost* columns respectively.
2. Combine the *FeaturizedTitle*, *FeaturizedUrl*, *FeaturizedPublisher* and *FeaturizedHost* into one column called *Features*.
3. Create a mapping of the text value contained in the *Category* column to a numerical key and store the result into a new column called *Label*.

### Algorithm pipeline

The algorithm pipeline contains the algorithm used to train the machine learning model. In this application, the multiclass classification algorithm used is `LbfgsMaximumEntropy`. To learn more about the algorithm, see the [ML.NET LbfgsMaximumEntropy multiclass trainer API documentation](https://docs.microsoft.com/en-us/dotnet/api/microsoft.ml.trainers.lbfgsmaximumentropymulticlasstrainer?view=ml-dotnet#training-algorithm-details).

```fsharp
let algorithm = 
    mlContext.MulticlassClassification.Trainers.LbfgsMaximumEntropy()
```

### Postprocessing pipeline

The postprocessing pipeline contains the series of transformations to get the output of training into a more readable format. The only transformation performed in this pipeline is mapping back the numerical value mapping of the predicted value into text form. 

```fsharp
let postProcessingPipeline = 
    mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel")
```

### Create training pipeline

Once the pipelines are defined, combine them into a single pipeline which applies all of the transformations to the data with a single function call.

```fsharp
let trainingPipeline = 
    preProcessingPipeline
        .Append(algorithm)
        .Append(postProcessingPipeline)
```

## Train the model

Use the `Fit` function to train the model by applying the set of transformations defined by `trainingPipeline` to the training dataset.

```fsharp
let model =
    datasets.TrainSet |> trainingPipeline.Fit
```

## Evaluate the model

Once the model is trained, evaluate how well it performs against the test dataset. First, use the trained model to get the predicted category by using the `Transform` function. Then, provide the test dataset containing predictions to the `Evaluate` function which calculates the model's performance metrics by comparing the predicted category to the actual category and print some of them out.

```fsharp
let metrics = 
    (datasets.TestSet |> model.Transform)
    |> mlContext.MulticlassClassification.Evaluate

printfn "Log Loss: %f | MacroAccuracy: %f" metrics.LogLoss metrics.MacroAccuracy
```

## Using the model on real data

Create a list of `ModelInput` items and use the `Transform` method to get the predicted category.

```fsharp
let predictions = 
    [
        { 
            Title="A FIRST LOOK AT SURFACE DUO, MICROSOFT’S FOLDABLE ANDROID PHONE"
            Url="https://www.theverge.com/2019/10/3/20895268/microsoft-surface-duo-foldable-phone-dual-screen-android-hands-on-features-price-photos-video"
            Publisher="The Verge"
            Hostname="www.theverge.com" 
            Category = "" 
        }
        { 
            Title="This Shrinking Economy With Low Inflation Is Stuck on Rates"
            Url="https://www.bloomberg.com/news/articles/2019-12-12/when-a-shrinking-economy-and-low-inflation-don-t-mean-rate-cuts?srnd=economics-vp"
            Publisher="Bloomberg"
            Hostname="www.bloomberg.com" 
            Category = "" 
        }
    ] 
    |> mlContext.Data.LoadFromEnumerable
    |> model.Transform
```

Then, create a `Sequence` of `ModelOutput` values and print out the *PredictedLabel* values.

```fsharp
mlContext.Data.CreateEnumerable<ModelOutput>(predictions,false)
|> Seq.iter(fun prediction -> printfn "Predicted Value: %s" prediction.PredictedLabel)
```

The final *Program.fs* file should look as follows:

```fsharp
open System
open Microsoft.ML
open Microsoft.ML.Data

[<CLIMutable>]
type ModelInput = {
    [<LoadColumn(1)>]
    Title:string
    [<LoadColumn(2)>]
    Url:string
    [<LoadColumn(3)>]
    Publisher:string
    [<LoadColumn(4)>]
    Category:string
    [<LoadColumn(6)>]
    Hostname:string
}

[<CLIMutable>]
type ModelOutput = {
    PredictedLabel: string
}

[<EntryPoint>]
let main argv =

    let mlContext = MLContext()

    let data = mlContext.Data.LoadFromTextFile<ModelInput>("data/newsCorpora.csv")

    let datasets = mlContext.Data.TrainTestSplit(data,testFraction=0.1)

    let preProcessingPipeline = 
        EstimatorChain()
            .Append(mlContext.Transforms.Text.FeaturizeText("FeaturizedTitle","Title"))
            .Append(mlContext.Transforms.Text.FeaturizeText("FeaturizedUrl","Url"))
            .Append(mlContext.Transforms.Text.FeaturizeText("FeaturizedPublisher","Publisher"))
            .Append(mlContext.Transforms.Text.FeaturizeText("FeaturizedHost","Hostname"))
            .Append(mlContext.Transforms.Concatenate("Features",[|"FeaturizedTitle"; "FeaturizedUrl" ;"FeaturizedPublisher"; "FeaturizedHost"|]))
            .Append(mlContext.Transforms.Conversion.MapValueToKey("Label","Category"))

    let algorithm = 
        mlContext.MulticlassClassification.Trainers.LbfgsMaximumEntropy()

    let postProcessingPipeline = 
        mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel")

    let trainingPipeline = 
        preProcessingPipeline
            .Append(algorithm)
            .Append(postProcessingPipeline)

    let model =
        datasets.TrainSet |> trainingPipeline.Fit

    let metrics = 
        (datasets.TestSet |> model.Transform)
        |> mlContext.MulticlassClassification.Evaluate

    printfn "Log Loss: %f | MacroAccuracy: %f" metrics.LogLoss metrics.MacroAccuracy

    let predictions = 
        [
            { 
                Title="A FIRST LOOK AT SURFACE DUO, MICROSOFT’S FOLDABLE ANDROID PHONE"
                Url="https://www.theverge.com/2019/10/3/20895268/microsoft-surface-duo-foldable-phone-dual-screen-android-hands-on-features-price-photos-video"
                Publisher="The Verge"
                Hostname="www.theverge.com" 
                Category = "" 
            }
            { 
                Title="This Shrinking Economy With Low Inflation Is Stuck on Rates"
                Url="https://www.bloomberg.com/news/articles/2019-12-12/when-a-shrinking-economy-and-low-inflation-don-t-mean-rate-cuts?srnd=economics-vp"
                Publisher="Bloomberg"
                Hostname="www.bloomberg.com" 
                Category = "" 
            }
        ] 
        |> mlContext.Data.LoadFromEnumerable
        |> model.Transform
 
    mlContext.Data.CreateEnumerable<ModelOutput>(predictions,false)
    |> Seq.iter(fun prediction -> printfn "Predicted Value: %s" prediction.PredictedLabel)

    0 // return an integer exit code
```

## Run the application

This particular model achieved a macro-accuracy of 0.92, where closer to 1 is preferred and log loss of 0.20 where closer to 0 is preferred.

```bash
Log Loss: 0.200502 | MacroAccuracy: 0.927742
```

The predicted values are the following:

```bash
Predicted Value: t
Predicted Value: b
```

Upon inspection, they appear to be correct, science and technology for the first link and business for the second link.

## Conclusion

In this writeup, I showed how to build a machine learning multiclass classification model that categorizes web links using ML.NET. Now that you have a model trained, you can save it and deploy it in another application (desktop, web) that bookmarks links. This model can be further improved and personalized by using data from Pocket which has already been tagged. Happy coding!