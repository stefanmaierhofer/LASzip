#!/bin/bash

dotnet tool restore
dotnet paket restore
dotnet build src/LASzip.sln --configuration Release
