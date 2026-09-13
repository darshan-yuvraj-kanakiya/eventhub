namespace EventHub.Api.Extensions;

public static class QueryableExtensions
{
    public static IQueryable<T> Page<T>(this IQueryable<T> query, int page, int size)
        => query.Skip((page - 1) * size).Take(size);
}
