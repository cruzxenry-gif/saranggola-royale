using System;

namespace Saranggola.Sim
{
    public enum PlayMode { Duos, Solo, Practice, Tutorial }
    public enum Difficulty { Easy, Normal, Hard }

    public sealed partial class Match
    {
        public const int HeroLimit = 12;
        public readonly Catalog Data;
        public readonly BattleMap Map;
        public readonly PlayMode Mode;
        public readonly Difficulty Difficulty;
        public readonly int Seed;
        public readonly bool AllBots;
        public readonly Actor[] Heroes;
        public readonly Actor[] Actors = new Actor[96];
        public readonly int[] TeamPlacement;
        public readonly ArenaEvent[] Events = new ArenaEvent[192];
        public readonly int[] LastTeamPingTick;
        public readonly Vec2[] TeamPing;
        public readonly int[] TeamPingKind;
        public readonly int[] FightTicks = new int[1024];
        public int FightCount { get; private set; }
        public int ActorCount { get; private set; }
        public int TeamCount => TeamPlacement.Length;
        public int EventCount { get; private set; }
        public int Tick { get; private set; }
        public float Time => Tick * Clock.Step;
        public Actor Player => Heroes[0];
        public bool Finished { get; private set; }
        public int Winner { get; private set; } = -1;
        public int Eliminations { get; private set; }
        public int StormEliminations { get; private set; }
        public int ReviveAttempts { get; private set; }
        public int Revives { get; private set; }
        public int BeaconReturns { get; private set; }
        public SeededRandom Rng;
        private int lastFight = -10000;
        public bool BotsEnabled = true;

        public Match(Catalog data, int seed, PlayMode mode = PlayMode.Duos, Difficulty difficulty = Difficulty.Normal,
                     int selected = 0, bool allBots = false, BattleMap map = null)
        {
            Data = data;
            Seed = seed;
            Mode = mode;
            Difficulty = difficulty;
            AllBots = allBots;
            Rng = new SeededRandom(seed);
            Map = map ?? new BattleMap(data);
            var size = mode == PlayMode.Practice ? 4 : 12;
            var duo = UsesDuos;
            TeamPlacement = new int[duo ? size / 2 : size];
            LastTeamPingTick = new int[TeamCount];
            Array.Fill(LastTeamPingTick, -10000);
            TeamPing = new Vec2[TeamCount];
            TeamPingKind = new int[TeamCount];
            Heroes = new Actor[size];
            Span<int> picks = stackalloc int[HeroLimit];
            picks[0] = selected % data.Heroes.Length;
            for (int i = 1; i < size; i++) picks[i] = (selected + i) % data.Heroes.Length;
            for (int i = size - 1; i > (allBots ? 0 : 1); i--)
            {
                int j = (allBots ? 0 : 1);
                j += Rng.Index(i - j + 1);
                (picks[i], picks[j]) = (picks[j], picks[i]);
            }
            for (int i = 0; i < size; i++)
            {
                var actor = AddActor();
                actor.AssignHero(data.Heroes[picks[i]], picks[i], duo ? i / 2 : i, data.Economy.StartGold);
                actor.Human = i == 0 && !allBots;
                actor.Life = Life.Active;
                actor.Previous = actor.Position = Map.Safe(Vec2.Right.Rotate(i * Clock.Tau / size) * 36);
                Heroes[i] = actor;
            }
            SetupMirrors();
            InitMode();
            InitLoot();
            InitMobs();
            InitBots();
        }

        public bool UsesDuos => Mode == PlayMode.Duos || Mode == PlayMode.Tutorial;

        public Actor AddActor()
        {
            if (ActorCount >= Actors.Length) throw new InvalidOperationException("Actor pool exhausted.");
            var actor = new Actor(ActorCount);
            Actors[ActorCount++] = actor;
            return actor;
        }

        public Actor At(int id) => (uint)id < (uint)ActorCount ? Actors[id] : null;
        public Actor Partner(Actor hero) => UsesDuos && hero.Kind == UnitKind.Hero ? Heroes[hero.Id ^ 1] : null;

        public void Step(SkillInput input)
        {
            if (Finished) return;
            ++Tick;
            AdvanceMode();
            if (BotsEnabled) AdvanceBots();
            if (Player.Human) ApplyInput(Player, input);
            for (int i = 0; i < Heroes.Length; i++) AdvanceHero(Heroes[i]);
            AdvanceLoot();
            AdvanceMobs();
            UpdateShots();
            UpdateEffects();
            ResolveStandings();
        }

        public void ApplyInput(Actor hero, SkillInput input)
        {
            hero.Motion = input.Move.Limit();
            if (input.Lock != -2) hero.Lock = input.Lock;
            if (input.Rank >= 0) hero.Improve(input.Rank);
            InputMode(hero, input);
            InputLoot(hero, input);
            if (hero.Channel != ChannelKind.None) return;
            if (input.Attack) Basic(hero);
            if (input.Ability >= 0) Cast(hero, input.Ability, input.Aim);
        }

