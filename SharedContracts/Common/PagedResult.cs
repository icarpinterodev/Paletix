namespace SharedContracts.Common;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages)
{
    public static PagedResult<T> Create(IReadOnlyList<T> items, PaginationQuery query, int totalItems)
    {
        var totalPages = totalItems == 0
            ? 0
            : (int)Math.Ceiling(totalItems / (double)query.PageSize);

        return new PagedResult<T>(items, query.Page, query.PageSize, totalItems, totalPages);
    }
}
