namespace RealmWeb.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Init : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ChatMessages",
                c => new
                    {
                        MobilePrimaryKey = c.String(nullable: false, maxLength: 128),
                        LastChangeServer = c.DateTime(nullable: false),
                        Author = c.String(),
                        Text = c.String(),
                        Text2 = c.String(),
                        DateTime = c.DateTimeOffset(nullable: false, precision: 7),
                    })
                .PrimaryKey(t => t.MobilePrimaryKey)
                .Index(t => new { t.MobilePrimaryKey, t.LastChangeServer }, name: "Sync");
            
        }
        
        public override void Down()
        {
            DropIndex("dbo.ChatMessages", "Sync");
            DropTable("dbo.ChatMessages");
        }
    }
}
