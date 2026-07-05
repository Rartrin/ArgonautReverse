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
				var nextByteOffset = reader.Read<int>();
				if(nextByteOffset == -1)
				{
					mapIndexGrid[i].Next = null;
				}
				else
				{
					Utils.Assert(nextByteOffset % ByteSize == 0);
					mapIndexGrid[i].Next = mapIndexGrid[nextByteOffset / ByteSize];
				}
			}

			return mapIndexGrid;
		}
	}

	public sealed class WaypointPSX:IReadable<WaypointPSX>
	{
		public const int ByteSize = 12 + POS.ByteSize;//36

		public WaypointPSX Next{get;private set;}
		public int NextRawValue;

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
	}

	public sealed class TrackChangePSX:IReadable<TrackChangePSX>
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
	}

	public sealed class CrystalPSX:IReadable<CrystalPSX>
	{
		public const int ByteSize = 4 + BPOS.ByteSize;//36

		public readonly BPOS Pos;
		public readonly uint Flag;

		private CrystalPSX(BPOS pos, uint flag)
		{
			Pos = pos;
			Flag = flag;
		}

		public static CrystalPSX Parse(WadReader reader)
		{
			var pos = reader.Read<BPOS>();
			var flags = reader.Read<uint>();
			return new CrystalPSX(pos, flags);
		}
	}

	public readonly struct LightTuplePSX:IReadable<LightTuplePSX>
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
		public IReadOnlyList<IReadOnlyList<MapIndexPSX>> Grid;//Refs to values in IndexGrid
		public IReadOnlyList<MapIndexPSX> IndexGrid;
		public IReadOnlyList<Zone>? ZoneData;
		public IReadOnlyList<uint> Pieces;
		public IReadOnlyList<TrackChangePSX> TrackChangeData;
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
		public (uint[] Palette, uint[] Bitmap)[] InvAddresses;
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

		public static MapPSX Parse(WadReader data_in, WadFlagPSX wadFlag, TObjectDataPSX[] chunk_models)
		{
			//Params are stored before the map
			var mapParamCount = data_in.Read<int>();
			var mapParams = data_in.ReadArray<int>(mapParamCount);

			var map = ParseStruct(data_in);
			map.Params = mapParams;

			var mapGridOffsets = new int[map.MapZ][];
			for(int z = 0; z < map.MapZ; z++)
			{
				mapGridOffsets[z] = data_in.ReadArray<int>(map.MapX);
			}

			map.IndexGrid = MapIndexPSX.ParseIndexGrid(map.NumberOfPieces, data_in);

			var mapGrid = new MapIndexPSX[map.MapXY][];
			for(int z = 0; z < map.MapZ; z++)
			{
				mapGrid[z] = new MapIndexPSX[map.MapX];
				for(int x = 0; x < map.MapX; x++)
				{
					var offset = mapGridOffsets[z][x];
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
			//	//HP1 has NEWZONES with out USESZONES
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

					var lightingTableIndices = data_in.ReadArray<int>(map.NumberOfPieces);
					map.LightTables = new CVECTOR[map.NumberOfPieces][];
					for(int i=0; i<map.NumberOfPieces; i++)
					{
						if(lightingTableIndices[i] == 0){continue;}

						map.LightTables[i] = data_in.ReadArray<CVECTOR>(chunk_models[map.Pieces[i]].Object.nvert * lightingTableIndices[i]);
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
						//TODO: What gaems use this? This appears to be in the wrong order compared to any of my versions.

						var sub_chunks_n_lighting = data_in.ReadArray<uint>(map.NumberOfPieces);
						var sub_chunks_n_add_lighting = data_in.ReadArray<uint>(map.NumberOfOtherPieces!.Value);


						for(int model_id = 0; model_id < map.NumberOfPieces; model_id++)
						{
							for(int i = 0; i < sub_chunks_n_lighting[model_id]; i++)
							{
								var size = 4 * chunk_models[map.Pieces[model_id]].Data.n_vertices;
								data_in.SkipBytes(size);
							}
						}
						for(int model_id = 0; model_id < map.NumberOfOtherPieces.Value; model_id++)
						{
							for(int i = 0; i < sub_chunks_n_add_lighting[model_id]; i++)
							{
								var size = 4 * chunk_models[map.TrackChangeData[model_id].OtherPiece].Data.n_vertices;
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
						var trackObj = chunk_models[map.Pieces[j]];
						preMap[i][j].lface = data_in.ReadArray<PolyAllPSX>(trackObj.Object.nface);
					}
				}
				map.pre_map = preMap;
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
				if((wadFlag & WadFlagPSX.WF_HASFULLSETOFICONS) != 0)
				{
					invCount = 10;
				}
				else
				{
					invCount = 6;
				}
				map.InvAddresses = new (uint[],uint[])[invCount];
				for(int i = 0; i < invCount; i++)
				{
					var palette = data_in.ReadArray<uint>(128);
					var bitmap = data_in.ReadArray<uint>(16*40);
					map.InvAddresses[i] = (palette, bitmap);
				}
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
				var stringOffsets = data_in.ReadArray<int>(numberOfLanguages * phraseCount);
				map.LanguageBase = new string[numberOfLanguages][];
				for(int i=0; i<numberOfLanguages; i++)
				{
					map.LanguageBase[i] = new string[phraseCount];
				}
				var stringData = data_in.ReadArray<sbyte>(4*(stringDataSize+1));//TODO:T3L1M001 needed +1. They forgot to count the null terminator?
				unsafe
				{
					fixed(sbyte* stringData0 = stringData)
					{
						int langIndex = 0;
						for(int l = 0; l < numberOfLanguages; l++)
						{
							for(int p = 0; p < phraseCount; p++)
							{
								map.LanguageBase[l][p] = new string(&stringData0[stringOffsets[langIndex]]);// Values are offsets from the LanguageBase
								langIndex++;
							}
						}
					}
				}
			}

			return map;
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
	}
}