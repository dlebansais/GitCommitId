namespace GitCommitId;

/// <summary>
/// Values for errors.
/// </summary>
internal enum Errors
{
    /// <summary>
    /// The success value.
    /// </summary>
    Success = 0,

    /// <summary>
    /// The missing argument error.
    /// </summary>
    MissingArgument,

    /// <summary>
    /// The invalid source file error.
    /// </summary>
    InvalidSourceFile,

    /// <summary>
    /// The unknown option error.
    /// </summary>
    UnknownOption,

    /// <summary>
    /// The no commit ID error.
    /// </summary>
    NoCommitId,

    /// <summary>
    /// The exception reading file error.
    /// </summary>
    ExceptionReadingFile,

    /// <summary>
    /// The exception writing file error.
    /// </summary>
    ExceptionWritingFile,

    /// <summary>
    /// Then invalid git repository error.
    /// </summary>
    InvalidGitRepository,

    /// <summary>
    /// The exception reading git error.
    /// </summary>
    ExceptionReadingGit,

    /// <summary>
    /// The not in git repository error.
    /// </summary>
    NotInGitRepository,

    /// <summary>
    /// The exception clearing file error.
    /// </summary>
    ExceptionClearingFile,
}
