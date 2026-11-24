using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Saranggola.Sim;

namespace Saranggola.View
{
    public sealed partial class GameBootstrap
    {
        private readonly Image[] skill = new Image[3];
        private readonly Image[] cooldown = new Image[3];
        private readonly Text[] seconds = new Text[3];
        private readonly Image[,] ranks = new Image[3,4];
        private readonly Button[] rankUp = new Button[3];
        private Image attack, cancel, hpBar, shieldBar, xpBar;
        private Text healthLine, playerLevel;
        private int attackFinger=int.MinValue, skillFinger=int.MinValue, dragSlot=-1;
        private Vector2 dragStart, dragNow;
        private bool cancelAim;
        private bool leftHanded;
        private float joystickSize=124, joystickOpacity=.63f;
        private bool aimAssist=true;
        private float nextHud;

        partial void BuildHud()
        {
            lab.color=Ui.Pale;
            var corner=new Vector2(0,1);
            Ui.Place(lab.rectTransform,corner,new Vector2(163,-38),new Vector2(268,31));
            var stats=Ui.Tile(panel,"Hero status",corner,new Vector2(167,-68),new Vector2(300,99),Ui.Panel);
            stats.transform.SetSiblingIndex(0);
            hpBar=Ui.Tile(stats.transform,"HP",Center,new Vector2(0,-4),new Vector2(251,9),PropFactory.Team(match.Player.Team));
            hpBar.rectTransform.pivot=new Vector2(0,.5f);
            hpBar.rectTransform.anchoredPosition=new Vector2(-126,-4);
            shieldBar=Ui.Tile(stats.transform,"Shield",Center,new Vector2(0,4),new Vector2(251,3),Ui.Blue);
            shieldBar.rectTransform.pivot=new Vector2(0,.5f);
            shieldBar.rectTransform.anchoredPosition=new Vector2(-126,3);
            xpBar=Ui.Tile(stats.transform,"XP",Center,new Vector2(0,-17),new Vector2(251,3),Ui.Gold);
            xpBar.rectTransform.pivot=new Vector2(0,.5f);
            xpBar.rectTransform.anchoredPosition=new Vector2(-126,-17);
            healthLine=Ui.Label(stats.transform,"",12,Ui.Muted,Center,new Vector2(63,-31),new Vector2(127,20),TextAnchor.MiddleRight);
            playerLevel=Ui.Label(stats.transform,"",12,Ui.Gold,Center,new Vector2(-74,-31),new Vector2(107,20));
            lab.text=match.Player.Name;
            var side=new Vector2(1,0);
            attack=Ui.Tile(panel,"Attack",side,new Vector2(-93,101),new Vector2(103,103),Ui.Coral,true);
            Ui.Label(attack.transform,"ATTACK",16,Ui.Panel,Center,Vector2.zero,new Vector2(85,41),TextAnchor.MiddleCenter);
            for(int i=0;i<3;i++)
            {
                var pos=SkillPosition(i);
                skill[i]=Ui.Tile(panel,"Skill "+i,side,pos,new Vector2(81,81),Ui.Panel,true);
                var icon=Ui.Tile(skill[i].transform,"Skill symbol",Center,new Vector2(0,4),new Vector2(48,48),Ui.Pale);
                icon.sprite=SkillIcons.Get(match.Player.Costume,i);
                icon.type=Image.Type.Simple;
                var shortLabel=Ui.Label(skill[i].transform,match.Player.Profile.Abilities[i].Short,10,Ui.Gold,Center,new Vector2(0,-25),new Vector2(75,18),TextAnchor.MiddleCenter);
                shortLabel.resizeTextForBestFit=true;shortLabel.resizeTextMinSize=8;shortLabel.resizeTextMaxSize=10;
                cooldown[i]=Ui.Tile(skill[i].transform,"Radial cooldown",Center,Vector2.zero,new Vector2(79,79),new Color(.10f,.14f,.20f,.81f),true);
                cooldown[i].type=Image.Type.Filled;
                cooldown[i].fillMethod=Image.FillMethod.Radial360;
                cooldown[i].fillOrigin=(int)Image.Origin360.Top;
                seconds[i]=Ui.Label(skill[i].transform,"",25,Ui.Pale,Center,new Vector2(0,3),new Vector2(69,48),TextAnchor.MiddleCenter);
                int slot=i;
                rankUp[i]=Ui.Button(skill[i].transform,"+",Center,new Vector2(28,29),new Vector2(27,27),Ui.Gold,()=>pending.Rank=slot,21);
                for(int dot=0;dot<4;dot++)
                    ranks[i,dot]=Ui.Tile(skill[i].transform,"Rank pip",Center,new Vector2(-12+8*dot,-36),new Vector2(5,5),Ui.Muted,true);
            }
            cancel=Ui.Tile(panel,"Cancel aiming",side,new Vector2(-82,349),new Vector2(81,60),Ui.Red);
            Ui.Label(cancel.transform,"CANCEL",14,Ui.Panel,Center,Vector2.zero,new Vector2(75,40),TextAnchor.MiddleCenter);
            cancel.gameObject.SetActive(false);
            ApplyHand();
            BuildMatchExtras();
        }

