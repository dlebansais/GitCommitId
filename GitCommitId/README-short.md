# GitCommitId

Update an assembly (.exe or .dll) with the Git Commit Id it was compiled from.

# Usage

    GitCommitId.exe <.exe or .dll> [-u | -r | -c] [-q]
    No option: read the Commit Id in the file.
    -u: update the file Commit Id with the current Id, except if no change.
    -r: replace the file Commit Id with the current Id even if no change, and insert it if necessary.
    -c: clear the Commit Id in the file.
    -w: search Git in the working directory.
    -q: quiet.
