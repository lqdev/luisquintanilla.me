// Docs integrity test.
// Run from the repo root:  dotnet fsi test-scripts/test-docs-integrity.fsx
// Add --all to print every finding instead of the first 15 per check.
// Exits 1 when any failing check has findings. Warnings never fail the run.
//
open System
open System.Collections.Generic
open System.Diagnostics
open System.IO
open System.Text.RegularExpressions

// ---------------------------------------------------------------------------
// Configuration. Later layers only edit this block.
// ---------------------------------------------------------------------------

/// Directories whose Markdown files are checked for links and paths.
let scopeDirs : string list = [ "docs" ]

/// Individual files checked for links and paths. Empty at first; layers add the files they fix:
/// layer 4 adds the sub-READMEs, layer 6 README.md and CONTRIBUTING.md, layer 7 AGENTS.md.
let scopeFiles : string list = []

/// Checked for links but not for path tokens: decision records describe the code as it was
/// when the decision was made, so old file names in them are correct.
let pathCheckExcludedPrefixes : string list = [ "docs/adr/" ]

/// Never checked: historical records and generated or published content.
let excludedPrefixes : string list =
    [ "archive/"; "projects/archive/"; "_src/"; "_public/"; "node_modules/"; "bin/"; "obj/" ]

/// Files and directories held to the voice rules (see docs/contributing/voice.md).
/// Starts small on purpose. Each layer adds the prose it rewrites.
let styleFiles : string list = []
let styleDirs : string list = [ "docs/contributing" ]

/// Markdown files allowed to sit directly in docs/.
let allowedDocsRootFiles : string list =
    [ "README.md"; "principles.md"; "architecture.md"; "getting-started.md" ]

/// Maximum line counts. Empty until the layer that rewrites each file adds its entry:
/// layer 6 adds "README.md", 150 and "CONTRIBUTING.md", 80; layer 7 adds "AGENTS.md", 150.
let lineBudgets : (string * int) list = []
let instructionFileBudget = 60
let skillFileBudget = 500

/// Directories that hold one skill per immediate subdirectory. Empty until layer 9, because
/// .github/skills/universal/ is a container folder, not a skill, until it moves to .agents/.
/// Layer 9 sets this to [ ".github/skills"; ".agents/skills" ].
let skillRoots : string list = []

/// Path-looking tokens that are allowed even though no such file exists.
let allowFile = "test-scripts/docs-integrity-allow.txt"

// ---------------------------------------------------------------------------
// Repository facts
// ---------------------------------------------------------------------------

let repoRoot = Directory.GetCurrentDirectory()
let norm (p: string) = p.Replace('\\', '/')
let showAll = fsi.CommandLineArgs |> Array.contains "--all"

let git (args: string) : string[] =
    let psi = ProcessStartInfo("git", args)
    psi.RedirectStandardOutput <- true
    psi.UseShellExecute <- false
    psi.WorkingDirectory <- repoRoot
    use p = Process.Start psi
    let out = p.StandardOutput.ReadToEnd()
    p.WaitForExit()
    out.Split([| '\n' |], StringSplitOptions.RemoveEmptyEntries) |> Array.map (fun s -> s.Trim())

let tracked : string[] = git "-c core.quotepath=off ls-files" |> Array.map norm
let ci = StringComparer.OrdinalIgnoreCase
let baseNames = HashSet<string>(tracked |> Array.map (fun f -> f.Substring(f.LastIndexOf('/') + 1)), ci)
let topLevel = HashSet<string>(tracked |> Array.map (fun f -> f.Split('/').[0]), ci)

let existsInRepo (relative: string) : bool =
    let full = Path.Combine(repoRoot, relative.TrimEnd('/'))
    File.Exists full || Directory.Exists full

