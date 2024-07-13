# File Search
I was just bored and I hated the default window explorer search so I created this lol

# Install
Go to [Release](https://github.com/Hajdenko/File-Searcher/releases/tag/Release) and download the zip file, extract it and then **create a shortcut** for the executable or you can build it yourself.

# Shortcuts
## What are shortcuts?
*Shortcuts are variables which you can use in the search location.*

**Example of shortcuts**:
```
Instead of typing:
  C:/Users/<username>/Desktop
You can use:
  $Desktop
```

## How to edit and add shortcuts?
Open `search_json.json` and edit it in a text editor.
  The file should create after launching for the first time in the same folder.

### Default search_json.json
```
{
  "SearchTerm": "",
  "SearchLocation": "",
  "Shortcuts": {
    "$Desktop": "C:\\Users\\<username>\\Desktop"
  }
}
```
*/<username> is automatically generated.*
