# VR180PhotoTools
Some .net tools for converting between equirectangular images and Googles VR180 Photo format

VR180PhotoTools consists of a couple of .NET command line applications for converting between equi-rectangular 3D 180 degree photos and Google's VR180 format.
They've been built and tested using Mircosoft/Xamarin's mono on macOS and should be compatible with .net on Windows and other Mono supported OS's.

The applications are

equiToVr180Photo.exe, which converts equi-rectangular to VR180 photos.

Usage: equiToVr180Photo -f (lr|rl|tb|bt) -i equirectangular.jpg -o vr180.jpg [-v 180x180] [-q 90]
Mono Usage: mono (lr|rl|tb|bt) -f (lr|rl|tb|bt) -i equirectangular.jpg -o vr180.jpg [-v 180x180] [-q 90]

    -f (lr|rl|tb|bt) describes the equi-rectangular image format
                     lr is left-right, rl is right-left
                     tb is top-bottm, bt is bottom-top
                     where the first location describes where the left eye image is located

    -i the input file path, a 3D equi-rectangular JPEG image in the format described in the -f paramters

    -o the output file path

    -v Optional parameter decribing the field of view of the equi-rectangular image
       the value should be in the format of horizonal degrees and vertical degees seperated by x e.g. 180x120 
       if the parameter is not used then the value 180x180 is used by default.
    -q Optional parameter with the jpeg quality setting for the two new jpeg files, 0-100, 0 is very low quailty, 100 should be lossless, defaults to 100

and vr180ToEquiPhoto.exe which converts VR180 to equi-rectangular 3D 180 degree photos

Usage: vr180ToEquiPhoto.exe -i vr180Photo.jpg -o equiPhoto.jpg [-q 90]
Mono Usage: mono vr180ToEquiPhoto.exe -i vr180Photo.jpg -o equiPhoto.jpg [-q 90]

    -i the input file path, a VR180 photo JPEG image, right eye image embedded in the left eye image.

    -o the output file path

    -q Optional parameter with the jpeg quality setting for the two new jpeg files, 0-100, 0 is very low quailty, 100 should be lossless, defaults to 100


You can find the source code on Github

https://github.com/Vargol/VR180PhotoTools

This version targets for Framework 4.7.2 and has no dependencies.

The code for ExifReadWrite is taken from ExifLibray https://github.com/devedse/exiflibrary
and is used under the Terms of the MIT licence included in the source repository.