let allow : HashSet<string> =
    let path = Path.Combine(repoRoot, allowFile)
    if File.Exists path then
        HashSet<string>(
            File.ReadAllLines path
            |> Array.map (fun l -> l.Trim())
            |> Array.filter (fun l -> l <> "" && not (l.StartsWith "#")), ci)
    else HashSet<string>(ci)

let isExcluded (relative: string) : bool =
    excludedPrefixes |> List.exists (fun p -> relative.StartsWith(p, StringComparison.OrdinalIgnoreCase))

let markdownUnder (dirs: string list) (files: string list) : string list =
    let fromDirs =
        dirs
        |> List.filter (fun d -> Directory.Exists(Path.Combine(repoRoot, d)))
        |> List.collect (fun d ->
            Directory.EnumerateFiles(Path.Combine(repoRoot, d), "*.md", SearchOption.AllDirectories)
            |> Seq.map (fun f -> norm (Path.GetRelativePath(repoRoot, f)))
            |> List.ofSeq)
    let fromFiles = files |> List.filter (fun f -> File.Exists(Path.Combine(repoRoot, f)))
    fromDirs @ fromFiles |> List.filter (isExcluded >> not) |> List.distinct |> List.sort

// ---------------------------------------------------------------------------
// Findings
// ---------------------------------------------------------------------------

type Finding = { Check: string; File: string; Line: int; Message: string }
let findings = ResizeArray<Finding>()
let warnings = ResizeArray<Finding>()
let fail (check: string) (file: string) (line: int) (message: string) =
    findings.Add { Check = check; File = file; Line = line; Message = message }
let warn (check: string) (file: string) (line: int) (message: string) =
    warnings.Add { Check = check; File = file; Line = line; Message = message }

/// Every line of a Markdown file with its 1-based number and whether it is inside a code fence.
let markdownLines (relative: string) : (int * string * bool) list =
    let result = ResizeArray<int * string * bool>()
    let mutable inFence = false
    let lines = File.ReadAllLines(Path.Combine(repoRoot, relative))
    for i in 0 .. lines.Length - 1 do
        let t = lines.[i].TrimStart()
        if t.StartsWith "```" || t.StartsWith "~~~" then inFence <- not inFence
        else result.Add((i + 1, lines.[i], inFence))
    List.ofSeq result

/// Lines outside fenced code blocks.
let proseLines (relative: string) : (int * string) list =
    markdownLines relative |> List.choose (fun (n, line, fenced) -> if fenced then None else Some(n, line))

/// Lines inside fenced code blocks.
let fencedLines (relative: string) : (int * string) list =
    markdownLines relative |> List.choose (fun (n, line, fenced) -> if fenced then Some(n, line) else None)

// ---------------------------------------------------------------------------
// Check 1: relative links resolve.  Check 2: path-looking tokens exist.
// ---------------------------------------------------------------------------

let linkRx = Regex(@"\[[^\]]*\]\(\s*<?([^)\s>]+)>?(?:\s+""[^""]*"")?\s*\)")
let codeSpanRx = Regex(@"`([^`\n]+)`")
let boldSpanRx = Regex(@"\*\*([^*\n]+)\*\*")
let pathTokenRx = Regex(@"^[A-Za-z0-9_.\-/\\]+$")
/// A bare file name must have a stem: "Builder.fs" qualifies, ".fs" does not.
let bareNameRx = Regex(@"^[A-Za-z0-9_\-][A-Za-z0-9_.\-]*\.[a-z][a-z0-9]*$")
let pathExtensions =
    [ ".fs"; ".fsx"; ".fsproj"; ".md"; ".yml"; ".yaml"; ".json"; ".ps1"; ".sh"; ".js"; ".css"; ".py" ]
/// Inside code fences only commands are checked, so only script-like extensions count.
let fencedExtensions = [ ".fsx"; ".ps1"; ".sh"; ".py"; ".yml"; ".yaml" ]

