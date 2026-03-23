using PosmatraciApp.Shared.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PosmatraciApp.Services
{
    public class IzlaznostService
    {
        private readonly StorageService _storage;
        private readonly Dictionary<string, BmIzlaznost> _data = new();

        public IzlaznostService(StorageService storage)
        {
            _storage = storage;
        }

        public async Task LoadAsync(List<string> bmIds)
        {
            foreach (var bmId in bmIds)
            {
                var saved = await _storage.GetAsync<BmIzlaznost>(StorageKey(bmId));
                if (saved != null)
                    _data[bmId] = saved;
            }
        }

        public BmIzlaznost GetOrCreate(BirackoMesto bm)
        {
            if (!_data.TryGetValue(bm.Id, out var existing))
            {
                existing = new BmIzlaznost
                {
                    BmId = bm.Id,
                    BmShortId = bm.ShortId,
                    BmDisplayName = bm.Name
                };
                _data[bm.Id] = existing;
            }
            return existing;
        }

        public BmIzlaznost? Get(string bmId)
        {
            _data.TryGetValue(bmId, out var d);
            return d;
        }

        public async Task SetUkupnoBiracaAsync(string bmId, int ukupno)
        {
            if (!_data.TryGetValue(bmId, out var d)) return;
            d.UkupnoBiraca = ukupno;
            await SaveAsync(bmId);
        }

        public async Task AddUnosAsync(string bmId, int brojGlasaca)
        {
            if (!_data.TryGetValue(bmId, out var d)) return;
            d.Unosi.Add(new IzlaznostEntry
            {
                Timestamp = DateTime.UtcNow,
                BrojGlasaca = brojGlasaca
            });
            await SaveAsync(bmId);
        }

        public async Task SaveAsync(string bmId)
        {
            if (_data.TryGetValue(bmId, out var d))
                await _storage.SetAsync(StorageKey(bmId), d);
        }

        private static string StorageKey(string bmId) => $"posmatraci_izlaznost_{bmId}";
    }
}
