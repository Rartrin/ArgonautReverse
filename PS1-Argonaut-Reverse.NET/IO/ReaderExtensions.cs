namespace ArgonautReverse.IO
{
	public delegate T DelRead<T>(WadReader reader);

	public interface IReadable<T>
	{
		public static abstract T Parse(WadReader reader);
	}

	//These are separate to prevent conflict with methods only differing by generic arguments
	public static class ReaderExtensions
	{
		#region IReadable
		public static T Read<T>(this WadReader that) where T : IReadable<T>
		{
			return T.Parse(that);
		}

		public static T[] ReadArray<T>(this WadReader that, int length) where T : IReadable<T>
		{
			var array = new T[length];
			for(int i = 0; i < length; i++)
			{
				array[i] = T.Parse(that);
			}
			return array;
		}

		public static void Read<T>(this WadReader that, out T value) where T : IReadable<T>
		{
			value = T.Parse(that);
		}

		public static void ReadArray<T>(this WadReader that, Span<T> array) where T : IReadable<T>
		{
			for(int i = 0; i < array.Length; i++)
			{
				array[i] = T.Parse(that);
			}
		}
		#endregion

		#region Lambda Methods
		public static T Read<T>(this WadReader that, DelRead<T> read)
		{
			return read(that);
		}

		public static T[] ReadArray<T>(this WadReader that, DelRead<T> read, int length)
		{
			var array = new T[length];
			for(int i = 0; i < length; i++)
			{
				array[i] = read(that);
			}
			return array;
		}

		public static void Read<T>(this WadReader that, DelRead<T> read, out T value)
		{
			value = read(that);
		}

		public static void ReadArray<T>(this WadReader that, DelRead<T> read, Span<T> array)
		{
			for(int i = 0; i < array.Length; i++)
			{
				array[i] = read(that);
			}
		}
		#endregion
	}
}
