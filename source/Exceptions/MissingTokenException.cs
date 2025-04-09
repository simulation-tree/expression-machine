using System;

namespace ExpressionMachine
{
    /// <summary>
    /// Error when an expected token is missing.
    /// </summary>
    public class MissingTokenException : Exception
    {

    }

    /// <summary>
    /// Error when a token to close a group is missing.
    /// </summary>
    public class MissingGroupCloseToken : Exception
    {

    }
}
