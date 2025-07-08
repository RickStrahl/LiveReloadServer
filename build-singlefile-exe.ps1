
# Make sure you have this in your project (No Razor Support doesn't compile in AOT):
#
# <DefineConstants>BUILD_EXE</DefineConstants>
#
# Note:
# For Razor Compilation `PublishTrimmed` does not work because
# it relies on dynamic interfaces when compiling Razor at runtime.
# If you compile with Razor disabled, or don't plan on using Razor
# with this LiveReloadServer, you can set /p:PublishTrimmed=true
# to cut the size of the exe in half.

# if (test-path './LiveReloadWebServer.exe' -PathType Leaf) { remove-item ./LiveReloadWebServer.exe }
if (test-path './build/SelfContainedSingle' -PathType Container) { remove-item ./build/SelfContainedSingle -Recurse -Force }

# Single File Exe output
# Copy-Item ./LiveReloadServer/LiveReloadWebServer.json ./LiveReloadWebServer.json

# Make sure hosted project gets built (in default project output folder)
dotnet publish -c Release /p:PublishSingleFile=true /p:PublishTrimmed=true -o ./build/SelfContainedSingle
copy-Item ./build/SelfContainedSingle/LiveReloadServer.exe ./build/SelfContainedSingle/LiveReloadWebServer.exe
Remove-Item ./build/SelfContainedSingle/LiveReloadServer.exe

# Sign exe
.\signtool.exe sign /v /n "West Wind Technologies"   /tr "http://timestamp.digicert.com" /td SHA256 /fd SHA256 ".\build\SelfContainedSingle\LiveReloadWebServer.exe"
.\signtool.exe sign /v /n "West Wind Technologies"   /tr "http://timestamp.digicert.com" /td SHA256 /fd SHA256 ".\build\SelfContainedSingle\LiveReloadServer.dll"

remove-item ".\LiveReloadWebServer-SelfContained-Single.zip"
7z a -tzip -r ".\LiveReloadWebServer-SelfContained-Single.zip" "./build/SelfContainedSingle/*.*"