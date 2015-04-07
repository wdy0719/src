Param(
    [string]$proj,
    [string]$ver,
    [string]$env,
    [string]$buildLocation = $Env:BuildLocation,
    [string]$subscriptionDataFile = "\\mlcbld\cfg\BuildScripts\TstInt.publishsettings"
)

function WriteLog($message,[switch]$createFile=$false){

    $logFile = "$buildLocation\LastDeploy_${proj}_${env}.log";
    if($createFile){
        New-Item $logFile -ItemType file -Force;
    }

    Write-Output "[$(Get-Date -f 'g')]: $message" | Out-File -Append $logFile;
}

$ErrorActionPreference = "stop";

try
    {
    WriteLog "Start." -createFile 

    $publishFolder = "$buildLocation\x64\$ver\app.publish\$proj";

    $packageLocation = "$publishFolder\${proj}.cspkg"
    $cloudConfigLocation = ((Get-ChildItem -Path $publishFolder) | where {$_.Name -like "ServiceConfiguration.Cloud.$env.cscfg"} | Get-Unique).FullName
    $azurePubXmlFile = ((Get-ChildItem -Path "$publishFolder") | where {$_.Name -like "$env*.azurePubxml"} | Get-Unique).FullName

    \\mlcbld\cfg\BuildScripts\AzurePublish2.3.ps1 -packageLocation $packageLocation -cloudConfigLocation $cloudConfigLocation -azurePubXmlFile $azurePubXmlFile -subscriptionDataFile $subscriptionDataFile;
    
    } 
catch [Exception]
    {
        WriteLog "Error:  "+ $_.Exception.ToString();
        throw
    }
finally 
    {
        WriteLog "End."
    }