#r "nuget:Net.Codecrete.QrCodeGenerator"

open Net.Codecrete.QrCodeGenerator
open System.IO
open System.Text

let target = "https://www.luisquintanilla.me/"
let savePath = Path.Combine(__SOURCE_DIRECTORY__, "..", "_src", "assets", "images", "contact", "qr-home.svg")

let qr = QrCode.EncodeText(target, QrCode.Ecc.High)
let svgString = qr.ToSvgString(4)
File.WriteAllText(savePath, svgString, Encoding.UTF8)

printfn "Wrote %s (%d bytes)" savePath svgString.Length
