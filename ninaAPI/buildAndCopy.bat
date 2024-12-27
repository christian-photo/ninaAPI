dotnet build C:\Users\Christian\source\repos\NINA\ninaAPI\ninaAPI\ninaAPI.csproj /property:GenerateFullPaths=true /consoleloggerparameters:NoSummary /p:Configuration=Release /p:Platform="AnyCPU"
pwsh.exe CreateNET7Manifest.ps1 -file "C:\Users\Christian\AppData\Local\NINA\Plugins\3.0.0\Advanced API\ninaAPI.dll" -includeAll -createArchive
cd ..
cd ..
cd nina.plugin.manifests
git pull
git pull https://AstroChris23@bitbucket.org/Isbeorn/nina.plugin.manifests.git
copy ..\ninaAPI\ninaAPI\manifest.json manifests\n\ninaAPI\3.0.0\manifest.json
echo "Please finish the manifest and upload the file"
pause
node gather
pause
set /p "version=Enter Version: "
git add manifests\n\ninaAPI\3.0.0\*
git commit -m "Added ninaAPI manifest"
git push origin main
pause