        private void AdvanceHero(Actor hero)
        {
            hero.Previous = hero.Position;
            if (!hero.Active)
            {
                MoveModeHero(hero);
                return;
            }
            if (hero.FlightSlot >= 0 && !hero.Has(Status.Flight, Tick)) EndFlight(hero);
            if (hero.Costume == 4 && hero.StatusEnd[(int)Status.Tree] == Tick) ExitTree(hero);
            if (hero.Costume == 5 && hero.HealthFraction < .30f && Tick >= hero.PassiveReady)
            {
                hero.Apply(Status.Detached, Tick, 4);
                hero.PassiveReady = Tick + Clock.Ticks(45);
                Emit(EventKind.Burst, hero.Position, 2.2f, hero.Id);
            }
            var motion = hero.Motion;
            if (hero.Incapacitated(Tick) || hero.Has(Status.Root, Tick) || hero.Has(Status.Tree, Tick) || hero.Channel != ChannelKind.None)
                motion = Vec2.Zero;
            if (motion.SqrLength > .01f)
            {
                hero.Direction = motion.Normalized;
                hero.Stationary = 0;
            }
            else if (hero.Costume == 4 && ++hero.Stationary >= 45 && Tick % 6 == hero.Id % 6 && Map.InBush(hero.Position, 2.2f))
            {
                hero.Apply(Status.Invisible, Tick, .28f);
                hero.Heal(hero.MaxHp * (BattleMap.ZoneAt(hero.Position) == Zone.Forest ? .006f : .004f));
            }
            bool flying = hero.Has(Status.Flight, Tick);
            if (motion.SqrLength > .001f)
            {
                var terrainSpeed = flying ? 1 : Map.GroundSpeed(hero.Position, hero.Has(Status.Water, Tick));
                hero.Position = Map.Move(hero.Position, motion * (hero.WalkSpeed(Tick) * terrainSpeed * Clock.Step), flying);
            }
            hero.LavaTicks = !flying && Map.InLava(hero.Position) ? hero.LavaTicks + 1 : 0;
            if (Tick % 15 == hero.Id % 15)
            {
                if (hero.LavaTicks >= 15) Hit(hero, null, hero.MaxHp * .024f, DamageKind.Hazard);
                Dots(hero);
                if (hero.Has(Status.Water, Tick)) hero.Heal(hero.MaxHp * .006f);
            }
            if (Tick % Clock.Hz == hero.Id)
            {
                GainGold(hero, Data.Economy.PassiveGold, GoldFrom.Passive);
                if (hero.LastCombat < Tick - 10 * Clock.Hz)
                    hero.Heal(hero.MaxHp * Data.Economy.PassiveRegen);
            }
            TickInteraction(hero);
        }

        public void Emit(EventKind kind, Vec2 p, float amount = 0, int source = -1, int target = -1)
        {
            if (AllBots) return;
            Events[EventCount++ % Events.Length] = new ArenaEvent(kind, Tick, p, amount, source, target);
        }

        public static void GainGold(Actor actor, float value, GoldFrom from)
        {
            actor.Gold += value;
            actor.Earnings[(int)from] += value;
        }

        public float NeedXp(Actor actor) => Data.Mode.XpCurve[Math.Min(actor.Level - 1, Data.Mode.XpCurve.Length - 1)];
        public void AwardXp(Actor actor, float amount)
        {
            if (actor.Kind != UnitKind.Hero || actor.Level >= Data.Economy.MaxLevel) return;
            actor.Xp += amount;
            while (actor.Level < Data.Economy.MaxLevel && actor.Xp >= NeedXp(actor))
            {
                actor.Xp -= NeedXp(actor);
                actor.LevelAt[++actor.Level] = Tick;
                actor.SkillPoints++;
                actor.MaxHp += actor.Profile.Stats.Hp * .08f;
                actor.Heal(actor.Profile.Stats.Hp * .08f);
                actor.Atk += actor.Profile.Stats.Damage * .06f;
                actor.Armor += 2;
                actor.Resist += 1.5f;
                Emit(EventKind.Burst, actor.Position, 2, actor.Id);
            }
            if (actor.Level == Data.Economy.MaxLevel) actor.Xp = 0;
        }

        private void Defeat(Actor hero, Actor source, DamageKind kind)
        {
            bool handled = false;
            DefeatMode(hero, source, kind, ref handled);
            if (!handled) Eliminate(hero, source, kind);
        }

