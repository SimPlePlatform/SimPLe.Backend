namespace SimPle.Application.Profiles.DTOs;

public sealed record UpdateLinksRequestDto(IReadOnlyList<LinkItemDto> Links);

public sealed record LinkItemDto(
    string Platform,
    string Url,
    string? DisplayLabel,
    int SortOrder);
