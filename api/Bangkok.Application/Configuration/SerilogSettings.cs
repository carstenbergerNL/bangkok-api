namespace Bangkok.Application.Configuration;

public class SerilogSettings
{
    public const string SectionName = "Serilog";

    public string MinimumLevel { get; set; } = "Information";
    public string MinimumLevelOverrideMicrosoft { get; set; } = "Warning";
}
