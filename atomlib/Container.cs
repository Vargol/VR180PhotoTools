using System;
using System.IO;

namespace atomlib
{
    public class Container : Box
    {

        public int headerLength { get; private set; } 
        public long filePosition { get; private set; }
        public Action<BinaryReader, BinaryWriter> writeBox { get; private set; }


        Container[] children; 

        public Container()
        {
        }

        public override void ReadBoxHeader(BinaryReader movie)
        {

            filePosition = movie.BaseStream.Position;

            length = ReadBEInt32(movie);

            if (length == 1)
            {
                name = movie.ReadBytes(4);
                length = ReadBEInt64(movie);
                headerLength = 16;
                return;
            }
            if (length == 0)
            {
                length = movie.BaseStream.Length;
            }

            name = movie.ReadBytes(4);
            headerLength = 8;
        }

        public override void CopyBox(BinaryReader movieIn, BinaryWriter movieOut)
        {

            movieIn.BaseStream.Seek(-headerLength, SeekOrigin.Current);

            base.CopyBox(movieIn, movieOut);

        }

        public void writeCammBox(BinaryReader movieIn, BinaryWriter movieOut, bool zeroX, bool zeroY, bool zeroZ)
        {

            movieIn.BaseStream.Seek(-headerLength, SeekOrigin.Current);
            base.CopyBox(movieIn, movieOut);

        }


    }
}
