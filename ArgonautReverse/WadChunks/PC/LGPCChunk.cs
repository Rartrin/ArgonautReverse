using ArgonautReverse.Engine;
using ArgonautReverse.IO;

namespace ArgonautReverse.WadChunks.PC
{
	public sealed class LGPCChunkInfo:BaseWADChunkInfo<LGPCChunk>
	{
		public static readonly LGPCChunkInfo Instance = new();

		public override WadVersion[] SupportedWadVersions => Configuration.ParsableWadsPC;
		public override string ChunkDescription => "Language data";
		public override ChunkType ChunkType => ChunkType.ID_PC_LANG;

		private LGPCChunkInfo(){}

		public override LGPCChunk Parse(WadReader reader)
		{
			var lastLanguage = reader.Read<int>();
			var languageCount = lastLanguage + 1;//?

			var languageStringCount = reader.Read<int>();

			//This should be the length of the string section (data following the length arrays). Length is number of int32s, not bytes.
			//This is used by PSX but not on PC. It also doesn't appear to be exactly correct here.
			_ = reader.Read<int>();

			var languageStrings = new string[languageStringCount][];
			var languageStringLengths = new int[languageStringCount][];
			int[] totalCharacters = new int[languageCount];
			for(int i=0; i < languageStringCount; i++)
			{
				languageStringLengths[i] = reader.ReadArray<int>(languageCount);
			}
			for(int i = 0; i < languageStringCount; i++)
			{
				languageStrings[i] = new string[languageCount];
				for(int j = 0; j < languageCount; j++)
				{
					totalCharacters[j] += languageStringLengths[i][j];
					//Strings are null terminated
					languageStrings[i][j] = reader.ReadString(languageStringLengths[i][j]).TrimEnd('\0');

				}
			}
			reader.AssertEndOfChunk(ChunkType);
			return new(this, languageStrings, reader.GetAllWadData());
		}
	}
	public sealed class LGPCChunk(BaseWADChunkInfo info, string[][] languageStrings, byte[]? data = null):BaseWadChunk(info, data)
	{
		public readonly string[][] LanguageStrings = languageStrings;

		protected override void WriteData(ChunkWriter writer)
		{
			
			var lastLanguage = LanguageStrings.Length - 1;
			writer.Write<int>(lastLanguage);

			writer.Write<int>(LanguageStrings[0].Length);
			writer.Write<int>(0);//See above note.

			for(int i=0; i < LanguageStrings.Length; i++)
			{
				for(int j = 0; j < LanguageStrings[i].Length; j++)
				{
					writer.Write<int>(LanguageStrings[i][j].Length);
				}
			}
			for(int i=0; i < LanguageStrings.Length; i++)
			{
				for(int j = 0; j < LanguageStrings[i].Length; j++)
				{
					var languageString = LanguageStrings[i][j];
					writer.WriteString(languageString.Length, languageString);
					writer.Write((byte)'\0');//Strings are null terminated
				}
			}
		}
	}
}
