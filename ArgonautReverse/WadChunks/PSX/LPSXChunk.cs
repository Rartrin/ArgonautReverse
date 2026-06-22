using System.Text;
using ArgonautReverse.Engine;
using ArgonautReverse.Engine.Versions;
using ArgonautReverse.IO;

namespace ArgonautReverse.WadChunks.PSX
{
	public sealed class LPSXChunkInfo:BaseWADChunkInfo<LPSXChunk>
	{
		public static readonly LPSXChunkInfo Instance = new LPSXChunkInfo();

		public override ChunkType ChunkType => ChunkType.ID_PSX_LANG;
		public override WadVersion[] SupportedWadVersions{get;} = [HARRY_POTTER_1_PS1.WadVersion, HARRY_POTTER_2_PS1.WadVersion];
		public override string ChunkDescription => "Language";

		public override LPSXChunk Parse(WadReader reader)
		{
			//Very similar to LGPC

			var lastLanguage = reader.Read<int>();
			var languageCount = lastLanguage + 1;//?

			var languageStringCount = reader.Read<int>();

			//This should be the length of the string section (data following the length arrays). Length is number of int32s, not bytes.
			//This is used by PSX but not on PC. It also doesn't appear to be exactly correct here.
			_ = reader.Read<int>();

			var languageStrings = new string[languageStringCount][];
			var languageStringOffsets = new int[languageStringCount][];
			for(int i=0; i < languageStringCount; i++)
			{
				languageStringOffsets[i] = reader.ReadArray<int>(languageCount);
			}
			int offset = 0;
			var builder = new StringBuilder();
			for(int i = 0; i < languageStringCount; i++)
			{
				languageStrings[i] = new string[languageCount];
				for(int j = 0; j < languageCount; j++)
				{
					int neededOffset = languageStringOffsets[i][j] >> 1;
					//while(offset < neededOffset)
					//{
					//	reader.AssertRead<char>('\0');
					//	offset++;
					//}
					if(neededOffset != offset)
					{
						throw new Exception("Invalid offset");
					}

					//Strings are null terminated UTF16 but offset by 1.

					builder.Clear();
					while(true)
					{
						char c = reader.Read<char>();
						if(c == '\0')
						{
							break;
						}
						builder.Append((char)(c+1));
					}
					languageStrings[i][j] = builder.ToString();
					offset += builder.Length + 1;//+1 for null terminator.

				}
			}
			while(reader.Remaining > 0)
			{
				reader.AssertRead<char>('\0');
			}
			reader.AssertEndOfChunk(ChunkType);
			return new(languageStrings, reader.GetAllWadData());
		}
	}
	public sealed class LPSXChunk(string[][] languageStrings, byte[]? data = null):BaseWadChunk(LPSXChunkInfo.Instance, data)
	{
		public readonly string[][] LanguageStrings = languageStrings;

		protected override void WriteData(ChunkWriter writer)
		{
			var lastLanguage = LanguageStrings.Length - 1;
			writer.Write<int>(lastLanguage);

			writer.Write<int>(LanguageStrings[0].Length);
			writer.Write<int>(0);//See above note.

			int offset = 0;
			for(int i=0; i < LanguageStrings.Length; i++)
			{
				for(int j = 0; j < LanguageStrings[i].Length; j++)
				{
					writer.Write<int>(2*offset);
					offset += LanguageStrings[i][j].Length + 1;//+1 for null terminator.
				}
			}
			for(int i=0; i < LanguageStrings.Length; i++)
			{
				for(int j = 0; j < LanguageStrings[i].Length; j++)
				{
					var languageString = LanguageStrings[i][j];
					for(int k=0; k<languageString.Length; k++)
					{
						writer.Write((char)(languageString[k] + 1));
					}
					writer.Write<char>('\0');//Strings are null terminated
				}
			}
		}
	}
}