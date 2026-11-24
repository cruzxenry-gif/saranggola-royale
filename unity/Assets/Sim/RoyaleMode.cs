using System;

namespace Saranggola.Sim
{
    public sealed class Storm
    {
        private readonly ModeSpec spec;
        private readonly Vec2 eye;
        public int Phase { get; private set; }
        public Vec2 Center { get; private set; }
        public Vec2 NextCenter { get; private set; }
        public float Radius { get; private set; }
        public float NextRadius { get; private set; }
        public float Rate { get; private set; }
        public float Seconds { get; private set; }
        public bool Closing { get; private set; }
        public bool Sudden { get; private set; }

        public Storm(ModeSpec settings, Vec2 center)
        {
            spec = settings;
            eye = center;
            Radius = settings.InitialRadius;
            NextRadius = Radius * settings.Radius[0];
            Seconds = settings.Start[0];
        }

        public void Sample(int tick)
        {
            float time = tick * Clock.Step;
            while (Phase < spec.Start.Length && time >= spec.Start[Phase]) Phase++;
            if (Phase == 0)
            {
                Seconds = spec.Start[0] - time;
                return;
            }
            int p = Phase - 1;
            float elapsed = time - spec.Start[p];
            float t = Clock.Clamp01(elapsed / spec.Shrink);
            var before = eye * (p == 0 ? 0 : (p - 1) / 3f);
            NextCenter = eye * (p / 3f);
            Center = Vec2.Lerp(before, NextCenter, t);
            var previousRadius = spec.InitialRadius * (p == 0 ? 1 : spec.Radius[p - 1]);
            NextRadius = spec.InitialRadius * spec.Radius[p];
            Radius = previousRadius + (NextRadius - previousRadius) * t;
            Closing = elapsed < spec.Shrink;
            Rate = spec.Damage[p];
            Seconds = Closing ? spec.Shrink - elapsed : (Phase < spec.Start.Length ? spec.Start[Phase] : spec.Sudden) - time;
            Sudden = time >= spec.Sudden;
            if (Sudden)
            {
                float fade = Clock.Clamp01((time - spec.Sudden) / 15);
                Radius += (spec.SuddenRadius - Radius) * fade;
                NextRadius = spec.SuddenRadius;
                Rate = spec.SuddenDamage;
                Closing = fade < 1;
                Seconds = MathF.Max(0, spec.End - time);
            }
        }

        public bool Outside(Vec2 p) => Phase > 0 && Vec2.Dist2(p, Center) > Radius * Radius;
    }

    public struct Soul
    {
        public Vec2 Position;
        public int Holder, Expires;
        public bool Active(int tick) => Expires > tick;
    }

    public sealed class Outpost
    {
        public readonly Vec2 Position;
        public int Owner = -1, Claim = -1, Since;
        public Outpost(Vec2 p) { Position = p; }
    }

    public sealed partial class Match
    {
        private int NearSupply(Actor hero)
        {
            int found=-1;
            FindSupply(hero,ref found);
            return found;
        }
        private bool IsSupplyNear(Actor hero,int index)
        {
            bool near=false;
            CheckSupply(hero,index,ref near);
            return near;
        }
        partial void FindSupply(Actor hero,ref int found);
        partial void CheckSupply(Actor hero,int index,ref bool near);
        public Storm Bagyo { get; private set; }
        public Vec2 KiteStart { get; private set; }
        public Vec2 KiteEnd { get; private set; }
        public Vec2 KiteDirection { get; private set; }
        public Vec2 KitePosition { get; private set; }
        public bool KiteFlying => Time < Data.Mode.DropSeconds && Mode != PlayMode.Practice;
        public Soul[] Souls { get; private set; }
        public Outpost[] Outposts { get; private set; }
        public bool BeaconsOnline => UsesDuos && Bagyo.Phase < 3;
        public string EndReason { get; private set; }
        private int lastPhase;

