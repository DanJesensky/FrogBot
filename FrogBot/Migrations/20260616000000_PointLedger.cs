using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FrogBot.Migrations
{
    [ExcludeFromCodeCoverage]
    public partial class PointLedger : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserPointBalances",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Balance = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPointBalances", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "PointLedgerEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Delta = table.Column<long>(type: "bigint", nullable: false),
                    Reason = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointLedgerEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PointLedgerEntries_UserId",
                table: "PointLedgerEntries",
                column: "UserId");

            // Seed balances from existing vote history, excluding banned voters.
            migrationBuilder.Sql(@"
                INSERT INTO ""UserPointBalances"" (""UserId"", ""Balance"")
                SELECT ""ReceiverId""::bigint, SUM(""VoteType"")
                FROM ""Votes""
                WHERE ""VoterId"" NOT IN (SELECT ""UserId"" FROM ""BannedVoters"")
                GROUP BY ""ReceiverId"";");

            // Record the seeded balances as a single migration ledger entry per user.
            migrationBuilder.Sql(@"
                INSERT INTO ""PointLedgerEntries"" (""UserId"", ""Delta"", ""Reason"", ""Timestamp"", ""Note"")
                SELECT ""UserId"", ""Balance"", 7, NOW(), 'Initial balance from vote history'
                FROM ""UserPointBalances"";");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "PointLedgerEntries");
            migrationBuilder.DropTable(name: "UserPointBalances");
        }
    }
}
