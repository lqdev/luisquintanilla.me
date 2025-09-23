# How-To Guide: Creating Travel Guides

This comprehensive guide explains how to author and publish travel guides on the website. Travel guides provide personalized recommendations with GPS coordinates, practical information, and downloadable GPX files for offline navigation.

## Overview

Travel guides are specialized collections that:
- Display locations with precise GPS coordinates
- Include personal stories and practical travel information
- Generate downloadable GPX files for offline navigation
- Provide clickable map links (geo URIs, OpenStreetMap, Google Maps)
- Feature category-based organization with visual icons
- Work seamlessly on mobile devices for actual travel use

## Travel Guide Architecture

### Technical Components

The travel guide system consists of several key components:

1. **Data Structure**: JSON files containing places, coordinates, and metadata
2. **Collection Configuration**: F# code defining the travel collection
3. **Views**: Specialized F# ViewEngine templates for travel-specific UI
4. **GPX Generation**: Automatic waypoint file creation for offline apps
5. **Build Integration**: Automatic processing during site generation

### Files Involved

- `Data/[collection-name].json` - Travel data with places and routes
- `Collections.fs` - Collection configuration and metadata  
- `Views/TravelViews.fs` - Travel-specific HTML generation
- `Domain.fs` - Data types and GPX generation logic

## Step-by-Step Guide

### Step 1: Create Travel Data File

Create a JSON file in the `Data/` directory with your travel recommendations:

```json
{
  "title": "Your Travel Guide Title",
  "description": "Brief description of your travel guide",
  "places": [
    {
      "id": "unique-place-id",
      "name": "Place Name",
      "lat": 41.8902,
      "lon": 12.4922,
      "category": "attraction",
      "description": "Detailed description of the place",
      "personalNote": "Your personal experience and tips",
      "practicalInfo": {
        "price": "€16",
        "hours": "8:30am-7pm",
        "phone": "+39 06 3996 7700"
      }
    }
  ],
  "routes": [
    {
      "name": "Route Name",
      "description": "Route description",
      "sequence": ["place-id-1", "place-id-2"]
    }
  ]
}
```

#### Data Structure Details

**Required Fields**:
- `title`: Main title for your travel guide
- `description`: Brief overview of the collection
- `places`: Array of location objects

**Place Object Fields**:
- `id`: Unique identifier (use kebab-case: "trattoria-monti")
- `name`: Display name of the location
- `lat`: Latitude (decimal degrees, 4+ decimal places for precision)
- `lon`: Longitude (decimal degrees, 4+ decimal places for precision)  
- `category`: Type of place (see Category Types below)
- `description`: Main description of the location
- `personalNote`: Your personal experience, tips, or stories (optional)
- `practicalInfo`: Object with practical details (optional)

**Practical Info Fields** (all optional):
- `price`: Cost information (€, €€, €€€, or specific prices)
- `hours`: Operating hours or schedule
- `phone`: Contact phone number
- `website`: Official website URL
- `reservations`: Reservation requirements

**Category Types**:
- `attraction` - Tourist attractions, landmarks (🏛️ icon)
- `restaurant` - Restaurants, cafes, bars (🍽️ icon)
- `hidden` - Hidden gems, local spots (💎 icon)
- `shopping` - Shopping areas, markets (🛍️ icon)
- `practical` - Practical services, transportation (🏪 icon)

**Routes** (optional):
- `name`: Route name for themed walking tours
- `description`: What the route covers
- `sequence`: Array of place IDs in visit order

### Step 2: Add Collection Configuration

Add your travel collection to the `Collections.fs` file in the `defaultCollections` array:

```fsharp
{
    Id = "your-collection-id"
    Title = "Your Travel Guide Title"
    Description = "Brief description matching your JSON file"
    CollectionType = Travel "city-guide"
    UrlPath = "/collections/travel/your-collection-id/"
    DataFile = "your-collection-id.json"
    Tags = [| "travel"; "location"; "category"; "recommendations" |]
    LastUpdated = DateTime.Now.ToString("yyyy-MM-dd")
    ItemCount = None
}
```

