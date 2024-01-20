using ArgonautReverse.IO;

namespace ArgonautReverse.Files
{
	public abstract class DATFile
	{
		protected readonly byte[] _data;
		public string Stem{get;}

		public string Name => $"{Stem}.{Suffix}";

		public abstract string Suffix{get;}

		public DATFile(string stem, byte[] data = null)
		{
			this._data = data;

			if(stem.Length > 8)
			{
				throw new Exception("The engine uses \"8.3 filenames\" (8-characters stem, dot then 3-characters extension), please use a compatible filename.");
			}
			this.Stem = stem;
		}
	
		public override string ToString() => "(?) Unknown file";

		public virtual void Parse(Configuration conf){}

		public virtual void Serialize(Serializer data_out)
		{
			data_out.WriteBytes(this._data);
		}
	}
}