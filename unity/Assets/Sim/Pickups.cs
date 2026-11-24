using System;

namespace Saranggola.Sim
{
    public enum LootKind { Weapon, Consumable, Coin, Box }
    public struct Pickup
    {
        public Vec2 At;
        public int Item, Rarity, Durability, Ready, Expires;
        public LootKind Kind;
        public bool Live(int tick) => Expires > tick;
    }

    public sealed partial class Match
    {
        public readonly Pickup[] Loot = new Pickup[180];
        public int LootCount { get; private set; }
        public int SuppliesOpened { get; private set; }
        public int LegendaryCount { get; private set; }

        partial void InitLoot()
        {
            foreach (var seed in Data.Items.SeedPoints)
            {
                var point = new Vec2(seed[0], seed[1]) * (5f / 6);
                // Draw one template for each group of four. Zone odds select the template,
                // then all four rotations receive identical pickups.
                var zone = BattleMap.ZoneAt(point.Quarter(Rng.Index(4)));
                var odds = Data.Zones.Zones[(int)zone].Rarity ?? Data.Zones.Zones[(int)Zone.Village].Rarity;
                int roll = Rng.Index(100);
                int rarity = roll < odds[0] ? 0 : roll < odds[0] + odds[1] ? 1 : 2;
                bool consumable = Rng.Unit() < .35f;
                if (rarity == 2 && LegendaryCount + 4 > Data.Items.Supply.MaxLiveLegendary) rarity = 1;
                if (consumable) rarity = 0;
                int item = consumable ? Rng.Index(Data.Items.Consumables.Length) : RandomWeapon(rarity);
                point = Map.Safe(point);
                for (int side = 0; side < 4; side++)
                    SpawnLoot(point.Quarter(side), consumable ? LootKind.Consumable : LootKind.Weapon,
                        item, rarity, consumable ? 0 : Data.Weapons[item].Hits);
            }
        }

        private int RandomWeapon(int rarity)
        {
            int selected = 0, seen = 0;
            for (int i = 0; i < Data.Weapons.Length; i++)
                if (Data.Weapons[i].Rarity == rarity && Rng.Index(++seen) == 0) selected = i;
            return selected;
        }

        private int SpawnLoot(Vec2 at, LootKind kind, int item, int rarity = 0, int hits = 0,
                              int lifetime = int.MaxValue, int ready = 0)
        {
            for (int i = 0; i < Loot.Length; i++)
            {
                if (Loot[i].Live(Tick)) continue;
                Loot[i] = new Pickup { At = at, Kind = kind, Item = item, Rarity = rarity, Durability = hits,
                    Expires = lifetime == int.MaxValue ? lifetime : Tick + lifetime, Ready = Tick + ready };
                if (kind == LootKind.Weapon && rarity == 2) LegendaryCount++;
                LootCount = Math.Max(LootCount, i + 1);
                return i;
            }
            return -1;
        }

        private void RemoveLoot(int index)
        {
            if (Loot[index].Expires == 0) return;
            if (Loot[index].Kind == LootKind.Weapon && Loot[index].Rarity == 2) LegendaryCount--;
            Loot[index].Expires = 0;
        }

        partial void AdvanceLoot()
        {
            for (int i = 0; i < LootCount; i++)
            {
                ref var drop = ref Loot[i];
                if (drop.Expires == 0) continue;
                if (!drop.Live(Tick)) { RemoveLoot(i); continue; }
                if (drop.Ready > Tick || drop.Kind == LootKind.Box) continue;
                foreach (var hero in Heroes)
                {
                    if (!hero.Active || (Tick + hero.Id) % 3 != 0 || Vec2.Dist2(hero.Position, drop.At) > 1.4f * 1.4f) continue;
                    if (drop.Kind == LootKind.Weapon && hero.Weapon >= 0) continue;
                    if (Take(hero, i)) break;
                }
            }
        }

