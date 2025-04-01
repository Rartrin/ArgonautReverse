using ArgonautReverse.IO;
using ArgonautReverse.Universal;
using ArgonautReverse.WadChunks.PC;

namespace ArgonautReverse.PC
{
	public sealed class MapPiecePC//WorldCellInfo
	{
		public Vector3F Pos;
		public int RawRotY;//Value is 0-1 in 24bit (0 to 0xFF0). Also, the lowest nibble is ignored.
		public float RotY;
		public int CellIndex;
		public bool bVisible = false;
		public int gapField6;
		public int field7 = 0;

		public MapPiecePC(in Vector3F pos, int rawRotY, float rotY, int n)
		{
			this.Pos = pos;
			this.RawRotY = rawRotY;
			this.RotY = Utils.Deg2Rad(rotY);
			this.CellIndex = n;
		}
	}

	public sealed class MapPieceListPC//WorldCell
	{
		public MapPiecePC Piece;
		public MapPieceListPC Next;

		public MapPieceListPC(MapPiecePC piece)
		{
			Piece = piece;
		}
	}

	[Flags]
	public enum MapFlagsPC:uint
	{
		MAP_FLAG_REMOVED = 0x2,
		MAP_FLAG_COLLECTED = 0x4,
		MAP_FLAG_ACTIVATED = 0x8,
	}

	public sealed class MapStratPC:IReadable<MapStratPC,MapPC>
	{
		public RotPos3I RotPos;

		public int instructionStreamOffset;
		
		//public int NumberParameters;
		public IReadOnlyList<int> ParamBlock;
		public int LocalCount;
		public int TriggerCount;
		public CollisionTypePC Collision;
		public int CollisionBoneCount;
		public WaypointPC FirstWP;
		public WaypointPC LastWP;
		public MapFlagsPC flags;

		public static MapStratPC Parse(WadReader reader, MapPC map)
		{
			var mapStrat = new MapStratPC();

			var mapStratRotation = reader.Read<Vector3<ushort>>();
			mapStrat.RotPos.Rotation.X = mapStratRotation.X;
			mapStrat.RotPos.Rotation.Y = mapStratRotation.Y;
			mapStrat.RotPos.Rotation.Z = mapStratRotation.Z;

			mapStrat.RotPos.Position = reader.Read<Vector3I>();
			mapStrat.instructionStreamOffset = reader.Read<int>();//Add to data.fileData to get the address of the instructionStream;

			int numberParameters = reader.Read<int>();
			int ptrOffset = reader.Read<int>();
			if(ptrOffset == -1)
			{
				mapStrat.ParamBlock = null;
			}
			else
			{
				mapStrat.ParamBlock = new ArraySegment<int>((int[])map.Params, ptrOffset, numberParameters);
			}
			mapStrat.LocalCount = reader.Read<int>();
			mapStrat.TriggerCount = reader.Read<int>();
			mapStrat.Collision = (CollisionTypePC)reader.Read<uint>();
			mapStrat.CollisionBoneCount = reader.Read<int>();
			int firstWaypointIndex = reader.Read<int>();
			if(firstWaypointIndex == -1)
			{
					mapStrat.FirstWP = null;
			}
			else
			{
				mapStrat.FirstWP = map.WaypointList[firstWaypointIndex];
			}
			if(firstWaypointIndex >= map.WaypointList.Count)
			{
				Console.WriteLine("Erruuughh...");
			}
			reader.AssertRead<uint>(0);//was a LastWP placeholder originally I think
			mapStrat.flags = (MapFlagsPC)reader.Read<uint>();

			return mapStrat;
		}
	}

	public sealed class StrategyListPC//WadChunkData
	{
		//public byte* fileData;

		public StratEntityPC FirstStrat;
		public StratEntityPC FirstUnused;//RootEntity
		public StratEntityPC Player;
		public StratEntityPC OldPlayer;
		public StratEntityPC TargetStrat;
		public StratEntityPC TargetStrat2;
		public StratEntityPC Boss;
		public StratEntityPC Camera;
		public StratEntityPC Dialog;
		public PCCameraStackEntry[] cameraStack;//[8];
		public int cameraStackCount;
	}

	public readonly struct ZonePC:IReadable<ZonePC>
	{
		public readonly uint Zone;
		//public uint ViewZone;

		public ZonePC(uint zone)
		{
			Zone = zone;
		}

		public static ZonePC Parse(WadReader reader)
		{
			var zone = reader.Read<uint>();
			return new ZonePC(zone);
		}
	}

	public sealed class TrackChangePC:IReadable<TrackChangePC,MapPiecePC>//PieceStruct
	{
		public Vector3I Pos;
		public int NormalPiece;//field0;
		public int OtherPiece;//modelIndex
		public MapPiecePC PieceMem;//cellInfo

		public TrackChangePC(Vector3I pos, int normalPiece, int otherPiece, MapPiecePC pieceMem)
		{
			Pos = pos;
			NormalPiece = normalPiece;
			OtherPiece = otherPiece;
			PieceMem = pieceMem;
		}

