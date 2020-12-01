using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenCvSharp;
using WebApplication1.Models;

namespace WebApplication1.Pages
{
    public class KuwaharaFilterModel : PageModel
    {
        public async Task<IActionResult> OnPostUploadAsync()
        {
            if (!ModelState.IsValid)
            {
                ResultBase64 = "";
                return Page();
            }

            await using var srcStream = FileUpload.FormFile.OpenReadStream();

            using var src = Mat.FromStream(srcStream, ImreadModes.Color);
            using var dst = new Mat();
            Kuwahara(src, dst);

            SourceBase64 = Convert.ToBase64String(src.ToBytes(".png"));
            ResultBase64 = Convert.ToBase64String(dst.ToBytes(".png"));
            return Page();
        }

        private static void Kuwahara(Mat src, Mat dst)
        {
            const int kernelSize = 5;
            const int margin = kernelSize / 2 + 1;

            if (src.Type() != MatType.CV_8UC3)
                throw new ArgumentException("src.Type() != 8UC3", nameof(src));

            using var srcBorder = new Mat();
            Cv2.CopyMakeBorder(src, srcBorder, margin, margin, margin, margin, BorderTypes.Reflect);

            using var sum = new Mat<Vec3i>();
            using var sqSum = new Mat<Vec3d>();
            Cv2.Integral(srcBorder, sum, sqSum);

            int w = src.Cols;
            int h = src.Rows;

            dst.Create(h, w, src.Type());
            var dstIndexer = dst.GetUnsafeGenericIndexer<Vec3b>();

            Parallel.For(margin, h + margin, y =>
            {
                for (int x = margin; x < w + margin; x++)
                {
                    var a = MeanAndVariance(y - margin, x - margin, margin, sum, sqSum);
                    var b = MeanAndVariance(y - margin, x - 0, margin, sum, sqSum);
                    var c = MeanAndVariance(y - 0, x - 2, margin, sum, sqSum);
                    var d = MeanAndVariance(y - 0, x - 0, margin, sum, sqSum);
                    var min = new[] {a, b, c, d}.OrderBy(mv => mv.Variance[0] + mv.Variance[1] + mv.Variance[2]).First();
                    dstIndexer[y - margin, x - margin] = new Vec3b(
                        (byte) min.Mean[0],
                        (byte) min.Mean[1],
                        (byte) min.Mean[2]);
                }
            });
        }

        private static (Vec3d Mean, Vec3d Variance) MeanAndVariance(int y, int x, int kernelSize, Mat<Vec3i> sum, Mat<Vec3d> sqSum)
        {
            int kernelPixels = kernelSize * kernelSize;
            int kx = x + kernelSize;
            int ky = y + kernelSize;

            var sumIndexer = sum.GetIndexer();
            var sqSumIndexer = sqSum.GetIndexer();

            var sumVal = IRoi(sumIndexer[ky, kx], sumIndexer[ky, x], sumIndexer[y, kx], sumIndexer[y, x]);
            var sqSumVal = DRoi(sqSumIndexer[ky, kx], sqSumIndexer[ky, x], sqSumIndexer[y, kx], sqSumIndexer[y, x]);
            var mean = new Vec3d(
                sumVal[0] / (double)kernelPixels, 
                sumVal[1] / (double)kernelPixels, 
                sumVal[2] / (double)kernelPixels);
            var variance = new Vec3d(
                (sqSumVal[0] / kernelPixels) - (mean[0] * mean[0]),
                (sqSumVal[1] / kernelPixels) - (mean[1] * mean[1]),
                (sqSumVal[2] / kernelPixels) - (mean[2] * mean[2]));

            return (mean, variance);

            static Vec3i IRoi(Vec3i d, Vec3i c, Vec3i b, Vec3i a)
            {
                var result = new int[3];
                for (var i = 0; i < 3; i++)
                {
                    result[i] = d[i] - c[i] - b[i] + a[i];
                }
                return new Vec3i(result[0], result[1], result[2]);
            }
            static Vec3d DRoi(Vec3d d, Vec3d c, Vec3d b, Vec3d a)
            {
                var result = new double[3];
                for (var i = 0; i < 3; i++)
                {
                    result[i] = d[i] - c[i] - b[i] + a[i];
                }
                return new Vec3d(result[0], result[1], result[2]);
            }
        } 

        [BindProperty]
        public BufferedSingleFileUploadPhysical FileUpload { get; set; }

        public string SourceBase64 { get; private set; }
        public string ResultBase64 { get; private set; }
    }
}
