using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;
using Saranggola.Sim;
using EnhancedTouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace Saranggola.View
{
    public sealed partial class GameBootstrap
    {
        private readonly static Vector2 Center = new Vector2(.5f,.5f);
        private Catalog data;
        private BattleMap map;
        private Match match;
        private ArenaVisual world;
        private Canvas canvas;
        private RectTransform panel;
        private Image joystick;
        private Image thumb;
        private Text lab;
        private SkillInput pending = SkillInput.Empty;
        private float accumulator;
        private int heldStick = int.MinValue;
        private Vector2 joystickAxis;
        private bool paused;
        private bool shownResults;
        private Vector2 previousMouse;
        private const int MousePointer = -10;
        private int selected;
        private PlayMode mode;
        private Difficulty difficulty = Difficulty.Normal;

        partial void BuildGame()
        {
            EnhancedTouchSupport.Enable();
            data = new Catalog();
            map = new BattleMap(data);
            Ui.EnsureEventSystem();
            canvas = Ui.Canvas(transform);
            bool showMenu=false;
            BuildFrontEnd(ref showMenu);
            if(!showMenu) StartRound(PlayMode.Duos);
        }

        private void StartRound(PlayMode play)
        {
            world?.Dispose();
            if(panel!=null){panel.gameObject.SetActive(false);Destroy(panel.gameObject);}
            ClearMatchPointers();
            mode=play;
            match=new Match(data, Environment.TickCount,play,difficulty,selected,map:map);
            world=new ArenaVisual(match,transform);
            ApplyQuality();
            accumulator=0;
            paused=false;
            shownResults=false;
            pending=SkillInput.Empty;
            heldStick=int.MinValue;
            joystickAxis=Vector2.zero;
            panel=Ui.Stretch(canvas.transform,"Match HUD");
            joystick=Ui.Tile(panel,"Joystick",Vector2.zero,new Vector2(113,114),new Vector2(124,124),new Color(.09f,.19f,.21f,.63f),true);
            thumb=Ui.Tile(joystick.rectTransform,"Thumb",Center,Vector2.zero,new Vector2(49,49),Ui.Gold,true);
            lab=Ui.Label(panel,"Laban ng Bayan",23,Ui.Pale,new Vector2(0,1),new Vector2(158,-51),new Vector2(264,44));
            ConfigureStick();
            BuildHud();
            BuildOptions();
        }

        private void Update()
        {
            var keys=Keyboard.current;
            ReadGlobalKeys(keys);
            if(match==null) return;
            float delta=Mathf.Min(.16f,Time.unscaledDeltaTime);
            TickPerformance(delta);
            if(!paused && !shownResults)
            {
                ReadTouches();
                ReadKeyboard(keys);
                accumulator+=delta;
                while(accumulator>=Clock.Step && !match.Finished)
                {
                    pending.Move=world.TranslateInput(joystickAxis+KeyboardAxis(keys));
                    ReadHeld(keys);
                    ApplyExperienceInput();
                    match.Step(pending);
                    pending=SkillInput.Empty;
                    accumulator-=Clock.Step;
                }
            }
            if(!paused) world.Sample(Mathf.Clamp01(accumulator/Clock.Step),delta);
            UpdateHud(delta);
            if(match.Finished && !shownResults)
            {
                shownResults=true;
                PresentResult();
            }
        }

        private void ReadTouches()
        {
            foreach(var touch in EnhancedTouch.activeTouches)
            {
                if(touch.phase==TouchPhase.Began)
                    PointerDown(touch.touchId,touch.screenPosition);
                else if(touch.phase==TouchPhase.Ended || touch.phase==TouchPhase.Canceled)
                    PointerUp(touch.touchId,touch.screenPosition,touch.phase==TouchPhase.Canceled);
                else if(touch.phase==TouchPhase.Moved)
                    PointerMove(touch.touchId,touch.screenPosition);
            }
            var mouse=Mouse.current;
            if(mouse==null) return;
            var point=mouse.position.ReadValue();
            if(mouse.leftButton.wasPressedThisFrame) PointerDown(MousePointer,point);
            else if(mouse.leftButton.wasReleasedThisFrame) PointerUp(MousePointer,point,false);
            else if(mouse.leftButton.isPressed && point!=previousMouse) PointerMove(MousePointer,point);
            previousMouse=point;
        }

        private void PointerDown(int finger,Vector2 p)
        {
            bool used=false;
            MatchPointerDown(finger,p,ref used);
            if(used || heldStick!=int.MinValue || joystick==null ||
                !RectTransformUtility.RectangleContainsScreenPoint(joystick.rectTransform,p)) return;
            heldStick=finger;
            UpdateStick(p);
        }

        private void PointerMove(int finger,Vector2 p)
        {
            if(heldStick==finger) UpdateStick(p);
            else MatchPointerMove(finger,p);
        }

        private void PointerUp(int finger,Vector2 p,bool canceled)
        {
            if(heldStick==finger) ClearStick();
            else MatchPointerUp(finger,p,canceled);
        }

        private void UpdateStick(Vector2 p)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(joystick.rectTransform,p,null,out var local);
            var d=Vector2.ClampMagnitude(local,joystick.rectTransform.sizeDelta.x*.44f);
            joystickAxis=d/(joystick.rectTransform.sizeDelta.x*.44f);
            thumb.rectTransform.anchoredPosition=d;
        }

        private void ClearStick()
        {
            heldStick=int.MinValue;
            joystickAxis=Vector2.zero;
            if(thumb!=null) thumb.rectTransform.anchoredPosition=Vector2.zero;
        }

        private static Vector2 KeyboardAxis(Keyboard keyboard)
        {
            if(keyboard==null) return Vector2.zero;
            return new Vector2((keyboard.dKey.isPressed||keyboard.rightArrowKey.isPressed?1:0)-(keyboard.aKey.isPressed||keyboard.leftArrowKey.isPressed?1:0),
                (keyboard.wKey.isPressed||keyboard.upArrowKey.isPressed?1:0)-(keyboard.sKey.isPressed||keyboard.downArrowKey.isPressed?1:0));
        }

        private void OnDestroy()
        {
            world?.Dispose();
            if(EnhancedTouchSupport.enabled) EnhancedTouchSupport.Disable();
        }

        private void OnApplicationFocus(bool focus)
        {
            if(focus) return;
            ClearStick();
            ClearMatchPointers();
        }

        partial void BuildFrontEnd(ref bool created);
        partial void BuildHud();
        partial void UpdateHud(float delta);
        partial void ConfigureStick();
        partial void MatchPointerDown(int finger,Vector2 position,ref bool used);
        partial void MatchPointerMove(int finger,Vector2 position);
        partial void MatchPointerUp(int finger,Vector2 position,bool canceled);
        partial void ClearMatchPointers();
        partial void ReadKeyboard(Keyboard keys);
        partial void ReadHeld(Keyboard keys);
        partial void PresentResult();
    }
}
