namespace Ship.Ses.Extractor.Application.Exceptions
{
    public class CustomerNotFoundApplicationException : ApplicationException
    {
        public CustomerNotFoundApplicationException(Guid id)
            : base($"Customer of id: '{id}' has not been found")
        {
        }
    }
}
