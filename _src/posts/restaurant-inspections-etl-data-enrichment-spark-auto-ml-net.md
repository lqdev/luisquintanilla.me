---
title: Restaurant Inspections ETL & Data Enrichment with Spark.NET and ML.NET Automated (Auto) ML
published_date: 2019-09-15 15:12:01
tags: [mlnet,ml,machine-learning,ai,artificial-intelligence,big-data,spark,dotnet, dotnetcore]
---

## Introduction

Apache Spark is an open-source, distributed, general-purpose analytics engine. For years, it has been a staple in the Big Data ecosystem for batch and real-time processing on large datasets. Although native support for the platform is limited to the JVM set of languages, other languages typically used for data processing and analytics like Python and R have plugged into Spark's Interop layer to make use of its functionality. Around the Build 2019 conference, Microsoft announced Spark.NET. Spark.NET provides bindings written for the Spark Interop layer that allow you to work with components like Spark SQL and Spark Streaming inside your .NET applications. Because Spark.NET is .NET Standard 2.0 compliant, it can run operating systems like Windows, Mac and Linux. Spark.NET is an evolution of the Mobius project which provided .NET bindings for Spark.

This sample takes a restaurant violation dataset from the NYC Open Data portal and processes it using Spark.NET. Then, the processed data is used to train a machine learning model that attempts to predict the grade an establishment will receive after an inspection. The model will be trained using ML.NET, an open-source, cross-platform machine learning framework. Finally, data for which no grade currently exists will be enriched using the trained model to assign an expected grade.

