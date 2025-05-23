using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vedect.Data;
using Vedect.Models.Domain;

namespace Vedect.Pages.Admin
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _dbContext;

        public IndexModel(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public List<User> Users { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminUsername")))
            {
                RedirectToPage("/Admin/Login");
            }

            Users = await _dbContext.Users
                .Include(u => u.SubscriptionPlan)
                .ToListAsync();
            return Page();
        }
    }
}
