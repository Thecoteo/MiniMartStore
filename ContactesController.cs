using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MiniSmartstoreMvc.Models;
using MiniSmartstoreMvc.ViewModels;

namespace MiniSmartstoreMvc.Controllers
{
    public class ContactController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ContactController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var model = new ContactViewModel();

            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);

                if (user != null)
                {
                    model.FullName = user.FullName ?? "";
                    model.Email = user.Email ?? "";
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(ContactViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            TempData["Success"] = "Cảm ơn bạn đã liên hệ. Chúng tôi sẽ phản hồi trong thời gian sớm nhất.";

            return RedirectToAction(nameof(Index));
        }
    }
}