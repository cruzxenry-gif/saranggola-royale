using System;

namespace Saranggola.Sim
{
    public enum Life { Riding, Gliding, Active, Downed, Eliminated }
    public enum UnitKind { Hero, Mob, Mirror }
    public enum Status
    {
        Slow, Root, Stun, Air, Silence, Invisible, Burn, Poison, Haste, Rage, Flight, Mark,
        Flag, Exhaust, Tree, Healing, Detached, Blood, Horse, Sight, Water, Fire, Eclipse, AttackSlow, Shard, Count
    }
    public enum EventKind { Damage, Burst, Ultimate, Telegraph, Death, Drop, Loot, Warning, Down, Revive, Ping, Bounty, Purchase, Capture, Coin, Channel, Hoy }
    public enum GoldFrom { Mobs, Kill, Assist, Passive, Pickup, Outpost, Bounty, Stolen, Count }
    public enum ChannelKind { None, Revive, Finish, Beacon, Shop, Supply }
    public enum DamageKind { Physical, Magic, Storm, Hazard }

    public readonly struct DamageSample
    {
        public readonly int Tick, Source;
        public readonly float Amount;
        public readonly DamageKind Kind;
        public DamageSample(int tick, int source, float amount, DamageKind kind)
        { Tick = tick; Source = source; Amount = amount; Kind = kind; }
    }

    public struct SkillInput
    {
        public Vec2 Move, Aim;
        public int Lock, Swap, Shop, Rank, Ability, Consume;
        public bool Attack, Interact, Jump, Dive;
        public static SkillInput Empty => new SkillInput { Lock = -2, Swap = -1, Shop = -1, Rank = -1, Ability = -1, Consume = -1 };
    }

    public readonly struct ArenaEvent
    {
        public readonly EventKind Kind;
        public readonly int Tick, Source, Target;
        public readonly Vec2 At;
        public readonly float Amount;
        public ArenaEvent(EventKind kind, int tick, Vec2 at, float amount, int source, int target)
        { Kind = kind; Tick = tick; At = at; Amount = amount; Source = source; Target = target; }
    }

    public sealed class Actor
    {
        private static readonly Status[] Debuffs =
        { Status.Slow, Status.Root, Status.Stun, Status.Air, Status.Silence, Status.Burn, Status.Poison, Status.Mark, Status.AttackSlow };

        public readonly int Id;
        public UnitKind Kind;
        public Life Life;
        public HeroSpec Profile;
        public MobSpec Mob;
        public int Team, Costume, Owner = -1;
        public string Name => Kind == UnitKind.Mob ? Mob.Name : Kind == UnitKind.Mirror ? "Salamin" : Profile.DisplayName;
        public Vec2 Position, Previous, Direction = Vec2.Forward, Motion, FlightDirection, Home, MobAim;
        public float Hp, MaxHp, Shield, Armor, Resist, Atk, Speed, Range, AttackSpeed, Radius;
        public bool Magic;
        public float Gold, Xp, DamageDealt, DamageTaken, Rage, CooldownBonus;
        public readonly float[] Earnings = new float[(int)GoldFrom.Count];
        public readonly int[] Rank = { 1, 1, 0 };
        public readonly int[] Cooldowns = new int[3];
        public readonly int[] StatusEnd = new int[(int)Status.Count];
        public readonly float[] Strength = new float[(int)Status.Count];
        public readonly int[] Aggressors = new int[12];
        public readonly int[] LevelAt = new int[9];
        public readonly DamageSample[] Recap = new DamageSample[96];
        public readonly int[] Items = { -1, -1 };
        public int Level = 1, SkillPoints, Kills, Deaths, Assists, Streak, Mobs, Pickups, RecapHead, DeathTick;
        public int AttackEnd, LastAttack = -10000, LastDamage = -10000, LastCombat = -10000, LastReveal = -10000;
        public int Attacker = -1, Lock = -1, LastCc = -10000, DotSource = -1, PoisonStacks, AttackCount;
        public int Weapon = -1, Durability, WeaponHits, WeaponStacks, WeaponReady, Bought;
        public int DownUntil, DownedBy = -1, GraceEnds, GlideUntil, GlideStarted, Stationary, PassiveReady, FlightSlot = -1;
        public DamageKind DownKind;
        public int EclipseFogUntil;
        public int TripleHits, TripleTarget = -1, TripleUntil, MirrorUntil;
        public int SpawnAt, MobPattern, MobWindup, MobTarget = -1, CampStart = -1, MobFleeUntil, CampRespawns;
        public int ChannelUntil, ChannelTarget = -1, ChannelStart, ChannelDamageVersion, DamageVersion;
        public ChannelKind Channel;
        public Vec2 ChannelOrigin;
        public int CarriedSoul = -1;
        public int LavaTicks;
        public bool Human;
        public bool Active => Life == Life.Active;
        public bool Grounded => Life == Life.Active || Life == Life.Downed;
        public bool Alive => Life != Life.Eliminated;
        public float HealthFraction => MaxHp > 0 ? Hp / MaxHp : 0;

