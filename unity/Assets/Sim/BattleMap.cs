using System;
using System.Collections.Generic;

namespace Saranggola.Sim
{
    public enum Zone { Road, Fields, Coast, Village, Forest, River, Market, Crater }

    public readonly struct Disc
    {
        public readonly Vec2 At;
        public readonly float Radius;
        public Disc(Vec2 at, float radius) { At = at; Radius = radius; }
        public bool Contains(Vec2 p, float extra = 0) => Vec2.Dist2(p, At) < (Radius + extra) * (Radius + extra);
    }

    public sealed class BattleMap
    {
        public const float Half = 100, RiverIn = 52.5f, RiverOut = 59.2f;
        public const float PlazaIn = 22.5f, PlazaOut = 40;
        private const float Rescale = 5f / 6f;
        private const int CellCount = 25, CellSize = 8;
        public readonly Disc[] Cover, Bushes, Lava;
        public readonly Vec2[] Landmarks, Stores, Outposts, Beacons, Vents, Crossings;
        private readonly List<int>[] cells = new List<int>[CellCount * CellCount];
        public readonly Catalog Data;

        public BattleMap(Catalog data)
        {
            Data = data;
            Cover = RotateDiscs(data.Zones.Cover);
            Bushes = RotateDiscs(data.Zones.Bushes);
            Lava = RotateDiscs(data.Zones.Lava);
            Stores = QuarterPairs(new Vec2(-77, -72), new Vec2(-46, -17));
            Outposts = QuarterPairs(new Vec2(-69, -38), new Vec2(-20, -33));
            Vents = QuarterPairs(new Vec2(-19,-14), new Vec2(-20,-5.8f));
            Landmarks = QuarterPairs(new Vec2(-72,-78), new Vec2(-30,-13));
            Beacons = new Vec2[6];
            for (int i = 0; i < 4; i++) Beacons[i] = Safe(new Vec2(-70, -72).Quarter(i));
            Beacons[4] = Safe(new Vec2(-39, 0));
            Beacons[5] = Safe(new Vec2(39, 0));
            Crossings = new Vec2[8];
            for (int i = 0; i < 8; i++) Crossings[i] = Vec2.Right.Rotate(i * MathF.PI / 4) * ((RiverIn + RiverOut) * 0.5f);
            for (int i = 0; i < cells.Length; i++) cells[i] = new List<int>(3);
            for (int i = 0; i < Cover.Length; i++)
            {
                var d = Cover[i];
                var r = d.Radius + 0.95f;
                for (int z = Cell(d.At.Z - r); z <= Cell(d.At.Z + r); z++)
                    for (int x = Cell(d.At.X - r); x <= Cell(d.At.X + r); x++)
                        cells[z * CellCount + x].Add(i);
            }
            KeepSymmetry(Stores);
            KeepSymmetry(Outposts);
            var beacon = Safe(Beacons[0]);
            for (int i = 0; i < 4; i++) Beacons[i] = beacon.Quarter(i);
            Beacons[4] = Safe(Beacons[4]);
            Beacons[5] = -Beacons[4];
        }

        private void KeepSymmetry(Vec2[] pair)
        {
            var a = Safe(pair[0]);
            var b = Safe(pair[1]);
            for (int i = 0; i < 4; i++)
            {
                pair[i * 2] = a.Quarter(i);
                pair[i * 2 + 1] = b.Quarter(i);
            }
        }

        private static Vec2[] QuarterPairs(Vec2 a, Vec2 b)
        {
            var result = new Vec2[8];
            for (int i = 0; i < 4; i++) { result[i * 2] = a.Quarter(i); result[i * 2 + 1] = b.Quarter(i); }
            return result;
        }

        private static Disc[] RotateDiscs(float[][] data)
        {
            var result = new Disc[data.Length * 4];
            for (int i = 0; i < result.Length; i++)
            {
                var point = data[i % data.Length];
                result[i] = new Disc((new Vec2(point[0], point[1]) * Rescale).Quarter(i / data.Length), point[2] * Rescale);
            }
            return result;
        }

