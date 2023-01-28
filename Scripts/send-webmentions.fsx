#r "../bin/Debug/net7.0/PersonalSite.dll"

Builder.loadReponses()
|> WebmentionService.sendWebmentions