import subprocess

subprocess.run(
    'dotnet build ninaAPI.csproj /property:GenerateFullPaths=true /consoleloggerparameters:NoSummary /p:Configuration=Release /p:Platform="AnyCPU"'
)

beta = input("Is this a beta release? y/n: ") == "y"
version = input("Plugin version: ")
folder = input("The folder in which the manifest should be located: ")

if beta:
    beta_version = input("Beta version: ")
    subprocess.run(
        f'pwsh.exe CreateNET7Manifest.ps1 -file "%localappdata%\\NINA\\Plugins\\3.0.0\\Advanced API\\ninaAPI.dll" -includeAll -createArchive -beta -installerUrl "https://github.com/christian-photo/ninaAPI/releases/download/{version}-b.{beta_version}/ninaAPI.zip"'
    )
    subprocess.run(
        f'gh release create "{version}-b.{beta_version}" -t "ninaAPI {version}-beta {beta_version}" -p -R christian-photo/ninaAPI ninaAPI.zip'
    )
else:
    subprocess.run(
        f'pwsh.exe CreateNET7Manifest.ps1 -file "%localappdata%\\NINA\\Plugins\\3.0.0\\Advanced API\\ninaAPI.dll" -includeAll -createArchive -installerUrl "https://github.com/christian-photo/ninaAPI/releases/download/{version}/ninaAPI.zip"'
    )
    subprocess.run(
        f'gh release create {version} -t "ninaAPI {version}" -R christian-photo/ninaAPI ninaAPI.zip'
    )

import os

os.chdir("..\\..\\nina.plugin.manifests")

subprocess.run("git pull")
subprocess.run("git pull https://github.com/isbeorn/nina.plugin.manifests.git")

if not os.path.exists(f"manifests\\n\\ninaAPI\\3.0.0\\{folder}"):
    os.makedirs(f"manifests\\n\\ninaAPI\\3.0.0\\{folder}")

subprocess.run(
    f"copy ..\\ninaAPI\\ninaAPI\\manifest.json manifests\\n\\ninaAPI\\3.0.0\\{folder}\\manifest.json"
)

print("Now testing the manifests...")
subprocess.run("node gather")
print("Please verify that the test ran successfully")
input("Press enter to continue...")

subprocess.run("git add manifests\\n\\ninaAPI\\3.0.0\\*")
subprocess.run(f'git commit -m "Added ninaAPI manifest for version {version}"')
subprocess.run("git push origin main")

doc = input("Do you want to update the documentation? y/n: ")
if doc == "y":
    os.chdir("..\\ninaAPI\\ninaAPI")

    from dotenv import load_dotenv
    from openapi_spec_validator import validate
    from openapi_spec_validator.readers import read_from_filename

    try:
        validate(read_from_filename("api_spec_v3.yaml"))
        print("The manifest is valid")
    except Exception as e:
        print(e)
        input("Press enter to exit...")
        exit(1)

    load_dotenv()

    bumpToken = os.getenv("BUMP")
    subprocess.run(
        f"bump deploy --doc 'advanced-api' --token {bumpToken} --branch v3 'api_spec_v3.yaml'"
    )

input("Press enter to exit...")
