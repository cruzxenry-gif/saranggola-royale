using UnityEngine;
using Saranggola.Sim;

namespace Saranggola.View
{
    public sealed partial class ArenaVisual
    {
        private MeshRenderer cloudWall;
        private ParticleSystem weather;
        private Transform[] wander;
        private Vector3[] homeWander;
        private float lastWeather;
        private static Color Look(string color)=>PropFactory.Html(color);

        partial void BuildExtraScenery()
        {
            var water=Surface("#4f9aa1","#d0e9d5",0);
            var lava=Surface("#b64c4f","#f5a65f",1);
            var grass=Surface("#648751","#9faf65",2);
            var paddy=Surface("#75a69c","#c4d6ac",3);
            var reeds=new MeshDraft();
            var waterways=new MeshDraft();
            var lavaStrips=new MeshDraft();
            var fields=new MeshDraft();
            var torches=new MeshDraft();
            var disk=PropFactory.Primitive(Shape.Post);
            var box=PropFactory.Primitive(Shape.Box);
            var cone=PropFactory.Primitive(Shape.Cone);
            var orb=PropFactory.Primitive(Shape.Sphere);
            var rng=new SeededRandom(830637);
            for(int i=0;i<128;i++)
            {
                float a=i*Clock.Tau/128;
                var at=Vec2.Right.Rotate(a)*55.85f;
                if(!BattleMap.Bridge(at))
                    waterways.Add(box,PropFactory.Xz(at,-.16f),new Vector3(2.93f,.06f,6.39f),Color.white,new Vector3(0,90-a*Mathf.Rad2Deg,0));
                if(i%6==0&&!BattleMap.Bridge(at))
                    waterways.Add(disk,PropFactory.Xz(at,.01f),new Vector3(.67f,.04f,.56f),Look("#86a36c"));
            }
            for(int i=0;i<10;i++)
            {
                var beach=MapBeach(62+i*3.3f);
                waterways.Add(orb,Point(beach,.005f),new Vector3(11.5f,.08f,5.2f),Look("#a4d2c9"),new Vector3(0,-39,0));
            }
            foreach(var d in Match.Map.Lava)
                lavaStrips.Add(orb,Point(d.At,.085f),new Vector3(d.Radius*1.85f,.17f,d.Radius*1.52f),Look("#f5b284"),new Vector3(0,18,0));
            foreach(var vent in Match.Map.Vents)
                torches.Add(cone,Point(vent,.47f),new Vector3(.52f,.86f,.58f),Look("#685d68"));
            for(int i=0;i<11;i++)
            {
                var center=new Vec2(48+i*4.2f,-68-i*.66f);
                fields.Add(box,Point(center,.067f),new Vector3(3.8f,.038f,10),Look("#b7c7a3"));
                for(int k=0;k<9;k++)
                    reeds.Add(cone,Point(center+new Vec2(rng.Range(-1.65f,1.65f),rng.Range(-4.8f,4.8f)),.15f),
                        new Vector3(.22f,.42f,.18f),Look("#a8bc81"));
            }
            Factory.Place(Root,"Ilog and shoreline",waterways.Commit("Water corridors"),water,Vector3.zero,Vector3.one);
            Factory.Place(Root,"Volcanic channels",lavaStrips.Commit("Lava"),lava,Vector3.zero,Vector3.one);
            Factory.Place(Root,"Reflective terraces",fields.Commit("Terrace paddies"),paddy,Vector3.zero,Vector3.one);
            Factory.Place(Root,"Wind-blown rice",reeds.Commit("Rice"),grass,Vector3.zero,Vector3.one);
            Factory.Place(Root,"Obsidian vents",torches.Commit("Vents"),Factory.WorldMaterial,Vector3.zero,Vector3.one);

            for(int i=0;i<Match.Map.Landmarks.Length;i++)
            {
                var site=Match.Map.Landmarks[i];
                var zone=BattleMap.Quadrant(site);
                var landmark=new MeshDraft();
                var p=Vector3.zero;
                if((i&1)==0)
                {
                    switch(zone)
                    {
                        case 0: Balete(landmark,p); break;
                        case 1: Watchtower(landmark,p);break;
                        case 2: Lighthouse(landmark,p);break;
                        case 3: Jeepney(landmark,p);break;
                    }
                }
                else
                {
                    switch(zone)
                    {
                        case 0: AddTree(landmark,p,1.45f);AddTree(landmark,p+Vector3.right*2.8f,1.02f);break;
                        case 1: Watchtower(landmark,p);break;
                        case 2: Bangka(landmark,p);break;
                        case 3: AddHut(landmark,p,1.4f);break;
                    }
                    if(i==1)
                    {
                        landmark.Add(box,new Vector3(1.5f,.29f,2.31f),new Vector3(2.5f,.49f,1.2f),Look("#887c71"));
                        landmark.Add(disk,new Vector3(1.5f,1.4f,2.31f),new Vector3(1.14f,2.29f,1.08f),Look("#aa9587"));
                        landmark.Add(cone,new Vector3(1.5f,2.95f,2.31f),new Vector3(2.58f,1.34f,2.47f),Look("#915e5a"));
                        landmark.Add(orb,new Vector3(1.5f,1.28f,1.71f),new Vector3(.5f,.6f,.26f),Ui.Gold);
                    }
                }
                Factory.Place(Root,"Zone landmark",landmark.Commit("Zone landmark "+i),Factory.WorldMaterial,Point(site),Vector3.one,true);
            }
            for(int q=0;q<4;q++)
            {
                var draft=new MeshDraft();
                var at=new Vec2(-17.6f,-18.9f).Quarter(q);
                var p=Point(at);
                for(int i=-1;i<=1;i+=2)
                {
                    draft.Add(disk,new Vector3(i*1.53f,1.36f,0),new Vector3(.21f,2.7f,.21f),PropFactory.Wood);
                    draft.Add(orb,new Vector3(i*1.27f,2.18f,0),Vector3.one*.28f,Ui.Gold);
                }
                draft.Add(box,new Vector3(0,2.71f,0),new Vector3(3.56f,.18f,.38f),PropFactory.Terracotta);
                for(int banner=-2;banner<=2;banner++)
                    draft.Add(box,new Vector3(banner*.43f,2.45f,0),new Vector3(.28f,.33f,.07f),
                        banner%2==0?Ui.Gold:Ui.Coral,new Vector3(0,0,banner*8));
                var gate=Factory.Place(Root,"Palengke Gate",draft.Commit("Market gate"),Factory.WorldMaterial,p,Vector3.one);
                gate.transform.rotation=Quaternion.LookRotation(PropFactory.Xz(at).normalized);
            }

            cloudWall=Factory.Place(Root,"Rotating storm cloud edge",StormCloud(),Factory.Unlit(Look("#5e637e")),Vector3.zero,Vector3.one);
            cloudWall.enabled=false;
            weather=new GameObject("Bagyo particle pool").AddComponent<ParticleSystem>();
            weather.transform.SetParent(Root,false);
            var main=weather.main;
            main.maxParticles=135;
            main.startSize=.061f;
            main.startLifetime=.88f;
            main.startSpeed=.8f;
            main.startColor=Look("#d1e2ed");
            main.simulationSpace=ParticleSystemSimulationSpace.World;
            var velocity=weather.velocityOverLifetime;
            velocity.enabled=true;velocity.x=1.8f;velocity.y=-9.5f;
            var shape=weather.shape;
            shape.shapeType=ParticleSystemShapeType.Box;
            shape.scale=new Vector3(28,.2f,25);
            var emission=weather.emission;
            emission.rateOverTime=0;
            var particles=weather.GetComponent<ParticleSystemRenderer>();
            particles.renderMode=ParticleSystemRenderMode.Stretch;
            particles.sharedMaterial=Factory.Unlit(Look("#a1b0c5"));
            particles.lengthScale=3.4f;

            wander=new Transform[4];homeWander=new Vector3[4];
            for(int i=0;i<wander.Length;i++)
            {
                var animal=new MeshDraft();
                bool carabao=i==3;
                var tint=carabao?Look("#766e70"):i==1?Look("#ab6b4b"):PropFactory.Cream;
                animal.Add(orb,new Vector3(0,carabao?.55f:.25f,0),carabao?new Vector3(1.15f,.64f,1.44f):new Vector3(.34f,.41f,.52f),tint);
                animal.Add(orb,new Vector3(0,carabao?.78f:.48f,.30f),Vector3.one*(carabao?.55f:.25f),tint);
                for(int leg=-1;leg<=1;leg+=2)for(int side=-1;side<=1;side+=2)
                    animal.Add(disk,new Vector3(side*(carabao?.35f:.08f),.14f,leg*(carabao?.49f:.12f)),new Vector3(.095f,.29f,.095f),PropFactory.Wood);
                if(carabao)
                    for(int side=-1;side<=1;side+=2)
                        animal.Add(cone,new Vector3(side*.35f,1.01f,.36f),new Vector3(.16f,.37f,.14f),Ui.Pale,new Vector3(0,0,-side*55));
                else animal.Add(cone,new Vector3(0,.5f,.44f),new Vector3(.11f,.19f,.11f),Ui.Coral,new Vector3(90,0,0));
                homeWander[i]=Point(carabao?new Vec2(68,-79):new Vec2(-77-i*2.8f,64+i*3.6f));
                wander[i]=Factory.Place(Root,carabao?"Carabao":"Village animal",animal.Commit("Wandering animal"),
                    Factory.WorldMaterial,homeWander[i],Vector3.one).transform;
            }
        }

