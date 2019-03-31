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
        private static readonly string RepositoryAddressString = "RepositoryAddress";
        private static readonly string CommitIdString = "CommitId";
        private static bool IsUpdate = false;
        private static bool IsReplace = false;
        private static bool IsClear = false;
        private static bool UseWorkingDirectory = false;
        private static bool IsQuiet = false;

        private static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                string[] CommandLineArgs = Environment.GetCommandLineArgs();
                string ProgramName = CommandLineArgs.Length > 0 ? Path.GetFileNameWithoutExtension(CommandLineArgs[0]) : "program";
                Console.WriteLine($"Usage: {ProgramName} <.exe or .dll> [-u | -r | -c] [-w] [-q]");
                Console.WriteLine("No option: read the Commit Id in the file.");
                Console.WriteLine("-u: update the file Commit Id with the current Id, except if no change.");
                Console.WriteLine("-r: replace the file Commit Id with the current Id even if no change, and insert it if necessary.");
                Console.WriteLine("-c: clear the Commit Id in the file.");
                Console.WriteLine("-w: search Git in the working directory.");
                Console.WriteLine("-q: quiet.");
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
                else if (Option == "-w")
                    UseWorkingDirectory = true;
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
                return UpdateFileCommitId(FileName, IsReplace);
            else if (IsClear)
                return ClearFileCommitId(FileName);
            else
                return ReadFileCommitId(FileName);
        }

        private static int ReadFileCommitId(string fileName)
        {
            try
            {
                VersionResource versionResource = new VersionResource();
                versionResource.LoadFrom(fileName);
                StringFileInfo StringFileInfo = (StringFileInfo)versionResource["StringFileInfo"];

                try
                {
                    string RepositoryAddress = ReadResourceString(StringFileInfo, RepositoryAddressString);
                    string CommitId = ReadResourceString(StringFileInfo, CommitIdString);

                    Output($"Current Repo: {RepositoryAddress}");
                    Output($"Current Id: {CommitId}");
                    return ToReturnCode(Errors.Success);
                }
                catch
                {
                    Output("File doesn't contain Git info.");
                    return ToReturnCode(Errors.NoCommitId);
                }
            }
            catch (Exception e)
            {
                Output(e.Message);
                return ToReturnCode(Errors.ExceptionReadingFile);
            }
        }

        private static int UpdateFileCommitId(string fileName, bool forceReplace)
        {
            int Result;
           
            if ((Result = GetCommitInfo(fileName, out string RepositoryAddress, out string CommitId)) < 0)
                return Result;

            try
            {
                VersionResource VersionResource = new VersionResource();
                VersionResource.LoadFrom(fileName);
                StringFileInfo StringFileInfo = (StringFileInfo)VersionResource["StringFileInfo"];

                string FileRepositoryAddress;
                string FileCommitId;
                try
                {
                    FileRepositoryAddress = ReadResourceString(StringFileInfo, RepositoryAddressString);
                    FileCommitId = ReadResourceString(StringFileInfo, CommitIdString);
                }
                catch
                {
                    FileRepositoryAddress = null;
                    FileCommitId = null;
                }

                bool IsRepositoryAddressUpdated;
                if (forceReplace || FileRepositoryAddress == null || FileRepositoryAddress != RepositoryAddress)
                {
                    WriteResourceString(StringFileInfo, RepositoryAddressString, RepositoryAddress);
                    Output($"Repo updated to: {RepositoryAddress}");
                    IsRepositoryAddressUpdated = true;
                }
                else
                    IsRepositoryAddressUpdated = false;

                bool IsCommitIdUpdated;
                if (forceReplace || FileCommitId == null || FileCommitId != CommitId)
                {
                    WriteResourceString(StringFileInfo, CommitIdString, CommitId);
                    Output($"Id updated to: {CommitId}");

                    IsCommitIdUpdated = true;
                }
                else
                    IsCommitIdUpdated = false;

                if (IsRepositoryAddressUpdated || IsCommitIdUpdated)
                    VersionResource.SaveTo(fileName);

                return ToReturnCode(Errors.Success);
            }
            catch (Exception e)
            {
                Output(e.Message);
                return ToReturnCode(Errors.ExceptionWritingFile);
            }
        }

        private static int GetCommitInfo(string fileName, out string repositoryAddress, out string commitId)
        {
            string Folder = UseWorkingDirectory ? Environment.CurrentDirectory : Path.GetDirectoryName(fileName);

            int Result;

            if ((Result = GetRepositoryAddress(Folder, out repositoryAddress)) < 0)
            {
                commitId = null;
                return Result;
            }

            if ((Result = GetCommitId(Folder, out commitId)) < 0)
                return Result;

            return ToReturnCode(Errors.Success);
        }

        private static int GetRepositoryAddress(string folder, out string repositoryAddress)
        {
            while (folder != null)
            {
                string GitPath = Path.Combine(folder, ".git/config");
                if (File.Exists(GitPath))
                {
                    try
                    {
                        using (FileStream fs = new FileStream(GitPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            using (StreamReader sr = new StreamReader(fs, Encoding.ASCII))
                            {
                                for (; ; )
                                {
                                    string Line = sr.ReadLine();
                                    if (Line == null)
                                        break;

                                    Line = Line.Trim();
                                    if (Line.StartsWith("url = "))
                                    {
                                        repositoryAddress = Line.Substring(6);
                                        return ToReturnCode(Errors.Success);
                                    }
                                }
                            }
                        }

                        Output("Warning: using localhost repository.");
                        repositoryAddress = "localhost";
                        return ToReturnCode(Errors.Success);
                    }
                    catch (Exception e)
                    {
                        Output(e.Message);
                        repositoryAddress = null;
                        return ToReturnCode(Errors.ExceptionReadingGit);
                    }
                }
                else
                    folder = Path.GetDirectoryName(folder);
            }

            Output("File not in a subdirectory of a Git repository.");
            repositoryAddress = null;
            return ToReturnCode(Errors.NotInGitRepository);
        }

        private static int GetCommitId(string folder, out string commitId)
        {
            while (folder != null)
            {
                string GitPath = Path.Combine(folder, ".git/HEAD");
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
                            GitPath = Path.Combine(folder, $".git/{Head}");
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

                        Output("Invalid Git repository. No ref.");
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
                    folder = Path.GetDirectoryName(folder);
            }

            Output("File not in a subdirectory of a Git repository.");
            commitId = null;
            return ToReturnCode(Errors.NotInGitRepository);
        }

        private static int ClearFileCommitId(string fileName)
        {
            try
            {
                VersionResource VersionResource = new VersionResource();
                VersionResource.LoadFrom(fileName);
                StringFileInfo StringFileInfo = (StringFileInfo)VersionResource["StringFileInfo"];

                bool IsRepositoryAddressRemoved = false;
                foreach (KeyValuePair<string, StringTable> StringEntry in StringFileInfo.Strings)
                    foreach (KeyValuePair<string, StringTableEntry> Entry in StringEntry.Value.Strings)
                        if (Entry.Key == RepositoryAddressString)
                        {
                            StringEntry.Value.Strings.Remove(RepositoryAddressString);
                            Output("Repo removed.");
                            IsRepositoryAddressRemoved = true;
                            break;
                        }

                bool IsCommitIdRemoved = false;
                foreach (KeyValuePair<string, StringTable> StringEntry in StringFileInfo.Strings)
                    foreach (KeyValuePair<string, StringTableEntry> Entry in StringEntry.Value.Strings)
                        if (Entry.Key == CommitIdString)
                        {
                            StringEntry.Value.Strings.Remove(CommitIdString);
                            Output("Commit Id removed.");
                            IsCommitIdRemoved = true;
                            break;
                        }

                if (IsRepositoryAddressRemoved || IsCommitIdRemoved)
                {
                    VersionResource.SaveTo(fileName);
                    return ToReturnCode(Errors.Success);
                }

                Output("Nothing to remove.");
                return ToReturnCode(Errors.Success);
            }
            catch (Exception e)
            {
                Output(e.Message);
                return ToReturnCode(Errors.ExceptionClearingFile);
            }
        }

        private static string ReadResourceString(StringFileInfo stringFileInfo, string key)
        {
            string Result = stringFileInfo[key];

            if (Result != null && Result.Length > 0 && Result[Result.Length - 1] == '\0')
                Result = Result.Substring(0, Result.Length - 1);

            return Result;
        }

        private static void WriteResourceString(StringFileInfo stringFileInfo, string key, string value)
        {
            stringFileInfo[key] = value;
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
