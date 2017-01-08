namespace RealmTst.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Initial : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ChatMessages",
                c => new
                    {
                        Id = c.String(nullable: false, maxLength: 128),
                        Author = c.String(),
                        Text = c.String(),
                        DateTime = c.DateTimeOffset(nullable: false, precision: 7),
                        SyncState = c.Int(nullable: false),
                        LastChangeClient = c.DateTimeOffset(nullable: false, precision: 7),
                        LastChangeServer = c.DateTimeOffset(nullable: false, precision: 7),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.ChatMessages");
        }
    }
}
