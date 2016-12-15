using System;

// TODO: The System.Drawing library can be accessed by adding it as a reference,
//       this will allow you to view the Imaging library as well, make sure you 
//       right click the solution and then add a reference.
using System.Drawing;
using System.Drawing.Imaging;

// FFmpeg.AutoGen will become available to use once it's reference has been added.
using FFmpeg.AutoGen;

/* Sample code:
 * 
 * This class will contain encoding/decoding, video and audio related sample code utilising the FFmpeg libraries 
 * via the FFmpeg.AutoGen bindings provided at (http://github.com/Ruslan-B/FFmpeg.AutoGen/).
*/

// TODO: Make sure the "Allow unsafe code" checkbox is checked in the project's Properties->Build (under the general section).
//       This is the only way we can use pointers in the code.

// TODO: 1. To build for 32-bit (x86) with 32-bit FFmpeg DLLs, make sure we have copied all the DLLs into the solutions /bin folder.
//       2. Now force the FFmpeg.AutoGen.Example to compile the FFmpeg.AutoGen.dll under the 32-bit CPU (make sure this is set in the configuration manager).
//       3. Make sure the InteropHelper's ffmpeg/library path is set to where the FFmpeg DLLs are (in this case all in the /bin folder), 
//          register the libraries search path as the current directory.
//       4. Finally add the FFmpeg.AutoGen.dll as a reference from the /bin folder of the FFmpeg.AutoGen.Example once it has been built/compiled and run once.

// NOTE: Only run the 32-bit FFmpeg DLLs with the x86 built FFMpeg.AutoGen.dll file.


namespace FFmpeg.AutoGen.Samples
{
    public class Sample
    {
        // Sample entry-point.
        static void Main(string[] args)
        {
            // Initiate the libraries since we are not initialising the class.
            InitiateAVLibraries();

            // Take a sample file in the /bin folder to use to encode.
            EncodeImage("Sample.png");

            // Decoding example by providing a URL or a path to a file which can be decoded and convereted into an image and saved.
            // DecodeFile("");
        }

        /// <summary>
        /// Initiate the class by running this method first.
        /// </summary>
        public Sample()
        {
            // When initiating make sure we register the FFmpeg library with the InteropHelper.
            InitiateAVLibraries();
        }

