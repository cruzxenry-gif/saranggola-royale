using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using Saranggola.Sim;

namespace Saranggola.View
{
    public sealed partial class GameBootstrap
    {
        private RectTransform optionsPanel;
        private RectTransform endPanel;
        private Text selectedName, selectedKit, difficultyTitle, settingsText;
        private Image portrait;
        private Button[] heroesMenu;
        private bool showFps, vibration;
        private int quality;
        private float lowFpsSeconds, fps = 60;

        partial void BuildFrontEnd(ref bool created)
        {
            created=true;
            SaveOrLoadSettings(false);
            ShowLobby();
        }

        private void ShowLobby()
        {
            if(panel!=null){panel.gameObject.SetActive(false);Destroy(panel.gameObject);}
            world?.Dispose();
            world=null;match=null;paused=false;shownResults=false;
            panel=Ui.Stretch(canvas.transform,"Saranggola lobby");
            var back=panel.gameObject.AddComponent<Image>();
            back.color=PropFactory.Html("#17343a");
            back.raycastTarget=false;
            Ui.Tile(panel,"Main panel",Center,new Vector2(0,-16),new Vector2(1040,509),PropFactory.Html("#294751"));
            Ui.Label(panel,"SARANGGOLA ROYALE",42,Ui.Gold,Center,new Vector2(-11,266),new Vector2(725,60),TextAnchor.MiddleCenter);
            Ui.Label(panel,"LABAN NG BAYAN",16,Ui.Muted,Center,new Vector2(0,222),new Vector2(725,30),TextAnchor.MiddleCenter);
            Ui.Button(panel,"Settings",new Vector2(1,1),new Vector2(-96,-51),new Vector2(132,42),Ui.Rim,()=>OpenOptions(true),15);
            portrait=Ui.Tile(panel,"Hero mark",Center,new Vector2(-370,83),new Vector2(155,155),Ui.Gold,true);
            Ui.Label(portrait.transform,"✦",73,Ui.Panel,Center,Vector2.zero,new Vector2(138,120),TextAnchor.MiddleCenter);
            selectedName=Ui.Label(panel,"",29,Ui.Pale,Center,new Vector2(-51,141),new Vector2(475,46));
            selectedKit=Ui.Label(panel,"",15,Ui.Muted,Center,new Vector2(51,55),new Vector2(680,139));
            heroesMenu=new Button[data.Heroes.Length];
            for(int i=0;i<data.Heroes.Length;i++)
            {
                int pick=i;
                var info=data.Heroes[i];
                var shortName=i==0?"Juan":i==1?"Pedro":i==3?"Tala":info.DisplayName;
                heroesMenu[i]=Ui.Button(panel,shortName,Center,new Vector2(-420+i*140,-77),new Vector2(132,52),Ui.Rim,()=>ChooseHero(pick),15);
            }
            Ui.Button(panel,"Play Duos",Center,new Vector2(226,-166),new Vector2(186,51),Ui.Coral,
                ()=>StartRound(PlayerPrefs.GetInt("sr.guide",0)==0?PlayMode.Tutorial:PlayMode.Duos),19);
            Ui.Button(panel,"Play Solo",Center,new Vector2(416,-166),new Vector2(175,51),Ui.Gold,()=>StartRound(PlayMode.Solo),19);
            Ui.Button(panel,"Practice",Center,new Vector2(-422,-166),new Vector2(135,49),Ui.Rim,()=>StartRound(PlayMode.Practice),16);
            Ui.Button(panel,"Tutorial",Center,new Vector2(-276,-166),new Vector2(135,49),Ui.Rim,()=>StartRound(PlayMode.Tutorial),16);
            var diff=Ui.Button(panel,"",Center,new Vector2(-121,-166),new Vector2(152,49),Ui.Rim,()=>
            {
                difficulty=(Difficulty)(((int)difficulty+1)%3);
                difficultyTitle.text=difficulty.ToString();
                SaveOrLoadSettings(true);
            });
            difficultyTitle=Ui.Label(diff.transform,difficulty.ToString(),16,Ui.Pale,Center,Vector2.zero,new Vector2(135,40),TextAnchor.MiddleCenter);
            Ui.Label(panel,"Desktop  ·  WASD move  ·  Space attack  ·  1–3 skills  ·  F jump  ·  G interact",13,Ui.Muted,
                Center,new Vector2(0,-228),new Vector2(895,30),TextAnchor.MiddleCenter);
            ChooseHero(selected);
            BuildOptions();
        }

