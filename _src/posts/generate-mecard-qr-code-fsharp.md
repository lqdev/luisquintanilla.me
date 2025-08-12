---
post_type: "article" 
title: "Generate QR Codes for MeCard Data in F#"
description: "Encode MECARD files in a QR code using F#"
published_date: "2023-10-23 19:24"
tags: ["dotnet","fsharp","qrcode"]
---

## Introduction

Today I learned about MeCard file formats for representing contact information while reading the post [Sharing contacts using QR Code without requiring a network connection](https://andregarzia.com/2023/10/sharing-contacts-using-qr-code.html) by Andre Garzia.

Currently I host my [vCard](../vcard.vcf) on my site in a very similar way Andre describes in his post. I've also encoded it in [QR Code format](/files/images/contact/qr-vcard.svg) to make it easy to import and share.  

When I saw the MeCard format, I immediately liked how simple it was so I decided to see how difficult it'd be to write some code to generate a QR code for my own contact data.

## What is MeCard?

According to Wikipedia, MeCard is "...a data file similar to vCard...It is largely compatible with most QR-readers for smartphones. It is an easy way to share a contact with the most used fields. Usually, devices can recognize it and treat it like a contact ready to import."

An example might look like: `MECARD:N:Doe,John;TEL:13035551212;EMAIL:john.doe@example.com;;`

## Encoding text as a QR code using F#

I'd written a [snippet](/resources/snippets/qr-code-generator) a few years ago to help me generate the QR Codes for all accounts in [my contact page](/contact).

The snippet is a F# Interactive script which uses [Net.Codecrete.QrCodeGenerator NuGet package](https://www.nuget.org/packages/Net.Codecrete.QrCodeGenerator) to encode text as a QR code and saves it to an SVG file. 

The core logic consists of the following code:

```fsharp
let createQrCode (savePath:string) (target:string) = 
    let qr = QrCode.EncodeText(target,QrCode.Ecc.High)
    let svgString = qr.ToSvgString(4)
    File.WriteAllText(savePath,svgString, Encoding.UTF8)
```

In this case `target` is the text I want to encode and `savePath` is the path of the SVG file I'm saving the generated QR code in. 

Given MeCard with the following content:

```csharp
MECARD:N:Doe,John;TEL:13035551212;EMAIL:john.doe@example.com;;
```

The QR Code generated might look as follows:

![MECARD QR CODE](/files/images/mecard.svg)

Scanning the QR Code with a smartphone would then immediately recognize it as a contact and prompt you to save the contact information to your device.

## Conclusion

Since I already had the code snippet to do this available, creating a MeCard was straightforward. I've since added my [MeCard](/mecard.txt) alongside by vCard. You can check out the corresponding [QR code](/files/images/contact/qr-mecard.svg) in my [contact](/contact) page.