**Configuration Fields**:
- `Id`: Must match your JSON filename (without .json)
- `Title`: Displayed title on the website
- `Description`: Brief overview for navigation
- `CollectionType`: Must be `Travel "category"` for travel collections (e.g., `Travel "city-guide"`, `Travel "national-parks"`)
- `UrlPath`: URL structure following `/collections/travel/[id]/`
- `DataFile`: Your JSON filename
- `Tags`: Include "travel" tag + location/theme tags
- `LastUpdated`: Automatic timestamp
- `ItemCount`: Leave as `None` (calculated automatically)

### Step 3: Update Travel Guides Index

Add your new guide to the main travel guides page in `_src/travel-guides.md`:

```markdown
## Guides

- [Rome Favorites](/collections/travel/rome-favorites/)
- [Your New Guide](/collections/travel/your-collection-id/)
```

### Step 4: Build and Test

1. **Build the site**:
   ```bash
   dotnet build
   dotnet run
   ```

2. **Verify generation**:
   - Check that your travel guide page generates properly
   - Confirm GPX file is created in the output
   - Test GPS coordinate links and map integration
   - Validate mobile responsiveness

3. **Test outputs**:
   - HTML page at `/collections/travel/your-collection-id/`
   - GPX file at `/collections/travel/your-collection-id/your-collection-id.gpx`
   - Travel guides index includes your new guide

## Content Guidelines

### Writing Effective Descriptions

**Main Descriptions**:
- Be specific and informative
- Include historical context or significance
- Mention what makes the place special
- Keep to 1-2 sentences for readability

**Personal Notes**:
- Share your actual experience
- Include practical tips and insider knowledge
- Mention best times to visit
- Note any potential challenges or considerations
- Use conversational tone

### GPS Coordinates

**Accuracy Guidelines**:
- Use at least 4 decimal places for precision
- Test coordinates in Google Maps before adding
- For large venues, use main entrance coordinates
- For viewpoints, use optimal viewing position

**Finding Coordinates**:
1. Open Google Maps and find your location
2. Right-click on the exact spot
3. Copy the coordinates from the popup
4. Use decimal degree format (not degrees/minutes/seconds)

### Practical Information

**Price Guidelines**:
- Use local currency symbols (€, $, £)
- For restaurants: €/€€/€€€ system or specific prices
- For attractions: Include ticket prices
- Note if prices are approximate or subject to change

**Hours Format**:
- Use clear, readable format: "8:30am-7pm"
- Include days: "Monday-Friday" or "Daily"
- Note seasonal variations: "Summer: 9am-8pm"
- Mention closures: "Closed Sundays"

**Phone Numbers**:
- Include country code: "+39 06 3996 7700"
- Use international format for international audiences
- Verify numbers are current before publishing

## Technical Features

### Generated Features

The system automatically generates:

1. **GPX File**: Waypoints for offline navigation apps
2. **Map Links**: geo: URIs, OpenStreetMap, Google Maps
3. **Category Icons**: Visual indicators for place types
4. **Responsive Design**: Mobile-optimized layout
5. **Download Section**: GPX download with app recommendations

### Mobile Integration

**Supported Apps**:
- OsmAnd (Android/iOS)
- Maps.me (Android/iOS)
- Any GPS app supporting GPX import

**Offline Features**:
- All coordinates work without internet
- GPX files include place descriptions
- Practical info embedded in waypoints

### URL Structure

Travel guides follow a consistent URL pattern:
- Main page: `/collections/travel/[guide-id]/`
- GPX file: `/collections/travel/[guide-id]/[guide-id].gpx`
- Travel index: `/collections/travel-guides/`

## Advanced Features

### Routes and Itineraries

Create themed walking routes by grouping places:

