using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyLibrary;

namespace ExampleApp.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IMyService _myService;

        public IndexModel(ILogger<IndexModel> logger, IMyService myService)
        {
            this._logger = logger;
            this._myService = myService;
        }

        public async Task<IActionResult> OnGet()
        {
            await this._myService.ReadMessage(messagePrefix: null).ConfigureAwait(false);

            return Page();
        }
    }
}
