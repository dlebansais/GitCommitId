namespace GitCommitId
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using McMaster.Extensions.CommandLineUtils;

    /// <summary>
    /// Update an assembly (.exe or .dll) with the Git Commit Id it was compiled from.
    /// </summary>
    [Command(ExtendedHelpText = @"
Update an assembly (.exe or .dll) with the Git Commit Id it was compiled from.
")]
    public partial class Program
    {
        /// <summary>
        /// Program entry point.
        /// </summary>
        /// <param name="arguments">Command-line arguments.</param>
        /// <returns>Negative value in case of error; otherwise 0.</returns>
        public static int Main(string[] arguments) => RunAndSetResult(CommandLineApplication.Execute<Program>(arguments));

        [Argument(0)]
        [Required]
        private string FileName { get; set; } = string.Empty;

        [Option(Description = "Replace the file Commit Id with the current Id even if no change, and insert it if necessary.", ShortName = "r", LongName = "replace")]
        private bool IsReplace { get; set; }

        [Option(Description = "Update the file Commit Id with the current Id, except if no change.", ShortName = "u", LongName = "update")]
        private bool IsUpdate { get; set; }

        [Option(Description = "Clear the Commit Id in the file.", ShortName = "c", LongName = "clear")]
        private bool IsClear { get; set; }

        [Option(Description = "Search Git in the working directory.", ShortName = "w", LongName = "working-directory")]
        private bool UseWorkingDirectory { get; set; }

        [Option(Description = "Quiet.", ShortName = "q", LongName = "quiet")]
        private bool IsQuiet { get; set; }

        private static int RunAndSetResult(int ignored)
        {
            return ExecuteResult;
        }

        private void OnExecute()
        {
            try
            {
                ShowCommandLineArguments();
                ExecuteResult = ExecuteProgram();
            }
            catch (Exception e)
            {
                PrintException(e);
            }
        }

        private static int ExecuteResult = -1;

        private void ShowCommandLineArguments()
        {
            if (FileName.Length > 0)
                Output($"File name: '{FileName}'");

            if (IsReplace)
                Output($"Replacing Id");
            else if (IsUpdate)
                Output($"Updating Id");
            else if (IsClear)
                Output($"Clearing Id");
            else
                Output($"Reading Id");

            if (UseWorkingDirectory)
                Output($"Using working directory: {Environment.CurrentDirectory}");
        }

        private static void PrintException(Exception e)
        {
            Exception? CurrentException = e;

            do
            {
                ConsoleDebug.Write("***************");
                ConsoleDebug.Write(CurrentException.Message);

                string? StackTrace = CurrentException.StackTrace;
                if (StackTrace != null)
                    ConsoleDebug.Write(StackTrace);

                CurrentException = CurrentException.InnerException;
            }
            while (CurrentException != null);
        }
    }
}