```json
"routes": [
  {
    "name": "Ancient Rome Day",
    "description": "Walking tour of ancient sites",
    "sequence": ["colosseum", "roman-forum", "palatine-hill"]
  },
  {
    "name": "Food Crawl",
    "description": "Best local eating spots",
    "sequence": ["restaurant-1", "cafe-2", "market-3"]
  }
]
```

Routes help visitors plan themed days and logical walking sequences.

### Category Organization

Use categories strategically:
- **Attractions**: Major sights and landmarks
- **Restaurants**: All dining experiences
- **Hidden**: Local gems and off-beaten-path spots
- **Shopping**: Markets, boutiques, shopping areas
- **Practical**: Transportation, services, necessities

### Extended Practical Information

Include comprehensive practical details:

```json
"practicalInfo": {
  "price": "€16",
  "hours": "8:30am-7pm daily",
  "phone": "+39 06 3996 7700",
  "website": "https://example.com",
  "reservations": "Recommended for dinner",
  "accessibility": "Wheelchair accessible",
  "languages": "English, Italian",
  "payment": "Cards accepted"
}
```

## Troubleshooting

### Common Issues

**Build Errors**:
- Verify JSON syntax with online validator
- Check that collection ID matches filename
- Ensure all required fields are present
- Confirm GPS coordinates are valid numbers

**Missing Features**:
- Travel collections must include "travel" tag
- Collection type must be `Travel "category"`
- GPX only generates for properly tagged collections

**Display Issues**:
- Check category spelling (affects icons)
- Verify coordinates are in decimal degree format
- Ensure practical info uses proper field names

### Validation Checklist

Before publishing:
- [ ] JSON file validates without syntax errors
- [ ] GPS coordinates tested in Google Maps
- [ ] All phone numbers include country codes
- [ ] Hours and pricing information is current
- [ ] Personal notes add value beyond basic info
- [ ] Collection properly configured in Collections.fs
- [ ] Build completes without errors
- [ ] GPX file generates correctly
- [ ] Mobile layout displays properly

## Best Practices

### Content Quality

1. **Authenticity**: Only include places you've personally visited
2. **Recency**: Keep information current and update regularly
3. **Specificity**: Provide precise, actionable details
4. **Balance**: Mix popular attractions with hidden gems
5. **Context**: Include cultural and historical background

### Technical Excellence

1. **Precision**: Use accurate GPS coordinates
2. **Completeness**: Include all relevant practical information
3. **Accessibility**: Ensure mobile compatibility
4. **Performance**: Keep place lists manageable (10-20 places)
5. **Standards**: Follow consistent naming and formatting

### User Experience

1. **Logical Organization**: Group related places by area or theme
2. **Clear Navigation**: Use descriptive place names
3. **Practical Value**: Focus on information travelers actually need
4. **Offline Capability**: Ensure GPX files work without internet
5. **Cultural Sensitivity**: Respect local customs and guidelines

## Examples and Templates

### Sample Place Entry

```json
{
  "id": "sant-eustachio-caffe",
  "name": "Sant'Eustachio Il Caffè",
  "lat": 41.8986,
  "lon": 12.4768,
  "category": "restaurant",
  "description": "Historic coffee roaster since 1938. Serves what many consider the best espresso in Rome.",
  "personalNote": "Stand at the bar like locals do. Don't be intimidated by the crowds - the coffee is worth the wait.",
  "practicalInfo": {
    "price": "€",
    "hours": "8:30am-1am daily",
    "reservations": "No reservations needed"
  }
}
```

### Sample Collection Configuration

```fsharp
{
    Id = "paris-favorites"
    Title = "Paris Favorites"
    Description = "Personal recommendations for Paris from multiple visits"
    CollectionType = Travel "city-guide"
    UrlPath = "/collections/travel/paris-favorites/"
    DataFile = "paris-favorites.json"
    Tags = [| "travel"; "paris"; "france"; "recommendations" |]
    LastUpdated = DateTime.Now.ToString("yyyy-MM-dd")
    ItemCount = None
}
```

This guide provides everything needed to create high-quality travel guides that help real travelers while maintaining technical excellence and consistent user experience.