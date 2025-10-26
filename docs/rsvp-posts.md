# RSVP Posts

This document explains how to create and use RSVP (Répondez s'il vous plaît) posts on your website using the custom RSVP block syntax.

## What are RSVP Posts?

RSVP posts are a type of [IndieWeb post](https://indieweb.org/rsvp) that lets you publicly respond to event invitations on your own website. They're part of the IndieWeb philosophy of owning your data and social interactions.

When you RSVP to an event, you're creating a post on your site that:
- References the event you're responding to
- Indicates your attendance status (yes, no, maybe, interested)
- Can include additional notes or comments
- Uses IndieWeb microformats for interoperability

## Use Cases

RSVPs are perfect for:

- **Community Events**: Responding to meetups, conferences, or community gatherings
- **Virtual Events**: RSVPing to webinars, online workshops, or live streams
- **Social Gatherings**: Indicating attendance at parties, dinners, or informal get-togethers
- **Professional Events**: Responding to conferences, talks, or networking events
- **Open Source Events**: RSVPing to hackathons, code sprints, or community days

RSVPs help:
- Keep event organizers informed
- Let your followers know what you're attending
- Maintain a personal history of events
- Support IndieWeb event discovery and coordination

## RSVP Custom Block Syntax

The RSVP feature uses a custom markdown block with the `:::rsvp` syntax. The block contains YAML-formatted data about the event and your response.

### Basic Syntax

```markdown
:::rsvp
event_name: "Event Name"
event_url: "https://example.com/event"
rsvp_status: "yes"
:::
```

### Complete Syntax with All Fields

```markdown
:::rsvp
event_name: "Event Name"
event_url: "https://example.com/event"
event_date: "2025-11-15 14:00"
rsvp_status: "yes"
event_location: "Seattle, WA"
notes: "Looking forward to this!"
:::
```

## RSVP Fields

### Required Fields

- **event_name** (string): The name of the event
- **event_url** (string): URL to the event page or invitation
- **rsvp_status** (string): Your response status - must be one of:
  - `"yes"` - ✅ Attending
  - `"no"` - ❌ Not Attending
  - `"maybe"` - ❓ Maybe Attending
  - `"interested"` - ⭐ Interested

### Optional Fields

- **event_date** (string): Date and time of the event (any readable format)
- **event_location** (string): Physical or virtual location of the event
- **notes** (string): Additional comments or notes about your RSVP

## Examples

### Example 1: Basic RSVP - Attending a Meetup

```markdown
---
post_type: "article"
title: "Attending the F# Meetup"
published_date: "2025-11-01 10:00 -05:00"
tags: ["events", "fsharp", "community"]
---

I'm excited to attend the upcoming F# meetup!

:::rsvp
event_name: "F# Seattle Monthly Meetup"
event_url: "https://www.meetup.com/fsharp-seattle/events/123456/"
event_date: "2025-11-15 18:00"
rsvp_status: "yes"
event_location: "Seattle, WA"
notes: "Can't wait to learn about functional programming patterns!"
:::
```

### Example 2: Maybe Attending - Checking Schedule

```markdown
---
post_type: "note"
title: "Might make it to the conference"
published_date: "2025-11-01 14:30 -05:00"
tags: ["events", "conference"]
---

:::rsvp
event_name: "IndieWeb Summit 2025"
event_url: "https://events.indieweb.org/2025/summit"
event_date: "2025-12-10"
rsvp_status: "maybe"
notes: "Need to check my December schedule first"
:::
```

### Example 3: Not Attending - Previous Commitment

```markdown
---
post_type: "note"
title: "Can't make the workshop"
published_date: "2025-11-01 16:00 -05:00"
tags: ["events"]
---

Unfortunately, I have a conflict and won't be able to attend.

:::rsvp
event_name: "Web Development Workshop"
event_url: "https://example.com/workshop"
event_date: "2025-11-20 14:00"
rsvp_status: "no"
notes: "Hope to catch the next one!"
:::
```

### Example 4: Interested - Virtual Event

```markdown
---
post_type: "note"
title: "Interested in the livestream"
published_date: "2025-11-02 09:00 -05:00"
tags: ["events", "livestream"]
---

:::rsvp
event_name: "Building Static Sites with F#"
event_url: "https://www.youtube.com/watch?v=example"
rsvp_status: "interested"
event_location: "Online (YouTube Live)"
:::
```

### Example 5: Multiple RSVPs in One Post

```markdown
---
post_type: "article"
title: "My November Event Schedule"
published_date: "2025-11-01 12:00 -05:00"
tags: ["events", "schedule"]
---

Here are the events I'm planning to attend this month:

:::rsvp
event_name: "F# Seattle Monthly Meetup"
event_url: "https://www.meetup.com/fsharp-seattle/events/123456/"
event_date: "2025-11-15 18:00"
rsvp_status: "yes"
event_location: "Seattle, WA"
:::

:::rsvp
event_name: "Tech Conference 2025"
event_url: "https://techconf.example.com"
event_date: "2025-11-22"
rsvp_status: "maybe"
notes: "Waiting for speaker lineup announcement"
:::
```

## Core Components

The RSVP feature is built on several F# components:

### RsvpData Type

Defined in `CustomBlocks.fs`:

```fsharp
[<CLIMutable>]
type RsvpData = {
    event_name: string
    event_url: string
    event_date: string
    rsvp_status: string  // "yes", "no", "maybe", "interested"
    event_location: string option
    notes: string option
}
```

### RsvpBlock Type

A custom block type that extends Markdig's container block system:

```fsharp
type RsvpBlock(parser: BlockParser) =
    inherit ContainerBlock(parser)
    member val RsvpData: RsvpData option = None with get, set
    member val RawContent: string = "" with get, set
    interface ICustomBlock with
        member _.BlockType = "rsvp"
        member this.RawContent = this.RawContent
```

### RsvpRenderer Module

Defined in `BlockRenderers.fs`, this module handles rendering RSVP blocks to HTML:

```fsharp
module RsvpRenderer =
    let render (rsvp: RsvpData) =
        // Renders event name with h3 heading
        // Displays status with appropriate emoji (✅, ❌, ❓, ⭐)
        // Shows event date if provided
        // Shows event URL as clickable link
        // Wraps everything in IndieWeb microformats (h-entry, p-name, dt-start, u-url)
```

The renderer automatically:
- Converts RSVP status to user-friendly text with emojis
- Adds proper CSS classes for styling
- Includes IndieWeb microformats for interoperability
- Handles optional fields gracefully

## IndieWeb Integration

RSVP posts use IndieWeb microformats for machine-readable data:

- **h-entry**: Marks the RSVP as an entry
- **p-name**: Event name
- **dt-start**: Event date/time
- **u-url**: Event URL
- **p-rsvp**: RSVP status (yes/no/maybe/interested)

This allows:
- Event pages to discover RSVPs via webmentions
- Aggregation services to track attendance
- Other IndieWeb tools to parse and display your RSVP

## VS Code Snippet

Add this snippet to `.vscode/metadata.code-snippets` for quick RSVP creation:

```json
"RSVP metadata": {
    "scope": "markdown",
    "prefix": "rsvp",
    "body": [
        ":::rsvp",
        "event_name: \"$1\"",
        "event_url: \"$2\"",
        "event_date: \"$3\"",
        "rsvp_status: \"$4\"",
        "event_location: \"$5\"",
        "notes: \"$6\"",
        ":::",
        "",
        "$0"
    ],
    "description": "RSVP custom block"
}
```

Usage: Type `rsvp` and press Tab in a markdown file to insert the template.

## Integration with Site Architecture

RSVPs integrate seamlessly with the existing site:

- **Custom Block System**: Uses the same `:::block` syntax as media, review, and venue blocks
- **Markdig Pipeline**: Parsed during markdown processing via `CustomBlockParser`
- **Build Process**: Rendered to HTML during site generation
- **CSS Styling**: Can be styled using `.custom-rsvp-block`, `.rsvp-event`, `.rsvp-response`, `.rsvp-date`, `.rsvp-url` classes
- **Content Organization**: RSVPs can be added to any post type (articles, notes, etc.)

## Styling Suggestions

Example CSS for RSVP blocks:

```css
.custom-rsvp-block {
    border-left: 4px solid #4CAF50;
    padding: 1rem;
    margin: 1.5rem 0;
    background-color: #f9f9f9;
}

.rsvp-event {
    margin: 0 0 0.5rem 0;
    color: #333;
}

.rsvp-response {
    font-size: 1.1rem;
    font-weight: bold;
    margin: 0.5rem 0;
}

.rsvp-date {
    color: #666;
    font-style: italic;
}

.rsvp-url {
    margin-top: 0.5rem;
}

.rsvp-url a {
    color: #1976D2;
    text-decoration: none;
}

.rsvp-url a:hover {
    text-decoration: underline;
}
```

## Best Practices

1. **Be Timely**: Post RSVPs soon after deciding to attend/not attend
2. **Update Status**: If your plans change, update your RSVP post
3. **Include Context**: Add notes explaining your decision when helpful
4. **Link to Event**: Always include the event URL so readers can learn more
5. **Tag Appropriately**: Use relevant tags (event type, location, topic)
6. **Send Webmentions**: If the event supports webmentions, your RSVP will notify organizers

## Troubleshooting

### RSVP Block Not Rendering

- Check that you're using `:::rsvp` and `:::` delimiters correctly
- Ensure YAML syntax is valid (proper indentation, quotes around strings)
- Verify required fields (event_name, event_url, rsvp_status) are present
- Check that rsvp_status is exactly: "yes", "no", "maybe", or "interested"

### Status Not Displaying Correctly

- rsvp_status must be lowercase
- Status must be in quotes: `rsvp_status: "yes"`
- Only four values are recognized: yes, no, maybe, interested

### Event Date Not Showing

- event_date is optional - if omitted, no date will display
- Use a readable date format: "2025-11-15 14:00" or "November 15, 2025"

## Resources

- [IndieWeb RSVP Documentation](https://indieweb.org/rsvp)
- [IndieWeb Events](https://events.indieweb.org/)
- [Microformats2 h-entry](http://microformats.org/wiki/h-entry)
- [Post Types on IndieWeb](https://indieweb.org/posts#Types_of_Posts)

## Future Enhancements

Potential improvements for the RSVP system:

- GitHub Issue form template for creating RSVPs
- Dedicated `/rsvps/` page listing all RSVP posts
- RSS feed for RSVP posts
- Calendar view of upcoming events
- Automatic webmention sending to event pages
- iCalendar export for RSVPs
- Integration with calendar services
