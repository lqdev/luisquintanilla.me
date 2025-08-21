module PartialViews

// Import all the modular view components
open ComponentViews
open TagViews  
open FeedViews
open ContentViews
open CollectionViews
open LayoutViews

// Re-export commonly used functions to maintain compatibility with existing code
// This provides a single entry point while using the modular architecture

// Components (from ComponentViews)
let emptyView = ComponentViews.emptyView
let underConstructionView = ComponentViews.underConstructionView
let seasonalCheckmarkEmoji = ComponentViews.seasonalCheckmarkEmoji
let cardHeader = ComponentViews.cardHeader
let cardFooter = ComponentViews.cardFooter
let albumCardFooter = ComponentViews.albumCardFooter
let feedBacklink = ComponentViews.feedBacklink
let webmentionForm = ComponentViews.webmentionForm

// Tags (from TagViews)
let tagLinkView = TagViews.tagLinkView
let tagPostLinkView = TagViews.tagPostLinkView
let tagResponseLinkView = TagViews.tagResponseLinkView
let allTagsView = TagViews.allTagsView
let individualTagView = TagViews.individualTagView

// Feeds (from FeedViews)
let rollLinkView = FeedViews.rollLinkView
let blogRollView = FeedViews.blogRollView
let podRollView = FeedViews.podRollView
let forumsView = FeedViews.forumsView
let youTubeFeedView = FeedViews.youTubeFeedView
let aiStarterPackFeedView = FeedViews.aiStarterPackFeedView

// Content (from ContentViews)
let feedPostView = ContentViews.feedPostView
let notePostView = ContentViews.notePostView
let responsePostView = ContentViews.responsePostView
let bookmarkPostView = ContentViews.bookmarkPostView
let bookPostView = ContentViews.bookPostView
let albumPostView = ContentViews.albumPostView
let presentationPageView = ContentViews.presentationPageView
let liveStreamPageView = ContentViews.liveStreamPageView
let feedPostViewWithBacklink = ContentViews.feedPostViewWithBacklink
let notePostViewWithBacklink = ContentViews.notePostViewWithBacklink
let reponsePostViewWithBacklink = ContentViews.reponsePostViewWithBacklink
let albumPostViewWithBacklink = ContentViews.albumPostViewWithBacklink

// Collections (from CollectionViews)
let recentPostsView = CollectionViews.recentPostsView
let feedView = CollectionViews.feedView
let notesView = CollectionViews.notesView
let responseView = CollectionViews.responseView
let bookmarkView = CollectionViews.bookmarkView
let bookmarkResponseView = CollectionViews.bookmarkResponseView
let libraryView = CollectionViews.libraryView
let snippetsView = CollectionViews.snippetsView
let wikisView = CollectionViews.wikisView
let presentationsView = CollectionViews.presentationsView
let liveStreamsView = CollectionViews.liveStreamsView
let albumsPageView = CollectionViews.albumsPageView
let albumPageView = CollectionViews.albumPageView
let unifiedFeedView = CollectionViews.unifiedFeedView
let enhancedSubscriptionHubView = CollectionViews.enhancedSubscriptionHubView

// Layout (from LayoutViews)
let homeView = LayoutViews.homeView
let timelineHomeView = LayoutViews.timelineHomeView
let contentView = LayoutViews.contentView
let contentViewWithTitle = LayoutViews.contentViewWithTitle
let snippetPageView = LayoutViews.snippetPageView
let wikiPageView = LayoutViews.wikiPageView
let reviewPageView = LayoutViews.reviewPageView
let liveStreamView = LayoutViews.liveStreamView
let blogPostView = LayoutViews.blogPostView
let postPaginationView = LayoutViews.postPaginationView
let eventView = LayoutViews.eventView
let linkView = LayoutViews.linkView
