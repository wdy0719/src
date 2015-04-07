$ErrorActionPreference = "stop";

$SOURCESDIRECTORY = $Env:TF_BUILD_SOURCESDIRECTORY
$BinDir = "${SOURCESDIRECTORY}\..\bin"

if( Test-Path "$BinDir" ) {
	Remove-Item "$BinDir\*" -recurse
}


if($error){
	exit -1
} else{
	exit 0
}
