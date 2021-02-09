---
title: Setting Up A New Ubuntu PC
date: 2017-12-09 12:49:19
tags: linux, ubuntu, java, sysadmin, emacs
---

# Users

This assumes that you are currently the root user.

Enter the adduser to command to create a new user.

```bash
adduser <username>
```

Immediately, you will be prompted for a password. Enter the password and continue the process. 

## Adding Root Privileges

This is only applicable when granting root priviliges to the newly created user. 

Enter the usermod command to add the new user to the sudo (root) group

```bash
usermod -aG sudo <username>
```

The user should now have root priviliges. 

To test that it worked log in as that user and try to run a command with sudo

```bash
su - <username>
sudo ls -la /root
```

# Emacs

By default, Ubuntu usually comes preloaded or has the default 24.x version of Emacs. I often like to use Emacs along with org-mode and to take full advantage, especially as it pertains to working with source code I have found version 25.x is much better suited for the task. While there are many ways to go about it, installing it via package manager appears to be the easiest. 

## Add repository to PPA

```bash
sudo add-apt-repository ppa:kelleyk/emacs
```

## Update the packages

```bash
sudo apt update
```

## Install Emacs. 
### Text-Only Interface

```bash
sudo apt-get install emacs25-nox
```

### GUI Interface

```bash
sudo apt-get install emacs25
```

## Backup files

Sometimes Emacs keeps a backup file by default. This can cause a lot of clutter in the current directory where work is being performed. There is a way to disable this.

Navigate to `~/.emacs.d/` and edit the `init.el` file and add the following line.

```lisp
(setq make-backup-files nil)
```

# Java

Make sure that your packages and repositories are up to date

```bash
sudo apt-get update
```

## OpenJDK

```bash
sudo apt-get install default-jdk
```

## Oracle JDK
### Add PPA Repository
```bash
sudo add-apt-repository ppa:webupd8team/java
sudo apt-get update
```

### Install the installer

```bash
sudo apt-get install oracle-java8-installer
```


#### Sources
[Users](https://www.digitalocean.com/community/tutorials/how-to-create-a-sudo-user-on-ubuntu-quickstart)  
[Emacs](http://ubuntuhandbook.org/index.php/2017/04/install-emacs-25-ppa-ubuntu-16-04-14-04/)  
[Java](https://www.digitalocean.com/community/tutorials/how-to-install-java-with-apt-get-on-ubuntu-16-04)
