using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MiniSmartstoreMvc.Models;

namespace MiniSmartstoreMvc.Data
{
    public static class DbSeeder
    {
        public static async Task SeedRolesAndAdminAsync(
            IServiceProvider serviceProvider, 
            bool seedDemoData = false)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            await context.Database.MigrateAsync();

            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            string[] roles = { "Admin", "Customer" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            string adminEmail = "admin@mini.com";
            string adminPassword = "Admin@123456";

            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Administrator",
                    EmailConfirmed = true,
                    CreatedAt = DateTime.Now
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
            else
            {
                if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }

                if (adminUser.CreatedAt < new DateTime(2000, 1, 1))
                {
                    adminUser.CreatedAt = DateTime.Now;
                    await userManager.UpdateAsync(adminUser);
                }
            }

            var usersWithoutCreatedAt = await context.Users
                .Where(u => u.CreatedAt < new DateTime(2000, 1, 1))
                .ToListAsync();

            foreach (var user in usersWithoutCreatedAt)
            {
                user.CreatedAt = DateTime.Now;
            }

            if (usersWithoutCreatedAt.Any())
            {
                await context.SaveChangesAsync();
            }
            if (!seedDemoData)
            {
                return;
            }
            if (!await context.Categories.AnyAsync())
            {
                var categories = new List<Category>
                {
                    new Category
                    {
                        Name = "Điện thoại",
                        Description = "Các sản phẩm điện thoại thông minh",
                        IsActive = true
                    },
                    new Category
                    {
                        Name = "Laptop",
                        Description = "Máy tính xách tay",
                        IsActive = true
                    },
                    new Category
                    {
                        Name = "Phụ kiện",
                        Description = "Phụ kiện công nghệ",
                        IsActive = true
                    },
                    new Category
                    {
                        Name = "Thời trang",
                        Description = "Sản phẩm thời trang",
                        IsActive = true
                    }
                };

                context.Categories.AddRange(categories);
                await context.SaveChangesAsync();
            }

            if (!await context.Products.AnyAsync())
            {
                var phoneCategory = await context.Categories.FirstAsync(c => c.Name == "Điện thoại");
                var laptopCategory = await context.Categories.FirstAsync(c => c.Name == "Laptop");
                var accessoryCategory = await context.Categories.FirstAsync(c => c.Name == "Phụ kiện");

                var products = new List<Product>
                {
                    new Product
                    {
                        Name = "iPhone 15",
                        Description = "Điện thoại Apple iPhone 15",
                        Price = 19990000,
                        OldPrice = 22990000,
                        StockQuantity = 20,
                        ImageUrl = "/images/products/iphone.jpg",
                        IsFeatured = true,
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        CategoryId = phoneCategory.Id
                    },
                    new Product
                    {
                        Name = "Samsung Galaxy S24",
                        Description = "Điện thoại Samsung Galaxy S24",
                        Price = 17990000,
                        OldPrice = 19990000,
                        StockQuantity = 15,
                        ImageUrl = "/images/products/samsung.jpg",
                        IsFeatured = true,
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        CategoryId = phoneCategory.Id
                    },
                    new Product
                    {
                        Name = "MacBook Air M2",
                        Description = "Laptop Apple MacBook Air M2",
                        Price = 24990000,
                        OldPrice = 27990000,
                        StockQuantity = 10,
                        ImageUrl = "/images/products/macbook.jpg",
                        IsFeatured = true,
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        CategoryId = laptopCategory.Id
                    },
                    new Product
                    {
                        Name = "Tai nghe Bluetooth",
                        Description = "Tai nghe không dây chất lượng cao",
                        Price = 990000,
                        OldPrice = 1290000,
                        StockQuantity = 50,
                        ImageUrl = "/images/products/headphone.jpg",
                        IsFeatured = false,
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        CategoryId = accessoryCategory.Id
                    }
                };

                context.Products.AddRange(products);
                await context.SaveChangesAsync();
            }

            var productsWithoutBasePrice = await context.Products
    .Where(p => p.BasePrice <= 0)
    .ToListAsync();

            foreach (var product in productsWithoutBasePrice)
            {
                product.BasePrice = product.Price;
            }

            if (productsWithoutBasePrice.Any())
            {
                await context.SaveChangesAsync();
            }

            if (!await context.ProductColors.AnyAsync())
            {
                var allProducts = await context.Products.ToListAsync();

                var colors = new List<ProductColor>();

                foreach (var product in allProducts)
                {
                    colors.Add(new ProductColor
                    {
                        ProductId = product.Id,
                        ColorName = "Đen",
                        ColorCode = "#111827"
                    });

                    colors.Add(new ProductColor
                    {
                        ProductId = product.Id,
                        ColorName = "Trắng",
                        ColorCode = "#f8fafc"
                    });

                    colors.Add(new ProductColor
                    {
                        ProductId = product.Id,
                        ColorName = "Xanh",
                        ColorCode = "#2563eb"
                    });
                }

                context.ProductColors.AddRange(colors);
                await context.SaveChangesAsync();
            }

            if (!await context.ShippingMethods.AnyAsync())
            {
                var shippingMethods = new List<ShippingMethod>
                {
                    new ShippingMethod
                    {
                        Name = "Miễn phí giao hàng",
                        Description = "Áp dụng cho đơn hàng lớn hoặc chương trình khuyến mãi",
                        Fee = 0,
                        IsActive = true
                    },
                    new ShippingMethod
                    {
                        Name = "Giao hàng tiêu chuẩn",
                        Description = "Nhận hàng sau 3 - 5 ngày",
                        Fee = 30000,
                        IsActive = true
                    },
                    new ShippingMethod
                    {
                        Name = "Giao hàng nhanh",
                        Description = "Nhận hàng sau 1 - 2 ngày",
                        Fee = 50000,
                        IsActive = true
                    }
                };

                context.ShippingMethods.AddRange(shippingMethods);
                await context.SaveChangesAsync();
            }

            if (!await context.Coupons.AnyAsync())
            {
                var coupons = new List<Coupon>
                {
                    new Coupon
                    {
                        Code = "SALE50K",
                        Description = "Giảm 50.000đ cho đơn hàng từ 500.000đ",
                        DiscountType = DiscountType.FixedAmount,
                        DiscountValue = 50000,
                        MinOrderAmount = 500000,
                        MaxDiscountAmount = null,
                        UsageLimit = 100,
                        UsedCount = 0,
                        StartDate = DateTime.Now.AddDays(-1),
                        EndDate = DateTime.Now.AddMonths(3),
                        IsActive = true
                    },
                    new Coupon
                    {
                        Code = "VIP10",
                        Description = "Giảm 10%, tối đa 200.000đ cho đơn hàng từ 1.000.000đ",
                        DiscountType = DiscountType.Percentage,
                        DiscountValue = 10,
                        MinOrderAmount = 1000000,
                        MaxDiscountAmount = 200000,
                        UsageLimit = 100,
                        UsedCount = 0,
                        StartDate = DateTime.Now.AddDays(-1),
                        EndDate = DateTime.Now.AddMonths(3),
                        IsActive = true
                    }
                };

                context.Coupons.AddRange(coupons);
                await context.SaveChangesAsync();
            }
        }
    }
}