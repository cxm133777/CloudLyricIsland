$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type @'
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
public static class IslandIconBuilder {
 [DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr icon);
 public static void Build(string input,string output){
  int[] sizes={16,20,24,32,48,64,128,256};byte[][] entries=new byte[sizes.Length][],payloads=new byte[sizes.Length][];
  using(var source=Image.FromFile(input)){
   for(int i=0;i<sizes.Length;i++)using(var bitmap=new Bitmap(sizes[i],sizes[i],PixelFormat.Format32bppArgb)){
    using(var graphics=Graphics.FromImage(bitmap)){graphics.InterpolationMode=InterpolationMode.HighQualityBicubic;graphics.CompositingQuality=CompositingQuality.HighQuality;graphics.PixelOffsetMode=PixelOffsetMode.HighQuality;graphics.DrawImage(source,0,0,sizes[i],sizes[i]);}
    using(var memory=new MemoryStream())using(var writer=new BinaryWriter(memory)){
     int size=sizes[i],maskStride=((size+31)/32)*4;
     writer.Write(40);writer.Write(size);writer.Write(size*2);writer.Write((ushort)1);writer.Write((ushort)32);writer.Write(0);writer.Write(size*size*4);writer.Write(0);writer.Write(0);writer.Write(0);writer.Write(0);
     for(int y=size-1;y>=0;y--)for(int x=0;x<size;x++){Color color=bitmap.GetPixel(x,y);writer.Write(color.B);writer.Write(color.G);writer.Write(color.R);writer.Write(color.A);}
     writer.Write(new byte[maskStride*size]);writer.Flush();payloads[i]=memory.ToArray();entries[i]=new byte[16];entries[i][0]=(byte)(size%256);entries[i][1]=(byte)(size%256);entries[i][4]=1;entries[i][6]=32;Array.Copy(BitConverter.GetBytes(payloads[i].Length),0,entries[i],8,4);
    }
   }
  }
  using(var writer=new BinaryWriter(File.Create(output))){writer.Write((ushort)0);writer.Write((ushort)1);writer.Write((ushort)sizes.Length);int offset=6+16*sizes.Length;for(int i=0;i<sizes.Length;i++){Array.Copy(BitConverter.GetBytes(offset),0,entries[i],12,4);writer.Write(entries[i]);offset+=payloads[i].Length;}foreach(var payload in payloads)writer.Write(payload);}
 }
}
'@ -ReferencedAssemblies System.Drawing
[IslandIconBuilder]::Build((Join-Path $PSScriptRoot 'assets\cloud-island.png'),(Join-Path $PSScriptRoot 'assets\cloud-island.ico'))
