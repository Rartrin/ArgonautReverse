namespace ArgonautReverse.IO
{
	public delegate T DelRead<T>(WadReader reader);
	public delegate T DelRead<T,A>(WadReader reader, A arg);

	public delegate void DelReadInto<T>(WadReader reader, T instance);

	public interface IReadable<T>
	{
		public static abstract T Parse(WadReader reader);
	}

	public interface IReadable<T,A>
	{
		public static abstract T Parse(WadReader reader, A arg);
	}

	//Generally used when an list uses the file data space to store itself and has additional data following it.
	public interface IReadableArrayMultipass<T> where T:class
	{
		public static abstract T ParseStruct(WadReader reader);
		public static abstract void ParseData(WadReader reader, T instance);
	}

	//These are separate to prevent conflict with methods only differing by generic arguments
	public static class ReaderExtensions
	{
		#region IReadable<T>
		public static T Read<T>(this WadReader that) where T : IReadable<T>
		{
			return T.Parse(that);
		}

		public static T[] ReadArray<T>(this WadReader that, int length) where T : IReadable<T>
		{
			if(length == 0){return Array.Empty<T>();}

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
		#region IReadable<T,A>
		public static T Read<T,A>(this WadReader that, A arg) where T : IReadable<T,A>
		{
			return T.Parse(that, arg);
		}

		public static T[] ReadArray<T,A>(this WadReader that, A arg, int length) where T : IReadable<T,A>
		{
			if(length == 0){return Array.Empty<T>();}

			var array = new T[length];
			that.ReadArray<T,A>(arg, array);
			return array;
		}
		public static T[] ReadArray<T,A>(this WadReader that, IReadOnlyList<A> args, int length) where T : IReadable<T,A>
		{
			if(length == 0){return Array.Empty<T>();}

			var array = new T[length];
			that.ReadArray<T,A>(args, array);
			return array;
		}

		public static void Read<T,A>(this WadReader that, A arg, out T value) where T : IReadable<T,A>
		{
			value = T.Parse(that, arg);
		}

		public static void ReadArray<T,A>(this WadReader that, A arg, Span<T> array) where T : IReadable<T,A>
		{
			for(int i = 0; i < array.Length; i++)
			{
				array[i] = T.Parse(that, arg);
			}
		}
		public static void ReadArray<T,A>(this WadReader that, IReadOnlyList<A> args, Span<T> array) where T : IReadable<T,A>
		{
			for(int i = 0; i < array.Length; i++)
			{
				array[i] = T.Parse(that, args[i]);
			}
		}
		#endregion
		#region IReadableMultipass<T>
		public static IReadOnlyList<T> ReadArrayMultipass<T>(this WadReader that, int length) where T:class,IReadableArrayMultipass<T>
		{
			if(length == 0){return Array.Empty<T>();}

			var array = new T[length];
			for(int i = 0; i < length; i++)
			{
				array[i] = T.ParseStruct(that);
			}
			for(int i = 0; i < length; i++)
			{
				T.ParseData(that, array[i]);
			}
			return array;
		}
		public static IReadOnlyList<T> ReadArrayWithoutMultipass<T>(this WadReader that, int length) where T:class,IReadableArrayMultipass<T>
		{
			if(length == 0){return Array.Empty<T>();}

			var array = new T[length];
			for(int i = 0; i < length; i++)
			{
				array[i] = T.ParseStruct(that);
				T.ParseData(that, array[i]);
			}
			return array;
		}
		#endregion

		#region DelRead<T>
		public static T Read<T>(this WadReader that, DelRead<T> read)
		{
			return read(that);
		}

		public static T[] ReadArray<T>(this WadReader that, DelRead<T> read, int length)
		{
			if(length == 0){return Array.Empty<T>();}

			var array = new T[length];
			that.ReadArray(read, array);
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
		#region DelReadInto<T>
		public static void ReadInto<T>(this WadReader that, DelReadInto<T> readInto, T instance)
		{
			readInto(that, instance);
		}

		public static void ReadIntoArray<T>(this WadReader that, DelReadInto<T> readInto, IReadOnlyList<T> array) where T : class
		{
			for(int i = 0; i < array.Count; i++)
			{
				readInto(that, array[i]);
			}
		}
		#endregion
		#region DelRead<T,A>
		public static T Read<T,A>(this WadReader that, DelRead<T,A> read, A arg)
		{
			return read(that, arg);
		}

		public static T[] ReadArray<T,A>(this WadReader that, DelRead<T,A> read, A arg, int length)
		{
			if(length == 0){return Array.Empty<T>();}

			var array = new T[length];
			that.ReadArray(read, arg, array);
			return array;
		}
		public static T[] ReadArray<T,A>(this WadReader that, DelRead<T,A> read, IReadOnlyList<A> args, int length)
		{
			if(length == 0){return Array.Empty<T>();}

			var array = new T[length];
			that.ReadArray(read, args, array);
			return array;
		}

		public static void Read<T,A>(this WadReader that, DelRead<T,A> read, A arg, out T value)
		{
			value = read(that, arg);
		}

		public static void ReadArray<T,A>(this WadReader that, DelRead<T,A> read, A arg, Span<T> array)
		{
			for(int i = 0; i < array.Length; i++)
			{
				array[i] = read(that, arg);
			}
		}
		public static void ReadArray<T,A>(this WadReader that, DelRead<T,A> read, IReadOnlyList<A> args, Span<T> array)
		{
			for(int i = 0; i < array.Length; i++)
			{
				array[i] = read(that, args[i]);
			}
		}
		#endregion
	}
}
