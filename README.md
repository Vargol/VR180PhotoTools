# VR180PhotoTools
Some .net tools for converting between equirectangular images and Googles VR180 Photo format

VR180PhotoTools consists of a couple of .NET command line applications for converting between equi-rectangular 3D 180 degree photos and Google's VR180 format.
They've been built and tested usin Mircosoft/Xamarin's mono on macOS

The application are

equiToVr180Photo.exe, which converts equi-rectangular to VR180 photos.

Usage: equiToVr180Photo (lr|rl|tb|bt) equirectangular.jpg vr180.jpg
Mono Usage: mono (lr|rl|tb|bt) equiToVr180Photo.exe equirectangular.jpg vr180.jpg
    (lr|rl|tb|bt) describes the equi-rectangular image format
    lr is left-right, rl is right-left
    tb is top-bottm, bt is bottom-top
  where the first location describes where the left eye image is located

Usage: vr180ToEquiPhoto.exe vr180Photo.jpg equiPhoto.jpg
Mono Usage: mono vr180ToEquiPhoto.exe vr180Photo.jpg equiPhoto.jpg

and vr180ToEquiPhoto.exe which converts VR180 to equi-rectangular 3D 180 degree photos

Usage: vr180ToEquiPhoto.exe vr180Photo.jpg equiPhoto.jpg
Mono Usage: mono vr180ToEquiPhoto.exe vr180Photo.jpg equiPhoto.jpg


You can find the source code on Github

https://github.com/Vargol/VR180PhotoTools

This version targets for Framework 4.7.2 and has no dependencies.

The code for ExifReadWrite is taken from ExifLibray https://github.com/devedse/exiflibrary
and is used under the Terms of the MIT licence included in the source repository.

