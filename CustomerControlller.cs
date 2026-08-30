using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MiniSmartstoreMvc.Data;
using MiniSmartstoreMvc.Models;
using MiniSmartstoreMvc.ViewModels;

namespace MiniSmartstoreMvc.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CustomerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public CustomerController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(List));
        }

        public async Task<IActionResult> List(
            string? search,
            string? role,
            string? active,
            int page = 1,
            int pageSize = 25)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize <= 0 ? 25 : pageSize;

            var users = await _userManager.Users
                .OrderBy(u => u.Email)
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(search))
            {
                users = users.Where(u =>
                    (u.Email != null && u.Email.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                    (u.UserName != null && u.UserName.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                    (u.FullName != null && u.FullName.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                    (u.Address != null && u.Address.Contains(search, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(role))
            {
                var filtered = new List<ApplicationUser>();

                foreach (var user in users)
                {
                    if (await _userManager.IsInRoleAsync(user, role))
                    {
                        filtered.Add(user);
                    }
                }

                users = filtered;
            }

            if (!string.IsNullOrWhiteSpace(active))
            {
                var needActive = active == "active";

                users = users
                    .Where(u => IsUserActive(u) == needActive)
                    .ToList();
            }

            var userIds = users.Select(u => u.Id).ToList();

            var orderStats = await _context.Orders
                .Where(o => userIds.Contains(o.UserId))
                .GroupBy(o => o.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    OrderCount = g.Count(),
                    TotalSpent = g
                        .Where(o => o.OrderStatus == OrderStatus.Completed || o.PaymentStatus == PaymentStatus.Paid)
                        .Sum(o => o.TotalAmount)
                })
                .ToDictionaryAsync(x => x.UserId, x => x);

            var cartStats = await _context.CartItems
                .Where(c => userIds.Contains(c.UserId))
                .GroupBy(c => c.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    CartItemCount = g.Sum(x => x.Quantity)
                })
                .ToDictionaryAsync(x => x.UserId, x => x.CartItemCount);

            var allItems = new List<AdminCustomerListItemViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                orderStats.TryGetValue(user.Id, out var orderInfo);
                cartStats.TryGetValue(user.Id, out var cartCount);

                allItems.Add(new AdminCustomerListItemViewModel
                {
                    Id = user.Id,
                    UserId = user.Id,
                    Email = user.Email ?? "",
                    UserName = user.UserName ?? "",
                    FullName = string.IsNullOrWhiteSpace(user.FullName) ? "Chưa có tên" : user.FullName,
                    PhoneNumber = user.PhoneNumber ?? "",
                    Address = user.Address ?? "",
                    RolesText = roles.Any() ? string.Join(", ", roles) : "Chưa có vai trò",
                    IsActive = IsUserActive(user),
                    OrderCount = orderInfo?.OrderCount ?? 0,
                    TotalSpent = orderInfo?.TotalSpent ?? 0,
                    CartItemCount = cartCount
                });
            }

            var totalItems = allItems.Count;
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));

            if (page > totalPages)
            {
                page = totalPages;
            }

            var model = new AdminCustomerListViewModel
            {
                Items = allItems
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList(),

                Search = search,
                Role = role,
                Active = active,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages,
                Roles = await GetRoleSelectListAsync(role)
            };

            return View(model);
        }

        public async Task<IActionResult> Create()
        {
            var model = new AdminCustomerFormViewModel
            {
                IsActive = true,
                SelectedRoles = new List<string> { "Customer" }
            };

            await LoadFormOptionsAsync(model);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdminCustomerFormViewModel model, bool saveContinue = false)
        {
            await LoadFormOptionsAsync(model);

            if (string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.AddModelError(nameof(model.Password), "Vui lòng nhập mật khẩu.");
            }

            if (model.Password != model.ConfirmPassword)
            {
                ModelState.AddModelError(nameof(model.ConfirmPassword), "Mật khẩu nhập lại không khớp.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var email = model.Email.Trim();

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                Address = model.Address,
                PreferredShippingMethodId = model.PreferredShippingMethodId,
                PreferredPaymentMethod = model.PreferredPaymentMethod,
                LockoutEnabled = true,
                LockoutEnd = model.IsActive ? null : DateTimeOffset.UtcNow.AddYears(100)
            };

            var result = await _userManager.CreateAsync(user, model.Password!);

            if (!result.Succeeded)
            {
                AddIdentityErrors(result);
                return View(model);
            }

            var selectedRoles = model.SelectedRoles ?? new List<string>();

            if (selectedRoles.Any())
            {
                await _userManager.AddToRolesAsync(user, selectedRoles);
            }
            else if (await _roleManager.RoleExistsAsync("Customer"))
            {
                await _userManager.AddToRoleAsync(user, "Customer");
            }

            TempData["Success"] = "Đã thêm khách hàng mới.";

            if (saveContinue)
            {
                return RedirectToAction(nameof(Edit), new { id = user.Id });
            }

            return RedirectToAction(nameof(List));
        }

        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            var model = await BuildCustomerFormModelAsync(user);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AdminCustomerFormViewModel model, bool saveContinue = false)
        {
            if (string.IsNullOrWhiteSpace(model.Id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(model.Id);

            if (user == null)
            {
                return NotFound();
            }

            await LoadFormOptionsAsync(model);
            await LoadCustomerExtraDataAsync(model, user);

            if (!string.IsNullOrWhiteSpace(model.Password) || !string.IsNullOrWhiteSpace(model.ConfirmPassword))
            {
                if (model.Password != model.ConfirmPassword)
                {
                    ModelState.AddModelError(nameof(model.ConfirmPassword), "Mật khẩu nhập lại không khớp.");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var email = model.Email.Trim();

            user.Email = email;
            user.UserName = email;
            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;
            user.Address = model.Address;
            user.PreferredShippingMethodId = model.PreferredShippingMethodId;
            user.PreferredPaymentMethod = model.PreferredPaymentMethod;
            user.LockoutEnabled = true;
            user.LockoutEnd = model.IsActive ? null : DateTimeOffset.UtcNow.AddYears(100);

            var updateResult = await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                AddIdentityErrors(updateResult);
                return View(model);
            }

            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passwordResult = await _userManager.ResetPasswordAsync(user, token, model.Password);

                if (!passwordResult.Succeeded)
                {
                    AddIdentityErrors(passwordResult);
                    return View(model);
                }
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            var selectedRoles = model.SelectedRoles ?? new List<string>();

            var rolesToRemove = currentRoles.Except(selectedRoles).ToList();
            var rolesToAdd = selectedRoles.Except(currentRoles).ToList();

            if (rolesToRemove.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            }

            if (rolesToAdd.Any())
            {
                await _userManager.AddToRolesAsync(user, rolesToAdd);
            }

            TempData["Success"] = "Đã cập nhật khách hàng.";

            if (saveContinue)
            {
                return RedirectToAction(nameof(Edit), new { id = user.Id });
            }

            return RedirectToAction(nameof(List));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            var isActive = IsUserActive(user);

            user.LockoutEnabled = true;
            user.LockoutEnd = isActive ? DateTimeOffset.UtcNow.AddYears(100) : null;

            await _userManager.UpdateAsync(user);

            TempData["Success"] = isActive
                ? "Đã khóa tài khoản khách hàng."
                : "Đã mở khóa tài khoản khách hàng.";

            return RedirectToAction(nameof(List));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var currentUserId = _userManager.GetUserId(User);

            if (id == currentUserId)
            {
                TempData["Error"] = "Không thể xóa chính tài khoản đang đăng nhập.";
                return RedirectToAction(nameof(List));
            }

            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            var hasOrders = await _context.Orders.AnyAsync(o => o.UserId == id);

            if (hasOrders)
            {
                user.LockoutEnabled = true;
                user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);

                await _userManager.UpdateAsync(user);

                TempData["Success"] = "Khách hàng đã có đơn hàng nên hệ thống không xóa dữ liệu. Tài khoản đã được khóa.";
                return RedirectToAction(nameof(List));
            }

            var cartItems = await _context.CartItems
                .Where(c => c.UserId == id)
                .ToListAsync();

            if (cartItems.Any())
            {
                _context.CartItems.RemoveRange(cartItems);
                await _context.SaveChangesAsync();
            }

            var result = await _userManager.DeleteAsync(user);

            if (!result.Succeeded)
            {
                AddIdentityErrors(result);
                TempData["Error"] = "Không thể xóa khách hàng này.";
                return RedirectToAction(nameof(List));
            }

            TempData["Success"] = "Đã xóa khách hàng.";

            return RedirectToAction(nameof(List));
        }

        private async Task<AdminCustomerFormViewModel> BuildCustomerFormModelAsync(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);

            var model = new AdminCustomerFormViewModel
            {
                Id = user.Id,
                Email = user.Email ?? "",
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                PreferredShippingMethodId = user.PreferredShippingMethodId,
                PreferredPaymentMethod = user.PreferredPaymentMethod,
                IsActive = IsUserActive(user),
                SelectedRoles = roles.ToList()
            };

            await LoadFormOptionsAsync(model);
            await LoadCustomerExtraDataAsync(model, user);

            return model;
        }

        private async Task LoadCustomerExtraDataAsync(AdminCustomerFormViewModel model, ApplicationUser user)
        {
            model.Orders = await _context.Orders
                .Where(o => o.UserId == user.Id)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new AdminCustomerOrderItemViewModel
                {
                    Id = o.Id,
                    OrderCode = o.OrderCode,
                    CreatedAt = o.CreatedAt,
                    TotalAmount = o.TotalAmount,
                    OrderStatusText = GetOrderStatusText(o.OrderStatus),
                    PaymentStatusText = GetPaymentStatusText(o.PaymentStatus)
                })
                .ToListAsync();

            model.CartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == user.Id)
                .Select(c => new AdminCustomerCartItemViewModel
                {
                    ProductName = c.Product != null ? c.Product.Name : "Sản phẩm không còn tồn tại",
                    ProductActive = c.Product != null && c.Product.IsActive,
                    Quantity = c.Quantity,
                    UnitPrice = c.Product != null ? c.Product.Price : 0,
                    TotalPrice = c.Product != null ? c.Product.Price * c.Quantity : 0,
                    UpdatedAt = null
                })
                .ToListAsync();

            model.AddressInfo = new AdminCustomerAddressViewModel
            {
                CustomerName = string.IsNullOrWhiteSpace(user.FullName) ? user.Email ?? "" : user.FullName,
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber ?? "",
                Address = user.Address ?? ""
            };
        }

        private async Task LoadFormOptionsAsync(AdminCustomerFormViewModel model)
        {
            model.AvailableRoles = await _roleManager.Roles
                .OrderBy(x => x.Name)
                .Select(x => new SelectListItem
                {
                    Value = x.Name ?? "",
                    Text = GetRoleText(x.Name),
                    Selected = model.SelectedRoles != null && x.Name != null && model.SelectedRoles.Contains(x.Name)
                })
                .ToListAsync();

            model.ShippingMethods = await _context.ShippingMethods
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.Name,
                    Selected = model.PreferredShippingMethodId == x.Id
                })
                .ToListAsync();

            model.PaymentMethods = Enum.GetValues<PaymentMethod>()
                .Select(x => new SelectListItem
                {
                    Value = ((int)x).ToString(),
                    Text = GetPaymentMethodText(x),
                    Selected = model.PreferredPaymentMethod == x
                })
                .ToList();
        }

        private async Task<List<SelectListItem>> GetRoleSelectListAsync(string? selectedRole = null)
        {
            return await _roleManager.Roles
                .OrderBy(r => r.Name)
                .Select(r => new SelectListItem
                {
                    Value = r.Name ?? "",
                    Text = GetRoleText(r.Name),
                    Selected = selectedRole != null && r.Name == selectedRole
                })
                .ToListAsync();
        }

        private static bool IsUserActive(ApplicationUser user)
        {
            return !user.LockoutEnd.HasValue || user.LockoutEnd.Value <= DateTimeOffset.UtcNow;
        }

        private void AddIdentityErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        private static string GetRoleText(string? roleName)
        {
            return roleName switch
            {
                "Admin" => "Quản trị viên",
                "Customer" => "Khách hàng",
                "Administrator" => "Quản trị viên",
                "Registered" => "Khách hàng đã đăng ký",
                "Guest" => "Khách vãng lai",
                null => "",
                _ => roleName
            };
        }

        private static string GetOrderStatusText(OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Pending => "Chờ xử lý",
                OrderStatus.Processing => "Đang xử lý",
                OrderStatus.Completed => "Hoàn thành",
                OrderStatus.Cancelled => "Đã hủy",
                _ => "Không xác định"
            };
        }

        private static string GetPaymentStatusText(PaymentStatus status)
        {
            return status switch
            {
                PaymentStatus.Pending => "Chờ thanh toán",
                PaymentStatus.Paid => "Đã thanh toán",
                PaymentStatus.Failed => "Thất bại",
                PaymentStatus.Cancelled => "Đã hủy",
                _ => "Không xác định"
            };
        }

        private static string GetPaymentMethodText(PaymentMethod method)
        {
            return method switch
            {
                PaymentMethod.CashOnDelivery => "Thanh toán khi nhận hàng",
                PaymentMethod.BankTransfer => "Chuyển khoản ngân hàng",
                PaymentMethod.OnlinePaymentComingSoon => "Thanh toán online",
                _ => "Không xác định"
            };
        }
    }
}