param(
    [switch] $Force
)

$ErrorActionPreference = 'Stop'

$apiPath = Join-Path $PSScriptRoot '..\src\SimPle.Api'
$envPath = Join-Path $apiPath '.env'

if ((Test-Path -LiteralPath $envPath) -and -not $Force) {
    Write-Host "Local .env already exists at $envPath. Use -Force to replace it."
    exit 0
}

function New-RandomBytes([int] $Length) {
    $bytes = New-Object byte[] $Length
    $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
    try { $rng.GetBytes($bytes) }
    finally { $rng.Dispose() }
    return $bytes
}

$jwtSecret = [Convert]::ToBase64String((New-RandomBytes 48))
$dbPassword = ([BitConverter]::ToString((New-RandomBytes 24))).Replace('-', '')

$lines = @(
    '# Local secrets — never commit. Re-run this script with -Force to regenerate.'
    ''
    '# ─── Database (also read by compose.auth.yml) ─────────────────────────────────'
    "ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=simple_auth_dev;Username=simple_auth;Password=$dbPassword"
    'POSTGRES_DB=simple_auth_dev'
    'POSTGRES_USER=simple_auth'
    "POSTGRES_PASSWORD=$dbPassword"
    ''
    '# ─── JWT signing key (≥32 random chars) ───────────────────────────────────────'
    "Jwt__SecretKey=$jwtSecret"
    ''
    '# ─── reCAPTCHA — get the SECRET KEY from google.com/recaptcha/admin ───────────'
    'Recaptcha__SecretKey=REPLACE_WITH_RECAPTCHA_V2_SECRET_KEY'
    ''
    '# ─── Email — Gmail: myaccount.google.com → Security → App passwords ───────────'
    'Email__From=REPLACE_WITH_YOUR_SENDER_EMAIL'
    'Email__Username=REPLACE_WITH_YOUR_GMAIL_ADDRESS'
    'Email__Password=REPLACE_WITH_YOUR_APP_PASSWORD'
    ''
    '# ─── Google OAuth — console.cloud.google.com → APIs & Services → Credentials ──'
    'Google__ClientId=REPLACE_WITH_GOOGLE_OAUTH_CLIENT_ID'
)

[IO.File]::WriteAllLines((Resolve-Path -LiteralPath $apiPath).Path + '\.env', $lines)
Write-Host "Created $envPath — fill in the REPLACE_WITH_* placeholders before starting the app."
Write-Host 'Install Docker Desktop or PostgreSQL before applying the Auth migration.'
