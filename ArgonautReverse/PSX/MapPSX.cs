using System.Text;
using ArgonautReverse.Engine.Versions;
using ArgonautReverse.IO;
using ArgonautReverse.PC;
using ArgonautReverse.PSX.LibGTE;
using ArgonautReverse.Universal;
using ArgonautReverse.WadChunks.PSX;

namespace ArgonautReverse.PSX
{
	public sealed class CameraPointPSX//:IReadable<CameraPointPSX>,IWritable
	{
		//public CameraPointPSX? Prev;
		//public CameraPointPSX? Next;public bool HasNext;
		public Fixed32 X;
		public Fixed32 Z;
		public ushort Type;
		public ushort Angle;
		public Fixed32 Y;
		//public int __padding;

		//public static CameraPointPSX Parse(WadReader reader)
		//{
		//	var cameraPoint = new CameraPointPSX();
		//	reader.AssertRead<uint>(0);//Prev placeholder
		//	cameraPoint.HasNext = reader.Read<uint>() switch
		//	{
		//		0 => false,
		//		1 => true,
		//		_ => throw new Exception("Invalid value"),
		//	};
		//	cameraPoint.X = reader.Read<Fixed32>();
		//	cameraPoint.Z = reader.Read<Fixed32>();
		//	cameraPoint.Y = reader.Read<Fixed32>();
		//	cameraPoint.Type = reader.Read<ushort>();
		//	cameraPoint.Angle = reader.Read<ushort>();
		//	reader.AssertRead<uint>(0);//Padding
		//	return cameraPoint;
		//}

		//public void Write(WadWriter writer)
		//{
		//	writer.Write<uint>(0);//Prev placeholder
		//	writer.Write<uint>((Next != null) ? 1u : 0u);//Next placeholder
		//	writer.Write<Fixed32>(X);
		//	writer.Write<Fixed32>(Z);
		//	writer.Write<Fixed32>(Y);
		//	writer.Write<ushort>(Type);
		//	writer.Write<ushort>(Angle);
		//	writer.Write<uint>(0);//Padding
		//}
	}

	public sealed class CameraLoopPSX(CameraPointPSX[] points)
	{
		//public ushort PointCount;
		//public readonly CameraPointFlags Flags = flags;
		public CameraPointPSX[] Points = points;
	}

	public sealed class CameraEntryPSX(CameraLoopPSX[] loops)
	{
		//public uint LoopCount;
		public readonly CameraLoopPSX[] Loops = loops;
	}

	public sealed class MapIndexPSX
	{
		public const int ByteSize = 8;

		public uint Index{get;private set;}
		public int NextByteOffset{get;private set;}
		public MapIndexPSX? Next{get;private set;}

		private MapIndexPSX(){}

		public static IReadOnlyList<MapIndexPSX> ParseIndexGrid(int pieceCount, WadReader reader)
		{
			var mapIndexGrid = new MapIndexPSX[pieceCount];
			//Create References
			for(int i=0; i<pieceCount; i++)
			{
				mapIndexGrid[i] = new MapIndexPSX();
			}

			//Parse data
			for(int i=0; i<pieceCount; i++)
			{
				mapIndexGrid[i].Index = reader.Read<uint>();
				mapIndexGrid[i].NextByteOffset = reader.Read<int>();
				if(mapIndexGrid[i].NextByteOffset == -1)
				{
					mapIndexGrid[i].Next = null;
				}
				else
				{
					Utils.Assert(mapIndexGrid[i].NextByteOffset % ByteSize == 0);
					mapIndexGrid[i].Next = mapIndexGrid[mapIndexGrid[i].NextByteOffset / ByteSize];
				}
			}

			return mapIndexGrid;
		}

		public static void WriteIndexGrid(WadWriter writer, IReadOnlyList<MapIndexPSX> mapIndexGrid)
		{
			//Parse data
			for(int i=0; i<mapIndexGrid.Count; i++)
			{
				writer.Write<uint>(mapIndexGrid[i].Index);
				writer.Write<int>(mapIndexGrid[i].NextByteOffset);
				//TODO: Compute NexByteOffset from NExt ref when needed.
			}
		}
	}

	public sealed class WaypointPSX:IReadable<WaypointPSX>,IWritable
	{
		public const int ByteSize = 12 + POS.ByteSize;//36

		public WaypointPSX Next{get;private set;}
		public int NextRawValue;//HasNext?

		public WaypointPSX Prev{get;private set;}
		public int PrevRawValue;

		public POS Position{get;private set;}
		public int Value{get;private set;}

		private WaypointPSX(){}

		public static WaypointPSX Parse(WadReader reader)
		{
			var waypoint = new WaypointPSX();
			waypoint.NextRawValue = reader.Read<int>();
			reader.AssertRead<uint>(0);//waypoint.PrevRawValue
			waypoint.Position = POS.ParseWithImportantPadding(reader);//TODO: Seems to have values in HP1.
			waypoint.Value = reader.Read<int>();
			return waypoint;
		}

		void IWritable.Write(WadWriter writer)
		{
			writer.Write<int>(NextRawValue);
			writer.Write<uint>(0);//waypoint.PrevRawValue
			POS.WriteWithImportantPadding(writer, Position);//TODO: Seems to have values in HP1.
			writer.Write<int>(Value);
		}

		public static IReadOnlyList<WaypointPSX> ParseWaypointList(WadReader parser, int waypointCount)
		{
			var waypoints = parser.ReadArray<WaypointPSX>(waypointCount);

			for(int i = 0; i < waypointCount; i++)
			{
				var waypoint = waypoints[i];

				//Sometimes this will be the index of the next value, but not always
				if(waypoint.NextRawValue == 0)
				{
					waypoint.Next = null;
				}
				else
				{
					waypoint.Next = waypoints[i + 1];
					waypoint.Next.Prev = waypoint;
				}
			}
			return waypoints;
		}