let linkTargets (relative: string) : (int * string) list =
    [ for n, line in proseLines relative do
        // Inline code often shows link syntax as an example; ignore it.
        let text = codeSpanRx.Replace(line, "")
        for m in linkRx.Matches text do
            yield (n, m.Groups.[1].Value.Trim()) ]

let resolveLink (fromFile: string) (target: string) : string option =
    let isExternal =
        target.StartsWith "http://" || target.StartsWith "https://" || target.StartsWith "mailto:"
        || target.StartsWith "#" || target.StartsWith "/" || target.Contains "{"
    // "url" or "image-url" is a placeholder in a syntax example, not a path.
    let isPlaceholder = not (target.Contains "/") && not (target.Contains ".")
    let withoutAnchor = target.Split('#').[0].Split('?').[0]
    if isExternal || isPlaceholder || withoutAnchor = "" then None
    else
        let dir = Path.GetDirectoryName(Path.Combine(repoRoot, fromFile))
        let full = Path.GetFullPath(Path.Combine(dir, Uri.UnescapeDataString withoutAnchor))
        Some(norm (Path.GetRelativePath(repoRoot, full)))

let checkLinks (relative: string) =
    for n, target in linkTargets relative do
        match resolveLink relative target with
        | Some resolved when not (existsInRepo resolved) -> fail "1-links" relative n $"broken link: {target}"
        | _ -> ()

let hasExtension (extensions: string list) (token: string) : bool =
    extensions |> List.exists (fun e -> token.EndsWith(e, StringComparison.Ordinal))

/// Reports a token that looks like a repo path or file name but does not exist.
let checkToken (extensions: string list) (relative: string) (n: int) (raw: string) =
    let token = norm (raw.Trim())
    // "./name" points at the repo root, so a file with that name elsewhere does not count.
    let rootRelative = token.StartsWith "./"
    let token = if rootRelative then token.Substring 2 else token
    let skip =
        token.Length < 3 || not (pathTokenRx.IsMatch token) || token.StartsWith "/"
        || token.StartsWith "~" || token.StartsWith "_public/" || token.Contains "..."
        || allow.Contains token
    if not skip then
        if token.Contains "/" then
            let first = token.Split('/').[0]
            if topLevel.Contains first && not (existsInRepo token) then
                fail "2-paths" relative n $"path does not exist: {token}"
        elif bareNameRx.IsMatch token && hasExtension extensions token then
            let found = existsInRepo token || (not rootRelative && baseNames.Contains token)
            if not found then
                fail "2-paths" relative n $"no file with this name: {token}"

let checkPathTokens (relative: string) =
    // Prose: file names written as `code` or **bold**.
    for n, line in proseLines relative do
        for m in codeSpanRx.Matches line do
            checkToken pathExtensions relative n m.Groups.[1].Value
        for m in boldSpanRx.Matches line do
            checkToken pathExtensions relative n m.Groups.[1].Value
    // Fenced blocks: script and workflow paths in commands.
    for n, line in fencedLines relative do
        for word in line.Split([| ' '; '\t'; '"'; '\''; '('; ')' |], StringSplitOptions.RemoveEmptyEntries) do
            if hasExtension fencedExtensions word then
                checkToken fencedExtensions relative n word

// ---------------------------------------------------------------------------
// Check 3: nothing loose in docs/.  Check 4: every docs page is reachable.
// ---------------------------------------------------------------------------

let checkDocsRoot () =
    let docs = Path.Combine(repoRoot, "docs")
    if Directory.Exists docs then
        for f in Directory.EnumerateFiles(docs, "*.md", SearchOption.TopDirectoryOnly) do
            let name = Path.GetFileName f
            if not (allowedDocsRootFiles |> List.exists (fun a -> ci.Equals(a, name))) then
                fail "3-docs-root" $"docs/{name}" 1 "file sits directly in docs/; move it into a section folder or archive/"

