using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Saranggola.Sim;

namespace Saranggola.View
{
    public sealed partial class GameBootstrap
    {
        private RadarGraphic radar;
        private Image jumpButton, interaction, shopToggle;
        private Text jumpText, actionText, stormText, detailText, areaText;
        private RectTransform shopPanel;
        private Button[] shopRows;
        private Text[] shopCosts;
        private bool shopShown;
        private float objectiveRefresh;

        partial void BuildMatchExtras()
        {
            shopShown=false;
            var top=new Vector2(.5f,1);
            var side=new Vector2(1,1);
            var weather=Ui.Tile(panel,"Bagyo clock",top,new Vector2(0,-56),new Vector2(270,82),Ui.Panel);
            stormText=Ui.Label(weather.transform,"",23,Ui.Pale,Center,Vector2.zero,new Vector2(256,48),TextAnchor.MiddleCenter);
            var box=Ui.Tile(panel,"Isla",side,new Vector2(-106,-118),new Vector2(177,196),Ui.Panel);
            Ui.Label(box.transform,"ISLA  ·  N",12,Ui.Gold,Center,new Vector2(0,81),new Vector2(145,18));
            var mapRect=Ui.Rect(box.transform,"Terrain radar",Center,new Vector2(0,-10),Vector2.one*151);
            var ground=mapRect.gameObject.AddComponent<RawImage>();
            ground.texture=world.Minimap;ground.raycastTarget=false;
            radar=Ui.Stretch(mapRect,"Radar marks").gameObject.AddComponent<RadarGraphic>();
            radar.raycastTarget=false;
            radar.Match=match;radar.Viewer=match.Player;
            var bottom=new Vector2(.5f,0);
            jumpButton=Ui.Tile(panel,"Fly controls",bottom,new Vector2(0,111),new Vector2(211,65),Ui.Coral);
            jumpText=Ui.Label(jumpButton.transform,"Tumalon!",24,Ui.Panel,Center,Vector2.zero,new Vector2(191,46),TextAnchor.MiddleCenter);
            interaction=Ui.Tile(panel,"Objective interaction",bottom,new Vector2(201,112),new Vector2(181,55),Ui.Panel);
            actionText=Ui.Label(interaction.transform,"",16,Ui.Gold,Center,Vector2.zero,new Vector2(164,39),TextAnchor.MiddleCenter);
            areaText=Ui.Label(panel,"",13,Ui.Pale,bottom,new Vector2(0,27),new Vector2(450,28),TextAnchor.MiddleCenter);
            detailText=Ui.Label(panel,"",17,Ui.Gold,top,new Vector2(0,-115),new Vector2(550,30),TextAnchor.MiddleCenter);
            shopToggle=Ui.Tile(panel,"Store opener",new Vector2(0,1),new Vector2(91,-150),new Vector2(151,48),Ui.Panel);
            Ui.Label(shopToggle.transform,"SARI-SARI  /  B",14,Ui.Gold,Center,Vector2.zero,new Vector2(136,34),TextAnchor.MiddleCenter);
            BuildShop();
            BuildInventory();
            BuildFeelHud();
        }

        partial void UpdateMatchExtras(float delta)
        {
            var player=match.Player;
            if(Time.unscaledTime>=objectiveRefresh)
            {
                objectiveRefresh=Time.unscaledTime+.10f;
                jumpButton.gameObject.SetActive(player.Life==Life.Riding||player.Life==Life.Gliding);
                jumpText.text=player.Life==Life.Riding?"Tumalon!  /  F":"Dive  /  F";
                var storm=match.Bagyo;
                int s=Mathf.CeilToInt(player.Life==Life.Riding?match.Data.Mode.DropSeconds-match.Time:storm.Seconds);
                stormText.text=player.Life==Life.Riding?$"SARANGGOLA  {s:00}s":storm.Sudden?$"SUDDEN DEATH   {s:00}s":
                    storm.Phase==0?$"BAGYO  ·  CALM  {s:00}s":$"BAGYO {storm.Phase}  ·  {s:00}s";
                radar.SetVerticesDirty();
                var action=player.Channel!=ChannelKind.None?player.Channel:match.GetInteraction(player,out _);
                interaction.gameObject.SetActive(action!=ChannelKind.None);
                actionText.text=ActionName(action);
                if(player.Channel!=ChannelKind.None)
                    actionText.text+=$" · {(player.ChannelUntil-match.Tick)*Clock.Step:0.0}s";
                shopToggle.gameObject.SetActive(match.NearStore(player)&&!shopShown);
                if(shopShown&&!match.NearStore(player))ToggleShop(false);
                if(shopShown)
                    for(int i=0;i<shopRows.Length;i++)
                    {
                        shopRows[i].interactable=match.CanShop(player)&&match.CanBuy(player,i)&&player.Channel==ChannelKind.None;
                        shopCosts[i].text=(i>0&&(player.Bought&(1<<i))!=0)?"Owned":$"{match.Data.Items.Shop[i].Cost:0} g";
                    }
                areaText.text=storm.Outside(player.Position)?"NASA BAGYO  ·  GET TO THE SAFE ZONE":
                    match.Data.Zones.Zones[(int)BattleMap.ZoneAt(player.Position)].Name;
                detailText.text=player.Life==Life.Downed?$"BAGSAK  ·  {(player.DownUntil-match.Tick)*Clock.Step:0}s  ·  Crawl to cover":
                    !player.Alive&&!match.Finished?"NANONOOD  ·  Spectating":player.CarriedSoul>=0?"KALULUWA  ·  Bring your partner to a Parol Beacon":"";
                attack.gameObject.SetActive(player.Active);
                for(int i=0;i<3;i++)skill[i].gameObject.SetActive(player.Active);
                joystick.gameObject.SetActive(player.Life!=Life.Riding&&player.Alive);
            }
            UpdateInventory(delta);
            UpdateFeelHud(delta);
        }

