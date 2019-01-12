using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using ExifLibrary;
using JpegParser;


namespace convertToVr180Photo
{
    class MainClass
    {


        public static void Main(string[] args)
        {


            string GetArgument(IEnumerable<string> opts, string option) => opts.SkipWhile(i => i != option).Skip(1).Take(1).FirstOrDefault();

            var format = GetArgument(args, "-f");
            var fov = GetArgument(args, "-v");
            string equiJpeg = GetArgument(args, "-i");
            string outJpeg = GetArgument(args, "-o");

            if (format == null || equiJpeg == null || outJpeg == null)
            {
                Usage();
                return;
            }

            var validFormats = new string[] { "lr", "rl", "tb", "bt" };
            if (! validFormats.Contains(format)) {
                Usage();
                return;
            }

            if (fov == null) fov = "180x180";
            Regex checkFov = new Regex(@"^\d+x\d+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            if (! checkFov.IsMatch(fov))
            {
                Usage();
                return;
            }

            var fovValues = fov.Split( new char[] {'x', 'X'});
            if (fovValues.Length != 2)
            {
                Usage();
                return;
            }

            float widthDegrees = float.Parse(fovValues[0]);
            float heightDegrees = float.Parse(fovValues[1]);

            Console.WriteLine("Starting");

            string base64jpeg;

            var jpegFile = new JpegParser.JpegFile();
            var jpegs = jpegFile.SplitImage(equiJpeg, format);

            // the right image gets embed in the left image.
            using (EncoderParameters encoderParameters = new EncoderParameters(1))
            using (EncoderParameter encoderParameter = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 90L))
            using (MemoryStream jpegms = new MemoryStream())
            {
                ImageCodecInfo codecInfo = ImageCodecInfo.GetImageDecoders().First(codec => codec.FormatID == ImageFormat.Jpeg.Guid);
                encoderParameters.Param[0] = encoderParameter;
                jpegs.GetRightEye.Save(jpegms, codecInfo, encoderParameters);
                jpegms.Position = 0;
                base64jpeg = Convert.ToBase64String(jpegms.ToArray());
            }

            string extendedXmpXml = "\n<x:xmpmeta xmlns:x=\"adobe:ns:meta/\""
                            + " x:xmptk=\"Adobe XMP\">"
                            + " <rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\">"
                            + " <rdf:Description xmlns:GImage=\"http://ns.google.com/photos/1.0/image/\""
                            + " rdf:about=\"\""
                            + " GImage:Data=\""
                            + base64jpeg + "\"/></rdf:RDF></x:xmpmeta>";

            MD5 md5 = MD5.Create();
            var extendedMd5Hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(extendedXmpXml));

            string xmpMetadata = jpegFile.GetXmpMetadata(jpegs.GetRightEye.Width, jpegs.GetRightEye.Height, widthDegrees, heightDegrees, extendedMd5Hash);


            // insert the xmp in the jpeg..
            jpegFile.WriteVr180Jpeg(jpegs, xmpMetadata, extendedMd5Hash, extendedXmpXml, outJpeg);

            Console.WriteLine("Finished");

        }

        private static void Usage ()
        {
            Console.WriteLine("Usage: equiToVr180Photo -f (lr|rl|tb|bt) -i equirectangular.jpg -o vr180.jpg [-v 180x180]");
            Console.WriteLine("Mono Usage: mono (lr|rl|tb|bt) -f (lr|rl|tb|bt) -i equirectangular.jpg -o vr180.jpg -v 180x180");

            Console.WriteLine("    -f (lr|rl|tb|bt) describes the equi-rectangular image format");
            Console.WriteLine("                     lr is left-right, rl is right-left");
            Console.WriteLine("                     tb is top-bottm, bt is bottom-top");
            Console.WriteLine("                     where the first location describes where the left eye image is located");
            Console.WriteLine("");
            Console.WriteLine("    -i the input file path, a 3D equi-rectangular JPEG image in the format described in the -f paramters");
            Console.WriteLine("");
            Console.WriteLine("    -o the output file path");
            Console.WriteLine("");
            Console.WriteLine("    -v Optional parameter decribing the field of view of the equi-rectangular image");
            Console.WriteLine("       the value should be in the format of horizonal degrees and vertical degees seperated by x e.g. 180x120 ");
            Console.WriteLine("       if the parameter is not used then the value 180x180 is used by default.");
            return;
        }
    }


}
