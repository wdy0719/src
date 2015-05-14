function Read-PhxLogs2
{
    param
    (
        [Parameter(Mandatory=$true, ValueFromPipeline=$true)]
        [string] $Machine,
        [Parameter(Mandatory=$true, Position=0)]
        [string] $FileNameBase,
        [Parameter(Mandatory=$true, ParameterSetName="StartEnd")]
        [Parameter(Mandatory=$true, ParameterSetName="StartDuration")]
        [datetime] $Start,
        [Parameter(Mandatory=$true, ParameterSetName="StartDuration")]
        [Parameter(Mandatory=$true, ParameterSetName="EndDuration")]
        [timespan] $Duration,
        [Parameter(Mandatory=$true, ParameterSetName="StartEnd")]
        [Parameter(Mandatory=$true, ParameterSetName="EndDuration")]
        [datetime] $End,
        [Parameter(Mandatory=$true, ParameterSetName="Last")]
        [timespan] $Last,
        [switch] $UpdateCache
    )

    begin
    {
        $logFolder = Join-Path "data\logs" (Split-Path $fileNameBase)
        $logName = Split-Path $fileNameBase -Leaf
        $logMask = "$logFolder\$($logName)*"

        switch ($PsCmdlet.ParameterSetName)
        {
            "StartEnd"      { }
            "StartDuration" { $end = $start + $duration }
            "EndDuration"   { $start = $end - $duration }
            "Last"          { $end = [datetime]::Now; $start = $end - $last }
            default         { throw "Unknown parameter set name: $($PsCmdlet.ParameterSetName)" }
        }
    }
    process
    {
        Write-Progress "Getting logs" "Reading log folder on $machine"
        $files = $machine | Read-PhxFile $logMask -UpdateCache:$updateCache -Csv

        Write-Progress "Getting logs" "Finding relevant log files on $machine"
        $files | Add-Member -MemberType ScriptProperty -Name Creation -Value {[datetime] $this.creationtime} -Force
        $files | Add-Member -MemberType ScriptProperty -Name Write -Value {[datetime] $this.lastwritetime} -Force
        $relevant = $files | where `
        {
            $creationCaptured = ($start -le $psitem.Creation) -and ($psitem.Creation -le $end)
            $writeCaptured = ($start -le $psitem.Write) -and ($psitem.Write -le $end)
            $included = ($start -ge $psitem.Creation) -and ($end -le $psitem.Write)
            $creationCaptured -or $writeCaptured -or $included
        }

        if( -not $files )
        {
            Write-Verbose "No logs '$FileNameBase' found on $machine"
        }
        else
        {
            $firstWrite = $files | sort Creation | select -first 1 | foreach Creation
            $lastWrite = $files | sort Write | select -last 1 | foreach Write
            Write-Verbose "First write for '$FileNameBase' on $($machine): $firstWrite"
            Write-Verbose "Last write for '$FileNameBase' on $($machine): $lastWrite"
        }

        $content = foreach( $log in $relevant | sort{[datetime] $psitem.creationtime} )
        {
            Write-Progress "Getting logs" "Reading relevant log file $($log.filename) on $machine"
            $machine | Read-PhxFile "$logFolder\$($log.filename)" -UpdateCache:$updateCache
        }

        Write-Progress "Getting logs" "Time filtration of $fileNameBase from $machine"
        $lines = BS -content $content -startIndex 0 -endIndex $content.Length -startTime $Start -endTime $End

        Write-Progress "Getting logs" "Parsing of $fileNameBase from $machine"
        $lines | ConvertFrom-ApLogs
    }
    end
    {
        Write-Progress "Getting logs" "Done" -Completed
    }
}

function BS($content,[int]$startIndex,[int]$endIndex, $startTime, $endTime){
    if($endIndex -lt $startIndex){
        return @();
    }

    $mid = $startIndex+[int](($endIndex-$startIndex)/2);
	if($mid -ne $startIndex)
	{
		$curLine= $content[$mid];
		$time = [datetime] ($curLine | parse '^\w,([^,]+).+$')
		if($startTime -ge $time){
			return BS -content $content -startIndex $mid -endIndex $endIndex  -startTime $startTime -endTime $endTime
		}
		if($endTime -le $time){
			return BS -content $content -startIndex $startIndex -endIndex $mid -startTime $startTime -endTime $endTime
		}
	}

    $i=$mid;
    do{
        if([datetime] ($content[$i] | parse '^\w,([^,]+).+$') -ge $startTime){
			$content[$i]
        }else{
            break;
        }

    }while(--$i -ge $startIndex); 

     $i=$mid;
     while(++$i -le $endIndex){
        if([datetime] ($content[$i] | parse '^\w,([^,]+).+$') -le $endTime){
			$content[$i]
        }else{
            break;
        }
	}
}

function testPerf(){
    $start= [DateTime]::Now;
    $r1= "CO2SCH010121017" | Read-PhxLogs "../Cslogs/local/*JobManagerDispatcher.exe_*" -Start "5/13/2015 3:14:31 PM" -End "5/13/2015 4:15:03 PM" -UpdateCache
    $end= [DateTime]::Now;

    $start2= [DateTime]::Now;
    $r2 = "CO2SCH010121017" | Read-PhxLogs2 "../Cslogs/local/*JobManagerDispatcher.exe_*" -Start "5/13/2015 3:14:31 PM" -End "5/13/2015 4:15:03 PM" -UpdateCache
    $end2= [DateTime]::Now;

    Write-Host("r1="+$r1.Length+"     r2="+$r2.Length);
    Write-Host("r1 time: "+($end-$start).ToString());
    Write-Host("r2 time: "+($end2-$start2).ToString());
}