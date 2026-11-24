using System;

namespace Saranggola.Sim
{
    public sealed partial class Match
    {
        private Vec2 AimPoint(Actor hero, Actor enemy, Vec2 direction, bool automatic, float reach)
            => automatic && enemy != null ? hero.Position.Toward(enemy.Position, reach) : hero.Position + direction * reach;

        public bool Cast(Actor hero, int slot, Vec2 requested = default)
        {
            if (!hero.Active || slot < 0 || slot >= 3 || hero.Channel != ChannelKind.None) return false;
            if (slot == 2 && hero.FlightSlot >= 0)
            {
                if (requested.SqrLength > .001f) hero.FlightDirection = requested.Normalized;
                return true;
            }
            if (hero.Rank[slot] == 0 || hero.Cooldowns[slot] > Tick || hero.Incapacitated(Tick) || hero.Has(Status.Silence, Tick))
                return false;
            var ability = hero.Profile.Abilities[slot];
            var enemy = At(hero.Lock);
            var reach = ability.Range;
            if (!Enemy(hero, enemy) || Vec2.Dist2(hero.Position, enemy.Position) > MathF.Max(8, reach + 1) * MathF.Max(8, reach + 1))
                enemy = Target(hero, MathF.Max(8, reach + 1));
            bool automatic = requested.SqrLength < .001f;
            var aim = !automatic ? requested.Normalized : enemy != null ? (enemy.Position - hero.Position).Normalized : hero.Direction;
            if (aim.SqrLength < .001f) aim = Vec2.Forward;
            hero.Direction = aim;
            float power = 1 + (hero.Rank[slot] - 1) * .18f + (hero.Level - 1) * .045f;
            var raw = ability.Get("damage") * power;
            var duration = ability.Get("duration");
            hero.Cooldowns[slot] = Tick + Clock.Ticks(ability.Cooldown * (1 - hero.CooldownBonus) * (hero.Has(Status.Shard, Tick) ? .75f : 1));
            hero.LastAttack = Tick;
            if (hero.Has(Status.Tree, Tick)) ExitTree(hero);
            hero.Reveal(Tick);
            Emit(slot == 2 ? EventKind.Ultimate : EventKind.Burst, hero.Position, MathF.Min(reach, 5), hero.Id);
            switch (ability.Id)
            {
                case "combo":
                    Area(hero, hero.Position, reach, raw, slot: slot, direction: aim, dot: .38f);
                    Field(hero, hero.Position, FieldTrait.Combo, reach, raw, .19f, .49f, 2, aim, ability.Get("spin") * power);
                    break;
                case "shout":
                    foreach (var ally in Heroes)
                        if (ally.Active && ally.Team == hero.Team && Vec2.Dist2(ally.Position, hero.Position) < reach * reach)
                            ally.Apply(Status.Haste, Tick, duration, .25f);
                    Area(hero, hero.Position, reach, 0, slot: slot, effect: Status.AttackSlow, duration: duration);
                    break;
                case "flag":
                    var point = AimPoint(hero, enemy, aim, automatic, 4);
                    Field(hero, point, FieldTrait.Flag, reach, 0, 0, duration);
                    Emit(EventKind.Telegraph, point, reach, hero.Id);
                    break;
                case "bolt":
                    Fire(hero, aim, reach, raw, DamageKind.Magic, 23, .48f, true, slot, ShotTrait.Mark);
                    break;
                case "blink":
                    Field(hero, hero.Position, FieldTrait.Afterimage, ability.Get("radius"), raw, 1, 1);
                    hero.Position = Map.Safe(hero.Position + aim * reach);
                    break;
                case "mirror":
                    Clone(hero, duration);
                    hero.Apply(Status.Invisible, Tick, 2);
                    break;
                case "smash":
                    Area(hero, hero.Position, reach, raw, slot: slot, effect: Status.Stun, duration: .6f);
                    break;
                case "leap":
                    hero.Position = Map.Safe(hero.Position + aim * reach);
                    Area(hero, hero.Position, ability.Get("radius"), raw, slot: slot, effect: Status.Slow, duration: 2, power: .40f);
                    Emit(EventKind.Burst, hero.Position, ability.Get("radius"), hero.Id);
                    break;
                case "rage":
                    hero.Apply(Status.Rage, Tick, duration);
                    hero.Apply(Status.Exhaust, Tick, duration + 2);
                    break;
                case "star":
                    hero.TripleHits = 0;
                    hero.TripleTarget = -1;
                    hero.TripleUntil = Tick + 40;
                    for (int i = -1; i <= 1; i++)
                        Fire(hero, aim.Rotate(i * .145f), reach, raw, DamageKind.Magic, 21, .5f, false, slot, ShotTrait.Star);
                    break;
                case "strike":
                    var end = Map.Safe(hero.Position + aim * reach);
                    var hit = Line(hero, hero.Position, end, .85f, raw, DamageKind.Physical, slot, false, out int defeated);
                    hero.Position = end;
                    if (hit >= 2 || defeated > 0) hero.Cooldowns[slot] = Tick + 9;
                    break;
                case "flight":
                case "night":
                    hero.FlightSlot = slot;
                    hero.FlightDirection = aim;
                    hero.Apply(Status.Flight, Tick, duration, raw);
                    if (ability.Id == "night") hero.Apply(Status.Invisible, Tick, duration);
                    break;
                case "smoke":
                    var at = AimPoint(hero, enemy, aim, automatic, ability.Get("reach"));
                    Field(hero, at, FieldTrait.Smoke, reach, raw, 0, duration);
                    Emit(EventKind.Telegraph, at, reach, hero.Id);
                    break;
                case "tree":
                    hero.Apply(Status.Tree, Tick, duration);
                    hero.Apply(Status.Invisible, Tick, duration);
                    hero.Apply(Status.Healing, Tick, duration, hero.MaxHp * .08f / duration);
                    break;
                case "quake":
                    Field(hero, hero.Position, FieldTrait.Quake, reach, raw, 0, 1.15f, 3);
                    break;
                case "wing":
                    var destination = Map.Safe(AimPoint(hero, enemy, aim, automatic, reach));
                    Line(hero, hero.Position, destination, .9f, raw, DamageKind.Physical, slot, true, out _);
                    hero.Position = destination;
                    break;
                case "bite":
                    Field(hero, hero.Position, FieldTrait.Bite, reach, raw, .08f, 1, 3, aim);
                    hero.Apply(Status.Root, Tick, 1);
                    break;
                case "trap":
                    int count = 0, oldest = -1;
                    for (int i = 0; i < FieldCount; i++)
                        if (Fields[i].Source == hero.Id && Fields[i].Trait == FieldTrait.Trap && Fields[i].End > Tick)
                        {
                            count++;
                            if (oldest < 0 || Fields[i].End < Fields[oldest].End) oldest = i;
                        }
                    if (count >= 3 && oldest >= 0) { Fields[oldest].End = Tick; Fields[oldest].Next = Tick + 1; }
                    Field(hero, AimPoint(hero, enemy, aim, automatic, ability.Get("reach")), FieldTrait.Trap, reach, raw, 1, 36);
                    break;
                case "vanish":
                    hero.Apply(Status.Invisible, Tick, duration);
                    hero.Apply(Status.Haste, Tick, duration, .20f);
                    break;
                case "charm":
                    var wounded = hero;
                    foreach (var ally in Heroes)
                        if (ally.Active && ally.Team == hero.Team && Vec2.Dist2(ally.Position, hero.Position) < reach * reach &&
                            ally.HealthFraction < wounded.HealthFraction) wounded = ally;
                    Charm(wounded, duration, .35f, power, ability.Get("shield"));
                    if (wounded != hero) Charm(hero, duration, .175f, power, ability.Get("shield") * .5f);
                    break;
                default:
                    throw new InvalidOperationException("Unported hero ability " + ability.Id);
            }
            return true;
        }

