using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenCvSharp;
using WebApplication1.Models;

namespace WebApplication1.Pages
{
    public class CannyModel : PageModel
    {
        public async Task<IActionResult> OnPostUploadAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            await using var srcStream = FileUpload.FormFile.OpenReadStream();

            using var srcMat = Mat.FromStream(srcStream, ImreadModes.Grayscale);
            using var dstMat = new Mat();
            Cv2.Canny(srcMat, dstMat, Threshold1, Threshold2);

            SourceBase64 = Convert.ToBase64String(srcMat.ToBytes(".png"));
            ResultBase64 = Convert.ToBase64String(dstMat.ToBytes(".png"));
            return Page();
        }

        [BindProperty]
        public BufferedSingleFileUploadPhysical FileUpload { get; set; }

        [BindProperty]
        public int Threshold1 { get; set; } = 50;

        [BindProperty]
        public int Threshold2 { get; set; } = 100;

        public string SourceBase64 { get; private set; }
        public string ResultBase64 { get; private set; }
    }
}
