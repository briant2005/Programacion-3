using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BBAPP.Pages
{
    [IgnoreAntiforgeryToken]
    public class TestFormModel : PageModel
    {
        [BindProperty]
        public InputModel Input { get; set; }
        public class InputModel { public string TestValue { get; set; } }

        public IActionResult OnPost()
        {
            Console.WriteLine($"Form Posted: {Input?.TestValue}");
            return Page();
        }
    }

}
