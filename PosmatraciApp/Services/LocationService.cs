using PosmatraciApp.Models;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace PosmatraciApp.Services
{
    public class LocationService
    {
        private List<Opstina> _opstine = new();

        public IReadOnlyList<Opstina> Opstine => _opstine;

        public async Task InitializeAsync(HttpClient http)
        {
            var data = await http.GetFromJsonAsync<LocationData>("data/locations.json");
            if (data != null)
                _opstine = data.Opstine;
        }

        public BirackoMesto? FindBm(string bmId)
        {
            return _opstine
                .SelectMany(o => o.BiracskeJedinice)
                .SelectMany(bj => bj.BiracaMesta)
                .FirstOrDefault(bm => bm.Id == bmId);
        }

        public (string opstinaName, string bjName) GetBmContext(string bmId)
        {
            foreach (var o in _opstine)
                foreach (var bj in o.BiracskeJedinice)
                    foreach (var bm in bj.BiracaMesta)
                        if (bm.Id == bmId)
                            return (o.Name, bj.Name);
            return ("", "");
        }

        public List<BirackoMesto> GetBmsForBj(string bjId)
        {
            return _opstine
                .SelectMany(o => o.BiracskeJedinice)
                .Where(bj => bj.Id == bjId)
                .SelectMany(bj => bj.BiracaMesta)
                .ToList();
        }
    }
}
