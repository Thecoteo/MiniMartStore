using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MiniSmartstoreMvc.Data;
using MiniSmartstoreMvc.Helpers;
using MiniSmartstoreMvc.Models;
using System.Text.RegularExpressions;

namespace MiniSmartstoreMvc.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;


        public ProductController(
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }


        public async Task<IActionResult> Index(
            string? search,
            int? categoryId,
            decimal? minPrice,
            decimal? maxPrice,
            bool? published,
            string? sortBy,
            int page = 1,
            int pageSize = 25,
            bool filterOpen = false)
        {
            if (page < 1)
            {
                page = 1;
            }


            if (pageSize != 10 &&
                pageSize != 25 &&
                pageSize != 50)
            {
                pageSize = 25;
            }


            var productsQuery = _context.Products
                .Include(p => p.Category)
                .AsQueryable();


            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                productsQuery = productsQuery.Where(p =>
                    p.Name.Contains(search) ||

                    (p.ProductCode != null &&
                     p.ProductCode.Contains(search)) ||

                    (p.Alias != null &&
                     p.Alias.Contains(search)) ||

                    (p.Description != null &&
                     p.Description.Contains(search)) ||

                    (p.SeoTitle != null &&
                     p.SeoTitle.Contains(search)) ||

                    (p.SeoKeywords != null &&
                     p.SeoKeywords.Contains(search)) ||

                    (p.Category != null &&
                     p.Category.Name.Contains(search))
                );
            }


            if (categoryId.HasValue &&
                categoryId.Value > 0)
            {
                productsQuery = productsQuery.Where(p =>
                    p.CategoryId == categoryId.Value);
            }


            if (minPrice.HasValue)
            {
                productsQuery = productsQuery.Where(p =>
                    p.Price >= minPrice.Value);
            }


            if (maxPrice.HasValue)
            {
                productsQuery = productsQuery.Where(p =>
                    p.Price <= maxPrice.Value);
            }

            // ===== LƯU Ý: LỌC THEO TRẠNG THÁI HIỂN THỊ THỰC TẾ =====
            if (published.HasValue)
            {
                var now = DateTime.Now;

                if (published.Value)
                {
                    productsQuery = productsQuery.Where(p =>
                        p.IsActive &&
                        (!p.AvailableStartDate.HasValue ||
                         p.AvailableStartDate.Value <= now) &&
                        (!p.AvailableEndDate.HasValue ||
                         p.AvailableEndDate.Value > now));
                }
                else
                {
                    productsQuery = productsQuery.Where(p =>
                        !p.IsActive ||
                        (p.AvailableStartDate.HasValue &&
                         p.AvailableStartDate.Value > now) ||
                        (p.AvailableEndDate.HasValue &&
                         p.AvailableEndDate.Value <= now));
                }
            }
            // ===== KẾT THÚC LỌC THEO TRẠNG THÁI HIỂN THỊ THỰC TẾ =====

            productsQuery = sortBy switch
            {
                "name_asc" =>
                    productsQuery.OrderBy(p => p.Name),

                "price_asc" =>
                    productsQuery.OrderBy(p => p.Price),

                "price_desc" =>
                    productsQuery.OrderByDescending(p => p.Price),

                "stock_asc" =>
                    productsQuery.OrderBy(p => p.StockQuantity),

                "stock_desc" =>
                    productsQuery.OrderByDescending(p => p.StockQuantity),

                "featured" =>
                    productsQuery
                        .OrderByDescending(p => p.IsFeatured)
                        .ThenBy(p => p.DisplayOrder)
                        .ThenByDescending(p => p.CreatedAt),

                "newest" =>
                    productsQuery.OrderByDescending(p => p.CreatedAt),

                "display_order" =>
                    productsQuery
                        .OrderBy(p => p.DisplayOrder)
                        .ThenByDescending(p => p.CreatedAt),

                _ =>
                    productsQuery
                        .OrderBy(p => p.DisplayOrder)
                        .ThenByDescending(p => p.CreatedAt)
            };


            int totalItems =
                await productsQuery.CountAsync();


            int totalPages =
                (int)Math.Ceiling(
                    totalItems / (double)pageSize);


            var products = await productsQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();


            ViewBag.Search = search;
            ViewBag.CategoryId = categoryId;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.Published = published;
            ViewBag.SortBy = sortBy;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;
            ViewBag.FilterOpen = filterOpen;


            ViewBag.Categories =
                new SelectList(
                    await _context.Categories
                        .OrderBy(c => c.Name)
                        .ToListAsync(),
                    "Id",
                    "Name",
                    categoryId
                );


            return View(products);
        }


        public async Task<IActionResult> Create()
        {
            await LoadCategoriesAsync();


            var product = new Product
            {
                IsActive = true,
                IsFeatured = false,
                AllowCustomerReviews = true,
                DeliveryTime = "3 - 5 ngày",
                LowStockThreshold = 5,
                DisplayOrder = 0,
                StockQuantity = 0,
                ImageUrl = "/images/products/no-image.jpg"
            };


            return View(product);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Product product,
            IFormFile? imageFile,
            List<IFormFile>? galleryImages,
            string? saveMode)
        {
            PrepareProductBeforeSave(
                product,
                isNew: true);


            if (await _context.Products
                .AnyAsync(p =>
                    p.Name == product.Name))
            {
                ModelState.AddModelError(
                    "Name",
                    "Tên sản phẩm đã tồn tại.");
            }


            var selectedCategory =
                await _context.Categories
                    .AsNoTracking()
                    .Include(c => c.ParentCategory)
                    .FirstOrDefaultAsync(c =>
                        c.Id == product.CategoryId);


            if (selectedCategory == null)
            {
                ModelState.AddModelError(
                    "CategoryId",
                    "Danh mục không hợp lệ.");
            }
            else
            {
                ProductSeoSearchHelper.FillMissingSeo(
                    product,
                    selectedCategory);
            }


            ValidateProduct(product);


            if (imageFile != null &&
                imageFile.Length > 0)
            {
                var uploadResult =
                    await UploadProductImageAsync(
                        imageFile);


                if (!uploadResult.Success)
                {
                    ModelState.AddModelError(
                        "ImageUrl",
                        uploadResult.ErrorMessage ??
                        "Upload ảnh thất bại.");
                }
                else
                {
                    product.ImageUrl =
                        uploadResult.ImageUrl;
                }
            }
            else
            {
                product.ImageUrl =
                    string.IsNullOrWhiteSpace(
                        product.ImageUrl)
                        ? "/images/products/no-image.jpg"
                        : product.ImageUrl;
            }


            if (ModelState.IsValid)
            {
                _context.Products.Add(product);

                await _context.SaveChangesAsync();


                await SaveProductGalleryImagesAsync(
                    product.Id,
                    galleryImages);


                TempData["Success"] =
                    "Thêm sản phẩm thành công.";


                if (saveMode == "continue")
                {
                    return RedirectToAction(
                        nameof(Edit),
                        new
                        {
                            id = product.Id
                        });
                }


                return RedirectToAction(
                    nameof(Index));
            }


            await LoadCategoriesAsync(
                product.CategoryId);


            return View(product);
        }


        public async Task<IActionResult> Edit(
            int id)
        {
            var product =
                await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.ProductImages)
                    .FirstOrDefaultAsync(p =>
                        p.Id == id);


            if (product == null)
            {
                return NotFound();
            }


            await LoadCategoriesAsync(
                product.CategoryId);


            await LoadProductOrdersAsync(
                product.Id);


            return View(product);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Product product,
            IFormFile? imageFile,
            List<IFormFile>? galleryImages,
            string? saveMode)
        {
            if (id != product.Id)
            {
                return NotFound();
            }


            var oldProduct =
                await _context.Products
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p =>
                        p.Id == id);


            if (oldProduct == null)
            {
                return NotFound();
            }


            if (await _context.Products
                .AnyAsync(p =>
                    p.Name == product.Name &&
                    p.Id != id))
            {
                ModelState.AddModelError(
                    "Name",
                    "Tên sản phẩm đã tồn tại.");
            }


            var selectedCategory =
                await _context.Categories
                    .AsNoTracking()
                    .Include(c => c.ParentCategory)
                    .FirstOrDefaultAsync(c =>
                        c.Id == product.CategoryId);


            if (selectedCategory == null)
            {
                ModelState.AddModelError(
                    "CategoryId",
                    "Danh mục không hợp lệ.");
            }


            product.CreatedAt =
                oldProduct.CreatedAt;


            product.UpdatedAt =
                DateTime.Now;


            if (string.IsNullOrWhiteSpace(
                    product.ProductCode))
            {
                product.ProductCode =
                    oldProduct.ProductCode;


                if (string.IsNullOrWhiteSpace(
                        product.ProductCode))
                {
                    product.ProductCode =
                        CreateProductCode();
                }
            }


            if (string.IsNullOrWhiteSpace(
                    product.Alias))
            {
                product.Alias =
                    CreateAlias(product.Name);
            }


            if (selectedCategory != null)
            {
                ProductSeoSearchHelper.FillMissingSeo(
                    product,
                    selectedCategory);
            }


            ValidateProduct(product);


            if (imageFile != null &&
                imageFile.Length > 0)
            {
                var uploadResult =
                    await UploadProductImageAsync(
                        imageFile);


                if (!uploadResult.Success)
                {
                    ModelState.AddModelError(
                        "ImageUrl",
                        uploadResult.ErrorMessage ??
                        "Upload ảnh thất bại.");
                }
                else
                {
                    product.ImageUrl =
                        uploadResult.ImageUrl;
                }
            }
            else
            {
                product.ImageUrl =
                    oldProduct.ImageUrl;
            }


            if (string.IsNullOrWhiteSpace(
                    product.ImageUrl))
            {
                product.ImageUrl =
                    "/images/products/no-image.jpg";
            }


            if (ModelState.IsValid)
            {
                _context.Products.Update(product);

                await _context.SaveChangesAsync();


                await SaveProductGalleryImagesAsync(
                    product.Id,
                    galleryImages);


                TempData["Success"] =
                    "Cập nhật sản phẩm thành công.";


                if (saveMode == "continue")
                {
                    return RedirectToAction(
                        nameof(Edit),
                        new
                        {
                            id = product.Id
                        });
                }


                return RedirectToAction(
                    nameof(Index));
            }


            await LoadCategoriesAsync(
                product.CategoryId);


            await LoadProductOrdersAsync(
                product.Id);


            return View(product);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(
            int id)
        {
            var product =
                await _context.Products
                    .FindAsync(id);


            if (product == null)
            {
                return NotFound();
            }


            product.IsActive =
                !product.IsActive;


            product.UpdatedAt =
                DateTime.Now;


            await _context.SaveChangesAsync();


            TempData["Success"] =
                product.IsActive
                    ? "Đã hiện sản phẩm."
                    : "Đã ẩn sản phẩm.";


            return RedirectToAction(
                nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            var hasOrders = await _context.OrderDetails
                .AnyAsync(x => x.ProductId == id);

            if (hasOrders)
            {
                product.IsActive = false;
                product.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["Error"] =
                    "Sản phẩm đã có trong đơn hàng nên không thể xóa cứng. Hệ thống đã chuyển sản phẩm sang trạng thái ẩn.";

                return RedirectToAction(nameof(Index));
            }

            try
            {
                // ===== LƯU Ý: XÓA DỮ LIỆU PHỤ THUỘC TRƯỚC KHI XÓA SẢN PHẨM =====
                var cartItems = await _context.CartItems
                    .Where(x => x.ProductId == id)
                    .ToListAsync();

                var wishlistItems = await _context.WishlistItems
                    .Where(x => x.ProductId == id)
                    .ToListAsync();

                var reviews = await _context.ProductReviews
                    .Where(x => x.ProductId == id)
                    .ToListAsync();

                var images = await _context.ProductImages
                    .Where(x => x.ProductId == id)
                    .ToListAsync();

                var colors = await _context.ProductColors
                    .Where(x => x.ProductId == id)
                    .ToListAsync();

                _context.CartItems.RemoveRange(cartItems);
                _context.WishlistItems.RemoveRange(wishlistItems);
                _context.ProductReviews.RemoveRange(reviews);
                _context.ProductImages.RemoveRange(images);
                _context.ProductColors.RemoveRange(colors);
                // ===== KẾT THÚC XÓA DỮ LIỆU PHỤ THUỘC =====

                _context.Products.Remove(product);

                await _context.SaveChangesAsync();

                DeletePhysicalImageIfExists(product.ImageUrl);

                foreach (var image in images)
                {
                    DeletePhysicalImageIfExists(image.ImageUrl);
                }

                TempData["Success"] = "Xóa sản phẩm thành công.";
            }
            catch (DbUpdateException)
            {
                _context.ChangeTracker.Clear();

                product = await _context.Products.FindAsync(id);

                if (product != null)
                {
                    product.IsActive = false;
                    product.UpdatedAt = DateTime.Now;

                    await _context.SaveChangesAsync();
                }

                TempData["Error"] =
                    "Sản phẩm còn dữ liệu liên quan nên không thể xóa cứng. Hệ thống đã chuyển sản phẩm sang trạng thái ẩn.";
            }

            return RedirectToAction(nameof(Index));
        }
        // ===== LƯU Ý: XÓA NHIỀU SẢN PHẨM =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSelected(List<int> selectedIds)
        {
            selectedIds = selectedIds?
                .Where(id => id > 0)
                .Distinct()
                .ToList() ?? new List<int>();

            if (!selectedIds.Any())
            {
                TempData["Error"] = "Vui lòng chọn ít nhất một sản phẩm.";
                return RedirectToAction(nameof(Index));
            }

            var products = await _context.Products
                .Where(p => selectedIds.Contains(p.Id))
                .ToListAsync();

            if (!products.Any())
            {
                TempData["Error"] = "Không tìm thấy sản phẩm cần xóa.";
                return RedirectToAction(nameof(Index));
            }

            var productIdsWithOrders = await _context.OrderDetails
                .Where(x => selectedIds.Contains(x.ProductId))
                .Select(x => x.ProductId)
                .Distinct()
                .ToListAsync();

            var orderProductIds = productIdsWithOrders.ToHashSet();

            var productsToHide = products
                .Where(p => orderProductIds.Contains(p.Id))
                .ToList();

            var productsToDelete = products
                .Where(p => !orderProductIds.Contains(p.Id))
                .ToList();

            var deleteIds = productsToDelete
                .Select(p => p.Id)
                .ToList();

            try
            {
                foreach (var product in productsToHide)
                {
                    product.IsActive = false;
                    product.UpdatedAt = DateTime.Now;
                }

                if (deleteIds.Any())
                {
                    var cartItems = await _context.CartItems
                        .Where(x => deleteIds.Contains(x.ProductId))
                        .ToListAsync();

                    var wishlistItems = await _context.WishlistItems
                        .Where(x => deleteIds.Contains(x.ProductId))
                        .ToListAsync();

                    var reviews = await _context.ProductReviews
                        .Where(x => deleteIds.Contains(x.ProductId))
                        .ToListAsync();

                    var images = await _context.ProductImages
                        .Where(x => deleteIds.Contains(x.ProductId))
                        .ToListAsync();

                    var colors = await _context.ProductColors
                        .Where(x => deleteIds.Contains(x.ProductId))
                        .ToListAsync();

                    _context.CartItems.RemoveRange(cartItems);
                    _context.WishlistItems.RemoveRange(wishlistItems);
                    _context.ProductReviews.RemoveRange(reviews);
                    _context.ProductImages.RemoveRange(images);
                    _context.ProductColors.RemoveRange(colors);
                    _context.Products.RemoveRange(productsToDelete);
                }

                await _context.SaveChangesAsync();

                foreach (var product in productsToDelete)
                {
                    DeletePhysicalImageIfExists(product.ImageUrl);
                }

                TempData["Success"] =
                    $"Đã xử lý {products.Count} sản phẩm. " +
                    $"Xóa {productsToDelete.Count}, ẩn {productsToHide.Count}.";
            }
            catch (DbUpdateException)
            {
                _context.ChangeTracker.Clear();

                var fallbackProducts = await _context.Products
                    .Where(p => selectedIds.Contains(p.Id))
                    .ToListAsync();

                foreach (var product in fallbackProducts)
                {
                    product.IsActive = false;
                    product.UpdatedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                TempData["Error"] =
                    "Một số sản phẩm còn dữ liệu liên quan nên không thể xóa cứng. Hệ thống đã chuyển chúng sang trạng thái ẩn.";
            }

            return RedirectToAction(nameof(Index));
        }
        // ===== KẾT THÚC XÓA NHIỀU SẢN PHẨM =====
        private void PrepareProductBeforeSave(
            Product product,
            bool isNew)
        {
            if (isNew)
            {
                product.CreatedAt =
                    DateTime.Now;
            }


            product.UpdatedAt =
                DateTime.Now;


            if (string.IsNullOrWhiteSpace(
                    product.ProductCode))
            {
                product.ProductCode =
                    CreateProductCode();
            }


            if (string.IsNullOrWhiteSpace(
                    product.Alias))
            {
                product.Alias =
                    CreateAlias(product.Name);
            }


            if (string.IsNullOrWhiteSpace(
                    product.ImageUrl))
            {
                product.ImageUrl =
                    "/images/products/no-image.jpg";
            }


            if (string.IsNullOrWhiteSpace(
                    product.DeliveryTime))
            {
                product.DeliveryTime =
                    "3 - 5 ngày";
            }


            if (product.LowStockThreshold < 0)
            {
                product.LowStockThreshold = 0;
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
            DeleteProductImage(int id)
        {
            var image =
                await _context.ProductImages
                    .FirstOrDefaultAsync(x =>
                        x.Id == id);


            if (image == null)
            {
                return NotFound();
            }


            int productId =
                image.ProductId;


            DeletePhysicalImageIfExists(
                image.ImageUrl);


            _context.ProductImages.Remove(
                image);


            await _context.SaveChangesAsync();


            TempData["Success"] =
                "Đã xóa ảnh phụ.";


            return RedirectToAction(
                nameof(Edit),
                new
                {
                    id = productId
                });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
            SetMainProductImage(int id)
        {
            var image =
                await _context.ProductImages
                    .FirstOrDefaultAsync(x =>
                        x.Id == id);


            if (image == null)
            {
                return NotFound();
            }


            var product =
                await _context.Products
                    .FirstOrDefaultAsync(x =>
                        x.Id == image.ProductId);


            if (product == null)
            {
                return NotFound();
            }


            product.ImageUrl =
                image.ImageUrl;


            product.UpdatedAt =
                DateTime.Now;


            await _context.SaveChangesAsync();


            TempData["Success"] =
                "Đã đặt ảnh này làm ảnh chính.";


            return RedirectToAction(
                nameof(Edit),
                new
                {
                    id = product.Id
                });
        }


        private void ValidateProduct(
            Product product)
        {
            if (string.IsNullOrWhiteSpace(
                    product.Name))
            {
                ModelState.AddModelError(
                    "Name",
                    "Tên sản phẩm không được để trống.");
            }


            if (product.Price <= 0)
            {
                ModelState.AddModelError(
                    "Price",
                    "Giá sản phẩm phải lớn hơn 0.");
            }


            if (product.OldPrice != null &&
                product.OldPrice < product.Price)
            {
                ModelState.AddModelError(
                    "OldPrice",
                    "Giá cũ không được nhỏ hơn giá bán.");
            }


            if (product.StockQuantity < 0)
            {
                ModelState.AddModelError(
                    "StockQuantity",
                    "Số lượng tồn kho không được âm.");
            }


            if (product.LowStockThreshold < 0)
            {
                ModelState.AddModelError(
                    "LowStockThreshold",
                    "Ngưỡng cảnh báo tồn kho không được âm.");
            }


            if (product.AvailableStartDate.HasValue &&
                product.AvailableEndDate.HasValue &&
                product.AvailableEndDate.Value <=
                product.AvailableStartDate.Value)
            {
                ModelState.AddModelError(
                    "AvailableEndDate",
                    "Ngày ngừng bán phải lớn hơn ngày bắt đầu bán.");
            }
        }


        private async Task LoadCategoriesAsync(
            int? selectedId = null)
        {
            ViewBag.Categories =
                new SelectList(
                    await _context.Categories
                        .Where(c => c.IsActive)
                        .OrderBy(c => c.Name)
                        .ToListAsync(),
                    "Id",
                    "Name",
                    selectedId
                );
        }


        private async Task LoadProductOrdersAsync(
            int productId)
        {
            ViewBag.ProductOrders =
                await _context.OrderDetails
                    .Include(od => od.Order)
                    .Where(od =>
                        od.ProductId ==
                        productId)
                    .OrderByDescending(od =>
                        od.Order != null
                            ? od.Order.CreatedAt
                            : DateTime.MinValue)
                    .Take(10)
                    .ToListAsync();
        }


        private async Task SaveProductGalleryImagesAsync(
            int productId,
            List<IFormFile>? galleryImages)
        {
            if (galleryImages == null ||
                !galleryImages.Any())
            {
                return;
            }


            int currentCount =
                await _context.ProductImages
                    .CountAsync(x =>
                        x.ProductId ==
                        productId);


            foreach (var file
                     in galleryImages)
            {
                if (file == null ||
                    file.Length <= 0)
                {
                    continue;
                }


                var uploadResult =
                    await UploadProductImageAsync(
                        file);


                if (!uploadResult.Success ||
                    string.IsNullOrWhiteSpace(
                        uploadResult.ImageUrl))
                {
                    continue;
                }


                _context.ProductImages.Add(
                    new ProductImage
                    {
                        ProductId =
                            productId,

                        ImageUrl =
                            uploadResult.ImageUrl,

                        DisplayOrder =
                            currentCount,

                        CreatedAt =
                            DateTime.Now
                    });


                currentCount++;
            }


            await _context.SaveChangesAsync();
        }


        private void DeletePhysicalImageIfExists(
            string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(
                    imageUrl))
            {
                return;
            }


            if (imageUrl.Contains(
                    "no-image.jpg",
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }


            string relativePath =
                imageUrl
                    .TrimStart('/')
                    .Replace(
                        "/",
                        Path.DirectorySeparatorChar
                            .ToString());


            string fullPath =
                Path.Combine(
                    _environment.WebRootPath,
                    relativePath);


            if (System.IO.File.Exists(
                    fullPath))
            {
                System.IO.File.Delete(
                    fullPath);
            }
        }


        private async Task<(
            bool Success,
            string? ImageUrl,
            string? ErrorMessage)>
            UploadProductImageAsync(
                IFormFile imageFile)
        {
            string[] allowedExtensions =
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".webp"
            };


            string extension =
                Path.GetExtension(
                        imageFile.FileName)
                    .ToLowerInvariant();


            if (!allowedExtensions.Contains(
                    extension))
            {
                return (
                    false,
                    null,
                    "Chỉ cho phép upload ảnh .jpg, .jpeg, .png, .webp."
                );
            }


            if (imageFile.Length >
                3 * 1024 * 1024)
            {
                return (
                    false,
                    null,
                    "Dung lượng ảnh không được vượt quá 3MB."
                );
            }


            string uploadFolder =
                Path.Combine(
                    _environment.WebRootPath,
                    "images",
                    "products");


            if (!Directory.Exists(
                    uploadFolder))
            {
                Directory.CreateDirectory(
                    uploadFolder);
            }


            string fileName =
                $"{Guid.NewGuid()}{extension}";


            string filePath =
                Path.Combine(
                    uploadFolder,
                    fileName);


            using (var stream =
                   new FileStream(
                       filePath,
                       FileMode.Create))
            {
                await imageFile.CopyToAsync(
                    stream);
            }


            string imageUrl =
                $"/images/products/{fileName}";


            return (
                true,
                imageUrl,
                null
            );
        }


        private static string CreateProductCode()
        {
            return $"P-{DateTime.Now:yyyyMMddHHmmss}";
        }


        private static string CreateAlias(
            string text)
        {
            if (string.IsNullOrWhiteSpace(
                    text))
            {
                return "";
            }


            var normalized =
                text
                    .ToLowerInvariant()
                    .Trim();


            normalized = normalized
                .Replace("đ", "d")

                .Replace("á", "a")
                .Replace("à", "a")
                .Replace("ả", "a")
                .Replace("ã", "a")
                .Replace("ạ", "a")

                .Replace("ă", "a")
                .Replace("ắ", "a")
                .Replace("ằ", "a")
                .Replace("ẳ", "a")
                .Replace("ẵ", "a")
                .Replace("ặ", "a")

                .Replace("â", "a")
                .Replace("ấ", "a")
                .Replace("ầ", "a")
                .Replace("ẩ", "a")
                .Replace("ẫ", "a")
                .Replace("ậ", "a")

                .Replace("é", "e")
                .Replace("è", "e")
                .Replace("ẻ", "e")
                .Replace("ẽ", "e")
                .Replace("ẹ", "e")

                .Replace("ê", "e")
                .Replace("ế", "e")
                .Replace("ề", "e")
                .Replace("ể", "e")
                .Replace("ễ", "e")
                .Replace("ệ", "e")

                .Replace("í", "i")
                .Replace("ì", "i")
                .Replace("ỉ", "i")
                .Replace("ĩ", "i")
                .Replace("ị", "i")

                .Replace("ó", "o")
                .Replace("ò", "o")
                .Replace("ỏ", "o")
                .Replace("õ", "o")
                .Replace("ọ", "o")

                .Replace("ô", "o")
                .Replace("ố", "o")
                .Replace("ồ", "o")
                .Replace("ổ", "o")
                .Replace("ỗ", "o")
                .Replace("ộ", "o")

                .Replace("ơ", "o")
                .Replace("ớ", "o")
                .Replace("ờ", "o")
                .Replace("ở", "o")
                .Replace("ỡ", "o")
                .Replace("ợ", "o")

                .Replace("ú", "u")
                .Replace("ù", "u")
                .Replace("ủ", "u")
                .Replace("ũ", "u")
                .Replace("ụ", "u")

                .Replace("ư", "u")
                .Replace("ứ", "u")
                .Replace("ừ", "u")
                .Replace("ử", "u")
                .Replace("ữ", "u")
                .Replace("ự", "u")

                .Replace("ý", "y")
                .Replace("ỳ", "y")
                .Replace("ỷ", "y")
                .Replace("ỹ", "y")
                .Replace("ỵ", "y");


            normalized =
                Regex.Replace(
                    normalized,
                    @"[^a-z0-9\s-]",
                    "");


            normalized =
                Regex.Replace(
                    normalized,
                    @"\s+",
                    "-");


            normalized =
                Regex.Replace(
                    normalized,
                    @"-+",
                    "-");


            return normalized.Trim('-');
        }
    }
}