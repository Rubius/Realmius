namespace RealmWeb.Migrations
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
                        MobilePrimaryKey = c.String(nullable: false, maxLength: 128),
                        Author = c.String(),
                        Text = c.String(),
                        Text2 = c.String(),
                        DateTime = c.DateTimeOffset(nullable: false, precision: 7),
                    })
                .PrimaryKey(t => t.MobilePrimaryKey);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.ChatMessages");
        }
    }
}
