namespace SimPle.Application.Auth.DTOs;

public sealed record RegisterRequestDto(
    string Username,
    string Email,
    string Password,
    string ConfirmPassword,
    string CaptchaToken = "");
