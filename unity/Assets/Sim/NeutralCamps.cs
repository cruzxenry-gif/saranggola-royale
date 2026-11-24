using System;

namespace Saranggola.Sim
{
    public sealed partial class Match
    {
        public Actor Boss { get; private set; }
        public int FirstMob { get; private set; }
        public readonly int[] CampClears = new int[8];
        public readonly long[] CampClearTicks = new long[8];

        partial void InitMobs()
        {
            FirstMob = ActorCount;
            if (Mode == PlayMode.Practice) return;
            for (int turn = 0; turn < 4; turn++)
            {
                for (int i = 0; i < Data.Mobs.Camps.Length; i++)
                {
                    var camp = Data.Mobs.Camps[i];
                    var p = new Vec2(camp.At[0], camp.At[1]) * (5f / 6);
                    for (int j = 0; j < camp.Pack; j++)
                    {
                        var offset = Vec2.Right.Rotate(j * Clock.Tau / 3) * (camp.Pack > 1 ? 1.75f : 0);
                        SpawnMob(camp.Types[0], Map.Safe(p + offset).Quarter(turn));
                    }
                }
                SpawnMob(3, Map.Safe(new Vec2(-58, -77)).Quarter(turn));
            }
            Boss = SpawnMob(7, Vec2.Zero);
            Boss.Life = Life.Eliminated;
            Boss.SpawnAt = Clock.Ticks(Data.Mode.BossSpawn);
        }

        private Actor SpawnMob(int costume, Vec2 position)
        {
            var mob = AddActor();
            mob.Kind = UnitKind.Mob;
            mob.Life = Life.Active;
            mob.Costume = costume;
            mob.Mob = Data.Mobs.Types[costume];
            mob.Team = -1;
            mob.Hp = mob.MaxHp = mob.Mob.Hp;
            mob.Atk = mob.Mob.Damage;
            mob.Speed = mob.Mob.Speed;
            mob.Radius = mob.Mob.Radius;
            mob.Armor = mob.Resist = costume == 7 ? 15 : 5;
            mob.Position = mob.Previous = mob.Home = position;
            return mob;
        }

        partial void AdvanceMobs()
        {
            for (int i = FirstMob; i < ActorCount; i++)
            {
                var mob = Actors[i];
                if (!mob.Active)
                {
                    if (mob.SpawnAt > 0 && Tick >= mob.SpawnAt && (Bagyo.Phase < 3 || mob == Boss))
                    {
                        ResetMob(mob, true);
                        if (mob == Boss) Emit(EventKind.Warning, mob.Home, 7, mob.Id);
                    }
                    continue;
                }

                mob.Previous = mob.Position;
                int frequency = mob.MobTarget >= 0 ? 6 : 15;
                if (Tick % frequency == i % frequency) DecideMob(mob);
                if (Tick % 15 == i % 15) Dots(mob);
                if (!mob.Active) continue;
                if (mob.MobWindup > 0 && Tick >= mob.MobWindup)
                {
                    mob.MobWindup = 0;
                    if (!mob.Incapacitated(Tick)) MobStrike(mob);
                }
                if (mob.MobWindup == 0 && !mob.Incapacitated(Tick) && !mob.Has(Status.Root, Tick))
                    mob.Position = Map.Move(mob.Position, mob.Motion * (mob.WalkSpeed(Tick) * Clock.Step), mob.Costume == 4 || mob.Costume == 6);
            }
        }

        private static void ResetMob(Actor mob, bool respawn)
        {
            mob.Hp = mob.MaxHp;
            mob.MobTarget = mob.Attacker = mob.CampStart = -1;
            mob.LastDamage = -10000;
            mob.MobWindup = mob.SpawnAt = mob.MobFleeUntil = 0;
            Array.Clear(mob.StatusEnd, 0, mob.StatusEnd.Length);
            if (respawn)
            {
                mob.Life = Life.Active;
                mob.Position = mob.Previous = mob.Home;
            }
            mob.Motion = Vec2.Zero;
        }

