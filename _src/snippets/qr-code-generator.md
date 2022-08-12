---
title: "QR Code Generator"
language: "F#"
tags: .net,f#,script
---

## Description

Script to generate a QR Code and save as PNG image from a URL

## Usage

```bash
dotnet fsi qr-code-generator.fsx "my-qr-code.svg" "https://twitter.com/user-profile"
```

## Snippet 

### qr-code-generator.fsx

```fsharp
open System.Threading

printfn "Loading packages"

#r "nuget:Net.Codecrete.QrCodeGenerator"

Thread.Sleep(5000)

printfn "Loaded QrCodeGenerator"

open Net.Codecrete.QrCodeGenerator
open System.IO
open System.Text

let createQrCode (savePath:string) (target:string) = 
    let qr = QrCode.EncodeText(target,QrCode.Ecc.High)
    let svgString = qr.ToSvgString(4)
    File.WriteAllText(savePath,svgString, Encoding.UTF8)
    
let args = fsi.CommandLineArgs
let savePath = args.[1]
let target = args.[2]

createQrCode savePath target
```