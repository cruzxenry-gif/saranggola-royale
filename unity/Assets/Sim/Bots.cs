using System;

namespace Saranggola.Sim
{
    public enum Intent { Rotate, Fight, Retreat, Loot, Farm, Revive, Finish, Soul, Beacon, Supply, Capture, Shop, Ping }

    public sealed partial class Match
    {
        private sealed class Brain
        {
            public Vec2 Drop, Anchor, Previous;
            public Vec2 Retreat;
            public int RetreatUntil;
            public float JumpAt;
            public int ThinkAt, ClearSide, Stalled;
        }

        private Brain[] brains;

        partial void InitBots()
        {
            brains = new Brain[Heroes.Length];
            for (int i = 0; i < Heroes.Length; i++)
            {
                var b = new Brain { ThinkAt = i, ClearSide = i % 2 == 0 ? 1 : -1 };
                var path = KiteEnd - KiteStart;
                var sides = UsesDuos ? i / 2 : i;
                float t0 = .10f + (sides + .5f) / TeamCount * .79f;
                var preferred = Vec2.Lerp(KiteStart,KiteEnd,t0)
                    + KiteDirection.Perpendicular * ((sides % 2 == 0 ? -1 : 1) * Rng.Range(14,26));
                if (UsesDuos && (i & 1) != 0) preferred = brains[i - 1].Drop;
                float best = float.NegativeInfinity;
                for (int j = 0; j < LootCount; j++)
                {
                    var pickup = Loot[j];
                    if (!pickup.Live(Tick) || pickup.At.SqrLength < 37*37) continue;
                    var t = Clock.Clamp(Vec2.Dot(pickup.At - KiteStart,path) / path.SqrLength,.08f,.94f);
                    float gap = Vec2.Dist2(KiteStart + path * t,pickup.At);
                    if(gap > 33*33) continue;
                    float value = pickup.Rarity * 6 + Rng.Range(0,4) - (pickup.At-preferred).Length;
                    if (value <= best) continue;
                    best = value;
                    b.Drop = pickup.At;
                    b.JumpAt = t * Data.Mode.DropSeconds;
                }
                if (float.IsNegativeInfinity(best))
                {
                    b.Drop=Map.Safe(preferred);
                    b.JumpAt=t0*Data.Mode.DropSeconds;
                }
                b.Anchor = Vec2.Right.Rotate((i / (UsesDuos?2:1)) * Clock.Tau / TeamCount + Rng.Range(-.2f,.2f));
                brains[i] = b;
            }
        }

        partial void AdvanceBots()
        {
            for (int i = 0; i < Heroes.Length; i++)
            {
                var h = Heroes[i];
                if (h.Human || !h.Alive || Mode == PlayMode.Practice) continue;
                var b = brains[i];
                if (h.Life == Life.Riding) { if (Time >= b.JumpAt) Jump(h); continue; }
                if (h.Life == Life.Gliding)
                {
                    h.Motion = (b.Drop - h.Position).Normalized;
                    if (Vec2.Dist2(h.Position,b.Drop)<2.2f*2.2f && Map.Walkable(h.Position)) Dive(h);
                    continue;
                }
                if (Tick < b.ThinkAt) continue;
                b.ThinkAt = Tick + Data.Mode.ReactionTicks[(int)Difficulty];
                if (h.Life == Life.Downed)
                {
                    var mate = Partner(h);
                    h.Motion = mate != null && mate.Active && Vec2.Dist2(h.Position,mate.Position)>2
                        ? Steer(h,b,mate.Position) : Vec2.Zero;
                    continue;
                }
                if (h.Channel != ChannelKind.None) continue;
                Spend(h);
                Think(h,b);
            }
        }

        private static void Spend(Actor h)
        {
            if (h.SkillPoints == 0) return;
            if (h.Improve(2)) return;
            if (h.Rank[0] <= h.Rank[1] && h.Improve(0)) return;
            if (!h.Improve(1)) h.Improve(0);
        }

