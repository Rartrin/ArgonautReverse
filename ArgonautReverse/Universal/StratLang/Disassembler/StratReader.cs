using ArgonautReverse.Files;
using ArgonautReverse.Universal.StratLang;

namespace ArgonautReverse.Universal.StratLang.Disassembler
{
	public sealed class StratReader(WADFile wadFile, Script script, InstructionAddress position)
	{
		public readonly WADFile WadFile = wadFile;
		public readonly Script Script = script;

		private readonly int[] data = script.Data;
		public InstructionAddress Position = position;

		private static void AssertAlligned(InstructionAddress bytePosition)
		{
			if(((int)bytePosition&0b11)!=0)
			{
				throw new Exception("Address not alligned");
			}
		}

		public int ReadInt()
		{
			AssertAlligned(Position);
			var ret = data[((int)Position)/4];
			Position += 4;
			return ret;
		}

		public uint ReadUInt() => (uint)ReadInt();

		//Read offset and add it to the following address
		public InstructionAddress ReadRelativeAddress()
		{
			var offset = ReadInt() * sizeof(int);
			return Position + offset;
		}

		public int[] ReadArray(int intCount)
		{
			AssertAlligned(Position);
			var ret = data.AsSpan(((int)Position)/4, intCount).ToArray();
			Position += sizeof(int)*intCount;
			return ret;
		}

		public int PeekInt()
		{
			AssertAlligned(Position);
			return data[((int)Position)/4];
		}

		public unsafe string ReadString(InstructionAddress position)
		{
			fixed(int* data0 = data)
			{
				return new string((sbyte*)data0 + (int)position);
			}
		}

		public int ReadAt(InstructionAddress bytePosition, int intOffset)
		{
			AssertAlligned(bytePosition);
			var offset = (((int)bytePosition)/4) + intOffset;
			return data[offset];
		}
	}
}
