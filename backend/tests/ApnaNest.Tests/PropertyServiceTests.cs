using ApnaNest.Data.Entities;
using ApnaNest.Data.Repositories;
using ApnaNest.Services.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace ApnaNest.Tests;

public class PropertyServiceTests
{
    private readonly Mock<IPropertyRepository> _repoMock;
    private readonly PropertyService _sut;

    public PropertyServiceTests()
    {
        _repoMock = new Mock<IPropertyRepository>();
        _sut = new PropertyService(_repoMock.Object);
    }

    [Fact]
    public async Task GetAllPropertiesAsync_ShouldReturnAllProperties()
    {
        var dtos = new List<PropertyDto>
        {
            new() { Id = Guid.NewGuid(), Title = "Property 1", AreaSqft = 1000, Price = 5000000 },
            new() { Id = Guid.NewGuid(), Title = "Property 2", AreaSqft = 800,  Price = 3500000 },
        };
        _repoMock.Setup(x => x.GetAllAsync(null, null, null, 1, 20)).ReturnsAsync(dtos);

        var result = await _sut.GetAllPropertiesAsync();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPropertyByIdAsync_ShouldReturnProperty_WhenPropertyExists()
    {
        var id  = Guid.NewGuid();
        var dto = new PropertyDto { Id = id, Title = "Property 1", AreaSqft = 1000, Price = 5000000 };
        _repoMock.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(dto);

        var result = await _sut.GetPropertyByIdAsync(id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
    }

    [Fact]
    public async Task GetPropertyByIdAsync_ShouldReturnNull_WhenPropertyDoesNotExist()
    {
        _repoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((PropertyDto?)null);

        var result = await _sut.GetPropertyByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }
}