        private void Think(Actor h, Brain b)
        {
            float vision = Time < 150 ? 9.5f : Time < 240 ? 13.5f : 20;
            var enemy = Target(h, vision + (BattleMap.HighGround(h.Position) ? 2 : 0), UnitKind.Hero);
            float enemyGap = enemy == null ? 1000 : (enemy.Position - h.Position).Length;
            var mate = Partner(h);
            bool storm = Bagyo.Outside(h.Position);
            bool pressure = Bagyo.Phase >= 2 && Vec2.Dist2(h.Position, Bagyo.NextCenter) > MathF.Pow(Bagyo.NextRadius*.92f,2);
            bool retreat = h.HealthFraction < (Time<410?.68f:.37f) && enemyGap < 12 && enemy.HealthFraction>.24f;
            var goal=Map.Safe(Bagyo.NextCenter + b.Anchor * (Bagyo.NextRadius*(Bagyo.Sudden ? .25f : .72f)));
            int objective=-1;
            if (Bagyo.Phase < 3 && !storm && !pressure)
                goal = Map.Safe((h.Position - Bagyo.NextCenter).Limit(Bagyo.NextRadius*.73f)+Bagyo.NextCenter);
            float score=Bagyo.Sudden ? 135 : storm ? 122 : pressure ? 45 : Time<210 ? 29 : Time<350 ? 27 : 21;
            var intent=Intent.Rotate;

            if (retreat && !Bagyo.Sudden)
            {
                var escape=(h.Position-enemy.Position).Normalized;
                var hide=Map.Safe(h.Position+escape*10);
                if(Bagyo.Outside(hide))
                    hide=Map.Safe(Bagyo.Center+(h.Position+escape*8-Bagyo.Center).Limit(Bagyo.Radius*.86f));
                if (!Bagyo.Outside(hide) && Vec2.Dist2(hide,h.Position)>2.5f)
                {
                    b.Retreat=hide;
                    b.RetreatUntil=Tick+Clock.Ticks(2.2f);
                }
            }

            if(!storm&&b.RetreatUntil>Tick&&Vec2.Dist2(b.Retreat,h.Position)>.6f)
                Choose(ref score,ref intent,ref goal,ref objective,86,Intent.Retreat,b.Retreat);

            if (mate != null)
            {
                if (mate.Life == Life.Downed && BeaconsOnline)
                    Choose(ref score,ref intent,ref goal,ref objective,105-(h.Position-mate.Position).Length*1.45f,Intent.Revive,mate.Position,mate.Id);
                var soul = Souls[mate.Id];
                if (BeaconsOnline && soul.Active(Tick))
                {
                    if(h.CarriedSoul==mate.Id)
                    {
                        foreach(var beacon in Map.Beacons)
                            Choose(ref score,ref intent,ref goal,ref objective,91-(h.Position-beacon).Length*.85f-(Bagyo.Outside(beacon)?32:0),Intent.Beacon,beacon);
                    }
                    else if(soul.Holder<0)
                        Choose(ref score,ref intent,ref goal,ref objective,90-(h.Position-soul.Position).Length*1.55f,Intent.Soul,soul.Position);
                }
                if (mate.Active && Vec2.Dist2(h.Position,mate.Position)>36*36 && Time>48)
                    Choose(ref score,ref intent,ref goal,ref objective,19,Intent.Rotate,mate.Position);
                if (Tick - LastTeamPingTick[h.Team] < 180)
                {
                    var p=TeamPing[h.Team];
                    if (TeamPingKind[h.Team]==2 && enemy!=null) p=Map.Safe(h.Position+(h.Position-enemy.Position).Normalized*12);
                    Choose(ref score,ref intent,ref goal,ref objective,TeamPingKind[h.Team]==3?12:76,Intent.Ping,p);
                }
            }
            if (enemy!=null)
            {
                float risk=(h.HealthFraction-enemy.HealthFraction)*24+(h.Level-enemy.Level)*3;
                if (mate!=null&&mate.Active&&Vec2.Dist2(mate.Position,enemy.Position)<13*13)risk+=10;
                if (Tick-enemy.LastCombat<60)risk+=5;
                if(BattleMap.HighGround(h.Position))risk+=4;
                float aggression=Time<140?-2:Time<245?3:Time<342?6:Time<417?9:81;
                if (enemy.Attacker==h.Id&&Tick-enemy.LastDamage<95||h.Attacker==enemy.Id&&Tick-h.LastDamage<95)aggression+=8;
                if(h.HealthFraction<.45f&&enemy.HealthFraction>.34f&&Time<385)aggression-=9;
                Choose(ref score,ref intent,ref goal,ref objective,aggression+risk-enemyGap*.81f,Intent.Fight,enemy.Position,enemy.Id);
            }
            if (BeaconsOnline)
                foreach(var foe in Heroes)
                    if(foe.Team!=h.Team&&foe.Life==Life.Downed&&Visible(h,foe))
                        Choose(ref score,ref intent,ref goal,ref objective,(Time<150?32:Time<225?47:73)-(h.Position-foe.Position).Length*3,Intent.Finish,foe.Position,foe.Id);

            PickBotItem(h, enemyGap);
            if (score < 85 && !Bagyo.Sudden)
            {
                for (int i=0;i<LootCount;i++)
                {
                    var p=Loot[i];
                    if(!p.Live(Tick)||p.Ready>Tick||Bagyo.Outside(p.At))continue;
                    float d=(h.Position-p.At).Length;
                    if(d>33)continue;
                    if(p.Kind==LootKind.Box)
                        Choose(ref score,ref intent,ref goal,ref objective,48-d*.73f,Intent.Supply,p.At,i);
                    else if(p.Kind==LootKind.Weapon)
                    {
                        float value = h.Weapon < 0 ? 42+p.Rarity*8
                            : (p.Rarity-Data.Weapons[h.Weapon].Rarity)*11 + (p.Item==h.Weapon ? 0 : 3) + (h.Durability<9 ? 13 : 0);
                        Choose(ref score,ref intent,ref goal,ref objective,value - d*.92f,Intent.Loot,p.At,i);
                    }
                }
                for(int i=FirstMob;i<ActorCount;i++)
                {
                    var mob=Actors[i];
                    if(!mob.Active||Bagyo.Outside(mob.Position))continue;
                    float d=(h.Position-mob.Position).Length;
                    if(d>42 || mob == Boss && (h.Level < 3 || h.HealthFraction < .69f || enemyGap<10))continue;
                    float travel=d/MathF.Max(1,h.WalkSpeed(Tick));
                    float clear=mob.Hp/MathF.Max(25,h.Power(Tick)*h.Rate(Tick)*1.22f);
                    float value=(mob.Mob.Xp+mob.Mob.Gold)/(clear+travel+2);
                    float bonus=mob==Boss?49:Time<240?(h.Level<5?31:25):18;
                    Choose(ref score,ref intent,ref goal,ref objective,bonus+value*1.28f-d*.42f,Intent.Farm,mob.Position,i);
                }
                for(int i=0;i<Outposts.Length;i++)
                {
                    var post=Outposts[i];
                    if(post.Owner==h.Team||Bagyo.Outside(post.Position))continue;
                    float d=(h.Position-post.Position).Length;
                    Choose(ref score,ref intent,ref goal,ref objective,(Time<310?30:16)-d*.66f,Intent.Capture,post.Position,i);
                }
                if(Tick-h.LastCombat>Clock.Ticks(Data.Mode.CombatCooldown)&&enemyGap>11)
                {
                    int purchase=ShopChoice(h);
                    if(purchase>=0)
                        foreach(var site in Map.Stores)
                            if(!Bagyo.Outside(site))
                                Choose(ref score,ref intent,ref goal,ref objective,(purchase==0?51:30)-(h.Position-site).Length*.78f,Intent.Shop,site,purchase);
                }
            }

            switch(intent)
            {
                case Intent.Revive:
                case Intent.Finish:
                case Intent.Beacon:
                case Intent.Supply:
                    if (GetInteraction(h,out _ )!=ChannelKind.None && Interact(h)) return;
                    break;
                case Intent.Shop:
                    if (RequestShop(h,objective)) return;
                    break;
                case Intent.Loot:
                    if (Vec2.Dist2(h.Position,goal)<2.55f*2.55f && Take(h,objective)){h.Motion=Vec2.Zero;return;}
                    break;
            }
            var target=(intent==Intent.Fight||intent==Intent.Farm)?At(objective):null;
            if (target != null)
            {
                FightBot(h,b,target);
                return;
            }
            h.Lock=-1;
            if (enemy!=null && h.HealthFraction>.63f && !storm && enemyGap<AttackReach(h)+.20f &&
                (Tick-h.LastDamage<100 || Time>125 && intent!=Intent.Retreat))
            {
                h.Lock=enemy.Id;
                Basic(h);
            }
            float sensitivity=intent==Intent.Capture?1.8f : intent==Intent.Rotate ? 1.25f : .58f;
            h.Motion=Vec2.Dist2(h.Position,goal)<sensitivity*sensitivity?Vec2.Zero:Steer(h,b,goal);
        }

