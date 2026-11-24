using System;
using UnityEngine;
using UnityEngine.Rendering;
using Saranggola.Sim;

namespace Saranggola.View
{
    public sealed partial class ArenaVisual : IDisposable
    {
        public readonly Match Match;
        public readonly PropFactory Factory;
        public readonly Transform Root;
        public readonly Camera Camera;
        public readonly Light Sun;
        public Vector3 Focus;
        public float Shake;
        private Vector3 smooth;
        private bool positioned;
        private readonly Color[] zoneColors = new Color[8];
        public Texture2D Minimap { get; private set; }

        public ArenaVisual(Match match, Transform owner)
        {
            Match = match;
            Factory = new PropFactory();
            Root = new GameObject("Laban ng Bayan · Isla").transform;
            Root.SetParent(owner,false);
            Camera = new GameObject("Kite camera").AddComponent<Camera>();
            Camera.transform.SetParent(Root,false);
            Camera.gameObject.tag = "MainCamera";
            Camera.gameObject.AddComponent<AudioListener>();
            Camera.fieldOfView = 45;
            Camera.nearClipPlane = .22f;
            Camera.farClipPlane = 265;
            Camera.clearFlags = CameraClearFlags.SolidColor;
            Camera.backgroundColor = PropFactory.Html("#a7cac4");
            Sun = new GameObject("Golden hour").AddComponent<Light>();
            Sun.transform.SetParent(Root,false);
            Sun.transform.rotation = Quaternion.Euler(51,-34,0);
            Sun.type = LightType.Directional;
            Sun.intensity = 1.12f;
            Sun.color = PropFactory.Html("#fff0cf");
            Sun.shadows = LightShadows.Soft;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = PropFactory.Html("#b6b6ae");
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 66;
            RenderSettings.fogEndDistance = 139;
            RenderSettings.fogColor = Camera.backgroundColor;

            for (int i=0;i<zoneColors.Length;i++) zoneColors[i] = PropFactory.Html(match.Data.Zones.Zones[i].Color);
            BuildGround();
            BuildBasics();
            MakeMinimap();
            BuildActors();
            BuildMode();
            BuildLootViews();
            BuildExtraScenery();
        }

        public static float Height(Vec2 p)
        {
            float r=p.Length;
            if (r<BattleMap.PlazaIn)
            {
                float s=1f-r/BattleMap.PlazaIn;
                return .3f+8.7f*s*s-(r<5.3f ? 3.4f*(1-r/5.3f) : 0);
            }
            if (BattleMap.River(p)&&!BattleMap.Bridge(p)) return -.34f;
            if (BattleMap.Quadrant(p)==1 && r>64) return Mathf.Clamp01((r-64)/43)*1.23f+.073f*Mathf.Sin(p.Z*.69f);
            return 0;
        }

        public Vector3 Point(Vec2 p, float lift = 0) => PropFactory.Xz(p, Height(p)+lift);

        private void BuildGround()
        {
            const int span = 25, steps = 15, width = steps + 1;
            for (int row=0; row<8; row++)
                for (int col=0; col<8; col++)
                {
                    var baseAt=new Vec2(-100+col*span,-100+row*span);
                    var vertices=new Vector3[width*width];
                    var paint=new Color32[vertices.Length];
                    var indices=new int[steps*steps*6];
                    for (int z=0;z<width;z++)
                        for (int x=0;x<width;x++)
                        {
                            var p=baseAt+new Vec2(x,z)*(span/(float)steps);
                            var zone=BattleMap.ZoneAt(p);
                            var c=zoneColors[(int)zone];
                            if (zone==Zone.Coast && p.X+p.Z>162) c=Color.Lerp(c,PropFactory.Html("#67a9a8"),Mathf.SmoothStep(0,1,(p.X+p.Z-162)/27));
                            if (zone==Zone.Fields && Mathf.Repeat(Mathf.Abs(p.Z),7)<2.6f) c=Color.Lerp(c,PropFactory.Html("#75a7a0"),.37f);
                            if (Match.Map.InLava(p)) c=PropFactory.Html("#d86c4c");
                            c *= .92f + .07f*Mathf.Sin(p.X*1.4f+p.Z*.8f) + .07f*Mathf.Cos(p.Z*.21f-p.X*.69f);
                            vertices[z*width+x]=Point(p);
                            c.a=1;
                            paint[z*width+x]=c;
                        }
                    for(int z=0;z<steps;z++)
                        for(int x=0;x<steps;x++)
                        {
                            int at=z*width+x,i=(z*steps+x)*6;
                            indices[i]=at; indices[i+1]=at+width; indices[i+2]=at+1;
                            indices[i+3]=at+1; indices[i+4]=at+width; indices[i+5]=at+width+1;
                        }
                    var mesh=new Mesh { name="Island tile", vertices=vertices, triangles=indices, colors32=paint };
                    mesh.RecalculateNormals();
                    Factory.Place(Root,"Ground",mesh,Factory.Toon ?? Factory.Flat(zoneColors[(int)BattleMap.ZoneAt(baseAt+new Vec2(12,12))]),
                        Vector3.zero,Vector3.one);
                }
            Factory.Place(Root,"Ocean",PropFactory.Primitive(Shape.Box),Factory.Flat(PropFactory.Html("#6ba2a2")),new Vector3(0,-1.58f,0),new Vector3(460,.4f,460));
        }

