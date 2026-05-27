using System.Threading;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Interfaces.Storage;
using RimMind.Domain.Settings;
using RimMind.Presentation.Settings;
using UnityEngine;
using Verse;

namespace RimMind.Presentation.UI
{
    public class RemoteSyncSettingsUI : ISettingsTab
    {
        public string Id => "remotesync";
        public string OwnerModId => "RimMindCore";
        public string Label => "RimMind.Settings.Tab.RemoteSync".Translate();

        private static Vector2 _scrollPos = Vector2.zero;
        private static string _statusText = "";
        private static Color _statusColor = Color.white;

        private static RemoteSyncSettings? _cachedSettings;
        private static IRemoteSyncService? _cachedSyncService;

        private static RemoteSyncSettings? GetSettings()
            => _cachedSettings ??= RimMindServiceLocator.Get<RemoteSyncSettings>();

        private static IRemoteSyncService? GetSyncService()
            => _cachedSyncService ??= RimMindServiceLocator.Get<IRemoteSyncService>();

        public void Draw(Rect inRect)
        {
            float contentH = EstimateHeight();
            Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, contentH);
            Widgets.BeginScrollView(inRect, ref _scrollPos, viewRect);

            var listing = new Listing_Standard();
            listing.Begin(viewRect);

            var settings = GetSettings();
            var syncService = GetSyncService();

            if (settings == null)
            {
                listing.Label("RimMind.Settings.RemoteSync.NotAvailable".Translate());
                listing.End();
                Widgets.EndScrollView();
                return;
            }

            // Copy to local for CheckboxLabeled ref parameters
            bool autoPull = settings.AutoPull;
            bool autoPush = settings.AutoPush;
            bool syncMemory = settings.SyncMemory;
            bool syncSettings = settings.SyncSettings;
            bool syncAgentIdentity = settings.SyncAgentIdentity;

            SettingsUIDrawer.DrawSectionHeader(listing, "RimMind.Settings.RemoteSync.Section.AutoSync".Translate());

            listing.CheckboxLabeled(
                "RimMind.Settings.RemoteSync.AutoPull".Translate(),
                ref autoPull,
                "RimMind.Settings.RemoteSync.AutoPull.Desc".Translate());

            listing.CheckboxLabeled(
                "RimMind.Settings.RemoteSync.AutoPush".Translate(),
                ref autoPush,
                "RimMind.Settings.RemoteSync.AutoPush.Desc".Translate());

            SettingsUIDrawer.DrawSectionHeader(listing, "RimMind.Settings.RemoteSync.Section.Granularity".Translate());

            listing.CheckboxLabeled(
                "RimMind.Settings.RemoteSync.SyncMemory".Translate(),
                ref syncMemory,
                "RimMind.Settings.RemoteSync.SyncMemory.Desc".Translate());

            listing.CheckboxLabeled(
                "RimMind.Settings.RemoteSync.SyncSettings".Translate(),
                ref syncSettings,
                "RimMind.Settings.RemoteSync.SyncSettings.Desc".Translate());

            listing.CheckboxLabeled(
                "RimMind.Settings.RemoteSync.SyncAgentIdentity".Translate(),
                ref syncAgentIdentity,
                "RimMind.Settings.RemoteSync.SyncAgentIdentity.Desc".Translate());

            // Write back from locals to properties
            settings.AutoPull = autoPull;
            settings.AutoPush = autoPush;
            settings.SyncMemory = syncMemory;
            settings.SyncSettings = syncSettings;
            settings.SyncAgentIdentity = syncAgentIdentity;

            SettingsUIDrawer.DrawSectionHeader(listing, "RimMind.Settings.RemoteSync.Section.Manual".Translate());

            bool isConfigured = syncService?.IsConfigured ?? false;

            if (!isConfigured)
            {
                GUI.color = Color.yellow;
                listing.Label("RimMind.Settings.RemoteSync.NotConfigured".Translate());
                GUI.color = Color.white;
            }

            Rect pullRow = listing.GetRect(30f);
            if (Widgets.ButtonText(pullRow, "RimMind.Settings.RemoteSync.ManualPull".Translate()))
            {
                if (isConfigured && syncService != null)
                {
                    _statusText = "RimMind.Settings.RemoteSync.Pulling".Translate();
                    _statusColor = Color.cyan;
                    _ = System.Threading.Tasks.Task.Run(async () =>
                    {
                        try
                        {
                            var result = await syncService.ManualPullAsync("all", CancellationToken.None);
                            LongEventHandler.ExecuteWhenFinished(() =>
                            {
                                _statusText = result.IsOk
                                    ? "RimMind.Settings.RemoteSync.PullSuccess".Translate()
                                    : "RimMind.Settings.RemoteSync.PullFailed".Translate(result.Error?.Message ?? "");
                                _statusColor = result.IsOk ? Color.green : Color.red;
                            });
                        }
                        catch (System.Exception ex)
                        {
                            LongEventHandler.ExecuteWhenFinished(() =>
                            {
                                _statusText = "RimMind.Settings.RemoteSync.PullFailed".Translate(ex.Message);
                                _statusColor = Color.red;
                            });
                        }
                    });
                }
            }

            Rect pushRow = listing.GetRect(30f);
            if (Widgets.ButtonText(pushRow, "RimMind.Settings.RemoteSync.ManualPush".Translate()))
            {
                if (isConfigured && syncService != null)
                {
                    _statusText = "RimMind.Settings.RemoteSync.Pushing".Translate();
                    _statusColor = Color.cyan;
                    _ = System.Threading.Tasks.Task.Run(async () =>
                    {
                        try
                        {
                            var result = await syncService.ManualPushAsync("all", "{}", 0, CancellationToken.None);
                            LongEventHandler.ExecuteWhenFinished(() =>
                            {
                                _statusText = result.IsOk
                                    ? "RimMind.Settings.RemoteSync.PushSuccess".Translate()
                                    : "RimMind.Settings.RemoteSync.PushFailed".Translate(result.Error?.Message ?? "");
                                _statusColor = result.IsOk ? Color.green : Color.red;
                            });
                        }
                        catch (System.Exception ex)
                        {
                            LongEventHandler.ExecuteWhenFinished(() =>
                            {
                                _statusText = "RimMind.Settings.RemoteSync.PushFailed".Translate(ex.Message);
                                _statusColor = Color.red;
                            });
                        }
                    });
                }
            }

            if (!string.IsNullOrEmpty(_statusText))
            {
                GUI.color = _statusColor;
                listing.Label(_statusText);
                GUI.color = Color.white;
            }

            listing.End();
            Widgets.EndScrollView();
        }

        private static float EstimateHeight()
        {
            float h = 30f;
            // AutoSync section: header + 2 checkboxes
            h += 24f + 24f + 24f;
            // Granularity section: header + 3 checkboxes
            h += 24f + 24f + 24f + 24f;
            // Manual section: header + warning? + 2 buttons + status
            h += 24f + 30f + 30f + 24f;
            return h + 40f;
        }
    }
}