		public static void WriteWaypointList(WadWriter writer, IReadOnlyList<WaypointPSX> waypoints)
		{
			//TODO: Setup NextRawValue if unset.
			writer.WriteArray<WaypointPSX>(waypoints);
		}
	}

	public sealed class TrackChangePSX:IReadable<TrackChangePSX>,IWritable
	{
		public readonly uint X, Y, Z;
		public readonly uint NormalPiece;
		public readonly uint OtherPiece;

		public readonly int PieceMemIndex;//This is the index of the mem in the Piece array
		//public readonly uint* PieceMem;

		private TrackChangePSX(uint x, uint y, uint z, uint normalPiece, uint otherPiece, int pieceMemIndex)
		{
			X = x;
			Y = y;
			Z = z;
			NormalPiece = normalPiece;
			OtherPiece = otherPiece;
			PieceMemIndex = pieceMemIndex;
		}

		public static TrackChangePSX Parse(WadReader reader)
		{
			var x = reader.Read<uint>();
			var y = reader.Read<uint>();
			var z = reader.Read<uint>();
			var normalPiece = reader.Read<uint>();
			var otherPiece = reader.Read<uint>();

			var pieceMemIndex = reader.Read<int>();

			return new TrackChangePSX(x, y, z, normalPiece, otherPiece, pieceMemIndex);
		}

		void IWritable.Write(WadWriter writer)
		{
			writer.Write<uint>(X);
			writer.Write<uint>(Y);
			writer.Write<uint>(Z);
			writer.Write<uint>(NormalPiece);
			writer.Write<uint>(OtherPiece);

			writer.Write<int>(PieceMemIndex);
		}
	}

	public sealed class CrystalPSX:IReadable<CrystalPSX>,IWritable
	{
		public const int ByteSize = 4 + BPOS.ByteSize;//36

		public readonly BPOS Pos;
		public readonly uint Flags;

		private CrystalPSX(BPOS pos, uint flags)
		{
			Pos = pos;
			Flags = flags;
		}

		public static CrystalPSX Parse(WadReader reader)
		{
			var pos = reader.Read<BPOS>();
			var flags = reader.Read<uint>();
			return new CrystalPSX(pos, flags);
		}

		void IWritable.Write(WadWriter writer)
		{
			writer.Write<BPOS>(Pos);
			writer.Write<uint>(Flags);
		}
	}

	public readonly struct LightTuplePSX:IReadable<LightTuplePSX>,IWritable
	{
		public readonly short LightA, LightB;

		private LightTuplePSX(short lightA, short lightB)
		{
			LightA = lightA;
			LightB = lightB;
		}

		public static LightTuplePSX Parse(WadReader reader)
		{
			if(reader.ReadVersion == HARRY_POTTER_1_PS1.WadVersion || reader.ReadVersion == HARRY_POTTER_2_PS1.WadVersion)
			{
				//TODO: LightTuple. Taking a guess on this. The full size is 16 bit in HP.
				var lightA = reader.Read<byte>();
				var lightB = reader.Read<byte>();
				return new LightTuplePSX(lightA, lightB);
			}
			else
			{
				var lightA = reader.Read<short>();
				var lightB = reader.Read<short>();
				return new LightTuplePSX(lightA, lightB);
			}
		}

		void IWritable.Write(WadWriter writer)
		{
			if(writer.WriteVersion == HARRY_POTTER_1_PS1.WadVersion || writer.WriteVersion == HARRY_POTTER_2_PS1.WadVersion)
			{
				//TODO: LightTuple. Taking a guess on this. The full size is 16 bit in HP.
				writer.Write<byte>((byte)LightA);
				writer.Write<byte>((byte)LightB);
			}
			else
			{
				writer.Write<short>(LightA);
				writer.Write<short>(LightB);
			}
		}
	}

	public sealed class MapPSX
	{
		public int NumberOfPieces;

		public ushort NumberOfStrats;
		public ushort PolyListSize;

		public ushort MaxStrats;
		public ushort LevelFlag;
		//ushort EffectFlags;

		public int MapXY;
		public int MapX;
		public int MapZ;
		public ushort NumberOfDoors;
		public ushort? NumberOfOtherPieces;
		public IReadOnlyList<DoorPSX> DoorList;
		public uint NumberOfWP;
		public IReadOnlyList<WaypointPSX> WPList;
		public IReadOnlyList<int> Params;

		#region Pre Croc 2 Demo
		//Moved to Door
		public uint DrawMode;
		#endregion

		public IReadOnlyList<CrystalPSX> CrystalList;
		public IReadOnlyList<MapStrategyPSX> Strats;
		public nint not_used1;
		public nint not_used2;
		public IReadOnlyList<POS> Positions;
		public int[][] GridOffsets;
		public IReadOnlyList<IReadOnlyList<MapIndexPSX>> Grid;//Refs to values in IndexGrid
		public IReadOnlyList<MapIndexPSX> IndexGrid;
		public IReadOnlyList<Zone>? ZoneData;
		public IReadOnlyList<uint> Pieces;
		public IReadOnlyList<TrackChangePSX> TrackChangeData;
		public int[] LightTableIndices;
		public CVECTOR[][] LightTables;

		#region Pre Croc 2 Demo
		//Moved to Door
		public int BackgroundAddr;//Offset to Background
		public ObjectPSX Background;
		public uint BackgroundAddYRotation;
		public int BackgroundHeightAdjust;
		#endregion

		public MATRIX wlm;
		public MATRIX lcm;

		#region Non-map fields
		//Values not part of the map struct
		public IReadOnlyList<ZoneInfo> ZoneTable;

