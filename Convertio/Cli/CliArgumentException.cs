namespace Convertio.Cli;

/// <summary>
/// Thrown for any user-facing argument problem. Program.cs catches this specifically
/// and prints the message plus usage, without a stack trace — the user mistyped a
/// flag, they didn't break the program.
/// </summary>
public sealed class CliArgumentException(string message) : Exception(message);
