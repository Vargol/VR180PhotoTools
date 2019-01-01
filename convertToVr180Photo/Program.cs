using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;


namespace convertToVr180Photo
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            String base64jpeg;

            if (args.Length != 2)
            {
                Console.WriteLine("Usage: equiToVr180Photo (lr|rl|tb|bt) equirectangular.jpg vr180.jpg");
                Console.WriteLine("Mono Usage: mono (lr|rl|tb|bt) equiToVr180Photo.exe equirectangular.jpg vr180.jpg");

                Console.WriteLine("Mono Usage: mono (lr|rl|tb|bt) convertToVr180Photo.exe equirectangular.jpg vr180.jpg");
                Console.WriteLine("    (lr|rl|tb|bt) describes the equi-rectangular image format");
                Console.WriteLine("    lr is left-right, rl is right-left");
                Console.WriteLine("    tb is top-bottm, bt is bottom-top");
                Console.WriteLine("  where the first location describes where the left eye image is located");
                return;
            }

            string equiJpeg = args[1];
            string outJpeg = args[2];
            JpegFile jpegFile = new JpegFile();
            var jpegs = jpegFile.SplitImage(equiJpeg, args[0]);

            // the right image gets embed in the left image.
            using (EncoderParameters encoderParameters = new EncoderParameters(1))
            using (EncoderParameter encoderParameter = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 90L))
            using (MemoryStream jpegms = new MemoryStream())
            {
                ImageCodecInfo codecInfo = ImageCodecInfo.GetImageDecoders().First(codec => codec.FormatID == ImageFormat.Jpeg.Guid);
                encoderParameters.Param[0] = encoderParameter;
                jpegs.Item2.Save(jpegms, codecInfo, encoderParameters);
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

            string xmpMetadata = JpegFile.GetXmpMetadata(jpegs.Item2.Width, jpegs.Item2.Height, 180.0f, extendedMd5Hash);


            // insert the xmp in the jpeg..
            jpegFile.WriteNewJpeg(jpegs.Item1, xmpMetadata, extendedMd5Hash, extendedXmpXml, outJpeg);



        }
    }


    class JpegFile
    {
        internal static string GetXmpMetadata(int width, int height, float degrees, byte[] digest)
        {

            float cropFactor = 360.0f / degrees;

            int fullHeight = (int)(cropFactor * height);
            int fullWidth = (int)(cropFactor * width);

            int cropLeft = (fullWidth - width) / 2;

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < digest.Length; i++)
            {
                sb.Append(digest[i].ToString("x2"));
            }


            return
            "\n<x:xmpmeta xmlns:x=\"adobe:ns:meta/\" x:xmptk=\"Adobe XMP\">"
            + " <rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\">"
            + " <rdf:Description "
                + " xmlns:GPano=\"http://ns.google.com/photos/1.0/panorama/\""
                + " xmlns:GImage=\"http://ns.google.com/photos/1.0/image/\""
                //               + " xmlns:GAudio=\"http://ns.google.com/photos/1.0/audio/\""
                + " xmlns:xmpNote=\"http://ns.adobe.com/xmp/note/\""
                + " rdf:about=\"\""
                + " GPano:PoseRollDegrees=\"0.0\""
                + " GPano:PosePitchDegrees=\"0.0\""
                + " GPano:CroppedAreaLeftPixels=\"" + cropLeft + "\""
                + " GPano:CroppedAreaTopPixels=\"0\""
                + " GPano:CroppedAreaImageWidthPixels=\"" + width + "\""
                + " GPano:CroppedAreaImageHeightPixels=\"" + height + "\""
                + " GPano:FullPanoWidthPixels=\"" + fullWidth + "\""
                + " GPano:FullPanoHeightPixels=\"" + height + "\""
                + " GPano:InitialViewHeadingDegrees=\"180\""
                + " GPano:ProjectionType=\"equirectangular\""
                //               + " GAudio:Mime=\"audio/mp4\""
                + " GImage:Mime=\"image/jpeg\""
                + " xmpNote:HasExtendedXMP=\"" + sb.ToString() + "\"/>"
                + " </rdf:RDF> </x:xmpmeta> ";

        }

        public Tuple<Bitmap, Bitmap> SplitImage(string orginalJpeg, string format)
        {
            Bitmap firstHalf, secondHalf;
            Bitmap originalImage = new Bitmap(Image.FromFile(orginalJpeg));
            Rectangle rect;

            switch (format)
            {
                case "lr":

                    rect = new Rectangle(0, 0, originalImage.Width / 2, originalImage.Height);
                    firstHalf = originalImage.Clone(rect, originalImage.PixelFormat);

                    rect = new Rectangle(originalImage.Width / 2, 0, originalImage.Width / 2, originalImage.Height);
                    secondHalf = originalImage.Clone(rect, originalImage.PixelFormat);

                    break;

                case "rl":

                    rect = new Rectangle(originalImage.Width / 2, 0, originalImage.Width / 2, originalImage.Height);
                    firstHalf = originalImage.Clone(rect, originalImage.PixelFormat);

                    rect = new Rectangle(0, 0, originalImage.Width / 2, originalImage.Height);
                    secondHalf = originalImage.Clone(rect, originalImage.PixelFormat);

                    break;

                case "tb":

                    rect = new Rectangle(0, 0, originalImage.Width, originalImage.Height / 2);
                    firstHalf = originalImage.Clone(rect, originalImage.PixelFormat);

                    rect = new Rectangle(0, originalImage.Height / 2, originalImage.Width, originalImage.Height / 2);
                    secondHalf = originalImage.Clone(rect, originalImage.PixelFormat);

                    break;

                case "bt":

                    rect = new Rectangle(0, originalImage.Height / 2, originalImage.Width, originalImage.Height / 2);
                    firstHalf = originalImage.Clone(rect, originalImage.PixelFormat);

                    rect = new Rectangle(0, 0, originalImage.Width, originalImage.Height / 2);
                    secondHalf = originalImage.Clone(rect, originalImage.PixelFormat);

                    break;

                default:

                    firstHalf = null;
                    secondHalf = null;
                    break;

            }

            int height = firstHalf.Height;
            int width = firstHalf.Width;

            int EXIF_HEIGHT_ID = 0x0100;
            int EXIF_WIDTH_ID = 0x0101;

            if (firstHalf != null)
            {

                foreach (PropertyItem property in originalImage.PropertyItems)
                {
                    if (property.Id == EXIF_HEIGHT_ID)
                    {
                        byte[] newHeight = BitConverter.GetBytes(height);
                        property.Value = newHeight;
                    }
                    else if (property.Id == EXIF_WIDTH_ID)
                    {
                        byte[] newWidth = BitConverter.GetBytes(width);
                        property.Value = newWidth;
                    }

                    firstHalf.SetPropertyItem(property);
                }
            }

            return new Tuple<Bitmap, Bitmap>(firstHalf, secondHalf);

        }

        internal void WriteNewJpeg(Bitmap leftJpeg, string xmpMetadata, byte[] extendedMd5Hash, string extendedXmp, string outJpeg)
        {
            byte[] XmpUri = Encoding.UTF8.GetBytes("http://ns.adobe.com/xap/1.0/");
            byte[] extendedXmpUri = Encoding.UTF8.GetBytes("http://ns.adobe.com/xmp/extension/");
            byte[] zeroByte = { 0x0 };
            byte[] app1 = { 0xFF, 0xE1 };
            byte[] xmpSignature = XmpUri.Concat(zeroByte).ToArray();
            byte[] extendedXmpSignature = extendedXmpUri.Concat(zeroByte).ToArray();

            int width = leftJpeg.Width;
            int height = leftJpeg.Height;


            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < extendedMd5Hash.Length; i++)
            {
                sb.Append(extendedMd5Hash[i].ToString("x2"));
            }

            byte[] md5bytes = Encoding.UTF8.GetBytes(sb.ToString());


            // the right image gets embed in the left image.
            using (EncoderParameters encoderParameters = new EncoderParameters(1))
            using (EncoderParameter encoderParameter = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 90L))
            using (MemoryStream jpegms = new MemoryStream())
            {
                ImageCodecInfo codecInfo = ImageCodecInfo.GetImageDecoders().First(codec => codec.FormatID == ImageFormat.Jpeg.Guid);
                encoderParameters.Param[0] = encoderParameter;
                leftJpeg.Save(jpegms, codecInfo, encoderParameters);
                jpegms.Position = 0;




                using (FileStream jpegOut = new FileStream(outJpeg, FileMode.Create, FileAccess.Write))
                {
                    byte[] jpegTagBuffer = new byte[2];
                    byte[] jpegLengthBuffer = new byte[2];
                    byte[] networkLength = new byte[2];

                    jpegms.Read(jpegTagBuffer, 0, 2);
                    jpegOut.Write(jpegTagBuffer, 0, 2);

                    bool writeXmpHere = false;

                    while (writeXmpHere == false)
                    {
                        jpegms.Read(jpegTagBuffer, 0, 2);

                        if (jpegTagBuffer[0] == 0xFF && (jpegTagBuffer[1] == 0xE1 || jpegTagBuffer[1] == 0xE0))
                        {

                            jpegms.Read(jpegLengthBuffer, 0, 2);
                            if (BitConverter.IsLittleEndian)
                            {
                                networkLength[0] = jpegLengthBuffer[1];
                                networkLength[1] = jpegLengthBuffer[0];
                            }
                            else
                            {
                                networkLength[0] = jpegLengthBuffer[0];
                                networkLength[1] = jpegLengthBuffer[1];
                            }

                            ushort length = BitConverter.ToUInt16(networkLength, 0);
                            byte[] segment = new byte[length - 2];

                            jpegms.Read(segment, 0, length - 2);

                            // write it out
                            jpegOut.Write(jpegTagBuffer, 0, 2);
                            jpegOut.Write(jpegLengthBuffer, 0, 2);
                            jpegOut.Write(segment, 0, length - 2);

                        }
                        else
                        {
                            writeXmpHere = true;
                        }

                    }

                    // add in the xmp
                    byte[] xmpBytes = Encoding.UTF8.GetBytes(xmpMetadata);
                    ushort xmpLength = (ushort)(xmpSignature.Length + xmpBytes.Length + 2);

                    networkLength = BitConverter.GetBytes(xmpLength);
                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(networkLength);

                    byte[] xmpTagBuffer = { 0xFF, 0xE1 };

                    jpegOut.Write(xmpTagBuffer, 0, 2);
                    jpegOut.Write(networkLength, 0, 2);
                    jpegOut.Write(xmpSignature, 0, xmpSignature.Length);
                    jpegOut.Write(xmpBytes, 0, xmpBytes.Length);

                    // split up and write the extended xmp segments
                    byte[] extendedXmpBytes = Encoding.UTF8.GetBytes(extendedXmp);
                    int extendedXmpLength = extendedXmpBytes.Length;
                    int offset = 0;

                    byte[] extendedXmpLengthBytes = BitConverter.GetBytes(extendedXmpLength);
                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(extendedXmpLengthBytes);

                    ushort maxSegmentLength = 65460;
                    int xmpBlockLength = maxSegmentLength - 77;
                    byte[] segmentLengthBytes = BitConverter.GetBytes(maxSegmentLength);
                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(segmentLengthBytes);

                    byte[] offsetBytes;

                    for (offset = 0; offset < extendedXmpLength - xmpBlockLength; offset += xmpBlockLength)
                    {
                        jpegOut.Write(xmpTagBuffer, 0, 2);
                        jpegOut.Write(segmentLengthBytes, 0, 2);

                        jpegOut.Write(extendedXmpSignature, 0, extendedXmpSignature.Length);
                        jpegOut.Write(md5bytes, 0, md5bytes.Length);

                        jpegOut.Write(extendedXmpLengthBytes, 0, 4);


                        offsetBytes = BitConverter.GetBytes(offset);
                        if (BitConverter.IsLittleEndian)
                            Array.Reverse(offsetBytes);

                        jpegOut.Write(offsetBytes, 0, 4);

                        jpegOut.Write(extendedXmpBytes, offset, xmpBlockLength);

                    }

                    // write out the remaining
                    ushort lengthLeft = (ushort)(extendedXmpLength - offset);
                    ushort segmentLength = (ushort)(lengthLeft + 77);

                    segmentLengthBytes = BitConverter.GetBytes(segmentLength);
                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(segmentLengthBytes);

                    jpegOut.Write(xmpTagBuffer, 0, 2);
                    jpegOut.Write(segmentLengthBytes, 0, 2);

                    jpegOut.Write(extendedXmpSignature, 0, extendedXmpSignature.Length);
                    jpegOut.Write(md5bytes, 0, md5bytes.Length);

                    jpegOut.Write(extendedXmpLengthBytes, 0, 4);

                    offsetBytes = BitConverter.GetBytes(offset);
                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(offsetBytes);

                    jpegOut.Write(offsetBytes, 0, 4);

                    jpegOut.Write(extendedXmpBytes, offset, lengthLeft);

                    // write out the test of the jpeg
                    jpegOut.Write(jpegTagBuffer, 0, 2);
                    jpegms.CopyTo(jpegOut);

                    jpegms.Flush();
                    jpegOut.Flush();
                }

            }

        }
    }
}
