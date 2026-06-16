module AssetsBuilder

    open System
    open System.IO
    open BuilderCommon

    let rec cleanOutputDirectory (outputDir:string) = 
        if Directory.Exists(outputDir) then
            let dirInfo = DirectoryInfo(outputDir)

            dirInfo.GetFiles()
            |> Array.iter(fun x -> x.Delete())

            dirInfo.GetDirectories()
            |> Array.iter(fun x -> 
                cleanOutputDirectory x.FullName
                x.Delete())

    let copyStaticFiles () =
        // Asset directories to copy to /assets/
        let assetDirectories = [
            ("css", "assets/css")
            ("js", "assets/js") 
            ("lib", "assets/lib")
        ]

        // Copy asset directories to /assets/
        assetDirectories
        |> List.iter(fun (srcPath, destPath) ->
            let sourcePath = Path.Join(srcDir, srcPath)
            let destPath = Path.Join(outputDir, destPath)
            
            if Directory.Exists(sourcePath) then
                Directory.CreateDirectory(destPath) |> ignore
                
                // Copy all files recursively
                Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories)
                |> Array.iter(fun file ->
                    let relativePath = Path.GetRelativePath(sourcePath, file)
                    let destFile = Path.Join(destPath, relativePath)
                    Directory.CreateDirectory(Path.GetDirectoryName(destFile)) |> ignore
                    File.Copy(file, destFile, true))
        )

        // Copy other static directories at root level
        let staticDirectories = [
            "assets/images"
            ".well-known"
            "lib"
        ]

        staticDirectories
        |> List.iter(fun dir ->
            let sourcePath = Path.Join(srcDir, dir)
            let destPath = Path.Join(outputDir, dir)
            
            if Directory.Exists(sourcePath) then
                Directory.CreateDirectory(destPath) |> ignore
                
                Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories)
                |> Array.iter(fun file ->
                    let relativePath = Path.GetRelativePath(sourcePath, file)
                    let destFile = Path.Join(destPath, relativePath)
                    Directory.CreateDirectory(Path.GetDirectoryName(destFile)) |> ignore
                    File.Copy(file, destFile, true))
        )
        
        // Copy favicon & avatar
        File.Copy(Path.Join(srcDir,"favicon.ico"),Path.Join(outputDir,"favicon.ico"),true)
        File.Copy(Path.Join(srcDir,"avatar.png"),Path.Join(outputDir,"avatar.png"),true)
        File.Copy(Path.Join(srcDir,"art-profile.png"),Path.Join(outputDir,"art-profile.png"),true)

        // Copy contact cards
        File.Copy(Path.Join(srcDir,"vcard.vcf"),Path.Join(outputDir,"vcard.vcf"),true)
        File.Copy(Path.Join(srcDir,"mecard.txt"),Path.Join(outputDir,"mecard.txt"),true)
        
        // Copy PWA files
        File.Copy(Path.Join(srcDir,"service-worker.js"),Path.Join(outputDir,"service-worker.js"),true)
        File.Copy(Path.Join(srcDir,"manifest.json"),Path.Join(outputDir,"manifest.json"),true)
        File.Copy(Path.Join(srcDir,"offline.html"),Path.Join(outputDir,"offline.html"),true)

        // Generate the homepage avatar flip-card QR. Emits a fully styled
        // SVG (slate rounded dots, orange rounded finder corners, embedded
        // avatar at center) directly into _public/assets/images/contact/ so
        // the avatarFlipCard view can reference it as a static asset without
        // pulling in qr-code-styling JS on the homepage hot path. Runs after
        // copyStaticFiles so it lands on top of (or alongside) any copied
        // contact-QR family members. See Services/QRStyled.fs.
        let qrOpts =
            Services.QRStyled.defaultOptions
                "https://lqdev.me/"
                (Some (Path.Join(outputDir, "avatar.png")))
        let qrOutPath = Path.Join(outputDir, "assets", "images", "contact", "qr-home.svg")
        Services.QRStyled.renderToFile qrOpts qrOutPath |> ignore

    /// Copy the Azure Static Web Apps config and pre-create the fixed output
    /// subdirectories the typed builders write into. Pure filesystem setup,
    /// lifted verbatim from Program.main's prep block (composition-root tidy).
    let prepareDirectories (outputDir: string) =
        // Copy Azure Static Web Apps configuration
        let configSourcePath = "staticwebapp.config.json"
        let configTargetPath = Path.Join(outputDir, "staticwebapp.config.json")
        if File.Exists(configSourcePath) then
            File.Copy(configSourcePath, configTargetPath, true)
            printfn "✅ Azure Static Web Apps configuration copied to output directory"

        // Create the fixed output subdirectories the typed builders write into
        Path.Join(outputDir,"feed") |> Directory.CreateDirectory |> ignore
        Path.Join(outputDir,"feed","responses") |> Directory.CreateDirectory |> ignore
        Path.Join(outputDir,"feed","starter") |> Directory.CreateDirectory |> ignore
        Path.Join(outputDir,"posts") |> Directory.CreateDirectory |> ignore
        Path.Join(outputDir,"presentations") |> Directory.CreateDirectory |> ignore
        Path.Join(outputDir,"snippets") |> Directory.CreateDirectory |> ignore
        Path.Join(outputDir,"wiki") |> Directory.CreateDirectory |> ignore
        Path.Join(outputDir,"tags") |> Directory.CreateDirectory |> ignore

    // Phase 3: pre-render a per-page styled QR SVG for every content page.
    // Same look as the homepage QR (slate dots #1a2332, orange finders
    // #ff6b35) with a downsized embedded center avatar, written to
    // `_public/assets/images/qr/<type>/<slug>.svg`. Replaces the runtime
    // `qr-code-styling` JS modal in `_src/js/qrcode.js`.
    //
    // The avatar is resized once at build start (full-res ~71 KB ->
    // 64 px / ~4.6 KB PNG) and re-read from disk by the inner renderer;
    // OS file cache absorbs the repeated reads. See
    // `Services/QRStyled.fs:ensureSmallAvatar`.
    let buildPerPageQRs (outputDir: string) (items: UnifiedFeeds.UnifiedFeedItem list) =
        let avatarSrc = Path.Join(outputDir, "avatar.png")
        let smallAvatarPath = Path.Join(outputDir, "assets", "images", "qr", "_avatar-sm.png")
        Services.QRStyled.ensureSmallAvatar avatarSrc smallAvatarPath |> ignore

        let mutable count = 0
        let mutable totalBytes = 0L
        for item in items do
            // item.Url is "https://www.lqdev.me/<path>/" (canonical www form used
            // across the site). Strip both apex variants, trim trailing slash,
            // emit one SVG per item. QR payload uses apex (lqdev.me) to match
            // the homepage QR convention (Phase 2 decision).
            let relPath =
                item.Url
                    .Replace("https://www.lqdev.me/", "")
                    .Replace("https://lqdev.me/", "")
                    .TrimEnd('/')
            if not (String.IsNullOrWhiteSpace relPath) then
                let payload = "https://lqdev.me/" + relPath + "/"
                let svgPath = Path.Join(outputDir, "assets", "images", "qr", relPath + ".svg")
                let opts =
                    Services.QRStyled.perPageOptions payload (Some smallAvatarPath)
                Services.QRStyled.renderPageQR opts svgPath item.Url |> ignore
                count <- count + 1
                totalBytes <- totalBytes + (FileInfo svgPath).Length
        printfn "📱 Generated %d per-page QR SVGs (%.1f MB total, %.1f KB avg)"
            count
            (float totalBytes / 1048576.0)
            (float totalBytes / float (max 1 count) / 1024.0)
