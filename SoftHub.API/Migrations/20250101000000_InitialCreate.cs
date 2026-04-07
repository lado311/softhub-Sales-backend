using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SoftHub.API.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FullName = table.Column<string>(nullable: false, defaultValue: ""),
                    Email = table.Column<string>(nullable: false, defaultValue: ""),
                    PasswordHash = table.Column<string>(nullable: false, defaultValue: ""),
                    Role = table.Column<string>(nullable: false, defaultValue: "Manager"),
                    IsActive = table.Column<bool>(nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_Users", x => x.Id));

            migrationBuilder.CreateTable(
                name: "Leads",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyName = table.Column<string>(nullable: false, defaultValue: ""),
                    Industry = table.Column<string>(nullable: false, defaultValue: ""),
                    ContactPersonName = table.Column<string>(nullable: false, defaultValue: ""),
                    ContactPersonPosition = table.Column<string>(nullable: false, defaultValue: ""),
                    Phone = table.Column<string>(nullable: false, defaultValue: ""),
                    Email = table.Column<string>(nullable: false, defaultValue: ""),
                    Location = table.Column<string>(nullable: false, defaultValue: ""),
                    CompanySize = table.Column<string>(nullable: false, defaultValue: "Small"),
                    Source = table.Column<string>(nullable: false, defaultValue: ""),
                    Status = table.Column<string>(nullable: false, defaultValue: "New"),
                    InterestLevel = table.Column<string>(nullable: false, defaultValue: "Medium"),
                    PotentialValue = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    NextFollowUpDate = table.Column<string>(nullable: true),
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    LastContactedAt = table.Column<DateTime>(nullable: true),
                    AssignedToId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leads", x => x.Id);
                    table.ForeignKey("FK_Leads_Users_AssignedToId", x => x.AssignedToId, "Users", "Id", onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Notes",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Text = table.Column<string>(nullable: false, defaultValue: ""),
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    LeadId = table.Column<int>(nullable: false),
                    AuthorId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notes", x => x.Id);
                    table.ForeignKey("FK_Notes_Leads_LeadId", x => x.LeadId, "Leads", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_Notes_Users_AuthorId", x => x.AuthorId, "Users", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ActivityLogs",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Action = table.Column<string>(nullable: false, defaultValue: ""),
                    Description = table.Column<string>(nullable: false, defaultValue: ""),
                    OldValue = table.Column<string>(nullable: true),
                    NewValue = table.Column<string>(nullable: true),
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    LeadId = table.Column<int>(nullable: false),
                    UserId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityLogs", x => x.Id);
                    table.ForeignKey("FK_ActivityLogs_Leads_LeadId", x => x.LeadId, "Leads", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_ActivityLogs_Users_UserId", x => x.UserId, "Users", "Id", onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Token = table.Column<string>(nullable: false, defaultValue: ""),
                    ExpiresAt = table.Column<DateTime>(nullable: false),
                    IsRevoked = table.Column<bool>(nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    UserId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey("FK_RefreshTokens_Users_UserId", x => x.UserId, "Users", "Id", onDelete: ReferentialAction.Cascade);
                });

            // Indexes
            migrationBuilder.CreateIndex("IX_Users_Email", "Users", "Email", unique: true);
            migrationBuilder.CreateIndex("IX_Leads_AssignedToId", "Leads", "AssignedToId");
            migrationBuilder.CreateIndex("IX_Notes_LeadId", "Notes", "LeadId");
            migrationBuilder.CreateIndex("IX_Notes_AuthorId", "Notes", "AuthorId");
            migrationBuilder.CreateIndex("IX_ActivityLogs_LeadId", "ActivityLogs", "LeadId");
            migrationBuilder.CreateIndex("IX_ActivityLogs_UserId", "ActivityLogs", "UserId");
            migrationBuilder.CreateIndex("IX_RefreshTokens_UserId", "RefreshTokens", "UserId");

            // Seed admin user
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "FullName", "Email", "PasswordHash", "Role", "IsActive", "CreatedAt" },
                values: new object[] {
                    1, "Admin User", "admin@softhub.io",
                    "$2a$11$rBnqhGLTkHKMQvkKp5Q5.OqHJhm3lfNlTRnVfJmvJgpJmZm6RYXS2",
                    "Admin", true,
                    new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable("RefreshTokens");
            migrationBuilder.DropTable("ActivityLogs");
            migrationBuilder.DropTable("Notes");
            migrationBuilder.DropTable("Leads");
            migrationBuilder.DropTable("Users");
        }
    }
}
