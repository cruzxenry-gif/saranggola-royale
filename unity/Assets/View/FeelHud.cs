using System;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Saranggola.Sim;

namespace Saranggola.View
{
    public sealed partial class GameBootstrap
    {
        private readonly Text[] unitTags=new Text[96];
        private readonly Image[] unitBars=new Image[96], unitHps=new Image[96];
        private readonly Text[] floatDamage=new Text[24];
        private readonly Vec2[] floatFrom=new Vec2[24];
        private readonly float[] floatSince=new float[24];
        private readonly Text[] arrows=new Text[6];
        private readonly StringBuilder statusText=new StringBuilder(40);
        private Text fpsText, alert, tutorialText, chatLine;
        private Image guide, warningArrow, touchPause;
        private RectTransform chatWheel;
        private Vector2 damageAngle;
        private int hudEvents, damageCursor, pendingPing=-1, tutorialStage;
        private bool guideOn, sawMove, sawSkill, sawAttack, sawLoot;
        private Vec2 pingAt;
        private float directionUntil, chatUntil, statusRefresh, lastVibe;
        private AudioSource sound, music;
        private AudioClip[] effects, ambiences;
        private int soundPhase;

        partial void BuildFeelHud()
        {
            hudEvents=damageCursor=tutorialStage=0;
            directionUntil=chatUntil=statusRefresh=0;
            pendingPing=-1;
            sawMove=sawSkill=sawAttack=sawLoot=false;
            var small=new Vector2(0,1);
            fpsText=Ui.Label(panel,"",11,Ui.Muted,new Vector2(1,0),new Vector2(-53,18),new Vector2(83,26),TextAnchor.MiddleRight);
            alert=Ui.Label(panel,"",16,Ui.Red,new Vector2(.5f,1),new Vector2(0,-147),new Vector2(437,31),TextAnchor.MiddleCenter);
            warningArrow=Ui.Tile(panel,"Damage direction",Center,new Vector2(0,0),new Vector2(31,31),Ui.Red);
            warningArrow.sprite=SkillIcons.Get(5,0);warningArrow.type=Image.Type.Simple;
            var settings=Ui.Button(panel,"Ⅱ",small,new Vector2(273,-146),new Vector2(45,43),Ui.Panel,()=>OpenOptions(true),19);
            touchPause=(Image)settings.targetGraphic;
            if(match.UsesDuos)
            {
                Ui.Button(panel,"Ping",new Vector2(1,1),new Vector2(-89,-206),new Vector2(95,36),Ui.Panel,ToggleWheel,15);
                chatWheel=Ui.Tile(panel,"Quick chat wheel",new Vector2(1,1),new Vector2(-151,-307),new Vector2(281,163),Ui.Panel).rectTransform;
                AddChat(0,"Tulong!",new Vector2(0,52));
                AddChat(1,"Dito!",new Vector2(71,0));
                AddChat(2,"Atras!",new Vector2(0,-52));
                AddChat(3,"Salamat!",new Vector2(-71,0));
                chatWheel.gameObject.SetActive(false);
            }
            else chatWheel=null;
            chatLine=Ui.Label(panel,"",16,Ui.Pale,new Vector2(.5f,1),new Vector2(0,-180),new Vector2(456,29),TextAnchor.MiddleCenter);
            for(int i=0;i<match.ActorCount;i++)
            {
                unitBars[i]=Ui.Tile(panel,"Billboard health",Vector2.zero,Vector2.zero,new Vector2(58,6),new Color(.14f,.21f,.25f,.83f));
                unitHps[i]=Ui.Tile(unitBars[i].transform,"Health fill",Center,new Vector2(-28,0),new Vector2(56,4),Ui.Blue);
                unitHps[i].rectTransform.pivot=new Vector2(0,.5f);
                unitTags[i]=Ui.Label(unitBars[i].transform,"",10,Ui.Pale,Center,new Vector2(0,13),new Vector2(139,20),TextAnchor.MiddleCenter);
                unitBars[i].gameObject.SetActive(false);
            }
            for(int i=0;i<floatDamage.Length;i++)
            {
                floatDamage[i]=Ui.Label(panel,"",18,Ui.Pale,Vector2.zero,Vector2.zero,new Vector2(114,29),TextAnchor.MiddleCenter);
                floatDamage[i].gameObject.SetActive(false);
                floatSince[i]=-100;
            }
            for(int i=0;i<arrows.Length;i++)
            {
                arrows[i]=Ui.Label(panel,"▲",18,Ui.Red,Vector2.zero,Vector2.zero,new Vector2(40,34),TextAnchor.MiddleCenter);
                arrows[i].gameObject.SetActive(false);
            }
            guide=Ui.Tile(panel,"Guided first match",new Vector2(.5f,1),new Vector2(0,-239),new Vector2(459,64),Ui.Panel);
            tutorialText=Ui.Label(guide.transform,"",15,Ui.Pale,Center,new Vector2(-36,0),new Vector2(340,51));
            Ui.Button(guide.transform,"Skip",Center,new Vector2(177,0),new Vector2(78,40),Ui.Rim,EndGuide,14);
            guideOn=mode==PlayMode.Tutorial;
            guide.gameObject.SetActive(guideOn);
            if(mode==PlayMode.Practice)
            {
                Ui.Label(panel,"PRACTICE  ·  Three passive opponents",16,Ui.Gold,new Vector2(.5f,1),new Vector2(0,-217),new Vector2(399,32),TextAnchor.MiddleCenter);
            }
            BuildAudio();
        }

