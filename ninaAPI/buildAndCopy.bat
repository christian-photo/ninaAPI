dotnet build C:\Users\Christian\source\repos\NINA\ninaAPI\ninaAPI\ninaAPI.csproj /property:GenerateFullPaths=true /consoleloggerparameters:NoSummary /p:Configuration=Release /p:Platform="AnyCPU"
set /p "beta=Should this be a beta release? y/n: "
echo "%beta%"
set /p "version=Version: "
set /p "folder=Folder: "
echo "%version%"
set /p "b=Beta Version (leave empty for no beta release): "
if "%beta%" == "y" (
    pwsh.exe CreateNET7Manifest.ps1 -file "C:\Users\Christian\AppData\Local\NINA\Plugins\3.0.0\Advanced API\ninaAPI.dll" -includeAll -createArchive -beta -installerUrl "https://github.com/christian-photo/ninaAPI/releases/download/%version%-b.%b%/ninaAPI.zip"
    gh release create "%version%-b.%b%" -t "ninaAPI %version%-beta %b%" -p -R christian-photo/ninaAPI ninaAPI.zip
) ELSE (
    pwsh.exe CreateNET7Manifest.ps1 -file "C:\Users\Christian\AppData\Local\NINA\Plugins\3.0.0\Advanced API\ninaAPI.dll" -includeAll -createArchive -installerUrl "https://github.com/christian-photo/ninaAPI/releases/download/%version%/ninaAPI.zip"
    gh release create %version% -t "ninaAPI %version%" -R christian-photo/ninaAPI ninaAPI.zip
)
cd ..
cd ..
cd nina.plugin.manifests
git pull
git pull https://AstroChris23@bitbucket.org/Isbeorn/nina.plugin.manifests.git
mkdir manifests\n\ninaAPI\3.0.0\%folder%
copy ..\ninaAPI\ninaAPI\manifest.json manifests\n\ninaAPI\3.0.0\%folder%\manifest.json
echo "Testing the manifest"
node gather
echo "Please verify that the test ran successfully"
pause
git add manifests\n\ninaAPI\3.0.0\*
git commit -m "Added ninaAPI manifest for version %version%"
git push origin main
pause