        private void FightBot(Actor h, Brain b, Actor foe)
        {
            if(!foe.Active)return;
            h.Lock=foe.Id;
            var delta=foe.Position-h.Position;
            float distance=delta.Length, basic=AttackReach(h);
            if(distance<basic+.37f) Basic(h);
            if(!foe.Active){h.Motion=Vec2.Zero;return;}
            bool fight=foe.Kind==UnitKind.Hero;
            bool meaningful=fight||foe.Hp>h.Atk*2.20f;
            if(foe==Boss&&h.HealthFraction<.39f) { h.Motion=Steer(h,b, h.Position-delta.Normalized*6); return; }
            if(meaningful&&distance<13)
            {
                var aim=delta.Normalized;
                float error=Data.Mode.AimError[(int)Difficulty];
                if (h.Rank[2]>0 && h.Cooldowns[2]<=Tick && (fight && (foe.HealthFraction<.60f || h.HealthFraction<.37f) || foe==Boss) &&
                    (h.Costume==6 || h.Costume==3 || h.Costume==5 || distance<6.1f))
                    Cast(h,2,aim.Rotate(Rng.Range(-error,error)));
                else if (h.Cooldowns[0]<=Tick && distance < h.Profile.Abilities[0].Range+.42f)
                    Cast(h,0,aim.Rotate(Rng.Range(-error,error)));
                else if (h.Cooldowns[1]<=Tick && (fight || h.Costume==5))
                {
                    bool good=false;
                    switch(h.Costume)
                    {
                        case 0: good=distance<8 && (h.HealthFraction<.60f||distance>basic);break;
                        case 1: good=distance<2.8f;aim=-aim;break;
                        case 2: good=distance>3.4f&&distance<8.1f;break;
                        case 3: good=distance>2.30f&&distance<5.8f;break;
                        case 4: good=h.HealthFraction<.57f&&distance>2.5f;break;
                        case 5: good=distance<3;break;
                        case 6: good=h.HealthFraction<.53f&&distance<6.8f;break;
                    }
                    if(good)Cast(h,1,aim.Rotate(Rng.Range(-error,error)));
                }
            }
            distance=(foe.Position-h.Position).Length;
            float wanted=basic>4?basic*.81f:MathF.Max(.9f,basic*.79f);
            if (distance>wanted+.22f || !Map.Sight(h.Position,foe.Position)) h.Motion=Steer(h,b,foe.Position);
            else if (fight && basic>4 && distance<MathF.Min(3.25f,wanted*.64f) && !Bagyo.Outside(h.Position-delta.Normalized*3))
                h.Motion=Steer(h,b,h.Position-delta.Normalized*3);
            else h.Motion=Vec2.Zero;
        }

