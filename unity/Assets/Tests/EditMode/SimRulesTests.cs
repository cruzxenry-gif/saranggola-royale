using NUnit.Framework;
using Saranggola.Sim;

public sealed class SimRulesTests
{
    [Test]
    public void ArmorAndResistApplyIndependentMitigation()
    {
        var match = new Match(new Catalog(), 9, PlayMode.Solo);
        match.BotsEnabled = false;
        var hero = match.Player;
        var victim = match.Heroes[2];
        hero.Life = victim.Life = Life.Active;
        victim.Armor = 100;
        victim.Resist = 25;
        Assert.That(match.Hit(victim, hero, 100), Is.EqualTo(50).Within(.01));
        Assert.That(match.Hit(victim, hero, 100, DamageKind.Magic), Is.EqualTo(80).Within(.01));
        Assert.That(match.Hit(victim, hero, 100, DamageKind.Storm), Is.EqualTo(100).Within(.01));
    }

    [Test]
    public void HardCrowdControlDiminishesAndCleanseClearsDebuffs()
    {
        var hero = new Actor(0);
        hero.Apply(Status.Root, 30, 2);
        hero.Apply(Status.Stun, 66, 2);
        Assert.That(hero.StatusEnd[(int)Status.Root], Is.EqualTo(90));
        Assert.That(hero.StatusEnd[(int)Status.Stun], Is.EqualTo(96));
        hero.Apply(Status.Haste, 66, 3, .25f);
        hero.Cleanse();
        Assert.That(hero.Has(Status.Stun, 66), Is.False);
        Assert.That(hero.Has(Status.Haste, 66), Is.True);
    }

    [Test]
    public void RankedSkillCooldownConsumesExactlyOneActivation()
    {
        var match = new Match(new Catalog(), 19);
        var hero = match.Player;
        hero.Life = Life.Active;
        Assert.That(match.Cast(hero, 2), Is.False);
        Assert.That(match.Cast(hero, 0), Is.True);
        Assert.That(hero.Cooldowns[0], Is.EqualTo(Clock.Ticks(hero.Profile.Abilities[0].Cooldown)));
        Assert.That(match.Cast(hero, 0), Is.False);
        match.AwardXp(hero, 400);
        Assert.That(hero.Improve(2), Is.True);
    }

    [Test]
    public void SameSeedAndInputsYieldSameHash()
    {
        var data = new Catalog();
        var left = new Match(data, 103, allBots: true);
        var right = new Match(data, 103, allBots: true);
        var input = SkillInput.Empty;
        for (int i = 0; i < 2700; i++) { left.Step(input); right.Step(input); }
        Assert.That(left.Hash(), Is.EqualTo(right.Hash()));
    }

    [Test]
    public void HealthMovementAndShieldsDoNotShareUnityState()
    {
        var match = new Match(new Catalog(), 20, PlayMode.Solo);
        var actor = match.Player;
        actor.Life = Life.Active;
        actor.Shield = 125;
        float hp = actor.Hp;
        match.Hit(actor, match.Heroes[1], 75);
        Assert.That(actor.Hp, Is.EqualTo(hp));
        Assert.That(actor.Shield, Is.LessThan(125));
        Assert.That(match.Map.Walkable(match.Map.Safe(new Vec2(80, 29))), Is.True);
    }

    [Test]
    public void StormShrinksOnScheduleAndDisablesBeaconInPhaseThree()
    {
        var data = new Catalog();
        var match = new Match(data, 11);
        var storm = match.Bagyo;
        storm.Sample(Clock.Ticks(60));
        Assert.That(storm.Phase, Is.EqualTo(1));
        Assert.That(storm.Rate, Is.EqualTo(.01f));
        storm.Sample(Clock.Ticks(92));
        Assert.That(storm.Radius, Is.EqualTo(data.Mode.InitialRadius * .7f).Within(.01));
        storm.Sample(Clock.Ticks(240));
        Assert.That(storm.Phase, Is.EqualTo(3));
        Assert.That(match.BeaconsOnline, Is.False);
        Assert.That(storm.Rate, Is.EqualTo(.035f));
        storm.Sample(Clock.Ticks(495));
        Assert.That(storm.Sudden, Is.True);
        Assert.That(storm.Radius, Is.EqualTo(data.Mode.SuddenRadius).Within(.01));
        Assert.That(storm.Rate, Is.EqualTo(.10f));
    }

