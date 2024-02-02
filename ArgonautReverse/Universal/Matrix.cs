using System.Diagnostics.CodeAnalysis;
using ArgonautReverse.IO;

namespace ArgonautReverse.Universal
{
	public unsafe struct Matrix4x4F:IReadable<Matrix4x4F>, IWritable, IEquatable<Matrix4x4F>
	{
		public static readonly Matrix4x4F Identity = new Matrix4x4F(new Vector4F(1,0,0,0), new Vector4F(0,1,0,0), new Vector4F(0,0,1,0), new Vector4F(0,0,0,1));

		//[column][row]. This is reverse standard in math
		//float m[4][4];
		public Vector3F rot0;	//[0][0-2]
		public float data0;		//[0][3]
		public Vector3F rot1;	//[1][0-2]
		public float data1;		//[1][3]
		public Vector3F rot2;	//[2][0-2]
		public float data2;		//[2][3]
		public Vector3F trans;	//[3][0-2]
		public float scale;		//[3][3]

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
	}
}
