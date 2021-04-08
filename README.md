# GitCommitId

Update an assembly (.exe or .dll) with the Git Commit Id it was compiled from.

[![Build status](https://ci.appveyor.com/api/projects/status/6viyxf5c3491uge8?svg=true)](https://ci.appveyor.com/project/dlebansais/gitcommitid)
[![CodeFactor](https://www.codefactor.io/repository/github/dlebansais/gitcommitid/badge)](https://www.codefactor.io/repository/github/dlebansais/gitcommitid)

## Purpose

An assembly compiled with source code from a Git repository can be tagged with the commit identifier that corresponds to the specific version used. This helps finding the original source code when only the final binary is available.

## Usage

    GitCommitId.exe <.exe or .dll> [-u | -r | -c] [-q]
    No option: read the Commit Id in the file.
    -u: update the file Commit Id with the current Id, except if no change.
    -r: replace the file Commit Id with the current Id even if no change, and insert it if necessary.
    -c: clear the Commit Id in the file.
    -w: search Git in the working directory.
    -q: quiet.

This program can be used in Visual Studio Solutions, in post-build events, or as a standalone program. If you sign the binary with for example [signtool.exe](https://docs.microsoft.com/en-us/dotnet/framework/tools/signtool-exe), make sure you update the Commit Id with `GitCommitId.exe $(TargetPath) -u` before you sign it, or this will destroy the digital signature. 
  
## How does it work?

The program adds a "RepositoryAddress" and "CommitId" string pairs in the FILEVERSION resource of the file with the corresponding information: the address of the Git repository (such as https://github.com/myname/myrepo) and the Commit Id of the head in the local copy.

Git information come from searching for Git files, starting where the target assembly is located, unless the -w option is used.

## Example

    GitCommitId myfile.exe
    File doesn't contain Git info.

    GitCommitId myfile.exe -u
    Repo updated to: https://github.com/myname/myrepo
    Id updated to: 71fb4cd62172257132259d2d00ed9c57e253c72d

    GitCommitId myfile.exe
    Current Repo: https://github.com/myname/myrepo
    Current Id: 71fb4cd62172257132259d2d00ed9c57e253c72d

    GitCommitId myfile.exe -c
    Repo removed.
    Commit Id removed.

    GitCommitId myfile.exe
    File doesn't contain Git info.
  
# Certification
This program is digitally signed with a [CAcert](https://www.cacert.org/) certificate.
