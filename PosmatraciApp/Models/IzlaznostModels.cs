using System;
using System.Collections.Generic;

namespace PosmatraciApp.Models
{
    public class IzlaznostEntry
    {
        public DateTime Timestamp { get; set; }
        public int BrojGlasaca { get; set; }
    }

    public class BmIzlaznost
    {
        public string BmId { get; set; } = "";
        public int BmShortId { get; set; }
        public string BmDisplayName { get; set; } = "";
        public int? UkupnoBiraca { get; set; }
        public List<IzlaznostEntry> Unosi { get; set; } = new();

        public double? PostotakIzlaznosti(int broj) =>
            UkupnoBiraca is > 0 ? Math.Round((double)broj / UkupnoBiraca.Value * 100, 1) : null;
    }
}
