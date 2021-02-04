using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace StormBot.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CallOfDutyPlayerDataEntity",
                columns: table => new
                {
                    ServerID = table.Column<ulong>(type: "INTEGER", nullable: false),
                    DiscordID = table.Column<ulong>(type: "INTEGER", nullable: false),
                    GameAbbrev = table.Column<string>(type: "TEXT", nullable: false),
                    ModeAbbrev = table.Column<string>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", nullable: true),
                    Tag = table.Column<string>(type: "TEXT", nullable: true),
                    Platform = table.Column<string>(type: "TEXT", nullable: true),
                    TotalKills = table.Column<double>(type: "REAL", nullable: false),
                    TotalWins = table.Column<double>(type: "REAL", nullable: false),
                    WeeklyKills = table.Column<double>(type: "REAL", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CallOfDutyPlayerDataEntity", x => new { x.ServerID, x.DiscordID, x.GameAbbrev, x.ModeAbbrev });
                });

            migrationBuilder.CreateTable(
                name: "ServersEntity",
                columns: table => new
                {
                    ServerID = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ServerName = table.Column<string>(type: "TEXT", nullable: true),
                    PrefixUsed = table.Column<string>(type: "TEXT", nullable: true),
                    AllowServerPermissionBlackOpsColdWarTracking = table.Column<bool>(type: "INTEGER", nullable: false),
                    ToggleBlackOpsColdWarTracking = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowServerPermissionModernWarfareTracking = table.Column<bool>(type: "INTEGER", nullable: false),
                    ToggleModernWarfareTracking = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowServerPermissionWarzoneTracking = table.Column<bool>(type: "INTEGER", nullable: false),
                    ToggleWarzoneTracking = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowServerPermissionSoundpadCommands = table.Column<bool>(type: "INTEGER", nullable: false),
                    ToggleSoundpadCommands = table.Column<bool>(type: "INTEGER", nullable: false),
                    CallOfDutyNotificationChannelID = table.Column<ulong>(type: "INTEGER", nullable: false),
                    SoundboardNotificationChannelID = table.Column<ulong>(type: "INTEGER", nullable: false),
                    AdminRoleID = table.Column<ulong>(type: "INTEGER", nullable: false),
                    WarzoneWinsRoleID = table.Column<ulong>(type: "INTEGER", nullable: false),
                    WarzoneKillsRoleID = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ModernWarfareKillsRoleID = table.Column<ulong>(type: "INTEGER", nullable: false),
                    BlackOpsColdWarKillsRoleID = table.Column<ulong>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServersEntity", x => x.ServerID);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CallOfDutyPlayerDataEntity");

            migrationBuilder.DropTable(
                name: "ServersEntity");
        }
    }
}
