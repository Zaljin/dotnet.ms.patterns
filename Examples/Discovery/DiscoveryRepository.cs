using Discovery.Core.Exceptions;

namespace Discovery;

public interface IDiscoveryRepository
{
    public DiscoveryModel Find(string name, string version);
    public void Add(DiscoveryModel entity);
}

public class DiscoveryRepository : IDiscoveryRepository
{
    private readonly Dictionary<string, DiscoveryModel> Discoveries = [];

    public DiscoveryModel Find(string name, string version)
    {
        var key = CreateDiscoveryKey(name, version);

        if (!Discoveries.TryGetValue(key, out var discovery))
        {
            throw new DiscoveryNotRegisteredException();
        }

        return discovery;
    }

    public void Add(DiscoveryModel entity)
    {
        var key = CreateDiscoveryKey(entity.Name, entity.Version);

        Discoveries.TryAdd(key, entity);
    }

    private string CreateDiscoveryKey(string name, string version) => $"{name}-{version}";
}