        private static Vec2 MapBeach(float p)=>new Vec2(p,Mathf.Min(98,158-p));

        private Material Surface(string a,string b,float mode)
        {
            var shader=Shader.Find("Saranggola/Island Surface");
            if(shader==null||!shader.isSupported)return Factory.Flat(Look(a));
            var mat=new Material(shader){name="Procedural terrain surface",enableInstancing=true};
            mat.SetColor("_BaseColor",Look(a));mat.SetColor("_Accent",Look(b));mat.SetFloat("_Mode",mode);
            Factory.Own(mat);
            return mat;
        }

        private static Mesh StormCloud()
        {
            var d=new MeshDraft();
            var q=PropFactory.Primitive(Shape.Sphere);
            var color=Look("#686478");
            for(int i=0;i<96;i++)
            {
                float a=i*Clock.Tau/96;
                d.Add(q,new Vector3(Mathf.Cos(a),.23f+Mathf.Sin(a*7)*.026f,Mathf.Sin(a)),
                    new Vector3(.10f,.13f,.10f),color);
            }
            return d.Commit("Low storm cloud belt");
        }

        private static void Balete(MeshDraft d,Vector3 p)
        {
            var post=PropFactory.Primitive(Shape.Post);
            var ball=PropFactory.Primitive(Shape.Sphere);
            for(int i=0;i<8;i++)
            {
                var r=Quaternion.Euler(0,i*45,0);
                d.Add(post,p+r*new Vector3(.83f,2.9f,0),new Vector3(.38f,6.49f,.43f),PropFactory.Wood,new Vector3(0,i*45,13));
                d.Add(post,p+r*new Vector3(1.64f,1.86f,0),new Vector3(.16f,3.94f,.16f),PropFactory.Wood);
                d.Add(ball,p+r*new Vector3(1.75f,6.1f+Mathf.Cos(i*2)*.57f,0),new Vector3(3.7f,2.03f,3.38f),PropFactory.Leaf);
            }
            d.Add(ball,p+Vector3.up*6.95f,new Vector3(4.7f,2.5f,4.42f),Look("#719d6a"));
        }