        public bool Take(Actor hero, int index)
        {
            if (!hero.Active || hero.Channel != ChannelKind.None || (uint)index >= (uint)LootCount) return false;
            var p = Loot[index];
            if (!p.Live(Tick) || p.Ready > Tick || Vec2.Dist2(hero.Position, p.At) > 3.25f * 3.25f) return false;
            switch (p.Kind)
            {
                case LootKind.Weapon:
                    RemoveLoot(index);
                    DropWeapon(hero);
                    hero.Weapon = p.Item;
                    hero.Durability = p.Durability;
                    break;
                case LootKind.Consumable:
                    int slot = hero.Items[0] < 0 ? 0 : hero.Items[1] < 0 ? 1 : -1;
                    if (slot < 0) return false;
                    hero.Items[slot] = p.Item;
                    RemoveLoot(index);
                    break;
                case LootKind.Coin:
                    GainGold(hero, 5, GoldFrom.Pickup);
                    hero.Heal(15);
                    RemoveLoot(index);
                    break;
                default:
                    return false;
            }
            hero.Pickups++;
            Emit(EventKind.Loot, p.At, p.Rarity, hero.Id);
            return true;
        }

        public bool UseItem(Actor hero, int slot)
        {
            if ((uint)slot >= (uint)hero.Items.Length || !hero.Active || hero.Channel != ChannelKind.None || hero.Items[slot] < 0) return false;
            var spec = Data.Items.Consumables[hero.Items[slot]];
            hero.Items[slot] = -1;
            if (spec.Heal > 0) hero.Apply(Status.Healing, Tick, spec.Duration, hero.MaxHp * spec.Heal / spec.Duration);
            if (spec.Shield > 0) hero.Shield += spec.Shield;
            if (spec.Speed > 0) hero.Apply(Status.Haste, Tick, spec.Duration, spec.Speed);
            if (spec.Cooldown > 0) hero.Apply(Status.Shard, Tick, spec.Duration, spec.Cooldown);
            Emit(EventKind.Burst, hero.Position, 1.6f, hero.Id);
            return true;
        }

        partial void InputLoot(Actor hero, SkillInput input)
        {
            if (input.Swap >= 0) Take(hero, input.Swap);
            if (input.Consume >= 0) UseItem(hero, input.Consume);
        }

        partial void DropWeapon(Actor hero)
        {
            if (hero.Weapon < 0) return;
            var spec = Data.Weapons[hero.Weapon];
            if (spec.Rarity < 2 || LegendaryCount < Data.Items.Supply.MaxLiveLegendary)
                SpawnLoot(Map.Safe(hero.Position + hero.Direction * .6f), LootKind.Weapon, hero.Weapon,
                    spec.Rarity, hero.Durability, Clock.Ticks(Data.Items.DropReset), Clock.Ticks(1.1f));
            ResetWeapon(hero);
        }

        private static void ResetWeapon(Actor hero)
        {
            hero.Weapon = -1;
            hero.WeaponHits = hero.WeaponStacks = hero.WeaponReady = hero.Durability = 0;
        }

        private int Held(Actor hero) => hero.Weapon == 9 && hero.WeaponReady > Tick ? -1 : hero.Weapon;

        partial void WeaponReach(Actor hero, ref float range)
        {
            int weapon = Held(hero);
            if (weapon < 0) return;
            var spec = Data.Weapons[weapon];
            range = spec.Range > 0 ? spec.Range : range + spec.ReachBonus;
        }

        partial void WeaponRate(Actor hero, ref float rate)
        {
            int weapon = Held(hero);
            if (weapon < 0) return;
            var spec = Data.Weapons[weapon];
            rate *= spec.Speed + hero.WeaponStacks * spec.AttackStack;
        }

