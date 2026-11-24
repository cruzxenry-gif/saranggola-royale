using UnityEngine;
using Saranggola.Sim;

namespace Saranggola.View
{
    public sealed partial class ArenaVisual
    {
        private MeshRenderer kite, landing, boundary;
        private MeshRenderer[] parachutes, lanterns, standards, soulView;
        private readonly Material[] colorsByTeam = new Material[12];
        private Material neutralStandard, offline;
        private Mesh kiteMesh;

        partial void BuildMode()
        {
            kiteMesh=MakeKite();
            kite=Factory.Place(Root,"Saranggola carrier",kiteMesh,Factory.Toon??Factory.Flat(Ui.Coral),Vector3.zero,new Vector3(6.1f,5.1f,6.1f));
            landing=Factory.Place(Root,"Landing marker",PropFactory.Primitive(Shape.Ring),Factory.Unlit(Ui.Gold),Vector3.zero,Vector3.one*1.56f);
            boundary=Factory.Place(Root,"Bagyo boundary",PropFactory.Primitive(Shape.Ring),Factory.Unlit(PropFactory.Html("#75bdd3")),Vector3.zero,Vector3.one);
            parachutes=new MeshRenderer[Match.Heroes.Length];
            soulView=new MeshRenderer[Match.Heroes.Length];
            for(int i=0;i<parachutes.Length;i++)
            {
                parachutes[i]=Factory.Place(Root,"Glide kite",kiteMesh,Factory.Toon??Factory.Flat(Ui.Coral),Vector3.zero,Vector3.one*1.27f);
                soulView[i]=Factory.Place(Root,"Kaluluwa",PropFactory.Primitive(Shape.Sphere),Factory.Unlit(PropFactory.Html("#99def1")),Vector3.zero,Vector3.one*.48f);
            }
            neutralStandard=Factory.Unlit(PropFactory.Html("#bfc9ac"));
            offline=Factory.Unlit(PropFactory.Html("#7e7e84"));
            for(int i=0;i<colorsByTeam.Length;i++)colorsByTeam[i]=Factory.Unlit(PropFactory.Team(i));
            standards=new MeshRenderer[Match.Outposts.Length];
            var basin=PropFactory.Primitive(Shape.Post);
            for(int i=0;i<standards.Length;i++)
            {
                var at=Point(Match.Outposts[i].Position);
                var d=new MeshDraft();
                d.Add(basin,Vector3.up*.11f,new Vector3(2.15f,.22f,2.15f),PropFactory.Rock);
                d.Add(basin,Vector3.up*1.09f,new Vector3(.18f,2.04f,.18f),PropFactory.Wood);
                d.Add(PropFactory.Primitive(Shape.Box),new Vector3(.29f,1.98f,0),new Vector3(.76f,.068f,.087f),PropFactory.Wood);
                Factory.Place(Root,"Bantayan platform",d.Commit("Bantayan"),Factory.Toon??Factory.Flat(PropFactory.Reed),at,Vector3.one);
                standards[i]=Factory.Place(Root,"Neutral standard",PropFactory.Primitive(Shape.Box),neutralStandard,at+new Vector3(.61f,1.76f,0),new Vector3(.62f,.50f,.075f));
            }
            lanterns=new MeshRenderer[Match.Map.Beacons.Length];
            for(int i=0;i<lanterns.Length;i++)
            {
                var at=Point(Match.Map.Beacons[i]);
                var d=new MeshDraft();
                d.Add(basin,Vector3.up*.12f,new Vector3(1.61f,.23f,1.61f),PropFactory.Reed);
                d.Add(basin,Vector3.up*.95f,new Vector3(.17f,1.79f,.17f),PropFactory.Wood);
                Factory.Place(Root,"Parol Beacon",d.Commit("Beacon"),Factory.Toon??Factory.Flat(PropFactory.Reed),at,Vector3.one);
                lanterns[i]=Factory.Place(Root,"Parol star",PropFactory.Primitive(Shape.Diamond),Factory.Unlit(Ui.Gold),at+Vector3.up*2.0f,new Vector3(.80f,.80f,.80f));
                lanterns[i].transform.localRotation=Quaternion.Euler(78,0,0);
            }
            foreach(var store in Match.Map.Stores)
            {
                var d=new MeshDraft();
                var box=PropFactory.Primitive(Shape.Box);
                for(int i=-1;i<=1;i+=2)for(int j=-1;j<=1;j+=2)
                    d.Add(basin,new Vector3(i*.70f,1.06f,j*.57f),new Vector3(.10f,2.12f,.10f),PropFactory.Wood);
                d.Add(box,new Vector3(0,.74f,0),new Vector3(1.64f,.81f,1.30f),PropFactory.Reed);
                for(int stripe=0;stripe<6;stripe++)
                    d.Add(box,new Vector3((stripe-2.5f)*.35f,2.16f,0),new Vector3(.36f,.13f,1.67f),
                        stripe%2==0?PropFactory.Html("#6fa89b"):PropFactory.Cream,new Vector3(8,0,0));
                Factory.Place(Root,"Sari-sari Store",d.Commit("Neutral store"),Factory.Toon??Factory.Flat(Ui.Coral),Point(store),Vector3.one);
            }
        }

