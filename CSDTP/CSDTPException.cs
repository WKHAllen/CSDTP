namespace CSDTP
{
    /// <summary>
    /// A CSDTP exception.
    /// </summary>
    public class CSDTPException : Exception
    {
        /// <summary>
        /// Instantiate a CSDTP exception.
        /// </summary>
        public CSDTPException() { }

        /// <summary>
        /// Instantiate a CSDTP exception with an error message.
        /// </summary>
        /// <param name="message">the error message.</param>
        public CSDTPException(string message) : base(message) { }

        /// <summary>
        /// Instantiate a CSDTP exception with an error message and an inner exception.
        /// </summary>
        /// <param name="message">the error message.</param>
        /// <param name="innerException">the inner exception.</param>
        public CSDTPException(string message, Exception innerException) : base(message, innerException) { }
    }
}
