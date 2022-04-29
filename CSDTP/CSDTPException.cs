namespace CSDTP
{
    public class CSDTPException : Exception
    {
        public CSDTPException() { }

        public CSDTPException(string message) : base(message) { }

        public CSDTPException(string message, Exception innerException) : base(message, innerException) { }
    }
}
