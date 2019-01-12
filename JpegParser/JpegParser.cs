using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using ExifLibrary;

namespace JpegParser
{

    public class Vr180Jpeg
    {

        public Vr180Jpeg (Bitmap left, Bitmap right, ExifReadWrite exifSegment)
        {
            GetLeftEye = left;
            GetRightEye = right;
            ExifSegment = exifSegment;

        }


        public Vr180Jpeg()
        {

        }

        public Bitmap GetLeftEye { get; private set; }

        public Bitmap GetRightEye { get; private set; }

        public ExifReadWrite ExifSegment { get; private set; }

    }

    public class JpegFile
    {
        public JpegFile()
        {
        }

        public string GetXmpMetadata(int width, int height, float widthDegrees, float heightDegrees, byte[] digest)
        {

            float widthCropFactor = 360.0f / widthDegrees;
            float heightCropFactor = 180.0f / heightDegrees;

            int fullHeight = (int)(heightCropFactor * height);
            int fullWidth = (int)(widthCropFactor * width);

            int cropLeft = (fullWidth - width) / 2;
            int cropTop = (fullHeight - height) / 2;

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
                + " GPano:CroppedAreaTopPixels=\"" + cropTop + "\""
                + " GPano:CroppedAreaImageWidthPixels=\"" + width + "\""
                + " GPano:CroppedAreaImageHeightPixels=\"" + height + "\""
                + " GPano:FullPanoWidthPixels=\"" + fullWidth + "\""
                + " GPano:FullPanoHeightPixels=\"" + fullHeight + "\""
                + " GPano:InitialViewHeadingDegrees=\"180\""
                + " GPano:ProjectionType=\"equirectangular\""
                //               + " GAudio:Mime=\"audio/mp4\""
                + " GImage:Mime=\"image/jpeg\""
                + " xmpNote:HasExtendedXMP=\"" + sb.ToString() + "\"/>"
                + " </rdf:RDF> </x:xmpmeta> ";

        }

        public Vr180Jpeg SplitImage(string orginalJpeg, string format)
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

            originalImage.Dispose();

            var exiflib = new ExifReadWrite();

            using (FileStream fs = new FileStream(orginalJpeg,FileMode.Open, FileAccess.Read))
            {

                var segments = Parse(fs);
                foreach (var segment in segments)
                {
                    if (segment.Item1 == "EXIF")
                    {
                        exiflib.ReadExifAPP1(segment.Item2);
                    }


                }

            }


            return new Vr180Jpeg(firstHalf, secondHalf, exiflib);

        }

