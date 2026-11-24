using UnityEngine;
using Saranggola.Sim;

namespace Saranggola.View
{
    public sealed class HeroVisual
    {
        public Transform Root { get; }
        public Transform Hand { get; }
        private readonly PropFactory factory;
        private readonly MeshRenderer body, hull, shadow;
        private readonly MeshFilter shape, outlineShape;
        private MeshRenderer held;
        private int weapon = -2;
        private bool wasHit;
        private Quaternion smoothDirection = Quaternion.identity;
        public Actor State { get; }

        public HeroVisual(Actor actor, Transform parent, PropFactory props)
        {
            State = actor;
            factory=props;
            Root = new GameObject(actor.Kind==UnitKind.Mirror?"Salamin":actor.Name).transform;
            Root.SetParent(parent,false);
            var bodyMesh=actor.Kind==UnitKind.Mob ? props.ModelMob(actor.Costume,actor.Mob.Color) : props.ModelHero(actor.Costume,actor.Profile?.Color??"#bb8ced");
            var paint=props.Toon??props.Flat(PropFactory.Html(actor.Kind==UnitKind.Mob?actor.Mob.Color:actor.Profile?.Color??"#bb8ced"));
            body=props.Place(Root,"Body",bodyMesh,paint,Vector3.zero,Vector3.one,true);
            shape=body.GetComponent<MeshFilter>();
            if(props.Outline!=null)
            {
                hull=props.Place(Root,"Ink silhouette",bodyMesh,props.Outline,Vector3.zero,Vector3.one);
                outlineShape=hull.GetComponent<MeshFilter>();
            }
            Hand=new GameObject("Grip").transform;
            Hand.SetParent(body.transform,false);
            Hand.localPosition=new Vector3(.60f,.86f,.22f);
            shadow=props.Place(parent,"Contact shade",PropFactory.Primitive(Shape.Sphere),props.Blob,Vector3.zero,
                new Vector3(1.41f,.025f,1.11f)*(actor.Kind==UnitKind.Mob && actor.Costume==7 ? 3.2f : 1));
            Root.gameObject.SetActive(false);
            shadow.gameObject.SetActive(false);
        }

        public void SetVisible(bool visible)
        {
            if(Root.gameObject.activeSelf!=visible) Root.gameObject.SetActive(visible);
            if(shadow.gameObject.activeSelf!=visible) shadow.gameObject.SetActive(visible);
        }

        public void Sample(ArenaVisual arena,float alpha,float delta,bool freeze)
        {
            if(freeze) return;
            int tick=arena.Match.Tick;
            var a=State;
            var p=Vec2.Lerp(a.Previous,a.Position,alpha);
            float altitude=0;
            if(a.Life==Life.Gliding)
                altitude=15f*Mathf.Clamp01((a.GlideUntil-tick)/Mathf.Max(1,a.GlideUntil-a.GlideStarted));
            if(a.Has(Status.Flight,tick)) altitude+=1.8f;
            if(a.Has(Status.Air,tick)) altitude+=.46f;
            if(a.Has(Status.Detached,tick)) altitude+=.62f;
            if(a.Kind==UnitKind.Mob && (a.Costume==4||a.Costume==6)) altitude+=.63f;
            var ground=arena.Point(p,.027f);
            shadow.transform.position=ground;
            Root.position=ground+Vector3.up*altitude;
            bool moving=Vec2.Dist2(a.Position,a.Previous)>.0011f;
            float age=(tick+alpha)/30;
            float bob=Mathf.Sin(age*(moving?13.5f:4.1f)+a.Id)* (moving?.105f:.04f);
            float attack=Mathf.Sin(Mathf.Clamp01((tick+alpha-a.LastAttack)/8)*Mathf.PI)*.26f;
            var direction=PropFactory.Xz(a.Direction);
            if(direction.sqrMagnitude>.001f)
                smoothDirection=Quaternion.Slerp(smoothDirection,Quaternion.LookRotation(direction),1-Mathf.Exp(-13*delta));
            Root.rotation=smoothDirection;
            var scale=a.Has(Status.Rage,tick)?1.35f:a.Kind==UnitKind.Hero&&a.Costume==6?.86f:1;
            body.transform.localPosition=new Vector3(0,bob,attack);
            body.transform.localScale=new Vector3(scale*(1-bob*.30f),scale*(1+bob*.36f),scale);
            body.transform.localRotation=a.Life==Life.Downed?Quaternion.Euler(62,0,14):Quaternion.identity;
            if(hull!=null)
            {
                hull.transform.localPosition=body.transform.localPosition;
                hull.transform.localScale=body.transform.localScale;
                hull.transform.localRotation=body.transform.localRotation;
            }
            if(a.Kind==UnitKind.Hero&&a.Costume==4)
            {
                var mesh=a.Has(Status.Tree,tick)?factory.TreeCostume():factory.ModelHero(a.Costume,a.Profile.Color);
                if(shape.sharedMesh!=mesh)
                {
                    shape.sharedMesh=mesh;
                    if(outlineShape!=null)outlineShape.sharedMesh=mesh;
                }
            }
            var flash=tick-a.LastDamage<5;
            if(flash!=wasHit && arena.Factory.Flash!=null)
            {
                wasHit=flash;
                body.sharedMaterial=flash?arena.Factory.Flash:arena.Factory.Toon;
            }
            UpdateWeapon(arena);
        }