        private static string ActionName(ChannelKind kind)
        {
            switch(kind)
            {
                case ChannelKind.Revive:return "Bangon  /  G";
                case ChannelKind.Finish:return "Tapos  /  G";
                case ChannelKind.Beacon:return "Hatid-Buhay  /  G";
                case ChannelKind.Supply:return "Open box  /  G";
                case ChannelKind.Shop:return "Buying…";
                default:return "";
            }
        }

        private void BuildShop()
        {
            shopPanel=Ui.Tile(panel,"Shop panel",Center,Vector2.zero,new Vector2(464,430),Ui.Panel).rectTransform;
            Ui.Label(shopPanel,"Sari-sari Store",27,Ui.Pale,Center,new Vector2(-71,172),new Vector2(267,49));
            Ui.Button(shopPanel,"Close",Center,new Vector2(161,172),new Vector2(100,41),Ui.Rim,()=>ToggleShop(false),16);
            var list=match.Data.Items.Shop;
            shopRows=new Button[list.Length];shopCosts=new Text[list.Length];
            for(int i=0;i<list.Length;i++)
            {
                int item=i;float y=112-i*45;
                shopRows[i]=Ui.Button(shopPanel,"",Center,new Vector2(0,y),new Vector2(428,41),Ui.Rim,()=>pending.Shop=item);
                Ui.Label(shopRows[i].transform,list[i].Name,17,Ui.Pale,Center,new Vector2(-60,0),new Vector2(276,33));
                shopCosts[i]=Ui.Label(shopRows[i].transform,"",14,Ui.Gold,Center,new Vector2(147,0),new Vector2(110,33),TextAnchor.MiddleRight);
            }
            shopPanel.gameObject.SetActive(false);
        }

        private void ToggleShop(bool value)
        {
            shopShown=value;
            shopPanel.gameObject.SetActive(value);
            if(value)shopPanel.SetAsLastSibling();
        }

        partial void ExtraPointerDown(int finger,Vector2 p,ref bool used)
        {
            if(shopShown){used=true;return;}
            if(jumpButton.gameObject.activeSelf&&RectTransformUtility.RectangleContainsScreenPoint(jumpButton.rectTransform,p))
            {
                pending.Jump=match.Player.Life==Life.Riding;
                pending.Dive=match.Player.Life==Life.Gliding;
                used=true;return;
            }
            if(!match.Player.Active)return;
            if(interaction.gameObject.activeSelf&&RectTransformUtility.RectangleContainsScreenPoint(interaction.rectTransform,p))
            {pending.Interact=true;used=true;return;}
            if(shopToggle.gameObject.activeSelf&&RectTransformUtility.RectangleContainsScreenPoint(shopToggle.rectTransform,p))
            {ToggleShop(true);used=true;return;}
            InventoryPointerDown(p,ref used);
            if(!used)FeelPointerDown(p,ref used);
        }

        partial void ReadExtraKeys(Keyboard keys)
        {
            if(keys.fKey.wasPressedThisFrame)
            {
                pending.Jump=match.Player.Life==Life.Riding;
                pending.Dive=match.Player.Life==Life.Gliding;
            }
            if(keys.gKey.wasPressedThisFrame)pending.Interact=true;
            if(keys.bKey.wasPressedThisFrame&&match.NearStore(match.Player))ToggleShop(!shopShown);
            ReadFeelKeys(keys);
        }

        partial void ReadExtraHeld(Keyboard keys) { }

        partial void ClearExtraPointers() { }

        partial void BuildInventory();
        partial void UpdateInventory(float delta);
        partial void InventoryPointerDown(Vector2 p,ref bool used);
        partial void BuildFeelHud();
        partial void UpdateFeelHud(float delta);
        partial void FeelPointerDown(Vector2 p,ref bool used);
        partial void ReadFeelKeys(Keyboard keys);
    }
}
