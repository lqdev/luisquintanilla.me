---
post_type: "note" 
title: "Impressed by Azure Cloud Shell"
published_date: "2022-10-13 22:45"
---

I can probably count the number of times I've used the Azure Cloud Shell inside the Azure Portal with one hand. However, I recently had a need to use it and was impressed by the experience. 

I wanted to create an Azure Function app but didn't want to install all the tools required. Currently I'm running Manjaro on an ASUS L210MA. That's my daily driver and although the specs are incredibly low I like the portability. The Azure set of tools run on Linux but are easier to install on Debian distributions. The low specs plus the Arch distro didn't make me want to go through the process of installing the Azure tools locally for a throwaway app. 

That's when I thought about using Azure Cloud Shell. Setup was relatively quick and easy. It was also great to see the latest version of .NET, Azure Functions Core Tools, and Azure CLI were already installed. Even Git was already there. After creating the function app, I was able to make some light edits using the built-in Monaco editor. When I was ready to test the app, I was able to open up a port, create a proxy, and test the function locally. It's not a high-end machine so I won't be training machine learning models on it anytime soon. However, for experimentation and working with my Azure account it's more than I need. 