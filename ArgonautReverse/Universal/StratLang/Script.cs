using ArgonautReverse.Files;
using ArgonautReverse.Universal.StratLang.Disassembler;

namespace ArgonautReverse.Universal.StratLang
{
	public abstract class Script
	{
		public readonly int DataChunkAddress;
		public readonly int DataChunkLength;

		public readonly int[] Data;

		public readonly List<int> EntryPointAddrs = new List<int>();

		public IReadOnlyList<AsmInstruction> EntryPoints;

		public readonly StratParser Parser;
		public bool Processed = false;

		public bool Failed = false;

		public Script(WADFile wadFile, int[] data, int dataChunkAddress)
		{
			Data = data;
			DataChunkAddress = dataChunkAddress;
			DataChunkLength = data.Length * sizeof(int);

			Parser = new StratParser(wadFile, this);
		}

		public bool ProcessScript()
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
				//This needs to be a list in case new stuff gets added while iterating.
				var entryPoints = new List<AsmInstruction>();
				for(int i=0; i<EntryPointAddrs.Count; i++)
				{
					entryPoints.Add(Parser.ParseAndSetup((InstructionAddress)EntryPointAddrs[i]));
				}
				EntryPoints = entryPoints;
				Processed = true;
				return true;
			}
			catch(Exception e)
			{
				Console.WriteLine($"Script failed: {e}");
				Failed = true;
				return false;
			}
		}

		public bool ExportAsm(string baseFilePath)
		{
			try
			{
				if(Failed || !Processed)
				{
					Console.WriteLine($"Failed or no script to export {baseFilePath}:");
					return false;
				}
				var writer = new Decompiler.Writer();
				Parser.Write(writer);
				File.WriteAllText($"{baseFilePath}.asm.strat", writer.GetString());
				return true;
			}
			catch(Exception e)
			{
				Console.WriteLine($"Failed to export ASM {baseFilePath}:");
				Console.WriteLine(e.Message);
				return false;
			}
		}
	}
}