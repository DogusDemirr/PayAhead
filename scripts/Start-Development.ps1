param(
    [int]$Port = 5080,
    [switch]$EmailCapture,
    [string]$ResetLinkBase
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$sdk = Join-Path $repo '.tools/dotnet/dotnet.exe'
if (!(Test-Path -LiteralPath $sdk)) { throw '.NET 8 SDK bulunamadı.' }
$env:DOTNET_CLI_HOME = Join-Path $repo '.tools/cli'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:DevelopmentIdentity__Enabled = 'false'
if ($Port -lt 1024 -or $Port -gt 65535) { throw 'Geçerli development portu girin.' }
if ($EmailCapture) {
    $link = $null
    if (![Uri]::TryCreate($ResetLinkBase, [UriKind]::Absolute, [ref]$link) -or $link.Scheme -ne 'exp' -or $link.Query -or $link.Fragment) {
        throw 'EmailCapture için Metro LAN adresini içeren -ResetLinkBase exp://IP:PORT/--/reset-password girin.'
    }
    $env:Email__Enabled = 'true'
    $env:Email__Host = '127.0.0.1'
    $env:Email__Port = '1025'
    $env:Email__EnableSsl = 'false'
    $env:Email__From = 'PayAhead <payahead@example.test>'
    $env:Email__Username = ''
    $env:Email__Password = ''
    $env:PasswordReset__LinkBase = $ResetLinkBase
    Write-Host 'Yerel SMTP yakalama seçildi; dış posta kutusuna teslim yapılmaz. Mailpit 1025 üzerinde açık olmalıdır.'
}
Push-Location $repo
try {
    # Always build current sources. Never run the stale src/bin binary with --no-build.
    & $sdk build src/PayAhead.Api/PayAhead.Api.csproj --artifacts-path artifacts/development
    if ($LASTEXITCODE -ne 0) { throw 'Development build başarısız.' }
    Write-Host 'Migration otomatik uygulanmaz. Readiness 503 ise hedef DB ve pending migration durumunu kontrol edin.'
    & $sdk artifacts/development/bin/PayAhead.Api/debug/PayAhead.Api.dll --contentRoot (Join-Path $repo 'src/PayAhead.Api') --urls "http://0.0.0.0:$Port"
    if ($LASTEXITCODE -ne 0) { throw 'Development API başlatılamadı. Portu kullanan eski API terminalini kontrollü kapatın.' }
} finally { Pop-Location }