		public LightTuplePSX[]? LightTuples;
		public IReadOnlyList<OmniLightPSX> omniLights;
		public IReadOnlyList<IReadOnlyList<PreLitPSX>> pre_map;

		public int MaxCameraHeight;
		public CameraEntryPSX[] CameraPointTable;
		public SimpleImagePSX[] InvAddresses;
		public string[][] LanguageBase;

		//Didn't have fields originally
		public int SequenceCount;
		public byte[] RawSequenceData;
		public IReadOnlyList<ALAmbiencePSX>? Ambiences;
		#endregion

		private MapPSX() { }

		private static MapPSX ParseStruct(WadReader reader)
		{
			var map = new MapPSX();

			map.NumberOfPieces = reader.Read<int>();
			map.NumberOfStrats = reader.Read<ushort>();

			ushort? unknownField = null;

			//TODO: What are these values
			if(reader.DatVersion != CROC_2_DEMO_PS1_DUMMY.DatVersion)
			{
				//Guessing here
				map.PolyListSize = reader.Read<ushort>();
				map.MaxStrats = reader.Read<ushort>();
				map.LevelFlag = reader.Read<ushort>();
			}
			else
			{
				//TODO: Which field is this? Or is this just padding?
				unknownField = reader.Read<ushort>();
				if(unknownField != 0)
				{
					throw new NotImplementedException();
				}
			}

			map.MapXY = reader.Read<int>();
			map.MapX = reader.Read<int>();
			map.MapZ = reader.Read<int>();
			Utils.Assert(map.MapXY == map.MapX * map.MapZ);

			ushort? n_lighting_headers;
			ushort? n_add_sub_chunks_lighting;
			int? doorListPlaceholder;
			if(reader.DatVersion != CROC_2_DEMO_PS1_DUMMY.DatVersion)
			{
				n_lighting_headers = reader.Read<ushort>();
				n_add_sub_chunks_lighting = reader.Read<ushort>();
				doorListPlaceholder = reader.Read<int>();
			}
			else
			{
				n_lighting_headers = null;
				n_add_sub_chunks_lighting = null;
				doorListPlaceholder = null;
			}

			//Not sure about these fields
			map.NumberOfDoors = n_lighting_headers ?? 0;
			map.NumberOfOtherPieces = n_add_sub_chunks_lighting;
			if(doorListPlaceholder.HasValue && doorListPlaceholder.Value != 0)
			{
				throw new Exception("DoorList reference set");
			}
			map.NumberOfWP = reader.Read<uint>();

			reader.AssertRead<uint>(0);//WPList placeholder
			reader.AssertRead<uint>(0);//Params placeholder
			if(reader.DatVersion != CROC_2_DEMO_PS1_DUMMY.DatVersion)
			{
				//uint DrawMode;
				reader.AssertRead<uint>(0);//CrystalList placeholder

				reader.AssertRead<uint>(0);//Strats placeholder

				reader.AssertRead<uint>(0, warn:true/*HP1*/);//not_used1 placeholder
				reader.AssertRead<uint>(0);//not_used2 placeholder

				reader.AssertRead<uint>(0);//Positions placeholder

				reader.AssertRead<uint>(0);//Grid placeholder
				reader.AssertRead<uint>(0);//IndexGrid placeholder

				reader.AssertRead<uint>(0);//ZoneData placeholder

				reader.AssertRead<uint>(0);//Pieces placeholder

				reader.AssertRead<uint>(0);//TrackChangeData placeholder
				reader.AssertRead<uint>(0);//LightTables placeholder

				//OBJECT *Background;
				//uint BackgroundAddYRotation;
				//int BackgroundHeightAdjust;

				map.wlm = reader.Read<MATRIX>();
				map.lcm = reader.Read<MATRIX>();
			}
			else
			{
				map.DrawMode = reader.Read<uint>();

				var placeholders1 = reader.ReadArray<int>(7);
				//These are likely placeholder fields
				for(int i = 0; i < 7; i++)
				{
					Utils.Assert(placeholders1[i] == 0);
					//reader.AssertRead<uint>(0);
				}

				map.BackgroundAddr = reader.Read<int>();
				map.BackgroundAddYRotation = reader.Read<uint>();
				map.BackgroundHeightAdjust = reader.Read<int>();

				var placeholders2 = reader.ReadArray<int>(7);
				//These are likely placeholder fields
				for(int i = 0; i < 7; i++)
				{
					Utils.Assert(placeholders2[i] == 0);
					//reader.AssertRead<uint>(0);
				}
			}

			return map;
		}

