#r "../bin/Debug/net9.0/PersonalSite.dll"

Loaders.loadReponses("_src")
|> WebmentionService.sendWebmentions