        partial void InitMode()
        {
            var final = Map.Landmarks[Rng.Index(4) * 2 + 1];
            Bagyo = new Storm(Data.Mode, final + Vec2.Right.Rotate(Rng.Unit() * Clock.Tau) * Rng.Range(0, 1.8f));
            KiteDirection = Vec2.Right.Rotate(Rng.Unit() * Clock.Tau);
            var cross = KiteDirection.Perpendicular * Rng.Range(-24,24);
            KiteStart = KiteEdge(cross, -KiteDirection);
            KiteEnd = KiteEdge(cross, KiteDirection);
            KitePosition = KiteStart;
            Souls = new Soul[Heroes.Length];
            for (int i = 0; i < Souls.Length; i++) Souls[i].Holder = -1;
            Outposts = new Outpost[Map.Outposts.Length];
            for (int i = 0; i < Outposts.Length; i++) Outposts[i] = new Outpost(Map.Outposts[i]);
            if (Mode == PlayMode.Practice)
            {
                for(int i=0;i<Heroes.Length;i++) Heroes[i].Previous=Heroes[i].Position=Map.Safe(new Vec2(-30+i*4,-12+i*3));
            }
            else
                foreach (var hero in Heroes)
                {
                    hero.Life = Life.Riding;
                    hero.Previous = hero.Position = KiteStart;
                    hero.Direction = KiteDirection;
                }
        }

        private static Vec2 KiteEdge(Vec2 from, Vec2 walk)
        {
            float x = MathF.Abs(walk.X) > .001f ? (MathF.Sign(walk.X) * 98 - from.X) / walk.X : float.PositiveInfinity;
            float z = MathF.Abs(walk.Z) > .001f ? (MathF.Sign(walk.Z) * 98 - from.Z) / walk.Z : float.PositiveInfinity;
            return from + walk * MathF.Min(x,z);
        }

        public bool Jump(Actor hero)
        {
            if (hero.Life != Life.Riding) return false;
            hero.Life = Life.Gliding;
            hero.Previous = hero.Position = KitePosition;
            hero.Direction = KiteDirection;
            hero.GlideStarted = Tick;
            hero.GlideUntil = Tick + Clock.Ticks(Data.Mode.GlideSeconds);
            Emit(EventKind.Drop, hero.Position, 0, hero.Id);
            return true;
        }

        public void Dive(Actor hero)
        {
            if (hero.Life == Life.Gliding)
                hero.GlideUntil = Math.Min(hero.GlideUntil, Tick + Clock.Ticks(Data.Mode.DiveSeconds));
        }

        public void Ping(Actor caller, Vec2 position, int kind)
        {
            if (!UsesDuos || !caller.Alive || LastTeamPingTick[caller.Team] > Tick - 18) return;
            TeamPing[caller.Team] = BattleMap.Clamp(position);
            LastTeamPingTick[caller.Team] = Tick;
            TeamPingKind[caller.Team] = kind;
            Emit(EventKind.Ping,position,kind,caller.Id);
        }

        partial void InputMode(Actor hero, SkillInput input)
        {
            if(input.Jump) Jump(hero);
            if(input.Dive) Dive(hero);
            if (!hero.Active) return;
            if (hero.Channel != ChannelKind.None && input.Move.SqrLength > .34f) CancelChannel(hero);
            if (input.Shop >= 0) RequestShop(hero,input.Shop);
            if (input.Interact && input.Move.SqrLength < .30f) Interact(hero);
        }

        partial void MoveModeHero(Actor hero)
        {
            switch (hero.Life)
            {
                case Life.Riding:
                    hero.Position = KitePosition;
                    if (!KiteFlying) Jump(hero);
                    break;
                case Life.Gliding:
                    var nudge=(KiteDirection*.29f+hero.Motion).Limit();
                    hero.Position=Map.Move(hero.Position,nudge*Data.Mode.GlideSpeed*Clock.Step,true);
                    if(nudge.SqrLength>.01f)hero.Direction=nudge.Normalized;
                    if (Tick >= hero.GlideUntil)
                    {
                        hero.Life=Life.Active;
                        hero.Position=Map.Safe(hero.Position);
                        hero.GraceEnds=Tick+16;
                        Emit(EventKind.Burst,hero.Position,2.5f,hero.Id);
                    }
                    break;
                case Life.Downed:
                    if (Tick >= hero.DownUntil || Bagyo.Phase >= 3)
                    {
                        Bleed(hero);
                        break;
                    }
                    hero.Position=Map.Move(hero.Position,hero.Motion.Limit()*Clock.Step*1.33f,false);
                    break;
            }
        }

