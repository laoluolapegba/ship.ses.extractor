using Ship.Ses.Extractor.Application.Shared;
using Ship.Ses.Extractor.Domain.Enums;
using Ship.Ses.Extractor.Infrastructure.Exceptions;

namespace Ship.Ses.Extractor.Infrastructure.Shared
{
    public class EmailTemplateFactory : IEmailTemplateFactory
    {
        private readonly string _templateDirectory = "EmailTemplates";

        public async Task<string> GetTemplateAsync(EmailTemplateType templateType)
        {
            var fileName = $"{templateType}.html";
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Ship.Ses.Extractor.Infrastructure", "EmailTemplates", fileName);

            if (!File.Exists(filePath))
            {
                throw new InfrastructureException($"Template '{fileName}' not found in '{_templateDirectory}'.");
            }

            return await File.ReadAllTextAsync(filePath);
        }

    }

}
