using System.Collections.Generic;
using System.Threading.Tasks;
using global::Ship.Ses.Extractor.UI.BlazorWeb.Models.ApiClient;

namespace Ship.Ses.Extractor.UI.BlazorWeb.Services
{
    
    public class EmrDatabaseService
    {
        private readonly ApiClientService _apiClient;

        public EmrDatabaseService(ApiClientService apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<List<EmrTableModel>> GetTablesAsync()
        {
            return await _apiClient.GetAsync<List<EmrTableModel>>("/api/emr/tables");
        }

        public async Task<EmrTableModel> GetTableSchemaAsync(string tableName)
        {
            return await _apiClient.GetAsync<EmrTableModel>($"/api/emr/tables/{tableName}");
        }

        public async Task TestConnectionAsync()
        {
            await _apiClient.GetAsync<object>("/api/emr/test-connection");
        }
    }
}
