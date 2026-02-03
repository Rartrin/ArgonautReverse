using ArgonautReverse.IO;
using ArgonautReverse.Universal;
using ArgonautReverse.WadChunks.PC;

namespace ArgonautReverse.PC
{
	public sealed class MapPiecePC(in Vector3F pos, int rawRotY, float rotY, int n, int cellIndex, bool hasOtherPiece)//WorldCellInfo
	{
		public Vector3F Pos = pos;
		public int RawRotY = rawRotY;//Value is 0-1 in 24bit (0 to 0xFF0). Also, the lowest nibble is ignored.
		public float RotY = Utils.Deg2Rad(rotY);
		public int N = n, CellIndex = cellIndex;//Same field. Alladin uses n, everything else uses CellIndex. The values are not the same either.
		public bool bVisible = false;
		public int gapField6;
		public int field7 = 0;

		public bool HasOtherPiece = hasOtherPiece;
	}

	public sealed class MapPieceListPC(MapPiecePC piece)//WorldCell
	{
		public MapPiecePC Piece = piece;
		public MapPieceListPC? Next;
	}

	[Flags]
	public enum MapFlagsPC:uint
	{
		MAP_FLAG_REMOVED = 0x2,
		MAP_FLAG_COLLECTED = 0x4,
		MAP_FLAG_ACTIVATED = 0x8,
	}

	public sealed class MapStratPC:IReadable<MapStratPC,MapPC>,IWritable
	{
		public RotPos3I RotPos;

		public ScriptPC Instructions;
		public int instructionStreamOffset;
		
		//public int NumberParameters;
		public ArraySegment<int>? ParamBlock;
		public int LocalCount;
		public int TriggerCount;
		public CollisionTypePC Collision;
		public int CollisionBoneCount;
		public WaypointPC FirstWP;
		public int FirstWPIndex;
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
			mapStrat.instructionStreamOffset = reader.Read<int>();//Add to data.fileData (STPC chunk) to get the address of the instructionStream;

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
			mapStrat.FirstWPIndex = reader.Read<int>();
			if(mapStrat.FirstWPIndex == -1)
			{
				mapStrat.FirstWP = null;
			}
			else
			{
				mapStrat.FirstWP = map.WaypointList[mapStrat.FirstWPIndex];
			}
			if(mapStrat.FirstWPIndex >= map.WaypointList.Count)
			{
				Console.WriteLine("Erruuughh...");
			}
			reader.AssertRead<uint>(0);//was a LastWP placeholder originally I think
			mapStrat.flags = (MapFlagsPC)reader.Read<uint>();

			mapStrat.Instructions = reader.WadFile.GetChunk(STPCChunkInfo.Instance).GetScript(mapStrat.instructionStreamOffset);

			return mapStrat;
		}

