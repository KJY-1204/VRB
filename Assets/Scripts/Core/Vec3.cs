using System;

namespace VRB.Core
{
    /// <summary>Minimal 3D vector so game logic can run without UnityEngine.</summary>
    public readonly struct Vec3 : IEquatable<Vec3>
    {
        public readonly float X;
        public readonly float Y;
        public readonly float Z;

        public Vec3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Vec3 Zero => new Vec3(0f, 0f, 0f);
        public static Vec3 Forward => new Vec3(0f, 0f, 1f);

        public float Magnitude => (float)Math.Sqrt(X * X + Y * Y + Z * Z);

        public Vec3 Normalized
        {
            get
            {
                var m = Magnitude;
                return m > 1e-6f ? this / m : Zero;
            }
        }

        public static float Dot(Vec3 a, Vec3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

        /// <summary>Angle between two vectors in degrees, 0 when either is zero.</summary>
        public static float AngleDegrees(Vec3 a, Vec3 b)
        {
            var na = a.Normalized;
            var nb = b.Normalized;
            if (na.Magnitude < 0.5f || nb.Magnitude < 0.5f) return 0f;
            var dot = Math.Max(-1f, Math.Min(1f, Dot(na, nb)));
            return (float)(Math.Acos(dot) * 180.0 / Math.PI);
        }

        public static Vec3 operator +(Vec3 a, Vec3 b) => new Vec3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vec3 operator -(Vec3 a, Vec3 b) => new Vec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vec3 operator *(Vec3 a, float s) => new Vec3(a.X * s, a.Y * s, a.Z * s);
        public static Vec3 operator *(float s, Vec3 a) => a * s;
        public static Vec3 operator /(Vec3 a, float s) => new Vec3(a.X / s, a.Y / s, a.Z / s);

        public bool Equals(Vec3 other) => X == other.X && Y == other.Y && Z == other.Z;
        public override bool Equals(object obj) => obj is Vec3 other && Equals(other);
        public override int GetHashCode() => (X, Y, Z).GetHashCode();
        public override string ToString() => $"({X:F3}, {Y:F3}, {Z:F3})";
    }
}
