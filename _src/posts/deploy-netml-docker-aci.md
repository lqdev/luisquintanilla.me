---
post_type: "article" 
title: Deploy .NET Machine Learning Models with ML.NET, ASP.NET Core, Docker and Azure Container Instances
tags: [azure, devops, dotnet, ml, ai, microsoft, programming, development, csharp, aci, docker, mlnet, webapi, aspnetcore, aspnet, machinelearning, artificialintelligence]
published_date: 2018-05-11 21:17:00
---


# Introduction

Leading up to and during MS Build 2018 Microsoft has released a wide range of products that reduce the complexity that comes with building and deploying software. The focus this year was on Machine Learning and Artificial Intelligence. Some of the products I found particularly interesting are [Azure Container Instances](https://azure.microsoft.com/en-us/services/container-instances/) which makes it easier to run containerized applications without provisioning or managing servers and [ML.NET](https://www.microsoft.com/net/learn/apps/machine-learning-and-ai/ml-dotnet) which is a .NET cross-platform machine learning framework. In this writeup, I will make use of both these products by creating a machine learning classification model with `ML.NET`, exposing it via an ASP.NET Core Web API, packaging it into a Docker container and deploying it to the cloud via Azure Container Instances. Source code for this project can be found [here](https://github.com/lqdev/mlnetacidemo).

## Prerequisites

This writeup assumes that you have some familiarity with Docker. The following software/dependencies are also required to build and deploy the sample application. It's important to note the application was built on a Ubuntu 16.04 PC, but all the software is cross-platform and should work on any environment.

- [Docker](https://docs.docker.com/install/linux/docker-ce/ubuntu/)
- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest)
- [.NET Core 2.0](https://www.microsoft.com/net/download/linux)
- [Docker Hub Account](https://hub.docker.com/)

## Setting Up The Project

The first thing we want to do is create a folder for our solution.

```bash
mkdir mlnetacidemo
```

Then, we want to create a solution inside our newly created folder.

```bash
cd mlnetacidemo
dotnet new sln
```

## Building The Model

Inside our solution folder, we want to create a new console application which is where we'll build and test our machine learning model.

### Setting Up the Model Project

First, we want to create the project. From the solution folder enter:

```bash
dotnet new console -o model
```

Now we want to add this new project to our solution.

```bash
dotnet sln mlnetacidemo.sln add model/model.csproj
```

### Adding Dependencies

Since we'll be using the `ML.NET` framework, we need to add it to our `model` project.

```bash
cd model
dotnet add package Microsoft.ML
dotnet restore
```

### Download The Data

Before we start training the model, we need to download the data we'll be using to train. We do so by creating a directory called `data` and downloading the data file onto there.

```bash
mkdir data
curl -o data/iris.txt https://archive.ics.uci.edu/ml/machine-learning-databases/iris/iris.data
```

If we take a look at the data file, it should look something like this:

```text
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

Now that we have all our dependencies set up, it's time to build our model. I leveraged the demo that is used on the [ML.NET Getting-Started website](https://www.microsoft.com/net/learn/apps/machine-learning-and-ai/ml-dotnet/get-started/linux/ubuntu16-04).

#### Defining Data Structures

In the root directory of our `model` project, let's create two classes called `IrisData` and `IrisPrediction` which will define our features and predicted attribute respectively. Both of them will use `Microsoft.ML.Runtime.Api` to add the property attributes.

Here is what our `IrisData` class looks like: 
```csharp
using Microsoft.ML.Runtime.Api;

namespace model
{
public class IrisData
    {
        [Column("0")]
        public float SepalLength;

        [Column("1")]
        public float SepalWidth;

        [Column("2")]
        public float PetalLength;
        
        [Column("3")]
        public float PetalWidth;

        [Column("4")]
        [ColumnName("Label")]
        public string Label;
    }       
}
```

Similarly, here is the `IrisPrediction` class:

```csharp
using Microsoft.ML.Runtime.Api;

namespace model
{
    public class IrisPrediction
    {
        [ColumnName("PredictedLabel")]
        public string PredictedLabels;
    }
}
```

#### Building Training Pipeline

The way the `ML.NET` computations process is via a sequential pipeline of steps that are performed eventually leading up to the training of the model. Therefore, we can create a class called `Model` to perform all of these tasks for us.

```csharp
using Microsoft.ML.Data;
using Microsoft.ML;
using Microsoft.ML.Runtime.Api;
using Microsoft.ML.Trainers;
using Microsoft.ML.Transforms;
using Microsoft.ML.Models;
using System;
using System.Threading.Tasks;

namespace model
{
    class Model
    {
        
        public static async Task<PredictionModel<IrisData,IrisPrediction>> Train(LearningPipeline pipeline, string dataPath, string modelPath)
        {
            // Load Data
            pipeline.Add(new TextLoader(dataPath).CreateFrom<IrisData>(separator:',')); 

            // Transform Data
            // Assign numeric values to text in the "Label" column, because 
            // only numbers can be processed during model training   
            pipeline.Add(new Dictionarizer("Label"));

            // Vectorize Features
            pipeline.Add(new ColumnConcatenator("Features", "SepalLength", "SepalWidth", "PetalLength", "PetalWidth"));

            // Add Learner
            pipeline.Add(new StochasticDualCoordinateAscentClassifier());

            // Convert Label back to text 
            pipeline.Add(new PredictedLabelColumnOriginalValueConverter() {PredictedLabelColumn = "PredictedLabel"});

            // Train Model
            var model = pipeline.Train<IrisData,IrisPrediction>();

            // Persist Model
            await model.WriteAsync(modelPath);

            return model;
        }
    }
}
```

In addition to building our pipeline and training our machine learning model, the `Model` class also serialized and persisted the model for future use in a file called `model.zip`.

#### Testing Our Model

Now that we have our data structures and model training pipeline set up, it's time to test everything to make sure it's working. We'll put our logic inside of our `Program.cs` file.

```csharp
using System;
using Microsoft.ML;

namespace model
{
    class Program
    {
        static void Main(string[] args)
        {

            string dataPath = "model/data/iris.txt";

            string modelPath = "model/model.zip";

            var model = Model.Train(new LearningPipeline(),dataPath,modelPath).Result;

            // Test data for prediction
            var prediction = model.Predict(new IrisData() 
            {
                SepalLength = 3.3f,
                SepalWidth = 1.6f,
                PetalLength = 0.2f,
                PetalWidth = 5.1f
            });

            Console.WriteLine($"Predicted flower type is: {prediction.PredictedLabels}");
        }
    }
}
```

All set to run. We can do so by entering the following command from our solution directory:

```bash
dotnet run -p model/model.csproj
```

Once the application has been run, the following output should display on the console. 

```text
Automatically adding a MinMax normalization transform, use 'norm=Warn' or
'norm=No' to turn this behavior off.Using 2 threads to train.
Automatically choosing a check frequency of 2.Auto-tuning parameters: maxIterations = 9998.
Auto-tuning parameters: L2 = 2.667734E-05.
Auto-tuning parameters: L1Threshold (L1/L2) = 0.Using best model from iteration 882.
Not training a calibrator because it is not needed.
Predicted flower type is: Iris-virginica
```

Additionally, you'll notice that a file called `model.zip` was created in the root directory of our `model` project. This persisted model can now be used outside of our application to make predictions, which is what we'll do next via an API.

## Exposing The Model

Once a machine learning model is built, you want to deploy it so it can start making predictions. One way to do that is via a REST API. At it's core, all our API needs to do is accept data input from the client and respond back with a prediction. To help us do that, we'll be using an ASP.NET Core API.

### Setting Up The API Project

The first thing we want to do is create the project.

```bash
dotnet new webapi -o api
```

Then we want to add this new project to our solution

```bash
dotnet sln mlnetacidemo.sln add api/api.csproj
```

### Adding Dependencies

Because we'll be loading our model and making predictions via our API, we need to add the `ML.NET` package to our `api` project.

```bash
cd api
dotnet add package Microsoft.ML
dotnet restore
```

### Referencing Our Model

In the previous step when we built our machine learning model, it was saved to a file called `model.zip`. This is the file we'll be referencing in our API to help us make predictions. To reference it in our API, simply copy it from the model project directory into our `api` project directory.

### Creating Data Models

Our model was built using data structures `IrisData` and `IrisPrediction` to define the features as well as the predicted attribute. Therefore, when our model makes predictions via our API, it needs to reference these data types as well. As a result, we need to define `IrisData` and `IrisPrediction` classes inside of our `api` project. The contents of the classes will be nearly identical to those in the `model` project with the only exception of our namespace changing from `model` to `api`.

```csharp
using Microsoft.ML.Runtime.Api;

namespace api
{
    public class IrisData
    {
        [Column("0")]
        public float SepalLength;

        [Column("1")]
        public float SepalWidth;

        [Column("2")]
        public float PetalLength;
        
        [Column("3")]
        public float PetalWidth;

        [Column("4")]
        [ColumnName("Label")]
        public string Label;
    }    
}
```

```csharp
using Microsoft.ML.Runtime.Api;

namespace api
{
    public class IrisPrediction
    {
        [ColumnName("PredictedLabel")]
        public string PredictedLabels;
    }
}
```

### Building Endpoints

Now that our project is set up, it's time to add a controller that will handle prediction requests from the client. In the `Controllers` directory of our `api` project we can create a new class called `PredictController` with a single `POST` endpoint. The contents of the file should look like the code below:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ML;

namespace api.Controllers
{
    [Route("api/[controller]")]
    public class PredictController : Controller
    {
        // POST api/predict
        [HttpPost]
        public string Post([FromBody] IrisData instance)
        {
            var model = PredictionModel.ReadAsync<IrisData,IrisPrediction>("model.zip").Result;
            var prediction = model.Predict(instance);
            return prediction.PredictedLabels;
        }
    }
}
```

### Testing The API

Once our `predict` endpoint is set up, it's time to test it. From the root directory of our `mlnetacidemo` solution, enter the following command.

```bash
dotnet run -p api/api.csproj
```

In a client like POSTMAN or Insomnia, send an HHTP POST request to the endpoint `http://localhost:5000/api/predict`.

The body our request should look similar to the snippet below:

```json
{
	"SepalLength": 3.3,
	"SepalWidth": 1.6,
	"PetalLength": 0.2,
	"PetalWidth": 5.1,
}
```

If successful, the output returned should equal `Iris-virginica` just like our console application.

## Packaging The Application

Great! Now that our application is successfully running locally, it's time to package it up into a Docker container and push it to Docker Hub.

### Creating The Dockerfile

In our `mlnetacidemo` solution directory, create a `Dockerfile` with the following content:

```Dockerfile
FROM microsoft/dotnet:2.0-sdk AS build
WORKDIR /app

# copy csproj and restore as distinct layers
COPY *.sln .
COPY api/*.csproj ./api/
RUN dotnet restore

# copy everything else and build app
COPY api/. ./api/
WORKDIR /app/api
RUN dotnet publish -c release -o out


FROM microsoft/aspnetcore:2.0 AS runtime
WORKDIR /app
COPY api/model.zip .
COPY --from=build /app/api/out ./
ENTRYPOINT ["dotnet", "api.dll"]
```

### Building Our Image

To build the image, we need to enter the following command into the command prompt. This make take a while because it needs to download the .NET Core SDK and ASP.NET Core runtime Docker images.

```bash
docker build -t <DOCKERUSERNAME>/<IMAGENAME>:latest .
```

### Test Image Locally

We need to test our image locally to make sure it can run on the cloud. To do so, we can use the `docker run` command. 

```bash
docker run -d -p 5000:80 <DOCKERUSERNAME>/<IMAGENAME>:latest
```

Although the API is exposing port 80, we bind it to the local port 5000 just to keep our prior API request intact. When sending a POST request to `http://localhost:5000/api/predict` with the appropriate body, the response should again equal `Iris-virginica`.

To stop the container, use `Ctrl + C`.

### Push to Docker Hub

Now that the Docker image is successfully running locally, it's time to push to Docker Hub. Again, we use the Docker CLI to do this.

```bash
docker login
docker push <DOCKERUSERNAME>/<IMAGENAME>:latest
```

## Deploying To The Cloud

Now comes the final step which is to deploy and expose our machine learning model and API to the world. Our deployment will occur via Azure Container Instances because it requires almost no provisioning or management of servers. 

### Prepare Deployment Manifest

Although deployments can be performed inline in the command line, it's usually best to place all the configurations in a file for documentation and to save time not having to type in the parameters every time. With Azure, we can do that via a JSON file. 

```json
{
  "$schema":
    "https://schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json#",
  "contentVersion": "1.0.0.0",
  "parameters": {
    "containerGroupName": {
      "type": "string",
      "defaultValue": "mlnetacicontainergroup",
      "metadata": {
        "description": "Container Group name."
      }
    }
  },
  "variables": {
    "containername": "mlnetacidemo",
    "containerimage": "<DOCKERUSERNAME>/<IMAGENAME>:latest"
  },
  "resources": [
    {
      "name": "[parameters('containerGroupName')]",
      "type": "Microsoft.ContainerInstance/containerGroups",
      "apiVersion": "2018-04-01",
      "location": "[resourceGroup().location]",
      "properties": {
        "containers": [
          {
            "name": "[variables('containername')]",
            "properties": {
              "image": "[variables('containerimage')]",
              "resources": {
                "requests": {
                  "cpu": 1,
                  "memoryInGb": 1.5
                }
              },
              "ports": [
                {
                  "port": 80
                }
              ]
            }
          }
        ],
        "osType": "Linux",
        "ipAddress": {
          "type": "Public",
          "ports": [
            {
              "protocol": "tcp",
              "port": "80"
            }
          ]
        }
      }
    }
  ],
  "outputs": {
    "containerIPv4Address": {
      "type": "string",
      "value":
        "[reference(resourceId('Microsoft.ContainerInstance/containerGroups/', parameters('containerGroupName'))).ipAddress.ip]"
    }
  }
}
```

It's a lot to look at but for now we can use this template and save it to the file `azuredeploy.json` in the root directory of our `mlnetacidemo` solution. The only thing that needs to be changed is the value of the `containerimage` property. Replace it with your Docker Hub username and the name of the image you just pushed to Docker Hub.

### Deploy

In order to deploy our application we need to make sure to log into our Azure account. To do so via the Azure CLI, type into the command prompt:

```bash
az login
```

Follow the prompts to log in. Once logged in, it's time to create a resource group for our container.

```bash
az group create --name mlnetacidemogroup --location eastus
```

After the group has been successfully created it's time to deploy our application.

```bash
az group deployment create --resource-group mlnetacidemogroup --template-file azuredeploy.json
```

Give it a few minutes for your deployment to initialize. If the deployment was successful, you should see some output on the command line. Look for the `ContainerIPv4Address` property. This is the IP Address where your container is accessible. In POSTMAN or Insomnia, replace the URL to which you previously made a POST request to with `http://<ContainerIPv4Address>/api/predict` where `ContainerIPv4Address` is the value that was returned to the command line after the deployment. If successful, the response should be just like previous requests `Iris-virginica`.

Once you're finished, you can clean up resources with the following command:

```bash
az group delete --name mlnetacidemogroup
```

## Conclusion

In this writeup, we built a classification machine learning model using `ML.NET` that predicts the class of an iris plant given four measurement features, exposed it via an ASP.NET Core REST API, packaged it into a container and deployed it to the cloud using Azure Container Instances. As the model changes and becomes more complex, the process is standardized enough that extending this example would require minimal changes to our existing application. Happy Coding!