        private void AddChat(int kind,string line,Vector2 at)=>Ui.Button(chatWheel,line,Center,at,new Vector2(109,41),Ui.Rim,()=>
        {
            pingAt=match.Player.Position;
            pendingPing=kind;
            chatWheel.gameObject.SetActive(false);
        },14);

        private void ToggleWheel()
        {
            if(chatWheel==null)return;
            chatWheel.gameObject.SetActive(!chatWheel.gameObject.activeSelf);
            if(chatWheel.gameObject.activeSelf)chatWheel.SetAsLastSibling();
        }

        private void ApplyExperienceInput()
        {
            if(pendingPing>=0)
            {
                match.Ping(match.Player,pingAt,pendingPing);
                pendingPing=-1;
            }
            sawMove|=pending.Move.SqrLength>.18f;
            sawAttack|=pending.Attack&&match.Player.Active;
            sawSkill|=pending.Ability>=0&&match.Player.Active;
        }

        partial void FeelPointerDown(Vector2 p,ref bool used)
        {
            if(RectTransformUtility.RectangleContainsScreenPoint((RectTransform)touchPause.transform,p)||
                chatWheel!=null&&chatWheel.gameObject.activeSelf&&RectTransformUtility.RectangleContainsScreenPoint(chatWheel,p))
            {used=true;return;}
            if(match.UsesDuos&&RectTransformUtility.RectangleContainsScreenPoint(radar.rectTransform,p))
            {
                pingAt=BattleMap.Clamp(radar.FromScreen(p));
                pendingPing=1;used=true;return;
            }
            if(guideOn&&RectTransformUtility.RectangleContainsScreenPoint(guide.rectTransform,p))used=true;
        }

        partial void ReadFeelKeys(Keyboard keys)
        {
            if(keys.tKey.wasPressedThisFrame)ToggleWheel();
            if(keys.pKey.wasPressedThisFrame&&match.UsesDuos){pingAt=match.Player.Position;pendingPing=1;}
        }

        partial void UpdateFeelHud(float delta)
        {
            bool refresh=Time.unscaledTime>=statusRefresh;
            if(refresh)
            {
                statusRefresh=Time.unscaledTime+.10f;
                fpsText.gameObject.SetActive(showFps);
                if(showFps)fpsText.text=$"{fps:0} FPS";
            }
            UpdateBillboards(refresh);
            ConsumeHudEvents();
            for(int i=0;i<floatDamage.Length;i++)
            {
                float age=Time.unscaledTime-floatSince[i];
                bool show=age<.77f&&!shownResults;
                floatDamage[i].gameObject.SetActive(show);
                if(!show)continue;
                if(ScreenPoint(world.Point(floatFrom[i],2.45f),out var pos))
                    floatDamage[i].rectTransform.anchoredPosition=pos+new Vector2((i%3-1)*13,age*47);
                else floatDamage[i].gameObject.SetActive(false);
            }
            warningArrow.gameObject.SetActive(directionUntil>Time.unscaledTime&&!shownResults);
            if(warningArrow.gameObject.activeSelf)
            {
                warningArrow.rectTransform.anchoredPosition=damageAngle*108;
                warningArrow.rectTransform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(damageAngle.y,damageAngle.x)*Mathf.Rad2Deg-90);
            }
            if(refresh)
            {
                chatLine.gameObject.SetActive(chatUntil>Time.unscaledTime);
                UpdateTutorial();
            }
            int phase=Mathf.Min(4,match.Bagyo.Phase);
            if(soundPhase!=phase)
            {
                soundPhase=phase;
                music.clip=ambiences[phase];
                music.Play();
            }
        }

