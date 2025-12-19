using System.Net;
using OfficeOpenXml;
using Newtonsoft.Json;
using Microsoft.Extensions.Options;
using webWarehouseInventoryManagement.Models;
using webWarehouseInventoryManagement.DataAccess.DbAccess;
using webWarehouseInventoryManagement.DataAccess.Models;

namespace webWarehouseInventoryManagement.DataAccess.Data
{
    public class ListingFormService : IListingFormService
    {
        public readonly ISqlDataAccess _sqlDataAccess;
        public  IConfiguration _config;
        private readonly AmazonSheetDefaults _amazonSheetDefaults;

        public ListingFormService(ISqlDataAccess sqlDataAccess, IConfiguration config, IOptions<AmazonSheetDefaults> amazonDefaults)
        {
            _sqlDataAccess = sqlDataAccess;
            _config = config;
            _amazonSheetDefaults = amazonDefaults.Value;
        }

        //Colors
        public async Task<IEnumerable<ColorModel>> GetColors()
        {
            IEnumerable<ColorModel> colorList = new List<ColorModel>();

            string query = string.Empty;

            query = $@"SELECT idColor,Color,ColorMap FROM tblColor ORDER BY Color ASC ";

            colorList = await _sqlDataAccess.GetData<ColorModel, dynamic>(query, new { });

            return colorList;
        }
        //CountryOfOrigin
        public async Task<IEnumerable<CountryModel>> GetCountryOfOrigin()
        {
            IEnumerable<CountryModel> countryList = new List<CountryModel>();

            string query = string.Empty;

            query = $@"SELECT idCountryOfOrigin,Country FROM tblCountryOfOrigin ORDER BY Country ASC ";

            countryList = await _sqlDataAccess.GetData<CountryModel, dynamic>(query, new { });

            return countryList;
        }

        //Design Type
        public async Task<IEnumerable<DesignTypeModel>> GetDesignType()
        {
            IEnumerable<DesignTypeModel> designTypeList = new List<DesignTypeModel>();

            string query = string.Empty;

            query = $@"SELECT idListingDesignType,DesignType FROM tblListingDesignType ORDER BY DesignType ASC ";

            designTypeList = await _sqlDataAccess.GetData<DesignTypeModel, dynamic>(query, new { });

            return designTypeList;
        }

        // Get Size Categories (Adults, Kids)
        public async Task<IEnumerable<SizeCategoryModel>> GetSizeCategories()
        {
            IEnumerable<SizeCategoryModel> sizeCategoryList = new List<SizeCategoryModel>();

            string query = @"SELECT idSizeCategory, SizeCategory FROM tblSizeCategory ORDER BY SizeCategory ASC";

            sizeCategoryList =  await _sqlDataAccess.GetData<SizeCategoryModel, dynamic>(query, new { });
            return sizeCategoryList;
        }

        // Get Sizes by Category
        public async Task<IEnumerable<SizeModel>> GetSizes(int idSizeCategory)
        {

            IEnumerable<SizeModel> sizeList = new List<SizeModel>();

            string query = @"SELECT idSize, idSizeCategory, SizeName,SizeMap FROM tblSizes 
                             WHERE idSizeCategory = @idSizeCategory ORDER BY idSize ASC";

            sizeList =  await _sqlDataAccess.GetData<SizeModel, dynamic>(query, new { idSizeCategory = idSizeCategory });
            return sizeList;
        }
        #region Generate template section
        // AddProduct
        public async Task<ResponseModel> AddProduct(ListingFormModel listingFormModel)
        {
            ResponseModel response = new ResponseModel();
            try
            {
                if (listingFormModel.idListingProduct != Guid.Empty)
                {
                    await _sqlDataAccess.ExecuteDML($@"DELETE FROM tblListingProduct WHERE idListingProduct = '{listingFormModel.idListingProduct}'", new {});
                }

                // if Change Fixed Price: No then implementation of add direct prices of 
                if (!string.IsNullOrEmpty(listingFormModel.PriceChange) && listingFormModel.PriceChange == "No")
                {
                    decimal DefaultPrices_Adults =  Convert.ToDecimal(_config["DefaultPrices:AdultPrice"]);
                    decimal DefaultPrices_KidsPrice = Convert.ToDecimal(_config["DefaultPrices:KidsPrice"]);
                    if (listingFormModel.Size == "Kids")
                    {
                        listingFormModel.KidsPrice = DefaultPrices_KidsPrice;
                    }
                    else if (listingFormModel.Size == "Adults")
                    {
                        listingFormModel.AdultPrice = DefaultPrices_Adults;
                    }
                    else
                    {
                        listingFormModel.KidsPrice = DefaultPrices_KidsPrice;
                        listingFormModel.AdultPrice = DefaultPrices_Adults;
                    }
                }

                listingFormModel.BulletPoints = JsonConvert.SerializeObject(new Dictionary<string, string>
                                            {
                                                { "Bp1", listingFormModel.Bp1 ?? "" },
                                                { "Bp2", listingFormModel.Bp2 ?? "" },
                                                { "Bp3", listingFormModel.Bp3 ?? "" },
                                                { "Bp4", listingFormModel.Bp4 ?? "" },
                                                { "Bp5", listingFormModel.Bp5 ?? "" }
                                            });

                // Example: Model.Colors = List<ColorModel> with idColor & Color
                string coloursJson = string.Empty;
                if(listingFormModel.Colour.FirstOrDefault() == "all")
                {
                    coloursJson = "All";
                }
                else
                {
                    var selectedColorNames = listingFormModel.Colors
                    .Where(c => listingFormModel.Colour.Contains(c.idColor.ToString()))
                    .Select(c => c.Color)
                    .ToList();
                    // Serialize names to JSON string
                    coloursJson = JsonConvert.SerializeObject(selectedColorNames);
                }
                

                var country = listingFormModel.Countrys
                           .FirstOrDefault(c => c.idCountryOfOrigin.ToString() == listingFormModel.CountryOfOrigin)
                           ?.Country;

                var designType = listingFormModel.DesignTypes
                          .FirstOrDefault(c => c.idListingDesignType.ToString() == listingFormModel.DesignType)
                          ?.DesignType;

                string adultPriceSql = listingFormModel.AdultPrice.HasValue
                                     ? listingFormModel.AdultPrice.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)
                                     : "0";

                string kidsPriceSql = listingFormModel.KidsPrice.HasValue
                                    ? listingFormModel.KidsPrice.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)
                                    : "0";

                string productCostSql = listingFormModel.ProductCost.HasValue
                                        ? listingFormModel.ProductCost.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)
                                        : "0";

                listingFormModel.idListingProduct = Guid.NewGuid();

                // add product data into tblListingProduct

