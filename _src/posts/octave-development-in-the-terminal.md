---
post_type: "article" 
title: Octave Development in the Terminal
published_date: 2017-12-09 12:46:54
tags: [octave,tmux,developer tools]
---

MatLab is a powerful tool that is used througout various disciplines for data processing and analysis. However, in order to get access to it and all its features, there is a fee associated with it. Depending on the needs of the user, this price can at times be somewhat large. Luckily, an open source alternative known as Octave exists which works just as good in many cases at zero monetary cost. Octave's syntax is for the most part identical to MatLab making the transition a relatively simple one. 

# Tools
- Octave
- Tmux
- Text Editor (Emacs)

# Installation

## Tmux

```bash
sudo apt-get install tmux
```

## Octave

```
sudo apt-add-repository ppa:octave/stable
sudo apt-get update
sudo apt-get install octave
```

# Setup

##  Create a new tmux session

```bash
tmux new -s octave-ide
```

## Configure Layout

```ascii
=========================
|           |           |
| Console   |           |
|           |           |
|-----------|  Script   |
|           |           |
| Terminal  |           |
|           |           |
=========================
```

In order to get the layout shown above, the following steps need to be taken

### Split panes horizontally
`Ctrl+b %`

### Split panes vertically

#### Switch to pane 0   
`Ctrl+b q 0`

#### Do vertical split
`Ctrl+b "`

# Workflow

Now that the layout is setup, work can finally be done. To start off, initialize Octave in pane 0.

## Test code snippets in the console

Enter the following snippet in the console window

```octave
function res = square(x)
    res = x * x
endfunction
```

Then call it to make sure it does what is expected

```octave
square(2) #Should equal 4
```

## Write code on script file

Once satisfied that the code works:

1. Switch to pane 2 using `Ctrl+b q 2`
2. Create a script file `myscript.m`
3. Edit `myscript.m` with desired code

```octave
1; # Prevents octave from processing file as function file.

function res = square(x)
    res = x * x
endfunction

square(2);
```

4. Save the script

## Run script file

Switch to pane 1 `Ctrl+b q 1` and try to run the newly created script 

```bash
octave -W myscript.m
```
The output should look the same as that in the console window.

###### Sources

[Octave](http://wiki.octave.org/Octave_for_Debian_systems)