let checkReachable () =
    let index = "docs/README.md"
    if File.Exists(Path.Combine(repoRoot, index)) then
        let linked (file: string) : string list =
            linkTargets file
            |> List.choose (fun (_, t) -> resolveLink file t)
            |> List.collect (fun r ->
                if r.EndsWith ".md" then [ r ]
                elif Directory.Exists(Path.Combine(repoRoot, r)) then [ r.TrimEnd('/') + "/README.md" ]
                else [])
            |> List.filter (fun r -> File.Exists(Path.Combine(repoRoot, r)))
        let firstHop = linked index
        let secondHop = firstHop |> List.filter (fun f -> f.EndsWith "/README.md") |> List.collect linked
        let reachable = HashSet<string>(index :: firstHop @ secondHop, ci)
        for page in markdownUnder [ "docs" ] [] do
            if not (reachable.Contains page) then
                fail "4-orphans" page 1 "not linked from docs/README.md or from a folder README it links to"

// ---------------------------------------------------------------------------
// Check 5: line budgets.  Check 6: skill frontmatter.
// ---------------------------------------------------------------------------

let lineCount (relative: string) = File.ReadAllLines(Path.Combine(repoRoot, relative)).Length

let checkBudgets () =
    for file, budget in lineBudgets do
        if File.Exists(Path.Combine(repoRoot, file)) && lineCount file > budget then
            fail "5-budgets" file 1 $"{lineCount file} lines; budget is {budget}"
    let instructions = Path.Combine(repoRoot, ".github", "instructions")
    if Directory.Exists instructions then
        for f in Directory.EnumerateFiles(instructions, "*.instructions.md") do
            let rel = norm (Path.GetRelativePath(repoRoot, f))
            if lineCount rel > instructionFileBudget then
                fail "5-budgets" rel 1 $"{lineCount rel} lines; budget is {instructionFileBudget}"

let skillNameRx = Regex(@"^[a-z0-9]+(-[a-z0-9]+)*$")

let checkSkills () =
    for root in skillRoots do
        let full = Path.Combine(repoRoot, root)
        if Directory.Exists full then
            for dir in Directory.EnumerateDirectories full do
                let dirName = Path.GetFileName dir
                let rel = $"{root}/{dirName}/SKILL.md"
                if not (File.Exists(Path.Combine(dir, "SKILL.md"))) then
                    fail "6-skills" $"{root}/{dirName}" 1 "skill directory has no SKILL.md (skills must be immediate children)"
                else
                    let lines = File.ReadAllLines(Path.Combine(dir, "SKILL.md"))
                    let field (key: string) =
                        lines
                        |> Array.tryFind (fun l -> l.StartsWith(key + ":"))
                        |> Option.map (fun l -> l.Substring(key.Length + 1).Trim().Trim('"', '\''))
                    match field "name" with
                    | None -> fail "6-skills" rel 1 "frontmatter has no name"
                    | Some n when n <> dirName -> fail "6-skills" rel 1 $"name '{n}' must equal directory name '{dirName}'"
                    | Some n when not (skillNameRx.IsMatch n) || n.Length > 64 -> fail "6-skills" rel 1 $"name '{n}' is not lowercase-hyphenated or is over 64 characters"
                    | Some _ -> ()
                    match field "description" with
                    | None -> fail "6-skills" rel 1 "frontmatter has no single-line description"
                    | Some d when d.Length = 0 || d.Length > 1024 -> fail "6-skills" rel 1 $"description is {d.Length} characters; must be 1 to 1024"
                    | Some _ -> ()
                    if lines.Length > skillFileBudget then
                        fail "6-skills" rel 1 $"{lines.Length} lines; budget is {skillFileBudget}"

// ---------------------------------------------------------------------------
// Check 7: voice. Fails on marks the author's own writing does not use.
// ---------------------------------------------------------------------------

