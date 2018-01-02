using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Realmius.Server.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "_RealmSyncStatus",
                columns: table => new
                {
                    MobilePrimaryKey = table.Column<string>(maxLength: 40, nullable: false),
                    Type = table.Column<string>(maxLength: 40, nullable: false),
                    ColumnChangeDatesSerialized = table.Column<string>(nullable: true),
                    FullObjectAsJson = table.Column<string>(nullable: true),
                    IsDeleted = table.Column<bool>(nullable: false),
                    LastChange = table.Column<DateTimeOffset>(nullable: false),
                    Tag0 = table.Column<string>(maxLength: 40, nullable: true),
                    Tag1 = table.Column<string>(maxLength: 40, nullable: true),
                    Tag2 = table.Column<string>(maxLength: 40, nullable: true),
                    Tag3 = table.Column<string>(maxLength: 40, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__RealmSyncStatus", x => new { x.MobilePrimaryKey, x.Type });
                });

            migrationBuilder.CreateTable(
                name: "LogEntryBase",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    AfterJson = table.Column<string>(nullable: true),
                    BeforeJson = table.Column<string>(nullable: true),
                    ChangesJson = table.Column<string>(nullable: true),
                    EntityType = table.Column<string>(nullable: true),
                    RecordIdInt = table.Column<int>(nullable: false),
                    RecordIdString = table.Column<string>(maxLength: 40, nullable: true),
                    Time = table.Column<DateTimeOffset>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogEntryBase", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX__RealmSyncStatus_LastChange",
                table: "_RealmSyncStatus",
                column: "LastChange");

            migrationBuilder.CreateIndex(
                name: "IX__RealmSyncStatus_Tag0",
                table: "_RealmSyncStatus",
                column: "Tag0");

            migrationBuilder.CreateIndex(
                name: "IX_LogEntryBase_RecordIdInt",
                table: "LogEntryBase",
                column: "RecordIdInt");

            migrationBuilder.CreateIndex(
                name: "IX_LogEntryBase_RecordIdString",
                table: "LogEntryBase",
                column: "RecordIdString");

            migrationBuilder.CreateIndex(
                name: "IX_LogEntryBase_Time",
                table: "LogEntryBase",
                column: "Time");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "_RealmSyncStatus");

            migrationBuilder.DropTable(
                name: "LogEntryBase");
        }
    }
}
