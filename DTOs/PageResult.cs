namespace IDV_Templates_Mongo_API.DTOs;
public class PageResult<T>
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public long Total { get; set; }
    public List<T> Items { get; set; } = new();
}
