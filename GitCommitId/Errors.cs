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
}
