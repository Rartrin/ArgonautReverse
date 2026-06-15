using ArgonautReverse.Engine;
using ArgonautReverse.IO;
using ArgonautReverse.PC;

namespace ArgonautReverse.WadChunks.PC
{
	public sealed class SPRTChunkInfo:BaseWADChunkInfo<SPRTChunk>
	{
		public static readonly SPRTChunkInfo Instance = new SPRTChunkInfo();

		public override ChunkType ChunkType => ChunkType.ID_PC_SPRITE;
		public override string ChunkDescription => "Sprite data";
		public override WadVersion[] SupportedWadVersions{get;} = Configuration.ParsableWadsPC;

		public override SPRTChunk Parse(WadReader reader)
		{
			var textureIndex = reader.Read<int>();
			int[] array = [];
			var wadFlags = reader.WadFile.GetChunk(WFPCChunkInfo.Instance).WadFlags;
			if((wadFlags & WadFlagPC.WAD_FLAG_100000) != 0)
			{
				int count = reader.Read<int>();
				array = reader.ReadArray<int>(count);
			}
			
			reader.AssertEndOfChunk(ChunkType);
			return new(textureIndex, array);
		}
	}
	public sealed class SPRTChunk(int textureIndex, int[] array):BaseWadChunk(SPRTChunkInfo.Instance)
	{
		public readonly int TextureIndex = textureIndex;
		public readonly int[] Array = array;

		protected override void WriteData(ChunkWriter writer)
		{
			writer.Write<int>(TextureIndex);

			var wadFlags = writer.WadFile.GetChunk(WFPCChunkInfo.Instance).WadFlags;
			if((wadFlags & WadFlagPC.WAD_FLAG_100000) != 0)
			{
				writer.Write<int>(Array.Length);
				writer.WriteArray(Array);
			}
		}
	}
}