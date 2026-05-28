namespace SimPle.Application.Auth.DTOs;

/// <summary>The credential returned by Google Identity Services after the user consents.</summary>
public record GoogleCallbackRequestDto(string IdToken);