        private void Charm(Actor target, float duration, float heal, float power, float shield)
        {
            target.Cleanse();
            target.Apply(Status.Healing, Tick, duration, target.MaxHp * heal * power / duration);
            target.Shield += shield * power;
        }

        private void ExitTree(Actor hero)
        {
            hero.StatusEnd[(int)Status.Tree] = 0;
            hero.Reveal(Tick);
            Area(hero, hero.Position, 3.5f, 0, slot: 1, effect: Status.Root, duration: 1);
            Emit(EventKind.Burst, hero.Position, 3.5f, hero.Id);
        }

        private void EndFlight(Actor hero)
        {
            float raw = hero.Strength[(int)Status.Flight];
            hero.FlightSlot = -1;
            hero.Reveal(Tick);
            if (hero.Costume == 3)
            {
                hero.Position = Map.Safe(hero.Position + hero.FlightDirection * 5);
                Area(hero, hero.Position, 4, raw, slot: 2);
            }
            else
            {
                Actor weak = null;
                foreach (var candidate in Heroes)
                    if (Enemy(hero, candidate, false) && Vec2.Dist2(hero.Position, candidate.Position) <= 144 &&
                        (weak == null || candidate.Hp < weak.Hp)) weak = candidate;
                if (weak != null)
                {
                    hero.Position = Map.Safe(weak.Position - hero.FlightDirection * .4f);
                    Hit(weak, hero, raw * (1 + (1 - weak.HealthFraction) * .75f), slot: 2);
                }
                else hero.Position = Map.Safe(hero.Position);
            }
            Emit(EventKind.Burst, hero.Position, 4, hero.Id);
        }

