---
post_type: "article" 
title: Transcribing Podcasts with Microsoft Speech API
tags: [azure, csharp, apis, nlp, machine-learning, artificial-intelligence, microsoft]
published_date: 2018-02-11 20:34:37 -05:00
---


# Introduction

I enjoy listening to podcasts in a wide range of topics that include but are not limited to politics, software development, history, comedy and true crime. While some of the more entertainment related podcasts are best enjoyed through audio, those that are related to software development or other type of hands-on topics would benefit greatly from having a transcript. Having a transcript allows me to go back after having listened to an interesting discussion and look directly at the content I am interested in without having to listen to the podcast again. This however is not always feasible given that it costs both time and money to produce a transcript. Fortunately, there are tools out there such as Google Cloud Speech and Microsoft Speech API which allow users to convert speech to text. For this writeup, I will be focusing on the Microsoft Speech API. Because podcasts tend to be long-form, I will be using the C# client library because it allows for long audio (greater than 15 seconds) to be transcribed. The purpose of this exercise is to create a console application that takes audio segments, converts them to text and stores the results in a text file with the goal of evaluating how well the Microsoft Speech API works. The source code for the console application can be found on [GitHub](https://github.com/lqdev/PodcastsBingSpeechAPIDemo)

## Prerequisites

- [Visual Studio Community](https://www.visualstudio.com/vs/community/)
- [Windows Subsystem for Linux (Ubuntu)](https://www.microsoft.com/store/productId/9NBLGGH4MSV6)
- [ffmpeg](https://ffmpeg.org/)

## Install/Enable Windows Subsystem for Linux
1. Open Powershell as Administrator and input
```powershell
Enable-WindowsOptionalFeature -Online -FeatureName Microsoft-Windows-Subsystem-Linux
```
2. Restart your computer
3. Open the Microsoft Store and install [Ubuntu](https://www.microsoft.com/store/productId/9NBLGGH4MSV6) Linux distribution
4. Once installed, click `Launch`
5. Create your LINUX user account (Keep in mind that this is not the same as your Windows account therefore it can be different).

## Install ffmpeg

Once Ubuntu is installed on your computer and you have created your LINUX user account, it's time to install `ffmpeg`. This will allow us to convert our file from `mp3` which is usually the format podcasts are in to `wav` which is the format accepted by the Microsoft Speech API

```bash
sudo apt install -y ffmpeg
```

## Get Bing Speech API Key

In order to use the Microsoft Speech API, an API key is required. This can be obtained using the following steps.

1. Navigate to the [Azure Cognitive Services](https://azure.microsoft.com/en-us/try/cognitive-services/) page.
2. Select the `Speech` tab
3. Click `Get API Key` and follow the instructions
4. Once you have an API key, make sure to store it somewhere like a text file in your computer for future use.

# File Prep

Before transcribing the files, they need to be converted to `wav` format if they are not already in it. In order to ease processing, they should be split into segments as opposed to having the API process an entire multi megabyte file. To do all this, inside the Ubuntu shell, first navigate to your `Documents` folder and create a new directory for the project.

```bash
cd /mnt/c/Users/<USERNAME>/Documents
mkdir testspeechapi
```

`USERNAME` is your Windows user name. This can be found by typing the following command into the Windows `CMD` prompt

```bash
echo %USERNAME%
```

## Download Audio Files

One of the podcasts I listen to is [Talk Python To Me](https://talkpython.fm/) by [Michael Kennedy](https://twitter.com/mkennedy). For the purpose of this exercise, aside from having great content, all of the episodes are transcribed and a link is provided to the `mp3` file. 

The file I will be using is from episode [#149](https://talkpython.fm/episodes/show/149/4-python-web-frameworks-compared), but it can easily be any of the episodes.

In the Ubuntu shell, download the file into the `testspeechapi` directory that was recently created using `wget`.

```bash
wget https://talkpython.fm/episodes/download/149/4-python-web-frameworks-compared.mp3 -O originalinput.mp3
```

## Download Transcripts

The transcripts can be found in GitHub at this [link](https://github.com/mikeckennedy/talk-python-transcripts/tree/master/transcripts). The original transcript will allow me to compare it to the output of the Microsoft Speech API and evaluate the accuracy.

We can download this file into the `testspeechapi` directory just like the audio file

```bash
wget https://raw.githubusercontent.com/mikeckennedy/talk-python-transcripts/master/transcripts/149.txt -O originaltranscript.txt
```

## Convert MP3 to WAV

Now that we have both the original audio file and transcript, it's time to convert the format from `mp3` to `wav`. To do this, we can use `ffmpeg`. In the Ubuntu shell, enter the following command.

```bash
ffmpeg -i originalinput.mp3 -f wav originalinput.wav
```

## Create Audio Segments

Once we have the proper format, it's time to make the files more manageable for processing. This can be done by splitting them up into equal segments using `ffmpeg`. In this case I'll be splitting it up into sixty second segments and storing them into a directory called `input` with the name `filexxxx.wav`. The `%04d` indicates that there will be 4 digits in the file name. Inside the Ubuntu shell and `testspeechapi` directory enter the following commands.

```bash
mkdir input
ffmpeg -i originalinput.wav -f segment -segment_time 60 -c copy input/file%04d.wav
```

# Building The Console Application

To get started building the console application, we can leverage the [Cognitive-Speech-STT-ServiceLibrary](https://github.com/Azure-Samples/Cognitive-Speech-STT-ServiceLibrary) sample program.

## Download The Sample Program

The first step will be to clone the sample program from GitHub onto your computer. In the Windows `CMD` prompt navigate to the `testspeechapi` folder and enter the following command.

```bash
git clone https://github.com/Azure-Samples/Cognitive-Speech-STT-ServiceLibrary
```

Once the project is on your computer, open the directory and launch the `SpeechClient.sln` solution in the `sample` directory

When the solution launches, open the `Program.cs` file and begin making modifications.

## Reading Files

To read files, we can create a function that will return the list of files in the specified directory.

```csharp
private static string[] GetFiles(string directory)
{
    string[] files = Directory.GetFiles(directory);
    return files;
}
```

## Process Files

Once we have the list of files, we can then process each file individually using the `Run` method. To do so, we need to make a slight modification to our `Main` method so that it iterates over each file and calls the `Run` method on it. To store the responses from the API, we'll also need a `StringBuilder` object which is declared at the top of our `Program.cs` file.

```csharp
finalResponse = new StringBuilder();
string[] files = GetFiles(args[0]);
foreach(var file in files)
{
    p.Run(file,"en-us",LongDictationUrl,args[1]).Wait();
    Console.WriteLine("File {0} processed",file);
}
```

## Transcribe Audio

The `Run` method can be left intact. However, the `Run` method uses the `OnRecognitionResult` method to handle the result of API responses. In the `OnRecognitionResult` method, we can remove almost everything that is originally there and replace it. The response from the API returns various results of potential phrases as well as a confidence value. Generally, most of the phrases are alike and the first value is good enough for our purposes. The code for this part will take the response from the API, append it to a `StringBuilder` object and return when completed.

```csharp
public Task OnRecognitionResult(RecognitionResult args)
{

    var response = args;

    if(response.Phrases != null)
    {
	finalResponse.Append(response.Phrases[0].DisplayText);
	finalResponse.Append("\n");
    }

    return CompletedTask;
}
```

## Output Transcribed Speech

When all the audio files have been processed, we can save the final output to a text file in the `testspeechapi` directory. This can be done with the `SaveOutput` function to which we pass in a file name and the `StringBuilder` object that captured responses from the API.

```csharp
private static void SaveOutput(string filename,StringBuilder content)
{
    StreamWriter writer = new StreamWriter(filename);
    writer.Write(content.ToString());
    writer.Close();
}
```

`SaveOutput` can then be called from our `Main` method like so.

```csharp
string username = Environment.GetEnvironmentVariable("USERNAME", EnvironmentVariableTarget.Process);
SaveOutput(String.Format(@"C:\\Users\\{0}\\Documents\\testspeechapi\\apitranscript.txt",username), finalResponse);
```

The final `Main` method should look similar to the code below

```csharp
public static void Main(string[] args)
{
    // Send a speech recognition request for the audio.
    finalResponse = new StringBuilder();

    string[] files = GetFiles(args[0]);

    var p = new Program();

    foreach (var file in files)
    {
	p.Run(file, "en-us", LongDictationUrl, args[1]).Wait();
	Console.WriteLine("File {0} processed", file);
    }

    string username = Environment.GetEnvironmentVariable("USERNAME", EnvironmentVariableTarget.Process);

    SaveOutput(String.Format(@"C:\\Users\\{0}\\Documents\\testspeechapi\\apitranscript.txt",username), finalResponse);
}
```

# Output

The program will save the output of the `StringBuilder` object `finalResponse` to the file `apitranscript.txt`.

Prior to running the program it needs to be built. In Visual Studio change the Solutions Configurations option from `Debug` to `Release` and build the application.

To run the program, navigate to the `C:\Users\%USERNAME%\Documents\testspeechapi\Cognitive-Speech-STT-ServiceLibrary\sample\SpeechClientSample\bin\Release` directory in the Windows `CMD` prompt and enter the following command and pass in the `input` directory where all the audio segments are stored and your API Key.

```bash
SpeechClientSample.exe C:\\Users\\%USERNAME%\\Documents\\testspeechapi\\input <YOUR-API-KEY>
```
This process may take a while due to the number of files being processed, so be patient.

## Sample Output

Original

> Are you considering getting into web programming? Choosing a web framework like Pyramid, Flask, or Django can be daunting. It would be great to see them all build out the same application and compare the results side-by-side. That's why when I heard what Nicholas Hunt-Walker was up to, I had to have him on the podcast. He and I chat about four web frameworks compared. He built a data-driven web app with Flask, Tornado, Pyramid, and Django and then put it all together in a presentation. We're going to dive into that right now. This is Talk Python To Me, Episode 149, recorded January 30th, 2018. Welcome to Talk Python to Me, a weekly podcast on Python, the language, the libraries, the ecosystem, and the personalities. This is your host, Michael Kennedy, follow me on Twitter where I'm @mkennedy. Keep up with the show and listen to past episodes at talkpython.fm, and follow the show on Twitter via @talkpython. Nick, welcome to Talk Python.

API Response

> Are you considering getting into web programming cheese in the web framework like plaster. Django can be daunting. It would be great to see them. Although that the same application and compare the results side by side. That's Why? When I heard. But Nicholas Hunt. Walker was up to, I had him on the podcast. He night chat about 4 web frameworks, compared he built a data driven web app with last tornado. Peermade Angangueo and then put it all together in a presentation. We have dive into that right now. This is talk by the to me, I was 149 recorded January 30th 2018. Welcome to talk python to me a weekly podcast on Python Language the library ecosystem in the personalities. Did you host Michael Kennedy? Follow me on Twitter where I'm at in Kennedy keep up with the show in the past episodes at talk python. Damn info, the show on Twitter via at Popeyes on. Nick Welcome to talk by phone.

# Conclusion

In this writeup, I converted an `mp3` podcast file to `wav` format and split it into sixty second segments using `ffmpeg`. The segments were then processed by a console application which uses the Microsoft Speech API to convert speech to text and the results were saved to a text file. When compared to the original transcript, the result produced by the API is inconsistent and at times incomprehensible. This does not mean that overall the API is unusable or inaccurate as many things could have contributed to the inaccuracy. However, the results were not as good as I would have hoped for.

###### Sources
- [Windows Subsystem for Linux Install Instructions](https://docs.microsoft.com/en-us/windows/wsl/install-win10)
- [Microsoft Speech API](https://docs.microsoft.com/en-us/azure/cognitive-services/speech/home)
