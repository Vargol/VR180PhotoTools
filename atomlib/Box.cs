using System;
using System.Runtime.InteropServices;
using System.IO;
using System.Linq;

namespace atomlib
{
    public class Box
    {
        public byte[] name { get; protected set; } 

        public long length { get; set; } 

        public virtual void ReadBoxHeader(BinaryReader movie )
        {
            length = ReadBEInt32(movie);

            if (length == 1)
            {
                name = movie.ReadBytes(4);
                length = ReadBEInt64(movie);
                movie.BaseStream.Seek(-16, SeekOrigin.Current);
                return;
            }
            if ( length == 0 ) {
                length = movie.BaseStream.Length;
            }

            name = movie.ReadBytes(4);
            movie.BaseStream.Seek(-8, SeekOrigin.Current);
        }

        public virtual void CopyBox(BinaryReader movieIn, BinaryWriter movieOut)
        {
            
            long readSoFar = 0L;
            var buffer = new byte[64 * 1024];

            do
            {
                var toRead = Math.Min(length - readSoFar, buffer.Length);
                var readNow = movieIn.BaseStream.Read(buffer, 0, (int)toRead);

                if (readNow == 0)
                    break; 
                
                movieOut.BaseStream.Write(buffer, 0, readNow);

                readSoFar += readNow;

            } while (readSoFar < length);

        }


        protected int ReadBEInt32 (BinaryReader movie) {

            return BitConverter.ToInt32(movie.ReadBytes(4).Reverse().ToArray(), 0);

        }

        protected long ReadBEInt64(BinaryReader movie)
        {

            return BitConverter.ToInt64(movie.ReadBytes(8).Reverse().ToArray(), 0);

        }

        public virtual bool CompareName(byte[] inName) {

            if (name[0] == inName[0] &&
                name[1] == inName[1] &&
                name[2] == inName[2] &&
                name[3] == inName[3]   ) {

                return true;
                
            }

            return false;

        }


    }
}