		private void WriteStruct(WadWriter writer)
		{
			writer.Write<int>(NumberOfPieces);
			writer.Write<ushort>(NumberOfStrats);

			ushort? unknownField = null;

			//TODO: What are these values
			if(writer.DatVersion != CROC_2_DEMO_PS1_DUMMY.DatVersion)
			{
				//Guessing here
				writer.Write<ushort>(PolyListSize);
				writer.Write<ushort>(MaxStrats);
				writer.Write<ushort>(LevelFlag);
			}
			else
			{
				//TODO: Which field is this? Or is this just padding?
				writer.Write<ushort>(/*unknownField*/0);
			}

			Utils.Assert(MapXY == MapX * MapZ);
			writer.Write<int>(MapXY);
			writer.Write<int>(MapX);
			writer.Write<int>(MapZ);

			if(writer.DatVersion != CROC_2_DEMO_PS1_DUMMY.DatVersion)
			{
				//Not sure about these fields
				writer.Write<ushort>(NumberOfDoors);
				writer.Write<ushort>(NumberOfOtherPieces!.Value);
				writer.Write<int>(0);//doorList placeholder
			}
			writer.Write<uint>(NumberOfWP);

			writer.Write<uint>(0);//WPList placeholder
			writer.Write<uint>(0);//Params placeholder
			if(writer.DatVersion != CROC_2_DEMO_PS1_DUMMY.DatVersion)
			{
				//uint DrawMode;
				writer.Write<uint>(0);//CrystalList placeholder

				writer.Write<uint>(0);//Strats placeholder

				//TODO: This field can be non-zero in HP1
				writer.Write<uint>(0);//not_used1 placeholder

				writer.Write<uint>(0);//not_used2 placeholder

				writer.Write<uint>(0);//Positions placeholder

				writer.Write<uint>(0);//Grid placeholder
				writer.Write<uint>(0);//IndexGrid placeholder

				writer.Write<uint>(0);//ZoneData placeholder

				writer.Write<uint>(0);//Pieces placeholder

				writer.Write<uint>(0);//TrackChangeData placeholder
				writer.Write<uint>(0);//LightTables placeholder

				//OBJECT *Background;
				//uint BackgroundAddYRotation;
				//int BackgroundHeightAdjust;

				writer.Write<MATRIX>(wlm);
				writer.Write<MATRIX>(lcm);
			}
			else
			{
				writer.Write<uint>(DrawMode);
				
				//These are likely placeholder fields
				writer.WriteEmptyArray<int>(7);

				writer.Write<int>(BackgroundAddr);
				writer.Write<uint>(BackgroundAddYRotation);
				writer.Write<int>(BackgroundHeightAdjust);

				//These are likely placeholder fields
				writer.WriteEmptyArray<int>(7);
			}
		}

