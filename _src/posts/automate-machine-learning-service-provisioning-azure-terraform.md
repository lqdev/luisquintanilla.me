---
post_type: "article" 
title: Automating Resource Provisioning for Machine Learning in Azure with Cognitive Services and Terraform
published_date: 2019-01-05 22:58:39
tags: [azure,cognitive services,terraform,machine learning,cloud]
---

## Introduction

Resources in the cloud, particularly Azure can be created in several ways some of which include Powershell, Azure CLI, Azure Portal and programatically through code. Another tool that can be leveraged to create resources in Azure is [Terraform](https://www.terraform.io/). Terraform via configuration files can build and manage infrastructure across various providers such as AWS, Azure and Google. One of the benefits of Terraform is that it allows infastructure and resources to be defined as code. The configuration files serve as a blueprint as well as an execution plan of what needs to be provisioned in the cloud. Not only does this allow for automation and removal of human error in configurations but it also serves as a way to document the different changes infrastructure has gone through thoroughout the application lifecycle. The purpose of this writeup is to show how to create an environment using Terraform for an application that utilizes Azure Cognitive Services. Our application will create a Computer Vision service that we can then use via HTTP requests. Source code for this writeup can be found at the following [link](https://github.com/lqdev/azcognitiveserviceterraformsample).

# Prerequisites

This writeup was built and tested using a PC running Ubuntu 18.04 but should work on both Windows and Mac. It also assumes that you have an Azure account as well as Azure CLI and Terraform CLI installed. Below are links to get all of the resources needed:

- [Azure Account](https://azure.microsoft.com/en-us/free/)
- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest)
- [Terraform CLI](https://www.terraform.io/downloads.html)

## Log into Azure CLI

The first thing we need to do is authenticate with Azure CLI so that Terraform can use your account information to create services. In the terminal, enter the following command: 

```bash
az login
```

## Create Terraform Resource Configuration Script

Once we're logged in, it's time to create the configuration file that Terraform will use to provision our services. First we'll need to create a directory for our application:

```bash
mkdir azcognitiveserviceterraformsample
```

Then, we'll enter that directory and create our configuration file:

```bash
cd azcognitiveserviceterraformsample
touch azcomputervision.tf
```

Using your preferred text editor, open the newly created `azcomputervision.tf` file and begin editing.

### Defining the Provider

At the top of our file we'll want to configure our provider. This tells Terraform where our resources will be deployed to. In the `azcomputervision.tf` file enter the following:

```bash
# Configure Azure Provider
provider "azurerm" {
  version="=1.20.0"
}
```

Because we've already authenticated with the Azure CLI, there's no need to provide credentials. The only thing we need to do is specify the provider name and the version of the provider that will be used by the configuration file. For more details on how to configure the Azure provider, visit this [link](https://www.terraform.io/docs/providers/azurerm/index.html).

### Create Resource Group

After defining the provider, it's time to create a Resource Group that will contain the resources we create. In the configuration file, under the provider definition, enter the following:

```bash
# Create Resource Group
resource "azurerm_resource_group" "terraform_cognitive_sample" {
  name="terraform_cognitive_sample"
  location="East US"
}
```

This defines the name of our resource groups as well as where it is hosted. The syntax for resources looks like the snippet below where the `type` is the type of resources as required by Terraform and the `name` is any value of your choosing.

```bash
resource "type" "name" {
# Properties
}
```

For more details on Resource Group configuration visit this [link](https://www.terraform.io/docs/providers/azurerm/d/resource_group.html).

### Create Azure Cognitive Service

With our resource group defined, it's time to define our Cognitive Service resource. Below our Resource Group definition, enter the following:

```bash
resource "azurerm_cognitive_account" "computer_vision_service" {
  name="computer_vision_service"
  resource_group_name="${azurerm_resource_group.terraform_cognitive_sample.name}"
  location="${azurerm_resource_group.terraform_cognitive_sample.location}"
  kind="ComputerVision"
  
  sku {
      name="F0"
      tier="Free"
  }
}
```

Like our Resource Group, we provide a name for our service. Additional properties we need to provide are the name of the Resource Group and the location of where to deploy our service to. Because this has been previously defined in our Resource Group, Terraform allows us to access the configuration values as variables. Additionally, we need to specify the `kind` of cognitive service to deploy. In our case it will be the Computer Vision service so we use `ComputerVision`. The last thing we need to do is specify the pricing tier to use. We'll be using the free tier for this project. There are several services and tiers to choose from. To get more details on acceptable values for these properties visit the following [link](https://www.terraform.io/docs/providers/azurerm/r/cognitive_account.html).

### Store Output Variables

When our application is deployed, in order to use it we'll need the endpoint to where we will be making HTTP requests to. This can be persisted by Terraform by defining an output variable inside of the configuration file. To get the enpoint of our cognitive service, enter the following in the `azcomputervision.tf` file:

```bash
output "computer_vision_endpoint" {
    value="${azurerm_cognitive_account.computer_vision_service.endpoint}"
}
```

Once finished, the `azcomputervision.tf` file should look like this:

```bash
# Configure Azure Provider
provider "azurerm" {
  version="=1.20.0"
}

# Create Resource Group
resource "azurerm_resource_group" "terraform_cognitive_sample" {
  name="terraform_cognitive_sample"
  location="East US"
}

# Create Cognitive Service
resource "azurerm_cognitive_account" "computer_vision_service" {
  name="computer_vision_service"
  resource_group_name="${azurerm_resource_group.terraform_cognitive_sample.name}"
  location="${azurerm_resource_group.terraform_cognitive_sample.location}"
  kind="ComputerVision"
  
  sku {
      name="F0"
      tier="Free"
  }
}

# Output Cognitive Services Endpoint
output "computer_vision_endpoint" {
    value="${azurerm_cognitive_account.computer_vision_service.endpoint}"
}
```

## Provision Resources

### Initialize Terraform Resources

When provisioning resources we first want to get the necessary plugins needed by Terraform to create such resources. Using Terraform CLI, enter the following in the terminal:

```bash
terraform init
```

### Check Execution Plan

Although this is a single resource provision, it's always a good idea to see the execution plan of the resources that will be created. To do so, enter the following command in the terminal:

```bash
terraform plan
```

The output should look similar to the content below:

```text
Refreshing Terraform state in-memory prior to plan...
The refreshed state will be used to calculate this plan, but will not be
persisted to local or remote state storage.


------------------------------------------------------------------------

An execution plan has been generated and is shown below.
Resource actions are indicated with the following symbols:
  + create

Terraform will perform the following actions:

  + azurerm_cognitive_account.computer_vision_service
      id:                  <computed>
      endpoint:            <computed>
      kind:                "ComputerVision"
      location:            "eastus"
      name:                "computer_vision_service"
      resource_group_name: "terraform_cognitive_sample"
      sku.#:               "1"
      sku.0.name:          "F0"
      sku.0.tier:          "Free"
      tags.%:              <computed>

  + azurerm_resource_group.terraform_cognitive_sample
      id:                  <computed>
      location:            "eastus"
      name:                "terraform_cognitive_sample"
      tags.%:              <computed>


Plan: 2 to add, 0 to change, 0 to destroy.
```

## Create Resources

It's now time to create our Computer Vision resource. Enter the following command in the terminal:

```bash
terraform apply
```

You will be asked to review the execution plan once again. If everything looks good, type `yes` in the terminal to continue with the creation of resources. If everything deployed successfully, you should be able to see it in Azure.

## Test Provisioned Resources

Using Azure CLI, enter the following command in the terminal to see whether your resource was deployed:

```bash
az resource list --resource-group terraform_cognitive_sample --output table
```

That command will output something similar to the content below:

```bash
Name                     ResourceGroup               Location    Type                                  Status
-----------------------  --------------------------  ----------  ------------------------------------  --------
computer_vision_service  terraform_cognitive_sample  eastus      Microsoft.CognitiveServices/accounts
```

### Get Resource Endpoint and Keys

In order to make a request to our service, we need both the endpoint and a key to authenticate our request.

To get the endpoint, we can use Terraform CLI to extract the output variable defined in our configuration file. We can do that by using the following command in the terminal:

```bash
terraform output computer_vision_endpoint
```

Save the output value somewhere because that's what will be used to make a request to the Computer Vision Cognitive Service.

To get the keys, we'll use Azure CLI. In the terminal, enter:

```bash
az cognitiveservices account keys list --resource-group terraform_cognitive_sample --name computer_vision_service
```

This will output the keys of your deployed service.

```json
{
  "key1": "<YOUR-KEY-1>",
  "key2": "<YOUR-KEY-2>"
}
```

### Make HTTP Request

Either key can be used to make requests. To test the service, I'll be using an image from the web. All you need is to get the URL for that image. The image I will be using can be found at this URL `https://upload.wikimedia.org/wikipedia/commons/1/10/Empire_State_Building_%28aerial_view%29.jpg`. Using cURL, I will make a POST request to the endpoint with the key provided.

One thing to note is that the endpoint is generic to many Cognitive Services. Therefore, we need to append to the path which service we will be using and the action we want to perform. In our case, we'll append the following path the the endpoint `/vision/v1.0/describe`. For more details, visit the Computer Vision API documentation at this [link](https://eastus.dev.cognitive.microsoft.com/docs/services/56f91f2d778daf23d8ec6739/operations/56f91f2e778daf14a499e1fe).

To make the request, enter the following into the terminal where `<YOUR-KEY>` is one of the keys of your Computer Vision service.

```bash
curl -H 'Ocp-Apim-Subscription-Key: <YOUR-KEY>' -H "Content-type: application/json" -d '{"url":"https://upload.wikimedia.org/wikipedia/commons/1/10/Empire_State_Building_%28aerial_view%29.jpg"}' 'https://eastus.api.cognitive.microsoft.com/vision/v1.0/describe'
```

If on Windows, you might want to try using a REST client such as POSTMAN or Insomnia.

If the request is successful, the output should be similar to the following:

```json
{
    "description":{
        "tags":[
            "mountain","building","city","sitting","table","view","full","filled","large","old","skyscraper","many","stacked","water","room","white"
        ],
        "captions":[
            {
                "text":"a view of a city","confidence":0.927147938733121
            }
        ]
    },
    "requestId":"a9aaf779-2aa1-4012-b1e0-2d2d9c20c6a5",
    "metadata":{
        "width":846,
        "height":1270,
        "format":"Jpeg"
    }
}
```

## Conclusion

In this writeup I went over how to create a Terraform configuration file for automating the creation of a Computer Vision Cognitive Service resource in Azure. Although this is a simple example, using the same concepts, more complex environments and infrastructure can be provisioned just as seamlessly in a safe and efficient manner. An example of how to extend this application would be to provision an Azure Storage Account as well as an Azure Function application that utilizes the Computer Vision Cognitive Service to automatically classify photos when they are uploaded to an Azure Storage container. In such scenario, the benefits of automation and documentation that Terraform provides can leveraged to a greater extent.