using System;

namespace Saranggola.Sim
{
    public enum ShotTrait { None, Mark, Star, Poison, Slipper, Pot, Returning }
    public enum FieldTrait { Combo, Flag, Afterimage, Smoke, Quake, Bite, Trap }

    public struct Projectile
    {
        public int Source, Focus, End, Slot;
        public Vec2 Position, Previous, Velocity;
        public float Raw, Radius;
        public DamageKind Kind;
        public ShotTrait Trait;
        public bool Pierces;
        public ulong Hits, HitsHigh;
    }

    public struct EffectField
    {
        public int Source, Next, End, Repeats;
        public Vec2 At, Direction;
        public float Radius, Raw, Extra;
        public FieldTrait Trait;
    }

    public sealed partial class Match
    {
        private const uint NoTarget = uint.MaxValue;
        public readonly Projectile[] Shots = new Projectile[112];
        public readonly EffectField[] Fields = new EffectField[64];
        private Actor[] mirrors;
        public int ShotCount { get; private set; }
        public int FieldCount { get; private set; }

        public static float Mitigate(float raw, float defense) => raw * 100f / (100f + MathF.Max(-90f, defense));

        public bool Visible(Actor viewer, Actor target)
        {
            if (viewer.Team == target.Team || target.Kind == UnitKind.Mob) return true;
            if (Vec2.Dist2(viewer.Position, target.Position) < 9) return true;
            if (target.Has(Status.Invisible, Tick)) return false;
            return viewer.Has(Status.Sight, Tick) || target.LastReveal > Tick - 28 || !Map.InBush(target.Position);
        }

        public bool Enemy(Actor source, Actor target, bool respectFog = true)
        {
            if (target == null || source == target || !target.Active || source.Team == target.Team || target.GraceEnds > Tick)
                return false;
            if (source.Kind == UnitKind.Mob && target.Kind == UnitKind.Mirror) return false;
            return !respectFog || Visible(source, target);
        }

        public Actor Target(Actor source, float reach, UnitKind? kind = null, bool fog = true, bool requireSight = false)
        {
            float best = reach * reach;
            Actor chosen = null;
            int count = kind == UnitKind.Hero ? Heroes.Length : ActorCount;
            for (int i = 0; i < count; i++)
            {
                var target = Actors[i];
                if (!target.Active || target.Team == source.Team || (kind.HasValue && target.Kind != kind.Value)) continue;
                var gap = Vec2.Dist2(target.Position, source.Position);
                if (gap < best && Enemy(source, target, fog) && (!requireSight || Map.Sight(source.Position, target.Position)))
                {
                    best = gap;
                    chosen = target;
                }
            }
            return chosen;
        }

        public float Hit(Actor target, Actor source, float raw, DamageKind kind = DamageKind.Physical, int slot = -1)
        {
            var pure = kind == DamageKind.Storm || kind == DamageKind.Hazard;
            if (target == null || !target.Active || (!pure && target.GraceEnds > Tick) || raw <= 0) return 0;
            var dealer = source?.Kind == UnitKind.Mirror ? At(source.Owner) : source;
            var amount = pure ? raw : Mitigate(raw, kind == DamageKind.Magic ? target.Resist : target.Armor);
            if (!pure)
            {
                if (target.Has(Status.Mark, Tick)) { amount *= 1.20f; target.StatusEnd[(int)Status.Mark] = 0; }
                if (target.Kind == UnitKind.Hero)
                {
                    if (target.Has(Status.Rage, Tick)) amount *= .70f;
                    var ally = Partner(target);
                    if (ally != null && ally.Active && (ally.Costume == 0 || target.Costume == 0) && Vec2.Dist2(ally.Position, target.Position) < 36)
                        amount *= .9f;
                }
                var soak = MathF.Min(target.Shield, amount);
                target.Shield -= soak;
                amount -= soak;
                target.LastCombat = Tick;
                if (dealer != null) dealer.LastCombat = Tick;
            }
            target.LastDamage = Tick;
            target.Attacker = dealer?.Id ?? -1;
            ++target.DamageVersion;
            if (dealer != null && dealer.Kind == UnitKind.Hero && target.Kind == UnitKind.Hero)
                target.Aggressors[dealer.Id] = Tick;
            if (target.Kind == UnitKind.Mob && target.CampStart < 0) target.CampStart = Tick;
            if (amount <= 0) return 0;
            float dealt = MathF.Min(target.Hp, amount);
            target.Hp -= dealt;
            target.DamageTaken += dealt;
            if (target.Kind == UnitKind.Hero)
                target.Recap[target.RecapHead++ % target.Recap.Length] = new DamageSample(Tick, dealer?.Id ?? -1, dealt, kind);
            if (dealer != null && dealer.Kind == UnitKind.Hero)
            {
                dealer.DamageDealt += dealt;
                if (!pure) dealer.Heal(dealt * dealer.Lifesteal(Tick));
                if (dealer.Costume == 3 && slot >= 0)
                    for (int i = 0; i < 3; i++) if (i != slot) dealer.Cooldowns[i] = Math.Max(Tick, dealer.Cooldowns[i] - 24);
            }
            if (target.Kind == UnitKind.Hero && target.Costume == 2 && !pure)
                target.Rage = MathF.Min(100, target.Rage + dealt * .17f);
            if (!pure) Emit(EventKind.Damage, target.Position, dealt, dealer?.Id ?? -1, target.Id);
            LogFight(dealer, target);
            if (target.Hp <= 0)
            {
                if (target.Kind == UnitKind.Mirror)
                {
                    target.Life = Life.Eliminated;
                    var hero = At(target.Owner);
                    if (hero != null) Area(hero, target.Position, 2.7f, hero.Profile.Abilities[2].Get("damage"), DamageKind.Magic);
                    Emit(EventKind.Burst, target.Position, 2.7f, target.Id);
                }
                else Defeat(target, dealer, kind);
            }
            return dealt;
        }

