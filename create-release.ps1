# Make sure you run this after you have build the Self-Contained EXE and Zip
# This script will:
#
# * Commit to GitHub and tag the commit with the latest file version numer
# * Update the Chocolatey script with the latest version number and SHA256 hash
#
# Once you've run this script, you can run the build-chocolatey.ps1 script to 
# build the Chocolatey package and the push to chocolatey.

$releaseFolder = "$PSScriptRoot\build\SelfContained"
$releaseFile = "$releaseFolder\LiveReloadWebServer.exe"
$releaseZip = ".\LiveReloadWebServer-SelfContained.zip"

$rawVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($releaseFile).FileVersion

$version = $rawVersion.Trim().Replace(".0","") 
"Writing Version File for: $version ($rawVersion)"

$downloadUrl = "https://github.com/RickStrahl/LiveReloadServer/raw/$version/LiveReloadWebServer-SelfContained.zip"               
"Download Url: $downloadUrl"

# Write out Verification.txt
$sha = get-filehash -path $releaseZip -Algorithm SHA256  | select -ExpandProperty "Hash"
write-host $sha

$filetext = @"
`$packageName = "LiveReloadWebServer"
`$url = "$downloadUrl"
`$drop = "`$(Split-Path -Parent `$MyInvocation.MyCommand.Definition)"
`$sha = "$sha"
`Install-ChocolateyZipPackage -PackageName "`$packageName" -Url "`$url" -UnzipLocation "`$drop" -checksum "`$sha" -checksumtype "sha256"
"@
out-file -filepath Chocolatey\tools\chocolateyInstall.ps1 -inputobject $filetext
Write-Host $filetext

# Write out new NuSpec file with Version
$chocoNuspec = ".\Chocolatey\LiveReloadWebServer.template.nuspec"
$content = Get-Content -Path $chocoNuspec
$content = $content.Replace("{{version}}",$version)
# Write-Host $content
out-file -filepath $chocoNuSpec.Replace(".template","")  -inputobject $content -Encoding utf8

# Commit  current changes and add a tag
git add --all

git tag --delete $version
git push --delete origin $version 

git commit -m "$version" 
git tag $version
git push origin master --tags