        public static Vec2 Clamp(Vec2 p) => new Vec2(Clock.Clamp(p.X, -99, 99), Clock.Clamp(p.Z, -99, 99));
        private static int Cell(float v) => Math.Clamp((int)((v + Half) / CellSize), 0, CellCount - 1);
        public static int Quadrant(Vec2 p) => p.Z < 0 ? (p.X < 0 ? 0 : 1) : (p.X >= 0 ? 2 : 3);
        public static bool Diagonal(Vec2 p, float width) => MathF.Abs(MathF.Abs(p.X) - MathF.Abs(p.Z)) < width * 1.414214f;
        public static bool River(Vec2 p) => p.SqrLength > RiverIn * RiverIn && p.SqrLength < RiverOut * RiverOut;
        public static bool Bridge(Vec2 p) => Diagonal(p, 2.05f);
        public static bool Ford(Vec2 p) => MathF.Min(MathF.Abs(p.X), MathF.Abs(p.Z)) < 4.5f;
        public static bool HighGround(Vec2 p) => p.SqrLength < 10 * 10 || p.SqrLength > 86 * 86;
        public static Zone ZoneAt(Vec2 p)
        {
            float r = p.SqrLength;
            if (r < PlazaIn * PlazaIn) return Zone.Crater;
            if (River(p) && !Bridge(p)) return Zone.River;
            if (Diagonal(p, 2.65f)) return Zone.Road;
            if (r < PlazaOut * PlazaOut) return Zone.Market;
            switch (Quadrant(p))
            {
                case 0: return Zone.Forest;
                case 1: return Zone.Fields;
                case 2: return Zone.Coast;
                default: return Zone.Village;
            }
        }

        public float GroundSpeed(Vec2 p, bool amphibious)
        {
            if (River(p) && !Bridge(p)) return amphibious ? 1 : 0.52f;
            if (Diagonal(p, 2.65f)) return Data.Zones.LaneSpeed;
            return p.SqrLength > 70 * 70 && MathF.Max(MathF.Abs(p.X), MathF.Abs(p.Z)) % 8 < 1.7f ? .79f : 1;
        }

        public bool Clear(Vec2 p, float margin)
        {
            var list = cells[Cell(p.Z) * CellCount + Cell(p.X)];
            if (list == null) return true;
            for (int i = 0; i < list.Count; i++) if (Cover[list[i]].Contains(p, margin)) return false;
            return true;
        }

        public bool Walkable(Vec2 p) => (!River(p) || Bridge(p) || Ford(p)) && Clear(p, .48f);
        public bool Sight(Vec2 start, Vec2 end)
        {
            var samples = Math.Max(1, (int)MathF.Ceiling((end - start).Length / 1.1f));
            for (int i = 1; i <= samples; i++)
                if (!Clear(Vec2.Lerp(start, end, i / (float)samples), .03f)) return false;
            return true;
        }

        public Vec2 Move(Vec2 from, Vec2 delta, bool airborne)
        {
            var to = Clamp(from + delta);
            if (airborne || Walkable(to)) return to;
            var slide = new Vec2(to.X, from.Z);
            if (Walkable(slide)) return slide;
            slide = new Vec2(from.X, to.Z);
            return Walkable(slide) ? slide : from;
        }

        public Vec2 Safe(Vec2 p)
        {
            p = Clamp(p);
            if (Walkable(p)) return p;
            for (int radius = 2; radius <= 10; radius += 2)
                for (int i = 0; i < 8; i++)
                {
                    var side = Clamp(p + Vec2.Right.Rotate(i * Clock.Tau / 8) * radius);
                    if (Walkable(side)) return side;
                }
            return Vec2.Zero;
        }

        public bool InBush(Vec2 p, float extra = 0)
        {
            for (int i = 0; i < Bushes.Length; i++) if (Bushes[i].Contains(p, extra)) return true;
            return false;
        }

        public bool InLava(Vec2 p)
        {
            if (p.SqrLength > 31 * 31) return false;
            for (int i = 0; i < Lava.Length; i++) if (Lava[i].Contains(p)) return true;
            return false;
        }

        public Vec2 TravelWaypoint(Vec2 from, Vec2 destination)
        {
            bool inner = from.SqrLength < RiverIn * RiverIn;
            bool farOuter = from.SqrLength > RiverOut * RiverOut;
            bool targetInner = destination.SqrLength < RiverIn * RiverIn;
            bool targetOuter = destination.SqrLength > RiverOut * RiverOut;
            if (!(inner && targetOuter) && !(farOuter && targetInner) && !River(from)) return destination;
            var best = float.PositiveInfinity;
            Vec2 crossing = Crossings[0];
            for (int i = 0; i < Crossings.Length; i++)
            {
                var candidate = Crossings[i];
                float score = (candidate - from).Length + (candidate - destination).Length * 0.62f + (i % 2 == 0 ? 2.7f : 0);
                if (score < best) { best = score; crossing = candidate; }
            }
            var direction = crossing.Normalized;
            var entry = direction * (inner ? RiverIn - .8f : RiverOut + .8f);
            if (!River(from) && Vec2.Dist2(from, entry) > 2.1f * 2.1f) return entry;
            return direction * (targetOuter ? RiverOut + 1.6f : RiverIn - 1.6f);
        }
    }
}
