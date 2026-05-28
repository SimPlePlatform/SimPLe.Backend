namespace SimPle.Application.Common.Options;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string From { get; init; } = string.Empty;
    public string FromName { get; init; } = "SimPle";
    public string SmtpHost { get; init; } = string.Empty;
    public int SmtpPort { get; init; } = 587;
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string AppUrl { get; init; } = "http://localhost:3000";
}
