using System;
using atomlib;
using System.IO;
using System.ComponentModel;
using System.Text;
using System.Diagnostics.Eventing.Reader;

namespace VR180Tools
{
    class Tools
    {
        public static void Main(string[] args)
        {

            var tools = new Tools();

            BinaryReader movieIn = new BinaryReader(
                                    new FileStream(
                    "/Users/davidburnett/Documents/Movies/gridtest/fisheye_still_injected2.mp4", FileMode.Open));

            BinaryWriter movieOut = new BinaryWriter(
                                    new FileStream(
                    "tools.mp4", FileMode.OpenOrCreate, FileAccess.Write));

            Box header = new Box();
            header.ReadBoxHeader(movieIn);
            System.Console.WriteLine(System.Text.Encoding.ASCII.GetString(header.name));
            System.Console.WriteLine(header.length);
            header.CopyBox(movieIn, movieOut);


            byte[] moov = Encoding.ASCII.GetBytes(@"moov"); 

            while (movieIn.BaseStream.Position != movieIn.BaseStream.Length) { 

                Box next = new Box();
                next.ReadBoxHeader(movieIn);
                System.Console.WriteLine(System.Text.Encoding.ASCII.GetString(next.name));
                System.Console.WriteLine(next.length);

                if (next.CompareName(moov))
                {
                    
                    tools.ProcessMoov(movieIn, movieOut);

                }
                else
                {

                    next.CopyBox(movieIn, movieOut);
                }
            }

            movieOut.Close();
            movieIn.Close();
        }

        public void ProcessMoov(BinaryReader movieIn, BinaryWriter movieOut) {
            
            byte[] trak = Encoding.ASCII.GetBytes(@"trak");

            long endPosition = movieIn.BaseStream.Position;

            var moov = new atomlib.Container();
            moov.ReadBoxHeader(movieIn);

            endPosition += moov.length;

            do
            {
                var next = new atomlib.Container();
                next.ReadBoxHeader(movieIn);
                System.Console.WriteLine("PM - " + System.Text.Encoding.ASCII.GetString(next.name));
                if (next.CompareName(trak))
                {
                    long startPosition = movieIn.BaseStream.Position;
                    bool foundCamm = findCammBox(movieIn, movieIn.BaseStream.Position + next.length - next.headerLength);
                    movieIn.BaseStream.Seek(startPosition, SeekOrigin.Begin);
                    if (foundCamm)
                    {
                        // need write this one out bit by bit
                        next.CopyBox(movieIn, movieOut);
                    }
                    else
                    {

                        next.CopyBox(movieIn, movieOut);
                    }

                } else {
                    next.CopyBox(movieIn, movieOut);
                }

            } while (movieIn.BaseStream.Position < endPosition);


        }

        private bool findCammBox(BinaryReader movieIn, long endPosition) {

            byte[] mdia = Encoding.ASCII.GetBytes(@"mdia");
            byte[] minf = Encoding.ASCII.GetBytes(@"minf");
            byte[] stbl = Encoding.ASCII.GetBytes(@"stbl");
            byte[] stsd = Encoding.ASCII.GetBytes(@"stsd");
            byte[] camm = Encoding.ASCII.GetBytes(@"camm");

            do
            {
                var next = new atomlib.Container();
                next.ReadBoxHeader(movieIn);
                System.Console.WriteLine("fCB - " + System.Text.Encoding.ASCII.GetString(next.name));
                if (next.CompareName(mdia))
                {
                    if (findContainer(minf, movieIn, movieIn.BaseStream.Position + next.length - next.headerLength))
                    {
                        if (findContainer(stbl, movieIn, movieIn.BaseStream.Position + next.length - next.headerLength))
                        {
                            if (findContainer(stsd, movieIn, movieIn.BaseStream.Position + next.length - next.headerLength))
                            {
                                movieIn.BaseStream.Seek(8, SeekOrigin.Current);
                                return findContainer(camm, movieIn, movieIn.BaseStream.Position + next.length - next.headerLength);
                            }
                        }
                    }
                }
                movieIn.BaseStream.Seek(next.length - next.headerLength, SeekOrigin.Current);
                    
            } while (movieIn.BaseStream.Position < endPosition);

            return false;

        } 


        private bool findContainer(byte[] name, BinaryReader movieIn, long endPosition) {

            do
            {
                var next = new atomlib.Container();
                next.ReadBoxHeader(movieIn);
                System.Console.WriteLine("fC - " + System.Text.Encoding.ASCII.GetString(next.name));
                if (next.CompareName(name))
                {
                    return true;
                }
                movieIn.BaseStream.Seek(next.length - next.headerLength, SeekOrigin.Current);
 
            } while (movieIn.BaseStream.Position < endPosition);

            return false;

        } 


    }
}
