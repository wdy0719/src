function Test-Installed ($productName){
    $allInstalled = Get-ItemProperty HKLM:\Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\* | select DisplayName
    return  [bool](($allInstalled | where {$_.DisplayName -like $productName}).Length -ne 0);
}

if(!(Test-installed "Microsoft Visual Studio Team Foundation Server 2013 Power Tools *")){
    Start-Process "\\mlcbld\cfg\Tools\Visual Studio Team Foundation Server 2013 Update 2 Power Tools .msi" -ArgumentList "-passive"
}

add-pssnapin Microsoft.TeamFoundation.PowerShell
[void][Reflection.Assembly]::LoadWithPartialName('Microsoft.TeamFoundation.Build.Client')
[void][Reflection.Assembly]::LoadWithPartialName('Microsoft.TeamFoundation.Client')

$buildUri = "$Env:TF_BUILD_BUILDURI";
$binFolder = "$Env:TF_BUILD_BINARIESDIRECTORY";
$srcFolder = $Env:TF_BUILD_SOURCESDIRECTORY
$tfsServer = "https://vstfskype.europe.corp.microsoft.com/tfs/skype";
$tfs = Get-TfsServer $tfsServer;
$buildServer = $tfs.GetService([Microsoft.TeamFoundation.Build.Client.IBuildServer]);

#$rbBuildDefinition = $buildserver.GetBuildDefinition("MLC", "MLC-Skypecast-DailyBuild")
#$build = $buildserver.GetBuild([Uri]($rbBuildDefinition.LastGoodBuildUri));
$curBuild = $buildserver.GetBuild([Uri]($buildUri));

if ($curBuild -ne "Succeeded"){
    $buildLog = $curBuild.LogLocation
    Write-Output "[$(Get-Date -f 'g')]: Build Error.  See log: $buildLog" | Out-File "$binFolder\error.log"
    Write-Error "Build Failed!"
}

# copy published files to bin folder
function CopyBuildOutput(){
    
    $ccprojs = (Get-ChildItem -Path $srcFolder -Recurse | where {$_.Name -like "*.ccproj"} | select FullName)
    
    foreach( $ccproj in $ccprojs){

        $ccprojLocation = (Get-Item "$ccproj").Directory.FullName;

        foreach($ver in "Debug","Release"){
            $targetDir = "$binFolder\$ver\$ccprojName\app.publish"
            if(!(Test-Path -Path $targetDir)){
                New-Item $targetDir -ItemType directory
            }
            Copy-Item "$ccprojLocation\bin\Debug\app.publish\*.cspkg" -Destination $targetDir -Force
            Copy-Item "$ccprojLocation\*.cscfg" -Destination $targetDir -Force
            Copy-Item "$ccprojLocation\Profiles\*.azurePubxml" -Destination $targetDir -Force
        }
    }
}
CopyBuildOutput