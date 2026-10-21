$ErrorActionPreference='Stop'
$OutputEncoding=[Text.UTF8Encoding]::new($false)
$workspace=(Get-Location).Path
$env:PATH="$env:LOCALAPPDATA\Programs\DockerDesktop\resources\bin;$env:PATH"
$env:DOTNET_CLI_HOME="$workspace\.tools\cli";$env:DOTNET_ROOT="$workspace\.tools\dotnet"
$dotnet="$workspace\.tools\dotnet\dotnet.exe"
$config=Get-Content .env -Raw | ConvertFrom-StringData
$suffix=[Guid]::NewGuid().ToString('N')
$database='PayAheadSmoke_'+$suffix;$restore='PayAheadRestore_'+$suffix;$container='payahead-phase10-'+$suffix
$envFile="$workspace\.tools\phase10-$suffix.env"
$env:ConnectionStrings__PayAhead="Server=127.0.0.1,$($config.PAYAHEAD_SQL_PORT);Database=$database;User Id=sa;Password=$($config.PAYAHEAD_SQL_PASSWORD);Encrypt=True;TrustServerCertificate=True"
$port=5097;while(Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue){$port++}
function Sql($query,$target,$step){
    $file="$workspace\.tools\$database-$step.sql"
    [IO.File]::WriteAllText($file,$query,[Text.UTF8Encoding]::new($false))
    docker compose --env-file .env -f docker/compose.yml cp $file "sqlserver:/tmp/$database-$step.sql" | Out-Null
    if($LASTEXITCODE -ne 0){throw 'SQL copy failed'}
    docker compose --env-file .env -f docker/compose.yml exec -T sqlserver bash -c "SQLCMDPASSWORD=`"`$MSSQL_SA_PASSWORD`" /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -d $target -b -i /tmp/$database-$step.sql"
    if($LASTEXITCODE -ne 0){throw 'SQL command failed'}
}
function Request($method,$path,$body,$token,$expected){
    $args=@{UseBasicParsing=$true;Method=$method;Uri="http://127.0.0.1:$port$path";ContentType='application/json';Headers=@{'X-Forwarded-Proto'='https'}}
    if($body){$args.Body=$body|ConvertTo-Json -Compress};if($token){$args.Headers.Authorization="Bearer $token"}
    $response=$null;$status=0
    try{$response=Invoke-WebRequest @args;$status=[int]$response.StatusCode}catch{if($_.Exception.Response){$status=[int]$_.Exception.Response.StatusCode}else{throw}}
    if($status -ne $expected){throw "$method $path status $status expected $expected"}
    Write-Host "$method $path = $status"
    if($response -and $response.Content){return $response.Content|ConvertFrom-Json}
}
try{
    & $dotnet ef database update --project src/PayAhead.Infrastructure --startup-project src/PayAhead.Api --no-build
    if($LASTEXITCODE -ne 0){throw 'Migration failed'}
    $gateway=docker network inspect payahead-phase1_default --format '{{(index .IPAM.Config 0).Gateway}}'
    if($LASTEXITCODE -ne 0){throw 'Development SQL network not found'}
    $key=New-Object byte[] 64;$rng=[Security.Cryptography.RandomNumberGenerator]::Create();$rng.GetBytes($key);$rng.Dispose()
    $content=@("ASPNETCORE_ENVIRONMENT=Testing","ConnectionStrings__PayAhead=Server=sqlserver,1433;Database=$database;User Id=sa;Password=$($config.PAYAHEAD_SQL_PASSWORD);Encrypt=True;TrustServerCertificate=True","Jwt__SigningKey=$([Convert]::ToBase64String($key))","TrustedProxies__0=$gateway","Maintenance__Enabled=false","AllowedHosts=127.0.0.1;localhost") -join "`n"
    [IO.File]::WriteAllText($envFile,$content,[Text.UTF8Encoding]::new($false))
    docker run -d --name $container --network payahead-phase1_default --read-only --tmpfs /tmp:rw,nosuid,size=64m --cap-drop ALL --security-opt no-new-privileges --memory 768m --cpus 2 --pids-limit 128 --init --env-file $envFile -p "127.0.0.1:${port}:8080" payahead:phase10-local | Out-Null
    if($LASTEXITCODE -ne 0){throw 'Container startup failed'}
    $ready=$false
    for($i=0;$i -lt 90;$i++){try{$null=Invoke-WebRequest -UseBasicParsing "http://127.0.0.1:$port/health/ready";$ready=$true;break}catch{Start-Sleep -Milliseconds 500}}
    if(!$ready){docker logs --tail 20 $container;throw 'Readiness failed'}
    $null=Request GET /health/live $null $null 200;$null=Request GET /health/ready $null $null 200
    $null=Request GET /swagger/v1/swagger.json $null $null 401
    $null=Request GET /api/users/me/export $null $null 401
    $account=@{email='container@test.invalid';password='ContainerTestPassphrase123!';displayName='Container'}
    $null=Request POST /api/auth/register $account $null 201
    $login=Request POST /api/auth/login @{email=$account.email;password=$account.password} $null 200
    $null=Request GET /swagger/v1/swagger.json $null $login.accessToken 404
    $null=Request GET /api/users/me/export $null $login.accessToken 200
    $null=Request POST /api/premium/verify-purchase @{store='Google';productId='unconfigured';evidence='invalid'} $login.accessToken 503
    Sql "BACKUP DATABASE [$database] TO DISK=N'/var/opt/mssql/data/$database.bak' WITH COPY_ONLY,INIT,CHECKSUM; RESTORE VERIFYONLY FROM DISK=N'/var/opt/mssql/data/$database.bak' WITH CHECKSUM; RESTORE DATABASE [$restore] FROM DISK=N'/var/opt/mssql/data/$database.bak' WITH MOVE N'$database' TO N'/var/opt/mssql/data/$restore.mdf', MOVE N'${database}_log' TO N'/var/opt/mssql/data/${restore}_log.ldf',CHECKSUM;" master backup
    Sql "IF (SELECT COUNT(*) FROM Users)<>1 THROW 50000,'Restored data mismatch',1; IF (SELECT COUNT(*) FROM __EFMigrationsHistory)<>6 THROW 50000,'Restored migration mismatch',1; DBCC CHECKDB WITH NO_INFOMSGS; SELECT 'Local isolated backup/restore passed' AS Result;" $restore restorecheck
    $null=Request DELETE /api/users/me @{currentPassword=$account.password} $login.accessToken 204
    $null=Request GET /api/users/me $null $login.accessToken 401
    docker exec $container dotnet PayAhead.Api.dll --health-probe
    if($LASTEXITCODE -ne 0){throw 'Image health probe failed'}
    docker exec $container id -u
    docker inspect --format '{{.HostConfig.ReadonlyRootfs}} {{.Config.User}}' $container
    docker stop --time 35 $container | Out-Null
    docker inspect --format '{{.State.ExitCode}}' $container
    Write-Output 'Container smoke passed: Testing env, real SQL/JWT, non-root/read-only/resources, health, no Swagger/fake fallback, export/deletion, isolated backup/restore, graceful shutdown. No production deployment.'
}finally{
    if($container -notmatch '^payahead-phase10-[a-f0-9]{32}$' -or $database -notmatch '^PayAheadSmoke_[a-f0-9]{32}$' -or $restore -notmatch '^PayAheadRestore_[a-f0-9]{32}$'){throw 'Unsafe cleanup target'}
    $existing=docker ps -aq --filter "name=^/$container$"
    if($existing){docker rm -f $container | Out-Null}
    Sql "IF DB_ID('$restore') IS NOT NULL BEGIN ALTER DATABASE [$restore] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$restore]; END; IF DB_ID('$database') IS NOT NULL BEGIN ALTER DATABASE [$database] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$database]; END;" master cleanup
    if([IO.Path]::GetFullPath($envFile).StartsWith("$workspace\.tools\",[StringComparison]::OrdinalIgnoreCase)){Remove-Item -LiteralPath $envFile -ErrorAction SilentlyContinue}
}
