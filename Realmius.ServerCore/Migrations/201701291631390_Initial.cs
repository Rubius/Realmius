namespace RealmSync.Server.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Initial : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.SyncStatusServerObjects",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        MobilePrimaryKey = c.String(maxLength: 40),
                        Type = c.String(maxLength: 40),
                        Version = c.Int(nullable: false),
                        LastChange = c.DateTimeOffset(nullable: false, precision: 7),
                        ChangesAsJson = c.String(),
                        FullObjectAsJson = c.String(),
                        Tag0 = c.String(maxLength: 40),
                        Tag1 = c.String(maxLength: 40),
                        Tag2 = c.String(maxLength: 40),
                        Tag3 = c.String(maxLength: 40),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => new { t.LastChange, t.Type, t.Tag0 }, name: "IX_Download0");
            
        }
        
        public override void Down()
        {
            DropIndex("dbo.SyncStatusServerObjects", "IX_Download0");
            DropTable("dbo.SyncStatusServerObjects");
        }
    }
}
