---
post_type: "article" 
title: "The lqvlc network protocol"
description: "Create and configure the lqvlc protocol, a custom network protocol to automatically launch audio and video streams in VLC from Firefox"
published_date: "07/07/2022 18:47"
tags: firefox, vlc, web, open source, linux
---

## Introduction

Whether it's videos, online radio, podcasts, or my personal music collection, VLC is my default player. I especially enjoy the ability to take the URL of an online video or audio stream playing it in VLC. What I don't like about that process is that I usually have to copy the link and use either the terminal or graphical user interface to play the stream. While not a large number of additional steps, it's still not ideal. The experience I would like is to automatically launch VLC whenever I click a link to an audio or video stream. That's possible using custom network protocols in Firefox. In this guide, I'll show how I defined and configured my own protocol (lqvlc) in Firefox to automatically launch VLC. 

## Prerequisites

- VLC
- Firefox

This guide was built to work on unix systems. However, setting up something similar with a PowerShell or CMD script on Windows should work similarly. 

## Network protocols

A network protocol defines how data is exchanged and transferred over a network. Some common protocols are HTTP, FTP, and SSH. In the context of browsers, the most common protocol is HTTP and the way the browser identifies the protocol is based on the scheme portion of the URL. So for the website [http://lqdev.me](http://lqdev.me), http is the scheme that tells the browser which protocol to use. For more information on network protocols and URLs, see the article [What is a URL?](https://developer.mozilla.org/docs/Learn/Common_questions/What_is_a_URL).

Firefox has a set of standard protocols like HTTP that are configured out of the box. However, you have the option of configuring a custom protocol. 

## The lqvlc protocl

Now that you have a general idea of what network protocols are and how browsers handle them, it's time to introduce the lqvlc protocol. The intent of the lqvlc protocol is to automatically take a URL from an online audio or video stream and launch it in VLC. lqvlc protocol URLs use lqvlc as the scheme instead of http. 

For example:

<table>
    <tr>
        <th>Original Audio Stream Link</th>
        <th>lqvlc Link</th>
    </tr>
    <tr>
        <td><a href="https://somafm.com/u80s64.pls">https://somafm.com/u80s64.pls</a></td>
        <td><a href="lqvlc://somafm.com/u80s64.pls">lqvlc://somafm.com/u80s64.pls</a></td>
    </tr>    
</table>

You'll notice the only difference is the scheme. That's enough information for the browser to know, lqvlc is a protocol I need to handle in some way. In the next sections, I'll describe what you need to do to get Firefox to recognize and handle the lqvlc protocol. 

## Configure network protocols in Firefox

To configure a custom network protocol

1. Launch Firefox
1. In the address bar, navigate to `about:config` and select **Accept the Risk and continue**. As the warning mentions, these are advanced configurations so proceed with caution.
1. Add the following configuration preferences:

    <table>
        <tr>
            <th>Configuration Preference</th>
            <th>Type</th>
            <th>Value</th>
        </tr>
        <tr>
            <td><i>network.protocol-handler.warn-external.lqvlc</i></td>
            <td>Boolean</td>
            <td>true</td>
        </tr>
        <tr>
            <td><i>network.protocol-handler.external.lqvlc</i></td>
            <td>Boolean</td>
            <td>true</td>
        </tr>
        <tr>
            <td><i>network.protocol-handler.expose.lqvlc</i></td>
            <td>Boolean</td>
            <td>false</td>
        </tr>                    
    </table>

    The first two tell Firefox to let an external application handle files with the protocol you've defined. In this case, that's `lqvlc`. If you're configuring your own protocol, you'd replace the lqvlc in the configuration preference with your own.

## Create a handler

Now that Firefox knows that it must use an external application to handle lqvlc URLs, it's time to configure the handler. The easiest way to create a handler is to use a bash script. 

1. Create a new file called *lqvlc-handler.sh* with the following content:

    ```bash
    #!/bin/bash

    link=$1

    vlc "${link/lqvlc/http}"
    ```

    The script takes the first argument it's passed, an lqvlc URL, replaces the scheme with `http`, and launches it in VLC.

1. Make the file executable. In the terminal, run the following command:

    ```bash
    chmod +x lqvlc-handler.sh
    ```

That's it! Now you just need to test it out. 

## Test it out

To use it for your own purposes, you'd have to create or update an HTML page and set the `href` in your anchor tags to use `lqvlc` instead of `http`. However, I've already configured lqvlc links in my own website. If you go to the [radio page](/radio) on my website, you can click on any of the lqvlc links. Alternatively, You can also click on the link to the [SOMA FM Underground 80's station lqvlc link](lqvlc://somafm.com/u80s64.pls). A prompt will pop up asking you to choose which app you want to use to handle the link. Select the lqvlc-handler.sh script you created. If everything is configured correctly, VLC should launch and the radio stations should start playing. 

## Next Steps

Although I've gotten this working for my personal site, I'd like to be able to use the lqvlc protocol in any website I visit that has links to audio and video streams. Arguably, I can also launch lqvlc links from other websites. However, that would require website owners to configure them. The next step I think to make that happen would be to create an Add-On that automatically rewrites stream links to use the lqvlc protocol. 