        public void StatusOn(Actor target, Status effect, float duration, float power = 0, Actor source = null)
        {
            if (!target.Active || target.Kind == UnitKind.Mirror) return;
            target.Apply(effect, Tick, duration, power);
            if (effect == Status.Burn || effect == Status.Poison) target.DotSource = source?.Id ?? -1;
        }

        public void Dots(Actor actor)
        {
            if (!actor.Active) return;
            var source = At(actor.DotSource);
            if (actor.Has(Status.Burn, Tick)) Hit(actor, source, actor.Strength[(int)Status.Burn] * .5f, DamageKind.Magic);
            if (actor.Has(Status.Poison, Tick))
                Hit(actor, source, actor.Strength[(int)Status.Poison] * .5f * actor.PoisonStacks, DamageKind.Magic);
            else actor.PoisonStacks = 0;
            if (actor.Has(Status.Healing, Tick)) actor.Heal(actor.Strength[(int)Status.Healing] * .5f);
        }

        public float AttackReach(Actor hero)
        {
            float reach = hero.Range;
            WeaponReach(hero, ref reach);
            return reach;
        }

        public bool Basic(Actor hero)
        {
            if (!hero.Active || hero.Channel != ChannelKind.None || hero.AttackEnd > Tick || hero.Incapacitated(Tick)) return false;
            var reach = AttackReach(hero);
            var target = At(hero.Lock);
            if (!Enemy(hero, target) || Vec2.Dist2(target.Position, hero.Position) > (reach + target.Radius) * (reach + target.Radius) ||
                !Map.Sight(hero.Position, target.Position))
                target = Target(hero, reach + .6f, requireSight: true);
            if (target == null) return false;
            if (hero.Has(Status.Tree, Tick)) ExitTree(hero);
            hero.Reveal(Tick);
            hero.LastAttack = Tick;
            hero.LastCombat = Tick;
            hero.Direction = (target.Position - hero.Position).Normalized;
            if (hero.Direction.SqrLength < .0001f) hero.Direction = Vec2.Forward;
            float rate = hero.Rate(Tick), raw = hero.Power(Tick);
            WeaponRate(hero, ref rate);
            hero.AttackEnd = Tick + Math.Max(1, Clock.Ticks(1 / rate));
            hero.AttackCount++;
            if (hero.Costume == 2 && hero.Rage >= 100)
            {
                hero.Rage = 0;
                Area(hero, hero.Position, 3.6f, raw * 1.95f, DamageKind.Physical, effect: Status.Stun, duration: .4f);
                Emit(EventKind.Burst, hero.Position, 3.6f, hero.Id);
            }
            bool armed = false;
            AttackWeapon(hero, target, reach, raw, ref armed);
            var kind = hero.Magic ? DamageKind.Magic : DamageKind.Physical;
            if (hero.Has(Status.Rage, Tick))
                Area(hero, hero.Position, armed ? 2.7f : reach + .5f, armed ? raw * .42f : raw, kind, direction: hero.Direction, dot: .1f);
            else if (!armed && reach > 4)
                Fire(hero, hero.Direction, reach + 1.2f, raw, kind, 22, .32f, focus: target.Id);
            else if (!armed)
                Hit(target, hero, raw, kind);
            if (hero.Costume == 1 && hero.AttackCount % 4 == 0)
            {
                int chains = 0;
                for (int i = 0; i < ActorCount && chains < 2; i++)
                {
                    var next = Actors[i];
                    if (next != target && Enemy(hero, next) && Vec2.Dist2(target.Position, next.Position) < 25)
                    {
                        Hit(next, hero, raw * .85f, DamageKind.Magic);
                        Emit(EventKind.Burst, next.Position, .95f, hero.Id);
                        chains++;
                    }
                }
            }
            if (hero.Has(Status.Fire, Tick)) StatusOn(target, Status.Burn, 3, 20, hero);
            if (hero.Costume == 6 && Rng.Unit() < .12f) LuckyCoin(target.Position);
            for (int i = 0; i < 2; i++)
            {
                var mirror = mirrors[hero.Id * 2 + i];
                if (!mirror.Active) continue;
                var enemy = Target(mirror, 8);
                if (enemy != null) Fire(mirror, (enemy.Position - mirror.Position).Normalized, 8, raw * .4f, DamageKind.Magic, 18, .33f, focus: enemy.Id);
            }
            return true;
        }