        partial void DefeatMode(Actor hero, Actor source, DamageKind cause, ref bool handled)
        {
            if (hero.Kind != UnitKind.Hero || !UsesDuos || Bagyo.Phase >= 3) return;
            var partner = Partner(hero);
            if (partner == null || !partner.Alive || partner.Life == Life.Downed) return;
            hero.Life = Life.Downed;
            hero.Hp = 0;
            hero.Shield = 0;
            hero.Channel = ChannelKind.None;
            hero.DownUntil = Tick + Clock.Ticks(Data.Mode.Bleed);
            hero.DownedBy = source?.Id ?? -1;
            hero.DownKind = cause;
            hero.FlightSlot = -1;
            hero.Cleanse();
            handled = true;
            Emit(EventKind.Down,hero.Position,Data.Mode.Bleed,source?.Id??-1,hero.Id);
        }

        private void Bleed(Actor hero)
        {
            var killer = At(hero.DownedBy);
            EndDown(hero,killer,hero.DownKind,true);
        }

        private void EndDown(Actor hero, Actor killer, DamageKind kind, bool orb)
        {
            Eliminate(hero,killer,kind);
            if (!orb || !BeaconsOnline || Partner(hero)?.Active != true) return;
            Souls[hero.Id] = new Soul { Position=hero.Position, Holder=-1, Expires=Tick+Clock.Ticks(Data.Mode.SoulLife) };
        }

        partial void HeroEliminated(Actor hero, Actor source, DamageKind kind)
        {
            DropWeapon(hero);
            if(hero.CarriedSoul>=0)
            {
                var id=hero.CarriedSoul;
                Souls[id].Holder=-1;
                Souls[id].Position=hero.Position;
                hero.CarriedSoul=-1;
            }
        }

        public ChannelKind GetInteraction(Actor hero, out int target)
        {
            target=-1;
            if(!hero.Active)return ChannelKind.None;
            if (BeaconsOnline && hero.CarriedSoul>=0 && Souls[hero.CarriedSoul].Active(Tick))
            {
                int point=Near(hero.Position,Map.Beacons,3.30f);
                if(point>=0){target=point;return ChannelKind.Beacon;}
            }
            if(UsesDuos && Bagyo.Phase<3)
            {
                var partner=Partner(hero);
                if(partner.Life==Life.Downed&&Vec2.Dist2(hero.Position,partner.Position)<2.6f*2.6f)
                {target=partner.Id;return ChannelKind.Revive;}
                foreach(var foe in Heroes)
                    if(foe.Team!=hero.Team&&foe.Life==Life.Downed&&Vec2.Dist2(hero.Position,foe.Position)<2.25f*2.25f)
                    {target=foe.Id;return ChannelKind.Finish;}
            }
            int supply=NearSupply(hero);
            if(supply>=0){target=supply;return ChannelKind.Supply;}
            return ChannelKind.None;
        }

        public bool Interact(Actor hero)
        {
            if(hero.Channel!=ChannelKind.None||hero.Incapacitated(Tick))return false;
            var kind=GetInteraction(hero,out var target);
            switch(kind)
            {
                case ChannelKind.Revive: ++ReviveAttempts; return StartChannel(hero,kind,target,Data.Mode.Revive);
                case ChannelKind.Finish: return StartChannel(hero,kind,target,Data.Mode.Finish);
                case ChannelKind.Beacon: ++ReviveAttempts; return StartChannel(hero,kind,target,Data.Mode.Beacon);
                case ChannelKind.Supply: return StartChannel(hero,kind,target,Data.Items.Supply.OpenSeconds);
            }
            return false;
        }

        public bool NearStore(Actor hero) => hero.Active && Near(hero.Position,Map.Stores,3.5f)>=0;
        public bool CanShop(Actor hero) => NearStore(hero) && Tick-hero.LastCombat>=Clock.Ticks(Data.Mode.CombatCooldown);
        public bool RequestShop(Actor hero,int item)
        {
            if(!CanShop(hero)||hero.Channel!=ChannelKind.None||!CanBuy(hero,item))return false;
            return StartChannel(hero,ChannelKind.Shop,item,Data.Mode.Buy);
        }

        public bool CanBuy(Actor hero,int item) => (uint)item<(uint)Data.Items.Shop.Length &&
            hero.Gold>=Data.Items.Shop[item].Cost && (item==0||(hero.Bought&(1<<item))==0);

