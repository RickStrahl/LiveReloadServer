$packageName = "LiveReloadWebServer"
$url = "https://github.com/RickStrahl/LiveReloadServer/raw/1/LiveReloadWebServer-SelfContained.zip"
$drop = "$(Split-Path -Parent $MyInvocation.MyCommand.Definition)"
$sha = ""
Install-ChocolateyZipPackage -PackageName "$packageName" -Url "$url" -UnzipLocation "$drop" -checksum64 "$sha" -checksumtype "sha256"
