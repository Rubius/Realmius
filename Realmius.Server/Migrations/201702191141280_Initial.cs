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
