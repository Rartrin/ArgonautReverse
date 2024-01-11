using ArgonautReverse.Engine.Versions;
using ArgonautReverse.IO;
using ArgonautReverse.LibGTE;

namespace ArgonautReverse.WadSections.DPSX
{
	public sealed class MAPINDEX
	{
		public const int ByteSize = 8;

		public uint Index;
		public MAPINDEX Next;
	}
	public sealed class ZONE
	{
		public uint Zone;
		//public uint ViewZone;
	}

	public sealed class Waypoint:IReadable<Waypoint>
	{
		public const int ByteSize = 12 + POS.ByteSize;//36

		public uint Next;
		public uint Prev;
		public readonly POS Position;
		public readonly int Value;

		private Waypoint(uint next, uint prev, in POS position, int value)
		{
			this.Next = next;
			this.Prev = prev;
			Position = position;
			Value = value;
		}

		public static Waypoint Parse(WadReader parser)
		{
			var next = parser.Read<uint>();
			var prev = parser.Read<uint>();
			
			var position = parser.Read<POS>();
			var value = parser.Read<int>();

			//Next won't always be set to zero.
			//Some times this will be the index of the next value.

			if(prev != 0)
			{
				throw new Exception("Prev reference was set");
			}
			return new Waypoint(next, prev, position, value);
		}
	}

	public sealed class TRACKCHANGE
	{
		public uint x, y, z;
		public uint NormalPiece;
		public uint OtherPiece;
		public uint[] PieceMem;
	}

	public sealed class CRYSTAL
	{
		public const int ByteSize = 4 + BPOS.ByteSize;//36

		public BPOS Pos;
		public uint Flag;
	}

	public sealed class Map
	{
		public uint NumberOfPieces;
	
		public ushort NumberOfStrats;
		public ushort PolyListSize;

		public ushort MaxStrats;
		public ushort LevelFlag;
		//ushort EffectFlags;

		public uint MapXY;
		public uint MapX;
		public uint MapZ;
		public ushort? NumberOfDoors;
		public ushort? NumberOfOtherPieces;
		public Door[] DoorList;
		public uint NumberOfWP;
		public Waypoint[] WPList;
		public int[] Params;

		//uint DrawMode;

		public CRYSTAL[] CrystalList;
		public MapStrategy[] Strats;
		public IntPtr not_used1;
		public IntPtr not_used2;
		public POS[] Positions;
		public MAPINDEX[][] Grid;
		public MAPINDEX[] IndexGrid;
		public ZONE[] ZoneData;
		public uint[] Pieces;
		public TRACKCHANGE[] TrackChangeData;
		public CVECTOR[][] LightTables;

		//OBJECT *Background;
		//uint BackgroundAddYRotation;
		//int BackgroundHeightAdjust;

		public MATRIX wlm;
		public MATRIX lcm;

		private Map(){}

		public static Map Parse(WadReader data_in, WadFlag wadFlag)
		{
			var map = new Map();

			var mapParamCount = data_in.ReadInt32();
			map.Params = data_in.ReadInt32Array(mapParamCount);

			map.NumberOfPieces = data_in.ReadUInt32();
			map.NumberOfStrats = data_in.ReadUInt16();

			//TODO: What are these values
			if(data_in.ReadVersion != CROC_2_DEMO_PS1_DUMMY.Instance)
			{
				//Guessing here
				map.PolyListSize = data_in.ReadUInt16();
				map.MaxStrats = data_in.ReadUInt16();
				map.LevelFlag = data_in.ReadUInt16();
			}
			else
			{
				//TODO: Which field is this?
				var unknown = data_in.ReadUInt16();
			}

			map.MapXY = data_in.ReadUInt32();
			map.MapX = data_in.ReadUInt32();
			map.MapZ = data_in.ReadUInt32();

			ushort? n_lighting_headers;
			ushort? n_add_sub_chunks_lighting;
			int? idk3;
			if(data_in.ReadVersion != CROC_2_DEMO_PS1_DUMMY.Instance)
			{
				n_lighting_headers = data_in.ReadUInt16();
				n_add_sub_chunks_lighting = data_in.ReadUInt16();
				idk3 = data_in.ReadInt32();
			}
			else
			{
				n_lighting_headers = null;
				n_add_sub_chunks_lighting = null;
				idk3 = null;
			}

			//Not sure about these fields
			map.NumberOfDoors = n_lighting_headers;
			map.NumberOfOtherPieces = n_add_sub_chunks_lighting;
			if(idk3.HasValue && idk3.Value != 0)
			{
				throw new Exception("DoorList reference set");
			}
			map.NumberOfWP = data_in.ReadUInt32();//n_idk4

			if(data_in.ReadVersion != CROC_2_DEMO_PS1_DUMMY.Instance)
			{
				//Skips the rest of the map
				data_in.Seek(116, SeekOrigin.Current);
			}
			else
			{
				data_in.Seek(80, SeekOrigin.Current);
			}

			return map;
		}
	}
}
