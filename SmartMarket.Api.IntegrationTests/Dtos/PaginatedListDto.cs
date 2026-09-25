namespace SmartMarket.Api.IntegrationTests.Dtos
{
    public class PaginatedListDto<T>
    {
        public IReadOnlyCollection<T> Items { get; set; }
        public int PageNumber { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}
