namespace ArgonautReverse.Files
{
	public abstract class DATFile
	{
		//public static string suffix;
		protected byte[] _data;
		public string suffix;
		public string stem;

		public string name => $"{stem}.{suffix}";

		public DATFile(string stem, string suffix = null, byte[] data = null)
		{
			if(data is not null)
			{
				this._data = data;
			}

			this.suffix = suffix;//??DATFile.suffix;

			if(stem.Length > 8 || this.suffix.Length > 3)
			{
				throw new Exception("The engine uses \"8.3 filenames\" (8-characters stem, dot then 3-characters extension), please use a compatible filename.");
			}
			this.stem = stem;
		}
	
		public override string ToString() => "(?) Unknown file";

		public virtual void parse(Configuration conf){}

		public void end_parse()
		{
			if(this._data != null)//hasattr(self, "_data")
			{
				this._data = null;//del this._data
			}
		}

		public virtual void serialize(object data_out, Configuration conf)
		{
			if(data_out is string path)
			{
				File.WriteAllBytes(path, this._data);
			}
			else if(data_out is BinaryWriter stream)
			{
				stream.Write(this._data);
			}
			else
			{
				throw new Exception();
			}
		}
	}
}