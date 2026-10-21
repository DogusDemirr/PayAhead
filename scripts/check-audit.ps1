param([Parameter(Mandatory=$true)][string]$ReportPath)
$ErrorActionPreference='Stop'
$report=Get-Content -LiteralPath $ReportPath -Raw -Encoding UTF8 | ConvertFrom-Json
if($report.version -ne 1 -or !$report.projects){throw 'Invalid or incomplete NuGet audit report'}
$findings=@()
if($report.problems){$findings+='NuGet audit reported problems'}
foreach($project in $report.projects){
    foreach($framework in $project.frameworks){
        foreach($package in @($framework.topLevelPackages)+@($framework.transitivePackages)){
            foreach($advisory in $package.vulnerabilities){$findings+="$($package.id): $($advisory.severity) $($advisory.advisoryurl)"}
        }
    }
}
if($findings.Count -gt 0){$findings|Write-Output;exit 1}
Write-Output 'NuGet audit: no reported vulnerabilities'
