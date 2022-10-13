## Receiving Webmentions implementation plan

1. Create web service to receive webmentions and store source and target urls in an Azure Storage Table. 
    1. [Request verification](https://www.luisquintanilla.me/snippets/webmentions-request-verification) (Done)
    1. [Webmention verification](https://www.luisquintanilla.me/snippets/webmentions-verification) (Done)
    1. Web service (Done)
    1. Upload to Azure Storage Table (Done)
    1. Create Webmentions Rss feed to get notifications.
        1. Generate this after a new entry is posted.
        1. Store in blob storage
        1. Consume Rss feed from reader
1. Create Webmentions Page to display Webmentions.
    1. Do this by parsing Rss feed of approved mentions.
1. Create ~~Azure Function~~ to periodically delete broken links and deleted mentions.
    1. Run locally

## Stretch

1. Implement Admin panel for moderation.
1. Train ML.NET model to detect spam mentions.
1. Train ML.NET model to detect malicious mentions.