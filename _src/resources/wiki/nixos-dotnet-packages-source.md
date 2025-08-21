---
post_type: "wiki" 
title: "Install .NET packages on NixOS from GitHub"
last_updated_date: "12/17/2023 17:56 -05:00"
tags: dotnet,nixos,linux,sysadmin,
---

## Overview

This guide shows how to build a .NET package from GitHub source. The project used to illustrate the process can be found on [GitHub](https://github.com/lqdev/fitch).


## Create derivation

The first thing you'll want to do is create a derivation for your package. 

Create a file for your package. In this case, I called mine *fitch.nix*.

Since I'm building a .NET package from the GitHub source, I use `buildDotnetModule`, `dotnetCorePackages`, and `fetchFromGithub`.

To get the rev, use the `git log` command. It's the hash of the latest commit. 

## Fake SHA

```nix
{
  fetchFromGitHub,
  buildDotnetModule,
  dotnetCorePackages
}:

buildDotnetModule {
  name = "fitch";

  src = fetchFromGitHub {
    owner = "lqdev";
    repo = "fitch";
    rev = "e5fb91ddf57eb5611e0e313af29126e590cd149f";
    sha256 = "";
  };

  projectFile = "src/fitch.fsproj";
  executables = "fitch";
  dotnet-sdk = dotnetCorePackages.sdk_8_0;
  dotnet-runtime = dotnetCorePackages.runtime_8_0;
  nugetDeps = ./deps.nix;
}
```

At this point the sha256 will be empty because you'll get it later in the process.

## Create default.nix

```nix
let
  pkgs = import <nixpkgs> { };
in
{
  fitch = pkgs.callPackage ./fitch.nix { };
}
```

## Fetch dependencies

Create a file called `deps.nix`. This will contain the dependencies you need for your application. 

```bash
touch deps.nix
```

Then, run `nix-build -A fitch.fetch-deps`. This should fail because of the empty SHA.

## Update SHA

The empty SHA will fail. However, the error message will contain the actual SHA value. Copy that and paste it into the sha256 property.

```nix
{
  lib    
  fetchFromGitHub,
  buildDotnetModule,
  dotnetCorePackages
}:

buildDotnetModule {
  name = "fitch";

  src = fetchFromGitHub {
    owner = "lqdev";
    repo = "fitch";
    rev = "e5fb91ddf57eb5611e0e313af29126e590cd149f";
    sha256 = "74xddAUGQNVliVs5o3zQQEAUAoF9r8iBmiiBr4qrUgw=";
  };

  projectFile = "src/fitch.fsproj";
  executables = "fitch";
  dotnet-sdk = dotnetCorePackages.sdk_7_0;
  dotnet-runtime = dotnetCorePackages.runtime_7_0;
  nugetDeps = ./deps.nix;
}
```

## Get dependencies

Now that you have the correct SHA, try getting the dependencies again. These dependencies are created in a series of steps:

1. Run the following command `sudo nix-build -A fitch.fetch-deps`. The result of this command is an executable script called *result*.
1. Run the *result* script. `sudo ./result deps.nix`. This will generate the lockfile called *deps.nix* and is referenced by the `nugetDeps` property.

## Build package

Once everything is set up, run `nix-build -A fitch`. This will package and install the package onto your system.

## Add to configuration

To add the newly built package to your *configuration.nix*, add it to your packages:

```nix
environment.systemPackages = with pkgs; [
    #...
    (callPackage ./fitch.nix {})
    #...
];
```

Then, run `nixos-rebuild switch`. This should now install the package onto your PC.

## (Optional) Initialize when shell starts

In my case, since I want fitch to run when the terminal starts, I edit my bash configuration in the *configuration.nix* file.

```nix
programs.bash = {
    interactiveShellInit = "fitch";
}
```

## Sources

- [Dotnet Nixpkgs](https://ryantm.github.io/nixpkgs/languages-frameworks/dotnet/)
- [Joining the NixOS Pyramid Scheme](https://wuffs.org/blog/joining-the-nixos-pyramid-scheme)
- [Packaging existing software with Nix](https://nix.dev/tutorials/learning-journey/packaging-existing-software)