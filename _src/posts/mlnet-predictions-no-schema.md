---
title: Make predictions with ML.NET models without defining schema classes
description: Use the .NET DataFrame API to make predictions with ML.NET models without defining model input and output schema classes. 
date: 2021-09-16 18:00:00
tags: dotnet, machine learning, mlnet, artificial intelligence
---

## Introduction

To make predictions with ML.NET models you often have to define schema classes for your model inputs and outputs. In previous posts I wrote how you can [use Netron to inspect an ML.NET model](inspect-mlnet-models-netron.html) to determine the name and types of your model inputs and outputs. If you have data samples of what your data input and output look like in JSON format, you can [automate the generation of model input and output classes by using Visual Studio's "Paste JSON as Classes" feature](vs-automate-mlnet-schema-generation.html). However, what if you want to make predictions without defining these classes? In this post I'll show how you can use the .NET DataFrame API to make predictions with ML.NET models without having to create model input and output classes. Code snippets are in F# but notebooks with complete [C#](https://github.com/lqdev/mlnet-noschema-predictions/blob/main/CSharp-NB.ipynb) & [F#](https://github.com/lqdev/mlnet-noschema-predictions/blob/main/FSharp-NB.ipynb) code can be found in the [mlnet-noschema-predictions repo](https://github.com/lqdev/mlnet-noschema-predictions) on GitHub.

## Install and reference packages

In addition to the [Microsoft.ML](https://www.nuget.org/packages/Microsoft.ML/) ML.NET NuGet package, you'll also need the [Microsoft.Data.Analysis](https://www.nuget.org/packages/Microsoft.Data.Analysis/) NuGet package to use the .NET DataFrame API. For more information on the .NET DataFrame API, see [an introduction to DataFrame](https://devblogs.microsoft.com/dotnet/an-introduction-to-dataframe/).

Once your packages are installed, reference them in your application.

```fsharp
open Microsoft.ML
open Microsoft.Data.Analysis
```

## Initialize MLContext and load the model

The `MLContext` is the entrypoint of ML.NET applications. Use it to load your model. The model used in this case categorizes sentiment as positive or negative. See the [use Netron to inspect an ML.NET model](inspect-mlnet-models-netron.html) blog post to learn more about the model.

```fsharp
let ctx = MLContext()
let model,schema = ctx.Model.Load("sentiment_model.zip")
```

Both the model and the input schema are returned when you load the model. The input schema is a [DataViewSchema](https://docs.microsoft.com/dotnet/api/microsoft.ml.dataviewschema?view=ml-dotnet) object containing a collection of [Columns](https://docs.microsoft.com/dotnet/api/microsoft.ml.dataviewschema.column?view=ml-dotnet).

## Define input and output column names

The input and output column names are for the DataFrames containing your input data and predictions. They help the model map the input and output values.

Use the `schema` which was loaded with the model to get the name of your input columns.

```fsharp
let inputColumnNames = 
    schema 
    |> Seq.map(fun column -> column.Name) 
    |> Array.ofSeq
```

Since this is a binary classification model by default only two columns are returned as part of the prediction:

- Score
- PredictedLabel

You can create an array containing the names of these columns. For more information on default output columns, see the [ML.NET Tasks documentation](https://docs.microsoft.com/dotnet/machine-learning/resources/tasks#binary-classification-inputs-and-outputs).

```fsharp
let outputColumnNames = [| "PredictedLabel" ; "Score" |]
```

## Create input data for predictions

Use the [`LoadCsvFromString`](https://docs.microsoft.com/dotnet/api/microsoft.data.analysis.dataframe.loadcsvfromstring?view=ml-dotnet-preview#Microsoft_Data_Analysis_DataFrame_LoadCsvFromString_System_String_System_Char_System_Boolean_System_String___System_Type___System_Int64_System_Int32_System_Boolean_) method to load your input data into a DataFrame. In this case, there's only one column and data instance so I represent it as a string literal. Additionally, I provide the name of the input columns.

```fsharp
let sampleInput = "This was a very bad steak"

let inputDataFrame = 
    DataFrame.LoadCsvFromString(
        sampleInput, 
        header=false, 
        columnNames=inputColumnNames)
```

## Make predictions

Now that you've loaded your input data, it's time to use the model to make predictions.

```fsharp
let predictionDV = 
    inputDataFrame 
    |> model.Transform 
```

Calling the [`Transform`](https://docs.microsoft.com/dotnet/api/microsoft.ml.itransformer.transform?view=ml-dotnet#Microsoft_ML_ITransformer_Transform_Microsoft_ML_IDataView_) method  returns an [`IDataView`](https://docs.microsoft.com/dotnet/api/microsoft.ml.idataview?view=ml-dotnet) with your predictions. You can then convert the `IDataView` into a DataFrame for further processing with the [`ToDataFrame`](https://docs.microsoft.com/dotnet/api/microsoft.ml.idataviewextensions.todataframe?view=ml-dotnet-preview) method.

```fsharp
let prediction = predictionDV.ToDataFrame(1L, outputColumnNames)
```

The resulting DataFrame should look something like the following:

| index | PredictedLabel | Score |
|---|---|---|
|0 |False | -2.1337974 |

## Conclusion

If you want to load a model and make predictions without defining  classes for your input and output schema's you can load your data into a DataFrame using the .NET DataFrame API. While this solution works, because DataFrames and IDataViews process data differently, I haven't tested whether this solution would scale for larger data sets.
