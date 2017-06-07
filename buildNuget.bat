SET NugetExe=%LOCALAPPDATA%\NuGet\NuGet.exe

del *.nupkg

%NugetExe% pack Realmius.Contracts\Realmius.Contracts.csproj -Build -properties Configuration=Release
%NugetExe% pack Realmius\Realmius.csproj -Build -properties Configuration=Release
%NugetExe% pack Realmius.Server\Realmius.Server.csproj -Build -properties Configuration=Release
