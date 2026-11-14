using AgroMarket.Api.Common;
using AgroMarket.Api.Database;
using AgroMarket.Api.Database.Entities;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace AgroMarket.Api.Features.Admin;

public static class VerifyUser
{
    public sealed record Request(
        bool IsVerified,
        UserRole? Role = null);

    public sealed record AdminUserItem(
        Guid Id,
        string PhoneNumber,
        string FullName,
        UserRole Role,
        bool IsVerified,
        long? TelegramChatId,
        DateTimeOffset CreatedAt,
        string? BusinessName,
        string? Region);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Role)
                .IsInEnum()
                .When(x => x.Role.HasValue);
        }
    }

    public static async Task<Ok<PagedResult<AdminUserItem>>> HandleListUsers(
        AppDbContext db,
        UserRole? role = null,
        bool? isVerified = null,
        string? search = null,
        int? page = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        var (normalizedPage, normalizedSize) = PaginationDefaults.Normalize(page, pageSize);

        var query = db.Users.AsNoTracking();

        if (role.HasValue)
        {
            query = query.Where(u => u.Role == role.Value);
        }

        if (isVerified.HasValue)
        {
            query = query.Where(u => u.IsVerified == isVerified.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(u =>
                EF.Functions.ILike(u.FullName, term) ||
                EF.Functions.ILike(u.PhoneNumber, term) ||
                (u.BusinessProfile != null && EF.Functions.ILike(u.BusinessProfile.BusinessName, term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(u => u.IsVerified)
            .ThenByDescending(u => u.CreatedAt)
            .Skip((normalizedPage - 1) * normalizedSize)
            .Take(normalizedSize)
            .Select(u => new AdminUserItem(
                u.Id,
                u.PhoneNumber,
                u.FullName,
                u.Role,
                u.IsVerified,
                u.TelegramChatId,
                u.CreatedAt,
                u.BusinessProfile != null ? u.BusinessProfile.BusinessName : null,
                u.BusinessProfile != null ? u.BusinessProfile.Region : null))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(PagedResult<AdminUserItem>.Create(items, normalizedPage, normalizedSize, totalCount));
    }

    public static async Task<Results<Ok<AdminUserItem>, ProblemHttpResult>> HandleVerify(
        Guid id,
        Request request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var user = await db.Users
            .Include(u => u.BusinessProfile)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (user is null)
        {
            return TypedResults.Problem(
                title: "User not found",
                detail: $"User '{id}' was not found.",
                statusCode: StatusCodes.Status404NotFound);
        }

        user.IsVerified = request.IsVerified;
        if (request.Role.HasValue)
        {
            user.Role = request.Role.Value;
        }

        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(new AdminUserItem(
            user.Id,
            user.PhoneNumber,
            user.FullName,
            user.Role,
            user.IsVerified,
            user.TelegramChatId,
            user.CreatedAt,
            user.BusinessProfile?.BusinessName,
            user.BusinessProfile?.Region));
    }
}
