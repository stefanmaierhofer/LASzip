@echo off
SETLOCAL
PUSHD %~dp0

dotnet build src/LASzip.sln --configuration Release

git tag %1
git push --tags

dotnet paket pack bin --version %1

set /p Key=<%HOMEPATH%\.ssh\nuget_uncodium.key
dotnet nuget push "bin\*.%1.nupkg" --skip-duplicate --source https://www.nuget.org/api/v2/package -k %Key%
