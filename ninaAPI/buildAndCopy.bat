dotnet build C:\Users\Christian\source\repos\NINA\ninaAPI\ninaAPI\ninaAPI.csproj /property:GenerateFullPaths=true /consoleloggerparameters:NoSummary /p:Configuration=Release /p:Platform="AnyCPU"
pwsh -Command Compress-Archive -Path 'C:\Users\Christian\AppData\Local\NINA\Plugins\3.0.0\Advanced API\*' -Destination ninaAPI.zip -Force
set /p beta="Should this be a beta release? y/n: "
set /p version="Version: "
if %beta%==y (
    set /p b_v="What beta version is that?: "
    gh release create %version%-b.%b_v% ninaAPI.zip -title ninaAPI %version%-beta %b_v% -p
    pwsh.exe CreateNET7Manifest.ps1 -file ninaAPI.zip -beta -installerUrl "https://github.com/christian-photo/ninaAPI/releases/download/%version%-b.%b_v%/ninaAPI.zip"
)
else (
    gh release create %version% ninaAPI.zip -title %version%
    pwsh.exe CreateNET7Manifest.ps1 -file ninaAPI.zip -installerUrl "https://github.com/christian-photo/ninaAPI/releases/download/%version%/ninaAPI.zip"
)
cd ..
cd ..
cd nina.plugin.manifests
git pull
git pull https://AstroChris23@bitbucket.org/Isbeorn/nina.plugin.manifests.git
copy ..\ninaAPI\ninaAPI\manifest.json manifests\n\ninaAPI\3.0.0\%version%\manifest.json
echo "Testing the manifest"
node gather
echo "Please verify that the test ran successfully"
pause
git add manifests\n\ninaAPI\3.0.0\*
git commit -m "Added ninaAPI manifest for version %version%"
git push origin main
pause