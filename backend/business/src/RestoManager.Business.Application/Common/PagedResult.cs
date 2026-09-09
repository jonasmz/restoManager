namespace RestoManager.Business.Application.Common;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);

public sealed record PageRequest(int Page = 1, int PageSize = 20)
{
    public int Skip => (Math.Max(1, Page) - 1) * ClampedSize;
    public int Take => ClampedSize;
    private int ClampedSize => Math.Clamp(PageSize, 1, 100);
}
