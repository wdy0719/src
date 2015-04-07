Param(  
        $packageLocation="C:\blds\227\Sources\SkypeCast\Scheduler\Scheduler\bin\Debug\app.publish\Scheduler.cspkg",
        $cloudConfigLocation="C:\blds\227\Sources\SkypeCast\Scheduler\Scheduler\ServiceConfiguration.Cloud.Tst.cscfg",
        $azurePubXmlFile="C:\blds\227\Sources\SkypeCast\Scheduler\Scheduler\Profiles\tst01wusscscsvcProduction.azurePubxml",
        $subscriptionDataFile="\\mlcbld\cfg\BuildScripts\TstInt.publishsettings",
        $deploymentLabelSufix = "",
        [bool]$enableUpgradeDeploy = $true
     )

      
function Publish()
{
    $deployment = Get-AzureDeployment -ServiceName $serviceName -Slot $slot -ErrorVariable a -ErrorAction silentlycontinue 
    if ($a[0] -ne $null)
    {
        Write-Output "$(Get-Date -f $timeStampFormat) - No deployment is detected. Creating a new deployment. "
    }
    #check for existing deployment and then either upgrade, delete + deploy, or cancel according to $alwaysDeleteExistingDeployments and $enableDeploymentUpgrade boolean variables
    if ($deployment.Name -ne $null)
    {
        switch ($alwaysDeleteExistingDeployments)
        {
            1 
            {
                switch ($enableDeploymentUpgrade)
                {
                    1  #Update deployment inplace (usually faster, cheaper, won't destroy VIP)
                    {
                        Write-Output "$(Get-Date -f $timeStampFormat) - Deployment exists in $servicename.  Upgrading deployment."
                        UpgradeDeployment
                    }
                    0  #Delete then create new deployment
                    {
                        Write-Output "$(Get-Date -f $timeStampFormat) - Deployment exists in $servicename.  Deleting deployment."
                        DeleteDeployment
                        CreateNewDeployment
                        
                    }
                } # switch ($enableDeploymentUpgrade)
            }
            0
            {
                Write-Output "$(Get-Date -f $timeStampFormat) - ERROR: Deployment exists in $servicename.  Script execution cancelled."
                exit
            }
        } #switch ($alwaysDeleteExistingDeployments)
    } else {
            CreateNewDeployment
    }
}

function CreateNewDeployment()
{
    write-progress -id 3 -activity "Creating New Deployment" -Status "In progress"
    Write-Output "$(Get-Date -f $timeStampFormat) - Creating New Deployment: In progress"

    $opstat = New-AzureDeployment -Slot $slot -Package $packageLocation -Configuration $cloudConfigLocation -label $deploymentLabel -ServiceName $serviceName
        
    $completeDeployment = Get-AzureDeployment -ServiceName $serviceName -Slot $slot
    $completeDeploymentID = $completeDeployment.deploymentid

    write-progress -id 3 -activity "Creating New Deployment" -completed -Status "Complete"
    Write-Output "$(Get-Date -f $timeStampFormat) - Creating New Deployment: Complete, Deployment ID: $completeDeploymentID"
    
    StartInstances
}

function UpgradeDeployment()
{
    write-progress -id 3 -activity "Upgrading Deployment" -Status "In progress"
    Write-Output "$(Get-Date -f $timeStampFormat) - Upgrading Deployment: In progress"

    # perform Update-Deployment
    $setdeployment = Set-AzureDeployment -Upgrade -Slot $slot -Package $packageLocation -Configuration $cloudConfigLocation -label $deploymentLabel -ServiceName $serviceName -Force
    
    $completeDeployment = Get-AzureDeployment -ServiceName $serviceName -Slot $slot
    $completeDeploymentID = $completeDeployment.deploymentid
    
    write-progress -id 3 -activity "Upgrading Deployment" -completed -Status "Complete"
    Write-Output "$(Get-Date -f $timeStampFormat) - Upgrading Deployment: Complete, Deployment ID: $completeDeploymentID"
}

function DeleteDeployment()
{

    write-progress -id 2 -activity "Deleting Deployment" -Status "In progress"
    Write-Output "$(Get-Date -f $timeStampFormat) - Deleting Deployment: In progress"

    #WARNING - always deletes with force
    $removeDeployment = Remove-AzureDeployment -Slot $slot -ServiceName $serviceName -Force

    write-progress -id 2 -activity "Deleting Deployment: Complete" -completed -Status $removeDeployment
    Write-Output "$(Get-Date -f $timeStampFormat) - Deleting Deployment: Complete"
    
}

