namespace Saranggola.Sim
{
    // Generated from the archived Godot data tables. The mode overrides live in Catalog.
    internal static class LegacyTables
    {
        internal const string Heroes = @"[
  {
    ""id"": ""juan"",
    ""display_name"": ""Juan Dela Cruz"",
    ""role"": ""Fighter"",
    ""color"": ""#55b9a1"",
    ""stats"": {
      ""hp"": 900,
      ""damage"": 62,
      ""armor"": 28,
      ""resist"": 20,
      ""speed"": 5.0,
      ""range"": 2.2,
      ""attack_speed"": 1.0,
      ""hit_radius"": 0.55,
      ""magic"": false
    },
    ""abilities"": [
      {
        ""id"": ""combo"",
        ""name"": ""Tatlong Hataw"",
        ""short"": ""HATAW"",
        ""cooldown"": 6,
        ""description"": ""Three bolo cuts. The final spin knocks enemies up."",
        ""shape"": ""cone"",
        ""range"": 3.4,
        ""effect"": {
          ""damage"": 132,
          ""spin"": 186
        }
      },
      {
        ""id"": ""shout"",
        ""name"": ""Sigaw ng Bayan"",
        ""short"": ""SIGAW"",
        ""cooldown"": 14,
        ""description"": ""Speed up allies and slow enemy attack speed in an 8 m radius."",
        ""shape"": ""circle"",
        ""range"": 8,
        ""effect"": {
          ""duration"": 3
        }
      },
      {
        ""id"": ""flag"",
        ""name"": ""Kalayaan!"",
        ""short"": ""FLAG"",
        ""cooldown"": 70,
        ""description"": ""Raise a flag that empowers and cleanses allies while slowing enemies."",
        ""shape"": ""circle"",
        ""range"": 6,
        ""effect"": {
          ""duration"": 6
        }
      }
    ],
    ""vfx"": ""#f9c977"",
    ""passive"": {
      ""name"": ""Bayanihan"",
      ""description"": ""Stay within 6 m of a teammate to share 10% damage reduction.""
    }
  },
  {
    ""id"": ""pedro"",
    ""display_name"": ""Pedro Penduko"",
    ""role"": ""Mage"",
    ""color"": ""#ab8bec"",
    ""stats"": {
      ""hp"": 640,
      ""damage"": 58,
      ""armor"": 14,
      ""resist"": 22,
      ""speed"": 4.8,
      ""range"": 7.0,
      ""attack_speed"": 0.9,
      ""hit_radius"": 0.55,
      ""magic"": true
    },
    ""abilities"": [
      {
        ""id"": ""bolt"",
        ""name"": ""Anting-anting Bolt"",
        ""short"": ""BOLT"",
        ""cooldown"": 5,
        ""description"": ""Fire a piercing shot that marks each enemy for extra damage."",
        ""shape"": ""line"",
        ""range"": 9,
        ""effect"": {
          ""damage"": 226
        }
      },
      {
        ""id"": ""blink"",
        ""name"": ""Blink"",
        ""short"": ""BLINK"",
        ""cooldown"": 12,
        ""description"": ""Teleport 6 m and leave an afterimage that bursts after a second."",
        ""shape"": ""line"",
        ""range"": 6,
        ""effect"": {
          ""damage"": 190,
          ""radius"": 3
        }
      },
      {
        ""id"": ""mirror"",
        ""name"": ""Salamin"",
        ""short"": ""MIRROR"",
        ""cooldown"": 65,
        ""description"": ""Turn invisible and summon two doubles that copy your attacks."",
        ""shape"": ""circle"",
        ""range"": 4,
        ""effect"": {
          ""duration"": 6,
          ""damage"": 160
        }
      }
    ],
    ""vfx"": ""#d4aaff"",
    ""passive"": {
      ""name"": ""Anting-anting"",
      ""description"": ""Every fourth basic attack chains lightning to two enemies.""
    }
  },
  {
    ""id"": ""bato"",
    ""display_name"": ""Bato"",
    ""role"": ""Tank"",
    ""color"": ""#a8ad66"",
    ""stats"": {
      ""hp"": 1300,
      ""damage"": 52,
      ""armor"": 45,
      ""resist"": 30,
      ""speed"": 4.4,
      ""range"": 2.4,
      ""attack_speed"": 0.8,
      ""hit_radius"": 0.67,
      ""magic"": false
    },
    ""abilities"": [
      {
        ""id"": ""smash"",
        ""name"": ""Dagundong"",
        ""short"": ""SMASH"",
        ""cooldown"": 9,
        ""description"": ""Smash the ground, damaging and stunning foes within 4 m."",
        ""shape"": ""circle"",
        ""range"": 4,
        ""effect"": {
          ""damage"": 240
        }
      },
      {
        ""id"": ""leap"",
        ""name"": ""Talon"",
        ""short"": ""LEAP"",
        ""cooldown"": 10,
        ""description"": ""Leap forward and slow enemies where you land."",
        ""shape"": ""line"",
        ""range"": 8,
        ""effect"": {
          ""damage"": 210,
          ""radius"": 3.1
        }
      },
      {
        ""id"": ""rage"",
        ""name"": ""Matinding Galit"",
        ""short"": ""GALIT"",
        ""cooldown"": 80,
        ""description"": ""Grow into a cleaving tank resistant to damage, slows and knockback."",
        ""shape"": ""circle"",
        ""range"": 3,
        ""effect"": {
          ""duration"": 5
        }
      }
    ],
    ""vfx"": ""#f7ab67"",
    ""passive"": {
      ""name"": ""Galit"",
      ""description"": ""Damage fills rage. At full rage your next attack unleashes a stunning shockwave.""
    }
  },
  {
    ""id"": ""tala"",
    ""display_name"": ""Tala ng Langit"",
    ""role"": ""Skirmisher"",
    ""color"": ""#f67c6d"",
    ""stats"": {
      ""hp"": 820,
      ""damage"": 66,
      ""armor"": 24,
      ""resist"": 20,
      ""speed"": 5.3,
      ""range"": 3.0,
      ""attack_speed"": 1.1,
      ""hit_radius"": 0.55,
      ""magic"": false
    },
    ""abilities"": [
      {
        ""id"": ""star"",
        ""name"": ""Bituin"",
        ""short"": ""STAR"",
        ""cooldown"": 6,
        ""description"": ""Fire a fan of three stars. A triple hit deals bonus damage."",
        ""shape"": ""cone"",
        ""range"": 9,
        ""effect"": {
          ""damage"": 110
        }
      },
      {
        ""id"": ""strike"",
        ""name"": ""Dash Strike"",
        ""short"": ""STRIKE"",
        ""cooldown"": 8,
        ""description"": ""Dash through enemies. Two hits or a kill reset the dash."",
        ""shape"": ""line"",
        ""range"": 5,
        ""effect"": {
          ""damage"": 252
        }
      },
      {
        ""id"": ""flight"",
        ""name"": ""Lipad!"",
        ""short"": ""LIPAD"",
        ""cooldown"": 60,
        ""description"": ""Fly over hazards and walls for 4 s, then dive at your aim point."",
        ""shape"": ""circle"",
        ""range"": 4,
        ""effect"": {
          ""duration"": 4,
          ""damage"": 378
        }
      }
    ],
    ""vfx"": ""#ffc258"",
    ""passive"": {
      ""name"": ""Lakas ng Bato"",
      ""description"": ""Landing a skill hit reduces the other cooldowns by 0.8 s.""
    }
  },
  {
    ""id"": ""kapre"",
    ""display_name"": ""Kapre"",
    ""role"": ""Ambush Tank"",
    ""color"": ""#dd8a58"",
    ""stats"": {
      ""hp"": 1150,
      ""damage"": 50,
      ""armor"": 38,
      ""resist"": 28,
      ""speed"": 4.6,
      ""range"": 2.8,
      ""attack_speed"": 0.8,
      ""hit_radius"": 0.67,
      ""magic"": false
    },
    ""abilities"": [
      {
        ""id"": ""smoke"",
        ""name"": ""Ulap ng Tabako"",
        ""short"": ""USOK"",
        ""cooldown"": 11,
        ""description"": ""Spread a smoke cloud that slows and damages enemies for 4 s."",
        ""shape"": ""circle"",
        ""range"": 4,
        ""effect"": {
          ""duration"": 4,
          ""damage"": 67,
          ""reach"": 6
        }
      },
      {
        ""id"": ""tree"",
        ""name"": ""Puno ng Balete"",
        ""short"": ""PUNO"",
        ""cooldown"": 14,
        ""description"": ""Become a hidden tree, heal, then root nearby enemies when you emerge."",
        ""shape"": ""circle"",
        ""range"": 3.5,
        ""effect"": {
          ""duration"": 2.5
        }
      },
      {
        ""id"": ""quake"",
        ""name"": ""Yanig"",
        ""short"": ""YANIG"",
        ""cooldown"": 75,
        ""description"": ""Three widening quakes damage and knock up enemies."",
        ""shape"": ""circle"",
        ""range"": 8,
        ""effect"": {
          ""damage"": 185
        }
      }
    ],
    ""vfx"": ""#b7c087"",
    ""passive"": {
      ""name"": ""Usok ng Puno"",
      ""description"": ""Stand still near cover for 1.5 s to hide and regenerate, faster in Kagubatan.""
    }
  },
  {
    ""id"": ""mana"",
    ""display_name"": ""Manananggal"",
    ""role"": ""Assassin"",
    ""color"": ""#cb7eb0"",
    ""stats"": {
      ""hp"": 700,
      ""damage"": 72,
      ""armor"": 18,
      ""resist"": 14,
      ""speed"": 5.6,
      ""range"": 2.0,
      ""attack_speed"": 1.2,
      ""hit_radius"": 0.55,
      ""magic"": false
    },
    ""abilities"": [
      {
        ""id"": ""wing"",
        ""name"": ""Pakpak Sugod"",
        ""short"": ""SUGOD"",
        ""cooldown"": 7,
        ""description"": ""Dive up to 7 m to damage and mark an enemy."",
        ""shape"": ""line"",
        ""range"": 7,
        ""effect"": {
          ""damage"": 265
        }
      },
      {
        ""id"": ""bite"",
        ""name"": ""Sipsip"",
        ""short"": ""SIPSIP"",
        ""cooldown"": 9,
        ""description"": ""Channel a biting cone for a second and heal for 60% of damage."",
        ""shape"": ""cone"",
        ""range"": 3.6,
        ""effect"": {
          ""damage"": 77
        }
      },
      {
        ""id"": ""night"",
        ""name"": ""Lipad sa Dilim"",
        ""short"": ""DILIM"",
        ""cooldown"": 70,
        ""description"": ""Fly unseen, then dive at the weakest nearby enemy."",
        ""shape"": ""circle"",
        ""range"": 12,
        ""effect"": {
          ""duration"": 3,
          ""damage"": 380
        }
      }
    ],
    ""vfx"": ""#f194cb"",
    ""passive"": {
      ""name"": ""Hating Gabi"",
      ""description"": ""Below 30% health, detach for 4 seconds of speed and lifesteal. Recharges in 45 s.""
    }
  },
  {
    ""id"": ""duwende"",
    ""display_name"": ""Duwende"",
    ""role"": ""Support"",
    ""color"": ""#f5bd56"",
    ""stats"": {
      ""hp"": 680,
      ""damage"": 40,
      ""armor"": 16,
      ""resist"": 24,
      ""speed"": 4.9,
      ""range"": 5.0,
      ""attack_speed"": 1.0,
      ""hit_radius"": 0.44,
      ""magic"": true
    },
    ""abilities"": [
      {
        ""id"": ""trap"",
        ""name"": ""Punso Bitag"",
        ""short"": ""PUNSO"",
        ""cooldown"": 10,
        ""description"": ""Place up to three hidden traps that root and reveal after arming."",
        ""shape"": ""circle"",
        ""range"": 1.9,
        ""effect"": {
          ""damage"": 118,
          ""reach"": 4
        }
      },
      {
        ""id"": ""vanish"",
        ""name"": ""Tago-Tago"",
        ""short"": ""TAGO"",
        ""cooldown"": 14,
        ""description"": ""Turn invisible and gain movement speed for 3 s. Attacking breaks it."",
        ""shape"": ""circle"",
        ""range"": 2,
        ""effect"": {
          ""duration"": 3
        }
      },
      {
        ""id"": ""charm"",
        ""name"": ""Anting ng Swerte"",
        ""short"": ""CHARM"",
        ""cooldown"": 60,
        ""description"": ""Heal and shield an ally over 4 s, cleanse them, and share half the effect."",
        ""shape"": ""circle"",
        ""range"": 9,
        ""effect"": {
          ""duration"": 4,
          ""shield"": 180
        }
      }
    ],
    ""vfx"": ""#a7f0ae"",
    ""passive"": {
      ""name"": ""Swerte"",
      ""description"": ""Basic hits have a 12% chance to drop a coin that heals and grants gold.""
    }
  }
]";
        internal const string Weapons = @"[
  {""id"":""bolo"",""name"":""Bolo"",""rarity"":0,""hits"":38,""damage"":1.15,""cleave"":0.55,""color"":""#d7e3cf""},
  {""id"":""balisong"",""name"":""Balisong"",""rarity"":0,""hits"":42,""speed"":1.35,""third_crit"":1.6,""color"":""#cbccdf""},
  {""id"":""tirador"",""name"":""Tirador"",""rarity"":0,""hits"":35,""range"":8,""damage"":0.85,""color"":""#d6ab7b""},
  {""id"":""sibat"",""name"":""Sibat"",""rarity"":1,""hits"":40,""reach_bonus"":1.5,""pierce"":true,""color"":""#84d3ef""},
  {""id"":""arnis"",""name"":""Arnis Pair"",""rarity"":1,""hits"":46,""third_stun"":0.6,""color"":""#d79a6e""},
  {""id"":""sarbakan"",""name"":""Sarbakan"",""rarity"":1,""hits"":38,""range"":7,""poison"":12,""color"":""#7accab""},
  {""id"":""palayok"",""name"":""Palayok Bomb"",""rarity"":1,""hits"":3,""range"":7,""damage"":2.20,""radius"":3,""color"":""#dd8968""},
  {""id"":""kampilan"",""name"":""Kampilan"",""rarity"":2,""hits"":52,""damage"":1.35,""cleave"":0.258819,""percent_hp"":0.05,""color"":""#f3cc77""},
  {""id"":""tabak"",""name"":""Tabak-Toyok"",""rarity"":2,""hits"":54,""attack_stack"":0.06,""max_stacks"":5,""color"":""#f3a663""},
  {""id"":""tsinelas"",""name"":""Tsinelas ng Nanay"",""rarity"":2,""hits"":18,""range"":9,""stun"":1,""cooldown"":6,""color"":""#faabcf""}
]";
        internal const string Items = @"{
  ""consumables"": [
    {""id"":""lugaw"",""name"":""Lugaw"",""duration"":4,""heal"":0.25,""color"":""#ffd694""},
    {""id"":""flower"",""name"":""Sampaguita"",""shield"":150,""color"":""#dbf2db""},
    {""id"":""boots"",""name"":""Tsinelas Boots"",""duration"":8,""speed"":0.20,""color"":""#80d8a4""},
    {""id"":""shard"",""name"":""Anting-anting Shard"",""duration"":10,""cooldown"":0.25,""color"":""#d8a3ef""}
  ],
  ""shop"": [
    {""id"":""combo"",""name"":""Lugaw Combo"",""cost"":80,""heal"":300},
    {""id"":""shoes"",""name"":""Tsinelas"",""cost"":250,""speed"":0.08},
    {""id"":""suman"",""name"":""Suman Shield"",""cost"":300,""armor"":15},
    {""id"":""whetstone"",""name"":""Bolo Whetstone"",""cost"":300,""damage"":12},
    {""id"":""charm"",""name"":""Sampaguita Charm"",""cost"":300,""resist"":15},
    {""id"":""kape"",""name"":""Kape Barako"",""cost"":350,""cooldown"":0.12},
    {""id"":""pandesal"",""name"":""Pandesal Pack"",""cost"":250,""hp"":150},
    {""id"":""revive"",""name"":""Buhay-Pa"",""cost"":900}
  ],
  ""seed_points"":[[-92,-70],[-79,-83],[-66,-63],[-76,-45],[-90,-18],[-108,-39],[-32,-90],[-59,-82],[-37,-35],[-41,-13],[-45,-27],[-22,-21],[-27,-12],[-11,-17],[-25,-38]],
  ""supply"":{""open_seconds"":3,""gold"":150,""max_live_legendary"":6},
  ""drop_reset"":78
}";
        internal const string Mobs = @"{
  ""aggro"":6,""leash"":18,""windup"":0.5,
  ""types"":[
    {""id"":""tiyanak"",""name"":""Tiyanak"",""hp"":180,""damage"":18,""xp"":25,""gold"":12,""radius"":0.48,""speed"":3.5,""respawn"":60,""color"":""#e5a086""},
    {""id"":""sigbin"",""name"":""Sigbin"",""hp"":350,""damage"":0,""xp"":60,""gold"":80,""radius"":0.55,""speed"":4.4,""respawn"":60,""color"":""#b095cb""},
    {""id"":""aswang"",""name"":""Aswang"",""hp"":700,""damage"":45,""xp"":90,""gold"":45,""radius"":0.72,""speed"":3.9,""respawn"":90,""buff"":""blood"",""color"":""#be7891""},
    {""id"":""tikbalang"",""name"":""Tikbalang"",""hp"":900,""damage"":55,""xp"":110,""gold"":55,""radius"":0.69,""speed"":3.6,""respawn"":90,""buff"":""horse"",""color"":""#c09162""},
    {""id"":""wakwak"",""name"":""Wakwak"",""hp"":450,""damage"":30,""xp"":70,""gold"":35,""radius"":0.62,""speed"":4.1,""respawn"":90,""buff"":""sight"",""color"":""#9187c0""},
    {""id"":""siyokoy"",""name"":""Siyokoy"",""hp"":800,""damage"":50,""xp"":100,""gold"":50,""radius"":0.75,""speed"":3.3,""respawn"":90,""buff"":""water"",""color"":""#6bbea2""},
    {""id"":""santelmo"",""name"":""Santelmo"",""hp"":400,""damage"":40,""xp"":80,""gold"":40,""radius"":0.55,""speed"":3.3,""respawn"":90,""buff"":""fire"",""color"":""#fa8563""},
    {""id"":""bakunawa"",""name"":""Bakunawa"",""hp"":6000,""damage"":90,""xp"":400,""gold"":300,""radius"":2.35,""speed"":2.35,""respawn"":0,""spawn"":240,""buff"":""eclipse"",""color"":""#827bb3""}
  ],
  ""camps"":[
    {""at"":[-51,-92],""pack"":3,""types"":[0,0,0,0]},
    {""at"":[-98,-36],""pack"":1,""types"":[2,3,6,2]},
    {""at"":[-80,-18],""pack"":1,""types"":[4,3,6,2]},
    {""at"":[-62,-27],""pack"":1,""types"":[5,5,5,5]},
    {""at"":[-22,-23],""pack"":1,""types"":[6,6,6,6]},
    {""at"":[-77,-51],""pack"":1,""types"":[1,1,1,1]}
  ]
}";
        internal const string Zones = @"{
  ""half_extent"": 120,
  ""base_offset"": 98,
  ""lane_half_width"": 2.6,
  ""lane_speed"": 1.1,
  ""river"": {""inner"": 63, ""outer"": 71, ""bridge_half_width"": 2.05, ""ford_half_width"": 5, ""speed"": 0.52},
  ""plaza"": {""inner"": 27, ""outer"": 47, ""gate_half_width"": 3},
  ""summit"": 24,
  ""zones"": [
    {""id"":""daan"",""name"":""Daan · Road"",""color"":""#c7ad7f"",""rarity"":[70,25,5]},
    {""id"":""palayan"",""name"":""Palayan · Rice terraces"",""color"":""#a8ab62"",""rarity"":[70,25,5]},
    {""id"":""tabing"",""name"":""Tabing-dagat · Coast"",""color"":""#eacb8b"",""rarity"":[70,25,5]},
    {""id"":""baryo"",""name"":""Baryo · Village"",""color"":""#b9a278"",""rarity"":[70,25,5]},
    {""id"":""gubat"",""name"":""Kagubatan · Forest"",""color"":""#6f9c66"",""rarity"":[50,40,10]},
    {""id"":""ilog"",""name"":""Ilog · River"",""color"":""#4aa5a1"",""rarity"":[70,25,5]},
    {""id"":""plaza"",""name"":""Palengke Plaza · Market"",""color"":""#d9ae7f"",""rarity"":[40,45,15]},
    {""id"":""bulkan"",""name"":""Bulkan · Volcano"",""color"":""#97766f"",""rarity"":[15,45,40]},
    {""id"":""base"",""name"":""Himpilan · Base"",""color"":""#c1a67c"",""rarity"":[70,25,5]}
  ],
  ""cover"": [
    [-39,-27,1.55],[-45,-22,1.4],[-39,-17,1.55],[-35,-14,1.25],
    [-29,-16,1.25],[-30,-11,1.1],[-36,-5,1.2],[-42,-7,1.4],
    [-81,-27,2],[-94,-24,2],[-101,-42,2],[-85,-47,2.1],[-72,-63,1.75],
    [-100,-64,2],[-54,-95,2],[-37,-97,1.7],[-21,-84,1.9],[-18,-102,1.9]
  ],
  ""bushes"": [[-79,-16,3],[-85,-36,2.6],[-91,-56,3.2],[-102,-12,3.2],[-41,-90,3],[-31,-86,2.5],[-50,-92,3],[-58,-45,2.5],[-40,-54,2.7],[-32,-62,2.3],[-46,-32,2.2]],
  ""lava"": [[-13,-14,2.4],[-25,-12,2.2],[-32,-9,1.6]],
  ""vents"": [[-23,-16],[-24,-7]]
}";
        internal const string Balance = @"{
  ""ticks_per_second"": 30,
  ""start_gold"": 300,
  ""passive_gold"": 2,
  ""xp_base"": 100,
  ""xp_step"": 70,
  ""kill_xp"": 120,
  ""kill_level_xp"": 20,
  ""kill_gold"": 200,
  ""assist_gold"": 80,
  ""tower_gold"": 150,
  ""tower_xp"": 105,
  ""assist_window"": 8,
  ""max_level"": 8,
  ""tower"": {
    ""hp"": 2400,
    ""damage"": 110,
    ""interval"": 1.1,
    ""range"": 9,
    ""armor"": 40
  },
  ""core"": {
    ""hp"": 5000,
    ""guards_required"": 2,
    ""armor"": 40
  },
  ""well"": {
    ""heal"": 0.08,
    ""damage"": 0.05,
    ""radius"": 4.2
  },
  ""respawn"": [
    5,
    5,
    5,
    12,
    0
  ],
  ""bagyo"": {
    ""initial_radius"": 170,
    ""start"": [
      90,
      210,
      330,
      420
    ],
    ""hold"": [
      10,
      10,
      8,
      8
    ],
    ""shrink"": [
      38,
      36,
      34,
      35
    ],
    ""radius"": [
      0.75,
      0.5,
      0.3,
      0.12
    ],
    ""damage"": [
      0.01,
      0.02,
      0.035,
      0.06
    ],
    ""center_shift"": [
      0,
      0.25,
      0.62,
      1
    ],
    ""eye_limit"": 40,
    ""tower_damage"": 0.005,
    ""core_damage"": 0.01,
    ""sudden_death"": 600,
    ""sudden_core_damage"": 0.022,
    ""limit"": 710
  },
  ""drop"": {
    ""duration"": 30,
    ""glide"": 3.0,
    ""dive"": 0.7,
    ""speed"": 12.5
  },
  ""difficulty"": {
    ""reaction_ticks"": [
      12,
      6
    ],
    ""aim_error"": [
      0.35,
      0.15
    ],
    ""siege_weight"": 1.32
  },
  ""passive_regen"": 0.006
}";
    }
}
