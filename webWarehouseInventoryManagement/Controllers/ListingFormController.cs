using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using System.Drawing;
using System.Text.RegularExpressions;
using webWarehouseInventoryManagement.DataAccess.Data;
using webWarehouseInventoryManagement.Models;

namespace webWarehouseInventoryManagement.Controllers
{
    public class ListingFormController : Controller
    {
        private readonly IListingFormService _services;
        public ListingFormController(IListingFormService services) { 

            _services = services;
        }
        public IActionResult Index(string id)
        {
            ListingFormModel listingFormModel = new ListingFormModel();
            if (!string.IsNullOrEmpty(id))
            {
                listingFormModel = _services.GetTemplateDetails(id).GetAwaiter().GetResult();
                ViewBag.Title = $"Edit Template | {listingFormModel.CategoryName}";
            }
            else
            {
                ViewBag.Title = "Generate Template";
            }
            listingFormModel.Colors = _services.GetColors().GetAwaiter().GetResult().ToList();
            //if(listingFormModel.color != null)
                //listingFormModel.hdnColors = string.Join(",",listingFormModel.color.Select(x=>x.idColor));
            listingFormModel.Countrys = _services.GetCountryOfOrigin().GetAwaiter().GetResult().ToList(); 

            listingFormModel.DesignTypes = _services.GetDesignType().GetAwaiter().GetResult().ToList();

            // Country of Origin - set selected value
            if (listingFormModel.idCountryOfOrigin != Guid.Empty)
                listingFormModel.CountryOfOrigin = listingFormModel.idCountryOfOrigin.ToString();

            var selectedDesign = listingFormModel.DesignTypes
                        .FirstOrDefault(d => d.DesignType.ToString() == listingFormModel.DesignType);

            if (selectedDesign != null)
                listingFormModel.DesignType = selectedDesign.idListingDesignType.ToString();

            // Sizes
            var sizeCategories = _services.GetSizeCategories().GetAwaiter().GetResult();

            // Load Adults and Kids sizes separately
            var adultCategory = sizeCategories.FirstOrDefault(x => x.SizeCategory == "Adults");
            var kidsCategory = sizeCategories.FirstOrDefault(x => x.SizeCategory == "Kids");

            var adultSizes = adultCategory != null ? _services.GetSizes(adultCategory.idSizeCategory).GetAwaiter().GetResult(): new List<SizeModel>();
            var kidsSizes = kidsCategory != null ? _services.GetSizes(kidsCategory.idSizeCategory).GetAwaiter().GetResult() : new List<SizeModel>();

            ViewBag.AdultSizes = adultSizes;
            ViewBag.KidsSizes = kidsSizes;

            // Pass TempData to ViewBag so Razor can use it
            //ViewBag.SuccessMessage = TempData["SuccessMessage"];
            //ViewBag.ErrorMessage = TempData["ErrorMessage"];
            //ViewBag.DownloadFile = TempData["DownloadFile"];

            return View(listingFormModel);
        }
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult Index(ListingFormModel listingFormModel)
        {
            string message = string.Empty;
            try
            {
                listingFormModel.Colors = _services.GetColorsByProductTypeForEdit(listingFormModel.ListingProduct, listingFormModel.Size).GetAwaiter().GetResult();                
                listingFormModel.Countrys = _services.GetCountryOfOrigin().GetAwaiter().GetResult().ToList();
                listingFormModel.DesignTypes = _services.GetDesignType().GetAwaiter().GetResult().ToList();

                // Sizes
                var sizeCategories = _services.GetSizeCategories().GetAwaiter().GetResult();

                // Load Adults and Kids sizes separately
                var adultCategory = sizeCategories.FirstOrDefault(x => x.SizeCategory == "Adults");
                var kidsCategory = sizeCategories.FirstOrDefault(x => x.SizeCategory == "Kids");

                var adultSizes = adultCategory != null ? _services.GetSizes(adultCategory.idSizeCategory).GetAwaiter().GetResult() : new List<SizeModel>();
                var kidsSizes = kidsCategory != null ? _services.GetSizes(kidsCategory.idSizeCategory).GetAwaiter().GetResult() : new List<SizeModel>();

                
                ViewBag.AdultSizes = adultSizes;
                ViewBag.KidsSizes = kidsSizes;


                if (listingFormModel.Colour != null && listingFormModel.Colour.Count > 0)
                    listingFormModel.StringColour = string.Join(",", listingFormModel.Colour);
                if (listingFormModel.AdultSize != null && listingFormModel.AdultSize.Count > 0)
                    listingFormModel.SelectedAdultSize = string.Join(",", listingFormModel.AdultSize);
                if (listingFormModel.KidsSize != null && listingFormModel.KidsSize.Count > 0)
                    listingFormModel.SelectedKidsSize = string.Join(",", listingFormModel.KidsSize);

                if (listingFormModel.idListingProduct != Guid.Empty)
                {
                    message = "Listing Form updated successfully and generated the template file";
                }
                else
                {
                    message = "Listing Form added successfully and generated the template file";
                }

                var response = _services.AddProduct(listingFormModel).GetAwaiter().GetResult();

                if (response.IsError)
                {
                    ViewBag.ErrorMessage = response.Message;
                    return View(listingFormModel);
                }

                //string fileName = _services.ReadListingProductFile();
                response = _services.ReadListingProductFile(listingFormModel).GetAwaiter().GetResult();

                if (response.IsError)
                {
                    ViewBag.ErrorMessage = response.Message;
                    return View(listingFormModel);
                }

                // Map ListingProduct to template file
                string templateKeyword = listingFormModel.ListingProduct?.ToLower() switch
                {
                    "tshirt" => "TShirt",
                    "hoodie" => "Hoodie",
                    "polo" => "Polo",
                    "sweatshirt" => "Sweatshirt",
                    _ => throw new ArgumentException($"Unknown product type: {listingFormModel.ListingProduct}")
                };

                // Template file
                string templateFile = $"{templateKeyword}Template.xlsm";

                // Export file name (CategoryName + template keyword)
                string exportFileName = $"{listingFormModel.CategoryName}_Template.xlsm";

                // Remove special characters except underscore (_) and dot (.) and space            
                string sanitizedFileName = Regex.Replace(exportFileName, @"[^a-zA-Z0-9_ .]+", "");

                // here check path of return response filename
                // Full path 
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/ListingFiles", sanitizedFileName);
                if (!System.IO.File.Exists(filePath))
                {
                    ViewBag.ErrorMessage = "Template file not found!";
                    return View(listingFormModel);
                }

                var fileBytes = System.IO.File.ReadAllBytes(filePath);

                FileInfo fileInfo = new FileInfo(filePath);

                #region Brand Name Replace in EXCEL
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage(fileInfo))
                {
                    var worksheet = package.Workbook.Worksheets["Template"];
                    if (worksheet == null)
                    {
                        ViewBag.ErrorMessage = "Worksheet 'Template' not found!";
                        return View(listingFormModel);
                    }

                    // ✅ UPDATE the values below Brand Name
                    //worksheet.Cells[2, 3].Value = "FULLY MERCHED";
                    //worksheet.Cells[3, 3].Value = "fully_merched";

                    // Save
                    package.Save();
                }
                #endregion

                //TempData["SuccessMessage"] = "Listing Form added successfully and generated the template file";
                TempData["SuccessMessage"] = message;

                TempData["DownloadFile"] = sanitizedFileName;

                // Redirect to GET (avoid form resubmission and clear the model)
                //return RedirectToAction("Index");
                return RedirectToAction("ListingFormListPage");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                return View(listingFormModel);
            }
        }
        // New Action for downloading file
        public IActionResult DownloadTemplate(string fileName)
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/ListingFiles", fileName);
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("File not found.");
            }

            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            return File(fileBytes,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileName);
        }

        //
        [HttpGet]
        public IActionResult ListingFormListPage()
        {
            if(TempData["SuccessMessage"] != null)
            {
                ViewBag.SuccessMessage = TempData["SuccessMessage"];
                ViewBag.DownloadFile = TempData["DownloadFile"];
                TempData["SuccessMessage"] = null;
                TempData["DownloadFile"] = null;
            }
           
            return View();
        }

        // Listings Template List
        [HttpGet]
        public async Task<JsonResult> GetListingTemplateList()
        {
            var result = await _services.GetListingTemplateList();
            
            return Json(result);
        }


        [HttpGet]
        public async Task<JsonResult> GetColors(string productType, string size)
        {
            if (string.IsNullOrEmpty(productType))
            {
                return Json(new List<string>());
            }
            var result = await _services.GetColorsByProductType(productType,size);
            
            return Json(result);
        }
        [HttpGet]
        public async Task<JsonResult> GetSizesByProductAndColors(string productType, List<string> colors = null, string size =null)
        {
            // Validate inputs
            if (string.IsNullOrEmpty(productType) && string.IsNullOrEmpty(size) )
            {
                return Json(new List<string>());
            }

            // Use productType and colors to query sizes
            var result = await _services.GetSizesByProductAndColors(productType, colors, size);

            return Json(result);
        }
    }
}
