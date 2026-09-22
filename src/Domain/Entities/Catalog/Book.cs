using Domain.Bases;
using Domain.Enums;
using Domain.ValueObjects;

namespace Domain.Entities.Catalog;

public sealed class Book : Entity
{
    public string Title { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public Money Price { get; init; } = new(0, "BRL");
    public BookFormat Format { get; init; }
}