		public static MapPSX Parse(WadReader data_in, WadFlagPSX wadFlag, TObjectPSX[] trackObjects)
		{
			//Params are stored before the map
			var mapParamCount = data_in.Read<int>();
			var mapParams = data_in.ReadArray<int>(mapParamCount);

			var map = ParseStruct(data_in);
			map.Params = mapParams;

			map.GridOffsets = new int[map.MapZ][];
			for(int z = 0; z < map.MapZ; z++)
			{
				map.GridOffsets[z] = data_in.ReadArray<int>(map.MapX);
			}

			map.IndexGrid = MapIndexPSX.ParseIndexGrid(map.NumberOfPieces, data_in);

			var mapGrid = new MapIndexPSX[map.MapXY][];
			for(int z = 0; z < map.MapZ; z++)
			{
				mapGrid[z] = new MapIndexPSX[map.MapX];
				for(int x = 0; x < map.MapX; x++)
				{
					var offset = map.GridOffsets[z][x];
					if(offset != -1)
					{
						Utils.Assert(offset % MapIndexPSX.ByteSize == 0);
						mapGrid[z][x] = map.IndexGrid[offset / MapIndexPSX.ByteSize];
					}
				}
			}
			map.Grid = mapGrid;

			//Taking a guess on this one
			//Not certain if WF_USESZONES can still be relied on
			//bool newZones = (wadFlag & WadFlagPSX.WF_NEWZONES) != 0;
			//bool useZones = (wadFlag & WadFlagPSX.WF_USESZONES) != 0;
			//if(newZones != useZones || newZones == (data_in.DatVersion == CROC_2_DEMO_PS1_DUMMY.DatVersion))
			//{
			//	//HP1 has NEWZONES without USESZONES
			//	throw new Exception("Not certain if WF_USESZONES can still be relied on");
			//}
			if((wadFlag & WadFlagPSX.WF_NEWZONES) != 0)
			{
				map.ZoneTable = data_in.ReadArray<ZoneInfo>(32);

				var zoneCount = data_in.Read<int>();
				map.ZoneData = data_in.ReadArray<Zone>(zoneCount);
				Utils.Assert(zoneCount == map.MapXY);
			}
			else if((wadFlag & WadFlagPSX.WF_USESZONES) != 0)
			{
				throw new NotImplementedException("Code for this exists");
			}
			else
			{
				map.ZoneData = null;
			}

			if(data_in.Read<uint>() == 0x00777666)
			{
				//Croc 2 PSX release doesn't have code for this.
				//TODO: Check other games. Ideally find out how to determine programmatically.

				map.LightTuples = data_in.ReadArray<LightTuplePSX>(map.MapXY);
			}
			else
			{
				map.LightTuples = null;
				data_in.SkipBytes(-4);//Undo the read string
			}

			map.Positions = data_in.ReadArray<POS>(map.NumberOfPieces);

			map.Pieces = data_in.ReadArray<uint>(map.NumberOfPieces);

			if(map.NumberOfDoors != 0)
			{
				map.DoorList = data_in.ReadArray<DoorPSX>(map.NumberOfDoors);

				//TODO: Assign Background for each door
				//foreach(var door in map.DoorList)
				//{
				//	door.Background = ;
				//}
			}

			map.WPList = WaypointPSX.ParseWaypointList(data_in, (int)map.NumberOfWP);

			var strats = new MapStrategyPSX[map.NumberOfStrats];
			for(int i = 0; i < map.NumberOfStrats; i++)
			{
				strats[i] = MapStrategyPSX.Parse(data_in, map);
			}
			map.Strats = strats;

			if((wadFlag & WadFlagPSX.WF_NOMATPOS) != 0)
			{
				if(data_in.DatVersion == CROC_2_DEMO_PS1_DUMMY.DatVersion)
				{
					//Getting here means that DUMMY definitely supports the WF_NOMATPOS flag.
					throw new NotImplementedException("This confirmed the DUMMY version supports WF_NOMATPOS. Document this and remove this exception.");
				}
			}
			else
			{
				if(data_in.DatVersion != CROC_2_DEMO_PS1_DUMMY.DatVersion)
				{
					throw new Exception("Unsure if DUMMY version supports this flag");
				}
				//TODO: Data can exist but is not used by Croc 2 Demo and Aladdin.
				//Is this data used by the dummy?
				var matpos = data_in.ReadArray<MATRIX>(map.NumberOfPieces);
				var matdraw = data_in.ReadArray<MATRIX>(map.NumberOfPieces);
			}

			if(data_in.DatVersion == CROC_2_DEMO_PS1_DUMMY.DatVersion)
			{
				//TODO: Unknown data
				var unknownData = data_in.ReadArray<int>(20);
			}

			ParseCameraPoints(map, data_in, wadFlag);

			if((wadFlag & WadFlagPSX.WF_HASOTHERPIECES) != 0)
			{
				if(data_in.DatVersion == CROC_2_DEMO_PS1_DUMMY.DatVersion)
				{
					throw new Exception("Unsure if DUMMY version supports this");
				}

				map.TrackChangeData = data_in.ReadArray<TrackChangePSX>(map.NumberOfOtherPieces!.Value);
			}

			if(data_in.DatVersion != CROC_2_DEMO_PS1_DUMMY.DatVersion)
			{
				var omniLightCount = data_in.Read<int>();

				if(data_in.DatVersion == CROC_2_DEMO_PS1.DatVersion && omniLightCount != 1)
				{
					//Old exporter skips 32 bytes for demo here.
					//Thinking it may be 1 int for count and 28 for a single OMNI_LIGHT
					throw new Exception("Hypothesis is wrong");
				}

				map.omniLights = data_in.ReadArray<OmniLightPSX>(omniLightCount);
			}

			if((wadFlag & WadFlagPSX.WF_MAP_LIGHTINGTABLE) != 0)
			{
				//TODO: WF_MAP_LIGHTINGTABLE

				//TODO: This is a lot to be skipping on the release version
				if(data_in.DatVersion == CROC_2_PS1.DatVersion)
				{
					//TODO: Why did this originally just skipped 30732 bytes? data_in.SkipBytes(30732);

					map.LightTableIndices = data_in.ReadArray<int>(map.NumberOfPieces);
					map.LightTables = new CVECTOR[map.NumberOfPieces][];
					for(int i=0; i<map.NumberOfPieces; i++)
					{
						if(map.LightTableIndices[i] == 0){continue;}

						map.LightTables[i] = data_in.ReadArray<CVECTOR>(trackObjects[map.Pieces[i]].nvert * map.LightTableIndices[i]);
					}
				}
				else if(data_in.DatVersion == CROC_2_DEMO_PS1_DUMMY.DatVersion)
				{
					throw new NotImplementedException();
				}
				else
				{
					if(map.NumberOfPieces != 0)
					{
						//TODO: What games use this? This appears to be in the wrong order compared to any of my versions.

						var sub_chunks_n_lighting = data_in.ReadArray<uint>(map.NumberOfPieces);
						var sub_chunks_n_add_lighting = data_in.ReadArray<uint>(map.NumberOfOtherPieces!.Value);


						for(int model_id = 0; model_id < map.NumberOfPieces; model_id++)
						{
							for(int i = 0; i < sub_chunks_n_lighting[model_id]; i++)
							{
								var size = 4 * trackObjects[map.Pieces[model_id]].nvert;
								data_in.SkipBytes(size);
							}
						}
						for(int model_id = 0; model_id < map.NumberOfOtherPieces.Value; model_id++)
						{
							for(int i = 0; i < sub_chunks_n_add_lighting[model_id]; i++)
							{
								var size = 4 * trackObjects[map.TrackChangeData[model_id].OtherPiece].nvert;
								data_in.SkipBytes(size);
							}
						}
						if(data_in.DatVersion != CROC_2_DEMO_PS1.DatVersion)// Not present in Croc 2 Demo Dummy
						{
							var idk_size = data_in.Read<int>();
							if(idk_size != 0)
							{
								data_in.SkipBytes(4 + idk_size);
							}
							else
							{
								data_in.SkipBytes(-4);
							}
							var n_idk3 = data_in.Read<int>();
							if(n_idk3 == 0)
							{
								data_in.SkipBytes(-4);
							}
							var idk4 = new byte[n_idk3][];//var idk3 = [int.from_bytes(data_in.read(40), "little") for _ in range(n_idk3)]
							for(int i=0; i<n_idk3; i++)
							{
								idk4[i] = data_in.ReadArray<byte>(40);
							}
						}
						data_in.SkipBytes(12);
					}
				}
			}

			if((wadFlag & WadFlagPSX.WF_MAP_PRELIT) != 0)
			{
				//Some early dummy wads fail on this but other ones are fine. Some later dummy wads fail while most are fine.
				try
				{
					var preMap = new PreLitPSX[2][];
					for(int i = 0; i < preMap.Length; i++)
					{
						preMap[i] = new PreLitPSX[map.NumberOfPieces];

						for(int j = 0; j < map.NumberOfPieces; j++)
						{
							var lfacePlaceholder = data_in.Read<uint>();
							if(lfacePlaceholder != 0)
							{
								throw new Exception();
							}
							preMap[i][j] = new PreLitPSX();
						}

						for(int j = 0; j < map.NumberOfPieces; j++)
						{
							var trackObj = trackObjects[map.Pieces[j]];
							preMap[i][j].lface = data_in.ReadArray<PolyAllPSX>(trackObj.nface);
						}
					}
					map.pre_map = preMap;
				}
				catch(Exception e)
				{
					Console.WriteLine("ERROR: Failed to read prelit data.");
					Console.WriteLine(e.Message);
				}
			}
			else
			{
				//TODO: What is this and where does it go? (DUMMY related)
				if(data_in.DatVersion == CROC_2_DEMO_PS1_DUMMY.DatVersion)
				{
					//This always appears at the end.
					var unknownData = data_in.ReadArray<int>(3);
				}
			}

			var soundDataFlags = data_in.WadFile.GetChunk(SPSXChunkInfo.Instance)?.spsx_flags ?? 0;
			if((soundDataFlags&SPSXFlagsPSX.HasAmbient)!=0)
			{
				if(data_in.Remaining == 0)
				{
					//TODO: Found in Harry Potter 1.
					Console.WriteLine("WARNING: Attemping to read ambient data past the end of the DPSX chunk.");
				}
				else if(data_in.DatVersion == CROC_2_DEMO_PS1_DUMMY.DatVersion)
				{
					//TODO: No data remaining
				}
				else
				{
					var totalSequenceByteLength = data_in.Read<int>();
					map.SequenceCount = 0;
					if((soundDataFlags&SPSXFlagsPSX.AmbientSeparate)!=0)
					{
						map.SequenceCount = data_in.Read<int>();
					}
					map.RawSequenceData = data_in.ReadArray<byte>(totalSequenceByteLength);
					//TODO: Parse sequence data

					IReadOnlyList<ALAmbiencePSX>? ambience = null;
					if((soundDataFlags&SPSXFlagsPSX.AmbientSeparate)!=0)
					{
						if((wadFlag&WadFlagPSX.WF_HASMULTIAMBIENT)!=0)
						{
							var ambienceCount = data_in.Read<int>();
							ambience = data_in.ReadArray<ALAmbiencePSX>(ambienceCount);
						}
					}
					else
					{
						if((wadFlag&WadFlagPSX.WF_HASMULTIAMBIENT)!=0)
						{
							var ambienceCount = data_in.Read<int>();
							ambience = data_in.ReadArray<ALAmbiencePSX>(ambienceCount);
						}
					}
					map.Ambiences = ambience;
				}
			}

			if((wadFlag & WadFlagPSX.WF_HASINVENTORY) != 0)
			{
				int invCount;
				if((wadFlag & WadFlagPSX.WF_HASEXTRAINVENTORY) != 0)
				{
					invCount = 7;
				}
				else if((wadFlag & WadFlagPSX.WF_HASFULLSETOFICONS) != 0)
				{
					invCount = 10;
				}
				else
				{
					invCount = 6;
				}
				map.InvAddresses = data_in.ReadArray<SimpleImagePSX>(invCount);
			}

			if((wadFlag & WadFlagPSX.WF_HASLANGUAGES) != 0 && (data_in.ReadVersion != HARRY_POTTER_1_PS1.WadVersion))//Languages stored in LPSX chunk in HP.
			{
				if(data_in.ReadVersion != CROC_2_PS1.WadVersion)
				{
					throw new NotImplementedException("Language only implemented on Croc2PSX");
				}
				int lastLanguageIndex = data_in.Read<int>();
				int numberOfLanguages = lastLanguageIndex + 1;
				int phraseCount = data_in.Read<int>();
				int stringDataSize = data_in.Read<int>();
				var stringOffsets = new int[phraseCount][];
				map.LanguageBase = new string[phraseCount][];
				for(int p = 0; p < phraseCount; p++)
				{
					stringOffsets[p] = data_in.ReadArray<int>(numberOfLanguages);
					map.LanguageBase[p] = new string[numberOfLanguages];
				}
				var stringData = data_in.ReadArray<sbyte>(4*(stringDataSize+1));//TODO:T3L1M001 needed +1. They forgot to count the null terminator?
				unsafe
				{
					fixed(sbyte* stringData0 = stringData)
					{
						for(int p = 0; p < phraseCount; p++)
						{
							for(int l = 0; l < numberOfLanguages; l++)
							{
								map.LanguageBase[p][l] = new string(&stringData0[stringOffsets[p][l]]);// Values are offsets from the LanguageBase
							}
						}
					}
				}
			}
			return map;
		}