        private static void Watchtower(MeshDraft d,Vector3 p)
        {
            for(int i=-1;i<=1;i+=2)for(int j=-1;j<=1;j+=2)
                d.Add(PropFactory.Primitive(Shape.Post),p+new Vector3(i*.9f,1.64f,j*.82f),new Vector3(.17f,3.32f,.17f),PropFactory.Wood);
            AddHut(d,p+Vector3.up*2.42f,1.08f);
            for(int i=0;i<10;i++)
                d.Add(PropFactory.Primitive(Shape.Box),p+new Vector3(1.04f,i*.34f,.85f+i*.09f),new Vector3(.79f,.095f,.26f),PropFactory.Reed);
        }

        private static void Lighthouse(MeshDraft d,Vector3 p)
        {
            var ring=PropFactory.Primitive(Shape.Post);
            for(int i=0;i<5;i++)
                d.Add(ring,p+Vector3.up*((i+.5f)*1.06f),new Vector3(1.73f-i*.15f,1.07f,1.73f-i*.15f),i%2==0?PropFactory.Cream:PropFactory.Terracotta);
            d.Add(ring,p+Vector3.up*5.63f,new Vector3(1.48f,.68f,1.48f),Ui.Gold);
            d.Add(PropFactory.Primitive(Shape.Cone),p+Vector3.up*6.17f,new Vector3(2.24f,.73f,2.24f),PropFactory.Terracotta);
        }

