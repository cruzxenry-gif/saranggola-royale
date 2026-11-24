using System;

namespace Saranggola.Sim
{
    public readonly struct Vec2 : IEquatable<Vec2>
    {
        public readonly float X, Z;
        public Vec2(float x, float z) { X = x; Z = z; }
        public static readonly Vec2 Zero = new Vec2(0, 0);
        public static readonly Vec2 Right = new Vec2(1, 0);
        public static readonly Vec2 Forward = new Vec2(0, 1);
        public float SqrLength => X * X + Z * Z;
        public float Length => MathF.Sqrt(SqrLength);
        public Vec2 Normalized => SqrLength > 0.000001f ? this / Length : Zero;
        public Vec2 Perpendicular => new Vec2(-Z, X);
        public Vec2 Limit(float size = 1) => SqrLength > size * size ? Normalized * size : this;
        public Vec2 Toward(Vec2 target, float distance) => this + (target - this).Limit(distance);
        public Vec2 Rotate(float radians)
        {
            float c = MathF.Cos(radians), s = MathF.Sin(radians);
            return new Vec2(X * c - Z * s, X * s + Z * c);
        }
        public Vec2 Quarter(int steps)
        {
            switch (steps & 3)
            {
                case 1: return new Vec2(-Z, X);
                case 2: return -this;
                case 3: return new Vec2(Z, -X);
                default: return this;
            }
        }
        public static Vec2 operator +(Vec2 a, Vec2 b) => new Vec2(a.X + b.X, a.Z + b.Z);
        public static Vec2 operator -(Vec2 a, Vec2 b) => new Vec2(a.X - b.X, a.Z - b.Z);
        public static Vec2 operator -(Vec2 a) => new Vec2(-a.X, -a.Z);
        public static Vec2 operator *(Vec2 a, float scalar) => new Vec2(a.X * scalar, a.Z * scalar);
        public static Vec2 operator /(Vec2 a, float scalar) => new Vec2(a.X / scalar, a.Z / scalar);
        public static float Dot(Vec2 a, Vec2 b) => a.X * b.X + a.Z * b.Z;
        public static float Dist2(Vec2 a, Vec2 b) => (a - b).SqrLength;
        public static Vec2 Lerp(Vec2 a, Vec2 b, float t) => a + (b - a) * t;
        public static Vec2 ClosestOnSegment(Vec2 p, Vec2 a, Vec2 b)
        {
            Vec2 d = b - a;
            return d.SqrLength < 0.00001f ? a : a + d * Clock.Clamp01(Dot(p - a, d) / d.SqrLength);
        }
        public bool Equals(Vec2 other) => X.Equals(other.X) && Z.Equals(other.Z);
        public override bool Equals(object other) => other is Vec2 vector && Equals(vector);
        public override int GetHashCode() => unchecked(X.GetHashCode() * 397 ^ Z.GetHashCode());
        public override string ToString() => $"({X:0.00}, {Z:0.00})";
    }

    public static class Clock
    {
        public const int Hz = 30;
        public const float Step = 1f / Hz;
        public const float Tau = 2f * MathF.PI;
        public static int Ticks(float seconds) => (int)MathF.Round(seconds * Hz, MidpointRounding.AwayFromZero);
        public static float Clamp(float value, float min, float max) => MathF.Max(min, MathF.Min(max, value));
        public static float Clamp01(float value) => Clamp(value, 0, 1);
    }

    public struct SeededRandom
    {
        private uint state;
        public SeededRandom(int seed) { state = unchecked((uint)seed * 747796405u + 2891336453u); if (state == 0) state = 0x9e3779b9u; }
        public uint Next()
        {
            uint n = state;
            n ^= n << 13;
            n ^= n >> 17;
            n ^= n << 5;
            return state = n;
        }
        public float Unit() => (Next() >> 8) * (1f / 16777216f);
        public int Index(int length) => (int)(Next() % (uint)length);
        public float Range(float a, float b) => a + (b - a) * Unit();
        public uint State => state;
    }
}