		public static TrackChangePC Parse(WadReader reader, MapPiecePC pieceMem)
		{
			var pos = reader.Read<Vector3I>();
			var normalPiece = reader.Read<int>();
			var otherPiece = reader.Read<int>();
			return new TrackChangePC(pos, normalPiece, otherPiece, pieceMem);
		}
	}

	public sealed class MapPC
	{
		public StratObjectPC Background;

		public IReadOnlyList<IReadOnlyList<MapPieceListPC>> MapPieceArray;
		public int NumPieces;
		public int MapWidth;
		public int MapHeight;

		//public int NumLights;
		public IReadOnlyList<LightPC> Lights;
		//public StrategyList    strategy_list;

		public IReadOnlyList<ZonePC> ZoneData;
		public IReadOnlyList<RotPos3Fx> Positions;

		//public uint NumberOfDoors;
		public IReadOnlyList<DoorPC> DoorList;

		//public int NumWaypoints;
		public IReadOnlyList<WaypointPC> WaypointList;

		//public int NumStrats;
		public int MaxStrats;
		public IReadOnlyList<MapStratPC> MapStrats;

		//public int NumParams;
		public IReadOnlyList<int> Params;

		public IReadOnlyList<TrackChangePC> TrackChangeData;
		//public int NumberOfOtherPieces;

		public short wField2;
		public short wField3;
		//public int array3Count;
		public IReadOnlyList<string> strArray;//Array of char[0x30]


		//Non-Map fields
		public IReadOnlyList<WaterLevelStructPC> WaterLevelArray;
		public int WaterLevel;

		public IReadOnlyList<IReadOnlyList<ColorBGRA32>> ColorArray;

		public int PolygonArraysCount;
		public IReadOnlyList<PolygonArrayPC> PolygonArrays;

		public int MaxParticles;

