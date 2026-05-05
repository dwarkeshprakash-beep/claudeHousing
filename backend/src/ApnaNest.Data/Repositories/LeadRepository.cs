using ApnaNest.Data.Entities;
using ApnaNest.Data.Factories;
using Dapper;

namespace ApnaNest.Data.Repositories;

public class LeadRepository : ILeadRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public LeadRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<Lead>> GetByPropertyIdAsync(Guid propertyId)
    {
        const string sql = "SELECT * FROM leads WHERE property_id = @propertyId ORDER BY created_at DESC";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Lead>(sql, new { propertyId });
    }

    public async Task<IEnumerable<Lead>> GetByOwnerIdAsync(Guid ownerId)
    {
        const string sql = @"
            SELECT l.*, p.title as PropertyTitle FROM leads l
            INNER JOIN properties p ON p.id = l.property_id
            WHERE p.owner_id = @ownerId
            ORDER BY l.created_at DESC";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Lead>(sql, new { ownerId });
    }

    public async Task<IEnumerable<Lead>> GetAllAsync()
    {
        const string sql = "SELECT * FROM leads ORDER BY created_at DESC";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Lead>(sql);
    }

    public async Task<Guid> AddAsync(Lead lead)
    {
        lead.Id = Guid.NewGuid();
        lead.CreatedAt = DateTime.UtcNow;
        lead.UpdatedAt = DateTime.UtcNow;

        using var connection = _connectionFactory.CreateConnection();
        
        // Fetch owner_id from property if not provided
        if (lead.OwnerId == Guid.Empty)
        {
            const string ownerSql = "SELECT owner_id FROM properties WHERE id = @PropertyId";
            lead.OwnerId = await connection.ExecuteScalarAsync<Guid>(ownerSql, new { lead.PropertyId });
        }

        const string sql = @"
            INSERT INTO leads (id, property_id, owner_id, buyer_name, buyer_phone, buyer_email, message, notes, status_id, created_at, updated_at)
            VALUES (@Id, @PropertyId, @OwnerId, @BuyerName, @BuyerPhone, @BuyerEmail, @Message, @Notes, @StatusId, @CreatedAt, @UpdatedAt)
            RETURNING id;";

        return await connection.ExecuteScalarAsync<Guid>(sql, lead);
    }

    public async Task<bool> UpdateStatusAsync(Guid id, short statusId)
    {
        const string sql = "UPDATE leads SET status_id = @statusId, updated_at = @now WHERE id = @id";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(sql, new { id, statusId, now = DateTime.UtcNow }) > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        const string sql = "DELETE FROM leads WHERE id = @id";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(sql, new { id }) > 0;
    }
}
