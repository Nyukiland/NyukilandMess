using UnityEngine;
using System;

namespace Modules.VarExtention
{
	public struct CVector3 : IEquatable<CVector3>
	{
		private Vector3 _vec;
		public Vector3 Vector
		{
			get => _vec;
			set => _vec = value;
		}

		//constructor
		public CVector3(Vector3 value) => _vec = value;
		public CVector3(float x, float y, float z) => _vec = new(x, y, z);
		public CVector3(int x, int y, int z) => _vec = new(x, y, z);
		public CVector3(float x, int y, float z) => _vec = new(x, y, z);
		public CVector3(float x, float y, int z) => _vec = new(x, y, z);
		public CVector3(float x, int y, int z) => _vec = new(x, y, z);
		public CVector3(int x, float y, int z) => _vec = new(x, y, z);
		public CVector3(int x, float y, float z) => _vec = new(x, y, z);
		public CVector3(int x, int y, float z) => _vec = new(x, y, z);
		public CVector3(Vector2 xy, float z) => _vec = new(xy.x, xy.y, z);
		public CVector3(Vector2 xy, int z) => _vec = new(xy.x, xy.y, z);
		public CVector3(float x, Vector2 yz) => _vec = new(x, yz.x, yz.y);
		public CVector3(int x, Vector2 yz) => _vec = new(x, yz.x, yz.y);

		//property
		public float x
		{
			get => _vec.x;
			set => _vec.x = value;
		}
		public float y
		{
			get => _vec.y;
			set => _vec.y = value;
		}
		public float z
		{
			get => _vec.z;
			set => _vec.z = value;
		}
		public Vector2 xx => new(x, x);
		public Vector2 yy => new(y, y);
		public Vector2 zz => new(z, z);

		public Vector2 xy => new(x, y);
		public Vector2 yx => new(y, x);

		public Vector2 xz => new(x, z);
		public Vector2 zx => new(z, x);

		public Vector2 yz => new(y, z);
		public Vector2 zy => new(z, y);

		public Vector3 xxx => new(x, x, x);
		public Vector3 yyy => new(y, y, y);
		public Vector3 zzz => new(z, z, z);

		public Vector3 xxy => new(x, x, y);
		public Vector3 xxz => new(x, x, z);

		public Vector3 xyx => new(x, y, x);
		public Vector3 xyy => new(x, y, y);
		public Vector3 xyz => new(x, y, z);

		public Vector3 xzx => new(x, z, x);
		public Vector3 xzy => new(x, z, y);
		public Vector3 xzz => new(x, z, z);

		public Vector3 yxx => new(y, x, x);
		public Vector3 yxy => new(y, x, y);
		public Vector3 yxz => new(y, x, z);

		public Vector3 yyx => new(y, y, x);
		public Vector3 yyz => new(y, y, z);

		public Vector3 yzx => new(y, z, x);
		public Vector3 yzy => new(y, z, y);
		public Vector3 yzz => new(y, z, z);

		public Vector3 zxx => new(z, x, x);
		public Vector3 zxy => new(z, x, y);
		public Vector3 zxz => new(z, x, z);

		public Vector3 zyx => new(z, y, x);
		public Vector3 zyy => new(z, y, y);
		public Vector3 zyz => new(z, y, z);

		public Vector3 zzx => new(z, z, x);
		public Vector3 zzy => new(z, z, y);



		public float magnitude => _vec.magnitude;
		public float sqrMagnitude => _vec.sqrMagnitude;
		public Vector3 normalized => _vec.normalized;


		//func
		public void Normalize() => _vec.Normalize();

		//redifine base stuff
		public static implicit operator Vector3(CVector3 v) => v._vec;
		public static implicit operator CVector3(Vector3 v) => new(v);
		public override string ToString() => $"({x}, {y}, {z})";
		public override int GetHashCode() => _vec.GetHashCode();
		public override bool Equals(object obj)
		{
			if (obj is Vector3 other)
			{
				return Equals(other);
			}
			else if (obj is CVector3 other2)
			{
				return Equals(other2);
			}

			return false;
		}

		public bool Equals(CVector3 other)
		{
			return x == other.x && y == other.y && z == other.z;
		}

		public bool Equals(Vector3 other)
		{
			return x == other.x && y == other.y && z == other.z;
		}

		public static CVector3 operator +(CVector3 a, CVector3 b) => new(a._vec + b._vec);
		public static CVector3 operator -(CVector3 a, CVector3 b) => new(a._vec - b._vec);
		public static CVector3 operator *(CVector3 a, float b) => new(a._vec * b);
		public static CVector3 operator /(CVector3 a, float b) => new(a._vec / b);
		public static bool operator ==(CVector3 a, CVector3 b) => a.Equals(b);
		public static bool operator !=(CVector3 a, CVector3 b) => !a.Equals(b);
	}
}