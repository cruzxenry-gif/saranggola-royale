using UnityEngine;
using UnityEngine.UI;
using Saranggola.Sim;

namespace Saranggola.View
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed partial class RadarGraphic : Graphic
    {
        public Match Match;
        public Actor Viewer;
        private Rect area;

        public Vec2 FromScreen(Vector2 position)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,position,null,out var local);
            var r=rectTransform.rect;
            return new Vec2((local.x-r.xMin)/r.width*200-100,100-(local.y-r.yMin)/r.height*200);
        }

        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear();
            if(Match==null||Viewer==null)return;
            area=rectTransform.rect;
            if(Match.KiteFlying)
            {
                Segment(vertices,Point(Match.KiteStart),Point(Match.KiteEnd),Ui.Gold,1.45f);
                Dot(vertices,Match.KitePosition,2.5f,Ui.Gold);
            }
            if(Match.Bagyo.Phase>0)
            {
                Circle(vertices,Match.Bagyo.Center,Match.Bagyo.Radius,Ui.Blue,false);
                Circle(vertices,Match.Bagyo.NextCenter,Match.Bagyo.NextRadius,Ui.Muted,true);
            }
            foreach(var outpost in Match.Outposts)Dot(vertices,outpost.Position,2,outpost.Owner<0?Ui.Muted:PropFactory.Team(outpost.Owner));
            if(Match.BeaconsOnline)foreach(var beacon in Match.Map.Beacons)Dot(vertices,beacon,1.5f,Ui.Blue);
            foreach(var actor in Match.Heroes)
            {
                if(!actor.Grounded||!Match.MapVision(Viewer,actor))continue;
                var tint=actor==Viewer?Color.white:actor.Streak>=3?Ui.Gold:PropFactory.Team(actor.Team);
                float size=actor==Viewer?3f:actor.Streak>=3?3.2f:1.9f;
                Dot(vertices,actor.Position,size,tint);
            }
            if(Match.UsesDuos&&Match.LastTeamPingTick[Viewer.Team]>Match.Tick-150)
                Dot(vertices,Match.TeamPing[Viewer.Team],4f,Ui.Coral);
            AddObjectives(vertices);
        }

        partial void AddObjectives(VertexHelper vertices);

        private Vector2 Point(Vec2 p) => new Vector2(area.xMin+(p.X+100)*area.width/200,area.yMax-(p.Z+100)*area.height/200);

        private void Circle(VertexHelper h,Vec2 at,float radius,Color color,bool dotted)
        {
            var p=Point(at);
            var scale=radius*area.width/200;
            var prev=p+Vector2.right*scale;
            for(int i=1;i<=48;i++)
            {
                float rad=i*Mathf.PI/24;
                var b=p+new Vector2(Mathf.Cos(rad),Mathf.Sin(rad))*scale;
                if((!dotted||i%2==0)&&area.Contains(prev)&&area.Contains(b))Segment(h,prev,b,color,1.1f);
                prev=b;
            }
        }

        private void Dot(VertexHelper h,Vec2 p,float width,Color tint)
        {
            var local=Point(p);
            if(!area.Contains(local))return;
            Quad(h,local+new Vector2(-width,-width),local+new Vector2(-width,width),
                 local+new Vector2(width,width),local+new Vector2(width,-width),tint);
        }

        private static void Segment(VertexHelper h,Vector2 from,Vector2 to,Color color,float width)
        {
            Vector2 dir=(to-from).normalized;
            Vector2 normal=new Vector2(-dir.y,dir.x)*width*.5f;
            Quad(h,from-normal,from+normal,to+normal,to-normal,color);
        }

        private static void Quad(VertexHelper h,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color tone)
        {
            var id=h.currentVertCount;
            h.AddVert(a,tone,Vector2.zero);h.AddVert(b,tone,Vector2.zero);h.AddVert(c,tone,Vector2.zero);h.AddVert(d,tone,Vector2.zero);
            h.AddTriangle(id,id+1,id+2);h.AddTriangle(id,id+2,id+3);
        }
    }
}
