using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Wolfsun
{
    public sealed partial class Game : MonoBehaviour
    {
        [Serializable] private sealed class DeckSlot
        {
            public string hero;
            public string[] cards;
        }
        [Serializable] private sealed class DailyQuest
        {
            public string id, label;
            public int target, progress;
            public int rewardEmbers;
            public bool claimed;
        }
        [Serializable] private sealed class Profile
        {
            public int progress, wins, losses, sealsClaimed, embers;
            public string hero="A23";
            public string[] deck=Campaign.Starter;
            public List<DeckSlot> loadouts=new List<DeckSlot>();
            public bool tutorialDone, skipTutorialOffer;
            public int aiDifficulty=1;
            public string collectionQuery="";
            public string favoriteHero="A23";
            public string cardBack="Foundry/CardBack";
            public string[] unlockedCardBacks=new[]{"Foundry/CardBack"};
            public string dailyStamp="";
            public List<DailyQuest> dailies=new List<DailyQuest>();
            public int ladderAbsolute, ladderStars, starBonus=2, winStreak, peakAbsolute;
            public int seasonYear, seasonMonth, legendPlace, rankedGames, apprenticeLeft=5;
            public double rankedMmr=1500, rankedRd=350, casualMmr=1500, casualRd=350;
            public string[] unlockedAchievements=Array.Empty<string>();
            // Campaign RPG layer (versioned via JsonUtility; migrate from progress on load).
            public string[] completedEncounters=Array.Empty<string>();
            public string[] ownedRelics=Array.Empty<string>();
            public string[] equippedRelics=Array.Empty<string>();
            public List<HeroSkillBuild> skillBuilds=new List<HeroSkillBuild>();
        }
        private enum Page { Home, Story, Collection, Battle, Outcome, Play, Career, Skills }
        private static readonly Color Ink=new Color32(14,23,35,249);
        private static readonly Color Gold=new Color32(242,195,127,255);
        private static readonly Color White=new Color32(245,235,218,255);
        private static readonly Color Muted=new Color32(172,189,197,255);
        private static readonly Color Frost=new Color32(173,221,240,255);
        private readonly Dictionary<string,Texture2D> textures=new Dictionary<string,Texture2D>();
        private readonly Dictionary<string,AudioClip> clips=new Dictionary<string,AudioClip>();
        private Page page;
        private Profile profile;
        private RectTransform stage,root;
        private Font titleFont, bodyFont;
        private AudioSource score, sfx;
        private Match match;
        private readonly HashSet<int> mulligan=new HashSet<int>();
        private List<string> build;
        private readonly HashSet<string> owned=new HashSet<string>();
        private int chapter, collectionPage, filter, cursor=-1, listened, newCards;
        private ActionKind selected=ActionKind.Pass;
        private string inspected, status="", result="", musicName="";
        private bool duel, settings, choosing, ai, presented, ownedOnly, inspectBack, tutorialMatch, queueSearching;
        private string[] rewardCards=Array.Empty<string>();
        private Net.Matchmaker matchmaker=new Net.Matchmaker();
        private Net.QueueMode pendingQueue=Net.QueueMode.Casual;
        private Net.AuthoritativeDuel onlineDuel;
        private string queueStatus="";
        private float queueStarted;
        private readonly List<string> battleLogCache=new List<string>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if(FindAnyObjectByType<Game>()==null)new GameObject("Dawnwager").AddComponent<Game>();
        }

        private void Awake()
        {
            Application.targetFrameRate=60;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            ImportPreviousWindowsProfile();
#endif
            SetDisplayMode(PlayerPrefs.GetInt(FullscreenPreference,1)!=0);
            if(Camera.main==null)
            {
                var camera=new GameObject("Camera").AddComponent<Camera>();camera.tag="MainCamera";
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Ink;
            }
            if(FindAnyObjectByType<AudioListener>()==null) Camera.main.gameObject.AddComponent<AudioListener>();
            score=gameObject.AddComponent<AudioSource>();score.loop=true;score.spatialBlend=0;
            sfx=gameObject.AddComponent<AudioSource>();sfx.spatialBlend=0;
            reduceMotion=PlayerPrefs.GetInt("wolfsun.reduceMotion",0)!=0;
            score.volume=PlayerPrefs.GetFloat("wolfsun.music",.62f);
            sfx.volume=PlayerPrefs.GetFloat("wolfsun.effects",.8f);
            try{profile=JsonUtility.FromJson<Profile>(PlayerPrefs.GetString("wolfsun.profile",""))??new Profile();}
            catch(ArgumentException){profile=new Profile();}
            profile.progress=Mathf.Clamp(profile.progress,0,Campaign.Chapters.Length);
            if(!UnlockedHero(profile.hero))profile.hero="A23";
            LoadOwned();
            if(!Campaign.LegalDeck(profile.deck,owned))profile.deck=Campaign.Starter;
            profile.loadouts=profile.loadouts??new List<DeckSlot>();
            profile.dailies=profile.dailies??new List<DailyQuest>();
            profile.unlockedAchievements=profile.unlockedAchievements??Array.Empty<string>();
            profile.completedEncounters=profile.completedEncounters??Array.Empty<string>();
            profile.ownedRelics=profile.ownedRelics??Array.Empty<string>();
            profile.equippedRelics=profile.equippedRelics??Array.Empty<string>();
            profile.skillBuilds=profile.skillBuilds??new List<HeroSkillBuild>();
            EnsureCampaignProfile();
            if(profile.starBonus<1)profile.starBonus=2;
            if(profile.rankedMmr<1)profile.rankedMmr=Ladder.DefaultMmr;
            if(profile.casualMmr<1)profile.casualMmr=Ladder.DefaultMmr;
            if(profile.apprenticeLeft<=0&&profile.rankedGames==0&&!profile.tutorialDone)profile.apprenticeLeft=5;
            SyncLadderFromProfile();
            RefreshDailies();
            build=profile.deck.ToList();
            titleFont=Resources.Load<Font>("Display");bodyFont=Resources.Load<Font>("Body");
            if(titleFont==null)titleFont=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if(bodyFont==null)bodyFont=titleFont;
            var canvas=new GameObject("Canvas",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1920,1080);
            scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            if(FindAnyObjectByType<EventSystem>()==null)
                new GameObject("Event System",typeof(EventSystem),typeof(InputSystemUIInputModule));
            CreateBackdrop(canvas.transform);
            var o=new GameObject("Stage",typeof(RectTransform));
            stage=o.GetComponent<RectTransform>();stage.SetParent(canvas.transform,false);
            stage.anchorMin=stage.anchorMax=new Vector2(.5f,.5f);stage.sizeDelta=new Vector2(1920,1080);
            Render();
        }

        private bool UnlockedHero(string id)
        {
            return id=="A23"||Campaign.Chapters.Take(profile.progress).Any(c=>c.Unlock==id);
        }
        private void LoadOwned()
        {
            owned.Clear();
            foreach(var id in Campaign.Starter)owned.Add(id);
            for(int i=0;i<profile.progress;i++)foreach(var id in Campaign.Rewards(i))owned.Add(id);
        }
        private void Save()
        {
            PlayerPrefs.SetString("wolfsun.profile",JsonUtility.ToJson(profile));PlayerPrefs.Save();
        }
        private void StoreDeck()
        {
            var slot=profile.loadouts.FirstOrDefault(d=>d.hero==profile.hero);
            if(slot==null){slot=new DeckSlot{hero=profile.hero};profile.loadouts.Add(slot);}
            slot.cards=profile.deck;
        }
        private void ChooseHero(string id)
        {
            if(!UnlockedHero(id)||profile.hero==id)return;
            StoreDeck();profile.hero=id;
            var saved=profile.loadouts.FirstOrDefault(d=>d.hero==id);
            profile.deck=saved!=null&&Campaign.LegalDeck(saved.cards,owned)?saved.cards:Campaign.Recommend(owned,id);
            build=profile.deck.ToList();Save();status="Deck equipped for "+Hero.Info(id).Name+".";Render();
        }
        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f11Key.wasPressedThisFrame) ToggleFullscreen();
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame || presenting) return;
            if(dragLayer){CancelDrag();return;}
            if(showRules){showRules=false;Render();return;}
            if(settings){settings=false;PlayerPrefs.Save();Render();return;}
            if(inspected!=null||choosing){inspected=null;choosing=false;selected=ActionKind.Pass;Render();return;}
            if(page==Page.Home||page==Page.Battle){settings=true;Render();}
            else Go(Page.Home);
        }

        private void Go(Page destination)
        {
            if(page==Page.Battle&&destination!=Page.Battle)StopAllCoroutines();
            if(destination!=Page.Play){queueSearching=false;queueStatus="";}
            if(destination!=Page.Home&&menuAmbience!=null&&menuAmbience.isPlaying)menuAmbience.Stop();
            ai=false;presenting=false;showRules=false;page=destination;inspected=null;settings=false;status="";
            if(destination!=Page.Battle&&destination!=Page.Outcome){tutorialMatch=false;onlineDuel=null;}
            if(destination!=Page.Battle)ResetPhaseAnnouncements();
            Render();
        }
        private Texture2D Texture(string name)
        {
            if (!textures.TryGetValue(name, out var tex) || tex == null)
            {
                tex = Resources.Load<Texture2D>("Art/" + name);
                textures[name] = tex;
            }
            return tex;
        }
        private AudioClip Clip(string name)
        {
            if(!clips.TryGetValue(name,out var clip)){clip=Resources.Load<AudioClip>("Audio/"+name);clips[name]=clip;}
            return clip;
        }
        private void Sound(string cue)
        {
            if(string.IsNullOrEmpty(cue))return;
            var path=cue.Length==3&&char.IsDigit(cue[1])?"Cards/"+cue:cue;
            var clip=Clip(path);if(clip)sfx.PlayOneShot(clip,path.StartsWith("Cards/")?.8f:.65f);
        }
        private void Music(string cue)
        {
            if(musicName==cue)return;
            musicName=cue;score.clip=Clip(cue);if(score.clip)score.Play();
        }
        private void Render()
        {
            CancelDrag();
            ClearCardPreview();
            viewportShades.Clear();
            if(root)
            {
                root.gameObject.SetActive(false);
                Destroy(root.gameObject);
            }
            root=Box(stage,"Screen",0,0,1920,1080);
            UpdateBackdrop();
            if(page!=Page.Home&&page!=Page.Battle&&page!=Page.Outcome)Nav();
            switch(page)
            {
                case Page.Home:Home();break;
                case Page.Story:Story();break;
                case Page.Collection:Collection();break;
                case Page.Battle:Battle();break;
                case Page.Outcome:Outcome();break;
                case Page.Play:PlayHub();break;
                case Page.Career:Career();break;
                case Page.Skills:SkillTree();break;
            }
            if(page==Page.Home)TryMenuAmbience();
            Music(page==Page.Battle?(!duel&&!tutorialMatch&&chapter==Campaign.Chapters.Length-1?"Boss":"Battle")
                :page==Page.Story||page==Page.Skills?"Campaign":"Menu");
            if(inspected!=null)Inspect();
            if(showRules)RulesOverlay();
            if(settings)Settings();
        }
        private RectTransform Box(Transform parent,string name,float x,float y,float w,float h)
        {
            var o=new GameObject(name,typeof(RectTransform));var r=o.GetComponent<RectTransform>();r.SetParent(parent,false);
            r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);
            r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;
        }
        private RawImage Picture(Transform parent,string name,float x,float y,float w,float h,Color? tint=null)
        {
            var view=Box(parent,name,x,y,w,h).gameObject.AddComponent<RawImage>();
            view.texture=Texture(name);view.color=tint??Color.white;view.raycastTarget=false;return view;
        }
        private RawImage Fit(Transform parent,string name,float x,float y,float w,float h)
        {
            var tex=Texture(name);
            if(tex==null)return Picture(parent,name,x,y,w,h,new Color(0,0,0,0));
            float s=Mathf.Min(w/tex.width,h/tex.height);float pw=tex.width*s,ph=tex.height*s;
            return Picture(parent,name,x+(w-pw)*.5f,y+(h-ph)*.5f,pw,ph);
        }
        private RawImage Cover(Transform parent,string name,float x,float y,float w,float h,Color tint)
        {
            var view=Picture(parent,name,x,y,w,h,tint);
            var t=view.texture;if(!t)return view;
            float aspect=w/h, src=(float)t.width/t.height;
            if(src>aspect){float crop=aspect/src;view.uvRect=new Rect((1-crop)/2,0,crop,1);}
            else{float crop=src/aspect;view.uvRect=new Rect(0,(1-crop)/2,1,crop);}
            return view;
        }
        private static Image Fill(RectTransform parent,Color fill,bool hit=false)
        {
            var image=parent.gameObject.AddComponent<Image>();image.color=fill;image.raycastTarget=hit;return image;
        }
        private RectTransform ColorBox(Transform parent,string name,float x,float y,float w,float h,Color fill,bool hit=false)
        {var box=Box(parent,name,x,y,w,h);Fill(box,fill,hit);return box;}
        private Text Label(Transform parent,string words,float x,float y,float w,float h,int size,Color color,bool formal=false,TextAnchor anchor=TextAnchor.MiddleLeft)
        {
            var o=Box(parent,words,x,y,w,h);
            var t=o.gameObject.AddComponent<Text>();t.font=formal?titleFont:bodyFont;
            t.text=words;t.fontSize=size;t.color=color;t.alignment=anchor;t.raycastTarget=false;
            t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;
            return t;
        }
        private Button Click(RectTransform rect,Action action,bool enabled=true,Graphic graphic=null)
        {
            // Unity allows only one Graphic on a GameObject. Clickable panels can already have an Image.
            graphic=graphic??rect.GetComponent<Graphic>()??Fill(rect,new Color(0,0,0,.012f),true);
            graphic.raycastTarget=true;
            var b=rect.gameObject.AddComponent<Button>();b.targetGraphic=graphic;b.interactable=enabled;
            if(action!=null)b.onClick.AddListener(()=>action());
            if(graphic is Image image&&image.color.a==0)b.transition=Selectable.Transition.None;
            return b;
        }
        private void Button(string label,float x,float y,float w,float h,Action action,bool enabled=true,bool primary=false,Transform parent=null)
        {
            TableButton(label,x,y,w,h,action,enabled,primary,parent);
        }
        private void Frame(Transform parent,float x,float y,float w,float h,bool ice=false,Color? tint=null)
        {
            ChromePanel(parent,"Panel",x,y,w,h, ice ? "PanelGold" : "Panel");
        }
        private void Rule(float x,float y,float w)=>ColorBox(root,"Section rule",x,y,w,1,Edge);
        private static string Lane(int lane)=>lane==0?"West":"East";
    }
}