        private void UpdateWeapon(ArenaVisual arena)
        {
            if(State.Kind!=UnitKind.Hero || weapon==State.Weapon)return;
            weapon=State.Weapon;
            if(weapon<0)
            {
                if(held!=null)held.gameObject.SetActive(false);
                return;
            }
            var model=arena.Factory.ModelWeapon(arena.Match.Data.Weapons[weapon],weapon);
            if(held==null)
                held=factory.Place(Hand,"Visible held weapon",model,factory.Toon??factory.Flat(PropFactory.Cream),Vector3.zero,Vector3.one*.71f);
            else
            {
                held.GetComponent<MeshFilter>().sharedMesh=model;
                held.gameObject.SetActive(true);
            }
            held.transform.localRotation=Quaternion.Euler(-22,0,-24);
        }
    }

    public sealed partial class PropFactory
    {
        private static readonly Mesh[] heroMeshes = new Mesh[7];
        private static readonly Mesh[] mobMeshes = new Mesh[8];
        private static Mesh treeMesh;
        private static readonly Mesh[] weapons = new Mesh[10];

        public Mesh ModelHero(int kit,string hex)
        {
            if(heroMeshes[kit]!=null)return heroMeshes[kit];
            var draft=new MeshDraft();
            var orb=Primitive(Shape.Sphere);var box=Primitive(Shape.Box);var cone=Primitive(Shape.Cone);
            var paint=Html(hex);
            Color skin=kit==2?Html("#c9bf83"):kit==4?Html("#c38762"):kit==5?Html("#e8aeb6"):Html("#f3bd8e");
            var hair=kit==5?Html("#493351"):Html("#4c3a43");
            var width=kit==2?1.24f:kit==4?1.15f:1;
            draft.Add(Primitive(Shape.Capsule),new Vector3(0,.87f,0),new Vector3(1.12f*width,1.13f,.88f),paint);
            draft.Add(box,new Vector3(0,.54f,0),new Vector3(.92f*width,.15f,.82f),Ink);
            draft.Add(box,new Vector3(0,.56f,.44f),new Vector3(.23f,.17f,.08f),Cream);
            for(int i=-1;i<=1;i+=2)
            {
                draft.Add(orb,new Vector3(i*.55f*width,.83f,.10f),new Vector3(.43f,.51f,.45f),skin);
                draft.Add(orb,new Vector3(i*.28f,.24f,.13f),new Vector3(.52f,.42f,.65f),Ink);
            }
            draft.Add(orb,new Vector3(0,1.67f,0),new Vector3(1.42f*width,1.37f,1.25f),skin);
            draft.Add(orb,new Vector3(0,1.99f,-.13f),new Vector3(1.47f*width,.79f,1.18f),hair);
            for(int i=-1;i<=1;i+=2)
            {
                draft.Add(orb,new Vector3(i*.28f,1.70f,.555f),new Vector3(.30f,.31f,.15f),Cream);
                draft.Add(orb,new Vector3(i*.28f,1.70f,.624f),new Vector3(.17f,.20f,.115f),Ink);
                draft.Add(orb,new Vector3(i*.28f+.023f,1.74f,.675f),Vector3.one*.061f,Color.white);
                draft.Add(orb,new Vector3(i*.36f,1.47f,.54f),new Vector3(.18f,.12f,.11f),Html("#da877b"));
            }
            draft.Add(box,new Vector3(0,1.45f,.58f),new Vector3(.22f,.035f,.08f),hair);
            switch(kit)
            {
                case 0:
                    draft.Add(orb,new Vector3(0,2.02f,.22f),new Vector3(1.48f,.26f,1.04f),Html("#b4d8ab"));
                    draft.Add(box,new Vector3(.74f,.88f,.22f),new Vector3(.10f,.66f,.13f),Cream,new Vector3(19,0,-30)); break;
                case 1:
                    draft.Add(orb,new Vector3(0,2.13f,0),new Vector3(1.95f,.17f,1.56f),paint*1.17f);
                    draft.Add(cone,new Vector3(-.11f,2.42f,0),new Vector3(1.13f,.77f,1.08f),paint,new Vector3(0,0,15));
                    draft.Add(orb,new Vector3(.75f,.93f,.30f),Vector3.one*.33f,Cream);break;
                case 2:
                    draft.Add(box,new Vector3(0,2.18f,-.06f),new Vector3(.74f,.26f,.55f),hair,new Vector3(4,0,-12));
                    draft.Add(box,new Vector3(0,.96f,.47f),new Vector3(.68f,.19f,.09f),Cream,new Vector3(0,0,9)); break;
                case 3:
                    draft.Add(cone,new Vector3(0,2.20f,.34f),new Vector3(.84f,.53f,.44f),Cream);
                    draft.Add(box,new Vector3(0,.93f,-.55f),new Vector3(1.12f,1.11f,.15f),paint,new Vector3(-19,0,0)); break;
                case 4:
                    draft.Add(orb,new Vector3(0,1.38f,.42f),new Vector3(1.03f,.57f,.66f),hair);
                    for(int i=-1;i<=1;i+=2)draft.Add(cone,new Vector3(i*.68f,2.16f,-.08f),new Vector3(.42f,.73f,.43f),hair,new Vector3(0,0,-i*28)); break;
                case 5:
                    for(int i=-1;i<=1;i+=2)
                    {
                        draft.Add(cone,new Vector3(i*.59f,2.18f,-.09f),new Vector3(.43f,.60f,.42f),hair,new Vector3(0,0,-i*23));
                        draft.Add(box,new Vector3(i*.84f,1.16f,-.42f),new Vector3(.99f,.46f,.10f),paint,new Vector3(15,0,i*39));
                        draft.Add(cone,new Vector3(i*.97f,1.01f,-.44f),new Vector3(.59f,.82f,.36f),hair,new Vector3(0,0,i*126));
                    }
                    break;
                case 6:
                    draft.Add(orb,new Vector3(0,2.04f,0),new Vector3(1.52f,.20f,1.43f),paint*.77f);
                    draft.Add(cone,new Vector3(.12f,2.36f,0),new Vector3(1.33f,.83f,1.15f),paint,new Vector3(0,0,-17));
                    draft.Add(orb,new Vector3(.24f,2.75f,.04f),Vector3.one*.18f,Cream); break;
            }
            return heroMeshes[kit]=draft.Commit("Chibi "+kit);
        }

