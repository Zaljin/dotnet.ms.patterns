namespace Discovery;

public interface IDiscoveryService
{
    void AddDiscovery(DiscoveryModel item);
    DiscoveryModel GetDiscovery(string name, string version);
}

public class DiscoveryService : IDiscoveryService
{
    private readonly IDiscoveryRepository _repository;

    public DiscoveryService(IDiscoveryRepository repository)
    {
        _repository = repository;
    }

    public DiscoveryModel GetDiscovery(string name, string version)
    {
        return _repository.Find(name, version);
    }

    public void AddDiscovery(DiscoveryModel item)
    {
        item.LastUpdateTimestamp = DateTime.UtcNow;
        _repository.Add(item);
    }
}