        public void Eliminate(Actor actor, Actor source, DamageKind kind)
        {
            if (actor.Life == Life.Eliminated) return;
            actor.Life = Life.Eliminated;
            actor.Hp = 0;
            actor.DeathTick = Tick;
            actor.Channel = ChannelKind.None;
            Emit(EventKind.Death, actor.Position, 2, source?.Id ?? -1, actor.Id);
            if (actor.Kind == UnitKind.Mob)
            {
                MobDefeated(actor, source);
                return;
            }
            if (actor.Kind != UnitKind.Hero) return;
            ++Eliminations;
            if (kind == DamageKind.Storm) ++StormEliminations;
            actor.Deaths++;
            HeroEliminated(actor, source, kind);
            if (source != null && source.Kind == UnitKind.Hero && source.Team != actor.Team)
            {
                float bounty = actor.Streak >= 3 ? 50 * actor.Streak : 0;
                source.Kills++;
                source.Streak++;
                GainGold(source, Data.Economy.KillGold, GoldFrom.Kill);
                if (bounty > 0) GainGold(source, bounty, GoldFrom.Bounty);
                var stolen = MathF.Floor(actor.Gold * .5f);
                actor.Gold -= stolen;
                GainGold(source, stolen, GoldFrom.Stolen);
                AwardXp(source, (Data.Economy.KillXp + Data.Economy.KillLevelXp * actor.Level) *
                    (actor.Level - source.Level >= 2 ? 1.25f : 1));
                var partner = Partner(source);
                if (partner != null && actor.Aggressors[partner.Id] > Tick - Clock.Ticks(Data.Economy.AssistWindow))
                {
                    partner.Assists++;
                    GainGold(partner, Data.Economy.AssistGold, GoldFrom.Assist);
                    AwardXp(partner, 70);
                }
                if (source.Streak == 3) Emit(EventKind.Bounty, source.Position, source.Streak, source.Id);
            }
            actor.Streak = 0;
        }

        private void LogFight(Actor source, Actor target)
        {
            if (source == null || source.Kind != UnitKind.Hero || target.Kind != UnitKind.Hero) return;
            if (Tick - lastFight > 6 * Clock.Hz && FightCount < FightTicks.Length) FightTicks[FightCount++] = Tick;
            lastFight = Tick;
        }

        public ulong Hash()
        {
            ulong h = 14695981039346656037UL;
            Mix(ref h, Tick); Mix(ref h, Winner); Mix(ref h, unchecked((int)Rng.State));
            Mix(ref h, Bagyo.Phase); Mix(ref h, BitConverter.SingleToInt32Bits(Bagyo.Radius));
            Mix(ref h, BitConverter.SingleToInt32Bits(Bagyo.Center.X)); Mix(ref h, BitConverter.SingleToInt32Bits(Bagyo.Center.Z));
            Mix(ref h, ShotCount); Mix(ref h, FieldCount);
            for (int i = 0; i < ActorCount; i++)
            {
                var a = Actors[i];
                Mix(ref h, (int)a.Life); Mix(ref h, a.Team); Mix(ref h, a.Costume);
                Mix(ref h, BitConverter.SingleToInt32Bits(a.Position.X)); Mix(ref h, BitConverter.SingleToInt32Bits(a.Position.Z));
                Mix(ref h, BitConverter.SingleToInt32Bits(a.Hp)); Mix(ref h, BitConverter.SingleToInt32Bits(a.Gold));
                Mix(ref h, a.Level); Mix(ref h, a.Kills); Mix(ref h, a.Weapon); Mix(ref h, a.Shield.GetHashCode());
                Mix(ref h, a.ChannelUntil); Mix(ref h, (int)a.Channel); Mix(ref h, a.Durability);
                Mix(ref h, a.Items[0]); Mix(ref h, a.Items[1]);
                for (int j = 0; j < 3; j++) { Mix(ref h, a.Cooldowns[j]); Mix(ref h, a.Rank[j]); }
                foreach (int until in a.StatusEnd) Mix(ref h, until);
            }
            for (int i = 0; i < TeamCount; i++) Mix(ref h, TeamPlacement[i]);
            foreach (var post in Outposts) Mix(ref h, post.Owner);
            for (int i = 0; i < LootCount; i++)
            {
                var loot = Loot[i];
                Mix(ref h, loot.Expires);
                if (!loot.Live(Tick)) continue;
                Mix(ref h, loot.Item); Mix(ref h, loot.Rarity); Mix(ref h, (int)loot.Kind);
                Mix(ref h, BitConverter.SingleToInt32Bits(loot.At.X)); Mix(ref h, BitConverter.SingleToInt32Bits(loot.At.Z));
            }
            return h;
        }

        private static void Mix(ref ulong hash, int value) => hash = unchecked((hash ^ (uint)value) * 1099511628211UL);

        partial void InitMode();
        partial void InitLoot();
        partial void InitMobs();
        partial void InitBots();
        partial void AdvanceMode();
        partial void AdvanceLoot();
        partial void AdvanceMobs();
        partial void AdvanceBots();
        partial void InputMode(Actor hero, SkillInput input);
        partial void InputLoot(Actor hero, SkillInput input);
        partial void MoveModeHero(Actor hero);
        partial void TickInteraction(Actor hero);
        partial void DefeatMode(Actor hero, Actor source, DamageKind cause, ref bool handled);
        partial void HeroEliminated(Actor hero, Actor source, DamageKind kind);
        partial void MobDefeated(Actor mob, Actor source);
        partial void ResolveStandings();
    }
}