        partial void AttackWeapon(Actor hero, Actor target, float reach, float damage, ref bool armed)
        {
            int weapon = Held(hero);
            if (weapon < 0) return;
            armed = true;
            var spec = Data.Weapons[weapon];
            float raw = damage * spec.Damage;
            bool third = ++hero.WeaponHits % 3 == 0;
            var direction = hero.Direction;
            switch (weapon)
            {
                case 0:
                case 7:
                    for (int i = 0; i < ActorCount; i++)
                    {
                        var foe = Actors[i];
                        if (!Enemy(hero, foe, false)) continue;
                        var delta = foe.Position - hero.Position;
                        float limit = reach + foe.Radius;
                        if (delta.SqrLength <= limit*limit && (delta.SqrLength < .0004f || Vec2.Dot(direction, delta.Normalized) >= spec.Cleave)
                            && Map.Sight(hero.Position, foe.Position)) Hit(foe, hero, raw + foe.Hp * spec.PercentHp);
                    }
                    break;
                case 1: Hit(target, hero, raw * (third ? spec.ThirdCrit : 1)); break;
                case 2: Fire(hero, direction, reach + .6f, raw, DamageKind.Physical, 25, .24f, focus: target.Id); break;
                case 3: Line(hero, hero.Position, hero.Position + direction * (reach + .6f), .45f, raw, DamageKind.Physical, -1, false, out _); break;
                case 4:
                    Hit(target, hero, raw);
                    if (third) StatusOn(target, Status.Stun, spec.ThirdStun);
                    break;
                case 5: Fire(hero, direction, reach + .5f, raw * .84f, DamageKind.Physical, 25, .27f, trait: ShotTrait.Poison, focus: target.Id); break;
                case 6: Fire(hero, direction, reach + .55f, raw, DamageKind.Physical, 13, .42f, trait: ShotTrait.Pot, focus: target.Id); break;
                case 8:
                    hero.WeaponStacks = Math.Min(spec.MaxStacks, hero.WeaponStacks + 1);
                    Area(hero, hero.Position, reach + .3f, raw, direction: direction, dot: -.15f);
                    break;
                case 9:
                    hero.WeaponReady = Tick + Clock.Ticks(spec.Cooldown);
                    Fire(hero, direction, reach + 1, raw * 1.35f, DamageKind.Physical, 20, .4f, trait: ShotTrait.Slipper, focus: target.Id);
                    break;
            }
            if (--hero.Durability <= 0) ResetWeapon(hero);
        }

        partial void LuckyCoin(Vec2 point) => SpawnLoot(point, LootKind.Coin, 0, lifetime: Clock.Ticks(16), ready: 7);

        partial void OnNewPhase()
        {
            var at = Map.Safe(Bagyo.NextCenter + Vec2.Right.Rotate(Rng.Unit() * Clock.Tau) * MathF.Sqrt(Rng.Unit()) * (Bagyo.NextRadius * .73f));
            SpawnLoot(at, LootKind.Box, 0, 2, lifetime: Clock.Ticks(105), ready: Clock.Ticks(3.5f));
            Emit(EventKind.Warning, at, 2);
        }

        partial void FindSupply(Actor hero, ref int found)
        {
            for (int i = 0; i < LootCount; i++)
                if (IsSupply(hero, i)) { found = i; return; }
        }

        partial void CheckSupply(Actor hero, int index, ref bool near) => near = IsSupply(hero, index);

        private bool IsSupply(Actor hero, int index) => (uint)index < (uint)LootCount &&
            Loot[index].Kind == LootKind.Box && Loot[index].Live(Tick) && Loot[index].Ready <= Tick &&
            Vec2.Dist2(hero.Position, Loot[index].At) < 3.1f * 3.1f;

        partial void CompleteSupply(Actor hero, int slot)
        {
            if (!IsSupply(hero, slot)) return;
            var at = Loot[slot].At;
            RemoveLoot(slot);
            if (LegendaryCount >= Data.Items.Supply.MaxLiveLegendary)
            {
                int oldest = -1;
                for (int i = 0; i < LootCount; i++)
                    if (Loot[i].Kind == LootKind.Weapon && Loot[i].Rarity == 2 && Loot[i].Live(Tick) &&
                        (oldest < 0 || Loot[i].Ready < Loot[oldest].Ready)) oldest = i;
                if (oldest >= 0) RemoveLoot(oldest);
            }
            int item = RandomWeapon(2);
            SpawnLoot(at, LootKind.Weapon, item, 2, Data.Weapons[item].Hits, Clock.Ticks(100));
            GainGold(hero, Data.Items.Supply.Gold, GoldFrom.Pickup);
            SuppliesOpened++;
            Emit(EventKind.Loot, at, 2, hero.Id);
        }
    }
}
