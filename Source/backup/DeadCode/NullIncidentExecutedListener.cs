namespace RimMind.Application.Common.Defaults
{
    public sealed class NullIncidentExecutedListener : RimMind.Application.Common.Interfaces.Extension.IIncidentExecutedListener
    {
        public static readonly NullIncidentExecutedListener Instance = new NullIncidentExecutedListener();

        public string Id => "null.incident-executed-listener";
        public void OnIncidentExecuted() { }
    }
}
