namespace ArgonautReverse
{
	public interface IConvertibleOSE<T>
	{
		public abstract T ToOSE();
	}

	public static class ConvertibleExtentions
	{
		public static IReadOnlyList<OSE> ToOSE<OSE>(this IReadOnlyList<IConvertibleOSE<OSE>> that)
		{
			var oseList = new OSE[that.Count];
			for(int i=0; i<that.Count; i++)
			{
				oseList[i] = that[i].ToOSE();
			}
			return oseList;
		}

		public static IReadOnlyList<OSE> ToOSE<From,OSE>(this IReadOnlyList<From> that) where From:IConvertibleOSE<OSE>
		{
			var oseList = new OSE[that.Count];
			for(int i=0; i<that.Count; i++)
			{
				oseList[i] = that[i].ToOSE();
			}
			return oseList;
		}

		public static IReadOnlyList<IReadOnlyList<OSE>> ToOSE<From,OSE>(this IReadOnlyList<IReadOnlyList<From>> that) where From:IConvertibleOSE<OSE>
		{
			var oseList = new IReadOnlyList<OSE>[that.Count];
			for(int i=0; i<that.Count; i++)
			{
				oseList[i] = that[i].ToOSE<From,OSE>();
			}
			return oseList;
		}
	}
}