        public int Area(Actor source, Vec2 at, float radius, float raw, DamageKind kind = DamageKind.Physical, int slot = -1,
                        Vec2 direction = default, float dot = -1, Status? effect = null, float duration = 0, float power = 0)
        {
            int count = 0;
            for (int i = 0; i < ActorCount; i++)
            {
                var actor = Actors[i];
                if (!Enemy(source, actor, false)) continue;
                var delta = actor.Position - at;
                var reach = radius + actor.Radius;
                if (delta.SqrLength > reach * reach || dot > -.99f && delta.SqrLength > .001f && Vec2.Dot(delta.Normalized, direction) < dot) continue;
                if (raw > 0) Hit(actor, source, raw, kind, slot);
                if (effect.HasValue) StatusOn(actor, effect.Value, duration, power, source);
                count++;
            }
            return count;
        }

        public int Line(Actor source, Vec2 start, Vec2 end, float width, float raw, DamageKind kind, int slot, bool mark, out int defeated)
        {
            int count = 0;
            defeated = 0;
            for (int i = 0; i < ActorCount; i++)
            {
                var actor = Actors[i];
                if (!Enemy(source, actor, false)) continue;
                var hit = Vec2.ClosestOnSegment(actor.Position, start, end);
                var widthAt = width + actor.Radius;
                if (Vec2.Dist2(actor.Position, hit) > widthAt * widthAt || !Map.Sight(start, hit)) continue;
                Hit(actor, source, raw, kind, slot);
                if (mark) StatusOn(actor, Status.Mark, 4);
                if (!actor.Active) defeated++;
                count++;
            }
            return count;
        }

        public void Fire(Actor source, Vec2 direction, float reach, float raw, DamageKind kind, float speed, float radius,
                         bool pierce = false, int slot = -1, ShotTrait trait = ShotTrait.None, int focus = -1)
        {
            if (ShotCount >= Shots.Length) return;
            var at = source.Position + direction * .34f;
            Shots[ShotCount++] = new Projectile {
                Source = source.Id, Focus = focus, Position = at, Previous = at, Velocity = direction * speed,
                Raw = raw, Kind = kind, Radius = radius, Pierces = pierce, Slot = slot, Trait = trait,
                End = Tick + Math.Max(1, Clock.Ticks(reach / speed))
            };
        }

