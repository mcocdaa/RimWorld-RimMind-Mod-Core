using RimMind.Application.Common.Interfaces.Client;

namespace RimMind.Infrastructure.Services.Clients.Player2
{
    internal sealed class Player2LifecycleService : IPlayer2Lifecycle
    {
        public void StopHealthCheck()
        {
            Player2Client.StopHealthCheck();
        }

        public void CheckStatusAndNotify()
        {
            Player2Client.CheckPlayer2StatusAndNotify();
        }

        public void RefreshBalance()
        {
            Player2Client.RefreshJoulesBalance();
        }

        public float CachedBalance => Player2Client.CachedJoulesBalance;
        public bool IsAvailable => Player2Client.CachedJoulesBalance >= 0;
    }
}