        private void Repeat(ref EffectField patch, int delay)
        {
            if (--patch.Repeats <= 0) patch.End = Tick;
            else patch.Next = Tick + delay;
        }

        private void Pulse(ref EffectField patch)
        {
            var hero = Actors[patch.Source];
            switch (patch.Trait)
            {
                case FieldTrait.Combo:
                    bool spin = patch.Repeats == 1;
                    if (hero.Active)
                    {
                        Area(hero, hero.Position, patch.Radius, spin ? patch.Extra : patch.Raw, slot: 0,
                            direction: patch.Direction, dot: spin ? -1 : .38f, effect: spin ? Status.Air : (Status?)null, duration: .5f);
                        Emit(EventKind.Burst, hero.Position, patch.Radius, hero.Id);
                    }
                    Repeat(ref patch, 6);
                    break;
                case FieldTrait.Flag:
                    foreach (var actor in Heroes)
                    {
                        if (!actor.Active || Vec2.Dist2(actor.Position, patch.At) > patch.Radius * patch.Radius) continue;
                        if (hero.Team == actor.Team)
                        {
                            if (!actor.Has(Status.Flag, Tick)) actor.Cleanse();
                            actor.Apply(Status.Flag, Tick, .54f);
                        }
                        else actor.Apply(Status.Slow, Tick, .54f, .20f);
                    }
                    patch.Next = Tick + 12;
                    break;
                case FieldTrait.Afterimage:
                    Area(hero, patch.At, patch.Radius, patch.Raw, DamageKind.Magic, 1, effect: Status.Slow, duration: 2, power: .30f);
                    Emit(EventKind.Burst, patch.At, patch.Radius, hero.Id);
                    patch.End = Tick;
                    break;
                case FieldTrait.Smoke:
                    Area(hero, patch.At, patch.Radius, patch.Raw * .5f, DamageKind.Magic, 0, effect: Status.Slow, duration: .63f, power: .35f);
                    patch.Next = Tick + 15;
                    break;
                case FieldTrait.Quake:
                    float ring = patch.Radius * (4 - patch.Repeats) / 3;
                    Area(hero, patch.At, ring, patch.Raw, slot: 2, effect: Status.Air, duration: .4f);
                    Emit(EventKind.Burst, patch.At, ring, hero.Id);
                    Repeat(ref patch, 14);
                    break;
                case FieldTrait.Bite:
                    if (hero.Active && !hero.Incapacitated(Tick))
                    {
                        var before = hero.DamageDealt;
                        Area(hero, hero.Position, patch.Radius, patch.Raw, slot: 1, direction: patch.Direction, dot: .38f);
                        hero.Heal((hero.DamageDealt - before) * .60f);
                        Emit(EventKind.Burst, hero.Position, patch.Radius, hero.Id);
                    }
                    Repeat(ref patch, 9);
                    break;
                case FieldTrait.Trap:
                    foreach (var foe in Heroes)
                        if (Enemy(hero, foe, false) && Vec2.Dist2(foe.Position, patch.At) <= (patch.Radius + foe.Radius) * (patch.Radius + foe.Radius))
                        {
                            Hit(foe, hero, patch.Raw, DamageKind.Magic, 0);
                            StatusOn(foe, Status.Root, 1.5f);
                            foe.Reveal(Tick);
                            Emit(EventKind.Burst, patch.At, patch.Radius, hero.Id);
                            patch.End = Tick;
                            return;
                        }
                    patch.Next = Tick + 5;
                    break;
            }
        }
    }
}
