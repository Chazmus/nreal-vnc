// VncSharp - .NET VNC Client Library
// Copyright (C) 2008 David Humphrey
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

using System.IO;

namespace VncSharp.Encodings
{
	/// <summary>
	/// A 16-bit PixelReader.
	/// </summary>
	public sealed class PixelReader16 : PixelReader
	{
		public PixelReader16(BinaryReader reader, Framebuffer framebuffer) : base(reader, framebuffer)
		{
		}
	
		public override int ReadPixel()
		{
			var b = reader.ReadBytes(2);

            var pixel = (ushort)((uint)b[0] & 0xFF | (uint)b[1] << 8);

			var red = (byte)(((pixel >> framebuffer.RedShift) & framebuffer.RedMax) * 255 / framebuffer.RedMax);
			var green = (byte)(((pixel >> framebuffer.GreenShift) & framebuffer.GreenMax) * 255 / framebuffer.GreenMax);
			var blue = (byte)(((pixel >> framebuffer.BlueShift) & framebuffer.BlueMax) * 255 / framebuffer.BlueMax);

			return ToGdiPlusOrder(red, green, blue);			
		}
	}
}
