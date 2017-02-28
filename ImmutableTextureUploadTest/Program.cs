
namespace ImmutableTextureUploadTest
{
    #region using directives
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using SharpDX.Direct3D;
    using SharpDX.Direct3D11;
    #endregion

    class Program
    {
        private const int TextureWidth = 2048;
        private const int TextureHeight = 2048;

        static void Main(string[] args)
        {
            using (var dx11Device = new Device(DriverType.Hardware, DeviceCreationFlags.BgraSupport))
            {
                for (int i = 0; ; i++)
                {
                    bool b = Test(dx11Device);
                    if (b == false)
                    {
                        Console.WriteLine("ERROR DETECTED!");
                        break;
                    }

                    if (i > 0 && (i + 1) % 100 == 0)
                    {
                        Console.WriteLine($"{i + 1} tests -> OK");
                    }

                    if (Console.KeyAvailable)
                    {
                        var cki = Console.ReadKey(false);
                        if (cki.Key == ConsoleKey.Escape)
                        {
                            break;
                        }
                    }
                }
            }
        }

        static unsafe bool Test(Device device)
        {

            int stride = TextureWidth * 4;
            byte[] data = new byte[stride * TextureHeight];
            for (int i = 0; i < data.Length; ++i)
            {
                data[i] = (byte)(i & 0xff);
            }

            Texture2DDescription desc = new Texture2DDescription
            {
                Width = TextureWidth,
                Height = TextureHeight,
                ArraySize = 1,
                BindFlags = BindFlags.None,
                Usage = ResourceUsage.Staging,
                CpuAccessFlags = CpuAccessFlags.Read,
                Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.None,
                SampleDescription = { Count = 1, Quality = 0 }
            };

            using (var stagingTexture = new Texture2D(device, desc))
            {
                fixed (byte* ptrByte = &data[0])
                {
                    using (var tex = UploadToImmutableTexture(device, TextureWidth, TextureHeight, stride, new IntPtr(ptrByte)))
                    {
                        device.ImmediateContext.CopySubresourceRegion(tex, 0, new ResourceRegion(0, 0, 0, TextureWidth, TextureHeight, 1), stagingTexture, 0);

                        var dataBox = device.ImmediateContext.MapSubresource(stagingTexture, 0, MapMode.Read, 0);

                        device.ImmediateContext.UnmapSubresource(stagingTexture, 0);

                        bool ok = Check(ptrByte, (byte*)dataBox.DataPointer.ToPointer(), TextureWidth, TextureHeight, stride, dataBox.RowPitch);

                        return ok;
                    }
                }
            }
        }

        static Texture2D UploadToImmutableTexture(Device device, int width, int height, int stride, IntPtr ptr)
        {
            Texture2DDescription desc = new Texture2DDescription
            {
                Width = TextureWidth,
                Height = TextureHeight,
                ArraySize = 1,
                BindFlags = BindFlags.ShaderResource,
                Usage = ResourceUsage.Default,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.None,
                SampleDescription = { Count = 1, Quality = 0 }
            };

            var texture = new Texture2D(device, desc, new[] { new SharpDX.DataRectangle(ptr, stride) });
            return texture;
        }

        static unsafe bool Check(byte* ptrOrig, byte* ptrCopy, int width, int height, int strideOrig, int strideCopy)
        {
            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    if (ptrOrig[x * 4] != ptrCopy[x * 4] || ptrOrig[x * 4 + 1] != ptrCopy[x * 4 + 1] || ptrOrig[x * 4 + 2] != ptrCopy[x * 4 + 2] || ptrOrig[x * 4 + 3] != ptrCopy[x * 4 + 3])
                    {
                        return false;
                    }
                }

                ptrOrig += strideOrig;
                ptrCopy += strideCopy;
            }

            return true;
        }
    }
}
