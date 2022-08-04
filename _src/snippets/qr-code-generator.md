---
title: "QR Code Generator"
language: "F#"
tags: .net,F#,script 
---

## Description

Script to generate a QR Code and save as PNG image from a URL

## Usage

```bash
dotnet fsi qr-code-generator.fsx "my-qr-code.png" "https://twitter.com/user-profile"
```

## Snippet 

### qr-code-generator.fsx

```fsharp
open System.Threading

printfn "Loading packages"

#r "nuget:Net.Codecrete.QrCodeGenerator"

Thread.Sleep(5000)

printfn "Loaded QrCodeGenerator"

#r "nuget:System.Drawing.Common"

Thread.Sleep(5000)
printfn "Loaded System Drawing"

open Net.Codecrete.QrCodeGenerator
open System.Drawing.Imaging

let createQrCode (savePath:string) (target:string) = 
    let qr = QrCode.EncodeText(target,QrCode.Ecc.High)
    use bitmap = qr.ToBitmap(4,5)
    bitmap.Save(savePath,ImageFormat.Png)

let args = fsi.CommandLineArgs
let savePath = args.[1]
let target = args.[2]

createQrCode savePath target
```