using ArgonautReverse.Engine;
using ArgonautReverse.IO;
using ArgonautReverse.PC;
using ArgonautReverse.Universal;

namespace ArgonautReverse.WadChunks.PC
{
	public sealed class LGHTChunkInfo:BaseWADChunkInfo<LGHTChunk>
	{
		public static readonly LGHTChunkInfo Instance = new LGHTChunkInfo();

		public override ChunkType ChunkType => ChunkType.ID_PC_LIGHT;
		public override string ChunkDescription => "Light data";
		public override WadVersion[] SupportedWadVersions{get;} = Configuration.ParsableWadsPC;

		public override LGHTChunk Parse(WadReader reader)
		{
			var lightCount = reader.Read<int>();
			var lights = reader.ReadArray<LightPC>(lightCount);
			reader.AssertEndOfChunk(ChunkType);
			return new(this, lights, reader.GetAllWadData());
		}
	}
	public sealed class LGHTChunk(BaseWADChunkInfo info, IReadOnlyList<LightPC> lights, byte[] data):BaseWadChunk(info, data)
	{
		public readonly IReadOnlyList<LightPC> Lights = lights;

		protected override void WriteData(ChunkWriter writer)
		{
			writer.Write<int>(Lights.Count);
			writer.WriteArray<LightPC>(Lights);
		}
	}
}