# FFmpeg.AutoGen Samples

### What is this?

This is a repository to host example code to show how the [FFmpeg.AutoGen](https://github.com/Ruslan-B/FFmpeg.AutoGen/) library can be used.

All credits for the library goes to [Ruslan-B](https://github.com/Ruslan-B/) and I am just trying to upload code as I go which might be useful to reference.

Included alongside sample code is also a quick use version of the *FFmpeg.AutoGen* library for Windows. This is a 32-bit built assembly of the FFmpeg.AutoGen library and it's configuration files alongside the 32-bit shared DLLs of the FFmpeg library.

### How do I use the pre-built version?

The current pre-built version of FFmpeg.AutoGen is the FFmpeg bindings, developed by Ruslan-B, for [**FFmpeg version 3.2**](https://www.ffmpeg.org/doxygen/3.2/).

This samples repository will be kept up-to-date with the main FFmpeg.AutoGen repository so new pre-built version are available to use.

All you need to do in order to use the version FFmpeg.AutoGen DLL library (in the AV_Libraries folder) in your code is by making sure that:

1.	you have **added a reference** to the FFmpeg.AutoGen DLL,
2.	 you have **set the path to the FFmpeg libraries DLLs**, so that the InteropHelp can see it,
3.	 you have made sure the all the files such as the **configuration files** are still **alongside** the **FFmpeg.AutoGen DLL**,
4.	 you have **declared** that you are using the library in your code:

```C#
using FFmpeg.AutoGen;
```

5. you are **not using a 64-bit shared FFmpeg library DLLs** with the 32-bit FFMpeg.AutoGen DLL. 

You should be fine to code very similarly to how examples in C(lang) are written.

*NOTE*:  You can alternatively build the library to work on 64-bit machines by setting the CPU in Visual Studio to build and compile the code with x64, however it is best to run it with x86 so that people who have 32-bit computers can use it as well.

