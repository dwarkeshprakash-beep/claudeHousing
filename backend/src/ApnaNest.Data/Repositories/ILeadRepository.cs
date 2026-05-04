using ApnaNest.Data.Entities;

namespace ApnaNest.Data.Repositories;

public interface ILeadRepository
{
    Task<IEnumerable<Lead>> GetByPropertyIdAsync(Guid propertyId);
    Task<IEnumerable<Lead>> GetByOwnerIdAsync(Guid ownerId);
    Task<IEnumerable<Lead>> GetAllAsync();
    Task<Guid> AddAsync(Lead lead);
}
