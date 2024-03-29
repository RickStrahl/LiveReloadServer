
if (test-path ./nupkg) {
    remove-item ./nupkg -Force -Recurse
}   

dotnet build -c Release

# $filename = 'LiveReloadServer.0.2.4.nupkg'
$filename = gci "./build/nupkg/*.nupkg" | sort LastWriteTime | select -last 1 | select -ExpandProperty "Name"
Write-host $filename

$len = $filename.length
Write-host $len

if ($len -gt 0) {
    Write-Host "signing..."
    nuget sign  ".\build\nupkg\$filename"   -CertificateSubject "West Wind Technologies" -timestamper " http://timestamp.digicert.com"  
    nuget push  ".\build\nupkg\$filename" -source "https://nuget.org"
}