# Personal Website

## How to navigate this repo

- Program.fs (main application entrypoint that calls on builder components)
- Builder.fs (app logic to stitch together and build the website)
- Domain.fs (base types used throughout website)
- Views (contains layouts and partial views for the website)
- Services (contains components for RSS, OPML, Markdown, and Webmentions)
- Scripts (throwaway code used for tinkering)
- Data (data displayed inside website)
- _src (website content)
  - albums (photo collections - like Flickr)
  - feed (microblog note posts)
  - images (static images used throughout website posts and albums)
  - library (personal library collection containing books I'm reading or have read as well as notes)
  - posts (long-form blog posts)
  - presentation (revealjs based presentations and relevant resources)
  - responses (microblog replies,likes,reshares, and bookmarks posts)
  - snippets (code snippets - like GitHub Gists)
  - wiki (random notes on topics)
- _public (build output directory. This is what's uploaded to the server and displayed to visitors)
- _sratch (scratchpad to note down ideas and drafts)