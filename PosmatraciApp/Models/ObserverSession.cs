using System;
using System.Collections.Generic;

namespace PosmatraciApp.Models
{
    public class ObserverSession
    {
        public string Email { get; set; } = "";
        public string EmailHash { get; set; } = "";
        public DateTime LoginTime { get; set; }
        public List<string> ActiveBmIds { get; set; } = new();
        public string? CurrentBmId { get; set; }
    }
}
