using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Saranggola.Sim;

namespace Saranggola.View
{
    public sealed partial class ArenaVisual
    {
        private MeshRenderer[] groundItems, rarityBeams;
        private MeshFilter[] itemFilters;
        private readonly MeshRenderer[] supplyKites = new MeshRenderer[4];
        private Mesh[] consumableModels;
        private Mesh crateModel, coinModel;
        private Material[] rarityPaint;
        private static readonly string[] RarityColors = { "#f3eee1", "#93e2a5", "#72ccf3", "#ffcb70" };

        partial void BuildLootViews()
        {
            groundItems = new MeshRenderer[Match.Loot.Length];
            rarityBeams = new MeshRenderer[Match.Loot.Length];
            itemFilters = new MeshFilter[Match.Loot.Length];
            rarityPaint = new Material[4];
            for (int i = 0; i < rarityPaint.Length; i++) rarityPaint[i] = Factory.Unlit(PropFactory.Html(RarityColors[i]));
            var box = PropFactory.Primitive(Shape.Box);
            var orb = PropFactory.Primitive(Shape.Sphere);
            var post = PropFactory.Primitive(Shape.Post);
            var d = new MeshDraft();
            d.Add(box, Vector3.zero, new Vector3(1.35f, .72f, .92f), PropFactory.Reed);
            d.Add(box, Vector3.up * .29f, new Vector3(1.40f, .17f, 1), PropFactory.Terracotta);
            for (int side = -1; side <= 1; side += 2)
                d.Add(box, new Vector3(side * .43f, .01f, 0), new Vector3(.13f, .81f, 1.04f), Ui.Gold);
            crateModel = d.Commit("Balikbayan box");
            coinModel = post;
            consumableModels = new Mesh[4];
            for (int i = 0; i < consumableModels.Length; i++)
            {
                d = new MeshDraft();
                var tint = PropFactory.Html(Match.Data.Items.Consumables[i].Color);
                switch (i)
                {
                    case 0:
                        d.Add(post, Vector3.zero, new Vector3(.61f, .27f, .61f), PropFactory.Terracotta);
                        d.Add(orb, Vector3.up * .15f, new Vector3(.62f,.16f,.62f), tint);
                        d.Add(box, new Vector3(.2f,.20f,0), new Vector3(.08f,.07f,.51f), PropFactory.Wood, new Vector3(0,30,28));
                        break;
                    case 1:
                        for (int petal=0;petal<5;petal++)
                            d.Add(orb, Quaternion.Euler(0,petal*72,0)*Vector3.forward*.20f, new Vector3(.25f,.14f,.25f), tint);
                        d.Add(orb,Vector3.up*.07f, Vector3.one*.18f,Ui.Gold);
                        break;
                    case 2:
                        for (int side=-1;side<=1;side+=2)
                        {
                            d.Add(orb,new Vector3(side*.19f,0,0),new Vector3(.25f,.09f,.61f),tint);
                            d.Add(box,new Vector3(side*.19f,.07f,0),new Vector3(.26f,.10f,.12f),PropFactory.Wood);
                        }
                        break;
                    default: d.Add(PropFactory.Primitive(Shape.Diamond),Vector3.zero,Vector3.one*.75f,tint,new Vector3(38,0,24)); break;
                }
                consumableModels[i] = d.Commit(Match.Data.Items.Consumables[i].Name);
                Factory.Own(consumableModels[i]);
            }
            var model = Factory.Toon ?? Factory.Flat(Ui.Gold);
            for (int i = 0; i < supplyKites.Length; i++)
            {
                supplyKites[i] = Factory.Place(Root,"Supply saranggola",kiteMesh,model,Vector3.zero,Vector3.one*.84f);
                supplyKites[i].enabled=false;
            }
            for (int i = 0; i < groundItems.Length; i++)
            {
                groundItems[i] = Factory.Place(Root, "Pickup pool", crateModel, model, Vector3.zero, Vector3.one);
                itemFilters[i] = groundItems[i].GetComponent<MeshFilter>();
                rarityBeams[i] = Factory.Place(Root, "Rarity beam pool", post, rarityPaint[0], Vector3.zero, new Vector3(.075f,1.65f,.075f));
                groundItems[i].shadowCastingMode = rarityBeams[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                groundItems[i].enabled = rarityBeams[i].enabled = false;
            }
        }

        partial void SampleLootViews(float blend,float delta)
        {
            int chute=0;
            for (int i = 0; i < Match.LootCount; i++)
            {
                var p=Match.Loot[i];
                var at=Point(p.At);
                bool show=p.Live(Match.Tick)&&(Focus-at).sqrMagnitude<43*43;
                groundItems[i].enabled=show;
                rarityBeams[i].enabled=show&&p.Kind!=LootKind.Coin;
                if (!show) continue;
                float falling=p.Kind==LootKind.Box?Mathf.Max(0,(p.Ready-Match.Tick)*Clock.Step*5):0;
                if(falling>0&&chute<supplyKites.Length)
                {
                    var dropKite=supplyKites[chute++];
                    dropKite.enabled=true;
                    dropKite.transform.SetPositionAndRotation(at+Vector3.up*(falling+2.2f),
                        Quaternion.Euler(0,Match.Time*17+i*13,9));
                }
                float phase=Match.Time*2.8f+i;
                int rarity=p.Kind==LootKind.Consumable?1:p.Rarity==2?3:p.Rarity==1?2:0;
                var mesh=p.Kind==LootKind.Weapon?Factory.ModelWeapon(Match.Data.Weapons[p.Item],p.Item):
                    p.Kind==LootKind.Consumable?consumableModels[p.Item]:p.Kind==LootKind.Coin?coinModel:crateModel;
                var renderer=groundItems[i];
                var filter=itemFilters[i];
                if(filter.sharedMesh!=mesh)filter.sharedMesh=mesh;
                renderer.transform.localScale=p.Kind==LootKind.Coin?new Vector3(.29f,.07f,.29f):Vector3.one;
                renderer.transform.SetPositionAndRotation(at+Vector3.up*(.55f+falling+Mathf.Sin(phase)*.15f),Quaternion.Euler(p.Kind==LootKind.Coin?70:0,phase*31,0));
                renderer.sharedMaterial=p.Kind==LootKind.Coin?rarityPaint[3]:Factory.WorldMaterial;
                if(rarityBeams[i].enabled)
                {
                    rarityBeams[i].sharedMaterial=rarityPaint[rarity];
                    rarityBeams[i].transform.position=at+Vector3.up*(1.55f+falling);
                }
            }
            for(int i=chute;i<supplyKites.Length;i++)supplyKites[i].enabled=false;
        }
    }

