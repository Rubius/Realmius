SET NugetApiKey=

call buildNuget.bat

%NugetExe% push Realmius.Contracts.* %NugetApiKey% -Source https://www.nuget.org/api/v2/package
%NugetExe% push Realmius.* %NugetApiKey% -Source https://www.nuget.org/api/v2/package
%NugetExe% push Realmius.Server.* %NugetApiKey% -Source https://www.nuget.org/api/v2/package
