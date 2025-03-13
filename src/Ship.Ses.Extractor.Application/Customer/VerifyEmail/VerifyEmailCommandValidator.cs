using FluentValidation;

namespace Ship.Ses.Extractor.Application.Customer.VerifyEmail
{
    public class VerifyEmailCommandValidator : AbstractValidator<VerifyEmailCommand>, IValidator
    {
        public VerifyEmailCommandValidator()
        {
            RuleFor(x => x.CustomerId).NotEmpty();
        }
    }
}
