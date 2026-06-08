using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using ArgonautReverse.IO;

namespace ArgonautReverse.Universal
{
	[StructLayout(LayoutKind.Explicit, Size = 16 * sizeof(float))]
	public unsafe struct Matrix4x4F:IReadable<Matrix4x4F>, IWritable, IEquatable<Matrix4x4F>
	{
		public static readonly Matrix4x4F Identity = new Matrix4x4F(new Vector4F(1,0,0,0), new Vector4F(0,1,0,0), new Vector4F(0,0,1,0), new Vector4F(0,0,0,1));

		//[column][row]. This is reverse standard in math
		[FieldOffset(0)]public fixed float m[4*4];
		[FieldOffset(sizeof(float)*0)]public Vector3F rot0;		//[0][0-2]
		[FieldOffset(sizeof(float)*3)]public float data0;		//[0][3]
		[FieldOffset(sizeof(float)*4)]public Vector3F rot1;		//[1][0-2]
		[FieldOffset(sizeof(float)*7)]public float data1;		//[1][3]
		[FieldOffset(sizeof(float)*8)]public Vector3F rot2;		//[2][0-2]
		[FieldOffset(sizeof(float)*11)]public float data2;		//[2][3]
		[FieldOffset(sizeof(float)*12)]public Vector3F trans;	//[3][0-2]
		[FieldOffset(sizeof(float)*15)]public float scale;		//[3][3]

		public float this[int col, int row]
		{
			readonly get => m[4*col + row];
			set => m[4*col + row] = value;
		}

		public Matrix4x4F(in Vector4F value0, in Vector4F value1, in Vector4F value2, in Vector4F value3)
		{
			fixed(Matrix4x4F* this0 = &this)
			{
				*(Vector4F*)&this0->rot0 = value0;
				*(Vector4F*)&this0->rot1 = value1;
				*(Vector4F*)&this0->rot2 = value2;
				*(Vector4F*)&this0->trans = value3;
			}
		}

		public static Matrix4x4F Parse(WadReader reader) => reader.ReadData<Matrix4x4F>();

		public override readonly int GetHashCode() => HashCode.Combine(rot0, data0, rot1, data1, rot2, data2, trans, scale);
		public override readonly bool Equals([NotNullWhen(true)] object obj) => obj is Matrix4x4F that && this.Equals(that);

		public readonly unsafe bool Equals(Matrix4x4F that)
		{
			fixed(Matrix4x4F* this0 = &this)
			{
				Matrix4x4F* that0 = &that;
				var thisValues = (int*)this0;
				var thatValues = (int*)that0;
				for(int i=0; i<sizeof(Matrix4x4F)/sizeof(int); i++)
				{
					if(thisValues[i] != thatValues[i])
					{
						return false;
					}
				}
				return true;
			}
		}

		public readonly void Write(WadWriter writer) => writer.WriteData(this);

		public readonly Vector3F TransformPoint(in Vector3F v) => new
		(
			x: this[0,0] * v.X + this[1,0] * v.Y + this[2,0] * v.Z + trans.X,
			y: this[0,1] * v.X + this[1,1] * v.Y + this[2,1] * v.Z + trans.Y,
			z: this[0,2] * v.X + this[1,2] * v.Y + this[2,2] * v.Z + trans.Z
		);

		public static void CreatePositionMatrix(in RotPos3F rotPos, out Matrix4x4F ret)
		{
			float cosX = MathF.Cos(rotPos.Rotation.X);
			float sinX = MathF.Sin(rotPos.Rotation.X);
			float cosY = MathF.Cos(rotPos.Rotation.Y);
			float sinY = MathF.Sin(rotPos.Rotation.Y);
			float cosZ = MathF.Cos(rotPos.Rotation.Z);
			float sinZ = MathF.Sin(rotPos.Rotation.Z);

			ret = default;
			ret.m[4*0 + 0] = (cosY * cosZ) - (sinX * sinY * sinZ);
			ret.m[4*0 + 1] = -cosX * sinZ;
			ret.m[4*0 + 2] = (sinY * cosZ) + (sinX * cosY * sinZ);
			ret.m[4*0 + 3] = 0f;

			ret.m[4*1 + 0] = (cosY * sinZ) + (sinX * sinY * cosZ);
			ret.m[4*1 + 1] = cosX * cosZ;
			ret.m[4*1 + 2] = -(sinX * cosY * cosZ) + (sinY * sinZ);
			ret.m[4*1 + 3] = 0f;

			ret.m[4*2 + 0] = -(cosX * sinY);
			ret.m[4*2 + 1] = sinX;
			ret.m[4*2 + 2] = cosX * cosY;
			ret.m[4*2 + 3] = 0f;

			ret.trans = rotPos.Position;
			ret.scale = 1f;
		}
	}
}
