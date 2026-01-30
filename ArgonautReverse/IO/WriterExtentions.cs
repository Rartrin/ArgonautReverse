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
		extension(WadWriter that)
		{
			#region IWritable<T>
			public void Write<T>(T value) where T : IWritable
			{
				value.Write(that);
			}

			public void WriteArray<T>(IReadOnlyList<T> array) where T : IWritable
			{
				for(int i = 0; i < array.Count; i++)
				{
					array[i].Write(that);
				}
			}
			public void WriteArray<T>(ReadOnlySpan<T> array) where T : IWritable
			{
				for(int i = 0; i < array.Length; i++)
				{
					array[i].Write(that);
				}
			}
			public void WriteSizedArray<T>(int size, IReadOnlyList<T> array) where T : IWritable
			{
				if(size != array.Count){throw new Exception();}

				that.WriteArray(array);
			}
			#endregion
			#region IWritable<T,A>
			public void Write<T,A>(A arg, T value) where T : IWritable<A>
			{
				value.Write(that, arg);
			}

			public void WriteArray<T,A>(A arg, IReadOnlyList<T> array) where T : IWritable<A>
			{
				for(int i = 0; i < array.Count; i++)
				{
					array[i].Write(that, arg);
				}
			}

			public void WriteArray<T,A>(IList<A> args, IReadOnlyList<T> array) where T : IWritable<A>
			{
				if(args.Count != array.Count){throw new Exception();}

				for(int i = 0; i < array.Count; i++)
				{
					array[i].Write(that, args[i]);
				}
			}
			#endregion
			#region IWritableMultipass<T>
			public void WriteArrayMultipass<T>(IReadOnlyList<T> array) where T:class,IWritableArrayMultipass
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
			public void WriteArrayWithoutMultipass<T>(IReadOnlyList<T> array) where T:class,IWritableArrayMultipass
			{
				for(int i = 0; i < array.Count; i++)
				{
					array[i].WriteStruct(that);
					array[i].WriteData(that);
				}
			}
			public void WriteSizedArrayMultipass<T>(int size, IReadOnlyList<T> array) where T:class,IWritableArrayMultipass
			{
				if(size != array.Count){throw new Exception();}
				that.WriteArrayMultipass(array);
			}
			public void WriteArrayWithoutMultipass<T>(int size, IReadOnlyList<T> array) where T:class,IWritableArrayMultipass
			{
				if(size != array.Count){throw new Exception();}
				that.WriteArrayWithoutMultipass(array);
			}
			#endregion

			#region DelWrite
			public void Write(DelWrite write)
			{
				write(that);
			}

			public void WriteArray(IReadOnlyList<DelWrite> writes)
			{
				foreach(var write in writes)
				{
					write(that);
				}
			}
			#endregion
			#region DelWriteFrom<T>
			public void WriteFrom<T>(DelWriteFrom<T> writeFrom, T instance)
			{
				writeFrom(that, instance);
			}

			public void WriteFromArray<T>(DelWriteFrom<T> writeFrom, IReadOnlyList<T> array)
			{
				for(int i = 0; i < array.Count; i++)
				{
					writeFrom(that, array[i]);
				}
			}
			#endregion
			#region DelWrite<T,A>
			public void Write<A>(DelWrite<A> write, A arg)
			{
				write(that, arg);
			}
			#endregion
		}
	}
}