		public void Write(WadWriter writer)
		{
			writer.Write<Vector3<ushort>>(new((ushort)RotPos.Rotation.X, (ushort)RotPos.Rotation.Y, (ushort)RotPos.Rotation.Z));

			writer.Write(RotPos.Position);
			writer.Write<int>(instructionStreamOffset);

			
			if(ParamBlock.HasValue)
			{
				writer.Write<int>(ParamBlock.Value.Count);//TODO: Determine the value of this when ptrOffest is -1
				writer.Write<int>(ParamBlock.Value.Offset);
			}
			else
			{
				ParamBlock = null;
				writer.Write<int>(0);//TODO: Determine the value of this when ptrOffest is -1
				writer.Write<int>(-1);
			}
			writer.Write<int>(LocalCount);
			writer.Write<int>(TriggerCount);
			writer.Write((uint)Collision);
			writer.Write<int>(CollisionBoneCount);
			writer.Write<int>(FirstWPIndex);
			writer.Write<uint>(0);//was a LastWP placeholder originally I think
			writer.Write((uint)flags);
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

	public readonly struct ZonePC(uint zone):IReadable<ZonePC>,IWritable
	{
		public readonly uint Zone = zone;
		//public uint ViewZone;

		public static ZonePC Parse(WadReader reader)
		{
			var zone = reader.Read<uint>();
			return new ZonePC(zone);
		}

		public void Write(WadWriter writer)
		{
			writer.Write<uint>(Zone); 
		}
	}

	public sealed class TrackChangePC(Vector3I pos, int normalPiece, int otherPiece, MapPiecePC pieceMem):IReadable<TrackChangePC,MapPiecePC>,IWritable//PieceStruct
	{
		public Vector3I Pos = pos;
		public int NormalPiece = normalPiece;//field0;
		public int OtherPiece = otherPiece;//modelIndex
		public MapPiecePC PieceMem = pieceMem;//cellInfo

		public static TrackChangePC Parse(WadReader reader, MapPiecePC pieceMem)
		{
			var pos = reader.Read<Vector3I>();
			var normalPiece = reader.Read<int>();
			var otherPiece = reader.Read<int>();
			return new TrackChangePC(pos, normalPiece, otherPiece, pieceMem);
		}

		public void Write(WadWriter writer)
		{
			writer.Write(Pos);
			writer.Write<int>(NormalPiece);
			writer.Write<int>(OtherPiece);
		}
	}

	public sealed class MapPC:IReadable<MapPC>,IWritable
	{
		//public StratObjectPC Background;

		public IReadOnlyList<MapPiecePC> AllMapPieces;
		public IReadOnlyList<IReadOnlyList<MapPieceListPC>> MapPieceArray;
		public int NumPieces;
		public int MapWidth;
		public int MapHeight;

		//public StrategyList strategy_list;

		public IReadOnlyList<ZonePC> ZoneData;
		public IReadOnlyList<RotPos3Fx> Positions;
		public IReadOnlyList<int> ModelIndices;

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

		public ColorBGRA32[][] ColorArray;

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
			var allMapPieces = new List<MapPiecePC>();
			for(int cellIndex=0; cellIndex<map.NumPieces; cellIndex++)
			{
				var chunkPos = reader.Read<Vector3F>();
				var rotY = reader.Read<int>();
				var n = reader.Read<int>();
				bool hasOtherPiece = reader.Read<int>() switch
				{
					0 => false,
					1 => true,
					_ => throw new Exception()
				};
				// Conversion from 0.0-1.0 angle in fixed point 24bit to floating point degrees.
				var worldCellInfo = new MapPiecePC(chunkPos, rawRotY:rotY, rotY:((rotY&0xFF0) * 360f) / 4096f, n:n, cellIndex:cellIndex, hasOtherPiece);
				allMapPieces.Add(worldCellInfo);

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
			map.AllMapPieces = allMapPieces;
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
			map.ModelIndices = reader.ReadArray<int>(map.NumPieces);
			if((wadFlags & WadFlagPC.WAD_FLAG_HAS_CHANGING_GEOMETRY) != 0)
			{
				var numberOfOtherPieces = reader.Read<int>();
				map.TrackChangeData = reader.ReadArray<TrackChangePC,MapPiecePC>(dataPos, numberOfOtherPieces);
			}
			else
			{
				map.TrackChangeData = [];
			}
			var colorArray = new ColorBGRA32[map.NumPieces][];
			var trackModels = reader.WadFile.GetChunk(TRAKChunkInfo.Instance).Models;
			for(int cellIndex=0; cellIndex<map.NumPieces; cellIndex++)
			{
				var cellModel = trackModels[map.ModelIndices[cellIndex]];
				byte maxAlpha = 1;
				colorArray[cellIndex] = reader.ReadArray<ColorBGRA32>(cellModel.vertices.Length);
				for(int vertIndex=0; vertIndex<cellModel.vertices.Length; vertIndex++)
				{
					var curAlpha = colorArray[cellIndex][vertIndex].A;
					if(curAlpha > maxAlpha)
					{
						maxAlpha = curAlpha;
					}
				}
				if(maxAlpha > 1)
				{
					Array.Resize(ref colorArray[cellIndex], cellModel.vertices.Length * maxAlpha);

					colorArray[cellIndex][0].A = maxAlpha;
					for(int alpha=1; alpha<maxAlpha; alpha++)
					{
						reader.ReadArray(colorArray[cellIndex].AsSpan(alpha * cellModel.vertices.Length, cellModel.vertices.Length));
					}
				}
				int buffer = cellModel.vertices.Length*maxAlpha;
				foreach(var trackChange in map.TrackChangeData)
				{
					if(trackChange.PieceMem.CellIndex != cellIndex){continue;}

					var pieceModel = trackModels[trackChange.OtherPiece];

					Array.Resize(ref colorArray[cellIndex], colorArray[cellIndex].Length + pieceModel.vertices.Length*maxAlpha);

					for(int vertIndex=0; vertIndex<pieceModel.vertices.Length; vertIndex++)
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
						for(int vertIndex=0; vertIndex<pieceModel.vertices.Length; vertIndex++)
						{
							ref var curColor = ref colorArray[cellIndex][vertIndex + buffer + alpha * pieceModel.vertices.Length];
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

		public void Write(WadWriter writer)
		{
			var wadFlags = writer.WadFile.GetChunk(WFPCChunkInfo.Instance).WadFlags;

			writer.Write<int>(NumPieces);
			writer.Write<int>(MapWidth);
			writer.Write<int>(MapHeight);
			for(int cellIndex=0; cellIndex<NumPieces; cellIndex++)
			{
				var worldCellInfo = AllMapPieces[cellIndex];
				writer.Write(worldCellInfo.Pos);
				writer.Write<int>(worldCellInfo.RawRotY);
				writer.Write<int>(worldCellInfo.N);
				writer.Write<int>(worldCellInfo.HasOtherPiece ? 1 : 0);
			}

			writer.Write<int>(Params.Count);
			writer.WriteArray<int>(Params);

			writer.Write<int>(DoorList.Count);
			writer.WriteArray<DoorPC>(DoorList);

			WaypointPC.WriteWaypoints(writer, WaypointList);

			writer.WriteSizedArray(32, WaterLevelArray);

			for(int m = 0; m < MapHeight; ++m)
			{
				for(int n = 0; n < MapWidth; ++n)
				{
					writer.Write(ZoneData[n + m * MapWidth]);
				}
			}
			writer.WriteSizedArray(NumPieces, Positions);
			writer.WriteSizedArray<int>(NumPieces, ModelIndices);
			if((wadFlags & WadFlagPC.WAD_FLAG_HAS_CHANGING_GEOMETRY) != 0)
			{
				writer.Write<int>(TrackChangeData.Count);
				writer.WriteArray(TrackChangeData);
			}
			var trackModels = writer.WadFile.GetChunk(TRAKChunkInfo.Instance).Models;
			for(int cellIndex=0; cellIndex<NumPieces; cellIndex++)
			{
				var cellModel = trackModels[ModelIndices[cellIndex]];
				byte maxAlpha = 1;
				for(int vertIndex=0; vertIndex<cellModel.vertices.Length; vertIndex++)
				{
					var curAlpha = ColorArray[cellIndex][vertIndex].A;
					if(curAlpha > maxAlpha)
					{
						maxAlpha = curAlpha;
					}
				}
				//This array can be larger than vertices.Length.
				writer.WriteArray(ColorArray[cellIndex].AsSpan(0, maxAlpha * cellModel.vertices.Length));

				int buffer = cellModel.vertices.Length*maxAlpha;
				foreach(var trackChange in TrackChangeData)
				{
					if(trackChange.PieceMem.CellIndex != cellIndex){continue;}

					var pieceModel = trackModels[trackChange.OtherPiece];

					Array.Resize(ref ColorArray[cellIndex], ColorArray[cellIndex].Length + pieceModel.vertices.Length*maxAlpha);

					for(int vertIndex=0; vertIndex<pieceModel.vertices.Length; vertIndex++)
					{
						var curAlpha = ColorArray[cellIndex][buffer + vertIndex].A;
						if(curAlpha > maxAlpha)
						{
							maxAlpha = curAlpha;
						}
					}
					writer.WriteArray(ColorArray[cellIndex].AsSpan(buffer, maxAlpha * pieceModel.vertices.Length));
					break;
				}
			}
			writer.Write<int>(strArray.Count);
			for(int i=0; i < strArray.Count; i++)
			{
				writer.WriteString(0x30, strArray[i]);
			}

			writer.Write<int>(MapStrats.Count);
			writer.Write<int>(MaxStrats);
			writer.WriteArray(MapStrats);

			if((wadFlags & WadFlagPC.WAD_FLAG_CAMERAPOINTS) != 0)
			{
				writer.Write<int>(PolygonArraysCount);
				if(PolygonArrays.Count != PolygonArraysCount){throw new Exception();}
				writer.WriteArray(wadFlags, PolygonArrays);
			}
			if((wadFlags & WadFlagPC.WAD_FLAG_HAS_PARTICLES) != 0)
			{
				writer.Write<int>(MaxParticles);
			}
			writer.Write<short>(wField2);
		}

		public static void ReadWadChunkMAP_InitCells(MapPieceListPC cell, MapPiecePC info)
		{
			if(cell.Next == null)
			{
				cell.Next = new MapPieceListPC(info);
				return;
			}
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
	}
}