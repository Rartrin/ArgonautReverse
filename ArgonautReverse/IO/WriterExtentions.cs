namespace ArgonautReverse.IO
{
	public delegate void DelWrite(WadWriter writer);
	public delegate void DelWrite<A>(WadWriter writer, A arg);

	public delegate void DelWriteFrom<T>(WadWriter writer, T instance);

	public interface IWritable
	{
		public abstract void Write(WadWriter writer);
	}

	public interface IWritable<A>
	{
		public abstract void Write(WadWriter writer, A arg);
	}

	//Generally used when an list uses the file data space to store itself and has additional data following it.
	public interface IWritableArrayMultipass
	{
		public abstract void WriteStruct(WadWriter writer);
		public abstract void WriteData(WadWriter writer);
	}

	//These are separate to prevent conflict with methods only differing by generic arguments
	public static class WriterExtensions
	{
		#region IWritable<T>
		public static void Write<T>(this WadWriter that, T value) where T : IWritable
		{
			value.Write(that);
		}

		public static void WriteArray<T>(this WadWriter that, IReadOnlyList<T> array) where T : IWritable
		{
			for(int i = 0; i < array.Count; i++)
			{
				array[i].Write(that);
			}
		}
		#endregion
		#region IWritable<T,A>
		public static void Write<T,A>(this WadWriter that, A arg, T value) where T : IWritable<A>
		{
			value.Write(that, arg);
		}

		public static void WriteArray<T,A>(this WadWriter that, A arg, IReadOnlyList<T> array) where T : IWritable<A>
		{
			for(int i = 0; i < array.Count; i++)
			{
				array[i].Write(that, arg);
			}
		}

		public static void WriteArray<T,A>(this WadWriter that, IList<A> args, IReadOnlyList<T> array) where T : IWritable<A>
		{
			for(int i = 0; i < array.Count; i++)
			{
				array[i].Write(that, args[i]);
			}
		}
		#endregion
		#region IWritableMultipass<T>
		public static void WriteArrayMultipass<T>(this WadWriter that, IReadOnlyList<T> array) where T:class,IWritableArrayMultipass
		{
			for(int i = 0; i < array.Count; i++)
			{
				array[i].WriteStruct(that);
			}
			for(int i = 0; i < array.Count; i++)
			{
				array[i].WriteData(that);
			}
		}
		public static void WriteArrayWithoutMultipass<T>(this WadWriter that, IReadOnlyList<T> array) where T:class,IWritableArrayMultipass
		{
			for(int i = 0; i < array.Count; i++)
			{
				array[i].WriteStruct(that);
				array[i].WriteData(that);
			}
		}
		#endregion

		#region DelWrite
		public static void Write(this WadWriter that, DelWrite write)
		{
			write(that);
		}

		public static void WriteArray(this WadWriter that, IReadOnlyList<DelWrite> writes)
		{
			foreach(var write in writes)
			{
				write(that);
			}
		}
		#endregion
		#region DelWriteFrom<T>
		public static void WriteFrom<T>(this WadWriter that, DelWriteFrom<T> writeFrom, T instance)
		{
			writeFrom(that, instance);
		}

		public static void WriteFromArray<T>(this WadWriter that, DelWriteFrom<T> writeFrom, IReadOnlyList<T> array)
		{
			for(int i = 0; i < array.Count; i++)
			{
				writeFrom(that, array[i]);
			}
		}
		#endregion
		#region DelWrite<T,A>
		public static void Write<A>(this WadWriter that, DelWrite<A> write, A arg)
		{
			write(that, arg);
		}
		#endregion
	}
}