The source code for this sample can be found in the [lqdev/RestaurantInspectionsSparkMLNET
GitHub repo](https://github.com/lqdev/RestaurantInspectionsSparkMLNET).

## Pre-requisites

This project was built using Ubuntu 18.04 but should work on Windows and Mac devices.

- [.NET Core 2.1 SDK](https://dotnet.microsoft.com/download/dotnet-core/2.1)
- [Java 8](https://www.java.com/en/download/)
- [Apache Spark 2.4.1 with Hadoop 2.7](https://archive.apache.org/dist/spark/spark-2.4.1/)
- [.NET Spark Worker 0.4.0](https://github.com/dotnet/spark/releases)

### Install Java

Since Spark runs on the JVM, you'll need Java on your PC. The minimum version required is version 8. To install and Java, enter the following command into the terminal:

```bash
sudo apt install openjdk-8-jdk openjdk-8-jre
```

Then, make sure that the recently installed version is the default

```bash
sudo update-alternatives --config java
```

### Download and configure Spark

Download Spark 2.4.1 with Hadoop 2.7 onto your computer. In this case, I'm placing it into my *Downloads* folder.

```bash
wget https://archive.apache.org/dist/spark/spark-2.4.1/spark-2.4.1-bin-hadoop2.7.tgz -O ~/Downloads/spark-2.4.1-bin-hadoop2.7.tgz
```

Extract the contents of the recently downloaded file into the */usr/bin/local* directory.

```bash
sudo tar -xvf ~/Downloads/spark-2.4.1-bin-hadoop2.7.tgz --directory /usr/local/bin
```

### Download and configure .NET Spark Worker

Download the .NET Spark worker onto your computer. In this case, I'm placing it into the *Downloads* folder.

```bash
wget https://github.com/dotnet/spark/releases/download/v0.4.0/Microsoft.Spark.Worker.netcoreapp2.1.linux-x64-0.4.0.tar.gz -O ~/Downloads/Microsoft.Spark.Worker.netcoreapp2.1.linux-x64-0.4.0.tar.gz
```

Extract the contents of the recently downloaded file into the */usr/bin/local* directory.

```bash
sudo tar -xvf ~/Downloads/Microsoft.Spark.Worker.netcoreapp2.1.linux-x64-0.4.0.tar.gz --directory /usr/local/bin
```

Finally, raise the permissions on the `Microsoft.Spark.Worker` program. This is required to execute User-Defined Functions (UDF).

```bash
sudo chmod +x /usr/local/bin/Microsoft.Spark.Worker-0.4.0/Microsoft.Spark.Worker
```

### Configure environment variables

Once you download and configure the pre-requisites, configure their locations in the system as environment variables. Open the *~/.bashrc* file and add the following content at the end of the file.

```bash
export SPARK_PATH=/usr/local/bin/spark-2.4.1-bin-hadoop2.7
export PATH=$SPARK_PATH/bin:$PATH
export HADOOP_HOME=$SPARK_PATH
export SPARK_HOME=$SPARK_PATH
export DOTNET_WORKER_DIR=/usr/local/bin/Microsoft.Spark.Worker-0.4.0
```

## Solution description

### Understand the data

The dataset used in this solution is the [*DOHMH New York City Restaurant Inspection Results*](https://data.cityofnewyork.us/Health/DOHMH-New-York-City-Restaurant-Inspection-Results/43nn-pn8j) and comes from the NYC Open Data portal. It is updated daily and contains assigned and pending inspection results and violation citations for restaurants and college cafeterias. The dataset excludes establishments that have gone out of business. Although the dataset contains several columns, only a subset of them are used in this solution. For a detailed description of the dataset, visit the dataset website.

### Understand the solution

This solution is made up of four different .NET Core applications:

- *RestaurantInspectionsETL*: .NET Core Console application that takes raw data and uses Spark.NET to clean and transform the data into a format that is easier to use as input for training and making predictions with a machine learning model built with ML.NET.
- *RestaurantInspectionsML*: .NET Core Class Library that defines the input and output schema of the ML.NET machine learning model. Additionally, this is where the trained model is saved to.
- *RestaurantInspectionsTraining*: .NET Core Console application that uses the graded data generated by the *RestaurantInspectionsETL* application to train a multiclass classification machine learning model using ML.NET's Auto ML.
- *RestaurantInspectionsEnrichment*: .NET Core Console application that uses the ungraded data generated by the *RestaurantInspectionsETL* application as input for the trained ML.NET machine learning model which predicts what grade an establishment is most likely to receive based on the violations found during inspection.

## Set up the solution

### Create solution directory

Create a new directory for your projects called *RestaurantInspectionsSparkMLNET* and navigate to it with the following command.

```bash
mkdir RestaurantInspectionsSparkMLNET && cd RestaurantInspectionsSparkMLNET
```

Then, create a solution using the `dotnet cli`.

```bash
dotnet new sln
```

To ensure that the 2.1 version of the .NET Core SDK is used as the target framework, especially if you have multiple versions of the .NET SDK installed, create a file called *globals.json* in the *RestaurantInspectionsSparkMLNET* solution directory.

```bash
touch global.json
```

In the *global.json* file, add the following content. Make sure to use the specific version of the SDK installed on your computer. In this case, I have version `2.1.801` installed on my computer. You can use the `dotnet --list-sdks` command to list the installed SDK versions.

```json
{
  "sdk": {
    "version": "2.1.801"
  }
}
```

### Create and configure the ETL project

The ETL project is responsible for taking the raw source data and using Spark to apply a series of transformations to prepare the data to train the machine learning model as well as to enrich data with missing grades.

Inside the *RestaurantInspectionsSparkMLNET* solution directory, create a new console application called *RestaurantInspectionsETL* using the `dotnet cli`.

```bash
dotnet new console -o RestaurantInspectionsETL
```

Add the newly created project to the solution with the `dotnet cli`.

```bash
dotnet sln add ./RestaurantInspectionsETL/
```

Since this project uses the `Microsoft.Spark` NuGet package, use the `dotnet cli` to install it.

```bash
dotnet add ./RestaurantInspectionsETL/ package Microsoft.Spark --version 0.4.0
```

### Create and configure the ML model project

The ML model class library will contain the domain model that defines the schema of model inputs and outputs as well as the trained model itself.

Inside the *RestaurantInspectionsSparkMLNET* solution directory, create a new class library called *RestaurantInspectionsML* using the `dotnet cli`.

```bash
dotnet new classlib -o RestaurantInspectionsML
```

Add the newly created project to the solution with the `dotnet cli`.

```bash
dotnet sln add ./RestaurantInspectionsML/
```

Since this project uses the `Microsoft.ML` NuGet package, use the `dotnet cli` to install it.

```bash
dotnet add ./RestaurantInspectionsML/ package Microsoft.ML --version 1.3.1
```

### Create and configure the ML training project

The purpose of the training project is to use the pre-processed graded data output by the *RestaurantInspectionsETL* project as input to train a multiclass classification model with ML.NET's Auto ML API. The trained model will then be saved in the *RestaurantInspectionsML* directory.

Inside the *RestaurantInspectionsSparkMLNET* solution directory, create a new console application called *RestaurantInspectionsTraining* using the `dotnet cli`.

```bash
dotnet new console -o RestaurantInspectionsTraining
```

Add the newly created project to the solution with the `dotnet cli`.

```bash
dotnet sln add ./RestaurantInspectionsTraining/
```

This project depends on the domain model created in the *RestaurantInspectionsML* project, so you need to add a reference to it.  

```bash
dotnet add ./RestaurantInspectionsTraining/ reference ./RestaurantInspectionsML/
```

Since this project uses the `Microsoft.Auto.ML` NuGet package, use the `dotnet cli` to install it.

```bash
dotnet add ./RestaurantInspectionsTraining/ package Microsoft.ML.AutoML --version 0.15.1
```

### Create and configure the data enrichment project

The data enrichment application uses the trained machine learning model created by the *RestaurantInspectionsTraining* application and use it on the pre-processed ungraded data created by the *RestaurantInspectionsETL* application to predict what grade that establishment is most likely to receive based on the violations found during inspection.

Inside the *RestaurantInspectionsSparkMLNET* solution directory, create a new console application called *RestaurantInspectionsEnrichment* using the `dotnet cli`.

```bash
dotnet new console -o RestaurantInspectionsEnrichment
```

Add the newly created project to the solution with the `dotnet cli`.

```bash
dotnet sln add ./RestaurantInspectionsEnrichment/
```

This project depends on the domain model created in the *RestaurantInspectionsML* project, so you need to add a reference to it.

```bash
dotnet add ./RestaurantInspectionsEnrichment/ reference ./RestaurantInspectionsML/
```

This uses the following NuGet packages:

- Microsoft.Spark
- Microsoft.ML.LightGBM (This is not required but predictions may fail if the final model is a LightGBM model).

Install the packages with the following commands:

```bash
dotnet add ./RestaurantInspectionsEnrichment/ package Microsoft.Spark --version 0.4.0
dotnet add ./RestaurantInspectionsEnrichment/ package Microsoft.ML.LightGBM --version 1.3.1
```

## Build ETL application

The first step is to prepare the data. To do so, apply a set of transformations using Spark.NET.

### Download the data

Navigate to the *RestaurantInspectionsETL* project and create a *Data* directory.

```bash
mkdir Data
```

Then, download the data into the newly created *Data* directory.

```bash
wget https://data.cityofnewyork.us/api/views/43nn-pn8j/rows.csv?accessType=DOWNLOAD -O Data/NYC-Restaurant-Inspections.csv
```

### Build the ETL pipeline

Add the following usings to the *Program.cs* file.

```csharp
using System;
using System.IO;
using Microsoft.Spark.Sql;
using static Microsoft.Spark.Sql.Functions;
```

Not all of the columns are relevant. Inside the `Main` method of the *Program.cs* file, define the columns to be removed.

```csharp
string[] dropCols = new string[]
{
    "CAMIS",
    "CUISINE DESCRIPTION",
    "VIOLATION DESCRIPTION",
    "BORO",
    "BUILDING",
    "STREET",
    "ZIPCODE",
    "PHONE",
    "ACTION",
    "GRADE DATE",
    "RECORD DATE",
    "Latitude",
    "Longitude",
    "Community Board",
    "Council District",
    "Census Tract",
    "BIN",
    "BBL",
    "NTA"
};
```

The entrypoint of Spark applications is the `SparkSession`. Create `SparkSession` inside the `Main` method of the *Program.cs* file.

```csharp
var sc =
    SparkSession
        .Builder()
        .AppName("Restaurant_Inspections_ETL")
        .GetOrCreate();
```

Then, load the data stored in the *NYC-Restaurant-Inspections.csv* file into a `DataFrame`.

```csharp
DataFrame df =
    sc
    .Read()
    .Option("header", "true")
    .Option("inferSchema", "true")
    .Csv("Data/NYC-Restaurant-Inspections.csv");
```

`DataFrames` can be thought of as tables in a database or sheets in Excel. Spark has various ways of representing data but `DataFrames` are the format supported by Spark.NET. Additionally, the `DataFrame` API is higher-level and easier to work with.

Once the data is loaded, get rid of the data that are not needed by creating a new `DataFrame` that excludes the `dropCols` as well as missing values.

```csharp
DataFrame cleanDf =
    df
        .Drop(dropCols)
        .WithColumnRenamed("INSPECTION DATE","INSPECTIONDATE")
        .WithColumnRenamed("INSPECTION TYPE","INSPECTIONTYPE")
        .WithColumnRenamed("CRITICAL FLAG","CRITICALFLAG")
        .WithColumnRenamed("VIOLATION CODE","VIOLATIONCODE")
        .Na()
        .Drop();
```

Typically, machine learning models expect values to be numerical, so in the ETL step try to convert as many values as possible into numerical values. The `CRITICALFLAG` column contains "Y"/"N" values that can be encoded as 0 and 1.

```csharp
DataFrame labeledFlagDf =
    cleanDf
        .WithColumn("CRITICALFLAG",
            When(Functions.Col("CRITICALFLAG") == "Y",1)
            .Otherwise(0));
```

This dataset contains one violation per row which correspond to different inspections. Therefore, all of the violations need to be aggregated by business and inspection.

```csharp
DataFrame groupedDf =
    labeledFlagDf
        .GroupBy("DBA", "INSPECTIONDATE", "INSPECTIONTYPE", "CRITICALFLAG", "SCORE", "GRADE")
        .Agg(Functions.CollectSet(Functions.Col("VIOLATIONCODE")).Alias("CODES"))
        .Drop("DBA", "INSPECTIONDATE")
        .WithColumn("CODES", Functions.ArrayJoin(Functions.Col("CODES"), ","))
        .Select("INSPECTIONTYPE", "CODES", "CRITICALFLAG", "SCORE", "GRADE");  
```

Now that the data is in the format used to train and make predictions, split the cleaned `DataFrame` into two new `DataFrames`, graded and ungraded. The graded dataset is the data used for training the machine learning model. The ungraded data will be used for enrichment.

```csharp
DataFrame gradedDf =
    groupedDf
    .Filter(
        Col("GRADE") == "A" |
        Col("GRADE") == "B" |
        Col("GRADE") == "C" );

DataFrame ungradedDf =
    groupedDf
    .Filter(
        Col("GRADE") != "A" &
        Col("GRADE") != "B" &
        Col("GRADE") != "C" );  
```

Take the `DataFrames` and save them as csv files for later use.

```csharp
var timestamp = ((DateTimeOffset) DateTime.UtcNow).ToUnixTimeSeconds().ToString();

var saveDirectory = Path.Join("Output",timestamp);

if(!Directory.Exists(saveDirectory))
{
    Directory.CreateDirectory(saveDirectory);
}

gradedDf.Write().Csv(Path.Join(saveDirectory,"Graded"));

ungradedDf.Write().Csv(Path.Join(saveDirectory,"Ungraded"));
```

### Publish and run the ETL application

The final *Program.cs* file should look as follows:

```csharp
using System;
using System.IO;
using Microsoft.Spark.Sql;
using static Microsoft.Spark.Sql.Functions;

namespace RestaurantInspectionsETL
{
    class Program
    {
        static void Main(string[] args)
        {
            // Define columns to remove
            string[] dropCols = new string[]
            {
                "CAMIS",
                "CUISINE DESCRIPTION",
                "VIOLATION DESCRIPTION",
                "BORO",
                "BUILDING",
                "STREET",
                "ZIPCODE",
                "PHONE",
                "ACTION",
                "GRADE DATE",
                "RECORD DATE",
                "Latitude",
                "Longitude",
                "Community Board",
                "Council District",
                "Census Tract",
                "BIN",
                "BBL",
                "NTA"
            };

            // Create SparkSession
            var sc =
                SparkSession
                    .Builder()
                    .AppName("Restaurant_Inspections_ETL")
                    .GetOrCreate();

            // Load data
            DataFrame df =
                sc
                .Read()
                .Option("header", "true")
                .Option("inferSchema", "true")
                .Csv("Data/NYC-Restaurant-Inspections.csv");

            //Remove columns and missing values
            DataFrame cleanDf =
                df
                    .Drop(dropCols)
                    .WithColumnRenamed("INSPECTION DATE","INSPECTIONDATE")
                    .WithColumnRenamed("INSPECTION TYPE","INSPECTIONTYPE")
                    .WithColumnRenamed("CRITICAL FLAG","CRITICALFLAG")
                    .WithColumnRenamed("VIOLATION CODE","VIOLATIONCODE")
                    .Na()
                    .Drop();

            // Encode CRITICAL FLAG column
            DataFrame labeledFlagDf =
                cleanDf
                    .WithColumn("CRITICALFLAG",
                        When(Functions.Col("CRITICALFLAG") == "Y",1)
                        .Otherwise(0));

             // Aggregate violations by business and inspection
            DataFrame groupedDf =
                labeledFlagDf
                    .GroupBy("DBA", "INSPECTIONDATE", "INSPECTIONTYPE", "CRITICALFLAG", "SCORE", "GRADE")
                    .Agg(Functions.CollectSet(Functions.Col("VIOLATIONCODE")).Alias("CODES"))
                    .Drop("DBA", "INSPECTIONDATE")
                    .WithColumn("CODES", Functions.ArrayJoin(Functions.Col("CODES"), ","))
                    .Select("INSPECTIONTYPE", "CODES", "CRITICALFLAG", "SCORE", "GRADE");

            // Split into graded and ungraded DataFrames
            DataFrame gradedDf =
                groupedDf
                .Filter(
                    Col("GRADE") == "A" |
                    Col("GRADE") == "B" |
                    Col("GRADE") == "C" );

            DataFrame ungradedDf =
                groupedDf
                    .Filter(
                        Col("GRADE") != "A" &
                        Col("GRADE") != "B" &
                        Col("GRADE") != "C" );

            // Save DataFrames
            var timestamp = ((DateTimeOffset) DateTime.UtcNow).ToUnixTimeSeconds().ToString();

            var saveDirectory = Path.Join("Output",timestamp);

            if(!Directory.Exists(saveDirectory))
            {
                Directory.CreateDirectory(saveDirectory);
            }

            gradedDf.Write().Csv(Path.Join(saveDirectory,"Graded"));

            ungradedDf.Write().Csv(Path.Join(saveDirectory,"Ungraded"));
        }
    }
}
```

Publish the application with the following command.

```bash
dotnet publish -f netcoreapp2.1 -r ubuntu.18.04-x64
```

Run the application with `spark-submit`.

```bash
spark-submit --class org.apache.spark.deploy.dotnet.DotnetRunner --master local bin/Debug/netcoreapp2.1/ubuntu.18.04-x64/publish/microsoft-spark-2.4.x-0.4.0.jar dotnet bin/Debug/netcoreapp2.1/ubuntu.18.04-x64/publish/RestaurantInspectionsETL.dll
```

## Build ML Domain

### Define the model input schema

Navigate to the *RestaurantInspectionsTraining* project directory and create a new file called *ModelInput.cs*.

```bash
touch ModelInput.cs
```

Open the *ModelInput.cs* file and add the following code.

```csharp
using Microsoft.ML.Data;

namespace RestaurantInspectionsML
{
    public class ModelInput
    {
        [LoadColumn(0)]
        public string InspectionType { get; set; }

        [LoadColumn(1)]
        public string Codes { get; set; }

        [LoadColumn(2)]
        public float CriticalFlag { get; set; }

        [LoadColumn(3)]
        public float InspectionScore { get; set; }

        [LoadColumn(4)]
        [ColumnName("Label")]
        public string Grade { get; set; }
    }
}
```

Using attributes in the schema, five properties are defined:

- InspectionType: The type of inspection performed.
- Codes: Violation codes found during inspection.
- CriticalFlag: Indicates if any of the violations during the inspection were critical (contribute to food-borne illness).
- InspectionScore: Score assigned after inspection.
- Grade: Letter grade assigned after inspection

The `LoadColumn` attribute defines the position of the column in the file. Data in the last column is assigned to the `Grade` property but is then referenced as `Label` in the `IDataView`. The reason for using the `ColumnName` attribute is ML.NET algorithms have default column names and renaming properties at the schema class level removes the need to define the feature and label columns as parameters in the training pipeline.

### Define the model output schema

In the *RestaurantInspectionsTraining* project directory and create a new file called *ModelOutput.cs*.

```bash
touch ModelOutput.cs
```

Open the *ModelOutput.cs* file and add the following code.

```csharp
namespace RestaurantInspectionsML
{
    public class ModelOutput
    {
        public float[] Scores { get; set; }
        public string PredictedLabel { get; set; }
    }
}
```

For the output schema, the `ModelOutput` class uses properties with the default column names of the outputs generated by the model training process:

- Scores: A float vector containing the probabilties for all the predicted classes.
- PredictedLabel: The value of the prediction. In this case, the `PredictedLabel` is the predicted grade expected to be assigned after inspection given the set of features for that inspection.

## Build the model training application

The application trains a multiclass classification algorithm. Finding the "best" algorithm with the right parameters requires experimentation. Fortunately, ML.NET's Auto ML does this for you given you provide it with the type of algorithm you want to train.

### Load the graded data

Navigate to the *RestaurantInspectionsTraining* project directory and add the following using statements to the *Program.cs* class.

```csharp
using System;
using System.IO;
using System.Linq;
using Microsoft.ML;
using static Microsoft.ML.DataOperationsCatalog;
using Microsoft.ML.AutoML;
using RestaurantInspectionsML;
```

Inside the `Main` method of the *Program.cs* file, define the path where the data files are stored.

```csharp
string solutionDirectory = "/home/lqdev/Development/RestaurantInspectionsSparkMLNET";
string dataLocation = Path.Combine(solutionDirectory,"RestaurantInspectionsETL","Output");
```

The entrypoint of an ML.NET application is the `MLContext`. Initialize an `MLContext` instance.

```csharp
MLContext mlContext = new MLContext();
```

Next, get the paths of the data files. The output generated by the *RestaurantInspectionsETL* application contains both the csv files as well as files containing information about the partitions that created them. For training, only the csv files are needed.

```csharp
var latestOutput =
    Directory
        .GetDirectories(dataLocation)
        .Select(directory => new DirectoryInfo(directory))
        .OrderBy(directoryInfo => directoryInfo.Name)
        .Select(directory => Path.Join(directory.FullName,"Graded"))
        .First();

var dataFilePaths =
    Directory
        .GetFiles(latestOutput)
        .Where(file => file.EndsWith("csv"))
        .ToArray();
```

Then, load the data into an `IDataView`. An `IDataView` is similar to a `DataFrame` in that it is a way to represent data as rows, columns and their schema.

```csharp
var dataLoader = mlContext.Data.CreateTextLoader<ModelInput>(separatorChar:',', hasHeader:false, allowQuoting:true, trimWhitespace:true);

IDataView data = dataLoader.Load(dataFilePaths);
```

It's good practice to split the data into training and test sets for evaluation. Split the data into 80% training and 20% test sets.

```csharp
TrainTestData dataSplit = mlContext.Data.TrainTestSplit(data,testFraction:0.2);
IDataView trainData = dataSplit.TrainSet;
IDataView testData = dataSplit.TestSet;
```

### Create the experiment

Auto ML takes the data and runs experiments using different models and hyper-parameters in search of the "best" model. Define the settings for your experiment. In this case, the model will run for 600 seconds or 10 minutes and will try to find the model with the lowest log loss metric.

```csharp
var experimentSettings = new MulticlassExperimentSettings();
experimentSettings.MaxExperimentTimeInSeconds = 600;
experimentSettings.OptimizingMetric = MulticlassClassificationMetric.LogLoss;
```

Then, create the experiment.

```csharp
var experiment = mlContext.Auto().CreateMulticlassClassificationExperiment(experimentSettings);
```

After creating the experiment, run it.

```csharp
var experimentResults = experiment.Execute(data, progressHandler: new ProgressHandler());
```

By default, running the application won’t display progress information. However, a `ProgressHandler` object can be passed into the `Execute` method of an experiment which calls the implemented `Report` method.

Inside the *RestaurantInspectionsTraining* project directory, create a new file called *ProgressHandler.cs*.

```bash
touch ProgressHandler.cs
```

Then, add the following code:

```csharp
using System;
using Microsoft.ML.Data;
using Microsoft.ML.AutoML;

namespace RestaurantInspectionsTraining
{
    public class ProgressHandler : IProgress<RunDetail<MulticlassClassificationMetrics>>
    {
        public void Report(RunDetail<MulticlassClassificationMetrics> run)
        {
            Console.WriteLine($"Trained {run.TrainerName} with Log Loss {run.ValidationMetrics.LogLoss:0.####} in {run.RuntimeInSeconds:0.##} seconds");
        }
    }
}
```

The ProgressHandler class derives from the `IProgress<T>` interface which requires the implementation of the `Report` method. The object being passed into the Report method after each run is an `RunDetail<MulticlassClassificationMetrics>` object. Each time a run is complete, the `Report` method is called and the code inside it executes.

### Evaluate the results

Once the experiment has finished running, get the model from the best run. Add the following code to the `Main` method of the *Program.cs*.

```csharp
var bestModel = experimentResults.BestRun.Model;
```

Evaluate the performance of the model using the test dataset and measure it's Micro-Accuracy metric.

```csharp
IDataView scoredTestData = bestModel.Transform(testData);  
var metrics = mlContext.MulticlassClassification.Evaluate(scoredTestData);
Console.WriteLine($"MicroAccuracy: {metrics.MicroAccuracy}");
```

### Save the trained model

Finally, save the trained model to the *RestaurantInspectionsML* project.

```csharp
string modelSavePath = Path.Join(solutionDirectory,"RestaurantInspectionsML","model.zip");
mlContext.Model.Save(bestModel, data.Schema, modelSavePath);
```

A file called *model.zip* should be created inside the *RestaurantInspectionsML* project.

Make sure that the trained model file is copied to the output directory by adding the following contents to the *RestaurantInspectionsML.csproj* file in the *RestaurantInspectionsML* directory.

```xml
<ItemGroup>
  <None Include="model.zip">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

Copying it to the output directory of the *RestaurantInspectionsML* makes it easier to reference from the *RestaurantInspectionsEnrichment* project since that project already contains a reference to the *RestaurantInspectionsML* class library.

## Train the model

The final *Program.cs* file should look as follows:

```csharp
using System;
using System.IO;
using System.Linq;
using Microsoft.ML;
using static Microsoft.ML.DataOperationsCatalog;
using Microsoft.ML.AutoML;
using RestaurantInspectionsML;

namespace RestaurantInspectionsTraining
{
    class Program
    {
        static void Main(string[] args)
        {
            // Define source data directory paths
            string solutionDirectory = "/home/lqdev/Development/RestaurantInspectionsSparkMLNET";
            string dataLocation = Path.Combine(solutionDirectory,"RestaurantInspectionsETL","Output");

            // Initialize MLContext
            MLContext mlContext = new MLContext();

            // Get directory name of most recent ETL output
            var latestOutput =
                Directory
                    .GetDirectories(dataLocation)
                    .Select(directory => new DirectoryInfo(directory))
                    .OrderBy(directoryInfo => directoryInfo.Name)
                    .Select(directory => Path.Join(directory.FullName,"Graded"))
                    .First();

            var dataFilePaths =
                Directory
                    .GetFiles(latestOutput)
                    .Where(file => file.EndsWith("csv"))
                    .ToArray();

            // Load the data
            var dataLoader = mlContext.Data.CreateTextLoader<ModelInput>(separatorChar:',', hasHeader:false, allowQuoting:true, trimWhitespace:true);
            IDataView data = dataLoader.Load(dataFilePaths);

            // Split the data
            TrainTestData dataSplit = mlContext.Data.TrainTestSplit(data,testFraction:0.2);
            IDataView trainData = dataSplit.TrainSet;
            IDataView testData = dataSplit.TestSet;

            // Define experiment settings
            var experimentSettings = new MulticlassExperimentSettings();
            experimentSettings.MaxExperimentTimeInSeconds = 600;
            experimentSettings.OptimizingMetric = MulticlassClassificationMetric.LogLoss;

            // Create experiment
            var experiment = mlContext.Auto().CreateMulticlassClassificationExperiment(experimentSettings);

            // Run experiment
            var experimentResults = experiment.Execute(data, progressHandler: new ProgressHandler());

            // Best Run Results
            var bestModel = experimentResults.BestRun.Model;

            // Evaluate Model
            IDataView scoredTestData = bestModel.Transform(testData);  
            var metrics = mlContext.MulticlassClassification.Evaluate(scoredTestData);
            Console.WriteLine($"MicroAccuracy: {metrics.MicroAccuracy}");

            // Save Model
            string modelSavePath = Path.Join(solutionDirectory,"RestaurantInspectionsML","model.zip");
            mlContext.Model.Save(bestModel, data.Schema, modelSavePath);
        }
    }
}
```

Once all the code and configurations are complete, from the *RestaurantInspectionsTraining* directory, run the application using the `dotnet cli`. Remember this will take 10 minutes to run.

```bash
dotnet run
```

The console output should look similar to the output below:

```text
Trained LightGbmMulti with Log Loss 0.1547 in 1.55 seconds
Trained FastTreeOva with Log Loss 0.0405 in 65.58 seconds
Trained FastForestOva with Log Loss 0.0012 in 53.37 seconds
Trained LightGbmMulti with Log Loss 0.0021 in 4.55 seconds
Trained FastTreeOva with Log Loss 0.8315 in 5.22 seconds
MicroAccuracy: 0.999389615839469
```

## Build the data enrichment application

Now that the model is trained, it can be used to enrich the ungraded data.

### Initialize the PredictionEngine

Navigate to the *RestaurantInspectionsEnrichment* project directory and add the following using statements to the *Program.cs* class.

```csharp
using System.IO;
using System.Linq;
using Microsoft.ML;
using Microsoft.Spark.Sql;
using static Microsoft.Spark.Sql.Functions;
using RestaurantInspectionsML;
```

To make predictions, the model has to be loaded into the applicaton and because predictions are made one row at a time, a `PredictionEngine` has be created as well.

Inside the `Program` class, define the `PredictionEngine`.

```csharp
private static readonly PredictionEngine<ModelInput,ModelOutput> _predictionEngine;
```

Then, create a constructor to load the model and initialize it.

```csharp
static Program()
{
    MLContext mlContext = new MLContext();
    ITransformer model = mlContext.Model.Load("model.zip",out DataViewSchema schema);
    _predictionEngine = mlContext.Model.CreatePredictionEngine<ModelInput,ModelOutput>(model);
}
```

### Load the ungraded data

In the `Main` method of the `Program` class, define the location of the data files.

```csharp
string solutionDirectory = "/home/lqdev/Development/RestaurantInspectionsSparkMLNET";
string dataLocation = Path.Combine(solutionDirectory,"RestaurantInspectionsETL","Output");
```

Then, get the path of the most recent ungraded data generated by the *RestaurantInspectionsETL* application.

```csharp
var latestOutput =
    Directory
        .GetDirectories(dataLocation)
        .Select(directory => new DirectoryInfo(directory))
        .OrderBy(directoryInfo => directoryInfo.Name)
        .Select(directory => directory.FullName)
        .First();
```

Initialize a `SparkSession` for your enrichment application.

```csharp
var sc =
    SparkSession
        .Builder()
        .AppName("Restaurant_Inspections_Enrichment")
        .GetOrCreate();
```

The data generated by the *RestaurantInspectionsETL* does not have headers. However, the schema can be defined and set when the data is loaded.

```csharp
var schema = @"
    INSPECTIONTYPE string,
    CODES string,
    CRITICALFLAG int,
    INSPECTIONSCORE int,
    GRADE string";

DataFrame df = 
    sc
    .Read()
    .Schema(schema)
    .Csv(Path.Join(latestOutput,"Ungraded"));
```

### Define the UDF

There is no built-in function in Spark that allows you to use a `PredictionEngine`. However, Spark can be extended through UDFs. Keep in mind that UDFs are not optimized like the built-in functions. Therefore, whenever possible, try to use the built-in functions as much as possible.

In the `Program` class, create a new method called `PredictGrade` which takes in the set of features that make up the `ModelInput` expected by the trained model.

```csharp
public static string PredictGrade(
    string inspectionType,
    string violationCodes,
    int criticalFlag,
    int inspectionScore)
{
    ModelInput input = new ModelInput
    {
        InspectionType=inspectionType,
        Codes=violationCodes,
        CriticalFlag=(float)criticalFlag,
        InspectionScore=(float)inspectionScore
    };

    ModelOutput prediction = _predictionEngine.Predict(input);

    return prediction.PredictedLabel;
}
```

Then, inside the `Main` method, register the `PredictGrade` method as a UDF in your `SparkSession`.

```csharp
sc.Udf().Register<string,string,int,int,string>("PredictGrade",PredictGrade);
```

### Enrich the data

Once the UDF is registered, use it inside of a `Select` statement which creates a new `DataFrame` that includes the input features as well as the predicted grade output by the trained mdoel.

```csharp
var enrichedDf =
    df
    .Select(
        Col("INSPECTIONTYPE"),
        Col("CODES"),
        Col("CRITICALFLAG"),
        Col("INSPECTIONSCORE"),
        CallUDF("PredictGrade",
            Col("INSPECTIONTYPE"),
            Col("CODES"),
            Col("CRITICALFLAG"),
            Col("INSPECTIONSCORE")
        ).Alias("PREDICTEDGRADE")
    );
```

Finally, save the enriched `DataFrame`

```csharp
string outputId = new DirectoryInfo(latestOutput).Name;
string enrichedOutputPath = Path.Join(solutionDirectory,"RestaurantInspectionsEnrichment","Output");
string savePath = Path.Join(enrichedOutputPath,outputId);

if(!Directory.Exists(savePath))
{
    Directory.CreateDirectory(enrichedOutputPath);
}

enrichedDf.Write().Csv(savePath);
```

### Publish and run the enrichment application

The final *Program.cs* file should look as follows.

```csharp
using System.IO;
using System.Linq;
using Microsoft.ML;
using Microsoft.Spark.Sql;
using static Microsoft.Spark.Sql.Functions;
using RestaurantInspectionsML;

namespace RestaurantInspectionsEnrichment
{
    class Program
    {
        private static readonly PredictionEngine<ModelInput,ModelOutput> _predictionEngine;

        static Program()
        {
            MLContext mlContext = new MLContext();
            ITransformer model = mlContext.Model.Load("model.zip",out DataViewSchema schema);
            _predictionEngine = mlContext.Model.CreatePredictionEngine<ModelInput,ModelOutput>(model);
        }

        static void Main(string[] args)
        {
            // Define source data directory paths
            string solutionDirectory = "/home/lqdev/Development/RestaurantInspectionsSparkMLNET";
            string dataLocation = Path.Combine(solutionDirectory,"RestaurantInspectionsETL","Output");

            var latestOutput = 
                Directory
                    .GetDirectories(dataLocation)
                    .Select(directory => new DirectoryInfo(directory))
                    .OrderBy(directoryInfo => directoryInfo.Name)
                    .Select(directory => directory.FullName)
                    .First();

            var sc = 
                SparkSession
                    .Builder()
                    .AppName("Restaurant_Inspections_Enrichment")
                    .GetOrCreate();

            var schema = @"
                INSPECTIONTYPE string,
                CODES string,
                CRITICALFLAG int,
                INSPECTIONSCORE int,
                GRADE string";

            DataFrame df = 
                sc
                .Read()
                .Schema(schema)
                .Csv(Path.Join(latestOutput,"Ungraded"));

            sc.Udf().Register<string,string,int,int,string>("PredictGrade",PredictGrade);

            var enrichedDf = 
                df
                .Select(
                    Col("INSPECTIONTYPE"),
                    Col("CODES"),
                    Col("CRITICALFLAG"),
                    Col("INSPECTIONSCORE"),
                    CallUDF("PredictGrade",
                        Col("INSPECTIONTYPE"),
                        Col("CODES"),
                        Col("CRITICALFLAG"),
                        Col("INSPECTIONSCORE")
                    ).Alias("PREDICTEDGRADE")
                );

            string outputId = new DirectoryInfo(latestOutput).Name;
            string enrichedOutputPath = Path.Join(solutionDirectory,"RestaurantInspectionsEnrichment","Output");
            string savePath = Path.Join(enrichedOutputPath,outputId);

            if(!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(enrichedOutputPath);
            }

            enrichedDf.Write().Csv(savePath);

        }

        public static string PredictGrade(
            string inspectionType,
            string violationCodes,
            int criticalFlag,
            int inspectionScore)
        {
            ModelInput input = new ModelInput
            {
                InspectionType=inspectionType,
                Codes=violationCodes,
                CriticalFlag=(float)criticalFlag,
                InspectionScore=(float)inspectionScore
            };

            ModelOutput prediction = _predictionEngine.Predict(input);

            return prediction.PredictedLabel;
        }
    }
}
```

From the *RestaurantInspectionsEnrichment* project publish the application with the following command.

```bash
dotnet publish -f netcoreapp2.1 -r ubuntu.18.04-x64
```

Navigate to the *publish* directory. In this case, it's *bin/Debug/netcoreapp2.1/ubuntu.18.04-x64/publish*.

From the *publish* directory, run the application with `spark-submit`.

```bash
spark-submit --class org.apache.spark.deploy.dotnet.DotnetRunner --master local microsoft-spark-2.4.x-0.4.0.jar dotnet RestaurantInspectionsEnrichment.dll
```

The file output should look similar to the contents below:

```text
Cycle Inspection / Initial Inspection,04N,1,13,A
Cycle Inspection / Re-inspection,08A,0,9,A
Cycle Inspection / Initial Inspection,"10B,10H",0,10,A
Cycle Inspection / Initial Inspection,10F,0,10,A
Cycle Inspection / Reopening Inspection,10F,0,3,C
```

## Conclusion

This solution showcased how Spark can be used within .NET applications with Spark.NET. Because it's part of the .NET ecosystem, other components and frameworks such as ML.NET can be leveraged to extend the system's capabilities. Although this sample was developed and run on a local, single-node cluster, Spark was made to run at scale. As such, this application can be further improved by setting up a cluster and running the ETL and enrichment workloads on there.

###### Resources

[Apache Spark](https://spark.apache.org/)
[Spark.NET](https://dotnet.microsoft.com/apps/data/spark)
[Spark.NET GitHub](https://github.com/dotnet/spark)
[Mobius](https://github.com/Microsoft/Mobius)
[NYC OpenData](https://opendata.cityofnewyork.us/)