        /// <summary>
        /// 
        /// </summary>
        private static unsafe void InitiateAVLibraries()
        {
            // Register the search path for the InteropHelper to locate the FFmpeg DLLs.
            // All of the 32-bit DLLs have been placed in the AV_Libraries folder.
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT:

                case PlatformID.Win32S:

                case PlatformID.Win32Windows:
                    // var ffmpegPath = Environment.CurrentDirectory;

                    // Since this searches in the /bin folder for the DLLs we need to specify their location instead as the DLLs are required to build, 
                    // we search for the FFmpeg DLLs four directories up, in the AV_Libraries folder.
                    var ffmpegPath = string.Format(@"../../../../AV_Libraries");
                    Console.WriteLine(@"Searching for DLLs in: {0}", ffmpegPath);

                    InteropHelper.RegisterLibrariesSearchPath(ffmpegPath);
                    Console.WriteLine(@"Found the DLLs in the library and they have been registered with InteropHelper.");
                    break;

                case PlatformID.Unix:

                case PlatformID.MacOSX:
                    var libraryPath = Environment.GetEnvironmentVariable(InteropHelper.LD_LIBRARY_PATH);
                    InteropHelper.RegisterLibrariesSearchPath(libraryPath);
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        public static unsafe void EncodeImage(string imageFileName)
        {
            ffmpeg.av_register_all();

            var fileFormatContext = ffmpeg.avformat_alloc_context();
            if (ffmpeg.avformat_open_input(&fileFormatContext, imageFileName, null, null) != 0)
            {
                Console.WriteLine($@"Error opening the file: {imageFileName}.");
            }
            else
            {
                Console.WriteLine(@"Opened the file for encoding.");
            }

            if (ffmpeg.avformat_find_stream_info(fileFormatContext, null) < 0)
            {
                Console.WriteLine(@"Error in finding stream information.");
                ffmpeg.avformat_close_input(&fileFormatContext);
            }

            // Set up a video stream context if there is a video stream in the file.
            AVStream* videoStream = null;
            int videoStreamIndex = -1;
            for (var i = 0; i < fileFormatContext->nb_streams; i++)
            {
                if (fileFormatContext->streams[i]->codec->codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO)
                {
                    Console.WriteLine(@"We found a potential video stream.");
                    videoStream = fileFormatContext->streams[i];
                    videoStreamIndex = i;
                    break;
                }
            }

            // Make sure there is a video stream in the AVStream variable we just set.
            if (videoStream == null)
            {
                throw new ApplicationException(@"We could not find video stream from the file context.");
            }
            else
            {
                Console.WriteLine(@"There is a video stream in the file.");
            }

            // Set up the JPEG decoding context.
            var decodeCodecContext = fileFormatContext->streams[videoStreamIndex]->codec;

            var decodingCodec = ffmpeg.avcodec_find_decoder(decodeCodecContext->codec_id);
            if (decodingCodec == null)
            {
                Console.WriteLine(@"We cannot find the decoder.");
                ffmpeg.avformat_close_input(&fileFormatContext);
            }
            else
            {
                Console.WriteLine($@"Found the decoder with codec id: {decodeCodecContext->codec_id}");
            }

            // Check the decoder's codec capabilities and if it is equal to truncated, set the decoding codec context flag to truncated.
            if ((decodingCodec->capabilities & ffmpeg.AV_CODEC_CAP_TRUNCATED) == ffmpeg.AV_CODEC_CAP_TRUNCATED)
            {
                decodeCodecContext->flags |= ffmpeg.AV_CODEC_CAP_TRUNCATED;
            }

            // Open the decoding codec with the codec context.
            if (ffmpeg.avcodec_open2(decodeCodecContext, decodingCodec, null) < 0)
            {
                Console.WriteLine(@"We cannot open the decoder.");
                ffmpeg.avformat_close_input(&fileFormatContext);
            }
            else
            {
                Console.WriteLine(@"Opened the decoder.");
            }

            // Get and initialise a new AVPacket to send our data into the decoder.
            var encodedPacket = new AVPacket();
            ffmpeg.av_init_packet(&encodedPacket);

            // Allocate the AVFrame which we will store the data decoded from the encoded packet.
            var decodedFrame = ffmpeg.av_frame_alloc();

            if (ffmpeg.av_read_frame(fileFormatContext, &encodedPacket) < 0)
            {
                Console.WriteLine(@"We could not read the frame.");
                ffmpeg.av_free_packet(&encodedPacket);
                ffmpeg.avcodec_close(decodeCodecContext);
                ffmpeg.avformat_close_input(&fileFormatContext);
            }
            else
            {
                Console.WriteLine(@"Read frame from image.");
            }

            // Send the packet to decode given the decoding codec context and the pointer to the packet.
            if (ffmpeg.avcodec_send_packet(decodeCodecContext, &encodedPacket) < 0)
            {
                // Remove references from the packet and frame if we were not able send the packet.
                ffmpeg.av_packet_unref(&encodedPacket);
                ffmpeg.av_frame_unref(decodedFrame);

                throw new ApplicationException(@"There was an error when sending the packet with avcodec_send_packet().");
            }

            // Receive the decoded frame given the decoding codec context and the pointer to the frame.
            if (ffmpeg.avcodec_receive_frame(decodeCodecContext, decodedFrame) < 0)
            {
                // Remove the reference to the decoded frame if we were unable decode data.
                ffmpeg.av_frame_unref(decodedFrame);

                throw new ApplicationException(@"There was an error when receiving the frame with avcodec_receive_frame().");
            }

            // Unreference the AVPacket pointer.
            ffmpeg.av_packet_unref(&encodedPacket);

            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            // We are now finished with decoding the frame, we now need to convert the frame's pixel format
            // to a pixel format the encoder supports.

            var sourcePixelFormat = decodeCodecContext->pix_fmt;
            var destinationPixelFormat = AVPixelFormat.AV_PIX_FMT_YUV420P;

            var width = decodeCodecContext->width;
            var height = decodeCodecContext->height;

            // A scaling context for converting the frame's pixel format to the encoding context pixel format when we fill the decoded frame.
            var convertPixelFormatContext = ffmpeg.sws_getContext(
                width, height, sourcePixelFormat,
                width, height, destinationPixelFormat,
                ffmpeg.SWS_FAST_BILINEAR, null, null, null);

            // Make sure we can initialise the conversion context.
            if (convertPixelFormatContext == null)
            {
                throw new ApplicationException(@"We could not initialise the conversion context.");
            }

            // Allocate our converted frame and buffer and fill the picture.
            var convertedFrame = ffmpeg.av_frame_alloc();

            var convertedFrameBufferSize = ffmpeg.av_image_get_buffer_size(destinationPixelFormat, width, height, 1);
            var convertedFrameBuffer = (sbyte*)ffmpeg.av_malloc((ulong)convertedFrameBufferSize);

            ffmpeg.avpicture_fill((AVPicture*)convertedFrame, convertedFrameBuffer, destinationPixelFormat, width, height);

            // Set up the image scaling parameters and scale the image.
            var sourceData = &decodedFrame->data0;

            var destinationData = &convertedFrame->data0;

            var sourceStride = decodedFrame->linesize;

            var destinationStride = convertedFrame->linesize;

            // TODO: Calling sws_scale results in a sws_scaler warning saying that the data is not aligned and that it can cause speedloss.
            //       Is this because we are not providing the whole data?
            ffmpeg.sws_scale(convertPixelFormatContext, sourceData, sourceStride, 0, height, destinationData, destinationStride);
            
            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            // Set up the encode codec and encode codec context.
            // AVCodecID encodeWith = AVCodecID.AV_CODEC_ID_FLV1;

            // var encodingCodec = ffmpeg.avcodec_find_encoder(encodeWith);
            // var encodeCodecContext = ffmpeg.avcodec_alloc_context3(encodingCodec);

            // Set up the encoding context parameters.
            // encodeCodecContext->width = 320;
            // encodeCodecContext->height = 240;
            // encodeCodecContext->pix_fmt = AVPixelFormat.AV_PIX_FMT_YUV420P;
            // encodeCodecContext->time_base.num = 1;
            // encodeCodecContext->time_base.den = 10;

            // Open the codec for encoding.
            // ffmpeg.avcodec_open2(encodeCodecContext, encodingCodec, null);

            // Set up data pointers from the received frame.
            // decodedFrame->format = videoStream->codec->pix_fmt;
            // decodedFrame->width = decodeCodecContext->width;
            // decodedFrame->height = decodeCodecContext->height;

            // Store the data from the frame.


            // Allocate the AVFrame which we will scale from the destination frame.
            // var dstFrame = ffmpeg.av_frame_alloc();

            // dstFrame->format = (int)encodeCodecContext->pix_fmt;
            // dstFrame->width = encodeCodecContext->width;
            // dstFrame->height = encodeCodecContext->height;

            Console.ReadLine();
        }

        public static unsafe void DecodeFile(string url)
        {
            // Register the libraries we intend to use.
            ffmpeg.av_register_all();
            ffmpeg.avcodec_register_all();
            ffmpeg.avformat_network_init();

            // Allocate a file format context to the file we will be streaming and make sure we can open it.
            var fileFormatContext = ffmpeg.avformat_alloc_context();
            if (ffmpeg.avformat_open_input(&fileFormatContext, url, null, null) != 0)
            {
                throw new ApplicationException(@"We could not open the file given in the url or string.");
            }

            // If we could open it, make sure we are able to obtain stream information from the file.
            if (ffmpeg.avformat_find_stream_info(fileFormatContext, null) != 0)
            {
                throw new ApplicationException(@"We could not find stream information.");
            }

            // Set up a video stream context if there is a video stream in the file.
            AVStream* videoStream = null;
            for (var i = 0; i < fileFormatContext->nb_streams; i++)
            {
                if (fileFormatContext->streams[i]->codec->codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO)
                {
                    Console.WriteLine(@"We found a potential video stream.");
                    videoStream = fileFormatContext->streams[i];
                    break;
                }
            }

            // Make sure there is a video stream in the AVStream variable we just set.
            if (videoStream == null)
            {
                throw new ApplicationException(@"We could not find video stream from the file context.");
            }
            else
            {
                Console.WriteLine(@"There is a video stream in the file.");
            }

            // Set up the codec and conversion context.
            var codecContext = *videoStream->codec;

            // Set up the image conversion parameters.
            var sourcePixelFormat = codecContext.pix_fmt;
            var conversionPixelFormat = AVPixelFormat.AV_PIX_FMT_BGR24;

            var width = codecContext.width;
            var height = codecContext.height;

            // Set up the conversion context.
            var convertContext = ffmpeg.sws_getContext(
                width, height, sourcePixelFormat,
                width, height, conversionPixelFormat,
                ffmpeg.SWS_FAST_BILINEAR, null, null, null);

            // Make sure we can initialise the conversion context.
            if (convertContext == null)
            {
                throw new ApplicationException(@"We could not initialise the conversion context.");
            }

            // Allocate our converted frame and buffer and fill the picture.
            var convertedFrame = ffmpeg.av_frame_alloc();

            var convertedFrameBufferSize = ffmpeg.av_image_get_buffer_size(conversionPixelFormat, width, height, 1);
            var convertedFrameBuffer = (sbyte*)ffmpeg.av_malloc((ulong)convertedFrameBufferSize);
            ffmpeg.avpicture_fill((AVPicture*)convertedFrame, convertedFrameBuffer, conversionPixelFormat, width, height);

            // Set up the decoder with the codec id from the file and make sure it has been found.
            var codecId = codecContext.codec_id;
            var decoderCodec = ffmpeg.avcodec_find_decoder(codecId);
            if (decoderCodec == null)
            {
                throw new ApplicationException(@"We could not find the codec specified; it could be unsupported.");
            }

            // Re-use the codec context from the video stream.
            var decodingCodecContext = &codecContext;

            // Check the decoder's codec capabilities and if it is equal to truncated, set the decoding codec context flag to truncated.
            if ((decoderCodec->capabilities & ffmpeg.AV_CODEC_CAP_TRUNCATED) == ffmpeg.AV_CODEC_CAP_TRUNCATED)
            {
                decodingCodecContext->flags |= ffmpeg.AV_CODEC_CAP_TRUNCATED;
            }

            // Make sure we can open the context to decode.
            if (ffmpeg.avcodec_open2(decodingCodecContext, decoderCodec, null) < 0)
            {
                throw new ApplicationException(@"We could not open the decoder's codec.");
            }

            // Allocate the decoded frame.
            var decodedFrame = ffmpeg.av_frame_alloc();

            // Initialise a new AVPacket from it's pointer for decoding.
            var packet = new AVPacket();
            var packetPointer = &packet;
            ffmpeg.av_init_packet(packetPointer);

            // Start a while loop to read frames and decode the packet.
            var frameNumber = 0;
            while (frameNumber < 7)
            {
                // Attempt to read the frame given the file context and the AVPacket.
                if (ffmpeg.av_read_frame(fileFormatContext, packetPointer) < 0)
                {
                    // If we could not read it remove the reference to the AVPacket and the AVFrame.
                    ffmpeg.av_packet_unref(packetPointer);
                    ffmpeg.av_frame_unref(decodedFrame);

                    throw new ApplicationException(@"We could not read the frame.");
                }

                // If the stream index in the packet does not match that in the video stream,
                // then start again at the top of the while loop.
                if (packetPointer->stream_index != videoStream->index)
                {
                    continue;
                }

                // Initialise the got picture variable and decode the data in the frame (given the codec context) to the packet.
                // var gotPicture = 0;
                // var size = ffmpeg.avcodec_decode_video2(decodingCodecContext, decodedFrame, &gotPicture, packetPointer);

                // Check if the data was decoded from the frame to the packet.
                // if (size < 0)
                // {
                //     throw new ApplicationException(string.Format(@"There was an error when decoding frame no. {0}", frameNumber));
                // }

                // If we received the decoded data, then proceed to generate an image from the buffer.
                // if (gotPicture == 1)

                // Make use of the new FFMpeg 3.2 API's send_packet() and receive_packet() functions:
                // Send the packet to decode given the decoding codec context and the pointer to the packet.
                if (ffmpeg.avcodec_send_packet(decodingCodecContext, packetPointer) < 0)
                {
                    // Remove references from the packet and frame if we were not able send the packet.
                    ffmpeg.av_packet_unref(packetPointer);
                    ffmpeg.av_frame_unref(decodedFrame);

                    throw new ApplicationException($@"There was an error when sending the packet {frameNumber} with avcodec_send_packet().");
                }

                // Receive the decoded frame given the decoding codec context and the pointer to the decoded frame.
                if (ffmpeg.avcodec_receive_frame(decodingCodecContext, decodedFrame) < 0)
                {
                    // Remove the reference to the decoded frame if we were unable decode data.
                    ffmpeg.av_frame_unref(decodedFrame);

                    throw new ApplicationException($@"There was an error when receiving the frame {frameNumber} with avcodec_receive_frame().");
                }

                // Unreference the AVPacket pointer.
                ffmpeg.av_packet_unref(packetPointer);

                Console.WriteLine($@"Decoded frame no.: {frameNumber}.");

                ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                // Set up the image scaling parameters and scale the image.
                var source = &decodedFrame->data0;
                var destination = &convertedFrame->data0;
                var sourceStride = decodedFrame->linesize;
                var destinationStride = convertedFrame->linesize;
                ffmpeg.sws_scale(convertContext, source, sourceStride, 0, height, destination, destinationStride);

                // Locate the address of the converted frame in memory.
                var convertedFrameAddress = convertedFrame->data0;

                // Retrieve the buffer from the address in memory.
                var imageBufferPointer = new IntPtr(convertedFrameAddress);

                // Set the linesize and create a new Bitmap image from the retrieved buffer from memory.
                var linesize = destinationStride[0];
                using (var bitmap = new Bitmap(width, height, linesize, PixelFormat.Format24bppRgb, imageBufferPointer))
                {
                    // Save the bitmap image as a JPEG to the current directory.
                    bitmap.Save(@"latest_frame_buffer.jpg", ImageFormat.Jpeg);
                }

                // Unreference the AVFrame pointer.
                ffmpeg.av_frame_unref(decodedFrame);

                // Increment frame number to keep a record of the number of frames decoded so far.
                frameNumber++;
            }

            // Once we have finished decoding the number of frames we specified in the while loop, 
            // free all the data from memory and close the decoding codecs and format contexts.
            ffmpeg.av_free(convertedFrame);
            ffmpeg.av_free(convertedFrameBuffer);
            ffmpeg.sws_freeContext(convertContext);

            ffmpeg.av_free(decodedFrame);
            ffmpeg.avcodec_close(decodingCodecContext);
            ffmpeg.avformat_close_input(&fileFormatContext);
        }

    }
}
