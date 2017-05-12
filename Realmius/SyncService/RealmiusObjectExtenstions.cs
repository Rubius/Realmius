////////////////////////////////////////////////////////////////////////////
//
// Copyright 2017 Rubius
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
////////////////////////////////////////////////////////////////////////////

using Realmius.SyncService.RealmModels;

namespace Realmius.SyncService
{
    public static class RealmiusObjectExtenstions
    {
        public static SyncState GetSyncState(this ObjectSyncStatusRealm obj)
        {
            return (SyncState)obj.SyncState;
        }
        public static void SetSyncState(this ObjectSyncStatusRealm obj, SyncState syncState)
        {
            obj.SyncState = (int)syncState;
        }
    }
}