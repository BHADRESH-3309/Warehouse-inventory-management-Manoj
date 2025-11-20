using Microsoft.AspNetCore.Mvc.Rendering;

namespace webWarehouseInventoryManagement.Models
{
    public class ProductListModel<T> : List<T>
    {
        public int CurrentPage { get;  set; }
        public int TotalPages { get;  set; }
        public int PageSize { get;  set; }
        public int TotalCount { get;  set; }
        public bool HasPrevious => CurrentPage > 1;
        public bool HasNext => CurrentPage < TotalPages;
        public IEnumerable<int> Pages { get; private set; }
        public SelectList PageSizeList { get; set; } = new SelectList(new[] { 40, 80, 120, 160 });

        public ProductListModel(List<T> items, int totalRecordsCount, int pageNumber, int pageSize, int maxPages)
        {
            TotalCount = totalRecordsCount;
            PageSize = pageSize;
            CurrentPage = pageNumber;
            TotalPages = (int)Math.Ceiling(totalRecordsCount / (double)pageSize);

            int startPage, endPage;
            if (TotalPages <= maxPages)
            {
                // total pages less than max so show all pages
                startPage = 1;
                endPage = TotalPages;
            }
            else
            {
                // total pages more than max so calculate start and end pages
                var maxPagesBeforeCurrentPage = (int)Math.Floor((decimal)maxPages / (decimal)2);
                var maxPagesAfterCurrentPage = (int)Math.Ceiling((decimal)maxPages / (decimal)2) - 1;
                if (CurrentPage <= maxPagesBeforeCurrentPage)
                {
                    // current page near the start
                    startPage = 1;
                    endPage = maxPages;
                }
                else if (CurrentPage + maxPagesAfterCurrentPage >= TotalPages)
                {
                    // current page near the end
                    startPage = TotalPages - maxPages + 1;
                    endPage = TotalPages;
                }
                else
                {
                    // current page somewhere in the middle
                    startPage = CurrentPage - maxPagesBeforeCurrentPage;
                    endPage = CurrentPage + maxPagesAfterCurrentPage;
                }
            }

            // create an array of pages that can be looped over
            Pages = Enumerable.Range(startPage, (endPage + 1) - startPage);

          
            AddRange(items);
        }
        public static ProductListModel<T> ToPagedList(IEnumerable<T> source, int pageNumber, int pageSize, int totalRecordsCount, int MaxPages)
        {
            return new ProductListModel<T>(source.ToList(), totalRecordsCount, pageNumber, pageSize, MaxPages);
        }

    }
}
