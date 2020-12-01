using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using OpenCvSharp.XFeatures2D;
using WebApplication1.Models;

namespace WebApplication1.Pages
{
    public class AkazeModel : PageModel
    {
        public async Task<IActionResult> OnPostUploadAsync()
        {
            if (!ModelState.IsValid)
            {
                ResultBase64 = "";
                return Page();
            }

            await using var srcStream1 = FileUpload1.FormFile.OpenReadStream();
            await using var srcStream2 = FileUpload2.FormFile.OpenReadStream();

            using var src1 = Mat.FromStream(srcStream1, ImreadModes.Color);
            using var src2 = Mat.FromStream(srcStream2, ImreadModes.Color);
            using var view = MatchBySurf(src1, src2);

            Source1Base64 = Convert.ToBase64String(src1.ToBytes(".png"));
            Source2Base64 = Convert.ToBase64String(src2.ToBytes(".png"));
            ResultBase64 = Convert.ToBase64String(view.ToBytes(".png"));
            return Page();
        }

        private Mat MatchBySurf(Mat src1, Mat src2)
        {
            using var gray1 = new Mat();
            using var gray2 = new Mat();

            Cv2.CvtColor(src1, gray1, ColorConversionCodes.BGR2GRAY);
            Cv2.CvtColor(src2, gray2, ColorConversionCodes.BGR2GRAY);

            //using var surf = SURF.Create(200, 4, 2, true);
            using var surf = AKAZE.Create();

            // Detect the keypoints and generate their descriptors using SURF
            using var descriptors1 = new Mat<float>();
            using var descriptors2 = new Mat<float>();
            surf.DetectAndCompute(gray1, null, out var keypoints1, descriptors1);
            surf.DetectAndCompute(gray2, null, out var keypoints2, descriptors2);

            // Match descriptor vectors 
            using var bfMatcher = new BFMatcher(NormTypes.L2, false);
            DMatch[] bfMatches = bfMatcher.Match(descriptors1, descriptors2);

            // Draw matches
            var bfView = new Mat();
            Cv2.DrawMatches(gray1, keypoints1, gray2, keypoints2, bfMatches, bfView, flags: DrawMatchesFlags.NotDrawSinglePoints);

            return bfView;
        }

        [BindProperty]
        public BufferedSingleFileUploadPhysical FileUpload1 { get; set; }
        [BindProperty]
        public BufferedSingleFileUploadPhysical FileUpload2 { get; set; }

        public string Source1Base64 { get; private set; }
        public string Source2Base64 { get; private set; }
        public string ResultBase64 { get; private set; }
    }
}
