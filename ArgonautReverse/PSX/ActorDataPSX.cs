using ArgonautReverse.Files;
using ArgonautReverse.IO;
using ArgonautReverse.Universal.StratLang;

namespace ArgonautReverse.PSX
{
	public sealed class ActorDataPSX(WADFile wadFile, int[] data, int dataChunkAddress):Script(wadFile, data, dataChunkAddress),IReadable<ActorDataPSX>,IWritable
	{
		public static ActorDataPSX Parse(WadReader data_in)
		{
			var size = data_in.Read<int>();
			//Chunks start after size
			var dataChunkAddress = data_in.Position;
			var data = data_in.ReadArray<int>(size);
			return new(data_in.WadFile, data, dataChunkAddress);
		}

		void IWritable.Write(WadWriter writer)
		{
			writer.Write<int>(Data.Length);
			writer.WriteArray<int>(Data);
		}
	}
}