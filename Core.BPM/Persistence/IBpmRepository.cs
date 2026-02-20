namespace Core.BPM.Persistence;

public interface IBpmRepository : IEventStoreRepository, IProcessRegistryRepository;
