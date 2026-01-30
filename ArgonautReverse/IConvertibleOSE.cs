namespace ArgonautReverse
{
	//public interface IConvertibleOSE<T>;
	public interface IConvertibleToOSE<OSE>
	{
		public abstract OSE ToOSE();
	}
	public interface IConvertibleFromOSE<From,To>
	{
		public static abstract To FromOSE(From ose);
	}

	public static class ConvertibleExtentions
	{
		public static IReadOnlyList<OSE> ToOSE<OSE>(this IReadOnlyList<IConvertibleToOSE<OSE>> that)
		{
			var oseList = new OSE[that.Count];
			for(int i=0; i<that.Count; i++)
			{
				oseList[i] = that[i].ToOSE();
			}
			return oseList;
		}

		public static IReadOnlyList<OSE> ToOSE<From,OSE>(this IReadOnlyList<From> that) where From:IConvertibleToOSE<OSE>
		{
			var oseList = new OSE[that.Count];
			for(int i=0; i<that.Count; i++)
			{
				oseList[i] = that[i].ToOSE();
			}
			return oseList;
		}

		public static IReadOnlyList<IReadOnlyList<OSE>> ToOSE<From,OSE>(this IReadOnlyList<IReadOnlyList<From>> that) where From:IConvertibleToOSE<OSE>
		{
			var oseList = new IReadOnlyList<OSE>[that.Count];
			for(int i=0; i<that.Count; i++)
			{
				oseList[i] = that[i].ToOSE<From,OSE>();
			}
			return oseList;
		}

		public static IReadOnlyList<To> FromOSE<From,To>(this IReadOnlyList<From> that) where To:IConvertibleFromOSE<From,To>
		{
			var retList = new To[that.Count];
			for(int i=0; i<that.Count; i++)
			{
				retList[i] = To.FromOSE(that[i]);
			}
			return retList;
		}

		public static IReadOnlyList<IReadOnlyList<To>> FromOSE<From,To>(this IReadOnlyList<IReadOnlyList<From>> that) where To:IConvertibleFromOSE<From,To>
		{
			var retList = new IReadOnlyList<To>[that.Count];
			for(int i=0; i<that.Count; i++)
			{
				retList[i] = that[i].FromOSE<From,To>();
			}
			return retList;
		}
	}
}