        public void WriteVr180Jpeg(Vr180Jpeg jpegs, string xmpMetadata, byte[] extendedMd5Hash, string extendedXmp, string outJpeg)
        {
            byte[] XmpUri = Encoding.UTF8.GetBytes("http://ns.adobe.com/xap/1.0/");
            byte[] extendedXmpUri = Encoding.UTF8.GetBytes("http://ns.adobe.com/xmp/extension/");
            byte[] zeroByte = { 0x0 };
            byte[] app1 = { 0xFF, 0xE1 };
            byte[] xmpSignature = XmpUri.Concat(zeroByte).ToArray();
            byte[] extendedXmpSignature = extendedXmpUri.Concat(zeroByte).ToArray();

            int width = jpegs.GetLeftEye.Width;
            int height = jpegs.GetLeftEye.Height;


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
                jpegs.GetLeftEye.Save(jpegms, codecInfo, encoderParameters);
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

                            // skip an exif if we are copying the originals
                            if (segment[0] == 'E' &&
                                segment[1] == 'x' &&
                                segment[2] == 'i' &&
                                segment[3] == 'f' &&
                                segment[4] == 0x00 && jpegs.ExifSegment.Properties.Count > 0)
                            {
                                Console.WriteLine("New Jpeg has an existing Exif, replacing.");
                                continue;
                            }


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



                    // add in an Exif if we've got properties...
                    if (jpegs.ExifSegment.Properties.Count > 0)
                    {
                        // replace height and width

                        foreach (var prop in jpegs.ExifSegment.Properties)
                        {
                            Console.WriteLine(prop.Name);
                            if (prop.Name == "PixelXDimension")
                            {
                                prop.Value = (uint)jpegs.GetLeftEye.Width;
                            }

                            if (prop.Name == "PixelYDimension")
                            {
                                prop.Value = (uint)jpegs.GetLeftEye.Height;
                            }


                        }

                        byte[] exif = jpegs.ExifSegment.WriteExifApp1(true);
                        byte[] exifTagBuffer = { 0xFF, 0xE1 };

                        ushort exifLength = (ushort)(exif.Length + 2);

                        networkLength = BitConverter.GetBytes(exifLength);
                        if (BitConverter.IsLittleEndian)
                            Array.Reverse(networkLength);

                        jpegOut.Write(exifTagBuffer, 0, 2);
                        jpegOut.Write(networkLength, 0, 2);
                        jpegOut.Write(exif, 0, exif.Length);

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

                    // write out the remaining bit of the extended xmp
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


        public LinkedList<Tuple<string, byte[]>> Parse(FileStream stream)
        {

            ushort jpegHeader = 0xFFD8;
            ushort jfifHeader = 0xE0;
            ushort exifHeader = 0xE1;

            ushort segmentLength;


            LinkedList<Tuple<String, byte[]>> segments = new LinkedList<Tuple<string, byte[]>>();

            byte[] buffer = new byte[2];

            int index = stream.Read(buffer, 0, 2);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(buffer);

            ushort bufferValue = BitConverter.ToUInt16(buffer, 0);

            if (bufferValue != jpegHeader)
            {
                Console.WriteLine("Not a Jpeg");
                return null;
            }

            index = stream.Read(buffer, 0, 2);
            byte[] lengthBuffer = new byte[2];

            while (buffer[0] == 0xFF && (buffer[1] & 0xE0) == 0xE0)
            {


                int readCount = stream.Read(lengthBuffer, 0, 2);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(lengthBuffer);
                segmentLength = BitConverter.ToUInt16(lengthBuffer, 0);

                byte[] segment = new byte[segmentLength - 2];
                readCount = stream.Read(segment, 0, segmentLength - 2);

                if (buffer[1] == exifHeader)
                {
                    //Console.WriteLine("Might be a Exif");
                    var exif = this.ValidateExif(segment);
                    if (exif != null)
                    {
                        segments.AddLast(exif);
                    }
                }
                else if (buffer[1] == jfifHeader)
                {
                    //Console.WriteLine("Might be a Jiff");
                    var jfif = this.ValidateJfif(segment);
                    if (jfif != null)
                    {
                        segments.AddLast(jfif);
                    }

                }
                else
                {
                    segments.AddLast(new Tuple<string, byte[]>("APP" + (buffer[1] & 0x0F), segment));
                }


                index = stream.Read(buffer, 0, 2);
                lengthBuffer = new byte[2];
            }

            return segments;

        }

        public Dictionary<string, string> ExtractGPano(string xmpString)
        {

            Dictionary<string, string> panoDict = new Dictionary<string, string>();
            XmlDocument xmp = new XmlDocument();
            xmp.LoadXml(xmpString);
            var nsmgr = new XmlNamespaceManager(xmp.NameTable);
            nsmgr.AddNamespace("rdf", "http://www.w3.org/1999/02/22-rdf-syntax-ns#");
            var xmlNodes = xmp.SelectNodes("//rdf:Description ", nsmgr);
            foreach (XmlNode node in xmlNodes)
            {
                foreach (XmlAttribute attribute in node.Attributes)
                {
                    if (attribute.Name.StartsWith("GPano:", StringComparison.InvariantCulture) || attribute.Name.Equals("xmpNote:HasExtendedXMP"))
                    {
                        // Console.WriteLine(attribute.Name + "=" + attribute.Value);
                        panoDict.Add(attribute.Name, attribute.Value);
                    }
                }
            }

            return panoDict;


        }

        private Tuple<string, byte[]> ValidateExif(byte[] segment)
        { 

            if (segment[0] == 'E' &&
                segment[1] == 'x' &&
                segment[2] == 'i' &&
                segment[3] == 'f' &&
                segment[4] == 0x00)
            {
                Console.WriteLine("it's an a Exif");
            }
            else
            {
                return new Tuple<string, byte[]>("APP1", segment);
            }

            return new Tuple<string, byte[]>("EXIF", segment);
        }


        private Tuple<string, byte[]> ValidateJfif(byte[] segment)
        {



            if (segment[0] == 'J' &&
                segment[1] == 'F' &&
                segment[2] == 'I' &&
                segment[3] == 'F' &&
                segment[4] == 0x00)
            {
                Console.WriteLine("it's an a JFIF");
            }
            else
            {
                return new Tuple<string, byte[]>("APP0", segment);
            }

            return new Tuple<string, byte[]>("JFIF", segment);
        }

        public bool segmentCompare(byte[] segment1, byte[] segment2)
        {

            for (int i = 0; i < segment1.Length; i++)
            {
                if (segment1[i] != segment2[i])
                {
                    return false;
                }
            }
            return true;

        }

        public string ProcessExtendedXMPSegemnt(byte[] item2, int xmpLengthSoFar)
        {

            byte[] lengthBuffer = new byte[4];
            lengthBuffer[0] = item2[35 + 32];
            lengthBuffer[1] = item2[35 + 33];
            lengthBuffer[2] = item2[35 + 34];
            lengthBuffer[3] = item2[35 + 35];

            if (BitConverter.IsLittleEndian)
                Array.Reverse(lengthBuffer);

            uint fullLength = BitConverter.ToUInt32(lengthBuffer, 0);

            // do a bit of invalidation

            if (xmpLengthSoFar >= fullLength)
            {
                throw new Exception("Extended XMP is longer than expected length.");
            }

            lengthBuffer[0] = item2[35 + 36];
            lengthBuffer[1] = item2[35 + 37];
            lengthBuffer[2] = item2[35 + 38];
            lengthBuffer[3] = item2[35 + 39];

            if (BitConverter.IsLittleEndian)
                Array.Reverse(lengthBuffer);

            uint offset = BitConverter.ToUInt32(lengthBuffer, 0);

            if (offset != xmpLengthSoFar)
            {
                throw new Exception("Extended XMP offset greated that current partial length.");
            }

            return Encoding.UTF8.GetString(item2, 35 + 40, item2.Length - 75);
        }

        public byte[] ProcessExtendedXMPXML(string extendXMP)
        {


            XmlDocument xmp = new System.Xml.XmlDocument();
            xmp.LoadXml(extendXMP);
            var nsmgr = new XmlNamespaceManager(xmp.NameTable);
            nsmgr.AddNamespace("rdf", "http://www.w3.org/1999/02/22-rdf-syntax-ns#");
            var xmlNodes = xmp.SelectNodes("//rdf:Description ", nsmgr);
            foreach (XmlNode node in xmlNodes)
            {
                foreach (XmlAttribute attribute in node.Attributes)
                {
                    if (attribute.Name.Equals("GImage:Data"))
                    {
                        Console.WriteLine("Found GImage");


                        var newImage = Convert.FromBase64String(attribute.Value + "==".Substring(0, attribute.Value.Length % 4));
                        //FileStream jpegFile = new FileStream("newjpeg.jpg", FileMode.OpenOrCreate, FileAccess.Write);
                        //jpegFile.Write(newImage, 0, newImage.Length);
                        return newImage;
                    }
                }
            }

            return null;

        }

        public string WriteCombineImage(string inputJpeg, string outputJpeg, byte[] newJpeg, ExifReadWrite exif)
        {


            Image img1 = Image.FromFile(inputJpeg);
            Image img2 = Image.FromStream(new MemoryStream(newJpeg));
            Bitmap newBitmap;
            Graphics canvas;
            int height, width;

            int EXIF_HEIGHT_ID = 0x0100;
            int EXIF_WIDTH_ID = 0x0101;

            if (img1.Width > img1.Height)
            {
                // top bottom
                height = img1.Height + img2.Height;
                width = Math.Max(img1.Width, img2.Width);

                newBitmap = new Bitmap(width, height);
                canvas = Graphics.FromImage(newBitmap);
                canvas.Clear(Color.Black);
                canvas.DrawImage(img1, new Point(0, 0));
                canvas.DrawImage(img2, new Point(0, img1.Height));

            }
            else
            {
                width = img1.Width + img2.Width;
                height = Math.Max(img1.Height, img2.Height);

                newBitmap = new Bitmap(width, height);
                canvas = Graphics.FromImage(newBitmap);

                canvas.Clear(Color.Black);
                canvas.DrawImage(img1, new Point(0, 0));
                canvas.DrawImage(img2, new Point(img1.Width, 0));

            }


            foreach (PropertyItem property in img1.PropertyItems)
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

                newBitmap.SetPropertyItem(property);
            }


            canvas.Dispose();
            img1.Dispose();
            img2.Dispose();

            using (MemoryStream jpegms = new MemoryStream())
            using (FileStream jpegOut = new FileStream(outputJpeg, FileMode.OpenOrCreate, FileAccess.Write))
            {


                using (EncoderParameters encoderParameters = new EncoderParameters(1))
                using (EncoderParameter encoderParameter = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 90L))
                {
                    ImageCodecInfo codecInfo = ImageCodecInfo.GetImageDecoders().First(codec => codec.FormatID == ImageFormat.Jpeg.Guid);
                    encoderParameters.Param[0] = encoderParameter;
                    newBitmap.Save(jpegms, codecInfo, encoderParameters);
                }

                height = newBitmap.Height;
                width = newBitmap.Width;

                newBitmap.Dispose();
                jpegms.Flush();
                jpegms.Position = 0;

                byte[] jpegTagBuffer = new byte[2];
                byte[] jpegLengthBuffer = new byte[2];
                byte[] networkLength = new byte[2];

                jpegms.Read(jpegTagBuffer, 0, 2);
                jpegOut.Write(jpegTagBuffer, 0, 2);

                bool writeExifRead = false;

                while (writeExifRead == false)
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

                        // skip an exif if we are copying the originals
                        if (segment[0] == 'E' &&
                            segment[1] == 'x' &&
                            segment[2] == 'i' &&
                            segment[3] == 'f' &&
                            segment[4] == 0x00 && exif.Properties.Count > 0)
                        {
                            Console.WriteLine("New Jpeg has an existing Exif, replacing.");
                            continue;
                        }


                        // write it out
                        jpegOut.Write(jpegTagBuffer, 0, 2);
                        jpegOut.Write(jpegLengthBuffer, 0, 2);
                        jpegOut.Write(segment, 0, length - 2);

                    }
                    else
                    {
                        writeExifRead = true;
                    }

                }



                // add in an Exif if we've got properties...
                if (exif.Properties.Count > 0)
                {
                    // replace height and width

                    foreach (var prop in exif.Properties)
                    {
                        // Console.WriteLine(prop.Name);
                        if (prop.Name == "PixelXDimension")
                        {
                            prop.Value = (uint)width;
                        }

                        if (prop.Name == "PixelYDimension")
                        {
                            prop.Value = (uint)height;
                        }


                    }

                    byte[] exifBuffer = exif.WriteExifApp1(true);
                    byte[] exifTagBuffer = { 0xFF, 0xE1 };

                    ushort exifLength = (ushort)(exifBuffer.Length + 2);

                    networkLength = BitConverter.GetBytes(exifLength);
                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(networkLength);

                    jpegOut.Write(exifTagBuffer, 0, 2);
                    jpegOut.Write(networkLength, 0, 2);
                    jpegOut.Write(exifBuffer, 0, exifBuffer.Length);

                }

                // finish of writing the jpeg, starting with the last tag read.
                jpegOut.Write(jpegTagBuffer, 0, 2);
                jpegms.CopyTo(jpegOut);

                jpegms.Flush();
                jpegOut.Flush();

            }

            //


            return "OK";
        }
    }
}
