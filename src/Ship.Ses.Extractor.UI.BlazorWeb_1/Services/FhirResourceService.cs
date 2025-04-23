using System.Collections.Generic;
using System.Threading.Tasks;
using global::Ship.Ses.Extractor.UI.BlazorWeb.Models.ApiClient;

namespace Ship.Ses.Extractor.UI.BlazorWeb.Services
{
   
    public class FhirResourceService
    {
        private readonly ApiClientService _apiClient;

        public FhirResourceService(ApiClientService apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<List<FhirResourceTypeModel>> GetResourceTypesAsync()
        {
            return await _apiClient.GetAsync<List<FhirResourceTypeModel>>("/api/mappings/resource-types");
        }

        public async Task<FhirResourceTypeModel> GetResourceTypeAsync(int id)
        {
            return await _apiClient.GetAsync<FhirResourceTypeModel>($"/api/mappings/resource-types/{id}");
        }

        public async Task<string> GetResourceStructureAsync(int resourceTypeId)
        {
            return await _apiClient.GetAsync<string>($"/api/mappings/resource-types/{resourceTypeId}/structure");
        }
    }

}
