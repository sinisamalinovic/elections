using System;
using System.Collections.Generic;
using System.Linq;

namespace PosmatraciApp.Models
{
    public class BmState
    {
        public string BmId { get; set; } = "";
        public int BmShortId { get; set; }
        public string BmDisplayName { get; set; } = "";
        public string OpstineName { get; set; } = "";
        public string BjName { get; set; } = "";
        public DateTime LastModified { get; set; }

        // Parallel arrays indexed by item.Id (0..totalItems-1)
        public AnswerState[] Answers { get; set; } = Array.Empty<AnswerState>();
        public CheckSeverity[] Severities { get; set; } = Array.Empty<CheckSeverity>();
        public Dictionary<int, string> Notes { get; set; } = new();

        public int TotalItems => Answers.Length;
        public int AnsweredCount => Answers.Count(a => a != AnswerState.Unanswered);
        public int NeCount => Answers.Count(a => a == AnswerState.Ne);

        public static BmState CreateNew(BirackoMesto bm, string opstineName, string bjName, int totalItems)
        {
            return new BmState
            {
                BmId = bm.Id,
                BmShortId = bm.ShortId,
                BmDisplayName = bm.Name,
                OpstineName = opstineName,
                BjName = bjName,
                LastModified = DateTime.UtcNow,
                Answers = new AnswerState[totalItems],
                Severities = new CheckSeverity[totalItems],
                Notes = new Dictionary<int, string>()
            };
        }
    }
}