        private bool StartChannel(Actor hero,ChannelKind kind,int target,float duration)
        {
            hero.Channel=kind;
            hero.ChannelTarget=target;
            hero.ChannelStart=Tick;
            hero.ChannelUntil=Tick+Clock.Ticks(duration);
            hero.ChannelDamageVersion=hero.DamageVersion;
            hero.ChannelOrigin=hero.Position;
            hero.Motion=Vec2.Zero;
            Emit(EventKind.Channel,hero.Position,duration,hero.Id,target);
            return true;
        }

        public void CancelChannel(Actor hero)
        {
            hero.Channel=ChannelKind.None;
            hero.ChannelTarget=-1;
            hero.ChannelUntil=0;
        }

        partial void TickInteraction(Actor hero)
        {
            if(BeaconsOnline && hero.CarriedSoul<0)
            {
                var mate=Partner(hero);
                ref var soul=ref Souls[mate.Id];
                if(soul.Active(Tick)&&soul.Holder<0&&Vec2.Dist2(hero.Position,soul.Position)<1.65f*1.65f)
                {
                    hero.CarriedSoul=mate.Id;
                    soul.Holder=hero.Id;
                    Emit(EventKind.Loot,hero.Position,0,hero.Id,mate.Id);
                }
            }
            if(hero.CarriedSoul>=0)
            {
                ref var soul=ref Souls[hero.CarriedSoul];
                if(!BeaconsOnline||!soul.Active(Tick)){soul.Holder=-1;hero.CarriedSoul=-1;}
                else soul.Position=hero.Position;
            }
            if(hero.Channel==ChannelKind.None)return;
            if(!hero.Active||hero.DamageVersion!=hero.ChannelDamageVersion||hero.Incapacitated(Tick)||
                Vec2.Dist2(hero.ChannelOrigin,hero.Position)>1.0f||!ChannelValid(hero))
            {CancelChannel(hero);return;}
            if(Tick<hero.ChannelUntil)return;
            var kind=hero.Channel;
            var t=hero.ChannelTarget;
            CancelChannel(hero);
            switch(kind)
            {
                case ChannelKind.Revive:
                    Reactivate(Actors[t],Actors[t].Position,.35f);
                    ++Revives;
                    break;
                case ChannelKind.Finish:
                    EndDown(Actors[t],hero,DamageKind.Physical,false);
                    break;
                case ChannelKind.Beacon:
                    int id=hero.CarriedSoul;
                    var replaced=Actors[id];
                    replaced.Level=Math.Max(1,replaced.Level-1);
                    replaced.Xp=0;
                    replaced.Rank[2]=Math.Min(replaced.Rank[2],replaced.SkillCap(2));
                    replaced.Rank[0]=Math.Min(replaced.Rank[0],replaced.SkillCap(0));
                    replaced.Rank[1]=Math.Min(replaced.Rank[1],replaced.SkillCap(1));
                    replaced.SkillPoints=Math.Max(0,replaced.Level+1-replaced.Rank[0]-replaced.Rank[1]-replaced.Rank[2]);
                    Souls[id].Expires=0;
                    Souls[id].Holder=-1;
                    hero.CarriedSoul=-1;
                    RebuildLevelStats(replaced);
                    Reactivate(replaced,Map.Safe(Map.Beacons[t]+new Vec2(1.2f,1)),.50f);
                    ++Revives;
                    ++BeaconReturns;
                    break;
                case ChannelKind.Shop: Buy(hero,t);break;
                case ChannelKind.Supply: CompleteSupply(hero,t);break;
            }
        }

        private bool ChannelValid(Actor hero)
        {
            int i=hero.ChannelTarget;
            switch(hero.Channel)
            {
                case ChannelKind.Revive: return BeaconsOnline&&Actors[i].Life==Life.Downed&&Vec2.Dist2(hero.Position,Actors[i].Position)<2.6f*2.6f;
                case ChannelKind.Finish: return Actors[i].Life==Life.Downed&&Vec2.Dist2(hero.Position,Actors[i].Position)<2.25f*2.25f;
                case ChannelKind.Beacon: return BeaconsOnline&&hero.CarriedSoul>=0&&Souls[hero.CarriedSoul].Active(Tick)&&Vec2.Dist2(hero.Position,Map.Beacons[i])<3.3f*3.3f;
                case ChannelKind.Shop: return CanShop(hero)&&CanBuy(hero,i);
                case ChannelKind.Supply: return IsSupplyNear(hero,i);
                default:return false;
            }
        }

