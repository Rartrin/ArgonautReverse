using ArgonautReverse.IO;
using ArgonautReverse.WadChunks;
using ArgonautReverse.WadChunks.DPSX;
using ArgonautReverse.WadChunks.MAP;
using ArgonautReverse.WadChunks.TRAK;
using ArgonautReverse.WadChunks.WFPC;

namespace ArgonautReverse.PC
{
	public sealed class MapPiecePC//WorldCellInfo
	{
		public Vector3F Pos;
		public float RotY;
		public int CellIndex;
		public int gapField5;
		public int gapField6;
		public int field7;

		public MapPiecePC(in Vector3F pos, float rotY, int n)
		{
			this.Pos = pos;
			this.RotY = Utils.Deg2Rad(rotY);
			this.CellIndex = n;
			this.gapField5 = (int)((uint)this.gapField5&0xFFFFFF00);
			this.field7 = 0;
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

	public enum MapFlags : uint
	{
		MAP_FLAG_2 = 0x2,
		MAP_FLAG_COLLECTED = 0x4,
		MAP_FLAG_ACTIVATED = 0x8,
	}

	public sealed class MapStratPC:IReadable<MapStratPC,MapPC>
	{
		public Vector3I checkPointRotation;
		public Vector3I checkPointPosition;
		public int instructionStreamOffset;
		//public int NumberParameters;
		public IReadOnlyList<int> ParamBlock;
		public int triggerIndex;
		public int count0;
		public SpawnFlags spawnFlags;
		public int count1;
		public WaypointPC alienFirst;
		public WaypointPC alienLast;
		public MapFlags flags;

		public static MapStratPC Parse(WadReader reader, MapPC map)
		{
			var mapStrat = new MapStratPC();

			var checkPointRotation = reader.Read<Vector3<ushort>>();
			mapStrat.checkPointRotation.X = checkPointRotation.X;
			mapStrat.checkPointRotation.Y = checkPointRotation.Y;
			mapStrat.checkPointRotation.Z = checkPointRotation.Z;

			mapStrat.checkPointPosition = reader.Read<Vector3I>();
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
			mapStrat.triggerIndex = reader.Read<int>();
			mapStrat.count0 = reader.Read<int>();
			mapStrat.spawnFlags = (SpawnFlags)reader.Read<uint>();
			mapStrat.count1 = reader.Read<int>();
			int alienFirstIndex = reader.Read<int>();
			if(alienFirstIndex == -1)
			{
					mapStrat.alienFirst = null;
			}
			else
			{
				mapStrat.alienFirst = map.WaypointList[alienFirstIndex];
			}
			if(alienFirstIndex >= map.WaypointList.Count)
			{
				Console.WriteLine("Erruuughh...");
			}
			reader.AssertRead<uint>(0);//was an alienLast placeholder originally I think
			mapStrat.flags = (MapFlags)reader.Read<uint>();

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

	public sealed class ZoneStruct
	{
		public uint Zone;
		public uint ViewZone;
	}

	public sealed class TrackChange:IReadable<TrackChange,MapPiecePC>//PieceStruct
	{
		public Vector3I Pos;
		public int NormalPiece;//field0;
		public int OtherPiece;//modelIndex
		public MapPiecePC PieceMem;//cellInfo

		public TrackChange(Vector3I pos, int normalPiece, int otherPiece, MapPiecePC pieceMem)
		{
			Pos = pos;
			NormalPiece = normalPiece;
			OtherPiece = otherPiece;
			PieceMem = pieceMem;
		}

		public static TrackChange Parse(WadReader reader, MapPiecePC pieceMem)
		{
			var pos = reader.Read<Vector3I>();
			var normalPiece = reader.Read<int>();
			var otherPiece = reader.Read<int>();
			return new TrackChange(pos, normalPiece, otherPiece, pieceMem);
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

		public IReadOnlyList<ZoneStruct> ZoneData;
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

		public IReadOnlyList<TrackChange> TrackChangeData;
		public int NumberOfOtherPieces;

		public short wField2;
		public short wField3;
		//public int array3Count;
		public IReadOnlyList<string> strArray;//Array of char[0x30]


		//Non-Map fields
		public IReadOnlyList<WaterLevelStructPC> WaterLevelArray;
		public int WaterLevel;

		public IReadOnlyList<IReadOnlyList<Color32>> ColorArray;

		public int PolygonArraysCount;
		public IReadOnlyList<PolygonArray> PolygonArrays;

		public int MaxParticles;

		public static MapPC Parse(WadReader reader)
		{
			var map = new MapPC();

			var wadFlags = reader.WadFile.GetChunk<WFPCChunk>(ChunkType.ID_PC_WADFLAGS).WadFlags;

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
				var worldCellInfo = new MapPiecePC(chunkPos, ((rotY&0xFF0) * 360f) / 4096f, cellIndex);

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
					var prevSecondaryCell = curCell.Next;
					if(worldCellInfo.Pos.Y <= prevInfo.Pos.Y)
					{
						if(prevSecondaryCell != null)
						{
							ReadWadChunkMAP_InitCells(prevSecondaryCell, worldCellInfo);
						}
						else
						{
							curCell.Next = new MapPieceListPC(worldCellInfo);
						}
					}
					else if(prevSecondaryCell != null)
					{
						ReadWadChunkMAP_InitCells(prevSecondaryCell, prevInfo);
						curCell.Piece = worldCellInfo;
					}
					else
					{
						curCell.Piece = worldCellInfo;
						curCell.Next = new MapPieceListPC(prevInfo);
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

			var waterIndexArray = new int[map.MapWidth * map.MapHeight];
			for(int m = 0; m < map.MapHeight; ++m)
			{
				for(int n = 0; n < map.MapWidth; ++n)
				{
					waterIndexArray[n + m * map.MapWidth] = reader.Read<int>();
				}
			}
			map.Positions = reader.ReadArray<RotPos3Fx>(map.NumPieces);
			var modelIndices = reader.ReadArray<int>(map.NumPieces);
			if((wadFlags & WadFlagsPC.WAD_FLAG_HAS_CHANGING_GEOMETRY) != 0)
			{
				var pieceCount = reader.Read<int>();
				map.TrackChangeData = reader.ReadArray<TrackChange,MapPiecePC>(dataPos, pieceCount);
			}
			else
			{
				map.TrackChangeData = Array.Empty<TrackChange>();
			}
			var colorArray = new Color32[map.NumPieces][];
			var trackModels = reader.WadFile.GetChunk<TRAKChunk>(ChunkType.ID_PC_TRACK).Models;
			for(int cellIndex=0; cellIndex<map.NumPieces; cellIndex++)
			{
				var cellModel = trackModels[modelIndices[cellIndex]];
				byte maxAlpha = 1;
				colorArray[cellIndex] = reader.ReadArray<Color32>(cellModel.vertexCount);
				for(int vertIndex=0; vertIndex<cellModel.vertexCount; vertIndex++)
				{
					var curAlpha = colorArray[cellIndex][vertIndex].alpha;
					if(curAlpha > maxAlpha)
					{
						maxAlpha = curAlpha;
					}
				}
				if(maxAlpha > 1)
				{
					Array.Resize(ref colorArray[cellIndex], cellModel.vertexCount * maxAlpha);

					colorArray[cellIndex][0].alpha = maxAlpha;
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
						curColor = reader.Read<Color32>();
						if(curColor.alpha > maxAlpha)
						{
							maxAlpha = curColor.alpha;
						}
					}
					if(maxAlpha > 1)
					{
						colorArray[cellIndex][buffer].alpha = maxAlpha;
					}
					for(int alpha = 1; alpha<maxAlpha; alpha++)
					{
						for(int vertIndex=0; vertIndex<pieceModel.vertexCount; vertIndex++)
						{
							ref var curColor = ref colorArray[cellIndex][vertIndex + buffer + alpha * pieceModel.vertexCount];
							curColor = reader.Read<Color32>();
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
				if(mapStrat.alienFirst != null)
				{
					WaypointPC curAlien = mapStrat.alienFirst;
					while(curAlien.Next!=null)
					{
						curAlien = curAlien.Next;
					}
					mapStrat.alienLast = curAlien;
				}
			}
			map.MapStrats = mapStrats;

			if((wadFlags & WadFlagsPC.WAD_FLAG_200000) != 0)
			{
				map.PolygonArraysCount = reader.Read<int>();
				map.PolygonArrays = reader.ReadArray<PolygonArray,WadFlagsPC>(wadFlags, map.PolygonArraysCount);
				//map.PolygonArraysCount--;
			}
			if((wadFlags & WadFlagsPC.WAD_FLAG_HAS_PARTICLES) != 0)
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
			var currCell = cell;
			while(info.Pos.Y <= (double)currCell.Piece.Pos.Y)
			{
				if(currCell.Next == null)
				{
					currCell.Next = new MapPieceListPC(info);
					return;
				}
				currCell = currCell.Next;
			}
			if(currCell.Next != null)
			{
				ReadWadChunkMAP_InitCells(currCell.Next, currCell.Piece);
				currCell.Piece = info;
			}
			else
			{
				currCell.Next = new MapPieceListPC(currCell.Piece);
				currCell.Piece = info;
			}
		}
	}
}