        private void ChooseHero(int pick)
        {
            selected=pick;
            var h=data.Heroes[pick];
            portrait.color=PropFactory.Html(h.Color);
            selectedName.text=h.DisplayName+"  ·  "+h.Role;
            var abilities=new StringBuilder().Append(h.Passive.Name).Append("  ·  ").AppendLine(h.Passive.Description);
            for(int i=0;i<3;i++)
                abilities.Append(i==2?"ULT  ":"S"+(i+1)+"  ").Append(h.Abilities[i].Name).Append("  ·  ").AppendLine(h.Abilities[i].Description);
            selectedKit.text=abilities.ToString();
            for(int i=0;i<heroesMenu.Length;i++)
                ((Image)heroesMenu[i].targetGraphic).color=i==pick?Ui.Coral:Ui.Rim;
        }

        private void SaveOrLoadSettings(bool save)
        {
            if(!save)
            {
                quality=Mathf.Clamp(PlayerPrefs.GetInt("sr.quality",1),0,2);
                leftHanded=PlayerPrefs.GetInt("sr.left",0)==1;
                aimAssist=PlayerPrefs.GetInt("sr.aim",1)==1;
                showFps=PlayerPrefs.GetInt("sr.fps",0)==1;
                vibration=PlayerPrefs.GetInt("sr.haptic",1)==1;
                joystickSize=PlayerPrefs.GetFloat("sr.size",124);
                joystickOpacity=PlayerPrefs.GetFloat("sr.opacity",.63f);
                difficulty=(Difficulty)Mathf.Clamp(PlayerPrefs.GetInt("sr.difficulty",1),0,2);
                ApplyQuality();
                return;
            }
            PlayerPrefs.SetInt("sr.quality",quality);
            PlayerPrefs.SetInt("sr.left",leftHanded?1:0);
            PlayerPrefs.SetInt("sr.aim",aimAssist?1:0);
            PlayerPrefs.SetInt("sr.fps",showFps?1:0);
            PlayerPrefs.SetInt("sr.haptic",vibration?1:0);
            PlayerPrefs.SetFloat("sr.size",joystickSize);
            PlayerPrefs.SetFloat("sr.opacity",joystickOpacity);
            PlayerPrefs.SetInt("sr.difficulty",(int)difficulty);
            PlayerPrefs.Save();
        }

        private void ApplyQuality()
        {
            string name=QualityName;
            string[] names=QualitySettings.names;
            int index=Array.IndexOf(names,name);
            if(index>=0)QualitySettings.SetQualityLevel(index,true);
            var asset=Resources.Load<UniversalRenderPipelineAsset>("Generated/"+name);
            if(asset!=null)QualitySettings.renderPipeline=asset;
            QualitySettings.vSyncCount=0;
            Application.targetFrameRate=60;
            if(world!=null)world.Sun.shadows=quality==0?LightShadows.None:LightShadows.Soft;
        }

        private string QualityName=>quality==0?"Low":quality==1?"Med":"High";

