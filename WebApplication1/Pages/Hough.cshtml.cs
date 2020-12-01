using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using WebApplication1.Models;

namespace WebApplication1.Pages
{
    public class HoughModel : PageModel
    {
        public async Task<IActionResult> OnPostUploadAsync()
        {
            if (!ModelState.IsValid)
            {
                ResultBase64 = "";
                return Page();
            }

            await using var srcStream = FileUpload.FormFile.OpenReadStream();

            using var src = Mat.FromStream(srcStream, ImreadModes.Grayscale);
            using var canny = new Mat();
            Cv2.Canny(src, canny, Threshold1, Threshold2);
            var lines = Cv2.HoughLinesP(canny, 1, Cv2.PI / 180, 50, 50, 10);

            using var lineView = new Mat();
            Cv2.CvtColor(canny, lineView, ColorConversionCodes.GRAY2BGR);
            Draw(lines, lineView);

            SourceBase64 = Convert.ToBase64String(src.ToBytes(".png"));
            ResultBase64 = Convert.ToBase64String(lineView.ToBytes(".png"));
            return Page();
        }

        private static void Draw(IEnumerable<LineSegmentPoint> lines, Mat dst)
        {
            // Draw the lines
            foreach (var line in lines)
            {
                Cv2.Line(dst, line.P1, line.P2, new Scalar(0, 0, 255), 3, LineTypes.AntiAlias);
            }
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
