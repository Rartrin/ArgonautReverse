global using Vector2I = ArgonautReverse.Universal.Vector2<int>;
global using Vector2F = ArgonautReverse.Universal.Vector2<float>;
global using Vector3I = ArgonautReverse.Universal.Vector3<int>;
global using Vector3F = ArgonautReverse.Universal.Vector3<float>;
global using Vector4I = ArgonautReverse.Universal.Vector4<int>;
global using Vector4F = ArgonautReverse.Universal.Vector4<float>;

using System.Numerics;
using ArgonautReverse.IO;

namespace ArgonautReverse.Universal
{
	#region Vector2
	public struct Vector2<T>:IReadable<Vector2<T>>, IWritable where T : unmanaged, IBinaryNumber<T>
	{
		public T X, Y;

		public readonly T LengthSquared => X*X + Y*Y;

		public Vector2(T x, T y)
		{
			X = x;
			Y = y;
		}

		public static Vector2<T> Parse(WadReader reader)
		{
			var x = reader.Read<T>();
			var y = reader.Read<T>();
			return new Vector2<T>(x, y);
		}

		public readonly void Write(WadWriter writer)
		{
			writer.Write(X);
			writer.Write(Y);
		}

		public static Vector2<T> operator +(in Vector2<T> a, in Vector2<T> b) => new Vector2<T>(a.X+b.X, a.Y+b.Y);
		public static Vector2<T> operator -(in Vector2<T> a, in Vector2<T> b) => new Vector2<T>(a.X-b.X, a.Y-b.Y);
	}
	#endregion
	#region Vector3
	public struct Vector3<T>:IReadable<Vector3<T>>, IWritable where T : unmanaged, IBinaryNumber<T>
	{
		public T X, Y, Z;

		public readonly T LengthSquared => X*X + Y*Y + Z*Z;

		public Vector3(T x, T y, T z)
		{
			X = x;
			Y = y;
			Z = z;
		}

		public static Vector3<T> Parse(WadReader reader)
		{
			var x = reader.Read<T>();
			var y = reader.Read<T>();
			var z = reader.Read<T>();
			return new Vector3<T>(x, y, z);
		}

		public readonly void Write(WadWriter writer)
		{
			writer.Write(X);
			writer.Write(Y);
			writer.Write(Z);
		}

		public static T Dot(in Vector3<T> a, in Vector3<T> b)
		{
			return a.X*b.X + a.Y*b.Y + a.Z*b.Z;
		}

		//Computes the cross product of a x b.
		//Remember this is anticommunitive. a x b == -(b x a)
		public static Vector3<T> Cross(in Vector3<T> a, in Vector3<T> b)
		{
			return new Vector3<T>
			(
				a.Y*b.Z - a.Z*b.Y,
				a.Z*b.X - a.X*b.Z,
				a.X*b.Y - a.Y*b.X
			);
		}

		public static Vector3<T> operator +(in Vector3<T> a, in Vector3<T> b) => new Vector3<T>(a.X+b.X, a.Y+b.Y, a.Z+b.Z);
		public static Vector3<T> operator -(in Vector3<T> a, in Vector3<T> b) => new Vector3<T>(a.X-b.X, a.Y-b.Y, a.Z-b.Z);
		public static Vector3<T> operator *(in Vector3<T> v, T s) => new Vector3<T>(v.X*s, v.Y*s, v.Z*s);
		public static Vector3<T> operator /(in Vector3<T> v, T d) => new Vector3<T>(v.X/d, v.Y/d, v.Z/d);

		public static Vector3<T> operator +(in Vector3<T> v) => v;
		public static Vector3<T> operator -(in Vector3<T> v) => new Vector3<T>(-v.X, -v.Y, -v.Z);
	}
	#endregion
	#region Vector4
	public struct Vector4<T>:IReadable<Vector4<T>>, IWritable where T : unmanaged, IBinaryNumber<T>
	{
		public T X, Y, Z, W;

		public Vector4(T x, T y, T z, T w)
		{
			X = x;
			Y = y;
			Z = z;
			W = w;
		}

		public static Vector4<T> Parse(WadReader reader)
		{
			var x = reader.Read<T>();
			var y = reader.Read<T>();
			var z = reader.Read<T>();
			var w = reader.Read<T>();
			return new Vector4<T>(x, y, z, w);
		}

		public readonly void Write(WadWriter writer)
		{
			writer.Write(X);
			writer.Write(Y);
			writer.Write(Z);
			writer.Write(W);
		}

		public readonly Vector3<T> GetVector3() => new Vector3<T>(X, Y, Z);
	}
	#endregion

	#region Connecting Operators
	//Vector3<float> operator*(in Vector3<int> v, float s){return Vector3<float>(v.X*s, v.Y*s, v.Z*s);}
	//Vector3<float> operator/(in Vector3<int> v, float d){return Vector3<float>(v.X/d, v.Y/d, v.Z/d);}
	#endregion

	public static class VectorMath
	{
		public static Vector3I Abs(in Vector3I v) => new Vector3I(Math.Abs(v.X), Math.Abs(v.Y), Math.Abs(v.Z));

		#region Extensions
		extension(Vector3F that)
		{
			public float Length() => MathF.Sqrt(that.LengthSquared);
			public Vector3F Normalized() => that / that.Length();
		}
		#endregion
	}
}