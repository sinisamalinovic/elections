using BirackaMestaReport.Data;
using Microsoft.EntityFrameworkCore;
using PosmatraciApp.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace BirackaMestaReport.Services
{
    public class BmReportRow
    {
        public string BmId { get; set; } = "";
        public int BmShortId { get; set; }
        public string BmDisplayName { get; set; } = "";
        public string OpstineName { get; set; } = "";
        public string BjName { get; set; } = "";
        public string Email { get; set; } = "";
        public DateTime ReceivedAt { get; set; }
        public BmState? BmState { get; set; }
        public BmIzlaznost? BmIzlaznost { get; set; }
    }

    public class ReportService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public ReportService(IDbContextFactory<AppDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        /// <summary>
        /// Returns the latest submission per BmId.
        /// </summary>
        public async Task<List<BmReportRow>> GetLatestPerBmAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            // Get max ReceivedAt per BmId first, then join to get full rows
            var maxPerBm = await db.Submissions
                .GroupBy(s => s.BmId)
                .Select(g => new { BmId = g.Key, MaxAt = g.Max(s => s.ReceivedAt) })
                .ToListAsync();

            var latest = new List<BmSubmission>();
            foreach (var m in maxPerBm)
            {
                var row = await db.Submissions
                    .Where(s => s.BmId == m.BmId && s.ReceivedAt == m.MaxAt)
                    .FirstOrDefaultAsync();
                if (row != null) latest.Add(row);
            }
            latest = latest.OrderBy(s => s.BmShortId).ToList();

            return latest.Select(s => new BmReportRow
            {
                BmId = s.BmId,
                BmShortId = s.BmShortId,
                BmDisplayName = s.BmDisplayName,
                OpstineName = s.OpstineName,
                BjName = s.BjName,
                Email = s.Email,
                ReceivedAt = s.ReceivedAt,
                BmState = TryDeserialize<BmState>(s.BmStateJson),
                BmIzlaznost = TryDeserialize<BmIzlaznost>(s.BmIzlaznostJson)
            }).ToList();
        }

        public async Task SaveSubmissionAsync(string email, BmState bmState, BmIzlaznost? bmIzlaznost)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            db.Submissions.Add(new BmSubmission
            {
                BmId = bmState.BmId,
                BmShortId = bmState.BmShortId,
                BmDisplayName = bmState.BmDisplayName,
                OpstineName = bmState.OpstineName,
                BjName = bmState.BjName,
                Email = email,
                ReceivedAt = DateTime.UtcNow,
                BmStateJson = JsonSerializer.Serialize(bmState),
                BmIzlaznostJson = bmIzlaznost != null ? JsonSerializer.Serialize(bmIzlaznost) : null
            });
            await db.SaveChangesAsync();
        }

        public async Task<List<string>> GetOpstinaListAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.Submissions
                .Select(s => s.OpstineName)
                .Distinct()
                .OrderBy(n => n)
                .ToListAsync();
        }

        private static T? TryDeserialize<T>(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return default;
            try { return JsonSerializer.Deserialize<T>(json); }
            catch { return default; }
        }
    }
}
