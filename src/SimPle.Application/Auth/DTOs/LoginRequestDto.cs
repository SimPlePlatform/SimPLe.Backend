namespace SimPle.Application.Auth.DTOs;

public sealed record LoginRequestDto(
    string EmailOrUsername,
    string Password,
    string CaptchaToken = "");