        private void Reactivate(Actor hero,Vec2 at,float health)
        {
            hero.Life=Life.Active;
            hero.Position=hero.Previous=at;
            hero.Motion=Vec2.Zero;
            hero.Hp=hero.MaxHp*health;
            hero.Shield=0;
            hero.GraceEnds=Tick+30;
            hero.DownUntil=0;
            hero.DownedBy=-1;
            hero.Cleanse();
            Array.Fill(hero.Aggressors,-10000);
            Emit(EventKind.Revive,at,health,hero.Id);
        }

        private void RebuildLevelStats(Actor h)
        {
            float levels=h.Level-1;
            var s=h.Profile.Stats;
            h.MaxHp=s.Hp*(1+.08f*levels);
            h.Atk=s.Damage*(1+.06f*levels);
            h.Armor=s.Armor+2*levels;
            h.Resist=s.Resist+1.5f*levels;
            for(int i=1;i<Data.Items.Shop.Length;i++)
            {
                if((h.Bought&(1<<i))==0)continue;
                var item=Data.Items.Shop[i];
                h.MaxHp+=item.Hp;h.Atk+=item.Damage;h.Armor+=item.Armor;h.Resist+=item.Resist;
            }
        }

        private bool Buy(Actor hero,int index)
        {
            if(!CanBuy(hero,index))return false;
            var item=Data.Items.Shop[index];
            hero.Gold-=item.Cost;
            hero.Bought|=1<<index;
            hero.MaxHp+=item.Hp;
            hero.Heal(item.Heal+item.Hp);
            hero.Speed*=1+item.Speed;
            hero.Armor+=item.Armor;
            hero.Resist+=item.Resist;
            hero.Atk+=item.Damage;
            hero.CooldownBonus+=item.Cooldown;
            Emit(EventKind.Purchase,hero.Position,index,hero.Id);
            return true;
        }

        public static int Near(Vec2 p,Vec2[] sites,float reach)
        {
            int slot=-1;float best=reach*reach;
            for(int i=0;i<sites.Length;i++)
            {
                var delta=Vec2.Dist2(p,sites[i]);
                if(delta<best){best=delta;slot=i;}
            }
            return slot;
        }

        partial void AdvanceMode()
        {
            KitePosition=Vec2.Lerp(KiteStart,KiteEnd,Clock.Clamp01(Time/Data.Mode.DropSeconds));
            Bagyo.Sample(Tick);
            if(Bagyo.Phase!=lastPhase)
            {
                lastPhase=Bagyo.Phase;
                Emit(EventKind.Warning,Bagyo.NextCenter,lastPhase);
                OnNewPhase();
            }
            if(Tick%15==0 && Bagyo.Phase>0)
            {
                foreach(var hero in Heroes)
                    if(hero.Active&&Bagyo.Outside(hero.Position))
                        Hit(hero,null,hero.MaxHp*Bagyo.Rate*.5f,DamageKind.Storm);
            }
            if(Tick%6==0) CaptureOutposts();
            if(Tick%180==158)
                foreach(var vent in Map.Vents) Emit(EventKind.Telegraph,vent,2.55f);
            if(Tick%180==0)
                foreach(var vent in Map.Vents)
                    foreach(var hero in Heroes)
                        if(hero.Active&&!hero.Has(Status.Flight,Tick)&&Vec2.Dist2(hero.Position,vent)<2.6f*2.6f)
                        {Hit(hero,null,hero.MaxHp*.085f,DamageKind.Hazard);StatusOn(hero,Status.Air,.30f);}
        }