        private static void Bangka(MeshDraft d,Vector3 p)
        {
            var bar=PropFactory.Primitive(Shape.Box);
            d.Add(PropFactory.Primitive(Shape.Sphere),p+Vector3.up*.27f,new Vector3(1.14f,.59f,5.23f),PropFactory.Terracotta);
            d.Add(bar,p+Vector3.up*.57f,new Vector3(.62f,.085f,3.1f),PropFactory.Reed);
            for(int i=-1;i<=1;i+=2)
            {
                d.Add(bar,p+new Vector3(i*1.55f,.23f,0),new Vector3(.14f,.18f,4.1f),PropFactory.Reed);
                d.Add(bar,p+new Vector3(0,.46f,i*.9f),new Vector3(3.2f,.14f,.14f),PropFactory.Wood);
            }
            d.Add(bar,p+Vector3.up*1.19f,new Vector3(.065f,2.12f,.065f),PropFactory.Wood);
            d.Add(bar,p+new Vector3(.31f,1.62f,0),new Vector3(.86f,.96f,.075f),Ui.Gold,new Vector3(0,0,-13));
        }

        private static void Jeepney(MeshDraft d,Vector3 p)
        {
            var box=PropFactory.Primitive(Shape.Box);
            d.Add(box,p+Vector3.up*.8f,new Vector3(1.66f,.65f,4.48f),Look("#7da4aa"));
            d.Add(box,p+new Vector3(0,1.14f,.25f),new Vector3(1.54f,.32f,3.8f),Ui.Gold);
            d.Add(box,p+new Vector3(0,1.53f,-.36f),new Vector3(1.59f,.61f,2.92f),Look("#537883"));
            d.Add(box,p+new Vector3(0,1.92f,-.43f),new Vector3(1.67f,.18f,3.24f),Ui.Coral);
            for(int i=-1;i<=1;i+=2)for(int side=-1;side<=1;side+=2)
                d.Add(PropFactory.Primitive(Shape.Post),p+new Vector3(side*.76f,.45f,i*1.39f),new Vector3(.72f,.26f,.72f),PropFactory.Ink,new Vector3(0,0,90));
            for(int i=-1;i<=1;i+=2)
                d.Add(box,p+new Vector3(i*.56f,.83f,2.27f),new Vector3(.30f,.23f,.09f),Ui.Pale);
        }

        partial void SampleExtraScenery(float delta)
        {
            int phase=Match.Bagyo.Phase;
            for(int i=0;i<wander.Length;i++)
            {
                float age=Match.Time*(i==3?.39f:1.08f)+i*1.7f;
                wander[i].position=homeWander[i]+new Vector3(Mathf.Sin(age)*2.6f,Mathf.Abs(Mathf.Sin(age*7))*.10f,Mathf.Cos(age)*1.9f);
                wander[i].rotation=Quaternion.LookRotation(new Vector3(Mathf.Cos(age),0,-Mathf.Sin(age)));
                wander[i].gameObject.SetActive((wander[i].position-Focus).sqrMagnitude<54*54);
            }
            cloudWall.enabled=phase>=2;
            if(cloudWall.enabled)
            {
                cloudWall.transform.SetPositionAndRotation(PropFactory.Xz(Match.Bagyo.Center),Quaternion.Euler(0,Match.Time*2.05f,0));
                cloudWall.transform.localScale=new Vector3(Match.Bagyo.Radius,Mathf.Min(37,Match.Bagyo.Radius),Match.Bagyo.Radius);
            }
            if(phase>=3)weather.transform.position=Focus+Vector3.up*11;
            if(Time.unscaledTime<lastWeather)return;
            lastWeather=Time.unscaledTime+.14f;
            float haze=phase<2?0:Mathf.Clamp01((phase-1)*.31f);
            float lightning=phase>=4&&Mathf.Repeat(Match.Time+3.7f,13.6f)<.13f?1.32f:0;
            Camera.backgroundColor=Color.Lerp(Look("#a7cac4"),Look("#45495f"),haze);
            RenderSettings.fogColor=Camera.backgroundColor;
            RenderSettings.fogStartDistance=66-28*haze;
            RenderSettings.fogEndDistance=139-39*haze;
            Sun.color=Color.Lerp(Look("#fff0cf"),Look("#c5c9ea"),haze);
            Sun.intensity=1.12f-.43f*haze+lightning;
            var emission=weather.emission;
            emission.rateOverTime=phase>=4?112:phase==3?20:0;
        }
    }
}
