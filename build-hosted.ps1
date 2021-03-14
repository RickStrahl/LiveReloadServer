
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



# Single File Exe output
dotnet publish -c Release -o ./build/Hosted
copy ./LiveReloadServer/LiveReloadWebServer.json ./LiveReloadWebServer.json

# Make sure hosted project gets built (in default project output folder)
dotnet publish -c Release /p:PublishSingleFile=false /p:PublishTrimmed=false 

#Move-Item ./SingleFileExe/LiveReloadServer.exe ./LiveReloadWebServer.exe -force
#remove-item ./SingleFileExe -Recurse -Force


# Sign exe
.\signtool.exe sign /v /n "West Wind Technologies"   /tr "http://timestamp.digicert.com" /td SHA256 /fd SHA256 ".\build\Hosted\LiveReloadServer.exe"


del  ".\LiveReloadServer-Hosted.zip"
7z a -tzip -r ".\LiveReloadServer-Hosted.zip" "./build/Hosted/*.*"