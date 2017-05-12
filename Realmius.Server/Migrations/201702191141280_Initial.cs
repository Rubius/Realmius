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
    public partial class Initial : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo._RealmSyncStatus",
                c => new
                    {
                        MobilePrimaryKey = c.String(nullable: false, maxLength: 40),
                        Type = c.String(maxLength: 40),
                        IsDeleted = c.Boolean(nullable: false),
                        LastChange = c.DateTimeOffset(nullable: false, precision: 7),
                        FullObjectAsJson = c.String(),
                        Tag0 = c.String(maxLength: 40),
                        Tag1 = c.String(maxLength: 40),
                        Tag2 = c.String(maxLength: 40),
                        Tag3 = c.String(maxLength: 40),
                        ColumnChangeDatesSerialized = c.String(),
                    })
                .PrimaryKey(t => t.MobilePrimaryKey)
                .Index(t => new { t.LastChange, t.Type, t.Tag0 }, name: "IX_Download0");
            
        }
        
        public override void Down()
        {
            DropIndex("dbo._RealmSyncStatus", "IX_Download0");
            DropTable("dbo._RealmSyncStatus");
        }
    }
}
