# Realmius

[![Build Status](https://travis-ci.org/RubiusGroup/Realmius.svg?branch=master)](https://travis-ci.org/RubiusGroup/Realmius)
[![NuGet](https://img.shields.io/nuget/dt/Realmius.svg)]()
[![NuGet](https://img.shields.io/nuget/dt/Realmius.Server.svg)]()
[![NuGet](https://img.shields.io/nuget/dt/Realmius.Contracts.svg)]()

## Sync engine for Realm database with Sql Server backend

#### Sample of usage
![realmius](https://cloud.githubusercontent.com/assets/3094339/26148250/3ff89b38-3b20-11e7-838e-ff1ee0a873ca.gif)

#### Getting started

##### Server
1. Use the NuGet package manager to add a reference to Realmius.Server
2. Setup Entity Framework, create entities with implementation of IRealmiusObjectServer interface
3. Create EF context with implementation of ChangeTrackingDbContext
4. Create sync configuration as implementation of SyncConfigurationBase
5. Start SignalR hub (also you may use long-pooling variant)

##### Client
1. Use the NuGet package manager to add a reference to Realmius
2. Setup Realm database, create entities with implementation of IRealmiusObjectClient interface
3. Create instance of IRealmiusSyncService using SyncServiceFactory
4. Connect to server

#### Examples

You can find examples in the `/Examples` folder in the code repository.

#### NuGet packages

The stable release packages published to [nuget.org](https://www.nuget.org/packages?q=Realmius).

Package | Targets
--------|---------------
Realmius | Portable class library, Xamarin.Ios, Xamarin.Android
Realmius.Contracts | NET46
Realmius.Server | NET46