        private void UpdateBillboards(bool refresh)
        {
            var p=match.Player;
            int count=0;
            bool threatened=false;
            Vector2 viewport=panel.rect.size;
            Vector2 mid=viewport*.5f;
            for(int i=0;i<match.ActorCount;i++)
            {
                var h=match.Actors[i];
                bool near=h!=p&&h.Grounded&&h.Alive&&world.Actors[i].Root.gameObject.activeSelf;
                if(!near||shownResults){unitBars[i].gameObject.SetActive(false);continue;}
                bool onscreen=ScreenPoint(world.Actors[i].Root.position+Vector3.up*(h==match.Boss?3.55f:h.Life==Life.Downed?1.3f:2.6f),out var pos);
                unitBars[i].gameObject.SetActive(near&&onscreen&&!shownResults);
                if(near&&onscreen&&!shownResults)
                {
                    unitBars[i].rectTransform.anchoredPosition=pos;
                    if(refresh)
                    {
                        unitHps[i].rectTransform.sizeDelta=new Vector2(56*Mathf.Clamp01(h.HealthFraction),4);
                        unitHps[i].color=h.Team==p.Team?Ui.Blue:h.Kind==UnitKind.Mob?Ui.Gold:Ui.Red;
                        statusText.Clear();
                        if(h.Streak>=3)statusText.Append("★ ");
                        if(h.Life==Life.Downed)statusText.Append("Bagsak");
                        else if(h.Kind==UnitKind.Hero)statusText.Append("Lv ").Append(h.Level);
                        else if(h==match.Boss)statusText.Append("Bakunawa");
                        if(h.Shield>1)statusText.Append(" ◈");
                        if(h.Has(Status.Stun,match.Tick)||h.Has(Status.Air,match.Tick))statusText.Append(" ✦");
                        if(h.Has(Status.Slow,match.Tick)||h.Has(Status.Root,match.Tick))statusText.Append(" ◆");
                        if(h.Has(Status.Burn,match.Tick)||h.Has(Status.Poison,match.Tick))statusText.Append(" •");
                        if(h.Has(Status.Silence,match.Tick))statusText.Append(" ×");
                        unitTags[i].text=statusText.ToString();
                    }
                }
                if(h.Team!=p.Team&&h.Active&&near)
                {
                    bool facing=(h.Lock==p.Id||h.MobTarget==p.Id)&&Vec2.Dist2(p.Position,h.Position)<12*12;
                    threatened|=facing;
                    if(!onscreen&&count<arrows.Length&&h.Kind==UnitKind.Hero&&match.MapVision(p,h))
                    {
                        var arrow=arrows[count++];
                        var dir=(pos-mid).normalized;
                        arrow.gameObject.SetActive(!shownResults);
                        arrow.rectTransform.anchoredPosition=new Vector2(mid.x+(mid.x-28)*dir.x,mid.y+(mid.y-37)*dir.y);
                        arrow.rectTransform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(dir.y,dir.x)*Mathf.Rad2Deg-90);
                    }
                }
            }
            for(int i=count;i<arrows.Length;i++)arrows[i].gameObject.SetActive(false);
            if(refresh)alert.text=threatened&&p.Active?"TINATARGET KA  ·  You are being targeted":"";
        }

        private bool ScreenPoint(Vector3 worldPosition,out Vector2 pos)
        {
            var point=world.Camera.WorldToScreenPoint(worldPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(panel,point,null,out var local);
            pos=local-panel.rect.min;
            return point.z>0&&panel.rect.Contains(local);
        }

        private void ConsumeHudEvents()
        {
            hudEvents=Mathf.Max(hudEvents,match.EventCount-match.Events.Length);
            for(;hudEvents<match.EventCount;hudEvents++)
            {
                var e=match.Events[hudEvents%match.Events.Length];
                var p=match.Player;
                if(e.Kind==EventKind.Ping&&match.At(e.Source)?.Team==p.Team)
                {
                    chatLine.text=e.Amount==0?"Tulong!  ·  Help!":e.Amount==1?"Dito!  ·  Here!":e.Amount==2?"Atras!  ·  Fall back!":"Salamat!  ·  Thanks!";
                    chatUntil=Time.unscaledTime+4;
                }
                if(e.Kind==EventKind.Loot&&e.Source==p.Id){sawLoot=true;sound.PlayOneShot(effects[1]);}
                if(e.Kind==EventKind.Warning&&e.Amount>=1&&e.Amount<=4)sound.PlayOneShot(effects[3],.63f);
                if(Vec2.Dist2(e.At,p.Position)>28*28)continue;
                if(e.Kind==EventKind.Ultimate){sound.PlayOneShot(effects[2],.83f);if(e.Source==p.Id||e.Target==p.Id)Vibrate();}
                if(e.Kind==EventKind.Down&&e.Target==p.Id)Vibrate();
                if(e.Kind==EventKind.Damage||e.Kind==EventKind.Hoy)
                {
                    if(e.Source!=p.Id&&e.Target!=p.Id&&!match.MapVision(p,match.At(e.Target)))continue;
                    int i=damageCursor++%floatDamage.Length;
                    floatFrom[i]=e.At;floatSince[i]=Time.unscaledTime;
                    var text=floatDamage[i];
                    text.text=e.Kind==EventKind.Hoy?"Hoy!":Mathf.RoundToInt(e.Amount).ToString();
                    text.color=e.Target==p.Id?Ui.Red:e.Source==p.Id?Ui.Gold:Ui.Pale;
                    if(e.Kind==EventKind.Damage&&(e.Source==p.Id||e.Target==p.Id))sound.PlayOneShot(effects[0],.40f);
                    var source=match.At(e.Source);
                    if(e.Target==p.Id&&source!=null)
                    {
                        var delta=PropFactory.Xz(source.Position-p.Position);
                        damageAngle=new Vector2(Vector3.Dot(delta,world.Camera.transform.right),Vector3.Dot(delta,world.Camera.transform.up)).normalized;
                        directionUntil=Time.unscaledTime+.63f;
                        if(e.Amount>175)Vibrate();
                    }
                }
            }
        }

        private void UpdateTutorial()
        {
            if(!guideOn)return;
            if(match.Time>=60){EndGuide();return;}
            var hero=match.Player;
            if(tutorialStage==0 && hero.Life!=Life.Riding)tutorialStage=1;
            if(tutorialStage==1 && sawMove&&hero.Active)tutorialStage=2;
            if(tutorialStage==2 && sawAttack)tutorialStage=3;
            if(tutorialStage==3 && sawSkill)tutorialStage=4;
            if(tutorialStage==4 && (sawLoot||match.Time>48))tutorialStage=5;
            if(match.Time>53)tutorialStage=5;
            tutorialText.text=tutorialStage==0?"Tumalon!  ·  Jump from the kite when you see a landing spot.":
                tutorialStage==1?"Galaw  ·  Use the stick to steer. Dive to touch down faster.":
                tutorialStage==2?"Laban  ·  Hold Attack when a target is in range.":
                tutorialStage==3?"Kakayahan  ·  Tap a skill to smart-cast. Drag it to aim.":
                tutorialStage==4?"Pulutin  ·  Walk onto supplies. Tap a weapon label to compare it.":"Bagyo  ·  Follow the minimap circle toward safety.";
        }

        private void EndGuide()
        {
            guideOn=false;
            guide.gameObject.SetActive(false);
            PlayerPrefs.SetInt("sr.guide",1);
            PlayerPrefs.Save();
        }

        private void Vibrate()
        {
            if(!vibration||!Application.isMobilePlatform||Time.unscaledTime-lastVibe<.45f)return;
            lastVibe=Time.unscaledTime;
            Handheld.Vibrate();
        }

        private void BuildAudio()
        {
            sound=world.Root.gameObject.AddComponent<AudioSource>();
            music=world.Root.gameObject.AddComponent<AudioSource>();
            sound.playOnAwake=music.playOnAwake=false;music.loop=true;music.volume=.19f;
            effects=new[] { Synthesize("Bolo hit",.12f,135,0), Synthesize("Loot bells",.30f,440,1),
                Synthesize("Ultimate",.61f,93,2), Synthesize("Storm signal",.63f,240,3) };
            ambiences=new AudioClip[5];
            for(int i=0;i<ambiences.Length;i++)ambiences[i]=Synthesize("Isla phase "+i,4,110+i*3,4+i);
            soundPhase=-1;
        }

        private AudioClip Synthesize(string title,float length,float tone,int kind)
        {
            const int rate=18000;
            var samples=new float[Mathf.RoundToInt(rate*length)];
            var rng=new SeededRandom(381+kind);
            float smoothed=0;
            for(int i=0;i<samples.Length;i++)
            {
                float t=i/(float)rate;
                float u=i/(float)samples.Length;
                smoothed=Mathf.Lerp(smoothed,rng.Range(-1,1),.029f);
                if(kind<4)
                {
                    float bend=kind==0?1-u*.76f:kind==1?1+u*.20f:kind==2?1-u*.51f:1;
                    float envelope=Mathf.Min(1,t*220)*(1-u)*(1-u);
                    samples[i]=envelope*(Mathf.Sin(tone*t*bend*2*Mathf.PI)*.48f+smoothed*(kind==0?.79f:.27f)+
                        (kind==0?0:Mathf.Sin(tone*1.508f*t*2*Mathf.PI)*.12f));
                }
                else
                {
                    int intensity=kind-4;
                    float pulse=Mathf.Pow(1-Mathf.Repeat(t*(1+intensity*.5f),1),4);
                    float seam=Mathf.Min(1,Mathf.Min(t,length-t)*9);
                    samples[i]=seam*(smoothed*(.28f+intensity*.043f)+.17f*pulse*Mathf.Sin((tone+(Mathf.FloorToInt(t)%2)*36)*t*2*Mathf.PI));
                }
            }
            var clip=AudioClip.Create(title,samples.Length,1,rate,false);
            clip.SetData(samples,0);
            world.Factory.Own(clip);
            return clip;
        }
    }
}