        private void BuildOptions()
        {
            optionsPanel=Ui.Tile(panel,"Settings and pause",Center,Vector2.zero,new Vector2(586,451),Ui.Panel).rectTransform;
            Ui.Label(optionsPanel,"Settings",29,Ui.Gold,Center,new Vector2(-126,186),new Vector2(230,43));
            settingsText=Ui.Label(optionsPanel,"",14,Ui.Muted,Center,new Vector2(0,133),new Vector2(535,36),TextAnchor.MiddleCenter);
            AddOption("Graphics",new Vector2(-135,79),()=>{quality=(quality+1)%3;ApplyQuality();});
            AddOption("Left-handed",new Vector2(135,79),()=>{leftHanded=!leftHanded;ConfigureStick();if(match!=null)ApplyHand();});
            AddOption("Joystick size",new Vector2(-135,24),()=>joystickSize=joystickSize>=154?103:joystickSize+17);
            AddOption("Joystick opacity",new Vector2(135,24),()=>joystickOpacity=joystickOpacity>=.88f?.32f:joystickOpacity+.19f);
            AddOption("Aim assist",new Vector2(-135,-31),()=>aimAssist=!aimAssist);
            AddOption("FPS counter",new Vector2(135,-31),()=>showFps=!showFps);
            AddOption("Haptics",new Vector2(-135,-86),()=>vibration=!vibration);
            if(match!=null)Ui.Button(optionsPanel,"Leave match",Center,new Vector2(138,-159),new Vector2(244,43),Ui.Rim,ShowLobby,16);
            Ui.Button(optionsPanel,"Close",Center,new Vector2(-138,-159),new Vector2(244,43),Ui.Coral,()=>OpenOptions(false),17);
            optionsPanel.gameObject.SetActive(false);
        }

        private void AddOption(string title,Vector2 at,Action action)
        {
            Ui.Button(optionsPanel,title,Center,at,new Vector2(244,45),Ui.Rim,()=>
            {
                action();
                if(match!=null)ConfigureStick();
                RefreshOptions();
                SaveOrLoadSettings(true);
            },16);
        }

        private void RefreshOptions()
        {
            settingsText.text=$"{QualityName}  ·  {(leftHanded?"Left":"Right")} hand  ·  Stick {joystickSize:0} / {joystickOpacity:P0}  ·  Aim {(aimAssist?"On":"Off")}  ·  FPS {(showFps?"On":"Off")}  ·  Haptics {(vibration?"On":"Off")}";
        }

        private void OpenOptions(bool open)
        {
            if(optionsPanel==null)return;
            paused=open&&match!=null;
            ClearStick();
            ClearMatchPointers();
            optionsPanel.gameObject.SetActive(open);
            if(open){RefreshOptions();optionsPanel.SetAsLastSibling();}
        }

        private void ReadGlobalKeys(Keyboard keys)
        {
            if(keys!=null&&keys.escapeKey.wasPressedThisFrame&&!shownResults&&optionsPanel!=null)
                OpenOptions(!optionsPanel.gameObject.activeSelf);
        }

        private void TickPerformance(float delta)
        {
            if(paused||shownResults||delta<=0)return;
            fps=Mathf.Lerp(fps,1f/delta,Mathf.Min(1,delta*2));
            lowFpsSeconds=fps<45?lowFpsSeconds+delta:0;
            if(lowFpsSeconds<5||quality==0)return;
            --quality;lowFpsSeconds=0;
            ApplyQuality();
            SaveOrLoadSettings(true);
        }

        partial void PresentResult()
        {
            var p=match.Player;
            bool won=match.Winner==p.Team;
            int placement=match.TeamPlacement[p.Team];
            var mvp=match.Heroes[0];
            foreach(var hero in match.Heroes)
                if(MvpScore(hero)>MvpScore(mvp))mvp=hero;
            endPanel=Ui.Tile(panel,"Match result",Center,Vector2.zero,new Vector2(867,555),Ui.Panel).rectTransform;
            Ui.Label(endPanel,won?"Panalo!":"Talo!",51,won?Ui.Gold:Ui.Coral,Center,new Vector2(0,213),new Vector2(390,78),TextAnchor.MiddleCenter);
            Ui.Label(endPanel,$"#{placement} OF {match.TeamCount}   ·   MVP {mvp.Name}   ·   {(int)match.Time/60}:{(int)match.Time%60:00}",19,Ui.Pale,
                Center,new Vector2(0,153),new Vector2(690,44),TextAnchor.MiddleCenter);
            Ui.Label(endPanel,$"{p.Kills} K / {p.Deaths} D / {p.Assists} A\n{Mathf.RoundToInt(p.DamageDealt)} damage dealt  ·  {Mathf.RoundToInt(p.DamageTaken)} taken\n{p.Mobs} mobs  ·  {p.Pickups} pickups  ·  Lv {p.Level}",17,Ui.Pale,
                Center,new Vector2(-230,48),new Vector2(357,111));
            var gold=new StringBuilder("GOLD EARNED\n");
            for(int i=0;i<(int)GoldFrom.Count;i++)
                gold.Append((GoldFrom)i).Append(' ').Append(Mathf.RoundToInt(p.Earnings[i])).Append(i%2==0?"   ·   ":"\n");
            Ui.Label(endPanel,gold.ToString(),15,Ui.Muted,Center,new Vector2(210,43),new Vector2(328,121));
            Ui.Label(endPanel,DeathRecap(p),15,Ui.Muted,Center,new Vector2(0,-100),new Vector2(739,130));
            Ui.Button(endPanel,"Play again",Center,new Vector2(112,-217),new Vector2(183,47),Ui.Coral,()=>StartRound(mode==PlayMode.Tutorial?PlayMode.Duos:mode),17);
            Ui.Button(endPanel,"Lobby",Center,new Vector2(-112,-217),new Vector2(183,47),Ui.Rim,ShowLobby,17);
            SaveResult(placement,mvp);
        }

