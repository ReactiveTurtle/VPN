using System;
using System.Net;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace VpnPortal.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_log",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ActorType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ActorId = table.Column<long>(type: "bigint", nullable: true),
                    Action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IpAddress = table.Column<IPAddress>(type: "inet", nullable: true),
                    DetailsJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_log", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "superadmins",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Username = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastLoginAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_superadmins", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "vpn_users",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Username = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    MaxDevices = table.Column<int>(type: "integer", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastLoginAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeactivatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vpn_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "account_tokens",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    TokenHash = table.Column<string>(type: "text", nullable: false),
                    Purpose = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Used = table.Column<bool>(type: "boolean", nullable: false),
                    UsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByAdminId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account_tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_account_tokens_superadmins_CreatedByAdminId",
                        column: x => x.CreatedByAdminId,
                        principalTable: "superadmins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "trusted_devices",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    DeviceUuid = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DeviceName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    DeviceType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Platform = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FirstSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trusted_devices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_trusted_devices_vpn_users_UserId",
                        column: x => x.UserId,
                        principalTable: "vpn_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vpn_requests",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    RequestedByIp = table.Column<IPAddress>(type: "inet", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ProcessedByAdminId = table.Column<long>(type: "bigint", nullable: true),
                    ApprovedUserId = table.Column<long>(type: "bigint", nullable: true),
                    AdminComment = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vpn_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vpn_requests_superadmins_ProcessedByAdminId",
                        column: x => x.ProcessedByAdminId,
                        principalTable: "superadmins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vpn_requests_vpn_users_ApprovedUserId",
                        column: x => x.ApprovedUserId,
                        principalTable: "vpn_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ip_change_confirmations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    DeviceId = table.Column<long>(type: "bigint", nullable: true),
                    RequestedIp = table.Column<IPAddress>(type: "inet", nullable: false),
                    TokenHash = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConfirmedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ip_change_confirmations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ip_change_confirmations_trusted_devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "trusted_devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ip_change_confirmations_vpn_users_UserId",
                        column: x => x.UserId,
                        principalTable: "vpn_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "trusted_ips",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    DeviceId = table.Column<long>(type: "bigint", nullable: true),
                    IpAddress = table.Column<IPAddress>(type: "inet", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FirstSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trusted_ips", x => x.Id);
                    table.ForeignKey(
                        name: "FK_trusted_ips_trusted_devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "trusted_devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_trusted_ips_vpn_users_UserId",
                        column: x => x.UserId,
                        principalTable: "vpn_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vpn_device_credentials",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    DeviceId = table.Column<long>(type: "bigint", nullable: false),
                    VpnUsername = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    RadiusNtHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RotatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastUsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vpn_device_credentials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vpn_device_credentials_trusted_devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "trusted_devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_vpn_device_credentials_vpn_users_UserId",
                        column: x => x.UserId,
                        principalTable: "vpn_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vpn_sessions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    DeviceId = table.Column<long>(type: "bigint", nullable: true),
                    SourceIp = table.Column<IPAddress>(type: "inet", nullable: false),
                    AssignedVpnIp = table.Column<IPAddress>(type: "inet", nullable: true),
                    NasIdentifier = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    SessionId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EndedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TerminationReason = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    Authorized = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vpn_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vpn_sessions_trusted_devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "trusted_devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_vpn_sessions_vpn_users_UserId",
                        column: x => x.UserId,
                        principalTable: "vpn_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_account_tokens_CreatedByAdminId",
                table: "account_tokens",
                column: "CreatedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_account_tokens_TokenHash",
                table: "account_tokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_account_tokens_user_email_purpose",
                table: "account_tokens",
                columns: new[] { "UserEmail", "Purpose" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_log_created_at",
                table: "audit_log",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ip_change_confirmations_DeviceId",
                table: "ip_change_confirmations",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_ip_change_confirmations_TokenHash",
                table: "ip_change_confirmations",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ip_change_confirmations_user_id_status",
                table: "ip_change_confirmations",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_superadmins_Username",
                table: "superadmins",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_trusted_devices_user_id_status",
                table: "trusted_devices",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_trusted_devices_UserId_DeviceUuid",
                table: "trusted_devices",
                columns: new[] { "UserId", "DeviceUuid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_trusted_ips_DeviceId",
                table: "trusted_ips",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "ix_trusted_ips_user_id_status",
                table: "trusted_ips",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_vpn_device_credentials_DeviceId",
                table: "vpn_device_credentials",
                column: "DeviceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vpn_device_credentials_user_id_status",
                table: "vpn_device_credentials",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_vpn_device_credentials_VpnUsername",
                table: "vpn_device_credentials",
                column: "VpnUsername",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vpn_requests_ApprovedUserId",
                table: "vpn_requests",
                column: "ApprovedUserId");

            migrationBuilder.CreateIndex(
                name: "ix_vpn_requests_email",
                table: "vpn_requests",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_vpn_requests_ProcessedByAdminId",
                table: "vpn_requests",
                column: "ProcessedByAdminId");

            migrationBuilder.CreateIndex(
                name: "ix_vpn_requests_status_submitted_at",
                table: "vpn_requests",
                columns: new[] { "Status", "SubmittedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_vpn_sessions_active",
                table: "vpn_sessions",
                column: "Active");

            migrationBuilder.CreateIndex(
                name: "IX_vpn_sessions_DeviceId",
                table: "vpn_sessions",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "ix_vpn_sessions_user_id_started_at",
                table: "vpn_sessions",
                columns: new[] { "UserId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "ux_vpn_sessions_session_id",
                table: "vpn_sessions",
                column: "SessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vpn_users_Email",
                table: "vpn_users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vpn_users_Username",
                table: "vpn_users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account_tokens");

            migrationBuilder.DropTable(
                name: "audit_log");

            migrationBuilder.DropTable(
                name: "ip_change_confirmations");

            migrationBuilder.DropTable(
                name: "trusted_ips");

            migrationBuilder.DropTable(
                name: "vpn_device_credentials");

            migrationBuilder.DropTable(
                name: "vpn_requests");

            migrationBuilder.DropTable(
                name: "vpn_sessions");

            migrationBuilder.DropTable(
                name: "superadmins");

            migrationBuilder.DropTable(
                name: "trusted_devices");

            migrationBuilder.DropTable(
                name: "vpn_users");
        }
    }
}