        private static Vector2 SkillPosition(int slot)
        {
            if(slot==0)return new Vector2(-211,101);
            if(slot==1)return new Vector2(-172,195);
            return new Vector2(-77,231);
        }

        partial void ConfigureStick()
        {
            var size=joystickSize;
            if(joystick==null)return;
            var h=leftHanded?1:0;
            float sign=h==1?-1:1;
            Ui.Place(joystick.rectTransform,new Vector2(h,0),new Vector2(sign*(size*.5f+45),size*.5f+42),Vector2.one*size);
            thumb.rectTransform.sizeDelta=Vector2.one*size*.40f;
            joystick.color=new Color(.10f,.24f,.27f,joystickOpacity);
        }

        private void ApplyHand()
        {
            float h=leftHanded?0:1;
            float sign=leftHanded?-1:1;
            var side=new Vector2(h,0);
            Ui.Place(attack.rectTransform,side,new Vector2(-93*sign,101),new Vector2(103,103));
            for(int i=0;i<3;i++)
            {
                var p=SkillPosition(i);p.x*=sign;
                Ui.Place(skill[i].rectTransform,side,p,new Vector2(81,81));
            }
            Ui.Place(cancel.rectTransform,side,new Vector2(-82*sign,349),new Vector2(81,60));
        }

        partial void UpdateHud(float delta)
        {
            if(Time.unscaledTime>=nextHud)
            {
                nextHud=Time.unscaledTime+.085f;
                var hero=match.Player;
                hpBar.rectTransform.sizeDelta=new Vector2(251*Mathf.Clamp01(hero.HealthFraction),9);
                shieldBar.rectTransform.sizeDelta=new Vector2(251*Mathf.Clamp01(hero.Shield/hero.MaxHp),3);
                xpBar.rectTransform.sizeDelta=new Vector2(251*Mathf.Clamp01(hero.Level>=8?1:hero.Xp/match.NeedXp(hero)),3);
                healthLine.text=$"{Mathf.CeilToInt(hero.Hp)} / {Mathf.CeilToInt(hero.MaxHp)}";
                playerLevel.text=$"LV {hero.Level}   •   {Mathf.FloorToInt(hero.Gold)} g";
                for(int i=0;i<3;i++)
                {
                    float left=Mathf.Max(0,(hero.Cooldowns[i]-match.Tick)*Clock.Step);
                    bool locked=hero.Rank[i]==0;
                    cooldown[i].fillAmount=locked?1:Mathf.Clamp01(left/hero.Profile.Abilities[i].Cooldown);
                    seconds[i].text=locked?"LV 3":left>0?Mathf.CeilToInt(left).ToString():"";
                    var ready=left<=0&&!locked;
                    skill[i].color=ready?Color.Lerp(Ui.Panel,PropFactory.Html(hero.Profile.Color),.20f+.11f*Mathf.Sin(Time.unscaledTime*6)):Ui.Panel;
                    rankUp[i].gameObject.SetActive(hero.SkillPoints>0&&hero.Rank[i]<hero.SkillCap(i)&&hero.Active);
                    for(int n=0;n<4;n++)
                    {
                        ranks[i,n].enabled=i<2||n<3;
                        ranks[i,n].color=n<hero.Rank[i]?Ui.Gold:new Color(.54f,.65f,.63f,.47f);
                    }
                }
            }
            if(dragSlot>=0)
            {
                var aim=world.TranslateInput((dragNow-dragStart).normalized);
                world.Aim(match.Player,dragSlot,aim,cancelAim||(dragNow-dragStart).sqrMagnitude<100);
            }
            else world.Aim(match.Player,-1,Vec2.Zero,false);
            UpdateMatchExtras(delta);
        }

