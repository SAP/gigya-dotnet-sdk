/*
 * Copyright (C) 2024 SAP SE
 * Modern .NET 9 SDK - Exception Classes
 */

namespace Gigya.Socialize.SDK;

/// <summary>
/// General Gigya exception.
/// </summary>
public class GSException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GSException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public GSException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GSException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public GSException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Thrown when attempting to fetch a key that does not exist in a GSObject.
/// </summary>
public class GSKeyNotFoundException : GSException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GSKeyNotFoundException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public GSKeyNotFoundException(string message) : base(message)
    {
    }
}

/// <summary>
/// Thrown when attempting to fetch data from a response that failed to arrive.
/// You should check the response error code before attempting to probe it.
/// </summary>
public class GSResponseNotInitializedException : GSException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GSResponseNotInitializedException"/> class.
    /// </summary>
    public GSResponseNotInitializedException()
        : base("You're trying to access data fields in a response that failed to arrive. Please check the response error code first.")
    {
    }
}