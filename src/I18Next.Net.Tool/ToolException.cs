using System;

namespace I18Next.Net.Tool;

/// <summary>
///     An error caused by the input of the user which is reported without a stack trace.
/// </summary>
internal class ToolException(string message, Exception innerException = null) : Exception(message, innerException);
