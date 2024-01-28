using ArgonautReverse.IO;

namespace ArgonautReverse.PC
{
	public sealed class STPC_Struct2:IReadable<STPC_Struct2>
	{
		public readonly int unknown0;
		//public readonly int arraySize;
		public readonly IReadOnlyList<STPC_Struct3> array;
		public readonly IReadOnlyList<StratObject2PC> structPtr0;

		public STPC_Struct2(int unknown0, IReadOnlyList<STPC_Struct3> array, IReadOnlyList<StratObject2PC> structPtr0)
		{
			this.unknown0 = unknown0;
			//this.arraySize = arraySize;
			if(array.Count != structPtr0.Count)
			{
				throw new Exception();
			}
			this.array = array;
			this.structPtr0 = structPtr0;
		}

		public static STPC_Struct2 Parse(WadReader reader)
		{
			var unknown0 = reader.Read<int>();
			var arraySize = reader.Read<int>();
			reader.AssertRead<uint>(0);//array placeholder
			reader.AssertRead<uint>(0);//structPtr0 placeholder

			var array = reader.ReadArray<STPC_Struct3>(arraySize);
			var structPtr0 = reader.ReadArrayMultipass<StratObject2PC>(arraySize);

			return new STPC_Struct2(unknown0, array, structPtr0);
		}
	}

	public sealed class STPC_Struct3:IReadable<STPC_Struct3>
	{
		public readonly int field0;
		public readonly int field1;

		public STPC_Struct3(int field0, int field1)
		{
			this.field0 = field0;
			this.field1 = field1;
		}

		public static STPC_Struct3 Parse(WadReader reader)
		{
			var field0 = reader.Read<int>();
			var field1 = reader.Read<int>();
			return new STPC_Struct3(field0, field1);
		}
	}
}
