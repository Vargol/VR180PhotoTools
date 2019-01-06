using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.Security.Cryptography;

using ExifLibrary;
using JpegParser;


namespace convertVr180Photos
{
    class VR180
    {
        public static void Main(string[] args)
        {


            if (args.Length != 2)
            {
                Usage();
                return;
            }

            LinkedList<Tuple<string, byte[]>> jpegSegments;
            JpegFile jpegFile = new JpegFile();
            Dictionary<string, string> panoDict;
            byte[] extendedXMPURI = Encoding.UTF8.GetBytes("http://ns.adobe.com/xmp/extension/");
            byte[] zeroByte = { 0x0 };
            byte[] extendedXMPSignature = null;
            string extendXMP = "";
            string inputJpeg = args[0];
            string outputJpeg = args[1];
            ExifReadWrite exif = new ExifReadWrite();


            using (var stream = File.OpenRead(inputJpeg))
                jpegSegments = jpegFile.Parse(stream);

            bool xmpFound = false;
            bool extendedXMPFound = false;


            foreach (var segment in jpegSegments)
            {

                //Console.WriteLine(segment.Item1);

                if (segment.Item1 == "EXIF")
                {
                    exif.ReadExifAPP1(segment.Item2);
                }

                if (xmpFound != true && segment.Item1 == "APP1")
                {
                    string start = Encoding.UTF8.GetString(segment.Item2, 0, 28);
                    if (start == "http://ns.adobe.com/xap/1.0/")
                    {
                        // XMP, extract the GPano if its there.
                        panoDict = jpegFile.ExtractGPano(Encoding.UTF8.GetString(segment.Item2, 29, segment.Item2.Length - 29));
                        string xmpMD5;
                        if (panoDict.TryGetValue("xmpNote:HasExtendedXMP", out xmpMD5))
                        {
                            extendedXMPSignature = extendedXMPURI.Concat(zeroByte).Concat(Encoding.UTF8.GetBytes(xmpMD5)).ToArray();
                            extendedXMPFound = true;
                        }
                    }

                }

                if (extendedXMPFound == true && segment.Item1 == "APP1" && jpegFile.segmentCompare(extendedXMPSignature, segment.Item2))
                {
                    extendXMP += jpegFile.ProcessExtendedXMPSegemnt(segment.Item2, extendXMP.Length);
                }

            }

            MD5 md5 = System.Security.Cryptography.MD5.Create();
            var md5hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(extendXMP));

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < md5hash.Length; i++)
            {
                sb.Append(md5hash[i].ToString("x2"));
            }

            //            Console.WriteLine(sb.ToString());

            if (extendXMP.Length > 0)
            {

                var newJpeg = jpegFile.ProcessExtendedXMPXML(extendXMP);
                if (newJpeg != null)
                {
                    var returnCode = jpegFile.WriteCombineImage(inputJpeg, outputJpeg, newJpeg, exif);
                }
            }

        }

        private static void Usage()
        {

            Console.WriteLine("Usage: vr180ToEquiPhoto.exe vr180Photo.jpg equiPhoto.jpg");
            Console.WriteLine("Mono Usage: mono vr180ToEquiPhoto.exe vr180Photo.jpg equiPhoto.jpg");
            return;

        }





    }


}
