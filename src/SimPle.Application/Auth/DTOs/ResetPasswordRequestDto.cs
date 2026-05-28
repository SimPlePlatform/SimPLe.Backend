namespace SimPle.Application.Auth.DTOs;

public sealed record ResetPasswordRequestDto(string Token, string NewPassword, string ConfirmNewPassword);