    [Test]
    public void DownedAllyChannelCancelsOnDamageAndRevivesAtThirtyFivePercent()
    {
        var match = new Match(new Catalog(), 11);
        match.BotsEnabled = false;
        var player = match.Player;
        var ally = match.Partner(player);
        var enemy = match.Heroes[3];
        player.Life = ally.Life = enemy.Life = Life.Active;
        player.Position = ally.Position = new Vec2(-15, -12);
        enemy.Position = new Vec2(58, -41);
        match.Hit(player, enemy, 10000);
        Assert.That(player.Life, Is.EqualTo(Life.Downed));
        Assert.That(match.Interact(ally), Is.True);
        match.Hit(ally, enemy, 10);
        match.Step(SkillInput.Empty);
        Assert.That(ally.Channel, Is.EqualTo(ChannelKind.None));
        Assert.That(match.Interact(ally), Is.True);
        for (int i = 0; i <= Clock.Ticks(3); i++) match.Step(SkillInput.Empty);
        Assert.That(player.Life, Is.EqualTo(Life.Active));
        Assert.That(player.HealthFraction, Is.EqualTo(.35f).Within(.01));
    }

    [Test]
    public void SoloKillAwardsBountyAndCatchUpXpWithoutDowning()
    {
        var match = new Match(new Catalog(), 7, PlayMode.Solo);
        match.BotsEnabled = false;
        var player = match.Player;
        var enemy = match.Heroes[1];
        player.Life = enemy.Life = Life.Active;
        enemy.Streak = 3;
        enemy.Level = 3;
        enemy.Gold = 310;
        match.Hit(enemy, player, 10000);
        Assert.That(enemy.Life, Is.EqualTo(Life.Eliminated));
        Assert.That(player.Kills, Is.EqualTo(1));
        Assert.That(player.Earnings[(int)GoldFrom.Kill], Is.EqualTo(200));
        Assert.That(player.Earnings[(int)GoldFrom.Bounty], Is.EqualTo(150));
        Assert.That(player.Earnings[(int)GoldFrom.Stolen], Is.EqualTo(155));
        Assert.That(player.Level, Is.EqualTo(2));
        Assert.That(player.Xp, Is.EqualTo(60).Within(.01));
    }

    [Test]
    public void FullDuoSimulationHasOneWinnerAndOpposingLosses()
    {
        var match = new Match(new Catalog(), 19, allBots: true);
        while (!match.Finished && match.Time < 541) match.Step(SkillInput.Empty);
        Assert.That(match.Finished, Is.True);
        Assert.That(match.Time, Is.LessThanOrEqualTo(540.01));
        Assert.That(match.TeamPlacement[match.Winner], Is.EqualTo(1));
        foreach (var team in match.TeamPlacement)
            Assert.That(team, Is.GreaterThanOrEqualTo(1));
        int wins = 0;
        foreach (var team in match.TeamPlacement) if (team == 1) wins++;
        Assert.That(wins, Is.EqualTo(1));
    }

    [Test]
    public void EquippedWeaponConsumesDurabilityAndDropsOnElimination()
    {
        var match = new Match(new Catalog(), 23, PlayMode.Solo);
        match.BotsEnabled = false;
        var hero = match.Player;
        var enemy = match.Heroes[1];
        hero.Life = enemy.Life = Life.Active;
        var drop = -1;
        for (int i = 0; i < match.LootCount; i++)
        {
            var item = match.Loot[i];
            if (item.Kind == LootKind.Weapon && item.Rarity < 2) { drop = i; break; }
        }
        Assert.That(drop, Is.GreaterThanOrEqualTo(0));
        var gear = match.Loot[drop];
        hero.Position = gear.At;
        Assert.That(match.Take(hero, drop), Is.True);
        Assert.That(hero.Weapon, Is.EqualTo(gear.Item));
        hero.Position = Vec2.Zero;
        enemy.Position = new Vec2(1.2f, 0);
        hero.Lock = enemy.Id;
        Assert.That(match.Basic(hero), Is.True);
        Assert.That(hero.Durability, Is.EqualTo(gear.Durability - 1));
        match.Hit(hero, enemy, 10000);
        Assert.That(hero.Weapon, Is.EqualTo(-1));
        bool dropped = false;
        for (int i = 0; i < match.LootCount; i++)
        {
            var item = match.Loot[i];
            if (item.Kind == LootKind.Weapon && item.Item == gear.Item && item.Durability == gear.Durability - 1
                && item.Live(match.Tick) && Vec2.Dist2(item.At, hero.Position) < 4) dropped = true;
        }
        Assert.That(dropped, Is.True);
    }
}