        public Mesh TreeCostume()
        {
            if(treeMesh!=null)return treeMesh;
            var d=new MeshDraft();
            d.Add(Primitive(Shape.Post),Vector3.up*.85f,new Vector3(.59f,1.74f,.59f),Wood);
            for(int i=0;i<5;i++)
                d.Add(Primitive(Shape.Sphere),Quaternion.Euler(0,i*72,0)*new Vector3(.58f,1.83f+(i%2)*.22f,0),
                    new Vector3(1.48f,1.18f,1.39f),Leaf);
            return treeMesh=d.Commit("Puno ng Balete");
        }

        public Mesh ModelMob(int kind,string hex)
        {
            if(mobMeshes[kind]!=null)return mobMeshes[kind];
            return mobMeshes[kind]=BuildMob(kind,Html(hex));
        }

        private Mesh BuildMob(int kind,Color tint)
        {
            var mesh=new MeshDraft();var orb=Primitive(Shape.Sphere);var cone=Primitive(Shape.Cone);
            if(kind==7)
            {
                for(int i=0;i<10;i++)
                {
                    var p=new Vector3(Mathf.Sin(i*.46f)*1.9f,.58f+i*.098f,Mathf.Cos(i*.46f)*1.89f);
                    mesh.Add(orb,p,new Vector3(1.28f,1.0f,1.43f)*(1-i*.027f),Color.Lerp(tint,Html("#76aaa2"),i*.065f));
                    mesh.Add(cone,p+Vector3.up*.48f,new Vector3(.43f,.71f,.36f),Cream,new Vector3(18,0,0));
                }
                mesh.Add(orb,new Vector3(0,2.05f,1.83f),new Vector3(1.66f,1.39f,1.44f),tint);
                mesh.Add(orb,new Vector3(0,1.89f,2.28f),new Vector3(1.08f,.44f,.93f),tint*.62f);
                for(int i=-1;i<=1;i+=2)
                {
                    mesh.Add(cone,new Vector3(i*.58f,2.76f,1.68f),new Vector3(.34f,1.02f,.37f),Cream,new Vector3(-21,0,-i*21));
                    mesh.Add(orb,new Vector3(i*.40f,2.17f,2.36f),new Vector3(.28f,.21f,.28f),Cream);
                }
            }
            else
            {
                float high=kind==2||kind==3||kind==5?1.13f:.85f;
                mesh.Add(orb,new Vector3(0,.59f,0),new Vector3(1.13f,high,1.02f),tint);
                mesh.Add(orb,new Vector3(0,high+.29f,.16f),new Vector3(.87f,.79f,.85f),tint*1.12f);
                for(int i=-1;i<=1;i+=2)
                {
                    mesh.Add(orb,new Vector3(i*.21f,high+.29f,.53f),new Vector3(.18f,.23f,.09f),Cream);
                    mesh.Add(orb,new Vector3(i*.21f,high+.26f,.58f),new Vector3(.09f,.13f,.05f),Ink);
                    if(kind==1||kind==2||kind==3||kind==5)
                        mesh.Add(cone,new Vector3(i*.35f,high+.70f,.08f),new Vector3(.35f,.67f,.32f),tint*.66f,new Vector3(0,0,-i*22));
                    if(kind==4||kind==6)
                        mesh.Add(cone,new Vector3(i*.64f,.61f,0),new Vector3(.63f,.87f,.48f),tint*.69f,new Vector3(0,0,i*87));
                    else mesh.Add(orb,new Vector3(i*.33f,.17f,.13f),new Vector3(.36f,.31f,.50f),tint*.65f);
                }
                if(kind==0)mesh.Add(Primitive(Shape.Box),new Vector3(0,.49f,.48f),new Vector3(.67f,.39f,.18f),Cream);
                if(kind==3)mesh.Add(orb,new Vector3(0,.96f,.56f),new Vector3(.60f,.43f,.51f),tint*.69f);
                if(kind==6)mesh.Add(cone,new Vector3(0,1.44f,.03f),new Vector3(.56f,.89f,.57f),Html("#ffbb74"),new Vector3(0,0,13));
            }
            return mesh.Commit("Folklore "+kind);
        }

