---
post_type: "article" 
title: "Automate YAML front-matter generation with custom Visual Studio Code snippets"
published_date: "10/04/2021 19:58"
tags: blogging,tooling,visual-studio,visual-studio-code,markdown,yaml
---

## Introduction

When creating content with static site generators such as Jekyll, Hexo, and many others, page metadata such as publish date, title, tags, and other details is created using YAML front-matter. Typically these pages are authored in markdown. At the top of each page, there's a YAML formatted section containing all of these details. Sometimes these static site generators include tooling to make it easier to scaffold new pages. In the case of my website, I created my own static site generator. Therefore, scaffolding a new page, especially writing the boilerplate YAML front-matter is cumbersome and time consuming. Since I use Visual Studio Code for authoring content and developing the static site generator, I've decided to use [snippets](https://code.visualstudio.com/docs/editor/userdefinedsnippets) to help me automate YAML front-matter generation. In this post, I'll show the general process. 

## Create snippets file

The first thing you'll want to do is create a snippets file. You can have one or as many as you want. For this website, I have a single snippets file called *metadata.code-snippets* for all my YAML front-matter. To create a snippets file:

1. Open the Visual Studio Code command palette.
1. Enter the command `>Preferences: Configure User Snippets` into the text box.
1. Select the **New Snippets file for \<REPO-OR-DIRECTORY-NAME\>** from the list of options. 

    ![Create snippet file from Visual Studio Code command palette](https://user-images.githubusercontent.com/11130940/135934846-a76dfa16-caed-4489-9e50-95183add673d.png)

1. Provide a name for your snippets file and press **Enter**.
1. A file with the *.code-snippets* extension is created in the *.vscode* directory inside your project. 

One benefit of creating a snippets file and saving it in the *.vscode* directory is you can check it into source control and use it anywhere, even github.dev. 

## Create snippets

Now that you've created the file, it's time to define your snippets.

1. Open your snippets file.

    ![Snippet file for luisquintanilla.me](https://user-images.githubusercontent.com/11130940/135935306-1cbd48e1-6e48-423c-b87f-cb6f7a8eb085.png)

1. Create a new JSON object and define your snippet.

    For example, the snippet for blog posts on this site looks like the following.

    ```json
    "Article Post metadata": {
        "scope": "markdown",
        "prefix": "article",
        "body": [
            "---",
            "post_type: \"article\" ",
            "title: \"\"",
            "published_date: \"$CURRENT_MONTH/$CURRENT_DATE/$CURRENT_YEAR $CURRENT_HOUR:$CURRENT_MINUTE\"",
            "tags: ",
            "---"
        ],
        "description": "Blog post article metadata"
    }
    ```

    Let's break down each of the properties:

    - `scope`: By default, snippets apply to all languages and projects. In my case, since I intend only to use these snippets in markdown files, I set the scope to `markdown`. 
    - `prefix`: The prefix is the word in the page that is associated with. When I type `article` in a markdown page, a recommendation to use the snippet appears.
    - `body`: The code or content of my snippet. In this case, the expected output is similar to the following.

        ```yaml
        ---
        post_type: "article" 
        title: ""
        published_date: "10/04/2021 18:58"
        tags: 
        ---
        ```

        VS Code also has built-in variables that you can use to automatically set date, time, and other values.

    - `description`: A text description of your snippet.

For more information on creating snippets, see [Create your own snippets](https://code.visualstudio.com/docs/editor/userdefinedsnippets#_create-your-own-snippets).

## Use your snippets

1. Create a new markdown file.
1. Type your prefix into the file. In my case, for blog posts my prefix is `article`.
1. Press **Ctrl + Space**. For Macs you might have to use Cmd instead of Ctrl. 

    ![Populate article front-matter using snippet](https://user-images.githubusercontent.com/11130940/135935808-46ca8314-c2be-47f1-9c9b-9b722e37d908.png)

1. A tooltip with your snippet's description appears. Press **Enter**. 

At this point, your snippet's prefix is replaced with the content defined in the snippet's body.

## Conclusion

In this post I've shown how you can use Visual Studio Code snippets to automate YAML front-matter generation when working with common static site generators that don't provide tooling to scaffold new pages. Snippets can be used for a variety of scenarios and if there's any boilerplate code or content you constantly have to write, they can save you a lot of time. 

Happy writing!