        private static Mesh MakeKite()
        {
            const int ribbons=13;
            var v=new Vector3[12+ribbons*3];
            var t=new int[v.Length];
            var c=new Color32[v.Length];
            var points=new[]{new Vector3(0,0,-.86f),new Vector3(-.75f,.09f,-.09f),new Vector3(0,0,1.0f),new Vector3(.75f,.09f,-.09f)};
            var colors=new[]{PropFactory.Html("#f5d084"),PropFactory.Html("#dc7562"),PropFactory.Html("#be5662"),PropFactory.Html("#eaa275")};
            for(int i=0;i<4;i++)
            {
                v[3*i]=points[i];v[3*i+1]=Vector3.up*.024f;v[3*i+2]=points[(i+1)%4];
                c[3*i]=c[3*i+1]=c[3*i+2]=colors[i];
            }
            for(int i=0;i<ribbons;i++)
            {
                int n=12+i*3;float z=1.01f+i*.16f;
                float side=Mathf.Sin(i*.72f)*.031f*i;
                v[n]=new Vector3(side-.04f,.01f,z);v[n+1]=new Vector3(side+.09f,.01f,z);v[n+2]=new Vector3(Mathf.Sin((i+1)*.72f)*.031f*(i+1),.01f,z+.21f);
                c[n]=c[n+1]=c[n+2]=i%2==0?Ui.Gold:Ui.Pale;
            }
            for(int i=0;i<t.Length;i++)t[i]=i;
            var indices=new int[t.Length*2];
            for(int i=0;i<t.Length;i+=3){indices[i]=t[i];indices[i+1]=t[i+1];indices[i+2]=t[i+2];
                indices[i+t.Length]=t[i];indices[i+1+t.Length]=t[i+2];indices[i+2+t.Length]=t[i+1];}
            var mesh=new Mesh{name="Cloth diamond and ribbon",vertices=v,colors32=c,triangles=indices};
            mesh.RecalculateNormals();
            return mesh;
        }

        partial void SampleMode(float blend,float delta)
        {
            kite.enabled=Match.KiteFlying;
            if(kite.enabled)
            {
                kite.transform.position=Point(Match.KitePosition,15.5f+Mathf.Sin(Match.Time*2.7f)*.47f);
                kite.transform.rotation=Quaternion.LookRotation(-PropFactory.Xz(Match.KiteDirection));
            }
            var user=Match.Player;
            landing.enabled=user.Life==Life.Gliding;
            if(landing.enabled)
            {
                var aim=Match.Map.Safe(user.Position+(user.Motion+Match.KiteDirection*.29f).Limit()*Mathf.Max(0,(user.GlideUntil-Match.Tick)*Clock.Step)*Match.Data.Mode.GlideSpeed);
                landing.transform.position=Point(aim,.17f);
            }
            boundary.enabled=Match.Bagyo.Phase>0;
            if(boundary.enabled)
            {
                boundary.transform.position=PropFactory.Xz(Match.Bagyo.Center,.13f);
                boundary.transform.localScale=Vector3.one*Match.Bagyo.Radius;
            }
            for(int i=0;i<parachutes.Length;i++)
            {
                var actor=Match.Heroes[i];
                bool visible=actor.Life==Life.Gliding&&Actors[i].Root.gameObject.activeSelf;
                parachutes[i].enabled=visible;
                if(visible)
                {
                    parachutes[i].transform.position=Actors[i].Root.position+Vector3.up*3.3f;
                    parachutes[i].transform.rotation=Quaternion.LookRotation(-PropFactory.Xz(actor.Direction));
                }
                var soul=Match.Souls[i];
                soulView[i].enabled=Match.BeaconsOnline&&soul.Active(Match.Tick)&&(Focus-Point(soul.Position)).sqrMagnitude<63*63;
                if(soulView[i].enabled)soulView[i].transform.position=Point(soul.Position,(soul.Holder>=0?2.7f:1f)+Mathf.Sin(Match.Time*3.6f+i)*.18f);
            }
            for(int i=0;i<standards.Length;i++)
            {
                var owner=Match.Outposts[i].Owner;
                standards[i].sharedMaterial=owner<0?neutralStandard:colorsByTeam[owner];
                standards[i].transform.rotation=Quaternion.Euler(0,Mathf.Sin(Match.Time*1.8f+i)*13,6);
            }
            for(int i=0;i<lanterns.Length;i++)lanterns[i].sharedMaterial=Match.BeaconsOnline?Factory.Unlit(Ui.Gold):offline;
        }
    }
}
