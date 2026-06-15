using ArgonautReverse.Engine;
using ArgonautReverse.IO;
using ArgonautReverse.PC;

namespace ArgonautReverse.WadChunks.PC
{
	public sealed class AMPCChunkInfo:BaseWADChunkInfo<AMPCChunk>
	{
		public static readonly AMPCChunkInfo Instance = new AMPCChunkInfo();

		public override ChunkType ChunkType => ChunkType.ID_PC_AMPC;
		public override string ChunkDescription => "Audio MIDI data";
		public override WadVersion[] SupportedWadVersions{get;} = Configuration.ParsableWadsPC;

		public override AMPCChunk Parse(WadReader reader)
		{
			var adsResourceCount = reader.Read<int>();
			var adsResources = reader.ReadArray<ADSResourceAMPC>(adsResourceCount);

			var midiCount = reader.Read<int>();
			var midis = reader.ReadArray<MidiStruct>(midiCount);

			reader.AssertEndOfChunk(ChunkType);
			return new(adsResources, midis);
		}
	}
	public sealed class AMPCChunk(ADSResourceAMPC[] adsResources, MidiStruct[] midis):BaseWadChunk(AMPCChunkInfo.Instance)
	{
		public readonly ADSResourceAMPC[] Resources = adsResources;
		public readonly MidiStruct[] MIDIs = midis;

		protected override void WriteData(ChunkWriter writer)
		{
			writer.Write<int>(Resources.Length);
			writer.WriteArray(Resources);

			writer.Write<int>(MIDIs.Length);
			writer.WriteArray(MIDIs);
		}
	}
}