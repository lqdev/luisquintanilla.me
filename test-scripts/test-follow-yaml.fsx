#r "../bin/Debug/net11.0/YamlDotNet.dll"

open System
open System.IO
open YamlDotNet.Serialization

let mutable failures = 0
let deserializer = DeserializerBuilder().Build()

let check (name: string) (condition: bool) =
    if condition then
        printfn "  PASS  %s" name
    else
        failures <- failures + 1
        printfn "  FAIL  %s" name

let parseYaml (path: string) =
    try
        File.ReadAllText(path)
        |> deserializer.Deserialize<obj>
        |> ignore
        check (path + " parses as YAML") true
    with ex ->
        check (path + " parses as YAML") false
        printfn "         %s" ex.Message

let issueTemplate = ".github/ISSUE_TEMPLATE/follow.yml"
let action = ".github/actions/process-follow/action.yml"
let issueWorkflow = ".github/workflows/process-content-issue.yml"
let choiceWorkflow = ".github/workflows/process-follow-choice.yml"

[ issueTemplate; action; issueWorkflow; choiceWorkflow ]
|> List.iter parseYaml

let templateText = File.ReadAllText(issueTemplate)
check "Feed URL is optional in the form" (
    System.Text.RegularExpressions.Regex.IsMatch(
        templateText,
        @"(?s)id:\s*feed_url.*?required:\s*false"
    )
)
check "Title form id is site_title while label remains Title" (
    templateText.Contains("id: site_title", StringComparison.Ordinal)
    && templateText.Contains("label: Title", StringComparison.Ordinal)
)
check "Form explains mandatory-feed no-feed closure" (
    templateText.Contains("A feed is still mandatory", StringComparison.Ordinal)
    && templateText.Contains("closed as not planned", StringComparison.Ordinal)
)
let choiceText = File.ReadAllText(choiceWorkflow)
check "Choice workflow handles issue body edits and created comments" (
    choiceText.Contains("issues:", StringComparison.Ordinal)
    && choiceText.Contains("types: [edited]", StringComparison.Ordinal)
    && choiceText.Contains("issue_comment:", StringComparison.Ordinal)
    && choiceText.Contains("types: [created]", StringComparison.Ordinal)
)
check "Choice workflow is restricted to lqdev and open follow issues" (
    choiceText.Contains("github.event.sender.login == 'lqdev'", StringComparison.Ordinal)
    && choiceText.Contains("github.event.sender.login != 'github-actions[bot]'", StringComparison.Ordinal)
    && choiceText.Contains("github.event.issue.state == 'open'", StringComparison.Ordinal)
    && choiceText.Contains("github.event.issue.pull_request == null", StringComparison.Ordinal)
)

if failures > 0 then exit 1
