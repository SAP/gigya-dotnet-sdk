/*
 * Copyright (C) 2024 SAP SE
 * Modern .NET 9 SDK - Logger
 */

using System.Text;

namespace Gigya.Socialize.SDK;

/// <summary>
/// Simple logging utility for SDK operations.
/// Captures diagnostic information during request/response processing.
/// </summary>
public class GSLogger
{
    private readonly StringBuilder _sb = new();

    /// <summary>
    /// Writes data to the log.
    /// </summary>
    /// <param name="data">The data to write.</param>
    public void Write(object? data)
    {
        if (data == null) return;
        Write(null, data.ToString());
    }

    /// <summary>
    /// Writes an exception to the log.
    /// </summary>
    /// <param name="ex">The exception to write.</param>
    public void Write(Exception ex)
    {
        Write(ex.StackTrace);
    }

    /// <summary>
    /// Writes a key-value pair to the log.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="data">The data value.</param>
    public void Write(string? key, object? data)
    {
        if (key != null)
            _sb.Append(key + ": ");
        if (data != null)
            _sb.Append(data.ToString() + "\r\n");
    }

    /// <summary>
    /// Writes formatted data to the log.
    /// </summary>
    /// <param name="format">The format string.</param>
    /// <param name="args">The format arguments.</param>
    public void WriteFormat(string format, params object[] args)
    {
        Write(string.Format(format, args));
    }

    /// <summary>
    /// Copies the contents of another logger to this logger.
    /// </summary>
    /// <param name="other">The logger to copy from.</param>
    internal void Write(GSLogger? other)
    {
        if (other != null)
            _sb.Append(other.ToString());
    }

    /// <summary>
    /// Returns the log contents as a string.
    /// </summary>
    /// <returns>The log contents.</returns>
    public override string ToString()
    {
        return _sb.ToString();
    }
}