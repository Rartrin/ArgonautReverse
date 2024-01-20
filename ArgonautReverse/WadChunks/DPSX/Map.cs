using System;
using ArgonautReverse.Engine.Versions;
using ArgonautReverse.IO;
using ArgonautReverse.LibGTE;
using ArgonautReverse.WadChunks.SPSX;

namespace ArgonautReverse.WadChunks.DPSX
{
	public sealed class MAPINDEX
	{
		public const int ByteSize = 8;

		public uint Index{get;private set;}
		public MAPINDEX Next{get;private set;}

		private MAPINDEX(){}

		public static IReadOnlyList<MAPINDEX> ParseIndexGrid(Map map, WadReader reader)
		{
			var mapIndexGrid = new MAPINDEX[map.NumberOfPieces];
			//Create References
			for(int i=0; i<map.NumberOfPieces; i++)
			{
				mapIndexGrid[i] = new MAPINDEX();
			}

			//Parse data
			for(int i=0; i<map.NumberOfPieces; i++)
			{
				mapIndexGrid[i].Index = reader.Read<uint>();
				var nextByteOffset = reader.Read<int>();
				if(nextByteOffset == -1)
				{
					mapIndexGrid[i].Next = null;
				}
				else
				{
					Utils.Assert((nextByteOffset%ByteSize)==0);
					mapIndexGrid[i].Next = mapIndexGrid[nextByteOffset/ByteSize];
				}
			}

			return mapIndexGrid;
		}
	}
	public sealed class ZONE:IReadable<ZONE>
	{
		public readonly uint Zone;
		//public uint ViewZone;

		private ZONE(uint zone)
		{
			Zone = zone;
		}

		public static ZONE Parse(WadReader reader)
		{
			var zone = reader.Read<uint>();
			return new ZONE(zone);
		}
	}

	public sealed class ZONEINFO:IReadable<ZONEINFO>
	{
		public readonly uint ViewZone;
		public readonly int WaterLevel;

		private ZONEINFO(uint viewZone, int waterLevel)
		{
			ViewZone = viewZone;
			WaterLevel = waterLevel;
		}

		public static ZONEINFO Parse(WadReader reader)
		{
			var viewZone = reader.Read<uint>();
			var waterLevel = reader.Read<int>();
			return new ZONEINFO(viewZone, waterLevel);
		}
	}

	public sealed class Waypoint
	{
		public const int ByteSize = 12 + POS.ByteSize;//36

		public Waypoint Next{get;private set;}
		public Waypoint Prev{get;private set;}
		public POS Position{get;private set;}
		public int Value{get;private set;}

		private Waypoint(){}

		public static IReadOnlyList<Waypoint> ParseWaypointList(WadReader parser, int waypointCount)
		{
			var list = new Waypoint[waypointCount];
			for(int i=0; i<waypointCount; i++)
			{
				list[i] = new Waypoint();
			}

			for(int i=0; i<waypointCount; i++)
			{
				var waypoint = list[i];
				
				//Sometimes this will be the index of the next value, but not always
				var nextValue = parser.Read<int>();
				if(nextValue == 0)
				{
					waypoint.Next = null;
				}
				else
				{
					waypoint.Next = list[i+1];
					waypoint.Next.Prev = waypoint;
				}

				var prevValue = parser.Read<int>();
				if(prevValue != 0)
				{
					throw new Exception("Prev reference was set");
				}
			
				waypoint.Position = parser.Read<POS>();
				waypoint.Value = parser.Read<int>();
			}
			return list;
		}
	}

	public sealed class TRACKCHANGE:IReadable<TRACKCHANGE>
	{
		public readonly uint X,Y,Z;
		public readonly uint NormalPiece;
		public readonly uint OtherPiece;

		public readonly int PieceMemIndex;//This is the index of the mem in the Piece array
		//public readonly uint* PieceMem;

		private TRACKCHANGE(uint x, uint y, uint z, uint normalPiece, uint otherPiece, int pieceMemIndex)
		{
			X = x;
			Y = y;
			Z = z;
			NormalPiece = normalPiece;
			OtherPiece = otherPiece;
			PieceMemIndex = pieceMemIndex;
		}

		public static TRACKCHANGE Parse(WadReader reader)
		{
			var x = reader.Read<uint>();
			var y = reader.Read<uint>();
			var z = reader.Read<uint>();
			var normalPiece = reader.Read<uint>();
			var otherPiece = reader.Read<uint>();

			var pieceMemIndex = reader.Read<int>();

			return new TRACKCHANGE(x, y, z, normalPiece, otherPiece, pieceMemIndex);
		}
	}

	public sealed class CRYSTAL:IReadable<CRYSTAL>
	{
		public const int ByteSize = 4 + BPOS.ByteSize;//36

