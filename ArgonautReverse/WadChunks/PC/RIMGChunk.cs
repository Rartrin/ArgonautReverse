using ArgonautReverse.Engine;
using ArgonautReverse.IO;
using ArgonautReverse.PC;

namespace ArgonautReverse.WadChunks.PC
{
	public sealed class RIMGChunkInfo:BaseWADChunkInfo<RIMGChunk>
	{
		public static readonly RIMGChunkInfo Instance = new();

		public override ChunkType ChunkType => ChunkType.ID_PC_RIMG;
		public override string ChunkDescription => "RIMG";
		public override WadVersion[] SupportedWadVersions{get;} = Configuration.PC_PARSABLE_WADS;

		public override RIMGChunk Parse(WadReader reader)
		{
			var surfaceCount = reader.Read<int>();
			var surfaces = reader.ReadArray<SurfacePC>(surfaceCount);
			reader.AssertEndOfChunk(ChunkType);
			return new(this, surfaces, reader.GetAllWadData());
		}
	}
	public sealed class RIMGChunk(BaseWADChunkInfo info, IReadOnlyList<SurfacePC> surfaces, byte[] data):BaseWadChunk(info, data)
	{
		public readonly IReadOnlyList<SurfacePC> Surfaces = surfaces;

		protected override void WriteData(ChunkWriter writer)
		{
			writer.Write<int>(Surfaces.Count);
			writer.WriteArray<SurfacePC>(Surfaces);
		}

		public void Export(WadFilePC wadFile, ProgramArgs args, Configuration config)
		{
			if(!args.ExtractTextures){return;}

			//var textureDirectory = args.GetExtractDirectory(wadFile.Stem, "RIMG");
			//for(int i=0; i<Surfaces.Count; i++)
			//{
			//	var texture = Surfaces[i];
			//	//TODO: Extract RIMG
			//}
		}
	}
}