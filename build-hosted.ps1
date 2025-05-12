# Make sure you have this in your project:
#
# <DefineConstants>USE_RAZORPAGES;BUILD_PACKAGE</DefineConstants>
#
# Note:
# For Razor Compilation `PublishTrimmed` does not work because
# it relies on dynamic interfaces when compiling Razor at runtime.
# If you compile with Razor disabled, or don't plan on using Razor
# with this LiveReloadServer, you can set /p:PublishTrimmed=true
# to cut the size of the exe in half.

if (test-path './build/Hosted' -PathType Container) { remove-item .\build\Hosted -Recurse -Force }

# Single File Exe output
dotnet publish -c Release -o ./build/Hosted

copy-Item ./build/Hosted/LiveReloadServer.exe ./build/Hosted/LiveReloadWebServer.exe
remove-item ./build/Hosted/LiveReloadServer.exe

# Sign exe
.\signtool.exe sign /v /n "West Wind Technologies"   /tr "http://timestamp.digicert.com" /td SHA256 /fd SHA256 ".\build\Hosted\LiveReloadWebServer.exe"
.\signtool.exe sign /v /n "West Wind Technologies"   /tr "http://timestamp.digicert.com" /td SHA256 /fd SHA256 ".\build\Hosted\LiveReloadWebServer.dll"

del  ".\LiveReloadServer-Hosted.zip"
7z a -tzip -r ".\LiveReloadServer-Hosted.zip" "./build/Hosted/*.*"