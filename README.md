# GitCommitId

Update an assembly (.exe or .dll) with the Git Commit Id it was compiled from.

## Purpose

An assembly compiled from source code in a Git repository can be tagged with the commit identifier that correspond to the specific version used. This helps finding the original source code when only the final binary is available.

## Usage

    GitCommitId.exe <.exe or .dll> [-u | -c] [-q]
    No option: read the Commit Id in the file.
    -u: update the file Commit Id with the current Id.
    -c: clear the Commit Id in the file.
    -q: quiet.

This program can be used in Visual Studio Solutions, in post-build events, or as a standalone program. If you sign the binary with for example [signtool.exe](https://docs.microsoft.com/en-us/dotnet/framework/tools/signtool-exe), make sure you update the Commit Id with `GitCommitId.exe $(TargetPath) -u` before you sign it, or this will destroy the digital signature. 
  