		public void Write(WadWriter writer, WadFlagPSX wadFlag, TObjectPSX[] trackObjects)
		{
			//Params are stored before the map
			writer.Write<int>(Params.Count);
			writer.WriteArray<int>(Params);

			WriteStruct(writer);

			for(int z = 0; z < MapZ; z++)
			{
				writer.WriteSizedArray<int>(MapX, GridOffsets[z]);
			}

			MapIndexPSX.WriteIndexGrid(writer, IndexGrid);

			//Taking a guess on this one
			//Not certain if WF_USESZONES can still be relied on
			//bool newZones = (wadFlag & WadFlagPSX.WF_NEWZONES) != 0;
			//bool useZones = (wadFlag & WadFlagPSX.WF_USESZONES) != 0;
			//if(newZones != useZones || newZones == (data_in.DatVersion == CROC_2_DEMO_PS1_DUMMY.DatVersion))
			//{
			//	//HP1 has NEWZONES with out USESZONES
			//	throw new Exception("Not certain if WF_USESZONES can still be relied on");
			//}
			if((wadFlag & WadFlagPSX.WF_NEWZONES) != 0)
			{
				writer.WriteSizedArray<ZoneInfo>(32, ZoneTable);

				Utils.Assert(ZoneData!.Count == MapXY);
				writer.Write<int>(ZoneData.Count);
				writer.WriteArray<Zone>(ZoneData);
			}
			else if((wadFlag & WadFlagPSX.WF_USESZONES) != 0)
			{
				throw new NotImplementedException("Code for this exists");
			}

			if(LightTuples != null)
			{
				//Croc 2 PSX release doesn't have code for this.
				//TODO: Check other games. Ideally find out how to determine programmatically.
				writer.Write<uint>(0x00777666);
				writer.WriteSizedArray<LightTuplePSX>(MapXY, LightTuples);
			}

			writer.WriteSizedArray<POS>(NumberOfPieces, Positions);

			writer.WriteSizedArray<uint>(NumberOfPieces, Pieces);

			if(NumberOfDoors != 0)
			{
				writer.WriteSizedArray<DoorPSX>(NumberOfDoors, DoorList);

				//TODO: Assign BackgroundAddr from Background for each door
			}

			WaypointPSX.WriteWaypointList(writer, WPList);

			for(int i = 0; i < NumberOfStrats; i++)
			{
				Strats[i].Write(writer);
			}

			if((wadFlag & WadFlagPSX.WF_NOMATPOS) != 0)
			{
				if(writer.DatVersion == CROC_2_DEMO_PS1_DUMMY.DatVersion)
				{
					//Getting here means that DUMMY definitely supports the WF_NOMATPOS flag.
					throw new NotImplementedException("This confirmed the DUMMY version supports WF_NOMATPOS. Document this and remove this exception.");
				}
			}
			else
			{
				if(writer.DatVersion != CROC_2_DEMO_PS1_DUMMY.DatVersion)
				{
					throw new Exception("Unsure if DUMMY version supports this flag");
				}
				//TODO: Data can exist but is not used by Croc 2 Demo and Aladdin.
				//Is this data used by the dummy?
				throw new NotImplementedException();
				//writer.WriteSizedArray<MATRIX>(NumberOfPieces, matpos);
				//writer.WriteSizedArray<MATRIX>(NumberOfPieces, matdraw);
			}

			if(writer.DatVersion == CROC_2_DEMO_PS1_DUMMY.DatVersion)
			{
				//TODO: Unknown data
				//writer.WriteSizedArray<int>(20, unknownData);
				throw new NotImplementedException();
			}

			WriteCameraPoints(writer, wadFlag);

			if((wadFlag & WadFlagPSX.WF_HASOTHERPIECES) != 0)
			{
				if(writer.DatVersion == CROC_2_DEMO_PS1_DUMMY.DatVersion)
				{
					throw new Exception("Unsure if DUMMY version supports this");
				}

				writer.WriteSizedArray<TrackChangePSX>(NumberOfOtherPieces!.Value, TrackChangeData);
			}

			if(writer.DatVersion != CROC_2_DEMO_PS1_DUMMY.DatVersion)
			{
				writer.Write<int>(omniLights.Count);
				writer.WriteArray<OmniLightPSX>(omniLights);
			}

			if((wadFlag & WadFlagPSX.WF_MAP_LIGHTINGTABLE) != 0)
			{
				if(writer.DatVersion == CROC_2_PS1.DatVersion)
				{
					//TODO: Calculate LightTableIndices.
					writer.WriteSizedArray<int>(NumberOfPieces, LightTableIndices);
					for(int i=0; i<NumberOfPieces; i++)
					{
						if(LightTableIndices[i] == 0){continue;}

						writer.WriteSizedArray<CVECTOR>(trackObjects[Pieces[i]].nvert * LightTableIndices[i], LightTables[i]);
					}
				}
				else if(writer.DatVersion == CROC_2_DEMO_PS1_DUMMY.DatVersion)
				{
					throw new NotImplementedException();
				}
				else
				{
					throw new NotImplementedException();
				}
			}

			if((wadFlag & WadFlagPSX.WF_MAP_PRELIT) != 0)
			{
				for(int i = 0; i < pre_map.Count; i++)
				{
					writer.WriteEmptyArray<uint>(NumberOfPieces);//lface placeholders

					for(int j = 0; j < NumberOfPieces; j++)
					{
						writer.WriteSizedArray<PolyAllPSX>(trackObjects[Pieces[j]].nface, pre_map[i][j].lface);
					}
				}
			}
			else
			{
				//TODO: What is this and where does it go? (DUMMY related)
				if(writer.DatVersion == CROC_2_DEMO_PS1_DUMMY.DatVersion)
				{
					//This always appears at the end.
					//writer.WriteSizedArray<int>(3, unknownData);
					throw new NotImplementedException();
				}
			}

			var soundDataFlags = writer.WadFile.GetChunk(SPSXChunkInfo.Instance)?.spsx_flags ?? 0;
			if((soundDataFlags&SPSXFlagsPSX.HasAmbient)!=0 && (writer.WriteVersion != HARRY_POTTER_1_PS1.WadVersion))//Not sure if all HP1 wads skip it here
			{
				if(writer.DatVersion == CROC_2_DEMO_PS1_DUMMY.DatVersion)
				{
					//TODO: No data remaining
				}
				else
				{
					writer.Write<int>(RawSequenceData.Length);
					if((soundDataFlags&SPSXFlagsPSX.AmbientSeparate)!=0)
					{
						writer.Write<int>(SequenceCount);
					}
					writer.WriteArray<byte>(RawSequenceData);

					if((soundDataFlags&SPSXFlagsPSX.AmbientSeparate)!=0)
					{
						if((wadFlag&WadFlagPSX.WF_HASMULTIAMBIENT)!=0)
						{
							writer.Write<int>(Ambiences!.Count);
							writer.WriteArray<ALAmbiencePSX>(Ambiences);
						}
					}
					else
					{
						if((wadFlag&WadFlagPSX.WF_HASMULTIAMBIENT)!=0)
						{
							writer.Write<int>(Ambiences!.Count);
							writer.WriteArray<ALAmbiencePSX>(Ambiences);
						}
					}
				}
			}

			if((wadFlag & WadFlagPSX.WF_HASINVENTORY) != 0)
			{
				int invCount;
				if((wadFlag & WadFlagPSX.WF_HASEXTRAINVENTORY) != 0)
				{
					invCount = 7;
				}
				else if((wadFlag & WadFlagPSX.WF_HASFULLSETOFICONS) != 0)
				{
					invCount = 10;
				}
				else
				{
					invCount = 6;
				}
				writer.WriteSizedArray<SimpleImagePSX>(invCount, InvAddresses);
			}

			if((wadFlag & WadFlagPSX.WF_HASLANGUAGES) != 0 && (writer.WriteVersion != HARRY_POTTER_1_PS1.WadVersion))//Languages stored in LPSX chunk in HP.
			{
				if(writer.WriteVersion != CROC_2_PS1.WadVersion)
				{
					throw new NotImplementedException("Language only implemented on Croc2PSX");
				}
				
				int phraseCount = LanguageBase.Length;
				int languageCount = LanguageBase[0].Length;

				int stringDataSize = 0;
				var stringOffsets = new int[phraseCount][];
				for(int p = 0; p < phraseCount; p++)
				{
					stringOffsets[p] = new int[languageCount];
					for(int l = 0; l < languageCount; l++)
					{
						stringOffsets[p][l] = stringDataSize;
						stringDataSize += LanguageBase[p][l].Length + 1;//+1 for null terminators
					}
				}
				
				//Size is in Int32 so round up.
				if((stringDataSize % 4) != 0)
				{
					stringDataSize += 4 - (stringDataSize % 4);
				}
				var stringData = new byte[stringDataSize];

				for(int p = 0; p < phraseCount; p++)
				{
					for(int l = 0; l < languageCount; l++)
					{
						var src = LanguageBase[p][l];
						var dst = stringData.AsSpan(stringOffsets[p][l]);
						for(int c=0; c<src.Length; c++)
						{
							dst[c] = (byte)src[c];
						}
						//Value will be '\0'.
					}
				}

				writer.Write<int>(languageCount-1);//Last language ID
				writer.Write<int>(phraseCount);//PhraseCount
				writer.Write<int>(stringDataSize/4);
				for(int p = 0; p < phraseCount; p++)
				{
					writer.WriteArray<int>(stringOffsets[p]);
				}
				writer.WriteArray<byte>(stringData);
			}
		}

