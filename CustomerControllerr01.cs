using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MiniSmartstoreMvc.Data;
using MiniSmartstoreMvc.Models;
using MiniSmartstoreMvc.ViewModels;

namespace MiniSmartstoreMvc.Controllers
{
    [Authorize]
    public class CustomerController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly RoleManager<IdentityRole> _roleManager;
        public CustomerController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _context = context;
            _roleManager = roleManager;
            _context = context;
        }

        public async Task<IActionResult> Info()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var model = new CustomerInfoViewModel
            {
                FullName = user.FullName ?? "",
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                PreferredShippingMethodId = user.PreferredShippingMethodId,
                PreferredPaymentMethod = user.PreferredPaymentMethod
            };

            await LoadCustomerInfoOptionsAsync(model);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Info(CustomerInfoViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            model.Email = user.Email ?? "";

            if (!ModelState.IsValid)
            {
                await LoadCustomerInfoOptionsAsync(model);
                return View(model);
            }

            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;
            user.Address = model.Address;
            user.PreferredShippingMethodId = model.PreferredShippingMethodId;
            user.PreferredPaymentMethod = model.PreferredPaymentMethod;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["Success"] = "Đã cập nhật thông tin tài khoản.";
                return RedirectToAction(nameof(Info));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            await LoadCustomerInfoOptionsAsync(model);
            return View(model);
        }

        public async Task<IActionResult> Addresses()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var model = new CustomerInfoViewModel
            {
                FullName = user.FullName ?? "",
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber,
                Address = user.Address
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Addresses(CustomerInfoViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;
            user.Address = model.Address;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["Success"] = "Đã cập nhật địa chỉ nhận hàng.";
                return RedirectToAction(nameof(Addresses));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearAddress()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            user.Address = null;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["Success"] = "Đã xóa địa chỉ nhận hàng.";
            }

            return RedirectToAction(nameof(Addresses));
        }

        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var result = await _userManager.ChangePasswordAsync(
                user,
                model.OldPassword,
                model.NewPassword
            );

            if (result.Succeeded)
            {
                TempData["Success"] = "Đã đổi mật khẩu thành công.";
                return RedirectToAction(nameof(ChangePassword));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        private async Task LoadCustomerInfoOptionsAsync(CustomerInfoViewModel model)
        {
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
        private async Task LoadFormOptionsAsync(AdminCustomerFormViewModel model)
        {
            model.AvailableRoles = await _roleManager.Roles
                .OrderBy(x => x.Name)
                .Select(x => new SelectListItem
                {
                    Value = x.Name ?? "",
                    Text = x.Name ?? ""
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

    }
}