                string listingProductquery = @"INSERT INTO tblListingProduct (idListingProduct, ProductType, NumberOfStyles, Colour, 
                                SizeType, CategoryName,StyleOption, PriceChange, AdultPrice, KidsPrice, CountryOfOrigin, idCountryOfOrigin,
                                Title, Description,BulletPoints, SearchTerms, DesignType, idListingDesignType, ProductCost) 
                                VALUES (@idListingProduct, @ProductType, @NumberOfStyles, @Colour, @SizeType, @CategoryName,
                                 @StyleOption, @PriceChange, @AdultPrice, @KidsPrice, @CountryOfOrigin, @idCountryOfOrigin, @Title, @Description,
                                 @BulletPoints, @SearchTerms, @DesignType, @idListingDesignType, @ProductCost)";

                await _sqlDataAccess.ExecuteDML(listingProductquery, new
                {
                    idListingProduct = listingFormModel.idListingProduct,
                    ProductType = listingFormModel.ListingProduct,
                    NumberOfStyles = listingFormModel.NumberOfStyles,
                    Colour = coloursJson,
                    SizeType = listingFormModel.Size,
                    CategoryName = listingFormModel.CategoryName.Trim(),
                    StyleOption = listingFormModel.StyleNameOption,
                    PriceChange = listingFormModel.PriceChange,
                    AdultPrice = listingFormModel.AdultPrice ?? 0,
                    KidsPrice = listingFormModel.KidsPrice ?? 0,
                    CountryOfOrigin = country,
                    idCountryOfOrigin = listingFormModel.CountryOfOrigin,
                    Title = listingFormModel.Title,
                    Description = listingFormModel.Description,
                    BulletPoints = listingFormModel.BulletPoints,
                    SearchTerms = listingFormModel.SearchTerms,
                    DesignType = designType,
                    idListingDesignType = listingFormModel.DesignType,
                    ProductCost = listingFormModel.ProductCost ?? 0
                });
                // end

                //  add product and styles  into tblListingStyle

                // Common list for final styles to insert
                List<string> finalStyleNames = new List<string>();

                // Case 1: User provides new style names
                // Only if StyleOption = "New"
                if (listingFormModel.StyleNameOption == "New" && listingFormModel.StyleNames != null && listingFormModel.StyleNames.Any())
                {
                    finalStyleNames = listingFormModel.StyleNames;
                }
                // Case 2: Auto-generate style names if option is "None"
                else if (listingFormModel.StyleNameOption == "None"
                         && !string.IsNullOrEmpty(listingFormModel.CategoryName)
                         && listingFormModel.NumberOfStyles > 0)
                {
                    int numberOfStyles = listingFormModel.NumberOfStyles ?? 0;

                    finalStyleNames = Enumerable.Range(1, numberOfStyles)
                                    .Select(i => $"{listingFormModel.CategoryName} {i:D2}") // e.g. "Shirt 01", "Shirt 02"
                                    .ToList();
                }

                // Insert into DB if we have styles
                if (finalStyleNames.Any())
                {
                    int styleCounter = 1;

                    foreach (var styleName in finalStyleNames)
                    {
                        Guid styleId = Guid.NewGuid(); // generate unique ID for style

                        string styleQuery = @"INSERT INTO tblListingStyle (idListingStyle, idListingProduct, StyleNo, StyleName, DateAdd)
                              VALUES (@idListingStyle, @idListingProduct, @StyleNo, @StyleName, @DateAdd)";

                        await _sqlDataAccess.ExecuteDML(styleQuery, new
                        {
                            idListingStyle = styleId,
                            idListingProduct = listingFormModel.idListingProduct,
                            StyleNo = styleCounter,
                            StyleName = styleName,
                            DateAdd = DateTime.Now
                        });

                        // Now insert images that belong to this style
                        if (listingFormModel.StyleImages != null && listingFormModel.StyleImages.Count >= styleCounter)
                        {
                            var image = listingFormModel.StyleImages[styleCounter - 1]; // match by index
                            
                            if(image.ColorImages.Count > 0)
                            {
                                foreach (var colorImage in image.ColorImages) // Each color under that style
                                {
                                    Guid imageId = Guid.NewGuid();

                                    string imageQuery = @"INSERT INTO tblListingImage (idListingImage, idListingProduct, idListingStyle, idColor,
                                                        Color,MainImageUrl, OtherImageUrlAdult,OtherImageUrlKids)
                                                       VALUES (@idListingImage, @idListingProduct, @idListingStyle,@idColor,@Color, @MainImageUrl, @OtherImageUrlAdult,@OtherImageUrlKids)";

                                    await _sqlDataAccess.ExecuteDML(imageQuery, new
                                    {
                                        idListingImage = imageId,
                                        idListingProduct = listingFormModel.idListingProduct,
                                        idListingStyle = styleId,
                                        idColor = colorImage.idColor,
                                        Color = colorImage.ColorName,
                                        MainImageUrl = colorImage.MainImage,
                                        OtherImageUrlAdult = image.OtherImage,
                                        OtherImageUrlKids = (listingFormModel.Size.ToLower() == "adults")? null:image.OtherImageUrlKids,
                                    });
                                }
                            }

                            //string imageQuery = @"INSERT INTO tblListingImage (idListingImage, idListingProduct, idListingStyle, MainImageUrl, OtherImageUrlAdult)
                            //      VALUES (@idListingImage, @idListingProduct, @idListingStyle, @MainImageUrl, @OtherImageUrlAdult)";

                            //await _sqlDataAccess.ExecuteDML(imageQuery, new
                            //{
                            //    idListingImage = imageId,
                            //    idListingProduct = listingFormModel.idListingProduct,
                            //    idListingStyle = styleId,
                            //   // MainImageUrl = image.MainImage,
                            //    OtherImageUrlAdult = image.OtherImage
                            //});
                        }

                        styleCounter++;
                    }

                    listingFormModel.FinalStyleNames = finalStyleNames;
                }
                // end

                // add product and sizes into tblListingSize
                listingFormModel.Sizes = (await GetSizeList()).ToList();
                var sizeCategories = await GetSizeCategories();

                if (listingFormModel.KidsSize != null)
                {
                    if(listingFormModel.KidsSize.FirstOrDefault() == "all")
                    {

                        // Load Adults and Kids sizes separately
                        var kidsCategory = sizeCategories.FirstOrDefault(x => x.SizeCategory == "Kids");

                        var kidsSizes = kidsCategory != null ? await GetSizes(kidsCategory.idSizeCategory) : new List<SizeModel>();
                        
                        foreach (var kidsSize in kidsSizes)
                        {

                            string sizeQuery = $@"INSERT INTO tblListingSize (idListingProduct,SizeCategory,SizeName,SizeMap)
                                              VALUES (@idListingProduct, @SizeCategory, @SizeName, @SizeMap)";
                            await _sqlDataAccess.ExecuteDML(sizeQuery, new
                            {
                                idListingProduct = listingFormModel.idListingProduct,
                                SizeCategory = "Kids",
                                SizeName = kidsSize?.SizeName,
                                SizeMap = kidsSize?.SizeMap
                            });
                        }
                    }
                    else
                    {
                        foreach (var kidsSize in listingFormModel.KidsSize)
                        {
                            var sizeInfo = listingFormModel.Sizes.FirstOrDefault(s => s.SizeName == kidsSize);

                            string sizeQuery = $@"INSERT INTO tblListingSize (idListingProduct,SizeCategory,SizeName,SizeMap)
                                              VALUES (@idListingProduct, @SizeCategory, @SizeName, @SizeMap)";
                            await _sqlDataAccess.ExecuteDML(sizeQuery, new
                            {
                                idListingProduct = listingFormModel.idListingProduct,
                                SizeCategory = "Kids",
                                SizeName = kidsSize,
                                SizeMap = sizeInfo?.SizeMap
                            });
                        }
                    }
                }
                if (listingFormModel.AdultSize != null && listingFormModel.AdultSize.Count > 0)
                {
                    if (listingFormModel.AdultSize.FirstOrDefault() == "all")
                    {
                        // Load Adults and Kids sizes separately
                        var adultCategory = sizeCategories.FirstOrDefault(x => x.SizeCategory == "Adults");

                        var adultSizes = adultCategory != null ? await GetSizes(adultCategory.idSizeCategory) : new List<SizeModel>();

                        foreach (var adultSize in adultSizes)
                        {

                            string sizeQuery = $@"INSERT INTO tblListingSize (idListingProduct,SizeCategory,SizeName,SizeMap)
                                              VALUES (@idListingProduct, @SizeCategory, @SizeName, @SizeMap)";
                            await _sqlDataAccess.ExecuteDML(sizeQuery, new
                            {
                                idListingProduct = listingFormModel.idListingProduct,
                                SizeCategory = "Adults",
                                SizeName = adultSize.SizeName,
                                SizeMap = adultSize?.SizeMap
                            });
                        }
                    }
                    else
                    {
                        foreach (var adultSize in listingFormModel.AdultSize)
                        {
                            var sizeInfo = listingFormModel.Sizes.FirstOrDefault(s => s.SizeName == adultSize);

                            string sizeQuery = $@"INSERT INTO tblListingSize (idListingProduct,SizeCategory,SizeName,SizeMap)
                                              VALUES (@idListingProduct, @SizeCategory, @SizeName, @SizeMap)";
                            await _sqlDataAccess.ExecuteDML(sizeQuery, new
                            {
                                idListingProduct = listingFormModel.idListingProduct,
                                SizeCategory = "Adults",
                                SizeName = adultSize,
                                SizeMap = sizeInfo?.SizeMap
                            });
                        }
                    }
                }
                // end

                // add product and chose color into tblListingColor
                if (listingFormModel.Colour != null && listingFormModel.Colour.Count > 0)
                {
                    if (listingFormModel.Colour.FirstOrDefault() == "all")
                    {
                        foreach (var color in listingFormModel.Colors)
                        {
                            var idColor = color.idColor;
                            var colorName  = color.Color;

                            string colorquery = $@"INSERT INTO tblListingColor(idListingProduct,idColor,Color)
                                               VALUES ('{listingFormModel.idListingProduct}','{idColor}','{colorName}')";

                            await _sqlDataAccess.ExecuteDML(colorquery, new { });
                        }
                    }
                    else
                    {
                        foreach (var color in listingFormModel.Colour)
                        {
                            var colorValue = color;
                            var colorName = listingFormModel.Colors
                              .FirstOrDefault(c => c.idColor.ToString() == colorValue)
                              ?.Color;

                            string colorquery = $@"INSERT INTO tblListingColor(idListingProduct,idColor,Color)
                                               VALUES ('{listingFormModel.idListingProduct}','{color}','{colorName}')";

                            await _sqlDataAccess.ExecuteDML(colorquery, new { });
                        }
                    }
                }
                // end

                response.IsError = false;
                return response;
            }
            catch (Exception ex)
            {
                response.IsError = true;
                response.Message = ex.Message;
                return response;
            }
        }

        public async Task<ResponseModel> ReadListingProductFile(ListingFormModel listingFormModel)
        {
            ResponseModel response = new ResponseModel();
            string fileName = string.Empty;
            string sheetId = string.Empty;
            string filePrefix = string.Empty;
            try
            {
                string productType = listingFormModel.ListingProduct;

                string saveFileFolderPath = _config["Files:FileFolderPath"];
                string saveFileFolderId ="";
                //string exportUrl = $"https://docs.google.com/spreadsheets/d/";
                string tshirtPoloFileId = _config["Files:TShirtPoloFileId"];
                string hoodieFileId = _config["Files:HoodieFileId"];
                string sweatshirtFileId = _config["Files:SweatshirtFileId"];

                // Decide which file based on type
                switch (productType)
                {
                    case "TShirt":
                        sheetId = tshirtPoloFileId;
                        filePrefix = "TShirt";
                        break;
                    case "Polo":
                        sheetId = tshirtPoloFileId;
                        filePrefix = "Polo";
                        break;
                    case "Hoodie":
                        sheetId = hoodieFileId;
                        filePrefix = "Hoodie";
                        break;
                    case "Sweatshirt":
                        sheetId = sweatshirtFileId;
                        filePrefix = "Sweatshirt";
                        break;
                    default:
                        throw new Exception("Invalid fileType passed.");
                }

                // ✅ Use Drive download link for uploaded Excel files
                string exportUrl = $"https://drive.google.com/uc?export=download&id={sheetId}";
                string downloadedFile = Path.Combine(saveFileFolderPath, $"{filePrefix}_Source.xlsm");

                using (var client = new WebClient())
                {
                    byte[] data = client.DownloadData(exportUrl);
                    File.WriteAllBytes(downloadedFile, data);
                }

                using (var package = new ExcelPackage(new FileInfo(downloadedFile)))
                {
                    // Get the template sheet
                    var templateSheet = package.Workbook.Worksheets["Template"];
                    if (templateSheet == null)
                        throw new Exception("Template sheet not found in file");

                    // ✅ Remove all other worksheets except Template
                    for (int i = package.Workbook.Worksheets.Count; i >= 1; i--)
                    {
                        var sheet = package.Workbook.Worksheets[i - 1]; // index is 0-based
                        if (sheet.Name != "Template")
                        {
                            package.Workbook.Worksheets.Delete(i - 1);
                        }
                    }

                    int totalRows = templateSheet.Dimension.End.Row;
                    if (totalRows > 3)
                    {
                        templateSheet.DeleteRow(4, totalRows - 3);
                    }

                    fileName = $"{listingFormModel.CategoryName}_Template.xlsm";

                    string resultFilePath = Path.Combine(saveFileFolderPath, fileName);
                    package.SaveAs(new FileInfo(resultFilePath));
                }

                // Delete source file after successful save
                if (File.Exists(downloadedFile))
                {
                    File.Delete(downloadedFile);
                }

                // update listing file 
                response = await UpdateListingProductFile(listingFormModel);

                if (response.IsError)
                {
                    response.IsError = true;
                    response.Message = response.Message;
                    response.fileName = null;
                    return response;
                }

                response.IsError = false;
                response.fileName = fileName;
                return response;
            }
            catch (Exception ex)
            {
                response.IsError = true;
                response.Message = ex.Message;
                response.fileName = null;
                return response;
            }
        }

        public async Task<ResponseModel> UpdateListingProductFile(ListingFormModel listingFormModel)
        {
            ResponseModel response = new ResponseModel();
            try
            {
                // get country name from country id
                var country = listingFormModel.Countrys
                           .FirstOrDefault(c => c.idCountryOfOrigin.ToString() == listingFormModel.CountryOfOrigin)
                           ?.Country;
                listingFormModel.CountryName = country;

                string parentSKU =  MakeParentSKU(listingFormModel.CategoryName, listingFormModel.ListingProduct);


                string parentSKUQuery = $@"INSERT INTO tblListingSKU (idListingProduct, SkuType, ParentSKU, Title,CountryOfOrigin)  
                                            VALUES (@idListingProduct, @SkuType, @ParentSKU,@Title, @CountryOfOrigin)";


                await _sqlDataAccess.ExecuteDML(parentSKUQuery, new
                {
                    idListingProduct = listingFormModel.idListingProduct,
                    SkuType = "Parent",
                    ParentSKU = parentSKU,
                    Title = listingFormModel.Title,
                    CountryOfOrigin = country
                });

                var allChildSKUs = MakeAllChildSKUs(listingFormModel, listingFormModel.FinalStyleNames);

                listingFormModel.ParentSKU = parentSKU;

                GetBrowseNodes(listingFormModel);

                //update sheet data
                UpdateAmazonSheet(allChildSKUs, parentSKU, listingFormModel);

                // date insert into database
                foreach (var childSKU in allChildSKUs)
                {
                    string insertQuery = @"INSERT INTO tblListingSKU (idListingProduct, idListingStyle, idListingColor, 
                                            idListingSize, SkuType, ParentSKU, SKU, Title, SizeCategory, SizeName, SizeMap, Color, SalePrice, 
                                            MainImageUrl, OtherImageUrlAdult,OtherImageUrlKids, CountryOfOrigin)  VALUES 
                                            (@idListingProduct, @idListingStyle, @idListingColor, @idListingSize,
                                             @SkuType, @ParentSKU, @SKU, @Title, @SizeCategory, @SizeName, @SizeMap, @Color, @SalePrice,
                                             @MainImageUrl, @OtherImageUrlAdult,@OtherImageUrlKids, @CountryOfOrigin)";

                    await _sqlDataAccess.ExecuteDML(insertQuery, new
                    {
                        idListingProduct = listingFormModel.idListingProduct,
                        idListingStyle = GetStyleIdFromDatabase(listingFormModel.idListingProduct, childSKU.StyleNo),
                        idListingColor = GetColorIdFromDatabase(listingFormModel.idListingProduct, childSKU.ColorId),
                        idListingSize = GetSizeIdFromDatabase(listingFormModel.idListingProduct, childSKU.SizeName),
                        SkuType = "Child",
                        ParentSKU = parentSKU,
                        SKU = childSKU.SKU,
                        Title = childSKU.Title,
                        SizeCategory = childSKU.SizeCategory,
                        SizeName = childSKU.SizeName,
                        SizeMap = childSKU.SizeMap,
                        Color = childSKU.Color,
                        SalePrice = childSKU.SalePrice,
                        MainImageUrl = childSKU.MainImageUrl,
                        OtherImageUrlAdult = childSKU.OtherImageUrlAdult,
						OtherImageUrlKids = childSKU.OtherImageUrlKids,
                        CountryOfOrigin = country
                    });
                }

                response.IsError = false;
                return response;
            }
            catch (Exception ex)
            {
                response.IsError = true;
                response.Message = ex.Message;
                return response;
            }
        }
        public string MakeParentSKU(string categoryName , string listingProduct)
        {
            string parentSKU = string.Empty;
            string listingProductType = GetProductAbbreviation(listingProduct);

            // remove all spaces
            string cleanedCategoryName = categoryName.Replace(" ", "");

            parentSKU = cleanedCategoryName + "_" + listingProductType + "_" + "Parent";

            return parentSKU;
        }

        public List<ChildSKUInfo> MakeAllChildSKUs(ListingFormModel listingFormModel, List<string> finalStyleNames)
        {
            var allChildSKUs = new List<ChildSKUInfo>();

            try
            {
                // Get abbreviated category name (first 6 characters)
                string categoryAbbrev = GetCategoryAbbreviation(listingFormModel.CategoryName);

                // Get product abbreviation
                string productAbbrev = GetProductAbbreviation(listingFormModel.ListingProduct);

                // Process each style
                for (int styleIndex = 0; styleIndex < finalStyleNames.Count; styleIndex++)
                {
                    int styleNo = styleIndex + 1;
                    // Style
                    string styleNameForSheet;
                    string styleName;
                    if (listingFormModel.StyleNameOption == "New")
                    {
                        // Use actual style name directly
                        styleName = finalStyleNames[styleIndex].Replace(" ", "");
                        styleNameForSheet = finalStyleNames[styleIndex];
                    }
                    else
                    {
                        // Use category + number
                        styleName = $"{listingFormModel.CategoryName.Replace(" ", "")}{styleNo:D2}";
                        styleNameForSheet = $"{listingFormModel.CategoryName} {styleNo:D2}";
                    }

                    // Get style images
                    //string mainImage = "";
                    //string otherImage = "";
                    //if (listingFormModel.StyleImages != null && listingFormModel.StyleImages.Count > styleIndex)
                    //{
                    //    //mainImage = listingFormModel.StyleImages[styleIndex].MainImage ?? "";
                    //    otherImage = listingFormModel.StyleImages[styleIndex].OtherImage ?? "";
                    //}

                    // Get style images
                    var styleImages = listingFormModel.StyleImages?[styleIndex];

                    // Process colors
                    var colorsToProcess = GetColorsToProcess(listingFormModel);

                    foreach (var colorInfo in colorsToProcess)
                    {
                        // Find matching color image for this style/color
                        string mainImage = "";
                        string otherImage = styleImages.OtherImage ?? "";
                        string OtherImageUrlKids = styleImages.OtherImageUrlKids ?? "";

                        if (styleImages?.ColorImages != null)
                        {
                            var colorImage = styleImages.ColorImages
                                                .FirstOrDefault(c => c.ColorName.Equals(colorInfo.ColorName, StringComparison.OrdinalIgnoreCase)
                                                                  || c.idColor == Convert.ToString(colorInfo.ColorId));

                            if (colorImage != null)
                            {
                                //OtherImageUrlKids = styleImages.OtherImageUrlKids;
                                mainImage = colorImage.MainImage ?? "";
                                // Optional: override otherImage per color if needed
                                // otherImage = colorImage.OtherImage ?? otherImage;
                            }
                        }

                        // Process sizes
                        var sizesToProcess = GetSizesToProcess(listingFormModel);

                        foreach (var sizeInfo in sizesToProcess)
                        {
                            var childSKU = new ChildSKUInfo
                            {
                               // StyleName = styleName,
                                StyleName = styleNameForSheet,  // ✅ For sheet (with spaces),
                                StyleNo = styleNo,
                                Color = colorInfo.ColorName,
                                ColorMap = colorInfo.ColorMap,
                                ColorId = colorInfo.ColorId,
                                SizeName = sizeInfo.SizeName,
                                SizeMap = sizeInfo.SizeMap,
                                SizeCategory = sizeInfo.SizeCategory,
                                MainImageUrl = mainImage,
                                OtherImageUrlAdult = otherImage,
                                OtherImageUrlKids = OtherImageUrlKids,
                                CountryOfOrigin = listingFormModel.CountryName
                            };

                            // Generate SKU based on size category
                            if (sizeInfo.SizeCategory == "Kids")
                            {
                                childSKU.SKU = $"{styleName}Kids{productAbbrev}_{colorInfo.ColorMap}{sizeInfo.SizeMap}";
                                childSKU.SalePrice = listingFormModel.KidsPrice ?? Convert.ToDecimal(_config["DefaultPrices:KidsPrice"]);
                                childSKU.ProductTextCode = "A_CLTH_CHILD";
                                childSKU.Title = $"{listingFormModel.Title} {colorInfo.ColorName} {sizeInfo.SizeName} {styleNameForSheet}";
                            }
                            else // Adults
                            {
                                // Format: CategoryStyle + Product + Color + Size
                                childSKU.SKU = $"{styleName}_{productAbbrev}_{colorInfo.ColorMap}{sizeInfo.SizeMap}";
                                childSKU.SalePrice = listingFormModel.AdultPrice ?? Convert.ToDecimal(_config["DefaultPrices:AdultPrice"]);
                                childSKU.ProductTextCode = "A_GEN_STANDARD";
                                // Generate title: Color + Size + Style name
                                childSKU.Title = $"{listingFormModel.Title} {colorInfo.ColorName} {sizeInfo.SizeMap} {styleNameForSheet}";
                            }

                            allChildSKUs.Add(childSKU);
                        }
                    }
                }

                return allChildSKUs;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating child SKUs: {ex.Message}");
            }
        }
        // Update data on sheet
        private void UpdateAmazonSheet(List<ChildSKUInfo> childSKUs, string parentSKU, ListingFormModel listingFormModel)
        {

            string fileName = $"{listingFormModel.CategoryName}_Template.xlsm";
            string filePath = Path.Combine(_config["Files:FileFolderPath"], $"{fileName}");

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var sheet = package.Workbook.Worksheets["Template"];
                if (sheet == null)
                    throw new Exception("Template sheet not found.");

                int headerRow = -1;
                for (int row = 1; row <= 20; row++) // scan first 20 rows
                {
                    var firstCell = sheet.Cells[row, 1].Value?.ToString();
                    if (!string.IsNullOrEmpty(firstCell) && firstCell.Contains("feed_product_type", StringComparison.OrdinalIgnoreCase))
                    {
                        headerRow = row;
                        break;
                    }
                }
                var columnMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                int totalCols = sheet.Dimension.End.Column;

                for (int col = 1; col <= totalCols; col++)
                {
                    var header = sheet.Cells[headerRow, col].Value?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(header) && !columnMap.ContainsKey(header))
                    {
                        columnMap[header] = col;
                    }
                }

                int currentRow = 4; // Data starts at row 5

                // ✅ Insert Parent SKU row
                sheet.Cells[currentRow, columnMap["feed_product_type"]].Value = _amazonSheetDefaults.FeedProductType;
                sheet.Cells[currentRow, columnMap["item_sku"]].Value = parentSKU;
                sheet.Cells[currentRow, columnMap["brand_name"]].Value = _amazonSheetDefaults.BrandName;
                sheet.Cells[currentRow, columnMap["item_name"]].Value = listingFormModel.Title;
                sheet.Cells[currentRow, columnMap["lifecycle_supply_type"]].Value = _amazonSheetDefaults.LifecycleSupplyType;
                sheet.Cells[currentRow, columnMap["country_of_origin"]].Value = listingFormModel.CountryName;
                sheet.Cells[currentRow, columnMap["product_description"]].Value = listingFormModel.Description;
                sheet.Cells[currentRow, columnMap["closure_type"]].Value = _amazonSheetDefaults.ClosureType;
                sheet.Cells[currentRow, columnMap["bullet_point1"]].Value = listingFormModel.Bp1;
                sheet.Cells[currentRow, columnMap["bullet_point2"]].Value = listingFormModel.Bp2;
                sheet.Cells[currentRow, columnMap["bullet_point3"]].Value = listingFormModel.Bp3;
                sheet.Cells[currentRow, columnMap["bullet_point4"]].Value = listingFormModel.Bp4;
                sheet.Cells[currentRow, columnMap["bullet_point5"]].Value = listingFormModel.Bp5;
                sheet.Cells[currentRow, columnMap["parent_child"]].Value = "Parent";
                sheet.Cells[currentRow, columnMap["variation_theme"]].Value = _amazonSheetDefaults.VariationTheme;
                sheet.Cells[currentRow, columnMap["update_delete"]].Value = _amazonSheetDefaults.UpdateDelete;
                sheet.Cells[currentRow, columnMap["generic_keywords"]].Value = listingFormModel.SearchTerms;

                currentRow++;

                var orderedChildSKUs = childSKUs.OrderBy(c => c.SizeCategory == "Kids" ? 1 : 0).ThenBy(c => c.StyleNo)                          
                                       .ThenBy(c => c.Color).ToList();

                // Insert each child SKU row
                foreach (var child in orderedChildSKUs)
                {
                    sheet.Cells[currentRow, columnMap["feed_product_type"]].Value = _amazonSheetDefaults.FeedProductType;
                    sheet.Cells[currentRow, columnMap["item_sku"]].Value = child.SKU;

                    //fixed value used 
                    sheet.Cells[currentRow, columnMap["brand_name"]].Value = _amazonSheetDefaults.BrandName;

                    sheet.Cells[currentRow, columnMap["item_name"]].Value = child.Title;

                    if (child.SizeCategory == "Kids")
                        sheet.Cells[currentRow, columnMap["recommended_browse_nodes"]].Value = listingFormModel.KidsBrowseNode;
                    else
                        sheet.Cells[currentRow, columnMap["recommended_browse_nodes"]].Value = listingFormModel.AdultBrowseNode;


                    sheet.Cells[currentRow, columnMap["outer_material_type"]].Value = _amazonSheetDefaults.OuterMaterialType;
                    sheet.Cells[currentRow, columnMap["color_map"]].Value = child.Color;
                    sheet.Cells[currentRow, columnMap["color_name"]].Value = child.Color;

                    sheet.Cells[currentRow, columnMap["size_name"]].Value = child.SizeName;

                    sheet.Cells[currentRow, columnMap["lifecycle_supply_type"]].Value = _amazonSheetDefaults.LifecycleSupplyType;

                    sheet.Cells[currentRow, columnMap["size_map"]].Value = child.SizeMap;
                    sheet.Cells[currentRow, columnMap["country_of_origin"]].Value = child.CountryOfOrigin;

                    sheet.Cells[currentRow, columnMap["fabric_type"]].Value = _amazonSheetDefaults.FabricType;
                    sheet.Cells[currentRow, columnMap["supplier_declared_material_regulation1"]].Value = _amazonSheetDefaults.SupplierDeclaredMaterialRegulation;
                    sheet.Cells[currentRow, columnMap["supplier_declared_material_regulation2"]].Value = _amazonSheetDefaults.SupplierDeclaredMaterialRegulation;
                    sheet.Cells[currentRow, columnMap["supplier_declared_material_regulation3"]].Value = _amazonSheetDefaults.SupplierDeclaredMaterialRegulation;


                    if (listingFormModel.ListingProduct == "TShirt" || listingFormModel.ListingProduct == "Polo")
                    {
                        sheet.Cells[currentRow, columnMap["standard_price"]].Value = _amazonSheetDefaults.TShirtStandardPrice;
                        sheet.Cells[currentRow, columnMap["list_price_with_tax"]].Value = _amazonSheetDefaults.TShirtStandardPrice;
                    }
                    else if(listingFormModel.ListingProduct == "Sweatshirt")
                    {
                        sheet.Cells[currentRow, columnMap["standard_price"]].Value = _amazonSheetDefaults.SweatshirtStandardPrice;
                        sheet.Cells[currentRow, columnMap["list_price_with_tax"]].Value = _amazonSheetDefaults.SweatshirtStandardPrice;
                    }
                    else if(listingFormModel.ListingProduct == "Hoodie")
                    {
                        sheet.Cells[currentRow, columnMap["standard_price"]].Value = _amazonSheetDefaults.HoodieStandardPrice;
                        sheet.Cells[currentRow, columnMap["list_price_with_tax"]].Value = _amazonSheetDefaults.HoodieStandardPrice;
                    }
                   
                    sheet.Cells[currentRow, columnMap["quantity"]].Value = _amazonSheetDefaults.Quantity;

                    sheet.Cells[currentRow, columnMap["main_image_url"]].Value = child.MainImageUrl;

                    if(child.SizeCategory == "Kids")
                        sheet.Cells[currentRow, columnMap["age_range_description"]].Value = "Kid";                        
                    else
                        sheet.Cells[currentRow, columnMap["age_range_description"]].Value = "Adult";

                    // no idea
                    sheet.Cells[currentRow, columnMap["style_name"]].Value = child.StyleName;

                    if (child.SizeCategory == "Kids")
                    {
                        sheet.Cells[currentRow, columnMap["target_gender"]].Value = _amazonSheetDefaults.KidsTargetGender;
                        sheet.Cells[currentRow, columnMap["department_name"]].Value = _amazonSheetDefaults.KidsDepartmentName;
                    }
                    else
                    {
                        sheet.Cells[currentRow, columnMap["target_gender"]].Value = _amazonSheetDefaults.AdultsTargetGender;
                        sheet.Cells[currentRow, columnMap["department_name"]].Value = _amazonSheetDefaults.AdultsDepartmentName;
                    }

                    sheet.Cells[currentRow, columnMap["product_description"]].Value = listingFormModel.Description;
                    sheet.Cells[currentRow, columnMap["model"]].Value = child.SKU;
                    
                    sheet.Cells[currentRow, columnMap["closure_type"]].Value = _amazonSheetDefaults.ClosureType;

                    sheet.Cells[currentRow, columnMap["model_name"]].Value = child.SKU;

                    sheet.Cells[currentRow, columnMap["manufacturer"]].Value = _amazonSheetDefaults.Manufacturer;
                    sheet.Cells[currentRow, columnMap["care_instructions"]].Value = _amazonSheetDefaults.CareInstructions;

                    sheet.Cells[currentRow, columnMap["bullet_point1"]].Value = listingFormModel.Bp1;
                    sheet.Cells[currentRow, columnMap["bullet_point2"]].Value = listingFormModel.Bp2;
                    sheet.Cells[currentRow, columnMap["bullet_point3"]].Value = listingFormModel.Bp3;
                    sheet.Cells[currentRow, columnMap["bullet_point4"]].Value = listingFormModel.Bp4;
                    sheet.Cells[currentRow, columnMap["bullet_point5"]].Value = listingFormModel.Bp5;

                    //
                    if(listingFormModel.ListingProduct == "TShirt" || listingFormModel.ListingProduct == "Polo")
                     sheet.Cells[currentRow, columnMap["item_type_name"]].Value = "T-Shirt";

                    else if(listingFormModel.ListingProduct == "Sweatshirt")
                      sheet.Cells[currentRow, columnMap["item_type_name"]].Value = "Sweatshirt";

                    else if (listingFormModel.ListingProduct == "Hoodie")
                        sheet.Cells[currentRow, columnMap["item_type_name"]].Value = "Hoodie";

                    if (listingFormModel.Size == "All") {
                        sheet.Cells[currentRow, columnMap["other_image_url1"]].Value = child.OtherImageUrlAdult;
                    }

                    if (listingFormModel.Size == "all")
                    {
                        if (child.SizeCategory == "Kids")
                            sheet.Cells[currentRow, columnMap["other_image_url1"]].Value = child.OtherImageUrlKids;
                        else
                            sheet.Cells[currentRow, columnMap["other_image_url1"]].Value = child.OtherImageUrlAdult;
                    }   
                    else {
						if (child.SizeCategory == "Kids")
							sheet.Cells[currentRow, columnMap["other_image_url1"]].Value = child.OtherImageUrlKids;
						else
							sheet.Cells[currentRow, columnMap["other_image_url1"]].Value = child.OtherImageUrlAdult;
					}

                    sheet.Cells[currentRow, columnMap["parent_child"]].Value = "Child";

                    sheet.Cells[currentRow, columnMap["parent_sku"]].Value = parentSKU;

                    sheet.Cells[currentRow, columnMap["relationship_type"]].Value = "variation";
                    sheet.Cells[currentRow, columnMap["variation_theme"]].Value = _amazonSheetDefaults.VariationTheme;
                    sheet.Cells[currentRow, columnMap["update_delete"]].Value = _amazonSheetDefaults.UpdateDelete;

                    sheet.Cells[currentRow, columnMap["part_number"]].Value = child.SKU;
                    sheet.Cells[currentRow, columnMap["generic_keywords"]].Value = listingFormModel.SearchTerms;

                    sheet.Cells[currentRow, columnMap["fit_type"]].Value = _amazonSheetDefaults.FitType;

                    if (listingFormModel.ListingProduct == "TShirt")
                        sheet.Cells[currentRow, columnMap["weave_type"]].Value = "Plain";

                    // for te-shirt 
                    if (listingFormModel.ListingProduct == "TShirt" || listingFormModel.ListingProduct == "Polo")
                        sheet.Cells[currentRow, columnMap["sleeve_type"]].Value = "Short-sleeve";

                    //Swtshirt and hoodie
                    else if (listingFormModel.ListingProduct == "Sweatshirt" || listingFormModel.ListingProduct == "Hoodie")
                        sheet.Cells[currentRow, columnMap["sleeve_type"]].Value = "Long-sleeve";


                    sheet.Cells[currentRow, columnMap["condition_type"]].Value = _amazonSheetDefaults.ConditionType;
                    sheet.Cells[currentRow, columnMap["currency"]].Value = _amazonSheetDefaults.Currency;

                    //sheet.Cells[currentRow, columnMap["sale_price"]].Value = child.SalePrice;
                    //sheet.Cells[currentRow, columnMap["sale_from_date"]].Value = DateTime.Now;
                    //sheet.Cells[currentRow, columnMap["sale_from_date"]].Style.Numberformat.Format = "d/m/yyyy";
                    //sheet.Cells[currentRow, columnMap["sale_end_date"]].Value = "1/1/2040";

                    decimal DefaultPrices_Adults = Convert.ToDecimal(_config["DefaultPrices:AdultPrice"]);
                    decimal DefaultPrices_KidsPrice = Convert.ToDecimal(_config["DefaultPrices:KidsPrice"]);
                    decimal? DefaultPrices = null; ;
                    //fixed value used 
                    if (child.SizeCategory == "Kids") {
                        DefaultPrices = DefaultPrices_KidsPrice;
                    } else
                    {
                        DefaultPrices = DefaultPrices_Adults;
                    }
                        sheet.Cells[currentRow, columnMap["uvp_list_price"]].Value = DefaultPrices;
                        sheet.Cells[currentRow, columnMap["standard_price"]].Value = DefaultPrices;
                        sheet.Cells[currentRow, columnMap["list_price_with_tax"]].Value = DefaultPrices;
                    

                    sheet.Cells[currentRow, columnMap["product_tax_code"]].Value = child.ProductTextCode;

                    currentRow++;
                }
                package.Save();
            }
        }
        public void GetBrowseNodes(ListingFormModel listingFormModel)
        {
            var allSettings = _sqlDataAccess
                .GetData<SettingsModel, object>(
                    "SELECT ProductType, Category, BrowseNodes FROM tblSettings",
                    null
                ).Result;

            listingFormModel.AdultBrowseNode = allSettings
                .FirstOrDefault(x => x.ProductType == listingFormModel.ListingProduct &&
                                     x.Category == "Adults")?.BrowseNodes;

            listingFormModel.KidsBrowseNode = allSettings
                .FirstOrDefault(x => x.ProductType == listingFormModel.ListingProduct &&
                                     x.Category == "Kids")?.BrowseNodes;
        }
        private string GetCategoryAbbreviation(string categoryName)
        {
            if (string.IsNullOrEmpty(categoryName))
                return "Cat";
            var words = categoryName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var result = string.Concat(words.Select(w =>
                char.ToUpper(w[0]) + w.Substring(1).ToLower()
            ));

            return result;
        }
        private string GetProductAbbreviation(string listingProduct)
        {
            if (listingProduct == "TShirt")
                listingProduct = "Tee";

            else if (listingProduct == "Hoodie")
                listingProduct = "Hood";

            else if (listingProduct == "Sweatshirt")
                listingProduct = "SwetShirt";

            else if (listingProduct == "Polo")
                listingProduct = "PoloTee";

            return listingProduct;
        }
        private List<(string ColorName, string ColorMap, Guid ColorId)> GetColorsToProcess(ListingFormModel listingFormModel)
        {
            var colorsToProcess = new List<(string ColorName, string ColorMap, Guid ColorId)>();

            if (listingFormModel.Colour != null && listingFormModel.Colour.Any())
            {
                if (listingFormModel.Colour.FirstOrDefault() == "all")
                {
                    // Add all available colors
                    foreach (var color in listingFormModel.Colors)
                    {
                        colorsToProcess.Add((color.Color, color.ColorMap, color.idColor));
                    }
                }
                else
                {
                    // Add selected colors
                    foreach (var selectedColorId in listingFormModel.Colour)
                    {
                        var color = listingFormModel.Colors.FirstOrDefault(c => c.idColor.ToString() == selectedColorId);
                        if (color != null)
                        {
                            colorsToProcess.Add((color.Color, color.ColorMap, color.idColor));
                        }
                    }
                }
            }

            return colorsToProcess;
        }
        private List<(string SizeName, string SizeMap, string SizeCategory)> GetSizesToProcess(ListingFormModel listingFormModel)
        {
            var sizesToProcess = new List<(string SizeName, string SizeMap, string SizeCategory)>();

            // Process Kids sizes
            if (listingFormModel.KidsSize != null && listingFormModel.KidsSize.Any())
            {
                if (listingFormModel.KidsSize.FirstOrDefault() == "all")
                {
                    // Add all kids sizes from the database
                    var kidsSizes = GetSizesFromDatabase("Kids"); // You'll need to implement this
                    foreach (var size in kidsSizes)
                    {
                        sizesToProcess.Add((size.SizeName, size.SizeMap, "Kids"));
                    }
                }
                else
                {
                    foreach (var kidsSize in listingFormModel.KidsSize)
                    {
                        SizeModel sizeInfo = null;
                        if (listingFormModel.Sizes.FirstOrDefault(s => s.SizeMap.ToLower().Trim() == kidsSize.ToLower().Trim()) != null)
                        {
                            sizeInfo = listingFormModel.Sizes.FirstOrDefault(s => s.SizeMap.ToLower().Trim() == kidsSize.ToLower().Trim());
                        }
                        else
                        {
                            sizeInfo = listingFormModel.Sizes.FirstOrDefault(s => s.SizeName.ToLower().Trim() == kidsSize.ToLower().Trim());
                        }
                        //var sizeInfo = listingFormModel.Sizes.FirstOrDefault(s => s.SizeName == kidsSize);
                        string sizeMap = sizeInfo.SizeMap;
                        sizesToProcess.Add((kidsSize, sizeMap, "Kids"));
                    }
                }
            }

            // Process Adult sizes
            if (listingFormModel.AdultSize != null && listingFormModel.AdultSize.Any())
            {
                if (listingFormModel.AdultSize.FirstOrDefault() == "all")
                {
                    // Add all adult sizes from the database
                    var adultSizes = GetSizesFromDatabase("Adults"); // You'll need to implement this
                    foreach (var size in adultSizes)
                    {
                        sizesToProcess.Add((size.SizeName, size.SizeMap, "Adults"));
                    }
                }
                else
                {
                    foreach (var adultSize in listingFormModel.AdultSize)
                    {
                        SizeModel sizeInfo = null;
                        if (listingFormModel.Sizes.FirstOrDefault(s => s.SizeMap.ToLower().Trim() == adultSize.ToLower().Trim()) != null)
                        {
                            sizeInfo = listingFormModel.Sizes.FirstOrDefault(s => s.SizeMap == adultSize);
                        }
                        else
                        {
                            sizeInfo = listingFormModel.Sizes.FirstOrDefault(s => s.SizeName == adultSize);
                        }
                        string sizeMap = sizeInfo.SizeMap;
                        sizesToProcess.Add((adultSize, sizeMap, "Adults"));
                    }
                }
            }

            return sizesToProcess;
        }
        // Helper methods to get sizes from database (you'll need to implement these)
        private List<SizeModel> GetSizesFromDatabase(string sizeName)
        {
            List<SizeModel> sizeList = new List<SizeModel>();
            string query = string.Empty;

            // Implementation to get kids sizes from database
            if(sizeName == "Kids")
            {
                query = "Select * from tblSizes Where idSizeCategory = '2'";
            }
            else
            {
                query = "Select * from tblSizes Where idSizeCategory = '1'";
            }
            
            sizeList =  _sqlDataAccess.GetData<SizeModel, dynamic>(query, new { }).GetAwaiter().GetResult().ToList();
            return sizeList;
        }
        // Helper methods to get IDs from database
        private Guid GetStyleIdFromDatabase(Guid idListingProduct, int styleNo)
        {
            // Implementation to get style ID based on product ID and style number
            string query = $@"SELECT idListingStyle FROM tblListingStyle WHERE idListingProduct = '{idListingProduct}' AND StyleNo = '{styleNo}'";
            Guid idListingStyle = Guid.Parse(_sqlDataAccess.GetSingleValue(query));
           
            return idListingStyle; 
        }
        private Guid GetColorIdFromDatabase(Guid idListingProduct, Guid colorId)
        {
            // Implementation to get color ID from tblListingColor
            string query = $@"SELECT idListingColor FROM tblListingColor WHERE idListingProduct = '{idListingProduct}' AND idColor = '{colorId}'";
            Guid idListingColor = Guid.Parse(_sqlDataAccess.GetSingleValue(query));

            return idListingColor;
        }
        private Guid GetSizeIdFromDatabase(Guid idListingProduct, string sizeName)
        {
            // Implementation to get size ID from tblListingSize
            string query = $@"SELECT idListingSize FROM tblListingSize WHERE idListingProduct = '{idListingProduct}' AND SizeName = '{sizeName}'";
            Guid idListingSize = Guid.Parse(_sqlDataAccess.GetSingleValue(query));

            return idListingSize;
        }
        public async Task<IEnumerable<SizeModel>> GetSizeList()
        {
            IEnumerable<SizeModel> sizeList = new List<SizeModel>();

            string query = @"SELECT idSize, idSizeCategory, SizeName,SizeMap 
                             FROM tblSizes ORDER BY idSize ASC";

            sizeList = await _sqlDataAccess.GetData<SizeModel, dynamic>(query, new { });
            return sizeList;
        }
        #endregion

        #region template list
        public async Task<ResponseModel> GetListingTemplateList()
        {
            ResponseModel response = new ResponseModel();
            string query = string.Empty;
            try
            {
                //Select only required columns
                query = @"SELECT idListingProduct,CategoryName,ProductType,Title,
                          CONVERT(varchar(20), DateAdd, 101) + ' ' + CONVERT(varchar(20), 
		                  CONVERT(TIME, DateAdd), 100) as DateAdd 
                          FROM tblListingProduct Order by DateAdd DESC";
                var data = await _sqlDataAccess.GetData<TemplateListModel, dynamic>(query, new { });

                response.IsError = false;
                response.Result = data;
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                response.IsError = true;
            }

            return response;
        }

        public async Task<ListingFormModel> GetTemplateDetails(string idListingProduct)
        {

            //string query = $@"SELECT idListingProduct,CategoryName,ProductType,Title,NumberOfStyles,
            //                SizeType,PriceChange,AdultPrice,KidsPrice,CountryOfOrigin,
            //                BulletPoints,Description,SearchTerms,DesignType 
            //                FROM tblListingProduct WHERE idListingProduct = @idListingProduct";

            //var result = _sqlDataAccess.GetData<ListingFormModel, dynamic>(query, new { idListingProduct = idListingProduct }).GetAwaiter().GetResult();
            //var product = result.FirstOrDefault(); // single item or null

            //return Task.FromResult(product);
            // ---------- Main product info ----------
            string queryProduct = @"SELECT idListingProduct, CategoryName, ProductType AS ListingProduct, Title,StyleOption as StyleNameOption,
                                   NumberOfStyles, SizeType AS Size, PriceChange, AdultPrice, KidsPrice,Colour as color,
                                   idCountryOfOrigin,CountryOfOrigin, BulletPoints, Description, SearchTerms, DesignType
                                   FROM tblListingProduct WHERE idListingProduct = @idListingProduct";

            var product = (await _sqlDataAccess.GetData<ListingFormModel, dynamic>(
                queryProduct, new { idListingProduct }
            )).FirstOrDefault();

            if (product == null)
                return null;

            if (!string.IsNullOrEmpty(product.BulletPoints))
            {
                var bpDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(product.BulletPoints);
                if (bpDict != null)
                {
                    product.Bp1 = bpDict.ContainsKey("Bp1") ? bpDict["Bp1"] : string.Empty;
                    product.Bp2 = bpDict.ContainsKey("Bp2") ? bpDict["Bp2"] : string.Empty;
                    product.Bp3 = bpDict.ContainsKey("Bp3") ? bpDict["Bp3"] : string.Empty;
                    product.Bp4 = bpDict.ContainsKey("Bp4") ? bpDict["Bp4"] : string.Empty;
                    product.Bp5 = bpDict.ContainsKey("Bp5") ? bpDict["Bp5"] : string.Empty;
                }
            }
            // ------------- Colors ----------------

            // Load selected colors (ids) from tblListingColor
            // ------------- Colors ----------------
            if (product.color == "All")
            {
                // Set "all" as selected in the dropdown
                product.Colour = new List<string> { "all" };
            }
            else
            {
                string queryColors = @"SELECT idColor FROM tblListingColor 
                           WHERE idListingProduct = @idListingProduct";

                var colorIds = await _sqlDataAccess.GetData<Guid, dynamic>(
                    queryColors, new { idListingProduct }
                );

                // Store them as string (since your <select> uses string values)
                product.Colour = colorIds.Select(c => c.ToString()).ToList();
                if (product.Colour != null && product.Colour.Count > 0)
                    product.StringColour = string.Join(",",colorIds.Select(c => c.ToString()).ToList());
            }
          
            // ---------- Styles ----------
            string queryStyles = @"SELECT StyleName,StyleNo FROM tblListingStyle 
                        WHERE idListingProduct = @idListingProduct 
                        ORDER BY StyleName DESC";

            //product.StyleNames = (await _sqlDataAccess.GetData<StyleModel, dynamic>(
            //    queryStyles, new { idListingProduct }
            //)).Select(s => s.StyleName).ToList();

            var styleList = await _sqlDataAccess.GetData<StyleModel, dynamic>(
                               queryStyles, new { idListingProduct });

            int maxStyleNo = styleList.Max(s => s.StyleNo);

            // Fill array by StyleNo, then convert to List<string>
            product.StyleNames = styleList
                .OrderBy(s => s.StyleNo)
                .Aggregate(new string[maxStyleNo], (arr, s) =>
                {
                    arr[s.StyleNo - 1] = s.StyleName;
                    return arr;
                })
                .ToList();

            // ---------- Sizes ----------
            string querySizes = @"SELECT ls.SizeName, sc.SizeCategory, sc.idSizeCategory FROM tblListingSize ls
                                JOIN tblSizeCategory sc ON sc.SizeCategory = ls.SizeCategory
                                WHERE ls.idListingProduct = @idListingProduct";

            var sizes = await _sqlDataAccess.GetData<SizeModel, dynamic>(
                querySizes, new { idListingProduct }
            );

            // Adults
            product.AdultSize = sizes
                .Where(s => s.SizeCategory.Equals("Adults", StringComparison.OrdinalIgnoreCase))
                .Select(s => s.SizeName)
                .ToList();
            if(product.AdultSize != null && product.AdultSize.Count > 0)
                product.SelectedAdultSize =  string.Join(",", product.AdultSize);
            // Kids
            product.KidsSize = sizes
                .Where(s => s.SizeCategory.Equals("Kids", StringComparison.OrdinalIgnoreCase))
                .Select(s => s.SizeName)
                .ToList();
            if (product.KidsSize != null && product.KidsSize.Count > 0)
                product.SelectedKidsSize = string.Join(",", product.KidsSize);

            // ---------- Images ----------
            //string queryImages = @"SELECT idListingImage, MainImageUrl as MainImage, OtherImageUrlAdult as OtherImage, idListingStyle
            //   FROM tblListingImage WHERE idListingProduct = @idListingProduct";

            //product.StyleImages = (await _sqlDataAccess.GetData<StyleImage, dynamic>(
            //                         queryImages, new { idListingProduct })).ToList();

            // ---------- Images (per Style + Color) ----------

            // ---------- Fetch styles ----------
            var styles = await _sqlDataAccess.GetData<ListingStyleDto, dynamic>(
                @"SELECT idListingStyle, StyleNo, StyleName FROM tblListingStyle WHERE idListingProduct = @idListingProduct
                  ORDER BY StyleNo", new { idListingProduct });

            // ---------- Fetch images ----------
            var images = await _sqlDataAccess.GetData<ListingImageDto, dynamic>(
                @"SELECT idListingImage, idListingStyle, idColor, Color, MainImageUrl, OtherImageUrlAdult,OtherImageUrlKids
                FROM tblListingImage WHERE idListingProduct = @idListingProduct", new { idListingProduct });

            // ---------- Map Styles with Images ----------
            var styleImages = styles.Select(style =>
            {
                var imagesForStyle = images
                    .Where(img => img.idListingStyle == style.idListingStyle)
                    .ToList();

                return new StyleImage
                {
                    StyleNo = style.StyleNo,
                    StyleName = style.StyleName,
                    ColorImages = imagesForStyle.Select(img => new ColorImage
                    {
                        idColor = img.idColor.ToString(),
                        ColorName = img.Color,
                        MainImage = img.MainImageUrl,
                        OtherImage = img.OtherImageUrlAdult,
                        OtherImageUrlKids = img.OtherImageUrlKids
                    }).ToList(),

                    // This gives you one OtherImage per Style

                    OtherImage = imagesForStyle.FirstOrDefault()?.OtherImageUrlAdult ?? "",

                    OtherImageUrlKids = product.Size.ToLower() == "adults"
    ? (imagesForStyle.FirstOrDefault()?.OtherImageUrlAdult ?? "")
    : (imagesForStyle.FirstOrDefault()?.OtherImageUrlKids ?? "")
                };
            }).OrderBy(s => s.StyleNo).ToList();

            product.StyleImages = styleImages;

            return product;
        }
        #endregion


        #region Get Colors
        public async Task<ResponseModel> GetColorsByProductType(string productType, string size)
        {
            ResponseModel response = new ResponseModel();
            string query = string.Empty;
            bool ignoreSizeFilter =
                    size != null &&
                    size.Equals("all", StringComparison.OrdinalIgnoreCase);
            try
            {
                query = @"
            SELECT DISTINCT c.idColor, c.Color, c.ColorMap
            FROM tblSizes pin
            INNER JOIN tblColor c ON pin.idColor = c.idColor
            WHERE pin.product_type = @ProductType
            AND (@IgnoreSizeFilter = 1 OR pin.idSizeCategory = @Size)
            ORDER BY c.Color ASC";

                // Assuming you have a ColorModel to map idColor, Color, ColorMap
                var data = await _sqlDataAccess.GetData<ColorModel, dynamic>(query, new { ProductType = productType , IgnoreSizeFilter = ignoreSizeFilter ? 1 : 0, Size = (size == "Kids")?2: (size == "Adults") ? 1 : (int?)null });

                response.IsError = false;
                response.Result = data;
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                response.IsError = true;
            }

            return response;
        }


        #endregion

        #region Return Size
        public async Task<ResponseModel> GetSizesByProductAndColors(string productType, List<string> colorIds, string  size)
        {
            ResponseModel response = new ResponseModel();

            try
            {
                // When "all" is passed → ignore color filter
                bool ignoreColorFilter = false;
                List<string> colors = null;
                string colorsCsv = "";
                string sizeCsv = "";
              

                if (colorIds != null && colorIds.Count > 0 && !colorIds.Select(x => x == "all").FirstOrDefault())
                {                   

                    string colorsCsv1 = string.Join(",", colorIds.Select(x=>x).ToList());
                    string queryColors = @"SELECT DISTINCT idColor  FROM tblColor 
                           WHERE idColor IN (SELECT value FROM STRING_SPLIT(@ColorsCsv, ','))";

                    var StringColors = await _sqlDataAccess.GetData<Guid, dynamic>(
                        queryColors, new { ColorsCsv = colorsCsv1 }
                    );

                    // Store them as string (since your <select> uses string values)
                    colors = StringColors.Select(c => c.ToString()).ToList();
                    if (colors != null && colors.Count > 0)
                        ignoreColorFilter = true;
                    if (ignoreColorFilter)
                    {
                        colorsCsv = !ignoreColorFilter ? string.Empty : string.Join(",", colors);
                    }

                }
                List<string> fixSize;

                if (size != null && size.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    // "all" means include both
                    fixSize = new List<string> { "1", "2" };
                }
                else if (size != null && size.Equals("Kids", StringComparison.OrdinalIgnoreCase))
                {
                    fixSize = new List<string> { "2" };
                }
                else if (size != null && size.Equals("Adults", StringComparison.OrdinalIgnoreCase))
                {
                    fixSize = new List<string> { "1" };
                }
                else
                {
                    // Default case if size is null or unknown
                    fixSize = new List<string>();
                }

                sizeCsv = string.Join(",", fixSize); // no extra quotes

                if (!string.IsNullOrEmpty(productType) && size == "all" && colorIds == null)
                {
                    response.IsError = false;
                    response.Result = null;
                    return response;
                }
                if (!string.IsNullOrEmpty(productType) && size == "Kids" && colorIds == null)
                {
                    response.IsError = false;
                    response.Result = null;
                    return response;
                }
                if (!string.IsNullOrEmpty(productType) && size == "Adults" && colorIds == null)
                {
                    response.IsError = false;
                    response.Result = null;
                    return response;
                }
                string query = @"
WITH DistinctSizes AS (
    SELECT
        pin.SizeName AS sizeName,
        pin.idSizeCategory AS sizeCategory,
        pin.SortOrder,
        ROW_NUMBER() OVER (
            PARTITION BY pin.SizeName
            ORDER BY pin.idSizeCategory
        ) AS rn
    FROM tblSizes pin
    WHERE pin.product_type = @ProductType
      AND (@ColorsCsv IS NULL OR @ColorsCsv = '' 
           OR pin.idColor IN (SELECT value FROM STRING_SPLIT(@ColorsCsv, ',')))
      AND (@SizesCsv IS NULL OR @SizesCsv = '' 
           OR pin.idSizeCategory IN (SELECT value FROM STRING_SPLIT(@SizesCsv, ',')))
)
SELECT sizeName, sizeCategory
FROM DistinctSizes
WHERE rn = 1
ORDER BY SortOrder;
";

                var parameters = new
                {
                    ProductType = productType,
                    ColorsCsv = string.IsNullOrEmpty(colorsCsv) ? null : colorsCsv,
                    SizesCsv = string.IsNullOrEmpty(sizeCsv) ? null : sizeCsv
                };

                var data = await _sqlDataAccess
                    .GetData<SizeModel, dynamic>(query, parameters);




                response.IsError = false;
                response.Result = data;
            }
            catch (Exception ex)
            {
                response.IsError = true;
                response.Message = ex.Message;
            }

            return response;
        }



        #endregion
    }
}