        partial void MatchPointerDown(int finger,Vector2 position,ref bool used)
        {
            if(shownResults)return;
            ExtraPointerDown(finger,position,ref used);
            if(used||!match.Player.Active)return;
            if(RectTransformUtility.RectangleContainsScreenPoint(attack.rectTransform,position))
            {
                attackFinger=finger;
                pending.Attack=true;
                used=true;
                return;
            }
            for(int i=0;i<3;i++)
            {
                if(!RectTransformUtility.RectangleContainsScreenPoint(skill[i].rectTransform,position))continue;
                used=true;
                if(rankUp[i].gameObject.activeSelf&&RectTransformUtility.RectangleContainsScreenPoint(
                    (RectTransform)rankUp[i].transform,position))return;
                if(dragSlot>=0)return;
                skillFinger=finger;
                dragSlot=i;
                dragNow=dragStart=position;
                cancelAim=false;
                cancel.gameObject.SetActive(true);
                return;
            }
            if(!RectTransformUtility.RectangleContainsScreenPoint(joystick.rectTransform,position))
                pending.Lock=world.PickTarget(position);
        }

        partial void MatchPointerMove(int finger,Vector2 position)
        {
            if(finger!=skillFinger)return;
            dragNow=position;
            cancelAim=RectTransformUtility.RectangleContainsScreenPoint(cancel.rectTransform,position);
            cancel.color=cancelAim?Ui.Coral:Ui.Red;
        }

        partial void MatchPointerUp(int finger,Vector2 position,bool canceled)
        {
            if(finger==attackFinger)attackFinger=int.MinValue;
            if(finger!=skillFinger)return;
            if(!canceled&&!RectTransformUtility.RectangleContainsScreenPoint(cancel.rectTransform,position))
            {
                pending.Ability=dragSlot;
                var delta=position-dragStart;
                if(delta.magnitude/Mathf.Max(1,canvas.scaleFactor)>15)
                    pending.Aim=Assist(world.TranslateInput(delta.normalized));
                else pending.Aim=Vec2.Zero;
            }
            dragSlot=-1;
            skillFinger=int.MinValue;
            cancel.gameObject.SetActive(false);
        }

        private Vec2 Assist(Vec2 aim)
        {
            if(!aimAssist||dragSlot<0)return aim;
            var p=match.Player;
            var ability=p.Profile.Abilities[dragSlot];
            var target=match.Target(p,ability.Range+1,UnitKind.Hero);
            if(target==null)return aim;
            var correct=(target.Position-p.Position).Normalized;
            return Vec2.Dot(correct,aim)>.89f?Vec2.Lerp(aim,correct,.36f).Normalized:aim;
        }

        partial void ClearMatchPointers()
        {
            attackFinger=skillFinger=int.MinValue;
            dragSlot=-1;
            if(cancel!=null)cancel.gameObject.SetActive(false);
            ClearExtraPointers();
        }

        partial void ReadKeyboard(Keyboard keys)
        {
            if(keys==null)return;
            var slot=keys.digit1Key.wasPressedThisFrame||keys.qKey.wasPressedThisFrame?0:
                keys.digit2Key.wasPressedThisFrame||keys.eKey.wasPressedThisFrame?1:
                keys.digit3Key.wasPressedThisFrame||keys.rKey.wasPressedThisFrame?2:-1;
            if(slot>=0)
            {
                if(keys.leftShiftKey.isPressed||keys.rightShiftKey.isPressed)pending.Rank=slot;
                else pending.Ability=slot;
            }
            if(keys.digit4Key.wasPressedThisFrame)pending.Consume=0;
            if(keys.digit5Key.wasPressedThisFrame)pending.Consume=1;
            ReadExtraKeys(keys);
        }

        partial void ReadHeld(Keyboard keys)
        {
            pending.Attack|=attackFinger!=int.MinValue||keys!=null&&keys.spaceKey.isPressed;
            ReadExtraHeld(keys);
        }

        partial void BuildMatchExtras();
        partial void UpdateMatchExtras(float delta);
        partial void ExtraPointerDown(int finger,Vector2 p,ref bool used);
        partial void ClearExtraPointers();
        partial void ReadExtraKeys(Keyboard keys);
        partial void ReadExtraHeld(Keyboard keys);
    }
}