    public sealed partial class RadarGraphic
    {
        partial void AddObjectives(VertexHelper vertices)
        {
            for(int i=0;i<Match.LootCount;i++)
            {
                var p=Match.Loot[i];
                if(p.Kind==LootKind.Box&&p.Live(Match.Tick)) Dot(vertices,p.At,2.75f,Ui.Gold);
            }
            if(Match.Boss!=null&&Match.Boss.Active)Dot(vertices,Match.Boss.Position,3.2f,Ui.Coral);
            if(Match.UsesDuos&&Match.BeaconsOnline)
            {
                var soul=Match.Souls[Match.Partner(Viewer).Id];
                if(soul.Active(Match.Tick)&&soul.Holder<0)Dot(vertices,soul.Position,2.5f,Ui.Blue);
            }
        }
    }

    public sealed partial class GameBootstrap
    {
        private Image heldGear, compare;
        private Text gearText, compareText;
        private readonly Button[] quickItems=new Button[2];
        private readonly Text[] quickLabels=new Text[2];
        private readonly Text[] pickupLabels=new Text[8];
        private readonly int[] visiblePickup=new int[8];
        private int compareIndex=-1;
        private float refreshLoot;

        partial void BuildInventory()
        {
            var anchor=new Vector2(.5f,0);
            heldGear=Ui.Tile(panel,"Equipment",anchor,new Vector2(0,60),new Vector2(158,34),Ui.Panel);
            gearText=Ui.Label(heldGear.transform,"Bare hands",13,Ui.Pale,Center,Vector2.zero,new Vector2(148,29),TextAnchor.MiddleCenter);
            for(int i=0;i<2;i++)
            {
                int index=i;
                quickItems[i]=Ui.Button(panel,"",anchor,new Vector2((i==0?-1:1)*115,60),new Vector2(63,35),Ui.Rim,()=>pending.Consume=index,12);
                quickLabels[i]=Ui.Label(quickItems[i].transform,"",11,Ui.Pale,Center,Vector2.zero,new Vector2(61,29),TextAnchor.MiddleCenter);
            }
            for(int i=0;i<pickupLabels.Length;i++)
            {
                pickupLabels[i]=Ui.Label(panel,"",14,Ui.Gold,Vector2.zero,Vector2.zero,new Vector2(179,27),TextAnchor.MiddleCenter);
                visiblePickup[i]=-1;
            }
            compare=Ui.Tile(panel,"Weapon compare",Center,new Vector2(0,-101),new Vector2(324,143),Ui.Panel);
            compareText=Ui.Label(compare.transform,"",15,Ui.Pale,Center,new Vector2(0,25),new Vector2(296,78),TextAnchor.MiddleCenter);
            Ui.Button(compare.transform,"Equip",Center,new Vector2(64,-43),new Vector2(118,38),Ui.Coral,()=>{pending.Swap=compareIndex; compareIndex=-1;},16);
            Ui.Button(compare.transform,"Keep",Center,new Vector2(-64,-43),new Vector2(118,38),Ui.Rim,()=>compareIndex=-1,16);
            compareIndex=-1;refreshLoot=0;
        }