        private void BuildBasics()
        {
            // Draw the blocking corridors in batches with the same footprints as the simulation.
            const int batches=16;
            var draft=new MeshDraft[batches];
            foreach(var disc in Match.Map.Cover)
            {
                int x=Mathf.Clamp((int)((disc.At.X+100)/50),0,3), z=Mathf.Clamp((int)((disc.At.Z+100)/50),0,3);
                var mesh=draft[z*4+x] ?? (draft[z*4+x]=new MeshDraft());
                var zone=BattleMap.ZoneAt(disc.At);
                var at=Point(disc.At);
                float s=disc.Radius;
                if(zone==Zone.Market)
                {
                    // By combining short, staggered stall groups and carts, the central plaza stays open.
                    mesh.Add(PropFactory.Primitive(Shape.Box),at+Vector3.up*.88f,new Vector3(s*1.54f,1.73f,s*.98f),PropFactory.Wood);
                    mesh.Add(PropFactory.Primitive(Shape.Box),at+Vector3.up*1.99f,new Vector3(s*1.8f,.18f,s*1.26f),
                        (disc.At.X+disc.At.Z)%2>0 ? PropFactory.Terracotta : PropFactory.Html("#78aaa1"),new Vector3(0,disc.At.X*3,7));
                    mesh.Add(PropFactory.Primitive(Shape.Box),at+Vector3.up*.26f+Vector3.forward*s*.5f,
                        new Vector3(.72f,.56f,.61f),PropFactory.Reed);
                }
                else if(zone==Zone.Forest)
                    AddTree(mesh,at,s*1.25f);
                else if(zone==Zone.Village)
                    AddHut(mesh,at,s*.9f);
                else
                    mesh.Add(PropFactory.Primitive(Shape.Sphere),at+Vector3.up*.57f,new Vector3(s*1.85f,1.55f,s*1.85f),PropFactory.Rock,new Vector3(0,disc.At.X*9,15));
            }
            foreach(var bush in Match.Map.Bushes)
            {
                int x=Mathf.Clamp((int)((bush.At.X+100)/50),0,3), z=Mathf.Clamp((int)((bush.At.Z+100)/50),0,3);
                var mesh=draft[z*4+x] ?? (draft[z*4+x]=new MeshDraft());
                for(int i=0;i<3;i++)
                    mesh.Add(PropFactory.Primitive(Shape.Sphere),Point(bush.At)+new Vector3((i-1)*bush.Radius*.43f,.44f,0),
                        new Vector3(bush.Radius*.82f,1f,bush.Radius*.71f),Color.Lerp(PropFactory.Leaf,PropFactory.Html("#8da977"),i*.21f));
            }
            for(int i=0;i<draft.Length;i++)
                if(draft[i]!=null) Factory.Place(Root,"Cover and concealment",draft[i].Commit("Regional props"),
                    Factory.Toon ?? Factory.Flat(PropFactory.Html("#a09171")),Vector3.zero,Vector3.one);
            for(int i=1;i<8;i+=2)
            {
                var at=Match.Map.Crossings[i];
                var bridge=new MeshDraft();
                for(int row=0;row<14;row++)
                    bridge.Add(PropFactory.Primitive(Shape.Post),new Vector3(0,.11f,(row-6.5f)*.56f),new Vector3(.40f,4.14f,.39f),
                        row%3==0 ? PropFactory.Wood:PropFactory.Reed,new Vector3(0,0,90));
                for(int side=-1;side<=1;side+=2)
                {
                    bridge.Add(PropFactory.Primitive(Shape.Box),new Vector3(side*2.01f,.62f,0),new Vector3(.08f,.08f,7.9f),PropFactory.Wood);
                    for(int post=-1;post<=1;post++)
                        bridge.Add(PropFactory.Primitive(Shape.Post),new Vector3(side*2.01f,.38f,post*3.8f),new Vector3(.21f,.94f,.21f),PropFactory.Reed);
                }
                var renderer=Factory.Place(Root,"Bamboo bridge",bridge.Commit("Bridge"),Factory.Toon??Factory.Flat(PropFactory.Reed),PropFactory.Xz(at,.02f),Vector3.one);
                renderer.transform.rotation=Quaternion.LookRotation(PropFactory.Xz(at).normalized);
            }
        }

