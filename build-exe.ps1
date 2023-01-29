
# Make sure you have this in your project:
#
# <PublishSingleFile>true</PublishSingleFile>
# <PublishTrimmed>true</PublishTrimmed>
# <RuntimeIdentifier>win-x64</RuntimeIdentifier>
#
# and you disable the Package Build
#
# <PackAsTool>false</PackAsTool>
#
# Note:
# For Razor Compilation `PublishTrimmed` does not work because
# it relies on dynamic interfaces when compiling Razor at runtime.
# If you compile with Razor disabled, or don't plan on using Razor
# with this LiveReloadServer, you can set /p:PublishTrimmed=true
# to cut the size of the exe in half.

# if (test-path './LiveReloadWebServer.exe' -PathType Leaf) { remove-item ./LiveReloadWebServer.exe }
if (test-path './build/SelfContained' -PathType Container) { remove-item ./build/SelfContained -Recurse -Force }

# Single File Exe output
# Copy-Item ./LiveReloadServer/LiveReloadWebServer.json ./LiveReloadWebServer.json

# Make sure hosted project gets built (in default project output folder)
dotnet publish -c Release /p:PublishSingleFile=false /p:PublishTrimmed=false -o ./build/SelfContained
copy-Item ./build/SelfContained/LiveReloadServer.exe ./build/SelfContained/LiveReloadWebServer.exe
Remove-Item ./build/SelfContained/LiveReloadServer.exe

# Sign exe
.\signtool.exe sign /v /n "West Wind Technologies"   /tr "http://timestamp.digicert.com" /td SHA256 /fd SHA256 ".\build\SelfContained\LiveReloadWebServer.exe"

remove-item ".\LiveReloadWebServer-SelfContained.zip"
7z a -tzip -r ".\LiveReloadWebServer-SelfContained.zip" "./build/SelfContained/*.*"