        partial void UpdateInventory(float delta)
        {
            var hero=match.Player;
            int shown=0;
            for(int i=0;i<match.LootCount&&shown<pickupLabels.Length;i++)
            {
                var p=match.Loot[i];
                if(!hero.Active||!p.Live(match.Tick)||p.Ready>match.Tick||p.Kind==LootKind.Coin||Vec2.Dist2(hero.Position,p.At)>36)continue;
                Vector3 screen=world.Camera.WorldToScreenPoint(world.Point(p.At,1.1f));
                if(screen.z<=0)continue;
                var label=pickupLabels[shown];
                label.gameObject.SetActive(true);
                if(visiblePickup[shown]!=i||Time.unscaledTime>=refreshLoot)
                {
                    label.text=p.Kind==LootKind.Box?"Balikbayan Box":p.Kind==LootKind.Consumable?data.Items.Consumables[p.Item].Name:data.Weapons[p.Item].Name;
                    label.color=p.Rarity==2?Ui.Gold:p.Rarity==1?Ui.Blue:Ui.Pale;
                    visiblePickup[shown]=i;
                }
                RectTransformUtility.ScreenPointToLocalPointInRectangle(panel,screen,null,out var local);
                label.rectTransform.anchoredPosition=local-panel.rect.min;
                shown++;
            }
            for(int i=shown;i<pickupLabels.Length;i++){pickupLabels[i].gameObject.SetActive(false);visiblePickup[i]=-1;}
            if(compareIndex>=0)
            {
                var p=match.Loot[compareIndex];
                if(!p.Live(match.Tick)||p.Kind!=LootKind.Weapon||Vec2.Dist2(hero.Position,p.At)>3.25f*3.25f)compareIndex=-1;
            }
            compare.gameObject.SetActive(compareIndex>=0);
            if(Time.unscaledTime<refreshLoot)return;
            refreshLoot=Time.unscaledTime+.12f;
            gearText.text=hero.Weapon<0?"No weapon":$"{data.Weapons[hero.Weapon].Name} · {hero.Durability}";
            for(int i=0;i<2;i++)
            {
                int slot=hero.Items[i];
                quickItems[i].interactable=slot>=0;
                quickLabels[i].text=slot<0?"—":data.Items.Consumables[slot].Name;
            }
        }

        partial void InventoryPointerDown(Vector2 p,ref bool used)
        {
            if(compare.gameObject.activeSelf&&RectTransformUtility.RectangleContainsScreenPoint(compare.rectTransform,p))
            {used=true;return;}
            for(int i=0;i<quickItems.Length;i++)
                if(RectTransformUtility.RectangleContainsScreenPoint((RectTransform)quickItems[i].transform,p)){used=true;return;}
            for(int i=0;i<pickupLabels.Length;i++)
            {
                int index=visiblePickup[i];
                if(index<0||!RectTransformUtility.RectangleContainsScreenPoint(pickupLabels[i].rectTransform,p))continue;
                var drop=match.Loot[index];
                if(drop.Kind==LootKind.Box)pending.Interact=true;
                else if(Vec2.Dist2(match.Player.Position,drop.At)<=3.25f*3.25f && drop.Kind==LootKind.Weapon && match.Player.Weapon>=0)
                {
                    compareIndex=index;
                    var newer=data.Weapons[drop.Item];
                    var older=data.Weapons[match.Player.Weapon];
                    float newReach=newer.Range>0?newer.Range:match.Player.Range+newer.ReachBonus;
                    float oldReach=older.Range>0?older.Range:match.Player.Range+older.ReachBonus;
                    compareText.text=$"{older.Name}  →  {newer.Name}\n{(newer.Damage-older.Damage)*100:+0;-0;0}% damage    {(newer.Speed-older.Speed)*100:+0;-0;0}% atk speed\n{newReach-oldReach:+0.0;-0.0;0.0} m reach    {drop.Durability} hits";
                    compare.transform.SetAsLastSibling();
                }
                else pending.Swap=index;
                used=true;return;
            }
        }
    }
}