		public readonly BPOS Pos;
		public readonly uint Flag;

		private CRYSTAL(BPOS pos, uint flag)
		{
			Pos = pos;
			Flag = flag;
		}

		public static CRYSTAL Parse(WadReader reader)
		{
			var pos = reader.Read<BPOS>();
			var flags = reader.Read<uint>();
			return new CRYSTAL(pos, flags);
		}
	}

	public readonly struct LightTuple:IReadable<LightTuple>
	{
		public readonly short LightA, LightB;

		private LightTuple(short lightA, short lightB)
		{
			LightA = lightA;
			LightB = lightB;
		}

		public static LightTuple Parse(WadReader reader)
		{
			var lightA = reader.Read<short>();
			var lightB = reader.Read<short>();
			return new LightTuple(lightA, lightB);
		}
	}

	public sealed class Map
	{
		public int NumberOfPieces;
	
		public ushort NumberOfStrats;
		public ushort PolyListSize;

		public ushort MaxStrats;
		public ushort LevelFlag;
		//ushort EffectFlags;

		public int MapXY;
		public uint MapX;
		public uint MapZ;
		public ushort NumberOfDoors;
		public ushort? NumberOfOtherPieces;
		public IReadOnlyList<Door> DoorList;
		public uint NumberOfWP;
		public IReadOnlyList<Waypoint> WPList;
		public IReadOnlyList<int> Params;

		#region Pre Croc 2 Demo
		public uint DrawMode;
		#endregion

		public IReadOnlyList<CRYSTAL> CrystalList;
		public IReadOnlyList<MapStrategy> Strats;
		public IntPtr not_used1;
		public IntPtr not_used2;
		public IReadOnlyList<POS> Positions;
		public IReadOnlyList<MAPINDEX> Grid;//Refs to values in IndexGrid
		public IReadOnlyList<MAPINDEX> IndexGrid;
		public IReadOnlyList<ZONE> ZoneData;
		public IReadOnlyList<uint> Pieces;
		public IReadOnlyList<TRACKCHANGE> TrackChangeData;
		public IReadOnlyList<CVECTOR[]> LightTables;

		#region Pre Croc 2 Demo
		public int BackgroundAddr;//Offset to Background
		public OBJECT Background;
		public uint BackgroundAddYRotation;
		public int BackgroundHeightAdjust;
		#endregion

		public MATRIX wlm;
		public MATRIX lcm;

		#region Non-map fields
		//Values not part of the map struct
		public IReadOnlyList<ZONEINFO> ZoneTable;

		//TODO: This is part of Aladdin, possibly in other games.
		public LightTuple[] LightTuples;
		public IReadOnlyList<OMNI_LIGHT> omniLights;
		public IReadOnlyList<IReadOnlyList<PRE_LIT>> pre_map;

		//Didn't have fields originally
		public int SequenceCount;
		public byte[] RawSequenceData;
		public IReadOnlyList<ALAmbience> Ambiences;
		#endregion

		private Map(){}

		private static Map ParseStruct(WadReader reader)
		{
			var map = new Map();

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
				if(unknownField!=0)
				{

				}
			}

			map.MapXY = reader.Read<int>();
			map.MapX = reader.Read<uint>();
			map.MapZ = reader.Read<uint>();
			Utils.Assert(map.MapXY == map.MapX*map.MapZ);

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

				reader.AssertRead<uint>(0);//not_used1 placeholder
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
				for(int i=0; i<7; i++)
				{
					Utils.Assert(placeholders1[i] == 0);
					//reader.AssertRead<uint>(0);
				}

				map.BackgroundAddr = reader.Read<int>();
				map.BackgroundAddYRotation = reader.Read<uint>();
				map.BackgroundHeightAdjust = reader.Read<int>();
				
				var placeholders2 = reader.ReadArray<int>(7);
				//These are likely placeholder fields
				for(int i=0; i<7; i++)
				{
					Utils.Assert(placeholders2[i] == 0);
					//reader.AssertRead<uint>(0);
				}
			}

