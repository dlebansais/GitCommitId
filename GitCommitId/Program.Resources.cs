namespace GitCommitId;

using System;
using System.Threading;
using Vestris.ResourceLib;

/// <summary>
/// Updates an assembly (.exe or .dll) with the Git Commit Id it was compiled from.
/// </summary>
internal partial class Program
{
    private static void LoadStringFileInfo(string fileName, out VersionResource versionResource, out StringFileInfo stringFileInfo)
    {
        versionResource = new VersionResource();
        versionResource.LoadFrom(fileName);
        stringFileInfo = (StringFileInfo)versionResource["StringFileInfo"];
    }

    private static string ReadResourceString(StringFileInfo stringFileInfo, string key)
    {
        string? Result = stringFileInfo[key];

        if (Result != null)
        {
            if (Result.Length > 0 && Result[^1] == '\0')
                Result = Result[..^1];

            return Result;
        }
        else
            return string.Empty;
    }

    private static void WriteResourceString(StringFileInfo stringFileInfo, string key, string value)
        => stringFileInfo[key] = value;

    private void SaveVersionResource(string fileName, VersionResource versionResource)
    {
        bool IsSaved = false;
        int TryCount = 1;

        while (!IsSaved)
        {
            try
            {
                versionResource.SaveTo(fileName);
                IsSaved = true;
            }
            catch
            {
                TryCount++;

                if (TryCount >= 4)
                {
                    Output($"Tried {TryCount} times to save to '{FileName}' but...");
                    throw;
                }

                Thread.Sleep(TimeSpan.FromSeconds(5));
            }
        }

        if (IsVerbose && IsSaved && TryCount > 1)
            Output($"({TryCount} tries required)");
    }
}
