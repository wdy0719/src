$ErrorActionPreference = "stop";

$SOURCESDIRECTORY = $Env:TF_BUILD_SOURCESDIRECTORY
$buildUri = "$Env:TF_BUILD_BUILDURI";
$buildNumber = "$env:TF_BUILD_BUILDNUMBER"; # MLCDaily_20141119.1
$yellowTailBuild = $buildNumber;

$subscriptionDataFile = "\\mlcbld\cfg\BuildScripts\TstInt.publishsettings"

#$settings_TFSURL = [URI]"https://vstfskype.europe.corp.microsoft.com/tfs/skype";
#$settings_TFSPROJ = "MLC";
$settings_testLogsLocation = "\\MlcBld\Logs\MLC-Skyepcast-Daily";
$settings_email_subject = "SkypeCast Automation Report [" + [DateTime]::Now.ToString("MM/dd/yyyy") + "]"
$settings_email_to = "v-wenlzh@microsoft.com","Vijay.Jayaraman@microsoft.com", "yuanji@microsoft.com", "SkypeMeetingLifeRV@microsoft.com"
$settings_email_user = "rajtest2@microsoft.com";
$settings_email_password = "N0Apples";
$settings_email_exchange = "outlook.office365.com";
$settings_email_template = "\\mlcbld\cfg\BuildScripts\MailTemplate.html";
#$settings_xsltFile = "\\mlcbld\cfg\BuildScripts\Trx2Html.xslt";

