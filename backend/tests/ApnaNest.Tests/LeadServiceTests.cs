using ApnaNest.Data.Entities;
using ApnaNest.Data.Repositories;
using ApnaNest.Services.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace ApnaNest.Tests;

public class LeadServiceTests
{
    private readonly Mock<ILeadRepository> _leadRepositoryMock;
    private readonly LeadService _sut;

    public LeadServiceTests()
    {
        _leadRepositoryMock = new Mock<ILeadRepository>();
        _sut = new LeadService(_leadRepositoryMock.Object);
    }

    [Fact]
    public async Task GetLeadsForPropertyAsync_ShouldReturnLeadsForProperty()
    {
        // Arrange
        var propertyId = Guid.NewGuid();
        var leads = new List<Lead>
        {
            new Lead { Id = Guid.NewGuid(), PropertyId = propertyId, BuyerName = "Buyer 1" },
            new Lead { Id = Guid.NewGuid(), PropertyId = propertyId, BuyerName = "Buyer 2" }
        };
        _leadRepositoryMock.Setup(x => x.GetByPropertyIdAsync(propertyId)).ReturnsAsync(leads);

        // Act
        var result = await _sut.GetLeadsForPropertyAsync(propertyId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(leads);
    }

    [Fact]
    public async Task SubmitLeadAsync_ShouldReturnLeadId()
    {
        // Arrange
        var newLeadId = Guid.NewGuid();
        var lead = new Lead { PropertyId = Guid.NewGuid(), BuyerName = "Test Buyer" };
        _leadRepositoryMock.Setup(x => x.AddAsync(lead)).ReturnsAsync(newLeadId);

        // Act
        var result = await _sut.SubmitLeadAsync(lead);

        // Assert
        result.Should().Be(newLeadId);
    }
}
