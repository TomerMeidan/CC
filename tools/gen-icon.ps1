param([string]$OutPath)

Add-Type -AssemblyName System.Drawing
$src = @'
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Collections.Generic;

public static class IconGen
{
    static byte[] RenderPng(int S)
    {
        var bmp = new Bitmap(S, S, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode     = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode   = PixelOffsetMode.HighQuality;
            g.Clear(Color.Transparent);

            float pad = S * 0.06f, side = S - 2 * pad, radius = S * 0.22f;
            var rect = new RectangleF(pad, pad, side, side);

            // Rounded-square tile
            using (var path = new GraphicsPath())
            {
                float d = radius * 2;
                path.AddArc(rect.X,            rect.Y,            d, d, 180, 90);
                path.AddArc(rect.Right - d,    rect.Y,            d, d, 270, 90);
                path.AddArc(rect.Right - d,    rect.Bottom - d,   d, d,   0, 90);
                path.AddArc(rect.X,            rect.Bottom - d,   d, d,  90, 90);
                path.CloseFigure();

                var c1 = Color.FromArgb(255, 14, 165, 233);  // sky
                var c2 = Color.FromArgb(255, 79,  70, 229);  // indigo
                using (var grad = new LinearGradientBrush(rect, c1, c2, 55f))
                    g.FillPath(grad, path);

                // top gloss
                var glossRect = new RectangleF(rect.X, rect.Y, rect.Width, rect.Height * 0.55f);
                using (var gloss = new LinearGradientBrush(glossRect,
                        Color.FromArgb(48, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), 90f))
                {
                    g.SetClip(path);
                    g.FillRectangle(gloss, glossRect);
                    g.ResetClip();
                }
            }

            // Gauge
            float cx = S / 2f, cy = S * 0.54f, gr = S * 0.28f;
            float sw = Math.Max(2f, S * 0.085f);
            var arcRect = new RectangleF(cx - gr, cy - gr, gr * 2, gr * 2);
            float start = 140f, full = 260f, value = 190f;

            using (var penTrack = new Pen(Color.FromArgb(70, 255, 255, 255), sw))
            { penTrack.StartCap = LineCap.Round; penTrack.EndCap = LineCap.Round;
              g.DrawArc(penTrack, arcRect, start, full); }

            using (var penVal = new Pen(Color.FromArgb(255, 255, 209, 102), sw)) // amber
            { penVal.StartCap = LineCap.Round; penVal.EndCap = LineCap.Round;
              g.DrawArc(penVal, arcRect, start, value); }

            // Needle
            double rad = (start + value) * Math.PI / 180.0;
            float nx = cx + (float)Math.Cos(rad) * (gr * 0.92f);
            float ny = cy + (float)Math.Sin(rad) * (gr * 0.92f);
            float bx = cx - (float)Math.Cos(rad) * (gr * 0.22f);
            float by = cy - (float)Math.Sin(rad) * (gr * 0.22f);
            using (var penN = new Pen(Color.White, Math.Max(1.5f, S * 0.05f)))
            { penN.StartCap = LineCap.Round; penN.EndCap = LineCap.Round;
              g.DrawLine(penN, bx, by, nx, ny); }

            // Center hub
            float hub = S * 0.075f;
            g.FillEllipse(Brushes.White, cx - hub, cy - hub, hub * 2, hub * 2);
            float hin = hub * 0.5f;
            using (var br = new SolidBrush(Color.FromArgb(255, 79, 70, 229)))
                g.FillEllipse(br, cx - hin, cy - hin, hin * 2, hin * 2);
        }
        using (var ms = new MemoryStream())
        {
            bmp.Save(ms, ImageFormat.Png);
            bmp.Dispose();
            return ms.ToArray();
        }
    }

    public static void Write(string outPath)
    {
        int[] sizes = { 16, 24, 32, 48, 64, 128, 256 };
        var pngs = new List<byte[]>();
        foreach (var s in sizes) pngs.Add(RenderPng(s));

        using (var fs = File.Create(outPath))
        using (var bw = new BinaryWriter(fs))
        {
            bw.Write((ushort)0);              // reserved
            bw.Write((ushort)1);              // type = icon
            bw.Write((ushort)sizes.Length);   // count
            int offset = 6 + 16 * sizes.Length;
            for (int i = 0; i < sizes.Length; i++)
            {
                int s = sizes[i];
                bw.Write((byte)(s >= 256 ? 0 : s));   // width
                bw.Write((byte)(s >= 256 ? 0 : s));   // height
                bw.Write((byte)0);                    // palette
                bw.Write((byte)0);                    // reserved
                bw.Write((ushort)1);                  // planes
                bw.Write((ushort)32);                 // bpp
                bw.Write((uint)pngs[i].Length);       // size
                bw.Write((uint)offset);               // offset
                offset += pngs[i].Length;
            }
            foreach (var p in pngs) bw.Write(p);
        }
    }
}
'@

Add-Type -TypeDefinition $src -ReferencedAssemblies System.Drawing
[IconGen]::Write($OutPath)
$fi = Get-Item $OutPath
Write-Host ("Wrote {0} ({1:N1} KB)" -f $fi.FullName, ($fi.Length / 1KB))
