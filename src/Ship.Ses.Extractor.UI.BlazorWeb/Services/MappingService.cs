using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ship.Ses.Extractor.UI.BlazorWeb.Models.ApiClient;
namespace Ship.Ses.Extractor.UI.BlazorWeb.Services
{

    public class MappingService
    {
        private readonly ApiClientService _apiClient;

        public MappingService(ApiClientService apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<List<MappingModel>> GetMappingsAsync()
        {
            return await _apiClient.GetAsync<List<MappingModel>>("/api/mappings");
        }

        public async Task<MappingModel> GetMappingAsync(Guid id)
        {
            return await _apiClient.GetAsync<MappingModel>($"/api/mappings/{id}");
        }

        public async Task<List<MappingModel>> GetMappingsByResourceTypeAsync(int resourceTypeId)
        {
            return await _apiClient.GetAsync<List<MappingModel>>($"/api/mappings/by-resource-type/{resourceTypeId}");
        }

        public async Task<Guid> CreateMappingAsync(MappingModel mapping)
        {
            return await _apiClient.PostAsync<Guid>("/api/mappings", mapping);
        }

        public async Task UpdateMappingAsync(MappingModel mapping)
        {
            await _apiClient.PutAsync($"/api/mappings/{mapping.Id}", mapping);
        }

        public async Task DeleteMappingAsync(Guid id)
        {
            await _apiClient.DeleteAsync($"/api/mappings/{id}");
        }

        public async Task<string> ExportMappingAsJsonAsync(Guid id)
        {
            return await _apiClient.GetAsync<string>($"/api/mappings/{id}/export");
        }
    }

}
