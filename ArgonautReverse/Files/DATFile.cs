using ArgonautReverse.IO;

namespace ArgonautReverse.Files
{
	public abstract class DATFile
	{
		protected readonly byte[] _data;
		public string Stem{get;}

		public string Name => $"{Stem}.{Suffix}";

		public abstract string Suffix{get;}

		public DATFile(string stem, byte[] data)
		{
			this._data = data;

			if(stem.Length > 8)
			{
				throw new Exception("The engine uses \"8.3 filenames\" (8-characters stem, dot then 3-characters extension), please use a compatible filename.");
			}
			this.Stem = stem;
		}
	
		public virtual void PrintInfo(TextWriter output)
		{
			output.WriteLine("(?) Unknown file");
		}

		public virtual void Parse(ProgramArgs args, Configuration conf){}

		public virtual void ExtractAssets(ProgramArgs args, Configuration conf){}

		public virtual void Serialize(WadWriter data_out)
		{
			data_out.WriteBytes(_data);
		}
	}
}