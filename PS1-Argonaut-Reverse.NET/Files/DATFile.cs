using ArgonautReverse.IO;

namespace ArgonautReverse.Files
{
	public abstract class DATFile
	{
		//public static string suffix;
		protected readonly byte[] _data;
		public string Suffix{get;}
		public string Stem{get;}

		public string Name => $"{Stem}.{Suffix}";

		public DATFile(string stem, string suffix = null, byte[] data = null)
		{
			if(data is not null)
			{
				this._data = data;
			}

			this.Suffix = suffix;//??DATFile.suffix;

			if(stem.Length > 8 || this.Suffix.Length > 3)
			{
				throw new Exception("The engine uses \"8.3 filenames\" (8-characters stem, dot then 3-characters extension), please use a compatible filename.");
			}
			this.Stem = stem;
		}
	
		public override string ToString() => "(?) Unknown file";

		public virtual void Parse(Configuration conf){}

		public virtual void Serialize(Serializer data_out, Configuration conf)
		{
			data_out.WriteBytes(this._data);
		}
	}
}