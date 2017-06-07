buildNuget.bat

%NugetExe% push Realmius.Contracts.%Version%.nupkg %NugetApiKey% -Source https://www.nuget.org/api/v2/package
%NugetExe% push Realmius.%Version%.nupkg %NugetApiKey% -Source https://www.nuget.org/api/v2/package
%NugetExe% push Realmius.Server.%Version%.nupkg %NugetApiKey% -Source https://www.nuget.org/api/v2/package