function DeployAzureService($ccprojName, $ver, $env , [bool]$enableUpgrade=$true){
    [bool]$passed=$false;
    $logLocation = "";

  try{
    Write-Verbose "Start Deploying $ccprojName";
    $ccproj = (Get-ChildItem -Path $SOURCESDIRECTORY -Recurse | where {$_.Name -like "${ccprojName}.ccproj"} | Get-Unique).FullName;
    $ccprojLocation = (Get-Item "$ccproj").Directory.FullName;
    $soluctionFile = ((Get-ChildItem "$ccprojLocation\..\") | where {$_.Name -like "*.sln"} | Get-Unique).FullName;

    $bld = & "${Env:ProgramFiles(x86)}\MSBuild\12.0\Bin\msbuild.exe" "$soluctionFile" /t:"clean;publish" /m:1 /p:"Configuration=$ver;Platform=x64" /p:RunCodeAnalysis=false

    if(!$?)
    {
        $bld  | Out-File "$logsLocation\$ccprojName.build.err"
        Write-Error "Build service failed! Please check build log at $logsLocation\$ccprojName.build.err" 
    }else{
        $bld  | Out-File "$logsLocation\$ccprojName.build.log"
    }

    $cloudConfigLocation = ((Get-ChildItem -Path $ccprojLocation) | where {$_.Name -like "ServiceConfiguration.Cloud.$env.cscfg"} | Get-Unique).FullName
    $azurePubXmlFile = ((Get-ChildItem -Path "$ccprojLocation\Profiles") | where {$_.Name -like "*$env*.azurePubxml"} | Get-Unique).FullName
    $publishFolder = "${ccprojLocation}\bin\${ver}\app.publish";
    $packageLocation = ((Get-ChildItem -Path $publishFolder) | where {$_.Name -like "*.cspkg"} | Get-Unique).FullName;

    if($cloudConfigLocation -eq $null) { Write-Error "CloudConfigLocation is null!"; }
    if($azurePubXmlFile -eq $null) { Write-Error "azurePubXmlFile is null!"; }
    if($packageLocation -eq $null) { Write-Error "packageLocation is null!"; }

    \\mlcbld\cfg\BuildScripts\AzurePublish2.3.ps1 -packageLocation $packageLocation -cloudConfigLocation $cloudConfigLocation -azurePubXmlFile $azurePubXmlFile -subscriptionDataFile $subscriptionDataFile -enableUpgradeDeploy $enableUpgrade | Out-Null;

    # TODO: copy build to output folder.

     $passed=$true;

  }catch
  {
        $logLocation = "$logsLocation\${ccprojName}.deploy.err";
        Write-Output $Error | Out-File $logLocation;
        $Error.Clear();
  }
  finally
  {
        PostDeploymentStatus -serviceName $ccprojName -environment $env -passStatus $passed -logLocation $logLocation | Out-Null
  }

  return [bool]$passed;
}

function UpdateTestConfig($browser, $version, $mode, $platform){
    $testConfigLocation = "${SOURCESDIRECTORY}\..\bin\SkypeCastMLCAutomation\WebDriverSettings.xml";
    [xml]$TestConfig = Get-Content $testConfigLocation;
    if($browser -ne $null) {
        $TestConfig.WebDriverSettings.Browser="$browser";
    }
    if($version -ne $null) {
        $TestConfig.WebDriverSettings.Version= "$version";
    }
    if($mode -ne $null) {
        $TestConfig.WebDriverSettings.Mode="$mode";
    }
    if($mode -ne $null) {
        $TestConfig.WebDriverSettings.Platform="$platform";
    }
    $TestConfig.Save($testConfigLocation);
}

function RunTests($workDir, $vstestArgs,$trxBackupPrefix, $mode=$null, $browser=$null,$browserVer=$null, $platform=$null){
    set-Location "$workDir";
    Remove-Item .\testresults -Force -Recurse -ErrorAction Ignore
    Remove-Item .\Screenshots -Force -Recurse -ErrorAction Ignore

    # Update test config.

    UpdateTestConfig -browser $browser -version $browserVer -mode $mode -platform $platform

    $browserShortName = $browser;
    if($browser -eq "internet explorer"){
        $browserShortName ="IE";
    }

    try{
        Start-Process "${env:ProgramFiles(x86)}\Microsoft Visual Studio 12.0\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" -ArgumentList $vstestArgs -wait -passthru -WorkingDirectory $workDir -NoNewWindow;

        # backup trx file
        $time=[DateTime]::Now.ToString("MMddyyHHmmss");
        $trx = (Get-ChildItem -Path ".\testresults\*.trx")[0];

        $variable= "${browserShortName}_${browserVer}".Replace(".","_").Trim('_');
        $trxBackupPrefix = "${trxBackupPrefix}".Replace(".","_");
        $backupFolder="$settings_testLogsLocation\$yellowTailBuild\${trxBackupPrefix}.${variable}.${time}";
        if((Test-Path $backupFolder) -eq $false){
            New-Item -ItemType Directory -Force -Path $backupFolder 
        }
        Copy-Item ".\testresults\*.trx" -Destination $backupFolder -Recurse -Force
        Copy-Item ".\Screenshots" -Destination $backupFolder -Recurse -Force -ErrorAction Ignore
        
        <#
        $trxBackupName= "${trxBackupPrefix}.${browserShortName}${browserVer}.${time}.trx.backup";
        Rename-Item $trx -NewName $trxBackupName;

        $backupLocation = "$settings_testLogsLocation\$yellowTailBuild";
        if((Test-Path $backupLocation) -eq $false){
            New-Item -ItemType Directory -Force -Path $backupLocation 
        }
        Copy-Item ".\testresults\*.trx.backup" -Destination $backupLocation -Recurse -Force
        Copy-Item ".\Screenshots" -Destination $backupLocation -Recurse -Force -ErrorAction Ignore
        #>

    } catch{
        Write-Output "Got error when run tests: $Error";
    }
}

function SendEmail($subject,[string[]]$to,$from,$body,$user,$password,$exchange){

    [void][Reflection.Assembly]::LoadWithPartialName('System.Net.Mail.SmtpClient');
    $sMail = New-Object System.Net.Mail.SmtpClient;
    $sMail.Host = $exchange;
    $sMail.DeliveryMethod = [System.Net.Mail.SmtpDeliveryMethod]::Network;
    $sMail.UseDefaultCredentials = $false;
    $sMail.EnableSsl = $true;
    $sMail.Credentials =  New-Object System.Net.NetworkCredential($user, $password);

    $sMessage = New-Object System.Net.Mail.MailMessage;
    $sMessage.From = New-Object System.Net.Mail.MailAddress($from);
    foreach($mailto in $to){
        $sMessage.To.Add($mailto);
    }
    $sMessage.Subject = $subject;
    $sMessage.Body = $body;
    $sMessage.IsBodyHtml = $true;

    $sMail.Send($sMessage);
}

function XmlToHtml([string]$trxFile, [string]$xsltFile){

    $transform = New-Object System.Xml.Xsl.XslCompiledTransform;
    $reader = [System.Xml.XmlReader]::Create([System.IO.File]::OpenText($xsltFile));
    $transform.Load($reader);

    $writer = New-Object System.IO.StringWriter;
    $reader2 = [System.Xml.XmlReader]::Create([System.IO.File]::OpenText($trxFile));
    $transform.Transform($reader2, $null, $writer);

    return [string]$writer.ToString();
}

function UpdateTrxToYellowTail($trxFile, $buildNumber, $variation, $proj="MLC", $component="Scheduler"){
    $url = [string]::Format("http://yellowtail/api/reports/project/{0}?type=functional&component={1}&variation={2}&build={3}",$proj,$component,$variation,$buildNumber);
    Invoke-RestMethod -Method Post -Uri $url -InFile "$trxFile";

    return [string]::Format("http://yellowtail/reports/{0}/functional/components/{1}?build={2}", $proj, $component,$buildNumber);
}

function PostDeploymentStatus([string]$serviceName, [string]$environment,[bool]$passStatus,[string]$logLocation ){
   try
   {
    $dt= [DateTime]::Now.ToString("yyyy-MM-dd");
    $url = [URI]"http://weiyao3/skypecastDashboard/skypecastReports?buildNumber=${buildNumber}&dt=${dt}&serviceName=${serviceName}&environment=${environment}&passStatus=${passStatus}&logLocation=${logLocation}"
    Invoke-RestMethod -Method Post -Uri $url -Body $null;
 
   }catch{
     #ignore error.
   }
}


function PostTestStatus([string]$buildNumber, [string]$suitename, [string]$environment,[string]$logLocation, [string]$trx){
  try
   {
    $dt= [DateTime]::Now.ToString("yyyy-MM-dd");
    $url = [URI]"http://weiyao3/skypecastDashboard/skypecastReports/attachments?buildNumber=${buildNumber}&suitename=${suitename}&environment=${environment}&logLocation=${logLocation}"
    Invoke-RestMethod -Method Post -Uri $url -InFile $trx
    
   }catch{
     #ignore error.
   }
}

$Report = New-Module -AsCustomObject -ScriptBlock {
    $OverallStatus='<b class="ft-Pass">Pass</b>';
    $BuildNumber= [DateTime]::Now.ToString("MMddyyHHmmss");
    $SSDeployment='<b class="ft-Pass">Skip</b>';
    $DSDeployment='<b class="ft-Pass">Skip</b>';
    $JSDeployment='<b class="ft-Pass">Skip</b>';
    $ASDeployment='<b class="ft-Pass">Skip</b>';
    $UITestResult="";
    $APITestResult="";
    $TestLogLocation="";
    $AdditionalInfo="";

    function GetHtmlStatus([string]$statusString){
        switch ($statusString.ToLower())
        {
            "pass" { $html= '<b class="ft-Pass">Pass</b>';}
            "fail" { $html= '<b class="ft-Fail">Fail</b>';}
            "skip" { $html= '<b class="ft-Pass">Skip</b>';}

        }
        
        return [string]$html;
    }

    function GetDeploymentStatus([bool]$passed){
        if($passed){
            return GetHtmlStatus -statusString "Pass";
        }else{
            return (GetHtmlStatus -statusString "Fail") + "&nbsp&nbsp<a href='$logsLocation'>error</a>";
        }
    }

    function GetHtmlTestResultRow($category, $total,$executed,$failed,$duration,$browser=$null){
        if([string]::IsNullOrEmpty($browser) -eq $false){
            $html= "<tr><td>$browser</td><td>$total</td><td>$executed</td><td class='bg-Pass'>$failed</td><td>${durationStr}m</td></tr>";
        } else {
            $html= "<tr><td>$category</td><td>$total</td><td>$executed</td><td class='bg-Pass'>$failed</td><td>${durationStr}m</td></tr>";
        }

        if($failed -ne 0){
            $html=$html.Replace("bg-Pass","bg-Fail");
        }
        
        return $html;
    }

    function SetAdditionalInfo([DateTime]$startTime,[DateTime]$endTime){
        $start = $startTime.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ssZ");
        $end = $endTime.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ssZ");
        $Report.AdditionalInfo="<span>* See Scheduler MDS log <a href='https://test1.diagnostics.monitoring.core.windows.net/content/search/search.html?table=skypemlcskypecastTstSchedulerWebRoleAppEvents&start=${start}&end=${end}&query=;groupby TaskName'>here</a>.</span>";
    }

    function GetHtmlReport($emailtemplate){
        $mail_Template = Get-Content $emailtemplate;
        $mail_Template = $mail_Template.Replace("%OverallStatus%", $OverallStatus);
        $mail_Template = $mail_Template.Replace("%BuildNumber%", $BuildNumber);
        $mail_Template = $mail_Template.Replace("%SSDeployment%", $SSDeployment);
        $mail_Template = $mail_Template.Replace("%DSDeployment%", $DSDeployment);
        $mail_Template = $mail_Template.Replace("%JSDeployment%", $JSDeployment);
        $mail_Template = $mail_Template.Replace("%ASDeployment%", $ASDeployment);
        $mail_Template = $mail_Template.Replace("%UITestResult%", $UITestResult);
        $mail_Template = $mail_Template.Replace("%APITestResult%", $APITestResult);
        $mail_Template = $mail_Template.Replace("%TestLogLocation%", $TestLogLocation);
        $mail_Template = $mail_Template.Replace("%AdditionalInfo%", $AdditionalInfo);

        return [string]$mail_Template;
    }

    function ParseTestLogs([string]$logBackupLoaction){
        if((Test-Path $logBackupLoaction) -eq $false){
            Write-Error "Location doesn't exists!"
        }


        $Report.UITestResult="";
        $Report.APITestResult="";

        $bldNumber = $logBackupLoaction.Split("\") | select -Last 1;
        
        foreach($backupFolder in (Get-ChildItem $logBackupLoaction -Directory)){
            [bool]$isUITest = $backupFolder.Name.Contains("UI");
            $tmp = $backupFolder.Name.Split('.');
            $category=$tmp[0];
            $browser=$tmp[1];
            if(!$isUITest)
                {$browser = $null;}
            $trxFile= (Get-ChildItem $backupFolder.FullName -Filter "*.trx") | select -First 1;

            [xml]$trx= Get-Content $trxFile.FullName;
            $summaryOutcome = $trx.TestRun.ResultSummary.outcome;
            $total = $trx.TestRun.ResultSummary.Counters.total;
            [int]$executed = $trx.TestRun.ResultSummary.Counters.executed;
            [int]$passed = $trx.TestRun.ResultSummary.Counters.passed;
            [int]$failed = $executed - $passed;
            [Timespan]$duration = [Datetime]$trx.TestRun.Times.finish - [Datetime]$trx.TestRun.Times.start;
            $durationStr = $duration.TotalMinutes.ToString("F2");

            $testeResultHtmlRow =  GetHtmlTestResultRow -category $category -total $total -executed $executed -failed $failed -duration $durationStr -browser $browser;

            # Appand test result rows.
            if($isUITest){
                $Report.UITestResult += $testeResultHtmlRow;
            } else {
                $Report.APITestResult += $testeResultHtmlRow;
            }

            # Set overall status to Fail if any tests failed.
            if($failed -ne 0){
                $Report.OverallStatus=$Report.GetHtmlStatus("fail");
            }
        }
    }

    function UploadTestReport([string]$logBackupLoaction){
        if((Test-Path $logBackupLoaction) -eq $false){
            Write-Error "Location doesn't exists!"
        }

        $bldNumber = $logBackupLoaction.Split("\") | select -Last 1;
        
        foreach($backupFolder in (Get-ChildItem $logBackupLoaction -Directory)){
            [bool]$isUITest = $backupFolder.Name.Contains("UI");
            $tmp = $backupFolder.Name.Split('.');
            $category=$tmp[0];
            $browser=$tmp[1];
            if(!$isUITest)
                {$browser = $null;}
            $trxFile= (Get-ChildItem $backupFolder.FullName -Filter "*.trx") | select -First 1;
            $trx=$trxFile.FullName;

            if($isUITest){
                 #uploads trx
                    $url = [string]::Format("http://yellowtail/api/reports/project/{0}?type=functional&component={1}&variation={2}&build={3}","mlc","SchedulerUI",$browser,$bldNumber);
                    Invoke-RestMethod -Method Post -Uri $url -InFile "$trx";

                    # uploads screenshots as attachments
                    if(Test-Path("$($backupFolder.FullName)\Screenshots\")){
                    foreach($file in (Get-ChildItem -Path "$($backupFolder.FullName)\Screenshots\")){
                        $fileName = $file.Name;
                        $filePath = $file.FullName;
                        Invoke-RestMethod -Method Post -Uri "http://yellowtail/api/reports/mlc/attachments?type=functional&component=SchedulerUI&variation=${browser}&build=$bldNumber" -InFile "$filePath" -Headers @{"Content-Disposition"="inline; filename=$fileName"}
                    }
                    }

                 #upload to vijay's site
                    PostTestStatus -buildNumber $bldNumber -suitename "SchedulerUI_$browser" -environment "Tst" -logLocation $backupFolder -trx "$trx"

            } else{
                 #uploads trx
                    $url = [string]::Format("http://yellowtail/api/reports/project/{0}?type=functional&component={1}&variation={2}&build={3}","mlc",$category,"Default",$bldNumber);
                    Invoke-RestMethod -Method Post -Uri $url -InFile "$trx";
            
                #upload to vijay's site
                    PostTestStatus -buildNumber $bldNumber -suitename $category -environment "Tst" -logLocation $backupFolder -trx "$trx"
            }
     
        }
    }

    Export-ModuleMember -Variable * -Function *}

################################################################################
# Start
################################################################################

$settings_email_to = "vijayara@microsoft.com"
$yellowTailBuild="MlcDaily_Test";
$SOURCESDIRECTORY = "c:\Blds\318\src";
$skipeDeploy=$true;
$skipeTest=$false;

$logsLocation = "$settings_testLogsLocation\$yellowTailBuild";
if(!(Test-Path $logsLocation)){
    New-Item -ItemType Directory -Force -Path $logsLocation; 
}


try{ 
 
  if(!$skipeDeploy)
   {
    $ssstatus = DeployAzureService -ccprojName "Scheduler" -ver "Release" -env "Tst"
    $Report.SSDeployment=$Report.GetDeploymentStatus($ssstatus);

    $dsstatus = DeployAzureService -ccprojName "DataService" -ver "Release" -env "Tst"
    $Report.DSDeployment=$Report.GetDeploymentStatus($dsstatus);

    $jsstatus = DeployAzureService -ccprojName "Join" -ver "Release" -env "Tst" -enableUpgrade $true
    $Report.JSDeployment=$Report.GetDeploymentStatus($jsstatus);

    $asstatus = DeployAzureService -ccprojName "Activator" -ver "Debug" -env "Tst"
    $Report.ASDeployment=$Report.GetDeploymentStatus($asstatus);
   
    if(!$ssstatus -or !$dsstatus -or !$jsstatus -or !$asstatus)
    {
        $Report.OverallStatus=$Report.GetHtmlStatus("fail");
        Write-Error "Build & Deploy services failed.  Please check logs."
    }
   }

  if(!$skipeTest)
   {
    $testStartTime = [DateTime]::Now;
    $testDir = "${SOURCESDIRECTORY}\..\bin\SkypeCastMLCAutomation";

    # Run UI tests on local, workaround IE sign in issue.
    if(Test-Path "hklm:SOFTWARE\Wow6432Node\Microsoft\Internet Explorer\MAIN\FeatureControl\FEATURE_DISABLE_INTERNAL_SECURITY_MANAGER"){
        Set-ItemProperty -Path "hklm:SOFTWARE\Wow6432Node\Microsoft\Internet Explorer\MAIN\FeatureControl\FEATURE_DISABLE_INTERNAL_SECURITY_MANAGER" -name iexplore.exe -value 1
        Set-ItemProperty -Path "hklm:SOFTWARE\Microsoft\Internet Explorer\MAIN\FeatureControl\FEATURE_DISABLE_INTERNAL_SECURITY_MANAGER" -name iexplore.exe -value 1
    }
    
    RunTests -workDir $testDir -vstestArgs "Microsoft.Skype.SkypeCast.Test.UITests.dll /InIsolation /TestCaseFilter:TestCategory!=LocalExclusive /logger:trx" -trxBackupPrefix "SchedulerUI" -mode "Local" -browser "internet explorer" -browserVer "11_8" -platform "windows"
    #RunTests -workDir $testDir -vstestArgs "Microsoft.Skype.SkypeCast.Test.UITests.dll /InIsolation /TestCaseFilter:TestCategory!=LocalExclusive /logger:trx" -trxBackupPrefix "SchedulerUI" -mode "Grid" -browser "firefox" -browserVer "" -platform "windows"
    #RunTests -workDir $testDir -vstestArgs "Microsoft.Skype.SkypeCast.Test.UITests.dll /InIsolation /TestCaseFilter:TestCategory!=LocalExclusive /logger:trx" -trxBackupPrefix "SchedulerUI" -mode "Grid" -browser "chrome" -browserVer "" -platform "windows"
    
    # mobile browsers:
    #RunTests -workDir $testDir -vstestArgs "Microsoft.Skype.SkypeCast.Test.UITests.dll /InIsolation /TestCaseFilter:TestCategory!=LocalExclusive /logger:trx" -trxBackupPrefix "SchedulerUI" -mode "Mobile" -browser "safari" -browserVer "iPhone.6" -platform "mac"
    #RunTests -workDir $testDir -vstestArgs "Microsoft.Skype.SkypeCast.Test.UITests.dll /InIsolation /TestCaseFilter:TestCategory!=LocalExclusive /logger:trx" -trxBackupPrefix "SchedulerUI" -mode "Mobile" -browser "chrome" -browserVer "asus.Nexus.7" -platform "android"

    # Run API tests: 
   # RunTests -workDir $testDir -vstestArgs "SchedulingSvcWebAPITests.dll /InIsolation /TestCaseFilter:TestCategory!=LocalExclusive /logger:trx" -trxBackupPrefix "SchedulerAPI";
   # RunTests -workDir $testDir -vstestArgs "ActivationServiceWebAPITests.dll /InIsolation /TestCaseFilter:TestCategory!=LocalExclusive&FullyQualifiedName~ActivationServiceWebAPITests.JoinServiceWebAPITests /logger:trx" -trxBackupPrefix "JoinSvcAPI";
   # RunTests -workDir $testDir -vstestArgs "ActivationServiceWebAPITests.dll /InIsolation /TestCaseFilter:TestCategory!=LocalExclusive&FullyQualifiedName~ActivationServiceWebAPITests.ActivationServiceWebAPITests /logger:trx" -trxBackupPrefix "ActivationSvcAPI";
    
    $testEndTime = [DateTime]::Now;

    $Report.BuildNumber=$yellowTailBuild;
    $Report.TestLogLocation=$logsLocation;
    $Report.ParseTestLogs($logsLocation);
    $Report.SetAdditionalInfo($testStartTime, $testEndTime);
    $Report.UploadTestReport($logsLocation);
    }
}
catch{
    $Report.OverallStatus=$Report.GetHtmlStatus("fail");
    Write-Output $Error | Out-File "$logsLocation\err.log";
    Write-Error $Error;
}
finally{
    Write-Output "$(Get-Date -f $timeStampFormat) - Send notification email.";
    $mailBody = $Report.GetHtmlReport($settings_email_template);
    $mailBody | Out-File "$logsLocation\report.html"
    SendEmail -subject $settings_email_subject -to $settings_email_to -from $settings_email_user -body $mailBody -user $settings_email_user -password $settings_email_password -exchange $settings_email_exchange;
}