        public Mesh ModelWeapon(WeaponSpec spec,int kind)
        {
            if(weapons[kind]!=null)return weapons[kind];
            var d=new MeshDraft();var box=Primitive(Shape.Box);var orb=Primitive(Shape.Sphere);var shaft=Primitive(Shape.Post);
            var tint=Html(spec.Color);
            switch(kind)
            {
                case 0: case 1: case 7:
                    float length=kind==7?1.04f:kind==1?.53f:.74f;
                    d.Add(box,new Vector3(0,length*.39f,0),new Vector3(kind==7?.29f:.20f,length,.073f),tint,new Vector3(0,0,-14));
                    d.Add(box,new Vector3(0,-.28f,0),new Vector3(.15f,.35f,.16f),Wood);
                    if(kind==7)d.Add(box,new Vector3(0,-.06f,0),new Vector3(.48f,.10f,.14f),Cream); break;
                case 2:
                    for(int i=-1;i<=1;i+=2)d.Add(box,new Vector3(i*.16f,.15f,0),new Vector3(.13f,.52f,.15f),Wood,new Vector3(0,0,-i*31));
                    d.Add(box,new Vector3(0,.39f,0),new Vector3(.55f,.04f,.06f),Cream);
                    d.Add(box,new Vector3(0,-.33f,0),new Vector3(.16f,.54f,.15f),tint);break;
                case 3:
                    d.Add(shaft,new Vector3(0,-.08f,0),new Vector3(.12f,1.53f,.12f),Wood);
                    d.Add(Primitive(Shape.Cone),new Vector3(0,.75f,0),new Vector3(.33f,.50f,.17f),tint);break;
                case 4: case 8:
                    for(int i=-1;i<=1;i+=2)
                    {
                        d.Add(shaft,new Vector3(i*.18f,0,0),new Vector3(.15f,.96f,.15f),tint,new Vector3(0,0,i*20));
                        d.Add(shaft,new Vector3(i*.27f,-.38f,0),new Vector3(.19f,.20f,.19f),Wood);
                    }
                    if(kind==8)d.Add(box,new Vector3(0,.21f,0),new Vector3(.34f,.065f,.061f),Cream);break;
                case 5:
                    d.Add(shaft,Vector3.zero,new Vector3(.24f,1.14f,.25f),tint,new Vector3(0,0,53));
                    d.Add(box,new Vector3(-.39f,.31f,0),new Vector3(.15f,.20f,.09f),Cream);break;
                case 6:
                    d.Add(orb,Vector3.zero,new Vector3(.71f,.78f,.72f),tint);
                    d.Add(shaft,new Vector3(0,.34f,0),new Vector3(.27f,.24f,.28f),Wood);
                    d.Add(box,new Vector3(.03f,.52f,0),new Vector3(.073f,.22f,.07f),Cream,new Vector3(0,0,-20));break;
                default:
                    d.Add(orb,Vector3.zero,new Vector3(.47f,.17f,.91f),tint);
                    for(int i=-1;i<=1;i+=2)d.Add(box,new Vector3(i*.10f,.09f,.10f),new Vector3(.23f,.1f,.12f),Cream,new Vector3(0,i*32,0));
                    break;
            }
            weapons[kind]=d.Commit(spec.Name);
            Own(weapons[kind]);
            return weapons[kind];
        }
    }
}
