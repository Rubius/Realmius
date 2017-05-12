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
