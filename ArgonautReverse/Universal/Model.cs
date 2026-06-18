using ArgonautReverse.IO;

namespace ArgonautReverse.Universal
{
	public abstract class Bound:IReadable<Bound>,IWritable
	{
		public static Bound Parse(WadReader reader)
		{
			if(reader.ReadVersion.NEW_COLLISION)
			{
				return Bound_NEW_COLLISION.Parse(reader);
			}
			else
			{
				return Bound_OLD_COLLISION.Parse(reader);
			}
		}

		public abstract void Write(WadWriter writer);
	}

	public sealed class Bound_NEW_COLLISION:Bound
	{
		public const int ByteSize = 6;

		public sbyte normalX;// (Signed FixedPoint 1.0.7)
		public sbyte normalY;// (Signed FixedPoint 1.0.7)
		public sbyte normalZ;// (Signed FixedPoint 1.0.7)
		public byte flags;
		public Fx16 Distance;// Distance from object origin

		private Bound_NEW_COLLISION(sbyte normalX, sbyte normalY, sbyte normalZ, byte flags, Fx16 distance)
		{
			this.normalX = normalX;
			this.normalY = normalY;
			this.normalZ = normalZ;
			this.flags = flags;
			Distance = distance;
		}

		new public static Bound_NEW_COLLISION Parse(WadReader reader)
		{
			var nx = reader.Read<sbyte>();
			var ny = reader.Read<sbyte>();
			var nz = reader.Read<sbyte>();
			var flags = reader.Read<byte>();
			var d = reader.Read<Fx16>();
			return new Bound_NEW_COLLISION(nx, ny, nz, flags, d);
		}

		public override void Write(WadWriter writer)
		{
			writer.Write<sbyte>(normalX);
			writer.Write<sbyte>(normalY);
			writer.Write<sbyte>(normalZ);
			writer.Write<byte>(flags);
			writer.Write<Fx16>(Distance);
		}
	}
	public sealed class Bound_OLD_COLLISION(UFx16 multX, UFx16 multY, UFx32 constant):Bound
	{
		public const int ByteSize = 8;

		public UFx16 MultX = multX;	// X Multiplier
		public UFx16 MultY = multY;	// Y Multiplier
		public UFx32 Constant = constant;

		new public static Bound_OLD_COLLISION Parse(WadReader reader)
		{
			var multX = reader.Read<UFx16>();
			var multY = reader.Read<UFx16>();
			var constant = reader.Read<UFx32>();
			return new Bound_OLD_COLLISION(multX, multY, constant);
		}

		public override void Write(WadWriter writer)
		{
			writer.Write<UFx16>(MultX);
			writer.Write<UFx16>(MultY);
			writer.Write<UFx32>(Constant);
		}
	}

	//These flags were from PSX. Work for PC too?
	[Flags]
	public enum FaceCollisionFlags:byte
	{
		Quad = 1 << 0,
		Eqn = 1 << 1,
		Ceiling = 1 << 2,
		Wall = 1 << 3,
		StickToFloor = 1 << 4,
		Slide = 1 << 5,
		NoHang = 1 << 6,
	}

	public sealed class FaceCollision:IReadable<FaceCollision>,IWritable
	{
		public FaceCollisionFlags flags;
		public byte surface;

		#region OLD_COLLISION
		public ushort? edgeFlags;/* edge flags for vertical surfaces */
		#endregion

		//These field were in opposite order on OLD_COLLISION
		public IReadOnlyList<Bound> boundaries;//[4]
		public Bound plane;

		private FaceCollision(FaceCollisionFlags flags, byte surface, ushort? edgeFlags, Bound plane, IReadOnlyList<Bound> boundaries)
		{
			this.flags = flags;
			this.surface = surface;
			this.edgeFlags = edgeFlags;
			this.plane = plane;
			this.boundaries = boundaries;
		}

		public static FaceCollision Parse(WadReader reader)
		{
			var flags = (FaceCollisionFlags)reader.Read<byte>();
			var surface = reader.Read<byte>();
			ushort? edgeFlags;
			Bound plane;
			IReadOnlyList<Bound> boundaries;
			if(reader.ReadVersion.NEW_COLLISION)
			{
				edgeFlags = null;
				//Plane then boundary
				plane = reader.Read<Bound>();
				boundaries = reader.ReadArray<Bound>(4);
			}
			else
			{
				edgeFlags = reader.Read<ushort>();
				//Boundary then plane
				boundaries = reader.ReadArray<Bound>(4);
				plane = reader.Read<Bound>();
			}
			return new FaceCollision(flags, surface, edgeFlags, plane, boundaries);
		}

		public void Write(WadWriter writer)
		{
			writer.Write<byte>((byte)flags);
			writer.Write<byte>(surface);
			if(writer.WriteVersion.NEW_COLLISION)
			{
				//Plane then boundary
				writer.Write<Bound>(plane);
				writer.WriteSizedArray<Bound>(4, boundaries);
			}
			else
			{
				//Boundary then plane
				writer.Write<ushort>(edgeFlags.Value);
				writer.WriteSizedArray<Bound>(4, boundaries);
				writer.Write<Bound>(plane);
			}
		}
	}
}
