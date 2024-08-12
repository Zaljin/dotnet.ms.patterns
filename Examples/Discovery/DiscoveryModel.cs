namespace Discovery
{
    public class DiscoveryModel
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public DateTime LastUpdateTimestamp { get; set; }
    }
}