function StartInstances()
{
    write-progress -id 4 -activity "Starting Instances" -status "In progress"
    Write-Output "$(Get-Date -f $timeStampFormat) - Starting Instances: In progress"

    $deployment = Get-AzureDeployment -ServiceName $serviceName -Slot $slot
    $runstatus = $deployment.Status

    if ($runstatus -ne 'Running') 
    {
        $run = Set-AzureDeployment -Slot $slot -ServiceName $serviceName -Status Running
    }
    $deployment = Get-AzureDeployment -ServiceName $serviceName -Slot $slot
    $oldStatusStr = @("") * $deployment.RoleInstanceList.Count
    
    while (-not(AllInstancesRunning($deployment.RoleInstanceList)))
    {
        $i = 1
        foreach ($roleInstance in $deployment.RoleInstanceList)
        {
            $instanceName = $roleInstance.InstanceName
            $instanceStatus = $roleInstance.InstanceStatus

            if ($oldStatusStr[$i - 1] -ne $roleInstance.InstanceStatus)
            {
                $oldStatusStr[$i - 1] = $roleInstance.InstanceStatus
                Write-Output "$(Get-Date -f $timeStampFormat) - Starting Instance '$instanceName': $instanceStatus"
            }

            write-progress -id (4 + $i) -activity "Starting Instance '$instanceName'" -status "$instanceStatus"
            $i = $i + 1
        }

        sleep -Seconds 1

        $deployment = Get-AzureDeployment -ServiceName $serviceName -Slot $slot
    }

    $i = 1
    foreach ($roleInstance in $deployment.RoleInstanceList)
    {
        $instanceName = $roleInstance.InstanceName
        $instanceStatus = $roleInstance.InstanceStatus

        if ($oldStatusStr[$i - 1] -ne $roleInstance.InstanceStatus)
        {
            $oldStatusStr[$i - 1] = $roleInstance.InstanceStatus
            Write-Output "$(Get-Date -f $timeStampFormat) - Starting Instance '$instanceName': $instanceStatus"
        }

        $i = $i + 1
    }
    
    $deployment = Get-AzureDeployment -ServiceName $serviceName -Slot $slot
    $opstat = $deployment.Status 
    
    write-progress -id 4 -activity "Starting Instances" -completed -status $opstat
    Write-Output "$(Get-Date -f $timeStampFormat) - Starting Instances: $opstat"
}

function AllInstancesRunning($roleInstanceList)
{
    foreach ($roleInstance in $roleInstanceList)
    {
        if ($roleInstance.InstanceStatus -ne "ReadyRole")
        {
            return $false
        }
    }
    
    return $true
}

 function Write-StdErr {
        param ([PSObject] $InputObject)
        $outFunc = if ($Host.Name -eq 'ConsoleHost') { 
            [Console]::Error.WriteLine
        } else {
            $host.ui.WriteErrorLine
        }
        if ($InputObject) {
            [void] $outFunc.Invoke($InputObject.ToString())
        } else {
            [string[]] $lines = @()
            $Input | % { $lines += $_.ToString() }
            [void] $outFunc.Invoke($lines -join "`r`n")
        }
    }

#$ErrorActionPreference = "Stop";

try
{

[xml]$azurePubXml=Get-Content $azurePubXmlFile

$serviceName = $azurePubXml.Project.PropertyGroup.AzureHostedServiceName
$storageAccountName = $azurePubXml.Project.PropertyGroup.AzureStorageAccountName
$environment = $azurePubXml.Project.PropertyGroup.AzureSlot
$deploymentLabel = $azurePubXml.Project.PropertyGroup.AzureDeploymentLabel
$timeStampFormat = "g"
$alwaysDeleteExistingDeployments = 1
if($enableUpgradeDeploy){
    $enableDeploymentUpgrade = 1
} else{
    $enableDeploymentUpgrade = 0
}
$azureCredentials = ConvertFrom-Json($azurePubXml.Project.PropertyGroup.AzureCredentials)


#configure powershell with Azure modules
# Import-Module Azure
#Import-Module "${env:ProgramFiles(x86)}\Microsoft SDKs\Azure\PowerShell\ServiceManagement\Azure\Azure.psd1"
& "${env:ProgramFiles(x86)}\Microsoft SDKs\Azure\PowerShell\ServiceManagement\Azure\Services\ShortcutStartup.ps1"
Get-AzureSubscription | ForEach-Object { Remove-AzureSubscription $_.SubscriptionName -confirm:$false -Force }

#configure powershell with publishsettings for your subscription
Copy-Item $subscriptionDataFile -Destination $env:temp\tmp.publishsettings -Force
$pubsettings = "$env:temp\tmp.publishsettings"
Import-AzurePublishSettingsFile "$pubsettings"

Write-Output "$(Get-Date -f $timeStampFormat) - Set current storage account"
$selectedsubscription=(Get-AzureSubscription | where {$_.SubscriptionId -EQ $azureCredentials.SubscriptionId}).SubscriptionName
Set-AzureSubscription -CurrentStorageAccount $storageAccountName -SubscriptionName $selectedsubscription
Select-AzureSubscription $selectedsubscription

#set remaining environment variables for Azure cmdlets
$subscription = Get-AzureSubscription $selectedsubscription
$subscriptionname = $subscription.SubscriptionName
$subscriptionid = $subscription.SubscriptionId
$slot = $environment
$deploymentLabel=$deploymentLabel + (Get-Date -format "yyyy-MM-dd hh:mm:ss") + " " + $deploymentLabelSufix

#main driver - publish & write progress to activity log
Write-Output "$(Get-Date -f $timeStampFormat) - Azure Cloud Service deploy script started."
Write-Output "$(Get-Date -f $timeStampFormat) - Preparing deployment of $deploymentLabel for $subscriptionname with Subscription ID $subscriptionid."

Publish

$deployment = Get-AzureDeployment -slot $slot -serviceName $servicename
$deploymentUrl = $deployment.Url

Write-Output "$(Get-Date -f $timeStampFormat) - Created Cloud Service with URL $deploymentUrl."
Write-Output "$(Get-Date -f $timeStampFormat) - Azure Cloud Service deploy script finished."

}
catch
{
   Write-Error "Exec : error : $Error";
   Exit -1;
}