		private static void ParseCameraPoints(MapPSX map, WadReader reader, WadFlagPSX wadFlag)
		{
			if((wadFlag & WadFlagPSX.WF_CAMERAPOINTS) == 0){return;}

			if(reader.ReadVersion != CROC_2_PS1.WadVersion)
			{
				throw new NotImplementedException("CAMERAPOINTS Only implemented on Croc2PSX");
			}

			var cameraEntryCount = reader.Read<int>();
			map.CameraPointTable = new CameraEntryPSX[cameraEntryCount];
			for(int i = 0; i < cameraEntryCount; i++)
			{
				uint loopCount = reader.Read<uint>();
				reader.AssertRead<uint>(0);//Loops placeholder
				map.CameraPointTable[i] = new(new CameraLoopPSX[loopCount]);
			}

			foreach(var entry in map.CameraPointTable)
			{
				for(int j = 0; j < entry.Loops.Length; j++)
				{
					reader.AssertRead<ushort>(0);//CameraLoopPSX.PointCount placeholder
					reader.AssertRead<ushort>(0);//CameraLoopPSX.Flags placeholder
					reader.AssertRead<uint>(0);//CameraLoopPSX.Points placeholder
				}
			}

			foreach(var entry in map.CameraPointTable)
			{
				for(int j = entry.Loops.Length - 1; j >= 0; j--)//Must be signed
				{
					var loop = new List<CameraPointPSX>();
					bool hasNext = true;
					while(hasNext)
					{
						var cameraPoint = new CameraPointPSX();
						reader.AssertRead<uint>(0);//Prev placeholder
						hasNext = reader.Read<uint>() switch
						{
							0 => false,
							1 => true,
							_ => throw new Exception("Invalid value"),
						};
						cameraPoint.X = reader.Read<Fixed32>();
						cameraPoint.Z = reader.Read<Fixed32>();
						cameraPoint.Y = reader.Read<Fixed32>();
						cameraPoint.Type = reader.Read<ushort>();
						cameraPoint.Angle = reader.Read<ushort>();
						reader.AssertRead<uint>(0);//Padding

						loop.Add(cameraPoint);
					}
					entry.Loops[j] = new(loop.ToArray());
				}
			}
			map.MaxCameraHeight = cameraEntryCount - 1;
		}

