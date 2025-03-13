using Ship.Ses.Extractor.Domain.Enums;

namespace Ship.Ses.Extractor.Application.Shared
{
    public interface IEmailTemplateFactory
    {
        Task<string> GetTemplateAsync(EmailTemplateType templateType);
    }
}
