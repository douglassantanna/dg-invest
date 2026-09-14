namespace api.Data;

public sealed class MigrationsSettings
{
    public bool RemoteTriggerEnabled { get; set; }

    public string RemoteTriggerToken { get; set; } = string.Empty;
}
