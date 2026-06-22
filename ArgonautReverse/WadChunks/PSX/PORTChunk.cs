using ArgonautReverse.Engine;
using ArgonautReverse.Engine.Versions;
using ArgonautReverse.IO;

namespace ArgonautReverse.WadChunks.PSX
{
	public class PORTChunkInfo:BaseWADChunkInfo<PORTChunk>
	{
		public static readonly PORTChunkInfo Instance = new PORTChunkInfo();

		public override ChunkType ChunkType => ChunkType.ID_PSX_PORT;
		public override WadVersion[] SupportedWadVersions{get;} = [HARRY_POTTER_1_PS1.WadVersion, HARRY_POTTER_2_PS1.WadVersion];
		public override string ChunkDescription => "chunk zone ids";

		public override PORTChunk Parse(WadReader data_in)
		{
			var n_zones = data_in.Read<int>();
			var n_idk1 = data_in.Read<int>();
			var idk1 = new byte[n_idk1][];
			for(int i = 0; i < n_idk1; i++)
			{
				idk1[i] = data_in.ReadArray<byte>(32);
			}
			var n_chunks_per_zone = new byte[n_zones];
			for(int i = 0; i < n_zones; i++)
			{
				data_in.SkipBytes(2);
				n_chunks_per_zone[i] = data_in.Read<byte>();
				data_in.SkipBytes(9);
			}
			var chunks_zones = new int[n_zones][];
			for(int i = 0; i < n_zones; i++)
			{
				var n_chunks = n_chunks_per_zone[i];
				chunks_zones[i] = new int[n_chunks];
				for(int v = 0; v < n_chunks; v++)
				{
					chunks_zones[i][v] = data_in.Read<short>();
				}
			}
			data_in.AssertEndOfChunk(ChunkType);
			return new PORTChunk(idk1, chunks_zones, data_in.GetAllWadData());
		}
	}

	public class PORTChunk(byte[][] idk1, int[][] chunks_zones, byte[]? fallback_data = null):BaseWadChunk(PORTChunkInfo.Instance, fallback_data)
	{
		public readonly byte[][] idk1 = idk1;
		public readonly int[][] chunks_zones = chunks_zones;

		public int size => 8 + 32 * idk1.Length + 12 * n_chunks_zones + 2 * n_chunks;

		public int n_chunks_zones => chunks_zones.Length;

		public int n_chunks => chunks_zones.Sum(zone => zone.Length);

		protected override void WriteData(ChunkWriter writer) => writer.WriteBytes(Data!);
	}
}