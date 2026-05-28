namespace SimPle.Application.Auth.DTOs;

public sealed record ChangePasswordRequestDto(
    string CurrentPassword,
    string NewPassword,
    string ConfirmNewPassword);
