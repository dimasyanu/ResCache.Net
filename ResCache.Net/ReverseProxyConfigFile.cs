namespace ResCache.Net;

public class ReverseProxyConfigFile
{
    public required string FilePath { get; set; }
    public bool Optional { get; set; }
    public bool ReloadOnChange { get; set; }
}
