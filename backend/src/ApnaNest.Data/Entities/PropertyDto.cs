namespace ApnaNest.Data.Entities;

/// <summary>
/// Enriched property response — includes city name, locality name, images and amenities
/// sourced from the vw_properties view (JOIN with cities, localities, users, property_images).
/// </summary>
public class PropertyDto
{
    // ── Core IDs ────────────────────────────────────────────
    public Guid   Id          { get; set; }
    public Guid   OwnerId     { get; set; }
    public Guid   CityId      { get; set; }
    public Guid   LocalityId  { get; set; }

    // ── Location ────────────────────────────────────────────
    public string?  AddressLine { get; set; }
    public decimal? Latitude    { get; set; }
    public decimal? Longitude   { get; set; }
    public string?  PinCode     { get; set; }

    // ── Classification ──────────────────────────────────────
    public short PropertyTypeId  { get; set; }
    public short ListingIntentId { get; set; }

    // ── Specs ───────────────────────────────────────────────
    public string  Title           { get; set; } = string.Empty;
    public string? Description     { get; set; }
    public short?  Bhk             { get; set; }
    public short?  Bathrooms       { get; set; }
    public decimal AreaSqft        { get; set; }
    public short?  FloorNumber     { get; set; }
    public short?  TotalFloors     { get; set; }
    public short?  AgeYears        { get; set; }

    // ── Pricing ─────────────────────────────────────────────
    public decimal  Price               { get; set; }
    public bool     IsPriceNegotiable   { get; set; }
    public decimal? MaintenancePerMonth { get; set; }

    // ── Condition ───────────────────────────────────────────
    public short?  FurnishingStatusId  { get; set; }
    public short?  PossessionStatusId  { get; set; }
    public DateOnly? AvailableFrom     { get; set; }

    // ── Flags ───────────────────────────────────────────────
    public bool   IsReraVerified  { get; set; }
    public string? ReraNumber     { get; set; }
    public bool   IsZeroBrokerage { get; set; }
    public bool   IsFeatured      { get; set; }
    public bool   IsActive        { get; set; }
    public int    ViewCount       { get; set; }
    public int    LeadCount       { get; set; }

    // ── Timestamps ──────────────────────────────────────────
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // ── Enriched (from JOINs) ───────────────────────────────
    public string  CityName      { get; set; } = string.Empty;
    public string  CitySlug      { get; set; } = string.Empty;
    public string  LocalityName  { get; set; } = string.Empty;
    public string  LocalitySlug  { get; set; } = string.Empty;
    public string? OwnerName     { get; set; }
    public string? OwnerPhone    { get; set; }
    public string? OwnerEmail    { get; set; }

    /// <summary>Ordered image URLs from property_images table.</summary>
    public string[] Images    { get; set; } = Array.Empty<string>();

    /// <summary>Amenity labels from property_amenities JOIN amenities.</summary>
    public string[] Amenities { get; set; } = Array.Empty<string>();

    // ── Computed display helpers (for frontend compatibility) ─
    public string PropertyTypeName => PropertyTypeId switch {
        1 => "Apartment",
        2 => "Villa",
        3 => "Plot",
        4 => "PG",
        5 => "Commercial",
        6 => "Studio",
        7 => "Builder Floor",
        _ => "Property"
    };

    public string ListingIntentName => ListingIntentId switch {
        1 => "Buy",
        2 => "Rent",
        3 => "PG",
        _ => "Rent"
    };

    public string FurnishingLabel => FurnishingStatusId switch {
        1 => "Unfurnished",
        2 => "Semi Furnished",
        3 => "Fully Furnished",
        _ => "Unfurnished"
    };

    public string PossessionLabel => PossessionStatusId switch {
        1 => "Ready to Move",
        2 => "Under Construction",
        3 => "New Launch",
        _ => "Ready to Move"
    };
}