let curlyQuoteRx = Regex("[\u2018\u2019\u201C\u201D]")
let emojiRx = Regex(@"[\u2600-\u27BF\u2B50\u2B55]|[\uD83C-\uD83E][\uDC00-\uDFFF]")
let boldLeadRx = Regex(@"^\s*[-*]\s+\*\*[^*]+\*\*\s*[:\-]")
let boldRx = Regex(@"\*\*[^*]+\*\*")
let wordRx = Regex(@"\b[\w']+\b")
let bannedRx =
    Regex(
        @"\b(delv(e|es|ing)|leverag(e|es|ed|ing)|seamless(ly)?|robust|comprehensive(ly)?|genuinely|pivotal|testament|"
        + @"underscor(e|es|ed|ing)|foster(s|ed|ing)?|showcas(e|es|ed|ing)|tapestry|elevat(e|es|ed|ing)|empower(s|ed|ing)?|"
        + @"game.changer|deep dive|production.ready|cutting.edge|state.of.the.art|world.class|best.in.class|effortless(ly)?|"
        + @"research.backed|successfully (implemented|completed|migrated)|it'?s worth noting|it is important to note|"
        + @"in conclusion|more than just|not (just|only|merely) [^.]{1,60} but|plays? an? (key|crucial|vital|pivotal) role)\b",
        RegexOptions.IgnoreCase)

let checkVoice (relative: string) =
    let mutable words = 0
    let mutable bold = 0
    for n, line in proseLines relative do
        let text = codeSpanRx.Replace(line, "")
        words <- words + wordRx.Matches(text).Count
        bold <- bold + boldRx.Matches(text).Count
        if text.Contains '\u2014' then fail "7-voice" relative n "em dash; use a comma, a colon, or two sentences"
        if curlyQuoteRx.IsMatch text then fail "7-voice" relative n "curly quote; use straight quotes"
        if emojiRx.IsMatch text then fail "7-voice" relative n "emoji or status symbol"
        for m in bannedRx.Matches text do
            fail "7-voice" relative n $"phrase to cut: \"{m.Value}\""
        if boldLeadRx.IsMatch text then warn "7-voice" relative n "bullet opens with a bold label; write a sentence or use a table"
    if words >= 200 && float bold * 1000.0 / float words > 8.0 then
        let perThousand = float bold * 1000.0 / float words
        warn "7-voice" relative 1 (sprintf "%.1f bold spans per 1,000 words; the author's posts average 1.5" perThousand)

// ---------------------------------------------------------------------------
// Run
// ---------------------------------------------------------------------------

let scope = markdownUnder scopeDirs scopeFiles
for file in scope do
    checkLinks file
    let skipPaths =
        pathCheckExcludedPrefixes
        |> List.exists (fun p -> file.StartsWith(p, StringComparison.OrdinalIgnoreCase))
    if not skipPaths then checkPathTokens file
checkDocsRoot ()
checkReachable ()
checkBudgets ()
checkSkills ()
for file in markdownUnder styleDirs styleFiles do
    checkVoice file

let report (title: string) (items: seq<Finding>) =
    for check, group in items |> Seq.groupBy (fun f -> f.Check) |> Seq.sortBy fst do
        let group = List.ofSeq group
        let files = group |> List.map (fun f -> f.File) |> List.distinct |> List.length
        printfn "%s %s: %d in %d file(s)" title check group.Length files
        let shown = if showAll then group else List.truncate 15 group
        for f in shown do
            printfn "  %s:%d: %s" f.File f.Line f.Message
        if group.Length > shown.Length then
            printfn "  ... %d more (run with --all)" (group.Length - shown.Length)

printfn "Docs integrity: %d file(s) in scope" scope.Length
report "FAIL" findings
report "WARN" warnings
if findings.Count = 0 then
    printfn "OK: no failures (%d warning(s))" warnings.Count
    exit 0
else
    printfn "FAILED: %d finding(s), %d warning(s)" findings.Count warnings.Count
    exit 1
