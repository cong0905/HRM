namespace HRM.Next.Application.Common;

public sealed class RequestValidationException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public sealed record PageRequest
{
    public int Page { get; }
    public int PageSize { get; }
    public string Sort { get; }
    public int Offset => checked((Page - 1) * PageSize);

    private PageRequest(int page, int pageSize, string sort) => (Page, PageSize, Sort) = (page, pageSize, sort);

    public static PageRequest Create(int page = 1, int pageSize = 20, string sort = "name")
    {
        if (page < 1 || pageSize is < 1 or > 100 || (long)(page - 1) * pageSize > int.MaxValue)
            throw new RequestValidationException("pagination.invalid", "page phải từ 1; pageSize từ 1 đến 100 và offset không vượt giới hạn.");
        if (sort is not ("name" or "-name" or "createdAt" or "-createdAt"))
            throw new RequestValidationException("pagination.invalid_sort", "sort không được hỗ trợ.");
        return new PageRequest(page, pageSize, sort);
    }
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, long TotalCount)
{
    public long TotalPages => TotalCount / PageSize + (TotalCount % PageSize == 0 ? 0 : 1);
}
