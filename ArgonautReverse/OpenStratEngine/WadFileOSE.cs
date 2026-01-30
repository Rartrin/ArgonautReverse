using ArgonautReverse.OpenStratEngine.Chunks;

namespace ArgonautReverse.OpenStratEngine
{
	public sealed class WadFileOSE(string stem)
	{
		public readonly string Stem = stem;
		public readonly List<ChunkOSE> Chunks = new List<ChunkOSE>();

		public AnimationChunkOSE AnimationChunk{private set;get => field ?? throw new Exception("Missing Animation chunk");}
		public DoorChunkOSE DoorChunk{private set;get => field ?? throw new Exception("Missing Door chunk");}
		public FontChunkOSE FontChunk{private set;get => field ?? throw new Exception("Missing Font chunk");}
		public MapPieceDataChunkOSE MapPieceDataChunk{private set;get => field ?? throw new Exception("Missing MapPieceData chunk");}
		public WadFlagsChunkOSE WadFlagsChunk{private set;get => field ?? throw new Exception("Missing WadFlags chunk");}
		public WaypointChunkOSE WaypointChunk{private set;get => field ?? throw new Exception("Missing Waypoint chunk");}

		public void AddChunk(ChunkOSE chunk)
		{
			Chunks.Add(chunk);
			switch(chunk.ChunkType)
			{
				case ChunkTypeOSE.Animations:EnsureEmpty(AnimationChunk);AnimationChunk = (AnimationChunkOSE)chunk;break;
				case ChunkTypeOSE.Doors:EnsureEmpty(DoorChunk);DoorChunk = (DoorChunkOSE)chunk;break;
				case ChunkTypeOSE.Fonts:EnsureEmpty(FontChunk);FontChunk = (FontChunkOSE)chunk;break;
				case ChunkTypeOSE.MapPieceData:EnsureEmpty(MapPieceDataChunk);MapPieceDataChunk = (MapPieceDataChunkOSE)chunk;break;
				case ChunkTypeOSE.WadFlags:EnsureEmpty(WadFlagsChunk);WadFlagsChunk = (WadFlagsChunkOSE)chunk;break;
				case ChunkTypeOSE.Waypoints:EnsureEmpty(WaypointChunk);WaypointChunk = (WaypointChunkOSE)chunk;break;
				default:throw new NotImplementedException();
			}

			static void EnsureEmpty(ChunkOSE chunk)
			{
				if(chunk != null)
				{
					throw new Exception("Chunk already exists");
				}
			}
		}
	}
}