using PosmatraciApp.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PosmatraciApp.Services
{
    public class EmailData
    {
        public int Version { get; set; }
        public List<string> Emails { get; set; } = new();
    }

    public class AuthService
    {
        private readonly HttpClient _http;
        private readonly StorageService _storage;
        private HashSet<string> _validEmails = new();

        public ObserverSession? CurrentSession { get; private set; }
        public bool IsAuthenticated => CurrentSession != null;

        public AuthService(HttpClient http, StorageService storage)
        {
            _http = http;
            _storage = storage;
        }

        public async Task InitializeAsync()
        {
            var data = await _http.GetFromJsonAsync<EmailData>("data/emails.json");
            if (data != null)
            {
                foreach (var email in data.Emails)
                    _validEmails.Add(email.ToLowerInvariant().Trim());
            }
            CurrentSession = await _storage.GetAsync<ObserverSession>("posmatraci_session");
        }

        public async Task<bool> LoginAsync(string email)
        {
            var normalized = email.ToLowerInvariant().Trim();
            if (!_validEmails.Contains(normalized))
                return false;

            var hash = ComputeEmailHash(normalized);
            CurrentSession = new ObserverSession
            {
                Email = normalized,
                EmailHash = hash,
                LoginTime = DateTime.UtcNow,
                ActiveBmIds = new List<string>()
            };
            await _storage.SetAsync("posmatraci_session", CurrentSession);
            return true;
        }

        public async Task LogoutAsync()
        {
            CurrentSession = null;
            await _storage.RemoveAsync("posmatraci_session");
        }

        public async Task SaveSessionAsync()
        {
            if (CurrentSession != null)
                await _storage.SetAsync("posmatraci_session", CurrentSession);
        }

        private static string ComputeEmailHash(string email)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(email));
            return Convert.ToHexString(bytes, 0, 4).ToLower();
        }
    }
}
