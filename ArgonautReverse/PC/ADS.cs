using ArgonautReverse.IO;

namespace ArgonautReverse.PC
{
	public abstract record class ADSResource(int Type, byte[] Data);

	public sealed record class ADSResourceSMPC(int Type, byte[] Data):ADSResource(Type, Data),IReadable<ADSResourceSMPC>,IWritable
	{
		public static ADSResourceSMPC Parse(WadReader reader)
		{
			int type = reader.Read<int>();
			int dataLength = reader.Read<int>();
			var data = reader.ReadArray<byte>(dataLength);
			return new(type, data);
		}

		public void Write(WadWriter writer)
		{
			writer.Write<int>(Type);
			writer.Write<int>(Data.Length);
			writer.WriteArray<byte>(Data);
		}
	}

	public sealed record class ADSResourceAMPC(int Unknown, int Type, byte[] Data):ADSResource(Type, Data),IReadable<ADSResourceAMPC>,IWritable
	{
		public static ADSResourceAMPC Parse(WadReader reader)
		{
			int unknown = reader.Read<int>();
			int type = reader.Read<int>();
			int dataLength = reader.Read<int>();
			var data = reader.ReadArray<byte>(dataLength);
			return new(unknown, type, data);
		}

		public void Write(WadWriter writer)
		{
			writer.Write<int>(Unknown);
			writer.Write<int>(Type);
			writer.Write<int>(Data.Length);
			writer.WriteArray<byte>(Data);
		}
	}

	public sealed class MidiStruct:IReadable<MidiStruct>,IWritable
	{
		public Vector3I Position;
		public int unknownMidiField0;
		public int StartTrailingRange;//Diameter
		public int AudibleRange;
		public int Channel;
		public int MaxVolume;
		public int enabledMaybe;
		public int Volume;

		public static MidiStruct Parse(WadReader reader)
		{
			return new MidiStruct
			{
				Position = reader.Read<Vector3I>(),
				unknownMidiField0 = reader.Read<int>(),
				StartTrailingRange = reader.Read<int>(),
				AudibleRange = reader.Read<int>(),
				Channel = reader.Read<int>(),
				MaxVolume = reader.Read<int>(),
				enabledMaybe = reader.Read<int>(),
				Volume = reader.Read<int>(),
			};
		}

		public void Write(WadWriter writer)
		{
			writer.Write<Vector3I>(Position);
			writer.Write<int>(unknownMidiField0);
			writer.Write<int>(StartTrailingRange);
			writer.Write<int>(AudibleRange);
			writer.Write<int>(Channel);
			writer.Write<int>(MaxVolume);
			writer.Write<int>(enabledMaybe);
			writer.Write<int>(Volume);
		}
	}
}