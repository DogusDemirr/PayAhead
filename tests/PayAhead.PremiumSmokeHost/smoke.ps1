$ErrorActionPreference='Stop'
$OutputEncoding=[System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding=[System.Text.UTF8Encoding]::new($false)
$workspace=(Get-Location).Path
$env:PATH="$env:LOCALAPPDATA\Programs\DockerDesktop\resources\bin;$env:PATH"
$env:DOTNET_CLI_HOME="$workspace\.tools\cli"
$env:DOTNET_ROOT="$workspace\.tools\dotnet"
$dotnetPath="$workspace\.tools\dotnet\dotnet.exe"
$config=Get-Content .env -Raw | ConvertFrom-StringData
$databaseName='PayAheadSmoke_'+[Guid]::NewGuid().ToString('N')
$userId=[Guid]::NewGuid().ToString()
$env:ConnectionStrings__PayAhead="Server=127.0.0.1,$($config.PAYAHEAD_SQL_PORT);Database=$databaseName;User Id=sa;Password=$($config.PAYAHEAD_SQL_PASSWORD);Encrypt=True;TrustServerCertificate=True"
$port=5091
while (Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue) { $port++ }
$base="http://localhost:$port"
$apiProcess=$null
function Run-SqlFile($sql,$target,$suffix) {
    $path="$workspace\.tools\$databaseName-$suffix.sql"
    [IO.File]::WriteAllText($path,$sql,[System.Text.UTF8Encoding]::new($false))
    docker compose --env-file .env -f docker/compose.yml cp $path "sqlserver:/tmp/$databaseName-$suffix.sql"
    if ($LASTEXITCODE -ne 0) { throw 'SQL file copy failed' }
    docker compose --env-file .env -f docker/compose.yml exec -T sqlserver bash -c "SQLCMDPASSWORD=`"`$MSSQL_SA_PASSWORD`" /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -d $target -b -i /tmp/$databaseName-$suffix.sql"
    if ($LASTEXITCODE -ne 0) { throw 'SQL file failed' }
}
function Request($method,$path,$body,$token,$expected) {
    $params=@{UseBasicParsing=$true;Method=$method;Uri="$base$path";ContentType='application/json'}
    if($body) { $params.Body=$body|ConvertTo-Json -Compress }
    if($token) { $params.Headers=@{Authorization="Bearer $token"} }
    $status=0; $response=$null
    try { $response=Invoke-WebRequest @params; $status=[int]$response.StatusCode }
    catch { if($_.Exception.Response) { $status=[int]$_.Exception.Response.StatusCode } else { throw } }
    if($status -ne $expected) { throw "Unexpected status for $method $path : $status expected $expected" }
    Write-Output "$method $path = $status" | Out-Host
    if($response -and $response.Content) { return ($response.Content|ConvertFrom-Json) }
}
function Random-Key {
    $bytes=New-Object byte[] 32;$rng=[Security.Cryptography.RandomNumberGenerator]::Create();$rng.GetBytes($bytes);$rng.Dispose();return [Convert]::ToBase64String($bytes)
}
function Base64Url($bytes) { return [Convert]::ToBase64String($bytes).TrimEnd('=').Replace('+','-').Replace('/','_') }
function Proof($owner,$revision,$state,$expires,$eventId) {
    $json=@{userId=$owner;productId='test.premium';originalId='smoke-membership';transactionId="transaction-$revision";expiresAtUtc=$expires.ToString('o');state=$state;revision=$revision;eventId=$eventId;autoRenew=$false;store='Google'}|ConvertTo-Json -Compress
    $body=Base64Url ([Text.Encoding]::UTF8.GetBytes($json))
    $hmac=New-Object Security.Cryptography.HMACSHA256;$hmac.Key=[Convert]::FromBase64String($env:PAYAHEAD_TEST_STORE_KEY)
    try { return $body+'.'+(Base64Url ($hmac.ComputeHash([Text.Encoding]::ASCII.GetBytes($body)))) } finally {$hmac.Dispose()}
}
try {
    & $dotnetPath ef database update --project src/PayAhead.Infrastructure --startup-project src/PayAhead.Api --no-build
    if($LASTEXITCODE -ne 0) { throw 'Migration failed' }
    $env:ASPNETCORE_ENVIRONMENT='Testing';$env:DevelopmentIdentity__Enabled='false';$env:Notifications__Enabled='false'
    $env:Jwt__SigningKey=Random-Key;$env:Stores__EncryptionKeys__v1=Random-Key;$env:PAYAHEAD_TEST_STORE_KEY=Random-Key
    $env:Stores__WorkerEnabled='true';$env:PAYAHEAD_TEST_PORT="$port";$base="http://127.0.0.1:$port"
    $dll="$workspace\tests\PayAhead.PremiumSmokeHost\bin\Debug\net8.0\PayAhead.PremiumSmokeHost.dll"
    $apiProcess=Start-Process -FilePath $dotnetPath -ArgumentList @("`"$dll`"") -WorkingDirectory "$workspace\src\PayAhead.Api" -WindowStyle Hidden -PassThru -RedirectStandardOutput "$workspace\.tools\phase9-api.out.log" -RedirectStandardError "$workspace\.tools\phase9-api.err.log"
    $ready=$false
    for($attempt=0;$attempt -lt 60;$attempt++){try{$null=Invoke-WebRequest -UseBasicParsing "$base/health/ready";$ready=$true;break}catch{Start-Sleep -Milliseconds 250}}
    if(!$ready){throw 'API readiness failed'}
    $null=Request GET '/health/ready' $null $null 200
    $null=Request GET '/api/premium/me' $null $null 401
    $account=@{email='phase9-first@example.com';password='SmokeExamplePassword123!';displayName='Smoke'}
    $null=Request POST '/api/auth/register' $account $null 201
    $first=Request POST '/api/auth/login' @{email=$account.email;password=$account.password} $null 200
    $free=Request GET '/api/premium/me' $null $first.accessToken 200
    if($free.isPremium -or $free.limits.activePaymentPlans -ne 5){throw 'Free mismatch'}
    $plan=@{name='Smoke bill';amount=1.2345;currencyCode='TRY';category='Bills';frequency='Monthly';interval=1;startDate=[DateTime]::UtcNow.ToString('yyyy-MM-dd');isActive=$true}
    for($i=0;$i -lt 5;$i++){$null=Request POST '/api/payments' $plan $first.accessToken 201}
    $null=Request POST '/api/payments' $plan $first.accessToken 403
    $evidence=Proof $free.appAccountToken 1 Active ([DateTime]::UtcNow.AddSeconds(8)) 'initial'
    $purchase=@{store='Google';productId='test.premium';evidence=$evidence}
    $premium=Request POST '/api/premium/verify-purchase' $purchase $first.accessToken 200
    if(!$premium.isPremium -or $premium.limits.activePaymentPlans -ne 1000){throw 'Premium mismatch'}
    $null=Request POST '/api/payments' $plan $first.accessToken 201
    Start-Sleep -Seconds 9
    $expired=Request GET '/api/premium/entitlements' $null $first.accessToken 200
    if($expired.isPremium -or $expired.state -ne 'Expired'){throw 'Expiration mismatch'}
    $null=Request POST '/api/payments' $plan $first.accessToken 403
    $restored=Proof $free.appAccountToken 2 Active ([DateTime]::UtcNow.AddDays(1)) 'restore'
    $purchase.evidence=$restored
    if(!(Request POST '/api/premium/restore' $purchase $first.accessToken 200).isPremium){throw 'Restore mismatch'}
    $account.email='phase9-second@example.com';$null=Request POST '/api/auth/register' $account $null 201
    $second=Request POST '/api/auth/login' @{email=$account.email;password=$account.password} $null 200
    $null=Request POST '/api/premium/restore' $purchase $second.accessToken 409
    $revoked=Proof $free.appAccountToken 3 Revoked ([DateTime]::UtcNow.AddDays(1)) 'refund-event'
    for($i=0;$i -lt 2;$i++){$null=Request POST '/api/webhooks/google/subscriptions' @{signedPayload=$revoked} $null 202}
    $done=$false
    for($i=0;$i -lt 30;$i++){Start-Sleep -Milliseconds 500;$result=Request GET '/api/premium/me' $null $first.accessToken 200;if($result.state -eq 'Revoked' -and !$result.isPremium){$done=$true;break}}
    if(!$done){throw 'Webhook reconciliation failed'}
    Run-SqlFile "IF (SELECT COUNT(*) FROM StoreWebhookInbox)<>1 THROW 50000,'Replay duplicate',1; IF (SELECT COUNT(*) FROM StoreWebhookInbox WHERE CompletedAtUtc IS NOT NULL)<>1 THROW 50000,'Inbox incomplete',1; IF (SELECT COUNT(*) FROM PaymentPlans)<>6 THROW 50000,'History lost',1; SELECT 'Replay=1, completed=1, payment plans preserved=6' AS Result;" $databaseName 'assert'
    $null=Request GET '/swagger/v1/swagger.json' $null $null 200
    Write-Output 'Phase 9 Kestrel + real SQL + JWT: Free/Premium limits, expiration, restore, cross-user binding, replay and data preservation passed. Test provider only; no real store purchase.'
}
finally {
    if($apiProcess -and !$apiProcess.HasExited){Stop-Process -Id $apiProcess.Id}
    if($databaseName -notmatch '^PayAheadSmoke_[a-f0-9]{32}$'){throw 'Unsafe cleanup target'}
    Run-SqlFile "IF DB_ID('$databaseName') IS NOT NULL BEGIN ALTER DATABASE [$databaseName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$databaseName]; END" 'master' 'cleanup'
}