		private void WriteCameraPoints(WadWriter writer, WadFlagPSX wadFlag)
		{
			if((wadFlag & WadFlagPSX.WF_CAMERAPOINTS) == 0){return;}

			if(writer.WriteVersion != CROC_2_PS1.WadVersion)
			{
				throw new NotImplementedException("CAMERAPOINTS Only implemented on Croc2PSX");
			}

			writer.Write<int>(CameraPointTable.Length);
			for(int i = 0; i < CameraPointTable.Length; i++)
			{
				writer.Write<uint>((uint)CameraPointTable[i].Loops.Length);
				writer.Write<uint>(0);//Loops placeholder
			}

			foreach(var entry in CameraPointTable)
			{
				for(int j = 0; j < entry.Loops.Length; j++)
				{
					writer.Write<ushort>(0);//CameraLoopPSX.PointCount placeholder
					writer.Write<ushort>(0);//CameraLoopPSX.Flags placeholder
					writer.Write<uint>(0);//CameraLoopPSX.Points placeholder
				}
			}

			foreach(var entry in CameraPointTable)
			{
				for(int j = entry.Loops.Length - 1; j >= 0; j--)//Must be signed
				{
					var points = entry.Loops[j].Points;
					for(int i=0; i<points.Length; i++)
					{
						var cameraPoint = points[i];
						writer.Write<uint>(0);//Prev placeholder
						writer.Write<int>((i+1 < points.Length) ? 1 : 0);
						writer.Write<Fixed32>(cameraPoint.X);
						writer.Write<Fixed32>(cameraPoint.Z);
						writer.Write<Fixed32>(cameraPoint.Y);
						writer.Write<ushort>(cameraPoint.Type);
						writer.Write<ushort>(cameraPoint.Angle);
						writer.Write<uint>(0);//Padding
					}
				}
			}
		}
	}
}