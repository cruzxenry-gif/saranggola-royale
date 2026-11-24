using UnityEngine;
using Saranggola.Sim;

namespace Saranggola.View
{
    public sealed partial class ArenaVisual
    {
        public HeroVisual[] Actors { get; private set; }
        private MeshRenderer aim, aimCone, aimLine, targetRing;
        private MeshRenderer[] projectile;
        private MeshRenderer[] waves;
        private int[] waveEnds;
        private float[] waveRadii;
        private int visualEvents;
        private float freezeUntil;

        partial void BuildActors()
        {
            Actors=new HeroVisual[Match.ActorCount];
            for(int i=0;i<Actors.Length;i++)Actors[i]=new HeroVisual(Match.Actors[i],Root,Factory);
            var teal=Factory.Unlit(PropFactory.Html("#74d9b7"));
            aim=Factory.Place(Root,"Aim circle",PropFactory.Primitive(Shape.Ring),teal,Vector3.zero,Vector3.one);
            var cone=new Mesh {name="Aim cone"};
            var vertex=new Vector3[17];
            var tris=new int[45];
            for(int i=0;i<16;i++)
            {
                float a=(i/15f-.5f)*1.38f;
                vertex[i+1]=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a));
                if(i<15){tris[i*3]=0;tris[i*3+1]=i+1;tris[i*3+2]=i+2;}
            }
            cone.vertices=vertex;cone.triangles=tris;cone.RecalculateNormals();
            aimCone=Factory.Place(Root,"Aim cone",cone,teal,Vector3.zero,Vector3.one);
            aimLine=Factory.Place(Root,"Aim line",PropFactory.Primitive(Shape.Box),teal,Vector3.zero,Vector3.one);
            targetRing=Factory.Place(Root,"Locked target",PropFactory.Primitive(Shape.Ring),Factory.Unlit(Ui.Coral),Vector3.zero,Vector3.one*.81f);
            ClearAim();
            projectile=new MeshRenderer[Match.Shots.Length];
            for(int i=0;i<projectile.Length;i++)
            {
                projectile[i]=Factory.Place(Root,"Shot",PropFactory.Primitive(Shape.Sphere),Factory.Unlit(Ui.Gold),Vector3.zero,Vector3.one);
                projectile[i].enabled=false;
            }
            waves=new MeshRenderer[28];
            waveEnds=new int[waves.Length];
            waveRadii=new float[waves.Length];
            for(int i=0;i<waves.Length;i++)
            {
                waves[i]=Factory.Place(Root,"VFX ring",PropFactory.Primitive(Shape.Ring),teal,Vector3.zero,Vector3.one);
                waves[i].enabled=false;
            }
        }

        private void ClearAim()
        {
            aim.enabled=aimCone.enabled=aimLine.enabled=false;
        }

        public void Aim(Actor actor,int slot,Vec2 direction,bool canceled)
        {
            ClearAim();
            if(slot<0||canceled||!actor.Active||direction.SqrLength<.001f)return;
            var ability=actor.Profile.Abilities[slot];
            var dir=PropFactory.Xz(direction.Normalized);
            if(ability.Shape=="line")
            {
                aimLine.enabled=true;
                aimLine.transform.position=Point(actor.Position+direction.Normalized*ability.Range*.5f,.10f);
                aimLine.transform.rotation=Quaternion.LookRotation(dir);
                aimLine.transform.localScale=new Vector3(.28f,.045f,ability.Range);
            }
            else if(ability.Shape=="cone")
            {
                aimCone.enabled=true;
                aimCone.transform.position=Point(actor.Position,.13f);
                aimCone.transform.rotation=Quaternion.LookRotation(dir);
                aimCone.transform.localScale=Vector3.one*ability.Range;
            }
            else
            {
                aim.enabled=true;
                aim.transform.position=Point(actor.Position+direction.Normalized*ability.Get("reach"),.13f);
                aim.transform.localScale=Vector3.one*ability.Range;
            }
        }

        public int PickTarget(Vector2 screen)
        {
            var player=Match.Player;
            float cost=(Screen.height*.067f)*(Screen.height*.067f);
            int found=-1;
            for(int i=0;i<Match.ActorCount;i++)
            {
                var item=Match.Actors[i];
                if(!Match.Enemy(player,item)||Vec2.Dist2(player.Position,item.Position)>31*31)continue;
                var point=Camera.WorldToScreenPoint(Point(item.Position,1.03f));
                if(point.z<=0)continue;
                float gap=((Vector2)point-screen).sqrMagnitude;
                if(gap<cost){cost=gap;found=i;}
            }
            return found;
        }

        partial void SampleActors(float blend,float delta)
        {
            var player=Match.Player;
            for(int i=0;i<Actors.Length;i++)
            {
                var actor=Match.Actors[i];
                bool visible=(actor.Grounded||actor.Life==Life.Gliding)&&actor.Alive&&Match.Visible(player,actor)&&
                    (Point(actor.Position)-Focus).sqrMagnitude<74*74;
                Actors[i].SetVisible(visible);
                if(visible)Actors[i].Sample(this,blend,delta,Time.unscaledTime<freezeUntil);
            }
            var target=Match.At(player.Lock);
            targetRing.enabled=target!=null&&Match.Enemy(player,target);
            if(targetRing.enabled)targetRing.transform.position=Point(target.Position,.15f);
            int index=0;
            for(int i=0;i<Match.ShotCount;i++)
            {
                var shot=Match.Shots[i];
                var at=Point(Vec2.Lerp(shot.Previous,shot.Position,blend),.91f);
                if((at-Focus).sqrMagnitude>53*53)continue;
                var mesh=projectile[index++];
                mesh.enabled=true;
                mesh.transform.position=at;
                mesh.transform.localScale=Vector3.one*shot.Radius*1.30f;
                var source=Match.At(shot.Source);
                var tint=source.Kind==UnitKind.Mob?Ui.Red:PropFactory.Html(source.Profile.Vfx);
                mesh.sharedMaterial=Factory.Unlit(tint);
            }
            for(int i=index;i<projectile.Length;i++)if(projectile[i].enabled)projectile[i].enabled=false;
            ConsumeWorldEffects();
            for(int i=0;i<waves.Length;i++)
            {
                bool live=waveEnds[i]>Match.Tick;
                waves[i].enabled=live;
                if(live)waves[i].transform.localScale=Vector3.one*waveRadii[i]*(.56f+.44f*(1-(waveEnds[i]-Match.Tick)/17f));
            }
        }

        private void ConsumeWorldEffects()
        {
            visualEvents=Mathf.Max(visualEvents,Match.EventCount-Match.Events.Length);
            for(;visualEvents<Match.EventCount;visualEvents++)
            {
                var e=Match.Events[visualEvents%Match.Events.Length];
                if((Point(e.At)-Focus).sqrMagnitude>48*48)continue;
                if(e.Kind==EventKind.Damage)
                {
                    if(e.Amount>200&&e.Target==Match.Player.Id)Shake=.40f;
                    continue;
                }
                if(e.Kind==EventKind.Ultimate)
                {
                    freezeUntil=Time.unscaledTime+.30f;
                    Shake=.76f;
                }
                if(e.Kind==EventKind.Purchase||e.Kind==EventKind.Ping||e.Kind==EventKind.Coin)continue;
                var owner=Match.At(e.Source);
                Color tint=owner==null?Ui.Gold:owner.Team==Match.Player.Team?Ui.Blue:Ui.Red;
                for(int i=0;i<waves.Length;i++)
                {
                    if(waveEnds[i]>Match.Tick)continue;
                    waveEnds[i]=Match.Tick+17;
                    waves[i].enabled=true;
                    waves[i].sharedMaterial=Factory.Unlit(tint);
                    waves[i].transform.position=Point(e.At,.15f);
                    waveRadii[i]=Mathf.Max(.72f,e.Amount);
                    break;
                }
            }
        }
    }
}
