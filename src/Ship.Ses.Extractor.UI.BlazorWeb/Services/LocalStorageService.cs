﻿using Blazored.LocalStorage;
using System.Threading.Tasks;
namespace Ship.Ses.Extractor.UI.BlazorWeb.Services
{


    public class LocalStorageService
    {
        private readonly ILocalStorageService _localStorage;

        public LocalStorageService(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        public async Task<T> GetItemAsync<T>(string key)
        {
            return await _localStorage.GetItemAsync<T>(key);
        }

        public async Task SetItemAsync<T>(string key, T value)
        {
            await _localStorage.SetItemAsync(key, value);
        }

        public async Task RemoveItemAsync(string key)
        {
            await _localStorage.RemoveItemAsync(key);
        }
    }
}
