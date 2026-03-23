using Microsoft.JSInterop;
using System.Text.Json;
using System.Threading.Tasks;

namespace PosmatraciApp.Services
{
    public class StorageService
    {
        private readonly IJSRuntime _js;

        public StorageService(IJSRuntime js)
        {
            _js = js;
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            var json = await _js.InvokeAsync<string?>("localStorage.getItem", key);
            if (json == null) return default;
            return JsonSerializer.Deserialize<T>(json);
        }

        public async Task SetAsync<T>(string key, T value)
        {
            var json = JsonSerializer.Serialize(value);
            await _js.InvokeVoidAsync("localStorage.setItem", key, json);
        }

        public async Task RemoveAsync(string key)
        {
            await _js.InvokeVoidAsync("localStorage.removeItem", key);
        }
    }
}
