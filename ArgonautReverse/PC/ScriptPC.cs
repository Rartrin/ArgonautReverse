using ArgonautReverse.Files;
using ArgonautReverse.IO;
using ArgonautReverse.Universal.StratLang;

namespace ArgonautReverse.PC
{
	public sealed class ScriptPC(WADFile wadFile, int[] data, int dataChunkAddress):Script(wadFile, data, dataChunkAddress),IReadable<ScriptPC>,IWritable
	{
		public static ScriptPC Parse(WadReader reader)
		{
			var length = reader.Read<int>();
			//TODO: Confirm chunks start after size.
			var dataChunkAddress = reader.Position;
			var data = reader.ReadArray<int>(length);
			return new(reader.WadFile, data, dataChunkAddress);
		}

		public void Write(WadWriter writer)
		{
			writer.Write<int>(Data.Length);
			writer.WriteArray<int>(Data);
		}
	}
}