        private static void AddTree(MeshDraft mesh,Vector3 at,float scale)
        {
            mesh.Add(PropFactory.Primitive(Shape.Post),at+Vector3.up*1.46f*scale,new Vector3(.43f,3.0f,.44f)*scale,PropFactory.Wood);
            for(int i=0;i<3;i++)
                mesh.Add(PropFactory.Primitive(Shape.Sphere),at+new Vector3((i-1)*.45f,2.95f+Mathf.Sin(i*2)*.25f,0)*scale,
                    new Vector3(2.02f,1.24f,1.92f)*scale,Color.Lerp(PropFactory.Leaf,PropFactory.Html("#839f70"),i*.26f));
        }

        private static void AddHut(MeshDraft mesh,Vector3 at,float scale)
        {
            for(int i=-1;i<=1;i+=2) for(int j=-1;j<=1;j+=2)
                mesh.Add(PropFactory.Primitive(Shape.Post),at+new Vector3(i*.76f,.64f,j*.59f)*scale,new Vector3(.14f,1.24f,.14f)*scale,PropFactory.Wood);
            mesh.Add(PropFactory.Primitive(Shape.Box),at+Vector3.up*1.45f*scale,new Vector3(1.8f,1f,1.4f)*scale,PropFactory.Reed);
            for(int i=-1;i<=1;i+=2)
                mesh.Add(PropFactory.Primitive(Shape.Box),at+new Vector3(i*.52f,2.04f,0)*scale,new Vector3(1.26f,.16f,1.8f)*scale,PropFactory.Terracotta,new Vector3(0,0,i*28));
        }

        private void MakeMinimap()
        {
            const int side=96;
            var colors=new Color32[side*side];
            for(int z=0;z<side;z++) for(int x=0;x<side;x++)
            {
                var p = new Vec2(x+.5f,side-z-.5f) * (200f/side) - new Vec2(100,100);
                colors[z*side+x]=zoneColors[(int)BattleMap.ZoneAt(p)]*.83f;
            }
            Minimap=new Texture2D(side,side,TextureFormat.RGBA32,false) { name="Isla minimap",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp };
            Minimap.SetPixels32(colors);
            Minimap.Apply(false,true);
        }

        public Vec2 TranslateInput(Vector2 move)
        {
            var right=Camera.transform.right;
            var ahead=Camera.transform.forward;
            return (PropFactory.FlatPosition(right).Normalized*move.x+PropFactory.FlatPosition(ahead).Normalized*move.y).Limit();
        }

        public void Sample(float blend,float delta)
        {
            var watch=Match.Player;
            if(!watch.Alive && Match.Partner(watch) is Actor partner && partner.Alive) watch=partner;
            var p=Vec2.Lerp(watch.Previous,watch.Position,blend);
            float zoom=watch.Grounded ? 1 : 1.55f;
            Focus=Point(p+watch.Motion*2.35f,watch.Grounded ? .6f : 7f);
            smooth=positioned ? Vector3.Lerp(smooth,Focus,1-Mathf.Exp(-5.5f*delta)) : Focus;
            positioned=true;
            Camera.transform.position=smooth+new Vector3(5.5f,24,15.75f)*zoom+new Vector3(Mathf.Sin(Time.unscaledTime*73),Mathf.Cos(Time.unscaledTime*57),0)*Shake;
            Camera.transform.LookAt(smooth);
            Shake=Mathf.MoveTowards(Shake,0,delta*2.8f);
            SampleActors(blend,delta);
            SampleMode(blend,delta);
            SampleLootViews(blend,delta);
            SampleExtraScenery(delta);
        }

        public void Dispose()
        {
            Root.gameObject.SetActive(false);
            UnityEngine.Object.Destroy(Root.gameObject);
            Factory.Dispose();
            UnityEngine.Object.Destroy(Minimap);
        }

        partial void BuildActors();
        partial void SampleActors(float blend,float delta);
        partial void BuildMode();
        partial void SampleMode(float blend,float delta);
        partial void BuildLootViews();
        partial void SampleLootViews(float blend,float delta);
        partial void BuildExtraScenery();
        partial void SampleExtraScenery(float delta);
    }
}
