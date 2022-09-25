## Receiving Webmentions implementation plan

1. Create Azure Function to receive webmentions and store source and target urls in an Azure Storage Table. 
    1. [Request verification](https://www.luisquintanilla.me/snippets/webmentions-request-verification)
    1. [Webmention verification](https://www.luisquintanilla.me/snippets/webmentions-verification)
1. Create Webmentions Page to display Webmentions. 
1. Create Webmentions Rss feed to get notifications. 
1. Create Azure Function to periodically query approved mentions. 
1. Create Azure Function to periodically delete broken links and deleted mentions. 

## Stretch

1. Implement Admin panel for moderation 
1. Train ML.NET model to detect spam mentions
1. Train ML.NET model to detect malicious mentions. 