        private void UpdateShots()
        {
            for (int i = ShotCount - 1; i >= 0; i--)
            {
                ref var shot = ref Shots[i];
                var source = Actors[shot.Source];
                shot.Previous = shot.Position;
                var focus = shot.Trait == ShotTrait.Returning ? source : At(shot.Focus);
                if (focus != null && focus.Active) shot.Velocity = (focus.Position - shot.Position).Normalized * shot.Velocity.Length;
                var end = shot.Position + shot.Velocity * Clock.Step;
                if (Tick >= shot.End || !Map.Clear(end, .03f) && shot.Trait != ShotTrait.Returning)
                {
                    if (shot.Trait == ShotTrait.Pot) BlastPot(source, shot.Position, shot.Raw);
                    RemoveShot(i);
                    continue;
                }
                if (shot.Trait == ShotTrait.Returning)
                {
                    if (Vec2.Dist2(source.Position, end) < .65f) RemoveShot(i);
                    else shot.Position = end;
                    continue;
                }
                float best = float.PositiveInfinity;
                Actor target = null;
                for (int j = 0; j < ActorCount; j++)
                {
                    var actor = Actors[j];
                    var bit = 1UL << (j & 63);
                    if (!actor.Active || actor.Team == source.Team || (bit & (j < 64 ? shot.Hits : shot.HitsHigh)) != 0) continue;
                    var closest = Vec2.ClosestOnSegment(actor.Position, shot.Position, end);
                    float range = actor.Radius + shot.Radius;
                    if (Vec2.Dist2(actor.Position, closest) > range * range || !Enemy(source, actor, false)) continue;
                    var distance = Vec2.Dist2(shot.Position, closest);
                    if (distance < best) { best = distance; target = actor; }
                }
                shot.Position = end;
                if (target == null) continue;
                if (shot.Trait == ShotTrait.Pot) BlastPot(source, target.Position, shot.Raw);
                else Hit(target, source, shot.Raw, shot.Kind, shot.Slot);
                switch (shot.Trait)
                {
                    case ShotTrait.Mark:
                        StatusOn(target, Status.Mark, 4); break;
                    case ShotTrait.Star:
                        if (source.TripleUntil > Tick && source.TripleTarget == target.Id)
                        {
                            if (++source.TripleHits == 3) Hit(target, source, shot.Raw * .9f, DamageKind.Magic, shot.Slot);
                        }
                        else { source.TripleTarget = target.Id; source.TripleHits = 1; }
                        break;
                    case ShotTrait.Poison:
                        target.PoisonStacks = Math.Min(3, target.PoisonStacks + 1);
                        StatusOn(target, Status.Poison, 4.5f, 12, source);
                        break;
                    case ShotTrait.Slipper:
                        StatusOn(target, Status.Stun, 1);
                        Emit(EventKind.Hoy, target.Position, 0, source.Id, target.Id);
                        shot.Trait = ShotTrait.Returning;
                        shot.Focus = source.Id;
                        shot.End = Tick + Clock.Hz;
                        continue;
                }
                if (!shot.Pierces) RemoveShot(i);
                else if (target.Id < 64) shot.Hits |= 1UL << target.Id;
                else shot.HitsHigh |= 1UL << (target.Id & 63);
            }
        }

        private void BlastPot(Actor source, Vec2 at, float raw)
        {
            Area(source, at, 3, raw, effect: Status.Slow, duration: 1.8f, power: .38f);
            Emit(EventKind.Burst, at, 3, source.Id);
        }

        private void RemoveShot(int i) { --ShotCount; Shots[i] = Shots[ShotCount]; }

        private void Field(Actor hero, Vec2 at, FieldTrait trait, float radius, float raw, float delay, float duration,
                           int repeats = 0, Vec2 direction = default, float extra = 0)
        {
            if (FieldCount == Fields.Length) return;
            Fields[FieldCount++] = new EffectField { Source = hero.Id, At = at, Direction = direction, Radius = radius,
                Raw = raw, Trait = trait, Repeats = repeats, Next = Tick + Clock.Ticks(delay), End = Tick + Clock.Ticks(duration), Extra = extra };
        }

        private void UpdateEffects()
        {
            for (int i = FieldCount - 1; i >= 0; i--)
            {
                ref var field = ref Fields[i];
                if (field.Next <= Tick && field.End >= Tick) Pulse(ref field);
                if (field.End <= Tick) { --FieldCount; Fields[i] = Fields[FieldCount]; }
            }
            foreach (var mirror in mirrors)
                if (mirror.Active && mirror.MirrorUntil <= Tick) mirror.Life = Life.Eliminated;
        }

        private void SetupMirrors()
        {
            mirrors = new Actor[Heroes.Length * 2];
            for (int i = 0; i < mirrors.Length; i++)
            {
                var actor = AddActor();
                actor.Kind = UnitKind.Mirror;
                actor.Life = Life.Eliminated;
                actor.Owner = i / 2;
                mirrors[i] = actor;
            }
        }

        private void Clone(Actor hero, float duration)
        {
            for (int i = 0; i < 2; i++)
            {
                var mirror = mirrors[hero.Id * 2 + i];
                mirror.Profile = hero.Profile;
                mirror.Team = hero.Team;
                mirror.Costume = hero.Costume;
                mirror.Hp = mirror.MaxHp = hero.MaxHp * .24f;
                mirror.Radius = .48f;
                mirror.Life = Life.Active;
                mirror.Direction = hero.Direction;
                mirror.Previous = mirror.Position = Map.Safe(hero.Position + hero.Direction.Perpendicular * (i * 4 - 2));
                mirror.MirrorUntil = Tick + Clock.Ticks(duration);
            }
        }

        partial void WeaponReach(Actor hero, ref float range);
        partial void WeaponRate(Actor hero, ref float rate);
        partial void AttackWeapon(Actor hero, Actor target, float reach, float damage, ref bool armed);
        partial void LuckyCoin(Vec2 point);
    }
}
