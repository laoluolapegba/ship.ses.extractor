﻿using Ship.Ses.Extractor.Domain.Models.Extractor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship.Ses.Extractor.Application.Services
{
    public interface ITableMappingService
    {
        Task<TableMapping> GetMappingForResourceAsync(string resourceType, CancellationToken cancellationToken = default);
    }

}
