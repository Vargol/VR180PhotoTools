using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.Security.Cryptography;


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


            using (var stream = File.OpenRead(inputJpeg))
                jpegSegments = jpegFile.Parse(stream);

            bool xmpFound = false;
            bool extendedXMPFound = false;


            foreach (var segment in jpegSegments)
            {

                Console.WriteLine(segment.Item1);

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
            Console.WriteLine(sb.ToString());

            if (extendXMP.Length > 0)
            {

                var newJpeg = jpegFile.ProcessExtendedXMPXML(extendXMP);
                if (newJpeg != null)
                {
                    var returnCode = jpegFile.WriteCombineImage(inputJpeg, outputJpeg, newJpeg);
                }
            }

        }

        private static void Usage()
        {

            Console.WriteLine("Usage: vr180ToEquiPhoto vr180Photo.jpg equiPhoto.jpg");
            Console.WriteLine("Mono Usage: mono vr180ToEquiPhoto vr180Photo.jpg equiPhoto.jpg");
            return;

        }





    }


    class JpegFile
    {
        public LinkedList<Tuple<string, byte[]>> Parse(FileStream stream)
        {

            ushort jpegHeader = 0xFFD8;
            ushort jfifHeader = 0xFFE0;
            ushort exifHeader = 0xFFE1;

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
            if (BitConverter.IsLittleEndian)
                Array.Reverse(buffer);

            bufferValue = BitConverter.ToUInt16(buffer, 0);

            if (bufferValue == exifHeader)
            {
                Console.WriteLine("Might be a Exif");
                var exif = this.ValidateExif(stream);
                if (exif != null)
                {
                    segments.AddLast(exif);
                }
            }
            else if (bufferValue == jfifHeader)
            {
                Console.WriteLine("Might be a Jiff");
                var jfif = this.ValidateJfif(stream);
                if (jfif != null)
                {
                    segments.AddLast(jfif);
                }

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
                segments.AddLast(new Tuple<string, byte[]>("APP" + (buffer[1] & 0x0F), segment));

                index = stream.Read(buffer, 0, 2);
                lengthBuffer = new byte[2];
            }

            return segments;

        }

        public Dictionary<string, string> ExtractGPano(string xmpString)
        {

            Dictionary<string, string> panoDict = new Dictionary<string, string>();
            XmlDocument xmp = new System.Xml.XmlDocument();
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
                        Console.WriteLine(attribute.Name + "=" + attribute.Value);
                        panoDict.Add(attribute.Name, attribute.Value);
                    }
                }
            }

            return panoDict;


        }

        private Tuple<string, byte[]> ValidateExif(FileStream stream)
        {

            byte[] lengthBuffer = new byte[2];

            int readCount = stream.Read(lengthBuffer, 0, 2);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(lengthBuffer);
            ushort segmentLength = BitConverter.ToUInt16(lengthBuffer, 0);

            byte[] segment = new byte[segmentLength];
            readCount = stream.Read(segment, 0, segmentLength - 2);

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
                return null;
            }

            return new Tuple<string, byte[]>("EXIF", segment);
        }


        private Tuple<string, byte[]> ValidateJfif(FileStream stream)
        {

            byte[] lengthBuffer = new byte[2];

            int readCount = stream.Read(lengthBuffer, 0, 2);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(lengthBuffer);

            ushort segmentLength = BitConverter.ToUInt16(lengthBuffer, 0);

            byte[] segment = new byte[segmentLength];
            readCount = stream.Read(segment, 0, segmentLength - 2);

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
                return null;
            }

            return new Tuple<string, byte[]>("JFIF", segment);
        }

        internal bool segmentCompare(byte[] segment1, byte[] segment2)
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

        internal string ProcessExtendedXMPSegemnt(byte[] item2, int xmpLengthSoFar)
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

        internal byte[] ProcessExtendedXMPXML(string extendXMP)
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
                        FileStream jpegFile = new FileStream("newjpeg.jpg", FileMode.OpenOrCreate, FileAccess.Write);
                        jpegFile.Write(newImage, 0, newImage.Length);
                        return newImage;
                    }
                }
            }

            return null;

        }

        internal string WriteCombineImage(string inputJpeg, string outputJpeg, byte[] newJpeg)
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

            using (EncoderParameters encoderParameters = new EncoderParameters(1))
            using (EncoderParameter encoderParameter = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 90L))
            {
                ImageCodecInfo codecInfo = ImageCodecInfo.GetImageDecoders().First(codec => codec.FormatID == ImageFormat.Jpeg.Guid);
                encoderParameters.Param[0] = encoderParameter;
                newBitmap.Save(outputJpeg, codecInfo, encoderParameters);
            }

            newBitmap.Dispose();


            return "OK";
        }
    }
}
