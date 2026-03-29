using Dapper;
using VpnPortal.Application.Interfaces;
using VpnPortal.Domain.Entities;
using VpnPortal.Domain.Enums;

namespace VpnPortal.Infrastructure.Persistence.PostgreSql;

public sealed class PostgreSqlRequestRepository(PostgreSqlConnectionFactory connectionFactory) : IRequestRepository
{
    public async Task<VpnRequest?> GetLatestPendingByEmailAsync(string email, CancellationToken cancellationToken)
    {
        const string sql = """
            select id,
                   email,
                   name,
                   requested_by_ip as RequestedByIp,
                   status,
                   admin_comment as AdminComment,
                   submitted_at as SubmittedAt,
                   processed_at as ProcessedAt
            from vpn_requests
            where email = @Email and status = 'pending'
            order by submitted_at desc
            limit 1;
            """;

        using var connection = connectionFactory.Create();
        var row = await connection.QuerySingleOrDefaultAsync<RequestRow>(new CommandDefinition(sql, new { Email = email }, cancellationToken: cancellationToken));
        return row?.ToEntity();
    }

    public async Task<VpnRequest> AddAsync(VpnRequest request, CancellationToken cancellationToken)
    {
        const string sql = """
            insert into vpn_requests (email, name, requested_by_ip, status, submitted_at)
            values (@Email, @Name, cast(@RequestedByIp as inet), @Status, @SubmittedAt)
            returning id,
                      email,
                      name,
                      requested_by_ip as RequestedByIp,
                      status,
                      admin_comment as AdminComment,
                      submitted_at as SubmittedAt,
                      processed_at as ProcessedAt;
            """;

        using var connection = connectionFactory.Create();
        var row = await connection.QuerySingleAsync<RequestRow>(new CommandDefinition(sql, new
        {
            request.Email,
            request.Name,
            request.RequestedByIp,
            Status = ToStorageStatus(request.Status),
            request.SubmittedAt
        }, cancellationToken: cancellationToken));

        return row.ToEntity();
    }

    public async Task<IReadOnlyCollection<VpnRequest>> GetAllAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            select id,
                   email,
                   name,
                   requested_by_ip as RequestedByIp,
                   status,
                   admin_comment as AdminComment,
                   submitted_at as SubmittedAt,
                   processed_at as ProcessedAt
            from vpn_requests
            order by submitted_at desc;
            """;

        using var connection = connectionFactory.Create();
        var rows = await connection.QueryAsync<RequestRow>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return rows.Select(x => x.ToEntity()).ToArray();
    }

    public async Task<VpnRequest?> GetByIdAsync(int requestId, CancellationToken cancellationToken)
    {
        const string sql = """
            select id,
                   email,
                   name,
                   requested_by_ip as RequestedByIp,
                   status,
                   admin_comment as AdminComment,
                   submitted_at as SubmittedAt,
                   processed_at as ProcessedAt
            from vpn_requests
            where id = @RequestId;
            """;

        using var connection = connectionFactory.Create();
        var row = await connection.QuerySingleOrDefaultAsync<RequestRow>(new CommandDefinition(sql, new { RequestId = requestId }, cancellationToken: cancellationToken));
        return row?.ToEntity();
    }

    public async Task UpdateAsync(VpnRequest request, CancellationToken cancellationToken)
    {
        const string sql = """
            update vpn_requests
            set status = @Status,
                admin_comment = @AdminComment,
                processed_at = @ProcessedAt
            where id = @Id;
            """;

        using var connection = connectionFactory.Create();
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            request.Id,
            Status = ToStorageStatus(request.Status),
            request.AdminComment,
            request.ProcessedAt
        }, cancellationToken: cancellationToken));
    }

    private static string ToStorageStatus(RequestStatus status) => status.ToString().ToLowerInvariant();

    private sealed record RequestRow(
        int Id,
        string Email,
        string? Name,
        string? RequestedByIp,
        string Status,
        string? AdminComment,
        DateTimeOffset SubmittedAt,
        DateTimeOffset? ProcessedAt)
    {
        public VpnRequest ToEntity()
        {
            return new VpnRequest
            {
                Id = Id,
                Email = Email,
                Name = Name,
                RequestedByIp = RequestedByIp,
                Status = Enum.Parse<RequestStatus>(Status, true),
                AdminComment = AdminComment,
                SubmittedAt = SubmittedAt,
                ProcessedAt = ProcessedAt
            };
        }
    }
}
