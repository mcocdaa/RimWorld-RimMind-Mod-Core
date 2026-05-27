using System;
using RimMind.Application.Common.Interfaces.Storage;
using UnityEngine;
using Verse;

namespace RimMind.Infrastructure.UI
{
    /// <summary>
    /// UI component for manual NPC data synchronization with remote backend.
    /// </summary>
    public static class NpcSyncActions
    {
        private static string _lastSyncStatus = "";
        private static bool _isSyncing;

        public static void DrawNpcSyncActions(Rect rect, string npcId, IRemoteSyncService? syncService)
        {
            if (syncService == null)
            {
                Widgets.Label(rect, "RimMind.RemoteSync.Unavailable".Translate());
                return;
            }

            float buttonHeight = 30f;
            float gap = 4f;
            float curY = rect.y;

            // Status label
            if (!string.IsNullOrEmpty(_lastSyncStatus))
            {
                var statusRect = new Rect(rect.x, curY, rect.width, buttonHeight);
                Widgets.Label(statusRect, _lastSyncStatus);
                curY += buttonHeight + gap;
            }

            // Pull button
            var pullRect = new Rect(rect.x, curY, rect.width / 2f - gap / 2f, buttonHeight);
            if (Widgets.ButtonText(pullRect, "RimMind.RemoteSync.PullNpc".Translate()))
            {
                _ = PullNpcAsync(syncService, npcId);
            }

            // Push button
            var pushRect = new Rect(rect.x + rect.width / 2f + gap / 2f, curY, rect.width / 2f - gap / 2f, buttonHeight);
            if (Widgets.ButtonText(pushRect, "RimMind.RemoteSync.PushNpc".Translate()))
            {
                _ = PushNpcAsync(syncService, npcId);
            }
        }

        private static async System.Threading.Tasks.Task PullNpcAsync(IRemoteSyncService syncService, string npcId)
        {
            if (_isSyncing) return;
            _isSyncing = true;
            _lastSyncStatus = "RimMind.RemoteSync.Syncing".Translate();
            try
            {
                var result = await syncService.ManualPullAsync(npcId);
                _lastSyncStatus = result.IsOk
                    ? "RimMind.RemoteSync.PullSuccess".Translate()
                    : $"{"RimMind.RemoteSync.PullFailed".Translate()}: {result.Error.Message}";
            }
            catch (Exception ex)
            {
                _lastSyncStatus = $"{"RimMind.RemoteSync.PullFailed".Translate()}: {ex.Message}";
            }
            finally
            {
                _isSyncing = false;
            }
        }

        private static async System.Threading.Tasks.Task PushNpcAsync(IRemoteSyncService syncService, string npcId)
        {
            if (_isSyncing) return;
            _isSyncing = true;
            _lastSyncStatus = "RimMind.RemoteSync.Syncing".Translate();
            try
            {
                var result = await syncService.EnqueuePushAsync(npcId, "", 0);
                _lastSyncStatus = result.IsOk
                    ? "RimMind.RemoteSync.PushQueued".Translate()
                    : $"{"RimMind.RemoteSync.PushFailed".Translate()}: {result.Error.Message}";
            }
            catch (Exception ex)
            {
                _lastSyncStatus = $"{"RimMind.RemoteSync.PushFailed".Translate()}: {ex.Message}";
            }
            finally
            {
                _isSyncing = false;
            }
        }
    }
}
