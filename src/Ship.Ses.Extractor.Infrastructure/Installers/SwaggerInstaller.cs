﻿using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Ship.Ses.Extractor.Infrastructure.Installers
{
    public static class SwaggerInstaller
    {
        public static void InstallSwagger(this WebApplicationBuilder builder)
        {
            builder.Services.AddSwaggerGen(options => options.EnableAnnotations());
        }
    }

}
