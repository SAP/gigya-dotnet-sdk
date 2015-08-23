/*
 * Copyright (C) 2011 Gigya, Inc.
 */

using System;

namespace Gigya.Socialize.SDK
{
    /// <summary>
    /// General Gigya exception.
    /// </summary>
    /// <remarks>author Raviv Pavel</remarks>
    public class GSException : Exception
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="msg">Exception message</param>
        public GSException(string msg):base(msg) {}
    }

    /// <summary>
    /// Thrown when attempting to fetch a key that does not exist.
    /// </summary>
    /// <remarks>author Raviv Pavel</remarks> 
    public class GSKeyNotFoundException : GSException
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="msg">Exception message</param>
        public GSKeyNotFoundException(string msg): base(msg) {}        
    }

    /// <summary>
    /// Thrown when attempting to fetch a key from a response that failed to arrive. You should check the response error code
    /// before attempting to probe it.
    /// </summary>
    /// <remarks>author Raviv Pavel</remarks> 
    public class GSResponseNotInitializedException : GSException
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="msg">Exception message</param>
        public GSResponseNotInitializedException(): base("You're trying to access data fields in a response that failed to arrive. Please check the response error code first") {}
    }
}
