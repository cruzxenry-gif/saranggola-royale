using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Saranggola.Sim
{
    public sealed class Catalog
    {
        public readonly HeroSpec[] Heroes;
        public readonly WeaponSpec[] Weapons;
        public readonly ItemTable Items;
        public readonly MobTable Mobs;
        public readonly ZoneTable Zones;
        public readonly BalanceSpec Economy;
        public readonly ModeSpec Mode;

        private const string ModeJson = @"{
          ""size"": 200,
          ""start"": [60,150,240,330], ""radius"": [0.70,0.45,0.25,0.10],
          ""damage"": [0.01,0.02,0.035,0.06], ""shrink"": 32,
          ""initial_radius"": 142, ""sudden"": 480, ""sudden_radius"": 3.6,
          ""sudden_damage"": 0.1, ""end"": 540,
          ""drop_seconds"": 30, ""glide_seconds"": 3, ""dive_seconds"": 0.7, ""glide_speed"": 12.5,
          ""bleed"":20, ""revive"":3, ""finish"":1.5, ""soul_life"":25, ""beacon"":5,
          ""outpost"":6, ""outpost_gold"":3, ""buy"":3, ""combat_cooldown"":4,
          ""xp_curve"":[165,230,160,180,180,165,135], ""mob_respawn"":73, ""boss_spawn"":210,
          ""reaction_ticks"":[12,6,4], ""aim_error"":[0.35,0.15,0.08]
        }";

        public Catalog()
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() }
            };
            Heroes = JsonConvert.DeserializeObject<HeroSpec[]>(LegacyTables.Heroes, settings);
            Weapons = JsonConvert.DeserializeObject<WeaponSpec[]>(LegacyTables.Weapons, settings);
            Items = JsonConvert.DeserializeObject<ItemTable>(LegacyTables.Items, settings);
            Mobs = JsonConvert.DeserializeObject<MobTable>(LegacyTables.Mobs, settings);
            Zones = JsonConvert.DeserializeObject<ZoneTable>(LegacyTables.Zones, settings);
            Economy = JsonConvert.DeserializeObject<BalanceSpec>(LegacyTables.Balance, settings);
            Mode = JsonConvert.DeserializeObject<ModeSpec>(ModeJson, settings);
            Items.Shop = Array.FindAll(Items.Shop, item => item.Id != "revive");
            if (Heroes.Length != 7 || Weapons.Length != 10 || Mobs.Types.Length != 8)
                throw new InvalidOperationException("The archived rally roster is incomplete.");
        }
    }

    public sealed class HeroSpec
    {
        public string Id, DisplayName, Role, Color, Vfx;
        public HeroStats Stats;
        public PassiveSpec Passive;
        public AbilitySpec[] Abilities;
    }

    public sealed class HeroStats
    {
        public float Hp, Damage, Armor, Resist, Speed, Range, AttackSpeed, HitRadius;
        public bool Magic;
    }

    public sealed class PassiveSpec { public string Name, Description; }

    public sealed class AbilitySpec
    {
        public string Id, Name, Description, Shape, Short;
        public float Range, Cooldown;
        public Dictionary<string, float> Effect;
        public float Get(string key, float fallback = 0) => Effect.TryGetValue(key, out var value) ? value : fallback;
    }

    public sealed class WeaponSpec
    {
        public string Id, Name, Color;
        public int Rarity, Hits, MaxStacks;
        public float Damage = 1, Speed = 1;
        public float Cleave, ThirdCrit, Range, ReachBonus, ThirdStun, Poison, Radius, PercentHp, AttackStack, Stun, Cooldown;
        public bool Pierce;
    }

    public sealed class ItemSpec
    {
        public string Id, Name, Color;
        public float Cost, Duration, Heal, Shield, Speed, Cooldown, Damage, Armor, Resist, Hp;
    }

    public sealed class SupplySpec { public float OpenSeconds, Gold; public int MaxLiveLegendary; }

    public sealed class ItemTable
    {
        public ItemSpec[] Consumables, Shop;
        public float[][] SeedPoints;
        public SupplySpec Supply;
        public float DropReset;
    }

    public sealed class MobSpec
    {
        public string Id, Name, Buff, Color;
        public float Hp, Damage, Xp, Gold, Radius, Speed, Respawn, Spawn;
    }

    public sealed class CampSpec { public float[] At; public int Pack; public int[] Types; }

    public sealed class MobTable
    {
        public float Aggro, Leash, Windup;
        public MobSpec[] Types;
        public CampSpec[] Camps;
    }

    public sealed class ZoneSpec { public string Id, Name, Color; public int[] Rarity; }

    public sealed class ZoneTable
    {
        public ZoneSpec[] Zones;
        public float[][] Cover, Bushes, Lava, Vents;
        public float LaneSpeed;
    }

    public sealed class BalanceSpec
    {
        public float StartGold, PassiveGold, KillXp, KillLevelXp, KillGold, AssistGold, AssistWindow, PassiveRegen;
        public int MaxLevel;
    }

    public sealed class ModeSpec
    {
        public float Size, Shrink, InitialRadius, Sudden, SuddenRadius, SuddenDamage, End;
        public float[] Start, Radius, Damage;
        public float DropSeconds, GlideSeconds, DiveSeconds, GlideSpeed;
        public float Bleed, Revive, Finish, SoulLife, Beacon, Outpost, OutpostGold, Buy, CombatCooldown;
        public float MobRespawn, BossSpawn;
        public float[] XpCurve;
        public int[] ReactionTicks;
        public float[] AimError;
    }
}
