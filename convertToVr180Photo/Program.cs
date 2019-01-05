using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using ExifLibrary;
using JpegParser;


namespace convertToVr180Photo
{
    class MainClass
    {
        public static void Main(string[] args)
        {

            if (args.Length != 3)
            {
                Usage();
                return;
            }

            Console.WriteLine("Starting");

            string base64jpeg;

            string equiJpeg = args[1];
            string outJpeg = args[2];
            var jpegFile = new JpegParser.JpegFile();
            var jpegs = jpegFile.SplitImage(equiJpeg, args[0]);

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

            string xmpMetadata = jpegFile.GetXmpMetadata(jpegs.GetRightEye.Width, jpegs.GetRightEye.Height, 180.0f, extendedMd5Hash);


            // insert the xmp in the jpeg..
            jpegFile.WriteVr180Jpeg(jpegs, xmpMetadata, extendedMd5Hash, extendedXmpXml, outJpeg);

            Console.WriteLine("Finished");

        }

        private static void Usage ()
        {
            Console.WriteLine("Usage: equiToVr180Photo (lr|rl|tb|bt) equirectangular.jpg vr180.jpg");
            Console.WriteLine("Mono Usage: mono (lr|rl|tb|bt) equiToVr180Photo.exe equirectangular.jpg vr180.jpg");

            Console.WriteLine("    (lr|rl|tb|bt) describes the equi-rectangular image format");
            Console.WriteLine("    lr is left-right, rl is right-left");
            Console.WriteLine("    tb is top-bottm, bt is bottom-top");
            Console.WriteLine("  where the first location describes where the left eye image is located");
            return;
        }
    }


}
