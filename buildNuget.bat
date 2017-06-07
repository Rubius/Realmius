SET Version=1.0.2
SET NugetExe=%LOCALAPPDATA%\NuGet\NuGet.exe
SET NugetApiKey=

%NugetExe% pack Realmius.Contracts\Realmius.Contracts.csproj -Version %Version% -Build -properties Configuration=Release
%NugetExe% pack Realmius\Realmius.csproj -Build -properties Configuration=Release
%NugetExe% pack Realmius.Server\Realmius.Server.csproj -Build -properties Configuration=Release


%NugetExe% push Realmius.Contracts.%Version%.nupkg %NugetApiKey% -Source https://www.nuget.org/api/v2/package
%NugetExe% push Realmius.%Version%.nupkg %NugetApiKey% -Source https://www.nuget.org/api/v2/package
%NugetExe% push Realmius.Server.%Version%.nupkg %NugetApiKey% -Source https://www.nuget.org/api/v2/package