			return map;
		}

		public static Map Parse(WadReader data_in, WadFlag wadFlag, LevelGeom3DData[] chunk_models)
		{
			//Params are stored before the map
			var mapParamCount = data_in.Read<int>();
			var mapParams = data_in.ReadInt32Array(mapParamCount);

			var map = ParseStruct(data_in);
			map.Params = mapParams;

			var mapGridOffsets = data_in.ReadArray<int>(map.MapXY);

			map.IndexGrid = MAPINDEX.ParseIndexGrid(map, data_in);
				
			var mapGrid = new MAPINDEX[map.MapXY];
			for(int i=0; i<map.MapXY; i++)
			{
				var offset = mapGridOffsets[i];
				if(offset == -1)
				{
					mapGrid[i] = null;
				}
				else
				{
					Utils.Assert((offset%MAPINDEX.ByteSize)==0);
					mapGrid[i] = map.IndexGrid[offset/MAPINDEX.ByteSize];
				}
			}
			map.Grid = mapGrid;

			//Taking a guess on this one
			//Not certain if WF_USESZONES can still be relied on
			bool newZones = (wadFlag&WadFlag.WF_NEWZONES)!=0;
			bool useZones = (wadFlag&WadFlag.WF_USESZONES)!=0;
			if((newZones != useZones) || (newZones == (data_in.DatVersion == CROC_2_DEMO_PS1_DUMMY.DatVersion)))
			{
				throw new Exception("Not certain if WF_USESZONES can still be relied on");
			}
			if((wadFlag&WadFlag.WF_NEWZONES)!=0)
			{
				map.ZoneTable = data_in.ReadArray<ZONEINFO>(32);

				var n_zone_ids = data_in.Read<int>();
				map.ZoneData = data_in.ReadArray<ZONE>(n_zone_ids);
				Utils.Assert(n_zone_ids == map.MapXY);
			}
			else if((wadFlag&WadFlag.WF_USESZONES)!=0)
			{
				throw new NotImplementedException("Code for this exists");
			}
			else
			{
				map.ZoneData = null;
			}

			if(data_in.ReadString(4) == "fvw\x00")
			{
				//LightTuples was fvw_data in the original exporter
				//TODO: Original exporter has LightTuple as 16 bits
				//Aladdin has it as 32 bits.


				map.LightTuples = new LightTuple[map.MapXY];
				for(int i=0; i<map.MapXY;i++)
				{
					map.LightTuples[i] = data_in.Read<LightTuple>();
				}

				throw new Exception("Uncertain LightTuple size");
			}
			else
			{
				map.LightTuples = null;
				data_in.SkipBytes(-4);
			}

			map.Positions =  data_in.ReadArray<POS>(map.NumberOfPieces);

			map.Pieces = data_in.ReadArray<uint>(map.NumberOfPieces);
			
			if(map.NumberOfDoors != 0)
			{
				map.DoorList = data_in.ReadArray<Door>(map.NumberOfDoors);

				//TODO: Assign Background for each door
			}

			map.WPList = Waypoint.ParseWaypointList(data_in, (int)map.NumberOfWP);

			var strats = new MapStrategy[map.NumberOfStrats];
			for(int i=0; i<map.NumberOfStrats; i++)
			{
				strats[i] = MapStrategy.Parse(data_in, map);
			}
			map.Strats = strats;

			if((wadFlag&WadFlag.WF_NOMATPOS)==0)//Notice, this is if the flag is NOT on
			{
				if(data_in.DatVersion!=CROC_2_DEMO_PS1_DUMMY.DatVersion)
				{
					throw new Exception("Unsure if DUMMY version supports this flag");
				}
				//TODO: Data can exist but is not used by Croc 2 Demo and Aladdin.
				//Is this data used by the dummy?
				var matpos = data_in.ReadArray<MATRIX>(map.NumberOfPieces);
				var matdraw = data_in.ReadArray<MATRIX>(map.NumberOfPieces);
			}
			else
			{
				if(data_in.DatVersion==CROC_2_DEMO_PS1_DUMMY.DatVersion)
				{
					//Getting here means that DUMMY definitely supports the WF_NOMATPOS flag
				}
			}


			if(data_in.DatVersion==CROC_2_DEMO_PS1_DUMMY.DatVersion)
			{
				//TODO: Unknown data
				var unknownData = data_in.ReadArray<int>(20);
			}




			if((wadFlag&WadFlag.WF_CAMERAPOINTS)!=0)
			{
				//TODO: WF_CAMERAPOINTS
				throw new NotImplementedException();
			}

			if((wadFlag&WadFlag.WF_HASOTHERPIECES)!=0)
			{
				if(data_in.DatVersion==CROC_2_DEMO_PS1_DUMMY.DatVersion)
				{
					throw new Exception("Unsure if DUMMY version supports this");
				}

				map.TrackChangeData = data_in.ReadArray<TRACKCHANGE>(map.NumberOfOtherPieces.Value);
			}

			if(data_in.DatVersion != CROC_2_DEMO_PS1_DUMMY.DatVersion)
			{
				//TODO: Omni Lights
				var omniLightCount = data_in.Read<int>();

				if(data_in.DatVersion == CROC_2_DEMO_PS1.DatVersion && omniLightCount!=1)
				{
					//Old exporter skips 32 bytes for demo here.
					//Thinking it may be be 1 int for count and 28 for a single OMNI_LIGHT
					throw new Exception("Hypothesis is wrong");
				}

				map.omniLights = data_in.ReadArray<OMNI_LIGHT>(omniLightCount);
			}

			if((wadFlag&WadFlag.WF_MAP_LIGHTINGTABLE)!=0)
			{
				//TODO: WF_MAP_LIGHTINGTABLE

				//TODO: This is a lot to be skipping on the release version
				if(data_in.DatVersion == CROC_2_PS1.DatVersion)
				{
					data_in.SkipBytes(30732);
				}
				else if(data_in.DatVersion == CROC_2_DEMO_PS1_DUMMY.DatVersion)
				{
					throw new NotImplementedException();
				}
				else
				{
					if(map.NumberOfPieces != 0)
					{
						//TODO: This appears to be in the wrong order compared to any of my versions

						var sub_chunks_n_lighting = data_in.ReadUInt32Array(map.NumberOfPieces);
						var sub_chunks_n_add_lighting = data_in.ReadUInt32Array(map.NumberOfOtherPieces.Value);


						for(int model_id=0; model_id<map.NumberOfPieces; model_id++)
						{
							for(int i=0; i<sub_chunks_n_lighting[model_id]; i++)
							{
								var size = 4 * chunk_models[map.Pieces[model_id]].Data.n_vertices;
								data_in.SkipBytes(size);
							}
						}
						for(int model_id=0; model_id<map.NumberOfOtherPieces.Value; model_id++)
						{
							for(int i=0; i<sub_chunks_n_add_lighting[model_id]; i++)
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
							var idk4 = data_in.ReadArrayOfByteArrays(40, n_idk3);//var idk3 = [int.from_bytes(data_in.read(40), "little") for _ in range(n_idk3)]
						}
						data_in.SkipBytes(12);
					}
				}
			}

			if(data_in.WadFile.Stem == "01507800")
			{

			}
			if((wadFlag&WadFlag.WF_MAP_PRELIT)!=0)
			{
				var preMap = new PRE_LIT[2][];
				for(int i=0; i<preMap.Length; i++)
				{
					preMap[i] = new PRE_LIT[map.NumberOfPieces];

					for(int j = 0; j<map.NumberOfPieces; j++)
					{
						var lfacePlaceholder = data_in.Read<uint>();
						if(lfacePlaceholder != 0)
						{
							throw new Exception();
						}
						preMap[i][j] = new PRE_LIT();
					}

					for(int j=0; j<map.NumberOfPieces; j++)
					{
						var trackObj = chunk_models[map.Pieces[j]];
						preMap[i][j].lface = data_in.ReadArray<POLY_ALL>(trackObj.Header.n_faces);
					}
				}
				map.pre_map = preMap;
			}
			else
			{
				//TODO: What is this and where does it go? (DUMMY related)
				if(data_in.DatVersion == CROC_2_DEMO_PS1_DUMMY.DatVersion)
				{
					//data_in.SkipBytes(12);
					var unknownData = data_in.ReadArray<int>(3);
				}
			}

			//var soundDataFlags = data_in.WadFile.SPSX?.spsx_flags ?? 0;
			//if((soundDataFlags&SPSXFlags.HAS_AMBIENT_TRACKS)!=0)
			//{
			//	var totalSequenceByteLength = data_in.Read<int>();
			//	map.SequenceCount = 0;
			//	if((soundDataFlags&SPSXFlags.AMBIENTSEP)!=0)
			//	{
			//		map.SequenceCount = data_in.Read<int>();
			//	}
			//	map.RawSequenceData = data_in.ReadArray<byte>(totalSequenceByteLength);
			//	//TODO: Parse sequence data

			//	IReadOnlyList<ALAmbience> ambience = null;
			//	if((soundDataFlags&SPSXFlags.AMBIENTSEP)!=0)
			//	{
			//		if((wadFlag&WadFlag.WF_HASMULTIAMBIENT)!=0)
			//		{
			//			var ambienceCount = data_in.Read<int>();
			//			ambience = data_in.ReadArray<ALAmbience>(ambienceCount);
			//		}
			//	}
			//	else
			//	{
			//		if((wadFlag&WadFlag.WF_HASMULTIAMBIENT)!=0)
			//		{
			//			var ambienceCount = data_in.Read<int>();
			//			ambience = data_in.ReadArray<ALAmbience>(ambienceCount);
			//		}
			//	}
			//	map.Ambiences = ambience;
			//}
			//else
			//{
				
			//}

			if((wadFlag&WadFlag.WF_HASINVENTORY)!=0)
			{
				throw new NotImplementedException();
			}

			if((wadFlag&WadFlag.WF_HASLANGUAGES)!=0)
			{
				throw new NotImplementedException();
			}

			return map;
		}
	}
}