using webWarehouseInventoryManagement.DataAccess.Models;
using webWarehouseInventoryManagement.Models;

namespace webWarehouseInventoryManagement.DataAccess.Data
{
    public interface IListingFormService
    {
        Task<IEnumerable<ColorModel>> GetColors();
        Task<IEnumerable<CountryModel>> GetCountryOfOrigin();
        Task<IEnumerable<DesignTypeModel>> GetDesignType();
        Task<IEnumerable<SizeCategoryModel>> GetSizeCategories();
        Task<IEnumerable<SizeModel>> GetSizes(int idSizeCategory);
        Task<ResponseModel> AddProduct(ListingFormModel listingFormModel);
        //public string ReadListingProductFile();
        Task<ResponseModel> ReadListingProductFile(ListingFormModel listingFormModel);

        Task<ResponseModel> GetListingTemplateList();

        Task<ListingFormModel> GetTemplateDetails(string idListingProduct);
        Task<ResponseModel> GetColorsByProductType(string productType, string size);
        Task<ResponseModel> GetSizesByProductAndColors(string productType, List<string> colors, string size);
        Task<List<ColorModel>> GetColorsByProductTypeForEdit(string productType, string size);
    }
}
