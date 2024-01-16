using ArgonautReverse.Engine;
using ArgonautReverse.Engine.Versions;
using ArgonautReverse.IO;

namespace ArgonautReverse.WadSections
{
	public class PORTSectionInfo:BaseWADSectionInfo<PORTSection>
	{
		public static readonly PORTSectionInfo Instance = new PORTSectionInfo();

		public override ChunkType ChunkType => ChunkType.ID_PORT;
		public override WadVersion[] supported_games{get;} = new WadVersion[]{HARRY_POTTER_1_PS1.WadVersion, HARRY_POTTER_2_PS1.WadVersion};
		public override string section_content_description => "chunk zone ids";

		public override PORTSection Parse(WadReader data_in)
		{
			var fallback_data = fallback_parse_data(data_in);
			base.parseInner(data_in, out var size, out var start);
			var n_zones = data_in.ReadInt32();
			var n_idk1 = data_in.ReadInt32();
			var idk1 = new byte[n_idk1][];
			for(int i=0; i<n_idk1; i++)
			{
				idk1[i] = data_in.ReadBytes(32);
			}
			var n_chunks_per_zone = new byte[n_zones];
			for(int i=0; i<n_zones; i++)
			{
				data_in.Seek(2, SeekOrigin.Current);
				n_chunks_per_zone[i] = data_in.ReadByte();
				data_in.Seek(9, SeekOrigin.Current);
			}
			var chunks_zones = new int[n_zones][];
			for(int i=0; i<n_zones; i++)
			{
				var n_chunks = n_chunks_per_zone[i];
				chunks_zones[i] = new int[n_chunks];
				for(int v=0; v<n_chunks; v++)
				{
					chunks_zones[i][v] = data_in.ReadInt16();
				}
			}
			check_size(size, start, data_in.Position);
			return new PORTSection(idk1, chunks_zones, fallback_data);
		}
	}

	public class PORTSection:BaseWADSection
	{
		public readonly byte[][] idk1;
		public readonly int[][] chunks_zones;
		public PORTSection(byte[][] idk1, int[][] chunks_zones, byte[] fallback_data = null):base(PORTSectionInfo.Instance, fallback_data)
		{
				this.idk1 = idk1;
				this.chunks_zones = chunks_zones;
		}

		public int size => 8 + 32 * this.idk1.Length + 12 * this.n_chunks_zones + 2 * this.n_chunks;

		public int n_chunks_zones => this.chunks_zones.Length;

		public int n_chunks => this.chunks_zones.Sum(zone => zone.Length);
	}
}