        private void CaptureOutposts()
        {
            foreach(var post in Outposts)
            {
                int team=-1;
                bool contested=false;
                Actor first=null;
                foreach(var hero in Heroes)
                {
                    if(!hero.Active||Vec2.Dist2(hero.Position,post.Position)>4.35f*4.35f)continue;
                    if(team>=0&&team!=hero.Team){contested=true;break;}
                    team=hero.Team;first=hero;
                }
                if(team<0||contested||team==post.Owner){post.Claim=-1;}
                else if(post.Claim!=team){post.Claim=team;post.Since=Tick;}
                else if(Tick-post.Since>=Clock.Ticks(Data.Mode.Outpost))
                {
                    post.Owner=team;post.Claim=-1;
                    Emit(EventKind.Capture,post.Position,4,first?.Id??-1);
                }
                if(Tick%30!=0||post.Owner<0||TeamPlacement[post.Owner]!=0)continue;
                int holders=0;
                foreach(var h in Heroes)if(h.Alive&&h.Team==post.Owner)holders++;
                if(holders==0)continue;
                foreach(var h in Heroes)if(h.Alive&&h.Team==post.Owner)GainGold(h,Data.Mode.OutpostGold/holders,GoldFrom.Outpost);
            }
        }

        public bool MapVision(Actor observer,Actor other)
        {
            if(other==null)return false;
            if(observer.Team==other.Team)return true;
            if(other.Streak>=3)return true;
            if(other.Has(Status.Eclipse,Tick)&&other.EclipseFogUntil>Tick)return false;
            if(!Visible(observer,other))return false;
            float reach=BattleMap.HighGround(observer.Position)?19:17;
            if(Vec2.Dist2(observer.Position,other.Position)<reach*reach)return true;
            foreach(var post in Outposts)
                if(post.Owner==observer.Team&&Vec2.Dist2(post.Position,other.Position)<19*19)return true;
            return false;
        }

        partial void ResolveStandings()
        {
            foreach(var hero in Heroes)
                if(hero.Life==Life.Downed)
                {
                    var mate=Partner(hero);
                    if(mate==null||mate.Life==Life.Eliminated||mate.Life==Life.Downed)
                    {
                        var source=At(hero.DownedBy);
                        EndDown(hero,source,hero.DownKind,false);
                    }
                }
            Span<bool> live=stackalloc bool[HeroLimit];
            live.Clear();
            foreach(var actor in Heroes)if(actor.Life!=Life.Eliminated&&actor.Life!=Life.Downed)live[actor.Team]=true;
            int survivors=0,last=-1,unplaced=0;
            for(int i=0;i<TeamCount;i++)
            {
                if(TeamPlacement[i]==0)unplaced++;
                if(live[i]){survivors++;last=i;}
            }
            if(survivors==0)last=PressureLeader(live,false);
            // Simultaneous eliminations share placement. The next living team still has a unique first place.
            for(int i=0;i<TeamCount;i++)
                if(!live[i]&&TeamPlacement[i]==0)TeamPlacement[i]=Math.Max(2,unplaced);
            if(survivors<=1||Time>=Data.Mode.End)
            {
                if(survivors>1) last=PressureLeader(live,true);
                Winner=last;
                RankSurvivors(last,live);
                Finished=true;
                EndReason=survivors==1?"Last standing":survivors==0?"Simultaneous elimination":"Sudden-death decision";
            }
        }

        private int PressureLeader(Span<bool> live,bool requireLife)
        {
            float best=float.NegativeInfinity;
            int team=0;
            for(int i=0;i<TeamCount;i++)
            {
                if(requireLife&&!live[i])continue;
                if(!requireLife&&TeamPlacement[i]!=0)continue;
                float score=PressureScore(i);
                if(score>best){best=score;team=i;}
            }
            return team;
        }

        private float PressureScore(int team)
        {
            float score=0;
            foreach(var hero in Heroes)if(hero.Team==team)
                score+=(hero.Active ? hero.Hp+hero.Shield*.35f : 0)+hero.DamageDealt*.022f+hero.Kills*38+hero.Assists*10;
            return score;
        }

        private void RankSurvivors(int winner,Span<bool> live)
        {
            TeamPlacement[winner]=1;
            live[winner]=false;
            for(int place=2;place<=TeamCount;place++)
            {
                int best=-1;float score=float.NegativeInfinity;
                for(int i=0;i<TeamCount;i++)
                {
                    if(!live[i])continue;
                    float s=PressureScore(i);
                    if(s>score){best=i;score=s;}
                }
                if(best<0)break;
                TeamPlacement[best]=place;live[best]=false;
            }
        }

        partial void OnNewPhase();
        partial void DropWeapon(Actor hero);
        partial void CompleteSupply(Actor hero,int slot);
    }
}
