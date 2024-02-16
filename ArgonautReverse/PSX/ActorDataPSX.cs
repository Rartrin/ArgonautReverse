using ArgonautReverse.Files;
using ArgonautReverse.IO;
using ArgonautReverse.PSX.StratLang;
using ArgonautReverse.Universal.StratLang;

namespace ArgonautReverse.PSX
{
	public sealed class ActorDataPSX
	{
		public readonly int DataChunkAddress;
		public readonly int DataChunkLength;

		public readonly int[] data;

		public readonly List<int> EntryPointAddrs = new List<int>();

		public IReadOnlyList<Instruction> EntryPoints;

		private StratParser parser;

		public bool Failed = false;

		public ActorDataPSX(int[] data, int dataChunkAddress)
		{
			this.data = data;
			DataChunkAddress = dataChunkAddress;
			DataChunkLength = data.Length * sizeof(int);
		}

		public int Size => data.Length;

		public static ActorDataPSX Parse(WadReader data_in)
		{
			var size = data_in.Read<int>();
			//Chunks start after size
			var dataChunkAddress = data_in.Position;
			return new ActorDataPSX(data_in.ReadArray<int>(size), dataChunkAddress);
		}

		public bool ProcessScript(WadFilePSX wadFile)
		{
			if(Failed)
			{
				return false;
			}
			if(EntryPointAddrs.Count == 0)
			{
				return false;
			}
			if(EntryPoints != null && EntryPoints.Count == EntryPointAddrs.Count)
			{
				return false;
			}
			try
			{
				var stratParser = new StratParser(wadFile, this);

				//This needs to be a list in case new stuff gets added while iterating.
				var entryPoints = new List<Instruction>();
				for(int i=0; i<EntryPointAddrs.Count; i++)
				{
					entryPoints.Add(stratParser.ParseAndSetup((InstructionAddress)EntryPointAddrs[i]));
				}
				EntryPoints = entryPoints;
				parser = stratParser;
				return true;
			}
			catch(Exception e)
			{
				Console.WriteLine("Script failed: " + e);
				Failed = true;
				return false;
			}
		}

		public void Write(string path)
		{
			if(Failed){return;}

			parser?.Write(path, false);
		}
	}
}