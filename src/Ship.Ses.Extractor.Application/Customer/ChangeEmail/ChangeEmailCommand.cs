namespace Ship.Ses.Extractor.Application.Customer.ChangeEmail
{
    public sealed record ChangeEmailCommand(Guid CustomerId, string NewEmail);
}
