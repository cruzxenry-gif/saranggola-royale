# Laban ng Bayan balance report

200 seeded Normal Duos matches with 12 bots each. Seed range 1–200. Runtime standalone .NET 8.0 (Unity editor unavailable). Fixed 30 Hz simulation. Digest `99981a2b10fea22d`.

| Metric | Observed | Target |
| --- | ---: | ---: |
| Match length, median | 7:05 | 7–9 min |
| Match length, longest | 8:16 | ≤9 min |
| First fight, median | 24.95 s | By about 3:00 |
| Gap between fights, median | 39.07 s | ≤45 s |
| Storm share of all hero eliminations | 0.6% | ≤25% |
| Kills per match minute | 1.20 |  |
| Fight episodes per match | 8.66 |  |
| Fighter vs fighter TTK at level 4 | 6.10 s | 6–8 s |
| Assassin vs squishy TTK at level 4 | 2.53 s | 2–3 s |
| Completed revive channels | 128/492 (26.0%) |  |
| Hatid-Buhay returns | 12 |  |
| Last standing | 200 |  |

## Average duo placement

Weighted mean **3.51**. Target is within 0.5 places of the mean.

| Hero | Entries | Mean | Deviation |
| --- | ---: | ---: | ---: |
| Juan Dela Cruz | 342 | 3.36 | -0.15 |
| Pedro Penduko | 342 | 3.59 | 0.08 |
| Bato | 343 | 3.07 | -0.43 |
| Tala ng Langit | 344 | 3.55 | 0.04 |
| Kapre | 344 | 3.59 | 0.09 |
| Manananggal | 343 | 3.55 | 0.04 |
| Duwende | 342 | 3.84 | 0.33 |

## Progression

| Level | Median among heroes who reached it | Fastest quartile | Reach rate | Target |
| --- | ---: | ---: | ---: | ---: |
| 3 | 2:50 | 1:53 | 35.9% | ~2:00 |
| 5 | 6:08 | 4:10 | 13.4% | ~4:00 |
| 8 | 6:59 | 5:56 | 2.3% | ~7:00 |

| Gold source | Gold per hero survival minute |
| --- | ---: |
| Mobs | 22.44 |
| Kill | 24.51 |
| Assist | 5.59 |
| Passive | 113.73 |
| Pickup | 8.27 |
| Outpost | 11.51 |
| Bounty | 0.59 |
| Stolen | 81.57 |

| Camp | Clears | Mean engagement to kill |
| --- | ---: | ---: |
| Tiyanak | 1430 | 1.90 s |
| Sigbin | 1007 | 2.58 s |
| Aswang | 849 | 4.01 s |
| Tikbalang | 500 | 5.30 s |
| Wakwak | 990 | 2.74 s |
| Siyokoy | 1002 | 4.86 s |
| Santelmo | 1518 | 1.86 s |
| Bakunawa | 1 | 38.33 s |

## Measurement and gaps

- A fight episode starts with hero damage after six seconds without hero damage anywhere in the match. Camp time is per creature and starts at aggro or first hit. Gold per survival minute omits starting gold.
- TTK uses an isolated level-4 attacker with free use of legacy skills and one stationary, unarmed target at full health. Bot match outcomes include weapons, terrain, and healing.
- Level 8 remains rare. Camp rewards are preserved, respawns stop at Phase 3, and eliminated heroes end their progression. The timing for this level is based on a small sample.
- Bakunawa kills are uncommon. Its 6,000 HP and leash are preserved, and Normal bots weigh survival and storm position ahead of the crater once Phase 3 begins.
