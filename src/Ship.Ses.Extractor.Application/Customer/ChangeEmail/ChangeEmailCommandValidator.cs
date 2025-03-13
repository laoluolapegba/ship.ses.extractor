using Ship.Ses.Extractor.Domain.Orders;
using FluentValidation;

namespace Ship.Ses.Extractor.Application.Customer.ChangeEmail
{
    public class ChangeEmailCommandValidator : AbstractValidator<ChangeEmailCommand>, IValidator
    {
        public ChangeEmailCommandValidator()
        {
            RuleFor(x => x.CustomerId).NotEmpty();
            RuleFor(x => x.NewEmail).NotEmpty().MaximumLength(CustomerConstants.Customer.EmailMaxLength);
        }
    }
}
