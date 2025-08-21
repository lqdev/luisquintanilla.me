---
post_type: "article" 
title: Reading Local JSON Files in R
published_date: 2017-12-11 14:21:21 -05:00
tags: [R,programming]
---

The following example is a way of reading local JSON files into R scripts. This can be useful for config files, although it's still also acceptable for data files as well.

# Import package to work with JSON and lists

```r
require(rlist)
require(rjson)
```

# Load file into script

```r
configPath <- '.'
fileName <- 'config.json'
jsonFile <- fromJSON(file=paste(configPath,fileName,sep="/"),method="C")
```

The data gets loaded in as a list. It may be tedious to access nested contents via indices. To aid with this, a helper function can be created to extract the individual nested data elements of the file.

```r
#Extract values form nested list
#top_level = list.select(obj$topkey,obj$subkey)
extractData <- function(top_level) {
    output <- unlist(lapply(top_level,'[[',1));
    return(output);
}
```

# Extract data 

Below is a default sample json file that includes both nested and non-nested data.

```json
{
    "port": 2100,
    "connectionStrings": {
        "default": "123.567.890"
    }
}
```

The data in that file can be accessed as shown below.

```r
#Not Nested
port <- config$port # Should store 2100

#Nested
connectionString <- extractData(list.select(config$connectionStrings,default)) #Should store 123.567.890
```