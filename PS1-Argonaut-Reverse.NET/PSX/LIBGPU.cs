namespace ArgonautReverse.LibGPU
{
	/* Flat Triangle */
	public struct POLY_F3
	{
		public const int ByteSize = sizeof(int) * 5;

		public uint  tag;
		public byte   r0, g0, b0, code;
		public short  x0,     y0;
		public short  x1, y1;
		public short  x2, y2;
	}

	/* Flat Quadrangle */
	public struct POLY_F4
	{
		public const int ByteSize = sizeof(int) * 6;

		public uint  tag;
		public byte   r0, g0, b0, code;
		public short  x0,     y0;
		public short  x1, y1;
		public short  x2, y2;
		public short  x3, y3;
	}

	/* Flat Textured Triangle */
	public struct POLY_FT3
	{
		public const int ByteSize = sizeof(int) * 8;

		public uint  tag;
		public byte   r0, g0, b0, code;
		public short  x0,     y0;
		public byte   u0, v0; public ushort  clut;
		public short  x1, y1;
		public byte   u1, v1; public ushort  tpage;
		public short  x2, y2;
		public byte   u2, v2; public ushort  pad1;
	}

	/* Flat Textured Quadrangle */
	public struct POLY_FT4
	{
		public const int ByteSize = sizeof(int) * 10;

		public uint  tag;
		public byte   r0, g0, b0, code;
		public short  x0,     y0;
		public byte   u0, v0; public ushort  clut;
		public short  x1, y1;
		public byte   u1, v1; public ushort  tpage;
		public short  x2, y2;
		public byte   u2, v2; public ushort  pad1;
		public short  x3, y3;
		public byte   u3, v3; public ushort  pad2;
	}

	/* Gouraud Triangle */
	public struct POLY_G3
	{
		public const int ByteSize = sizeof(int) * 7;

		public uint  tag;
		public byte   r0, g0, b0, code;
		public short  x0,     y0;
		public byte   r1, g1, b1, pad1;
		public short  x1, y1;
		public byte   r2, g2, b2, pad2;
		public short  x2, y2;
	}

	/* Gouraud Quadrangle */
	public struct POLY_G4
	{
		public const int ByteSize = sizeof(int) * 9;

		public uint  tag;
		public byte   r0, g0, b0, code;
		public short  x0,     y0;
		public byte   r1, g1, b1, pad1;
		public short  x1, y1;
		public byte   r2, g2, b2, pad2;
		public short  x2, y2;
		public byte   r3, g3, b3, pad3;
		public short  x3, y3;
	}

	/* Gouraud Textured Triangle */
	public struct POLY_GT3
	{
		public const int ByteSize = sizeof(int) * 10;

		public uint  tag;
		public byte   r0, g0, b0, code;
		public short  x0,     y0;
		public byte   u0, v0; public ushort  clut;
		public byte   r1, g1, b1, p1;
		public short  x1, y1;
		public byte   u1, v1; public ushort  tpage;
		public byte   r2, g2, b2, p2;
		public short  x2, y2;
		public byte   u2, v2; public ushort  pad2;
	}

	/* Gouraud Textured Quadrangle */
	public struct POLY_GT4
	{
		public const int ByteSize = sizeof(int) * 13;

		public uint  tag;
		public byte   r0, g0, b0, code;
		public short  x0,     y0;
		public byte   u0, v0; public ushort  clut;
		public byte   r1, g1, b1, p1;
		public short  x1, y1;
		public byte   u1, v1; public ushort  tpage;
		public byte   r2, g2, b2, p2;
		public short  x2, y2;
		public byte   u2, v2; public ushort  pad2;
		public byte   r3, g3, b3, p3;
		public short  x3, y3;
		public byte   u3, v3; public ushort  pad3;
	}

	/* free size Sprite */
	public struct SPRT
	{
		public const int ByteSize = sizeof(int) * 5;

		public uint	tag;
		public byte	r0, g0, b0, code;
		public short	x0, 	y0;
		public byte	u0, v0;	public ushort	clut;
		public short	w,	h;
	}

	/* free size Tile */
	public struct TILE
	{
		public const int ByteSize = sizeof(int) * 4;

		public uint tag;
		public byte r0, g0, b0, code;
		public short	x0, 	y0;
		public short	w,	h;
	}

	/* 1x1 Tile */
	public struct TILE_1
	{
		public const int ByteSize = sizeof(int) * 3;

		public uint	tag;
		public byte	r0, g0, b0, code;
		public short   x0, 	y0;
	}

	/* Drawing Mode */
	public unsafe struct DR_MODE
	{
		public const int ByteSize = sizeof(int) * 3;

		public uint tag;
		public fixed uint code[2];
	}				

	/* MoveImage */
	public unsafe struct DR_MOVE
	{
		public const int ByteSize = sizeof(int) * 6;

		public uint tag;
		public fixed uint code[5];
	}

	/* Drawing TPage */
	public unsafe struct DR_TPAGE
	{
		public const int ByteSize = sizeof(int) * 2;

		public uint tag;
		public fixed uint code[1];
	}
}
