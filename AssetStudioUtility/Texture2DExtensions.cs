using System;
using System.Buffers.Binary;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace AssetStudio
{
    /// <summary>
    /// A decoded BGRA32 texture held in a pooled buffer. Dispose returns the buffer
    /// to the pool. Rows are in decode order (top-down before any flip).
    /// </summary>
    public sealed class DecodedBgra32 : IDisposable
    {
        private byte[] buffer;

        internal DecodedBgra32(byte[] buffer, int dataLength, int sourceWidth, int width, int height)
        {
            this.buffer = buffer;
            DataLength = dataLength;
            SourceWidth = sourceWidth;
            Width = width;
            Height = height;
        }

        /// <summary>Row stride of the decoded buffer in pixels (uncropped width for Switch-swizzled textures).</summary>
        public int SourceWidth { get; }
        public int Width { get; }
        public int Height { get; }
        public int DataLength { get; }

        public ReadOnlySpan<byte> Pixels => buffer.AsSpan(0, DataLength);

        public void Dispose()
        {
            if (buffer != null)
            {
                BigArrayPool<byte>.Shared.Return(buffer, clearArray: true);
                buffer = null;
            }
        }
    }

    public static class Texture2DExtensions
    {
        private static ReadOnlySpan<byte> RgbaIrMagic => "HARUKI_RGBAIR_V1"u8;
        private const int RgbaIrHeaderSize = 36;

        /// <summary>
        /// Exact RGBA IR payload length for a texture of the given dimensions
        /// (36-byte header plus RGBA rows), matching <see cref="WriteRgbaIr"/> output.
        /// </summary>
        public static long RgbaIrPayloadLength(int width, int height)
        {
            return RgbaIrHeaderSize + (long)width * height * 4;
        }

        /// <summary>
        /// Decodes the texture once into a pooled BGRA32 buffer without touching
        /// ImageSharp. Returns null when the pixel data cannot be decoded.
        /// </summary>
        public static DecodedBgra32 DecodeBgra32(this Texture2D m_Texture2D)
        {
            var converter = new Texture2DConverter(m_Texture2D);
            var buff = BigArrayPool<byte>.Shared.Rent(converter.OutputDataSize);
            try
            {
                if (!converter.DecodeTexture2D(buff))
                {
                    BigArrayPool<byte>.Shared.Return(buff, clearArray: true);
                    return null;
                }

                var sourceWidth = converter.UsesSwitchSwizzle
                    ? converter.GetUncroppedSize().Width
                    : m_Texture2D.m_Width;
                return new DecodedBgra32(buff, converter.OutputDataSize, sourceWidth, m_Texture2D.m_Width, m_Texture2D.m_Height);
            }
            catch
            {
                BigArrayPool<byte>.Shared.Return(buff, clearArray: true);
                throw;
            }
        }

        /// <summary>
        /// Writes the RGBA IR payload (HARUKI_RGBAIR_V1 header + vertically flipped
        /// RGBA rows) straight from a decoded BGRA buffer. Byte-for-byte identical to
        /// the previous ImageSharp path (ConvertToImage(flip: true) + WriteRgbaIrToStream)
        /// without the intermediate Image&lt;Bgra32&gt; allocation, extra copy and flip pass.
        /// </summary>
        public static void WriteRgbaIr(Stream destination, DecodedBgra32 decoded)
        {
            const int bytesPerPixel = 4;
            var width = decoded.Width;
            var height = decoded.Height;
            var stride = checked(width * bytesPerPixel);
            Span<byte> header = stackalloc byte[RgbaIrHeaderSize];
            RgbaIrMagic.CopyTo(header);
            BinaryPrimitives.WriteInt32LittleEndian(header.Slice(16, sizeof(int)), width);
            BinaryPrimitives.WriteInt32LittleEndian(header.Slice(20, sizeof(int)), height);
            BinaryPrimitives.WriteInt32LittleEndian(header.Slice(24, sizeof(int)), stride);
            BinaryPrimitives.WriteInt32LittleEndian(header.Slice(28, sizeof(int)), 1);
            BinaryPrimitives.WriteInt32LittleEndian(header.Slice(32, sizeof(int)), 0);
            destination.Write(header);

            var source = decoded.Pixels;
            var sourceStride = checked(decoded.SourceWidth * bytesPerPixel);
            var rowBytes = new byte[stride];
            for (var y = 0; y < height; y++)
            {
                // IR rows are the vertically flipped image: IR row y = decoded row (height-1-y).
                var sourceRow = source.Slice((height - 1 - y) * sourceStride, stride);
                for (var offset = 0; offset < stride; offset += bytesPerPixel)
                {
                    rowBytes[offset] = sourceRow[offset + 2];     // R (from BGRA)
                    rowBytes[offset + 1] = sourceRow[offset + 1]; // G
                    rowBytes[offset + 2] = sourceRow[offset];     // B
                    rowBytes[offset + 3] = sourceRow[offset + 3]; // A
                }
                destination.Write(rowBytes);
            }
        }

        public static Image<Bgra32> ConvertToImage(this Texture2D m_Texture2D, bool flip)
        {
            return ImageSharpNativeAotGuard.Run(() =>
            {
                var converter = new Texture2DConverter(m_Texture2D);
                var buff = BigArrayPool<byte>.Shared.Rent(converter.OutputDataSize);
                var spanBuff = buff.AsSpan(0, converter.OutputDataSize);
                try
                {
                    if (!converter.DecodeTexture2D(buff))
                        return null;

                    Image<Bgra32> image;
                    if (converter.UsesSwitchSwizzle)
                    {
                        var uncroppedSize = converter.GetUncroppedSize();
                        image = Image.LoadPixelData<Bgra32>(spanBuff, uncroppedSize.Width, uncroppedSize.Height);
                        image.Mutate(x => x.Crop(m_Texture2D.m_Width, m_Texture2D.m_Height));
                    }
                    else
                    {
                        image = Image.LoadPixelData<Bgra32>(spanBuff, m_Texture2D.m_Width, m_Texture2D.m_Height);
                    }

                    if (flip)
                    {
                        image.Mutate(x => x.Flip(FlipMode.Vertical));
                    }
                    return image;
                }
                finally
                {
                    BigArrayPool<byte>.Shared.Return(buff, clearArray: true);
                }
            });
        }

        public static MemoryStream ConvertToStream(this Texture2D m_Texture2D, ImageFormat imageFormat, bool flip)
        {
            return ImageSharpNativeAotGuard.Run(() =>
            {
                var image = ConvertToImage(m_Texture2D, flip);
                if (image != null)
                {
                    using (image)
                    {
                        return image.ConvertToStream(imageFormat);
                    }
                }
                return null;
            });
        }

        public static bool WriteBmpToStream(this Texture2D m_Texture2D, Stream destination, bool flip)
        {
            var converter = new Texture2DConverter(m_Texture2D);
            var buff = BigArrayPool<byte>.Shared.Rent(converter.OutputDataSize);
            try
            {
                if (!converter.DecodeTexture2D(buff))
                {
                    return false;
                }

                var uncroppedSize = converter.UsesSwitchSwizzle
                    ? converter.GetUncroppedSize()
                    : new Size(m_Texture2D.m_Width, m_Texture2D.m_Height);
                WriteBgra32Bmp(
                    destination,
                    buff.AsSpan(0, converter.OutputDataSize),
                    uncroppedSize.Width,
                    m_Texture2D.m_Width,
                    m_Texture2D.m_Height,
                    flip);
                return true;
            }
            finally
            {
                BigArrayPool<byte>.Shared.Return(buff, clearArray: true);
            }
        }

        private static void WriteBgra32Bmp(Stream destination, ReadOnlySpan<byte> bgra, int sourceWidth, int width, int height, bool flip)
        {
            const int fileHeaderSize = 14;
            const int dibHeaderSize = 40;
            const int bytesPerPixel = 4;
            var rowBytes = checked(width * bytesPerPixel);
            var pixelBytes = checked(rowBytes * height);
            var pixelOffset = fileHeaderSize + dibHeaderSize;
            var fileSize = checked(pixelOffset + pixelBytes);

            Span<byte> header = stackalloc byte[pixelOffset];
            header[0] = (byte)'B';
            header[1] = (byte)'M';
            BinaryPrimitives.WriteInt32LittleEndian(header.Slice(2, sizeof(int)), fileSize);
            BinaryPrimitives.WriteInt32LittleEndian(header.Slice(10, sizeof(int)), pixelOffset);
            BinaryPrimitives.WriteInt32LittleEndian(header.Slice(14, sizeof(int)), dibHeaderSize);
            BinaryPrimitives.WriteInt32LittleEndian(header.Slice(18, sizeof(int)), width);
            BinaryPrimitives.WriteInt32LittleEndian(header.Slice(22, sizeof(int)), height);
            BinaryPrimitives.WriteInt16LittleEndian(header.Slice(26, sizeof(short)), 1);
            BinaryPrimitives.WriteInt16LittleEndian(header.Slice(28, sizeof(short)), 32);
            BinaryPrimitives.WriteInt32LittleEndian(header.Slice(34, sizeof(int)), pixelBytes);
            destination.Write(header);

            for (var fileRow = 0; fileRow < height; fileRow++)
            {
                var sourceY = flip ? fileRow : height - 1 - fileRow;
                destination.Write(bgra.Slice(sourceY * sourceWidth * bytesPerPixel, rowBytes));
            }
        }
    }
}
