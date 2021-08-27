---
title: Automate ML.NET model schema generation from sample JSON data with Visual Studio
description: Use "Paste JSON As Classes" in Visual Studio to automatically generate model input and output schema classes for your ML.NET models using JSON data samples.
date: 2021-08-26 18:00:00
tags: dotnet, machine learning, mlnet, artificial intelligence, tooling, visual studio
---

## Introduction

When using ML.NET models to make predictions, you often have to define classes for the model's input and output schema. In a previous post, I showed how you can [use Netron to inspect ML.NET models](/posts/inspect-mlnet-models-netron.html) and manually define classes to represent the input and output schema for your model. That works for models that don't have a lot of features. However, as the number of features grows, it can become cumbersome to define these classes. Visual Studio has a feature that can help automate that process. Assuming you have a sample of your input and output data in JSON format, you can leverage a built-in feature "Paste JSON As Classes" to take the sample and convert it to a class. In this post, I'll show how to do that.

## Prerequisites

- Visual Studio 2019. Though I haven't tested with VS2022, I assume the "Past JSON as class" feature is also available there.

## Convert sample JSON data to classes

In this post, I'll work with the *sentiment_model.zip* model to classify sentiment, which you can find in the [dotnet/samples](https://github.com/dotnet/samples/blob/main/machine-learning/models/sentimentanalysis/sentiment_model.zip) repo.

The model input has 3 columns:

- SentimentText (string)
- Label (boolean)
- SamplingKeyColumn (float32)

The model output has at least 2 columns since it uses a binary classification algorithm. For information on expected output columns based on the machine learning algorithm/task, see the [ML.NET tasks article](https://docs.microsoft.com/dotnet/machine-learning/resources/tasks#binary-classification-inputs-and-outputs).

- Score (Single). Single is a single-precision floating-point number.
- PredictedLabel (boolean)

With that in mind, let's then assume that we have sample input data in JSON format that looks as follows.

```json
{
    "SentimentText": "This was a very bad steak",
    "Label": false,
    "SamplingKeyColumn": 1.0
}
```

Inside a C# project in Visual Studio:

1. Create a C# class
   1. Right-click your project.
   2. Select **Add > Class**.
   3. Provide a name for your class in the New Item dialog.
2. Copy the input JSON data sample to your clipboard.
3. Place your cursor inside the namespace block of your newly created class.
4. In the Visual Studio toolbar, select **Edit > Paste Special > Paste JSON as Classes**.
5. The result should look similar to the following:

```csharp
 public class Rootobject
 {
     public string SentimentText { get; set; }
     public bool Label { get; set; }
     public float SamplingKeyColumn { get; set; }
 }
```

6. Rename the class to something more descriptive like `ModelInput`. The class should look similar to the following:

```csharp
 public class ModelInput
 {
     public string SentimentText { get; set; }
     public bool Label { get; set; }
     public float SamplingKeyColumn { get; set; }
 }
```

7. Create a JSON data sample for your output. In this case it'd look something like:

```json
{
    "Score": 1.0,
    "PredictedLabel": false
}
```

1. Repeat steps 1-6 for your output JSON data sample. The resulting class should look similar to the following:

```csharp
 public class ModelOutput
 {
     public float Score { get; set; }
     public bool PredictedLabel { get; set; }
 }
```

It's important to note that the name of the class does not matter so long as the column names and types are the same the ones the model expects.

At this point, you can go through the process of [using the model to make predictions on new data](https://docs.microsoft.com/dotnet/machine-learning/how-to-guides/machine-learning-model-predictions-ml-net).

## Conclusion

Although the model used in this post does not have many columns, when you have many columns "Paste JSON As Classes" can significantly simplify the process of creating your input and output schema classes. Happy coding!