namespace GitCommitId
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using Vestris.ResourceLib;

    public partial class Program
    {
        private const string RepositoryAddressString = "RepositoryAddress";
        private const string CommitIdString = "CommitId";

        private int ExecuteProgram()
        {
            if (!File.Exists(FileName))
            {
                Output($"Invalid file name.", isError: true);
                return ToReturnCode(Errors.InvalidSourceFile);
            }

            if (IsUpdate || IsReplace)
                return UpdateFileCommitId(FileName, IsReplace);
            else if (IsClear)
                return ClearFileCommitId(FileName);
            else
                return ReadFileCommitId(FileName);
        }

        private int ReadFileCommitId(string fileName)
        {
            try
            {
                LoadStringFileInfo(fileName, out VersionResource VersionResource, out StringFileInfo StringFileInfo);

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
                    Output("File doesn't contain Git info.", isError: true);
                    return ToReturnCode(Errors.NoCommitId);
                }
            }
            catch (Exception e)
            {
                Output(e.Message, isError: true);
                return ToReturnCode(Errors.ExceptionReadingFile);
            }
        }

        private int UpdateFileCommitId(string fileName, bool forceReplace)
        {
            int Result;

            if ((Result = GetCommitInfo(fileName, out string RepositoryAddress, out string CommitId)) < 0)
                return Result;

            try
            {
                LoadStringFileInfo(fileName, out VersionResource VersionResource, out StringFileInfo StringFileInfo);

                string FileRepositoryAddress;
                string FileCommitId;
                try
                {
                    FileRepositoryAddress = ReadResourceString(StringFileInfo, RepositoryAddressString);
                    FileCommitId = ReadResourceString(StringFileInfo, CommitIdString);
                }
                catch
                {
                    FileRepositoryAddress = string.Empty;
                    FileCommitId = string.Empty;
                }

                bool IsRepositoryAddressUpdated;
                if (forceReplace || FileRepositoryAddress.Length == 0 || FileRepositoryAddress != RepositoryAddress)
                {
                    WriteResourceString(StringFileInfo, RepositoryAddressString, RepositoryAddress);
                    Output($"Repo updated to: {RepositoryAddress}");
                    IsRepositoryAddressUpdated = true;
                }
                else
                    IsRepositoryAddressUpdated = false;

                bool IsCommitIdUpdated;
                if (forceReplace || FileCommitId.Length == 0 || FileCommitId != CommitId)
                {
                    WriteResourceString(StringFileInfo, CommitIdString, CommitId);
                    Output($"Id updated to: {CommitId}");

                    IsCommitIdUpdated = true;
                }
                else
                    IsCommitIdUpdated = false;

                if (IsRepositoryAddressUpdated || IsCommitIdUpdated)
                    SaveVersionResource(fileName, VersionResource);

                return ToReturnCode(Errors.Success);
            }
            catch (Exception e)
            {
                Output(e.Message, isError: true);
                return ToReturnCode(Errors.ExceptionWritingFile);
            }
        }

        private int GetCommitInfo(string fileName, out string repositoryAddress, out string commitId)
        {
            string Folder = UseWorkingDirectory ? Environment.CurrentDirectory : Path.GetDirectoryName(fileName)!;

            int Result;

            if ((Result = GetRepositoryAddress(Folder, out repositoryAddress)) < 0)
            {
                commitId = string.Empty;
                return Result;
            }

            if ((Result = GetCommitId(Folder, out commitId)) < 0)
                return Result;

            return ToReturnCode(Errors.Success);
        }

        private int GetRepositoryAddress(string folder, out string repositoryAddress)
        {
            while (folder != null)
            {
                string GitPath = Path.Combine(folder, ".git/config");
                if (File.Exists(GitPath))
                {
                    try
                    {
                        using FileStream GitPathStream = new FileStream(GitPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                        using StreamReader GitPathReader = new StreamReader(GitPathStream, Encoding.ASCII);

                        for (;;)
                        {
                            string? Line = GitPathReader.ReadLine();
                            if (Line == null)
                                break;

                            Line = Line.Trim();
                            if (Line.StartsWith("url = "))
                            {
                                repositoryAddress = Line.Substring(6);
                                return ToReturnCode(Errors.Success);
                            }
                        }

                        if (IsVerbose)
                            Output("Warning: using localhost repository.");

                        repositoryAddress = "localhost";
                        return ToReturnCode(Errors.Success);
                    }
                    catch (Exception e)
                    {
                        Output(e.Message, isError: true);
                        repositoryAddress = string.Empty;
                        return ToReturnCode(Errors.ExceptionReadingGit);
                    }
                }
                else
                    folder = Path.GetDirectoryName(folder)!;
            }

            Output("File not in a subdirectory of a Git repository.", isError: true);
            repositoryAddress = string.Empty;
            return ToReturnCode(Errors.NotInGitRepository);
        }

        private int GetCommitId(string folder, out string commitId)
        {
            while (folder != null)
            {
                string GitPath = Path.Combine(folder, ".git/HEAD");
                if (File.Exists(GitPath))
                {
                    try
                    {
                        commitId = string.Empty;
                        string Head = string.Empty;

                        using FileStream GitPathStream = new FileStream(GitPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                        using StreamReader GitPathReader = new StreamReader(GitPathStream, Encoding.ASCII);

                        string? Spec = GitPathReader.ReadLine();
                        if (Spec != null)
                        {
                            if (Spec.StartsWith("ref: "))
                                Head = Spec.Substring(5);
                            else if (Spec.Length == 40)
                                commitId = Spec;
                        }

                        if (Head.Length > 0)
                        {
                            string RefPath = Path.Combine(folder, $".git/{Head}");
                            if (File.Exists(RefPath))
                            {
                                using FileStream RefPathStream = new FileStream(RefPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                                using StreamReader RefPathReader = new StreamReader(RefPathStream, Encoding.ASCII);

                                commitId = RefPathReader.ReadLine()!;
                                return ToReturnCode(Errors.Success);
                            }
                        }

                        if (commitId.Length > 0)
                            return ToReturnCode(Errors.Success);

                        Output("Invalid Git repository. No ref or head.", isError: true);
                        return ToReturnCode(Errors.InvalidGitRepository);
                    }
                    catch (Exception e)
                    {
                        Output(e.Message, isError: true);
                        commitId = string.Empty;
                        return ToReturnCode(Errors.ExceptionReadingGit);
                    }
                }
                else
                    folder = Path.GetDirectoryName(folder)!;
            }

            Output("File not in a subdirectory of a Git repository.", isError: true);
            commitId = string.Empty;
            return ToReturnCode(Errors.NotInGitRepository);
        }

        private int ClearFileCommitId(string fileName)
        {
            try
            {
                LoadStringFileInfo(fileName, out VersionResource VersionResource, out StringFileInfo StringFileInfo);

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
                    SaveVersionResource(fileName, VersionResource);
                    return ToReturnCode(Errors.Success);
                }

                Output("Nothing to remove.");
                return ToReturnCode(Errors.Success);
            }
            catch (Exception e)
            {
                Output(e.Message, isError: true);
                return ToReturnCode(Errors.ExceptionClearingFile);
            }
        }

        private void Output(string message, bool isError = false)
        {
            if (IsQuiet)
                return;

            ConsoleDebug.Write(message, isError);
        }

        private static int ToReturnCode(Errors error)
        {
            return -(int)error;
        }
    }
}
