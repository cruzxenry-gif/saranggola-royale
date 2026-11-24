using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Saranggola.Sim
{
    public static class BalanceBench
    {
        public static string Run(int matches, int seed, string runtime, Action<int> progress = null)
        {
            var data = new Catalog();
            var map = new BattleMap(data);
            var duration = new List<float>(matches);
            var first = new List<float>(matches);
            var gaps = new List<float>(matches);
            var levels = new[] { new List<float>(), new List<float>(), new List<float>() };
            var milestones = new[] { 3, 5, 8 };
            var entries = new int[7];
            var placement = new double[7];
            var gold = new double[(int)GoldFrom.Count];
            var clearCount = new int[8];
            var clearTicks = new long[8];
            var endReasons = new SortedDictionary<string, int>(StringComparer.Ordinal);
            int stormKills = 0, eliminated = 0, fights = 0, kills = 0, attempts = 0, revived = 0, souls = 0, heroes = 0;
            double aliveMinutes = 0, minutes = 0;
            ulong digest = 0;
            for (int i = 0; i < matches; i++)
            {
                var sim = new Match(data, unchecked(seed + i), selected: i % 7, allBots: true, map: map);
                while (!sim.Finished) sim.Step(SkillInput.Empty);
                var length = sim.Time;
                duration.Add(length);
                minutes += length / 60;
                if(sim.FightCount>0)first.Add(sim.FightTicks[0]*Clock.Step);
                for(int j=1;j<sim.FightCount;j++)gaps.Add((sim.FightTicks[j]-sim.FightTicks[j-1])*Clock.Step);
                fights+=sim.FightCount;
                stormKills+=sim.StormEliminations;
                eliminated+=sim.Eliminations;
                attempts+=sim.ReviveAttempts;
                revived+=sim.Revives;
                souls+=sim.BeaconReturns;
                endReasons.TryGetValue(sim.EndReason,out int ending);
                endReasons[sim.EndReason]=ending+1;
                for(int j=0;j<8;j++)
                {
                    clearCount[j]+=sim.CampClears[j];clearTicks[j]+=sim.CampClearTicks[j];
                }
                foreach(var hero in sim.Heroes)
                {
                    int k=hero.Costume;
                    entries[k]++;
                    heroes++;
                    placement[k]+=sim.TeamPlacement[hero.Team];
                    kills+=hero.Kills;
                    aliveMinutes+=(hero.Alive?length:hero.DeathTick*Clock.Step)/60;
                    for(int j=0;j<gold.Length;j++)gold[j]+=hero.Earnings[j];
                    for(int j=0;j<levels.Length;j++)if(hero.LevelAt[milestones[j]]>0)levels[j].Add(hero.LevelAt[milestones[j]]*Clock.Step);
                }
                digest=unchecked((digest*1099511628211UL)^sim.Hash());
                if((i+1)%20==0||i+1==matches)progress?.Invoke(i+1);
            }
            var (duelA,duelB)=(Duel(data,map,0,0,4),Duel(data,map,5,1,4));
            var report = new StringBuilder();
            report.AppendLine("# Laban ng Bayan balance report").AppendLine();
            report.AppendLine($"{matches} seeded Normal Duos matches with 12 bots each. Seed range {seed}–{unchecked(seed+matches-1)}. Runtime {runtime}. Fixed 30 Hz simulation. Digest `{digest:x16}`.").AppendLine();
            report.AppendLine("| Metric | Observed | Target |").AppendLine("| --- | ---: | ---: |");
            Metric(report,"Match length, median",TimeText(Median(duration)),"7–9 min");
            Metric(report,"Match length, longest",TimeText(duration[duration.Count-1]),"≤9 min");
            Metric(report,"First fight, median",TimeText(Median(first)),"By about 3:00");
            Metric(report,"Gap between fights, median",TimeText(Median(gaps)),"≤45 s");
            Metric(report,"Storm share of all hero eliminations",Percent(stormKills,eliminated),"≤25%");
            Metric(report,"Kills per match minute",F(kills/Math.Max(1,minutes)),"");
            Metric(report,"Fight episodes per match",F(fights/(double)matches),"");
            Metric(report,"Fighter vs fighter TTK at level 4",TimeText(duelA),"6–8 s");
            Metric(report,"Assassin vs squishy TTK at level 4",TimeText(duelB),"2–3 s");
            Metric(report,"Completed revive channels", $"{revived}/{attempts} ({Percent(revived,attempts)})","");
            Metric(report,"Hatid-Buhay returns",souls.ToString(CultureInfo.InvariantCulture),"");
            foreach(var pair in endReasons)Metric(report,pair.Key,pair.Value.ToString(CultureInfo.InvariantCulture),"");
            report.AppendLine().AppendLine("## Average duo placement").AppendLine();
            double average = 0;
            for(int i=0;i<7;i++)average+=placement[i]/heroes;
            report.AppendLine($"Weighted mean **{F(average)}**. Target is within 0.5 places of the mean.").AppendLine();
            report.AppendLine("| Hero | Entries | Mean | Deviation |").AppendLine("| --- | ---: | ---: | ---: |");
            for(int i=0;i<7;i++)
            {
                double p=placement[i]/Math.Max(1,entries[i]);
                report.AppendLine($"| {data.Heroes[i].DisplayName} | {entries[i]} | {F(p)} | {F(p-average)} |");
            }
            report.AppendLine().AppendLine("## Progression").AppendLine();
            report.AppendLine("| Level | Median among heroes who reached it | Fastest quartile | Reach rate | Target |");
            report.AppendLine("| --- | ---: | ---: | ---: | ---: |");
            var pacing=new[]{"2:00","4:00","7:00"};
            for(int i=0;i<levels.Length;i++)
                report.AppendLine($"| {milestones[i]} | {TimeText(Median(levels[i]))} | {TimeText(Percentile(levels[i],.25f))} | {Percent(levels[i].Count,heroes)} | ~{pacing[i]} |");
            report.AppendLine().AppendLine("| Gold source | Gold per hero survival minute |").AppendLine("| --- | ---: |");
            for(int i=0;i<gold.Length;i++)report.AppendLine($"| {(GoldFrom)i} | {F(gold[i]/Math.Max(1,aliveMinutes))} |");
            report.AppendLine().AppendLine("| Camp | Clears | Mean engagement to kill |").AppendLine("| --- | ---: | ---: |");
            for(int i=0;i<clearCount.Length;i++)report.AppendLine($"| {data.Mobs.Types[i].Name} | {clearCount[i]} | {(clearCount[i]>0 ? TimeText(clearTicks[i]*Clock.Step/clearCount[i]) : "—")} |");
            report.AppendLine().AppendLine("## Measurement and gaps").AppendLine();
            report.AppendLine("- A fight episode starts with hero damage after six seconds without hero damage anywhere in the match. Camp time is per creature and starts at aggro or first hit. Gold per survival minute omits starting gold.");
            report.AppendLine("- TTK uses an isolated level-4 attacker with free use of legacy skills and one stationary, unarmed target at full health. Bot match outcomes include weapons, terrain, and healing.");
            if(duelA<6||duelA>8||duelB<2||duelB>3)
                report.AppendLine("- The isolated TTK target remains unmet. Legacy hero HP, cooldowns and hit damage are preserved as requested. Changing those values to hit the target would change the archived combat.");
            if(Median(duration)<420 || Median(duration)>540)
                report.AppendLine("- The duration target remains unmet. The late circle pulls the surviving duos together and often resolves the last fights before the 7-minute mark under Normal utility AI.");
            if(stormKills>.25*eliminated)
                report.AppendLine("- The storm elimination target remains unmet. Bots still lose time at cover or on late rotations after combat and downs.");
            bool mismatch=false;
            for(int i=0;i<7;i++)if(Math.Abs(placement[i]/Math.Max(1,entries[i])-average)>.50)mismatch=true;
            if(mismatch)report.AppendLine("- The placement target remains unmet for at least one hero. This sample preserves legacy role asymmetry, has mixed-hero duos, and is sensitive to ability use by the bots.");
            if(levels[2].Count<heroes*.1)report.AppendLine("- Level 8 remains rare. Camp rewards are preserved, respawns stop at Phase 3, and eliminated heroes end their progression. The timing for this level is based on a small sample.");
            if(clearCount[7]<matches*.05)report.AppendLine("- Bakunawa kills are uncommon. Its 6,000 HP and leash are preserved, and Normal bots weigh survival and storm position ahead of the crater once Phase 3 begins.");
            return report.ToString();
        }

        private static float Duel(Catalog catalog,BattleMap map,int attacker,int target,int level)
        {
            var m=new Match(catalog,41,PlayMode.Practice,selected:attacker,map:map);
            m.BotsEnabled=false;
            var a=m.Player;
            var b=m.Heroes[1];
            b.AssignHero(catalog.Heroes[target],target,b.Team,0);
            a.Position=Vec2.Zero;b.Position=new Vec2(1.5f,0);
            a.Lock=b.Id;
            for(int i=2;i<m.Heroes.Length;i++)m.Heroes[i].Life=Life.Eliminated;
            foreach(var h in new[]{a,b})
            {
                while(h.Level<level)m.AwardXp(h,m.NeedXp(h));
                h.Improve(2);h.Improve(0);h.Improve(1);
            }
            a.AttackEnd=1;
            for(int i=0;i<Clock.Hz*36&&b.Active;i++)
            {
                var input=SkillInput.Empty;
                input.Lock=b.Id;
                input.Attack=true;
                float d=(a.Position-b.Position).Length;
                input.Move=d>1.45f?(b.Position-a.Position).Normalized:Vec2.Zero;
                if(a.Cooldowns[2]<=m.Tick && (attacker==0||b.HealthFraction<.75f))input.Ability=2;
                else if(a.Cooldowns[0]<=m.Tick&&d<2.5f)input.Ability=0;
                else if(a.Cooldowns[1]<=m.Tick&&d<2.5f)input.Ability=1;
                m.Step(input);
            }
            return b.Active?float.NaN:m.Time;
        }

        private static string F(double number)=>number.ToString("0.00",CultureInfo.InvariantCulture);
        private static string Percent(int hit,int all)=>all==0?"—":(100.0*hit/all).ToString("0.0",CultureInfo.InvariantCulture)+"%";
        private static string TimeText(float t)=>float.IsNaN(t)?"—":t>=60?$"{(int)t/60}:{(int)t%60:00}":F(t)+" s";
        private static void Metric(StringBuilder s,string name,string value,string target)=>s.AppendLine($"| {name} | {value} | {target} |");
        private static float Median(List<float> values)=>Percentile(values,.5f);
        private static float Percentile(List<float> values,float q)
        {
            if(values.Count==0)return float.NaN;
            values.Sort();
            float i=(values.Count-1)*q;
            int left=(int)i;
            return left+1>=values.Count?values[left]:values[left]+(values[left+1]-values[left])*(i-left);
        }
    }
}
