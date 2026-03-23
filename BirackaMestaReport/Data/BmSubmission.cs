using System;
using System.ComponentModel.DataAnnotations;

namespace BirackaMestaReport.Data
{
    public class BmSubmission
    {
        public int Id { get; set; }

        [Required] public string BmId { get; set; } = "";
        public int BmShortId { get; set; }
        [Required] public string BmDisplayName { get; set; } = "";
        public string OpstineName { get; set; } = "";
        public string BjName { get; set; } = "";
        [Required] public string Email { get; set; } = "";
        public DateTime ReceivedAt { get; set; }
        public string BmStateJson { get; set; } = "";
        public string? BmIzlaznostJson { get; set; }
    }
}
