using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Vestris.ResourceLib;

namespace GitCommitId
{
    public enum Errors
    {
        Success = 0,
        MissingArgument,
        InvalidSourceFile,
        UnknownOption,
        NoCommitId,
        ExceptionReadingFile,
        ExceptionWritingFile,
        InvalidGitRepository,
        ExceptionReadingGit,
        NotInGitRepository,
        ExceptionClearingFile,
    }

    public class Program
    {
        private static readonly string CommitIdString = "CommitId";
        private static bool IsUpdate = false;
        private static bool IsReplace = false;
        private static bool IsClear = false;
        private static bool IsQuiet = false;

        private static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                Output($"Usage: {Path.GetFileNameWithoutExtension(Environment.CommandLine)} <.exe or .dll> [-u | -r | -c] [-q]");
                Output("No option: read the Commit Id in the file.");
                Output("-u: update the file Commit Id with the current Id, but does nothing if no change.");
                Output("-r: replace the file Commit Id with the current Id even if no change, and insert it if necessary.");
                Output("-c: clear the Commit Id in the file.");
                Output("-q: quiet.");
                return ToReturnCode(Errors.MissingArgument);
            }

            for (int i = 1; i < args.Length; i++)
            {
                string Option = args[i];

                if (Option == "-u")
                    IsUpdate = true;
                else if (Option == "-r")
                    IsReplace = true;
                else if (Option == "-c")
                    IsClear = true;
                else if (Option == "-q")
                    IsQuiet = true;
                else
                {
                    Output($"Unknown option {args[1]}.");
                    return ToReturnCode(Errors.UnknownOption);
                }
            }

            string FileName = args[0];

            if (!File.Exists(FileName))
            {
                Output($"Invalid file {FileName}.");
                return ToReturnCode(Errors.InvalidSourceFile);
            }

            if (IsUpdate || IsReplace)
                return UpdateCommitId(FileName, IsReplace);
            else if (IsClear)
                return ClearCommitId(FileName);
            else
                return ReadCommitId(FileName);
        }

        private static int ReadCommitId(string fileName)
        {
            try
            {
                VersionResource versionResource = new VersionResource();
                versionResource.LoadFrom(fileName);
                StringFileInfo StringFileInfo = (StringFileInfo)versionResource["StringFileInfo"];

                try
                {
                    string CommitId = StringFileInfo[CommitIdString];

                    Output($"Current Id: {CommitId}");
                    return ToReturnCode(Errors.Success);
                }
                catch
                {
                    Output("File doesn't contain a Commit Id.");
                    return ToReturnCode(Errors.NoCommitId);
                }
            }
            catch (Exception e)
            {
                Output(e.Message);
                return ToReturnCode(Errors.ExceptionReadingFile);
            }
        }

        private static int UpdateCommitId(string fileName, bool forceReplace)
        {
            int Result;

            if ((Result = GetCommitId(fileName, out string CommitId)) < 0)
                return Result;

            try
            {
                VersionResource VersionResource = new VersionResource();
                VersionResource.LoadFrom(fileName);
                StringFileInfo StringFileInfo = (StringFileInfo)VersionResource["StringFileInfo"];

                string FileCommitId;
                try
                {
                    FileCommitId = StringFileInfo[CommitIdString];
                }
                catch
                {
                    FileCommitId = null;
                }

                if (forceReplace || FileCommitId == null || FileCommitId != CommitId)
                {
                    StringFileInfo[CommitIdString] = CommitId;
                    VersionResource.SaveTo(fileName);

                    Output($"Id updated to: {CommitId}");
                }

                return ToReturnCode(Errors.Success);
            }
            catch (Exception e)
            {
                Output(e.Message);
                return ToReturnCode(Errors.ExceptionWritingFile);
            }
        }

        private static int GetCommitId(string fileName, out string commitId)
        {
            string Folder = Path.GetDirectoryName(fileName);
            while (Folder != null)
            {
                string GitPath = Path.Combine(Folder, ".git/HEAD");
                if (File.Exists(GitPath))
                {
                    try
                    {
                        string Head = null;
                        using (FileStream fs = new FileStream(GitPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            using (StreamReader sr = new StreamReader(fs, Encoding.ASCII))
                            {
                                string Spec = sr.ReadLine();
                                if (Spec.StartsWith("ref: "))
                                    Head = Spec.Substring(5);
                            }
                        }

                        if (Head != null)
                        {
                            GitPath = Path.Combine(Folder, $".git/{Head}");
                            if (File.Exists(GitPath))
                            {
                                using (FileStream fs = new FileStream(GitPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                                {
                                    using (StreamReader sr = new StreamReader(fs, Encoding.ASCII))
                                    {
                                        commitId = sr.ReadLine();
                                        return ToReturnCode(Errors.Success);
                                    }
                                }
                            }
                        }

                        Output("Invalid Git repository.");
                        commitId = null;
                        return ToReturnCode(Errors.InvalidGitRepository);
                    }
                    catch (Exception e)
                    {
                        Output(e.Message);
                        commitId = null;
                        return ToReturnCode(Errors.ExceptionReadingGit);
                    }
                }
                else
                    Folder = Path.GetDirectoryName(Folder);
            }

            Output("File not in a subdirectory of a Git repository.");
            commitId = null;
            return ToReturnCode(Errors.NotInGitRepository);
        }

        private static int ClearCommitId(string fileName)
        {
            try
            {
                VersionResource VersionResource = new VersionResource();
                VersionResource.LoadFrom(fileName);
                StringFileInfo StringFileInfo = (StringFileInfo)VersionResource["StringFileInfo"];

                foreach (KeyValuePair<string, StringTable> StringEntry in StringFileInfo.Strings)
                    foreach (KeyValuePair<string, StringTableEntry> Entry in StringEntry.Value.Strings)
                        if (Entry.Key == CommitIdString)
                        {
                            StringEntry.Value.Strings.Remove(CommitIdString);
                            VersionResource.SaveTo(fileName);
                            Output("Commit Id removed.");

                            return ToReturnCode(Errors.Success);
                        }

                Output("No Commit Id to remove.");
                return ToReturnCode(Errors.Success);
            }
            catch (Exception e)
            {
                Output(e.Message);
                return ToReturnCode(Errors.ExceptionClearingFile);
            }
        }

        private static void Output(string message)
        {
#if DEBUG
            Debug.WriteLine(message);
            Debug.WriteLine("");
#endif

            if (IsQuiet)
                return;

            Console.WriteLine(message);
        }

        private static int ToReturnCode(Errors error)
        {
            return -(int)error;
        }
    }
}
