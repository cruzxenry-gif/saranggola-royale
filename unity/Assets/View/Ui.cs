using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Saranggola.View
{
    internal static class Ui
    {
        public static readonly Color Panel = PropFactory.Html("#213943");
        public static readonly Color Rim = PropFactory.Html("#527a73");
        public static readonly Color Pale = PropFactory.Html("#fff0cc");
        public static readonly Color Muted = PropFactory.Html("#bdd2c6");
        public static readonly Color Gold = PropFactory.Html("#f3c376");
        public static readonly Color Coral = PropFactory.Html("#f0976e");
        public static readonly Color Red = PropFactory.Html("#ed746c");
        public static readonly Color Blue = PropFactory.Html("#6bc3ef");
        private static Font font;
        private static Sprite round, disc;

        public static Canvas Canvas(Transform parent)
        {
            var obj = new GameObject("Isla HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            obj.transform.SetParent(parent,false);
            var canvas = obj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = obj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280,720);
            scaler.matchWidthOrHeight = 0.58f;
            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var obj = new GameObject("Input router", typeof(EventSystem));
            var input = obj.AddComponent<InputSystemUIInputModule>();
            input.AssignDefaultActions();
        }

        public static RectTransform Rect(Transform parent,string name,Vector2 anchor,Vector2 offset,Vector2 size)
        {
            var rect = new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=anchor;
            rect.pivot = new Vector2(.5f,.5f);
            rect.anchoredPosition = offset;
            rect.sizeDelta=size;
            return rect;
        }

        public static RectTransform Stretch(Transform parent,string name)
        {
            var rect=Rect(parent,name,new Vector2(.5f,.5f),Vector2.zero,Vector2.zero);
            rect.anchorMin=Vector2.zero;
            rect.anchorMax=Vector2.one;
            rect.offsetMin=rect.offsetMax=Vector2.zero;
            return rect;
        }

        public static void Place(RectTransform r,Vector2 anchor,Vector2 at,Vector2 size)
        {
            r.anchorMin=r.anchorMax=anchor;
            r.anchoredPosition=at;
            r.sizeDelta=size;
        }

        public static Image PanelOn(RectTransform rect,Color tint,bool circle=false)
        {
            var image=rect.gameObject.AddComponent<Image>();
            image.sprite=circle? Circle : Rounded;
            image.type=circle? Image.Type.Simple : Image.Type.Sliced;
            image.color=tint;
            image.raycastTarget=false;
            return image;
        }

        public static Image Tile(Transform parent,string name,Vector2 anchor,Vector2 at,Vector2 size,Color tint,bool circle=false)
            => PanelOn(Rect(parent,name,anchor,at,size),tint,circle);

        public static Text Label(Transform parent,string value,int pixels,Color ink,Vector2 anchor,Vector2 pos,Vector2 bounds,
                                 TextAnchor alignment=TextAnchor.MiddleLeft)
        {
            var r=Rect(parent,"Label",anchor,pos,bounds);
            var text=r.gameObject.AddComponent<Text>();
            if(font==null) font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.font=font;
            text.fontSize=pixels;
            text.color=ink;
            text.text=value;
            text.alignment=alignment;
            text.horizontalOverflow=HorizontalWrapMode.Wrap;
            text.verticalOverflow=VerticalWrapMode.Truncate;
            text.raycastTarget=false;
            return text;
        }

        public static Button Button(Transform parent,string title,Vector2 anchor,Vector2 at,Vector2 size,Color tint,System.Action action,int fontSize=18)
        {
            var rect=Rect(parent,title,anchor,at,size);
            var background=PanelOn(rect,tint);
            background.raycastTarget=true;
            var button=rect.gameObject.AddComponent<Button>();
            button.targetGraphic=background;
            var color=button.colors;
            color.highlightedColor=new Color(1.08f,1.08f,1.08f,1);
            color.pressedColor=new Color(.82f,.83f,.86f,1);
            button.colors=color;
            button.navigation=new Navigation { mode=Navigation.Mode.None };
            button.onClick.AddListener(()=>action());
            Label(rect,title,fontSize,tint==Coral||tint==Gold?Panel:Pale,new Vector2(.5f,.5f),Vector2.zero,size-Vector2.one*8,TextAnchor.MiddleCenter);
            return button;
        }

        public static Sprite Rounded => round!=null?round:round=ShapeSprite(false);
        public static Sprite Circle => disc!=null?disc:disc=ShapeSprite(true);

        private static Sprite ShapeSprite(bool circle)
        {
            const int size=64;
            float radius=circle?31:12;
            var colors=new Color32[size*size];
            for(int y=0;y<size;y++) for(int x=0;x<size;x++)
            {
                var ax=Mathf.Abs(x+.5f-size*.5f); var ay=Mathf.Abs(y+.5f-size*.5f);
                var delta=new Vector2(Mathf.Max(ax-(32-radius),0),Mathf.Max(ay-(32-radius),0));
                float alpha=Mathf.Clamp01(radius+.45f-delta.magnitude);
                colors[y*size+x]=new Color(1,1,1,alpha);
            }
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name=circle?"Control disc":"Rounded panel",filterMode=FilterMode.Bilinear};
            texture.SetPixels32(colors);texture.Apply(false,true);
            return Sprite.Create(texture,new Rect(0,0,size,size),Vector2.one*.5f,100,0,SpriteMeshType.FullRect,
                circle?Vector4.zero:new Vector4(13,13,13,13));
        }
    }
}
