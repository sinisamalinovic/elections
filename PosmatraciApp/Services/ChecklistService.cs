using PosmatraciApp.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PosmatraciApp.Services
{
    public class ChecklistService : IAsyncDisposable
    {
        private readonly StorageService _storage;
        private readonly Dictionary<string, BmState> _bmStates = new();
        private List<ChecklistSectionDefinition> _sections = new();
        private Timer? _autoSaveTimer;
        private bool _isDirty;

        public int TotalItems { get; private set; }
        public IReadOnlyList<ChecklistSectionDefinition> Sections => _sections;

        public ChecklistService(StorageService storage)
        {
            _storage = storage;
        }

        public async Task InitializeAsync(HttpClient http)
        {
            var def = await http.GetFromJsonAsync<ChecklistDefinition>("data/checklist.json");
            if (def != null)
            {
                _sections = def.Sections;
                TotalItems = def.TotalItems;
            }
        }

        public async Task LoadBmStatesAsync(List<string> bmIds)
        {
            foreach (var bmId in bmIds)
            {
                var state = await _storage.GetAsync<BmState>($"posmatraci_bm_{bmId}");
                if (state != null)
                    _bmStates[bmId] = state;
            }
        }

        public BmState GetOrCreateBmState(BirackoMesto bm, string opstinaName, string bjName)
        {
            if (!_bmStates.TryGetValue(bm.Id, out var state))
            {
                state = BmState.CreateNew(bm, opstinaName, bjName, TotalItems);
                _bmStates[bm.Id] = state;
            }
            return state;
        }

        public BmState? GetBmState(string bmId)
        {
            _bmStates.TryGetValue(bmId, out var state);
            return state;
        }

        public void SetAnswer(string bmId, int itemId, AnswerState answer)
        {
            if (!_bmStates.TryGetValue(bmId, out var state)) return;
            state.Answers[itemId] = answer;
            state.LastModified = DateTime.UtcNow;
            MarkDirtyAndScheduleSave();
        }

        public void SetSeverity(string bmId, int itemId, CheckSeverity severity)
        {
            if (!_bmStates.TryGetValue(bmId, out var state)) return;
            state.Severities[itemId] = severity;
            state.LastModified = DateTime.UtcNow;
            MarkDirtyAndScheduleSave();
        }

        public void SetNote(string bmId, int itemId, string note)
        {
            if (!_bmStates.TryGetValue(bmId, out var state)) return;
            if (string.IsNullOrWhiteSpace(note))
                state.Notes.Remove(itemId);
            else
                state.Notes[itemId] = note;
            state.LastModified = DateTime.UtcNow;
            MarkDirtyAndScheduleSave();
        }

        public async Task RemoveBmStateAsync(string bmId)
        {
            _bmStates.Remove(bmId);
            await _storage.RemoveAsync($"posmatraci_bm_{bmId}");
        }

        public async Task SaveAllAsync()
        {
            foreach (var (bmId, state) in _bmStates)
                await _storage.SetAsync($"posmatraci_bm_{bmId}", state);
            _isDirty = false;
        }

        private void MarkDirtyAndScheduleSave()
        {
            _isDirty = true;
            _autoSaveTimer?.Dispose();
            _autoSaveTimer = new Timer(async _ =>
            {
                if (_isDirty)
                    await SaveAllAsync();
            }, null, TimeSpan.FromSeconds(30), Timeout.InfiniteTimeSpan);
        }

        public async ValueTask DisposeAsync()
        {
            _autoSaveTimer?.Dispose();
            if (_isDirty)
                await SaveAllAsync();
        }
    }
}
