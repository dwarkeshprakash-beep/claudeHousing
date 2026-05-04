using ApnaNest.Data.Entities;
using ApnaNest.Data.Repositories;
using ApnaNest.Services.Interfaces;

namespace ApnaNest.Services.Services;

public class LeadService : ILeadService
{
    private readonly ILeadRepository _leadRepository;

    public LeadService(ILeadRepository leadRepository)
    {
        _leadRepository = leadRepository;
    }

    public Task<IEnumerable<Lead>> GetLeadsForPropertyAsync(Guid propertyId)
    {
        return _leadRepository.GetByPropertyIdAsync(propertyId);
    }

    public Task<IEnumerable<Lead>> GetLeadsForOwnerAsync(Guid ownerId)
        => _leadRepository.GetByOwnerIdAsync(ownerId);

    public Task<IEnumerable<Lead>> GetAllLeadsAsync()
        => _leadRepository.GetAllAsync();

    public Task<Guid> SubmitLeadAsync(Lead lead)
        => _leadRepository.AddAsync(lead);
}