		public static MapPC Parse(WadReader reader)
		{
			var map = new MapPC();

			var wadFlags = reader.WadFile.GetChunk(WFPCChunkInfo.Instance).WadFlags;

			map.NumPieces = reader.Read<int>();
			map.MapWidth = reader.Read<int>();
			map.MapHeight = reader.Read<int>();

			var mapPieceArray = new MapPieceListPC[map.MapHeight][];
			for(int i = 0; i < map.MapHeight; ++i)
			{
				mapPieceArray[i] = new MapPieceListPC[map.MapWidth];
				for(int j = 0; j < map.MapWidth; j++)
				{
					mapPieceArray[i][j] = null;
				}
			}
			var dataPos = new MapPiecePC[map.NumPieces];
			int pos_count = 0;
			for(int cellIndex=0; cellIndex<map.NumPieces; cellIndex++)
			{
				var chunkPos = reader.Read<Vector3F>();
				var rotY = reader.Read<int>();
				var n = reader.Read<int>();

				// Conversion from 0.0-1.0 angle in fixed point 24bit to floating point degrees
				//Aladdin uses n instead of cellIndex for last arg
				var worldCellInfo = new MapPiecePC(chunkPos, rotY, ((rotY&0xFF0) * 360f) / 4096f, cellIndex);

				bool hasOtherPiece = reader.Read<int>() switch
				{
					0 => false,
					1 => true,
					_ => throw new Exception()
				};
				int cellX = (int)(chunkPos.X - 0.5f);
				int cellZ = (int)(-chunkPos.Z - 0.5f);
				var curCell = mapPieceArray[cellZ][cellX];
				if(curCell != null)
				{
					var prevInfo = curCell.Piece;
					if(worldCellInfo.Pos.Y <= prevInfo.Pos.Y)
					{
						ReadWadChunkMAP_InitCells(curCell, worldCellInfo);
					}
					else
					{
						ReadWadChunkMAP_InitCells(curCell, prevInfo);
						curCell.Piece = worldCellInfo;
					}
				}
				else
				{
					mapPieceArray[cellZ][cellX] = new MapPieceListPC(worldCellInfo);
				}
				if(hasOtherPiece)
				{
					dataPos[pos_count++] = worldCellInfo;
				}
			}
			map.MapPieceArray = mapPieceArray;

			var paramCount = reader.Read<int>();
			var paramBlock = reader.ReadArray<int>(paramCount);
			map.Params = paramBlock;

			var doorCount = reader.Read<int>();
			map.DoorList = reader.ReadArray<DoorPC>(doorCount);

			map.WaypointList = WaypointPC.ParseWaypoints(reader);

			map.WaterLevelArray = reader.ReadArray<WaterLevelStructPC>(32);
			map.WaterLevel = -65536;

			var zoneData = new ZonePC[map.MapWidth * map.MapHeight];
			for(int m = 0; m < map.MapHeight; ++m)
			{
				for(int n = 0; n < map.MapWidth; ++n)
				{
					zoneData[n + m * map.MapWidth] = reader.Read<ZonePC>();
				}
			}
			map.ZoneData = zoneData;
			map.Positions = reader.ReadArray<RotPos3Fx>(map.NumPieces);
			var modelIndices = reader.ReadArray<int>(map.NumPieces);
			if((wadFlags & WadFlagPC.WAD_FLAG_HAS_CHANGING_GEOMETRY) != 0)
			{
				var numberOfOtherPieces = reader.Read<int>();
				map.TrackChangeData = reader.ReadArray<TrackChangePC,MapPiecePC>(dataPos, numberOfOtherPieces);
			}
			else
			{
				map.TrackChangeData = Array.Empty<TrackChangePC>();
			}
			var colorArray = new ColorBGRA32[map.NumPieces][];
			var trackModels = reader.WadFile.GetChunk(TRAKChunkInfo.Instance).Models;
			for(int cellIndex=0; cellIndex<map.NumPieces; cellIndex++)
			{
				var cellModel = trackModels[modelIndices[cellIndex]];
				byte maxAlpha = 1;
				colorArray[cellIndex] = reader.ReadArray<ColorBGRA32>(cellModel.vertexCount);
				for(int vertIndex=0; vertIndex<cellModel.vertexCount; vertIndex++)
				{
					var curAlpha = colorArray[cellIndex][vertIndex].A;
					if(curAlpha > maxAlpha)
					{
						maxAlpha = curAlpha;
					}
				}
				if(maxAlpha > 1)
				{
					Array.Resize(ref colorArray[cellIndex], cellModel.vertexCount * maxAlpha);

					colorArray[cellIndex][0].A = maxAlpha;
					for(int alpha=1; alpha<maxAlpha; alpha++)
					{
						reader.ReadArray(colorArray[cellIndex].AsSpan(alpha * cellModel.vertexCount, cellModel.vertexCount));
					}
				}
				int buffer = cellModel.vertexCount*maxAlpha;
				foreach(var trackChange in map.TrackChangeData)
				{
					if(trackChange.PieceMem.CellIndex != cellIndex){continue;}

					var pieceModel = trackModels[trackChange.OtherPiece];

					Array.Resize(ref colorArray[cellIndex], colorArray[cellIndex].Length + pieceModel.vertexCount*maxAlpha);

					for(int vertIndex=0; vertIndex<pieceModel.vertexCount; vertIndex++)
					{
						ref var curColor = ref colorArray[cellIndex][buffer + vertIndex];
						curColor = reader.Read<ColorBGRA32>();
						if(curColor.A > maxAlpha)
						{
							maxAlpha = curColor.A;
						}
					}
					if(maxAlpha > 1)
					{
						colorArray[cellIndex][buffer].A = maxAlpha;
					}
					for(int alpha = 1; alpha<maxAlpha; alpha++)
					{
						for(int vertIndex=0; vertIndex<pieceModel.vertexCount; vertIndex++)
						{
							ref var curColor = ref colorArray[cellIndex][vertIndex + buffer + alpha * pieceModel.vertexCount];
							curColor = reader.Read<ColorBGRA32>();
						}
					}
					break;
				}
			}
			map.ColorArray = colorArray;
			var strArrayCount = reader.Read<int>();
			var strArray = new string[strArrayCount];
			for(int i=0; i < strArrayCount; i++)
			{
				strArray[i] = reader.ReadString(0x30);
			}
			map.strArray = strArray;

			var mapStratCount = reader.Read<int>();
			map.MaxStrats = reader.Read<int>();
			var mapStrats = reader.ReadArray<MapStratPC,MapPC>(map, mapStratCount);
			for(int mapIndex=0; mapIndex<mapStratCount; mapIndex++)
			{
				var mapStrat = mapStrats[mapIndex];
				if(mapStrat.FirstWP != null)
				{
					WaypointPC curAlien = mapStrat.FirstWP;
					while(curAlien.Next!=null)
					{
						curAlien = curAlien.Next;
					}
					mapStrat.LastWP = curAlien;
				}
			}
			map.MapStrats = mapStrats;

			if((wadFlags & WadFlagPC.WAD_FLAG_CAMERAPOINTS) != 0)
			{
				map.PolygonArraysCount = reader.Read<int>();
				map.PolygonArrays = reader.ReadArray<PolygonArrayPC,WadFlagPC>(wadFlags, map.PolygonArraysCount);
				//map.PolygonArraysCount--;
			}
			if((wadFlags & WadFlagPC.WAD_FLAG_HAS_PARTICLES) != 0)
			{
				map.MaxParticles = reader.Read<int>();
			}
			else
			{
				map.MaxParticles = 200;
			}
			map.wField2 = reader.Read<short>();

			return map;
		}

		public static void ReadWadChunkMAP_InitCells(MapPieceListPC cell, MapPiecePC info)
		{
			if(cell.Next != null)
			{
				var currCell = cell.Next;
				while(info.Pos.Y <= (double)currCell.Piece.Pos.Y)
				{
					if(currCell.Next == null)
					{
						currCell.Next = new MapPieceListPC(info);
						return;
					}
					currCell = currCell.Next;
				}
				ReadWadChunkMAP_InitCells(currCell, currCell.Piece);
				currCell.Piece = info;

			}
			else
			{
				cell.Next = new MapPieceListPC(info);
			}
		}
	}
}