        private void DecideMob(Actor mob)
        {
            var tune = Data.Mobs;
            var enemy = At(mob.MobTarget);
            float leash = tune.Leash * tune.Leash;
            if (enemy != null && (!enemy.Active || Vec2.Dist2(enemy.Position, mob.Home) > leash || Vec2.Dist2(mob.Position, mob.Home) > leash))
            {
                if (mob.Costume == 1 && mob.MobFleeUntil > 0) mob.MobTarget = -1;
                else ResetMob(mob, false);
                enemy = null;
            }

            if (enemy == null && Vec2.Dist2(mob.Position, mob.Home) < leash * .91f)
            {
                var recent = At(mob.Attacker);
                enemy = recent != null && recent.Active && Tick - mob.LastDamage < 90 &&
                    Vec2.Dist2(recent.Position, mob.Home) < leash ? recent : Target(mob, tune.Aggro, UnitKind.Hero, false);
                if (enemy != null)
                {
                    mob.MobTarget = enemy.Id;
                    if (mob.CampStart < 0) mob.CampStart = Tick;
                    if (mob.Costume == 1 && mob.MobFleeUntil == 0) mob.MobFleeUntil = Tick + Clock.Ticks(25);
                }
            }

            if (mob.Costume == 1 && mob.MobFleeUntil > 0)
            {
                if (Tick >= mob.MobFleeUntil) { RetireMob(mob); return; }
                if (enemy != null)
                {
                    var home = (mob.Home - mob.Position).Normalized;
                    mob.Motion = Vec2.Lerp((mob.Position - enemy.Position).Normalized, home,
                        Vec2.Dist2(mob.Home, mob.Position) > leash * .82f ? .76f : .27f).Normalized;
                    mob.Direction = mob.Motion;
                }
                else mob.Motion = (mob.Home - mob.Position).Normalized;
                return;
            }

            if (enemy == null)
            {
                var home = mob.Home - mob.Position;
                mob.Motion = home.Length > .55f ? home.Normalized : Vec2.Zero;
                return;
            }
            if (mob.MobWindup > 0) return;
            var offset = enemy.Position - mob.Position;
            float reach = mob.Costume == 7 ? 5.25f : mob.Costume == 5 ? 3.2f : 1.6f;
            mob.Direction = offset.Normalized;
            mob.Motion = offset.Length > reach * .84f ? mob.Direction : Vec2.Zero;
            if (offset.Length > reach || mob.AttackEnd > Tick || mob.Incapacitated(Tick) || !Map.Sight(mob.Position, enemy.Position)) return;
            mob.AttackEnd = Tick + Clock.Ticks(mob.Costume == 7 ? 2.4f : 1.58f);
            if ((++mob.MobPattern % 2 == 0 || mob.Costume == 7) && !mob.Has(Status.Silence, Tick))
            {
                mob.MobAim = enemy.Position;
                mob.MobWindup = Tick + Clock.Ticks(tune.Windup);
                Emit(EventKind.Telegraph, mob.Costume == 7 ? mob.Position : mob.MobAim, mob.Costume == 7 ? 6.2f : 2.25f, mob.Id);
            }
            else
            {
                Hit(enemy, mob, mob.Atk, mob.Costume == 6 ? DamageKind.Magic : DamageKind.Physical);
                if (mob.Costume == 6) StatusOn(enemy, Status.Burn, 3, 13, mob);
            }
        }

        private void MobStrike(Actor mob)
        {
            var dir = (mob.MobAim - mob.Position).Normalized;
            switch (mob.Costume)
            {
                case 0:
                case 2:
                    mob.Position = Map.Safe(mob.Position + (mob.MobAim - mob.Position).Limit(2.5f));
                    Area(mob, mob.Position, 2.15f, mob.Atk * 1.35f, effect: Status.Slow, duration: 1, power: .25f);
                    break;
                case 3: Area(mob, mob.Position, 2.85f, mob.Atk * 1.55f, direction: dir, dot: .34f, effect: Status.Air, duration: .35f); break;
                case 4: Area(mob, mob.Position, 3.35f, mob.Atk * 1.4f, DamageKind.Magic, effect: Status.Silence, duration: .7f); break;
                case 5: Area(mob, mob.MobAim, 2.50f, mob.Atk * 1.25f, effect: Status.Root, duration: 1.25f); break;
                case 6: Area(mob, mob.MobAim, 2.60f, mob.Atk, DamageKind.Magic, effect: Status.Burn, duration: 3, power: 17); break;
                case 7:
                    switch (mob.MobPattern % 3)
                    {
                        case 0: Area(mob, mob.Position, 7.6f, mob.Atk * 1.65f, DamageKind.Magic, direction: dir, dot: .56f, effect: Status.Slow, duration: 1.6f, power: .28f); break;
                        case 1: Area(mob, mob.Position, 6, mob.Atk * 1.70f, effect: Status.Air, duration: .5f); break;
                        case 2:
                            for (int i = -2; i <= 2; i++) Fire(mob, dir.Rotate(i * .22f), 13, mob.Atk * 1.18f, DamageKind.Magic, 13, .64f);
                            break;
                    }
                    break;
            }
            Emit(EventKind.Burst, mob.Position, mob.Costume == 7 ? 6.3f : 2.5f, mob.Id);
        }

        private void RetireMob(Actor mob)
        {
            mob.Life = Life.Eliminated;
            mob.SpawnAt = mob.Costume != 7 && Bagyo.Phase < 3 && mob.CampRespawns++ == 0
                ? Tick + Clock.Ticks(Data.Mode.MobRespawn) : 0;
        }

        partial void MobDefeated(Actor mob, Actor source)
        {
            RetireMob(mob);
            CampClears[mob.Costume]++;
            CampClearTicks[mob.Costume] += Math.Max(1, Tick - (mob.CampStart < 0 ? Tick : mob.CampStart));
            if (mob.Costume == 6)
            {
                Area(mob, mob.Position, 2.65f, 48, DamageKind.Magic, effect: Status.Burn, duration: 2, power: 15);
                Emit(EventKind.Burst, mob.Position, 2.65f, mob.Id);
            }
            if (source == null || source.Kind != UnitKind.Hero) return;
            source.Mobs++;
            if (mob.Costume == 7)
            {
                foreach (var hero in Heroes)
                {
                    if (hero.Team != source.Team) continue;
                    GainGold(hero, mob.Mob.Gold, GoldFrom.Mobs);
                    AwardXp(hero, mob.Mob.Xp);
                    hero.Apply(Status.Eclipse, Tick, 90);
                    hero.EclipseFogUntil = Tick + Clock.Ticks(30);
                }
                Emit(EventKind.Warning, mob.Position, 8, source.Id);
                return;
            }
            GainGold(source, mob.Mob.Gold, GoldFrom.Mobs);
            AwardXp(source, mob.Mob.Xp);
            switch (mob.Costume)
            {
                case 2: source.Apply(Status.Blood, Tick, 60); break;
                case 3: source.Apply(Status.Horse, Tick, 60); break;
                case 4: source.Apply(Status.Sight, Tick, 60); break;
                case 5: source.Apply(Status.Water, Tick, 60); break;
                case 6: source.Apply(Status.Fire, Tick, 60); break;
            }
        }
    }
}
