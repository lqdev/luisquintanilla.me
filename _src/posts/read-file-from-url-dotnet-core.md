---
title: Read A Text File From a URL in .NET Core
published_date: 2017-12-18 21:09:15
tags: .net,.net core,microsoft,programming
---

# Create New .NET Core Project

```bash
dotnet new console -o urlreader
```

# Navigate to Project Folder
```bash
cd urlreader
```

# Import Dependencies

```csharp
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
```

# Create Reader

Create a function in the `Program.cs` file that will return a `StreamReader` from the specified URL. Because in this case it will be used inside the `Main` method which is `static`, the new function will also have to be `static`. 

```csharp
static StreamReader URLStream(String fileurl){
    return new StreamReader(new HttpClient().GetStreamAsync(fileurl).Result);
}
```

Inside the function, we return a `StreamReader` which is instantiated by creating an `HTTPClient` and asynchronously executing a `GET` request which returns a `Task<StreamReader>`. In order to resolve the object, the `Result` property needs to be requested.


# Reading File Contents

Replace the contents of the `Main` method with the following

```csharp
static void Main(string[] args) {
    string line;
    StreamReader s = URLStream(@"https://algs4.cs.princeton.edu/15uf/tinyUF.txt");
    String myline = s.ReadLine(); //First Line
    while((line = s.ReadLine()) != null) //Subsequent Lines
    {
        Console.WriteLine(line);
    }
}
```

In this case, the format of the first line is different from the others. However, reading the contents of that line is no different than the other lines. 

# Run The Program

```bash
dotnet run

#Expected Output
#4 3
#3 8
#6 5
#9 4
#2 1
#8 9
#5 0
#7 2
#6 1
#1 0
#6 7
```


