---
post_type: "article" 
title: Use .NET Interactive to run .NET code in Jupyter Notebooks on an Azure Machine Learning compute instance
published_date: 2020-08-29 20:23:09
tags: [dotnet, Jupyter Notebooks, dotnet Interactive, Azure Machine Learning, Azure, Programming, Development Tools]
---

# Introduction

In this post, I'll go over how you can run .NET code in Jupyter Notebooks using .NET Interactive and Azure Machine Learning compute instances.

[Jupyter Notebooks](https://jupyter.org/) are an graphical interactive computing environment used in disciplines like data science and education. When it comes to prototyping or learning scenarios, they can help users see near real-time what their code is doing. The interactive computing protocol used by Jupyter Notebooks is extensible and as a result, you're able to run a variety of languages inside of this environment via kernels. Kernels are processes that take the input from the user, execute the code, and return the output for display and further processing. Using [.NET Interactive](https://github.com/dotnet/interactive), a .NET Core global tool, that among its many features provides a kernel for Jupyter Notebooks to run .NET (C#/F#) and PowerShell code.

Although you can install and run Jupyter Notebooks on your local computer, services like [Azure Machine Learning](https://docs.microsoft.com/azure/machine-learning/) provide single VMs known as compute instances which have Jupyter Notebooks as well as many popular data science libraries and development tools like Docker preinstalled. Therefore, installing .NET Interactive on one of these VMs is a seamless experience. Additionally, Azure Machine Learning gives you the ability to share computing resources with teams of various sizes.

Let's get started!

## Prerequisites

- [Azure account](https://azure.microsoft.com/free/). If you don't have one, create one.

## Create Azure Machine Learning workspace

A workspace organizes all of you Azure Machine Learning resources and assets such as compute instances in a single place. In order to use Azure Machine Learning, you have to create a workspace.

There's multiple ways to create a workspace. For this writeup, I use the Azure Portal.

Navigate to [portal.azure.com](https://portal.azure.com)

In the portal, select **Create a resource**.

![Create new resource Azure Portal](https://user-images.githubusercontent.com/11130940/91647989-d79d6380-ea2f-11ea-9d97-cc265c79801f.png)

From the resource list, select **AI + Machine Learning > Machine Learning**

![Create Azure Machine Learning Resource](https://user-images.githubusercontent.com/11130940/91647999-01ef2100-ea30-11ea-954a-3c3280a9cbca.png)

Fill in the form and select **Review + create**.

![Create Azure Machine Learning workspace](https://user-images.githubusercontent.com/11130940/91648065-07993680-ea31-11ea-9273-c28b4ec4bf7c.png)

Review your information before creating the workspace and select **Create**. Deployment takes a few minutes. As part of your workspace deployment, additional Azure resources are created such as Azure Container Registry, Azure Storage account, Azure Application Insights, and Azure KeyVault.

For more information on workspaces, see the [Azure Machine Learning workspace documentation](https://docs.microsoft.com/azure/machine-learning/concept-workspace).

## Create an Azure Machine Learning compute instance

As mentioned earlier, an Azure Machine Learning compute instance is a single VM that comes with a variety of development tools and libraries commonly used for data science and machine learning workflows preinstalled.

Navigate to [Azure Machine Learning studio](https://ml.azure.com). The studio is a web interface for managing your Azure Machine Learning resources such as data, experiments, models, and compute.

In studio, select **Create new > Compute instance** to create a new compute instance.

![Create new Azure Machine Learning compute instance](https://user-images.githubusercontent.com/11130940/91648186-73c86a00-ea32-11ea-9986-205306fe76b0.png)

Provide a name for your instance and select **Create**. You can also customize the size of your VM and whether you want it to be GPU enabled. In this case, I just selected the preselected option.

![Provision Azure Machine Learning compute instance](https://user-images.githubusercontent.com/11130940/91648251-1c76c980-ea33-11ea-91de-818e1f63767c.png)

Once your compute instance is provisioned, select the **Jupyter** link to launch Jupyter Notebooks.

![Launch Jupyter Notebooks](https://user-images.githubusercontent.com/11130940/91648307-f271d700-ea33-11ea-9b80-57e44f0bcdf5.png)

For more information on compute instances, see the [Azure Machine Learning compute instance documentation](https://docs.microsoft.com/azure/machine-learning/concept-compute-instance).

## Install the .NET Core 3.1 SDK

At the time of this writing, the image used by compute instances is Ubuntu 16.04. Therefore, we'll be installing the Linux version of the .NET Core SDK.

Inside the Jupyter Notebooks, select **New > Terminal** to create a new terminal.

![image](https://user-images.githubusercontent.com/11130940/91648326-1c2afe00-ea34-11ea-8f84-48b98e1ec148.png)

In the terminal, add the Microsoft package signing key to your list of trusted keys.

```bash
wget https://packages.microsoft.com/config/ubuntu/16.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
```

Install the .NET Core 3.1 SDK with the following command:

```bash
sudo apt-get update; \
  sudo apt-get install -y apt-transport-https && \
  sudo apt-get update && \
  sudo apt-get install -y dotnet-sdk-3.1
```

> Note that as of the time of this writing, .NET Interactive requires .NET Core 3.1.

You can also find these installation instructions in the [.NET Core installation documentation](https://docs.microsoft.com/dotnet/core/install/linux-ubuntu#1604-).

## Install .NET Interactive

Start off by checking which kernels are installed. In the terminal, enter the following command.

```bash
jupyter kernelspec list
```

The output should look something like the output below

```console
Available kernels:
  python3            /anaconda/envs/azureml_py36/share/jupyter/kernels/python3
  ir                 /usr/local/share/jupyter/kernels/ir
  python3-azureml    /usr/local/share/jupyter/kernels/python3-azureml
```

Then, install the .NET Interactive global tool

```bash
dotnet tool install -g --add-source "https://dotnet.myget.org/F/dotnet-try/api/v3/index.json" Microsoft.dotnet-interactive
```

Use the .NET Interactive tool to install the .NET and powershell kernels.

```bash
dotnet interactive jupyter install
```

Finally, run the following command to confirm that the kernels have been installed.

```bash
jupyter kernelspec list
```

In the output you should see the C#, F#, and PowerShell kernels listed

```bash
Available kernels:
  .net-csharp        /home/azureuser/.local/share/jupyter/kernels/.net-csharp
  .net-fsharp        /home/azureuser/.local/share/jupyter/kernels/.net-fsharp
  .net-powershell    /home/azureuser/.local/share/jupyter/kernels/.net-powershell
  python3            /anaconda/envs/azureml_py36/share/jupyter/kernels/python3
  ir                 /usr/local/share/jupyter/kernels/ir
  python3-azureml    /usr/local/share/jupyter/kernels/python3-azureml
```

## Add .NET global tools directory to PATH

The Jupyter Notebook server is managed by a `systemd` service. Although during installation .NET Interactive is added to your `PATH` environment variable, it's not added to the environment file `/etc/environment` which is used by `systemd`. Therefore, in order to run .NET Interactive, you have to add the .NET global tools directory to that file.

The contents of the file should look similar to the one below

```text
PATH="/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin:/usr/games:/usr/local/games"
AML_CloudName=AzureCloud
```

Open the `/etc/environment` using your preferred text editor (vi/nano) and replace the `PATH` with the following content:

```text
PATH="/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin:/usr/games:/usr/local/games:/home/azureuser/.dotnet/tools"
```

Save your changes and restart the Jupyter Server. In the terminal, run the following command.

```bash
sudo service jupyter restart
```

This will close your terminal session since the notebook server has been restarted.

Close all Jupyter Notebook windows.

## Create new .NET Jupyter Notebook

From Azure Machine Learning studio, launch Jupyter Notebooks again by selecting the **Jupyter** link.

Then, in Jupyter Notebooks select **New** and create a new notebook (C#/F#/PowerShell). In this case, I created an F# notebook.

Once the kernel is ready, enter code into the first cell and run it.

![FSharp Jupyter Notebook running in AML compute instance](https://user-images.githubusercontent.com/11130940/91648958-20f3b000-ea3c-11ea-95a1-3e4ba71c31d8.png)

Congratulations! You should now be able to run .NET and PowerShell code inside of your Azure Machine Learning compute instance.

## Conclusion

In this post, I showed how you can run .NET code in Jupyter Notebooks on an Azure Machine Learning compute instance with the help of .NET Interactive. This enables you to interactively prototype solutions remotely while still having control over your environment and dependencies. Now that you have .NET interactive setup, compute instances also give you the option of using JupyterLab. Feel free to tinker with the different environments and see which one works best for you. Happy coding!

## Sources

- [Azure Machine Learning documentation](https://docs.microsoft.com/azure/machine-learning/)
- [.NET Interactive GitHub repository](https://github.com/dotnet/interactive)
- [Jupyter Notebooks](https://jupyter.org/)
