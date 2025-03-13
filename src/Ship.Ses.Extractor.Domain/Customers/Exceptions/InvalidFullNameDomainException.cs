﻿namespace Ship.Ses.Extractor.Domain.Customers.Exceptions
{
    public class InvalidFullNameDomainException : DomainException
    {
        public InvalidFullNameDomainException(string fullName)
            : base($"Fullname of '{fullName}' should consist of more than 3 and less than 150 characters.")
        {

        }
    }
}