        private void PickBotItem(Actor h,float threat)
        {
            for(int i=0;i<h.Items.Length;i++)
            {
                bool use=h.Items[i]==0&&h.HealthFraction<.70f||h.Items[i]==1&&h.Shield<30&&threat<8||
                         h.Items[i]==2&&(Bagyo.Outside(h.Position)||threat<6&&h.HealthFraction<.45f)||
                         h.Items[i]==3&&threat<7&&(h.Cooldowns[0]>Tick||h.Cooldowns[1]>Tick);
                if(use){UseItem(h,i);return;}
            }
        }

        private int ShopChoice(Actor h)
        {
            if(h.HealthFraction<.69f&&CanBuy(h,0))return 0;
            if(CanBuy(h,3))return 3;
            if(CanBuy(h,6))return 6;
            if(CanBuy(h,1))return 1;
            if(CanBuy(h,2))return 2;
            if(CanBuy(h,4))return 4;
            if(CanBuy(h,5))return 5;
            return -1;
        }

        private Vec2 Steer(Actor h,Brain b,Vec2 destination)
        {
            if (Vec2.Dist2(h.Position,b.Previous)<.19f*.19f) b.Stalled++;
            else b.Stalled=0;
            b.Previous=h.Position;
            var waypoint=Map.TravelWaypoint(h.Position,destination);
            var dir=(waypoint-h.Position).Normalized;
            if(dir.SqrLength<.001f||h.Has(Status.Flight,Tick))return dir;
            float look=b.Stalled>3?1.56f:1.02f;
            if(Map.Walkable(h.Position+dir*look)&&Map.Walkable(h.Position+dir*.35f))return dir;
            if(b.Stalled==8)b.ClearSide=-b.ClearSide;
            for(int i=1;i<=5;i++)
            {
                var a=dir.Rotate(b.ClearSide*i*.38f);
                var other=dir.Rotate(-b.ClearSide*i*.38f);
                if(Map.Walkable(h.Position+a*look)&&Map.Walkable(h.Position+a*.35f))return a;
                if(Map.Walkable(h.Position+other*look)&&Map.Walkable(h.Position+other*.35f))return other;
            }
            return dir.Rotate(b.ClearSide*1.08f);
        }

        private static void Choose(ref float best,ref Intent intent,ref Vec2 goal,ref int objective,float value,Intent option,Vec2 at,int target=-1)
        {
            if(value<=best)return;
            best=value;intent=option;goal=at;objective=target;
        }
    }
}