        private static float MvpScore(Actor h)=>h.Kills*180+h.Assists*90+h.DamageDealt*.08f+h.Mobs*15-h.Deaths*25;

        private string DeathRecap(Actor hero)
        {
            if(hero.DeathTick==0)return "DEATH RECAP  ·  Survived";
            var sums=new System.Collections.Generic.Dictionary<string,float>();
            var start=Mathf.Max(0,hero.RecapHead-hero.Recap.Length);
            for(int i=start;i<hero.RecapHead;i++)
            {
                var item=hero.Recap[i%hero.Recap.Length];
                if(item.Tick<hero.DeathTick-5*Clock.Hz||item.Tick>hero.DeathTick)continue;
                var title=match.At(item.Source)?.Name??(item.Kind==DamageKind.Storm?"Bagyo":"Terrain");
                sums.TryGetValue(title,out float total);
                sums[title]=total+item.Amount;
            }
            var builder=new StringBuilder("DEATH RECAP  ·  Last 5 seconds\n");
            int amount=0;
            foreach(var item in sums)
            {
                if(amount++>=6)break;
                builder.Append(item.Key).Append("   ").Append(Mathf.RoundToInt(item.Value)).Append(" dmg   ");
            }
            if(amount==0)builder.Append("Bleed-out / finish");
            return builder.ToString();
        }

        [Serializable] private sealed class MatchLog
        {
            public string time, mode, difficulty, outcome, hash, mvp, end;
            public float seconds;
            public int seed, placement, winner, revives, stormDeaths;
            public HeroLog[] heroes;
        }

        [Serializable] private sealed class HeroLog
        {
            public string hero;
            public int team, kills, deaths, assists, level, mobs;
            public float damage, taken, gold;
            public float[] income;
        }

        private void SaveResult(int placement,Actor mvp)
        {
            if(mode==PlayMode.Practice)return;
            var result=new MatchLog
            {
                time=DateTime.UtcNow.ToString("O"), mode=mode.ToString(), difficulty=difficulty.ToString(), seconds=match.Time,
                seed=match.Seed, placement=placement, winner=match.Winner, revives=match.Revives, stormDeaths=match.StormEliminations,
                outcome=placement==1?"win":"loss", hash=match.Hash().ToString("x16"), mvp=mvp.Name,end=match.EndReason,
                heroes=new HeroLog[match.Heroes.Length]
            };
            for(int i=0;i<result.heroes.Length;i++)
            {
                var h=match.Heroes[i];
                result.heroes[i]=new HeroLog { hero=h.Name, team=h.Team,kills=h.Kills,deaths=h.Deaths,assists=h.Assists,
                    level=h.Level,mobs=h.Mobs,damage=h.DamageDealt,taken=h.DamageTaken,gold=h.Gold,income=h.Earnings };
            }
            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);
                File.AppendAllText(Path.Combine(Application.persistentDataPath,"matches.jsonl"),JsonUtility.ToJson(result)+"\n",new UTF8Encoding(false));
            }
            catch(IOException error){Debug.LogWarning("Could not save match history. "+error.Message);}
            catch(UnauthorizedAccessException error){Debug.LogWarning("Could not save match history. "+error.Message);}
        }
    }
}
