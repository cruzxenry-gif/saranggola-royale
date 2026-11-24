using UnityEngine;

namespace Saranggola.View
{
    internal static class SkillIcons
    {
        private static readonly Sprite[,] cache = new Sprite[7,3];
        public static Sprite Get(int hero,int slot)
        {
            if(cache[hero,slot]!=null)return cache[hero,slot];
            const int size=48;
            var pixels=new Color32[size*size];
            var code=hero*3+slot;
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                var p=new Vector2((x+0.5f-size*.5f)/(size*.5f),(y+.5f-size*.5f)/(size*.5f));
                var r=p.magnitude;
                var angle=Mathf.Atan2(p.y,p.x);
                bool central=false;
                switch(code%7)
                {
                    case 0: central=Mathf.Abs(p.x+p.y*.82f)<.11f&&r<.71f || Mathf.Abs(p.y-.31f)<.09f&&p.x<-.26f;break;
                    case 1: central=r<.59f+Mathf.Cos(angle*(4+slot))*.13f&&r>.18f;break;
                    case 2: central=Mathf.Abs(p.x)+Mathf.Abs(p.y)*.66f<.53f&&Mathf.Abs(p.x-p.y)>.08f;break;
                    case 3: central=r<.72f&&r>.43f&&(Mathf.Sin(angle*3+hero)>.03f||Mathf.Abs(p.x)<.12f);break;
                    case 4: central=Mathf.Abs(p.y+Mathf.Sin(p.x*4.1f)*.20f)<.12f&&Mathf.Abs(p.x)<.72f || Mathf.Abs(p.x)<.070f&&r<.69f;break;
                    case 5: central=Mathf.Abs(Mathf.Abs(p.x)-p.y*.74f)<.12f&&r<.69f || r<.24f;break;
                    case 6: central=Mathf.Abs(p.x)<.105f&&Mathf.Abs(p.y)<.67f||Mathf.Abs(p.y-.28f)<.1f&&Mathf.Abs(p.x)<.63f;break;
                }
                bool accents=(p-new Vector2(-.64f+slot*.13f,-.65f+hero*.075f)).sqrMagnitude<.013f ||
                    (p-new Vector2(.55f-slot*.12f,.61f-hero*.06f)).sqrMagnitude<.014f;
                pixels[y*size+x]=new Color(1,1,1,central||accents?1:0);
            }
            var tex=new Texture2D(size,size,TextureFormat.RGBA32,false){name="Skill symbol "+code,filterMode=FilterMode.Bilinear};
            tex.SetPixels32(pixels);tex.Apply(false,true);
            return cache[hero,slot]=Sprite.Create(tex,new Rect(0,0,size,size),Vector2.one*.5f);
        }
    }
}
