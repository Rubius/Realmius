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

using System.Data.Entity.Migrations;

namespace Realmius.Server.Migrations
{
    public partial class SyncStatusCompositeKey : DbMigration
    {
        public override void Up()
        {
            DropIndex("dbo._RealmSyncStatus", "IX_Download0");
            DropPrimaryKey("dbo._RealmSyncStatus");
            AlterColumn("dbo._RealmSyncStatus", "Type", c => c.String(nullable: false, maxLength: 40));
            AddPrimaryKey("dbo._RealmSyncStatus", new[] { "Type", "MobilePrimaryKey" });
            CreateIndex("dbo._RealmSyncStatus", new[] { "LastChange", "Type", "Tag0" }, name: "IX_Download0");
        }
        
        public override void Down()
        {
            DropIndex("dbo._RealmSyncStatus", "IX_Download0");
            DropPrimaryKey("dbo._RealmSyncStatus");
            AlterColumn("dbo._RealmSyncStatus", "Type", c => c.String(maxLength: 40));
            AddPrimaryKey("dbo._RealmSyncStatus", "MobilePrimaryKey");
            CreateIndex("dbo._RealmSyncStatus", new[] { "LastChange", "Type", "Tag0" }, name: "IX_Download0");
        }
    }
}