        public Actor(int id)
        {
            Id = id;
            Array.Fill(Aggressors, -10000);
        }

        public void AssignHero(HeroSpec hero, int costume, int team, float gold)
        {
            Profile = hero;
            Costume = costume;
            Team = team;
            Kind = UnitKind.Hero;
            var def = hero.Stats;
            Hp = MaxHp = def.Hp;
            Atk = def.Damage;
            Armor = def.Armor;
            Resist = def.Resist;
            Speed = def.Speed;
            Range = def.Range;
            AttackSpeed = def.AttackSpeed;
            Radius = def.HitRadius;
            Magic = def.Magic;
            Gold = gold;
        }

        public bool Has(Status status, int tick) => StatusEnd[(int)status] > tick;
        public bool Incapacitated(int tick) => Has(Status.Stun, tick) || Has(Status.Air, tick);
        public void Apply(Status effect, int tick, float length, float power = 0)
        {
            if ((effect == Status.Slow || effect == Status.Air) && Has(Status.Rage, tick)) return;
            if (effect == Status.Root || effect == Status.Stun || effect == Status.Air)
            {
                if (tick - LastCc < 5 * Clock.Hz) length *= 0.5f;
                LastCc = tick;
            }
            var index = (int)effect;
            StatusEnd[index] = Math.Max(StatusEnd[index], tick + Math.Max(1, Clock.Ticks(length)));
            Strength[index] = power;
        }

        public void Cleanse()
        {
            for (int i = 0; i < Debuffs.Length; i++) StatusEnd[(int)Debuffs[i]] = 0;
            PoisonStacks = 0;
        }

        public void Reveal(int now)
        {
            LastReveal = now;
            StatusEnd[(int)Status.Invisible] = 0;
            Stationary = 0;
        }

        public void Heal(float amount) { if (Active) Hp = MathF.Min(MaxHp, Hp + amount); }
        public int SkillCap(int slot) => slot < 2 ? Math.Min(4, Level) : Level < 3 ? 0 : Level < 5 ? 1 : Level < 7 ? 2 : 3;
        public bool Improve(int slot)
        {
            if (slot < 0 || slot > 2 || SkillPoints <= 0 || Rank[slot] >= SkillCap(slot)) return false;
            Rank[slot]++;
            SkillPoints--;
            return true;
        }

        public float WalkSpeed(int tick)
        {
            var bonus = 1f + (Has(Status.Haste, tick) ? Strength[(int)Status.Haste] : 0) +
                (Has(Status.Detached, tick) ? 0.25f : 0) + (Has(Status.Horse, tick) ? 0.2f : 0);
            if (Costume == 3 && Kind == UnitKind.Hero && Has(Status.Flight, tick)) bonus += 0.2f;
            if (Has(Status.Slow, tick) && !Has(Status.Rage, tick)) bonus *= 1 - Strength[(int)Status.Slow];
            if (Has(Status.Exhaust, tick) && !Has(Status.Rage, tick)) bonus *= 0.76f;
            return Speed * bonus;
        }
        public float Power(int tick) => Atk * (Has(Status.Flag, tick) ? 1.30f : 1) * (Has(Status.Eclipse, tick) ? 1.15f : 1);
        public float Rate(int tick) => AttackSpeed * (Has(Status.Flag, tick) ? 1.30f : 1) * (Has(Status.AttackSlow, tick) ? 0.85f : 1);
        public float Lifesteal(int tick) => (Has(Status.Detached, tick) ? 0.15f : 0) + (Has(Status.Blood, tick) ? 0.12f : 0) + (Has(Status.Eclipse, tick) ? 0.10f : 0);
    }
}
