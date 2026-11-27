using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Wolfsun
{
    public sealed partial class Game
    {
        private void StartMatch(bool practice)
        {
            if(!practice&&!Campaign.CanPlayChapter(chapter,profile.progress))return;
            StopAllCoroutines();
            if(!Campaign.LegalDeck(profile.deck,owned))
            {
                profile.deck=Campaign.Starter;
                Save();
            }
            PlayerPrefs.SetInt("dawnwager.rated",0);
            ai=false;returnPage=practice?Page.Play:Page.Story;
            duel=practice;showRules=false;rewardCards=Array.Empty<string>();newCards=0;presented=false;listened=0;
            mulligan.Clear();choosing=false;presenting=false;selected=ActionKind.Pass;cursor=-1;inspected=null;status="";
            ResetPhaseAnnouncements();
            tutorialMatch=false;
            if(practice)
            {
                var random=new System.Random();
                var skirmish=Campaign.Skirmishes[random.Next(Campaign.Skirmishes.Length)];
                match=new Match(profile.deck,Catalog.Deck(Catalog.ThemedDecks[skirmish.Deck]),profile.hero,"M23",skirmish.First,skirmish.Second,random.Next(),skirmish.Health);
                status=skirmish.Name;
            }
            else
            {
                var foe=Campaign.Chapters[chapter];
                var enc=CampaignMeta.MainAt(chapter);
                activeEncounterId=enc?.Id??("C0"+(chapter+1));
                var rules=SnapshotCampaignRules(activeEncounterId);
                match=new Match(profile.deck,Campaign.EnemyDeck(chapter),profile.hero,foe.Hero,foe.First,foe.Second,Environment.TickCount,foe.Health,rules);
            }
            Go(Page.Battle);
            DealOpeningHand();
        }

        private void ResetMatchState()
        {
            ai=false;showRules=false;rewardCards=Array.Empty<string>();newCards=0;presented=false;listened=0;
            mulligan.Clear();choosing=false;presenting=false;selected=ActionKind.Pass;cursor=-1;inspected=null;status="";
            pendingRelicChoices=Array.Empty<string>();pendingRelicEncounter="";
            PlayerPrefs.SetInt("dawnwager.rated",0);
            ResetPhaseAnnouncements();
        }

        private void Command(Move move, Vector2? dragOrigin = null)
        {
            endTurnArmed=false;
            if(presenting || !ResolveWithMotion(0,move,dragOrigin))return;
            Schedule();
        }
        private string MixLog()
        {
            var newBeats=match.Log.Skip(listened).Where(b=>!string.IsNullOrEmpty(b.Sound)).ToList();
            listened=match.Log.Count;
            if(newBeats.Count==0)return null;
            // Priority queue so combat/UI cues are not dropped behind a single card VO.
            string[] priority={"prophecy-reveal","beacon-claim","unit-hit","shield-break","pass-turn",
                "card-play-unit","card-play-order","card-play-site","card-deal","card-draw-1"};
            var ordered=new System.Collections.Generic.List<string>();
            foreach(var key in priority)
            {
                var hit=newBeats.FirstOrDefault(b=>b.Sound==key);
                if(hit!=null&&!ordered.Contains(key))ordered.Add(key);
            }
            foreach(var beat in newBeats)
            {
                if(ordered.Contains(beat.Sound))continue;
                // Card VO — play kind foley first, then voice after a short delay in the queue.
                if(beat.Sound.Length==3&&char.IsDigit(beat.Sound[1]))
                {
                    string foley=PlayFoleyForCard(beat.Sound);
                    if(foley!=null&&!ordered.Contains(foley))ordered.Add(foley);
                    ordered.Add(beat.Sound);
                    continue;
                }
                ordered.Add(beat.Sound);
            }
            if(ordered.Count==0)ordered.Add(newBeats[newBeats.Count-1].Sound);
            StartCoroutine(PlayCueQueue(ordered));
            return ordered[0];
        }

        private static string PlayFoleyForCard(string id)
        {
            try
            {
                var card=Catalog.Get(id);
                if(card.Kind==Kind.Unit)return "card-play-unit";
                if(card.Kind==Kind.Order)return "card-play-order";
                if(card.Kind==Kind.Site)return "card-play-site";
            }
            catch { /* ignore unknown */ }
            return null;
        }

        private System.Collections.IEnumerator PlayCueQueue(System.Collections.Generic.List<string> cues)
        {
            string lastVariation=null;
            int burst=0;
            float burstWindow=Time.unscaledTime;
            for(int i=0;i<cues.Count;i++)
            {
                string cue=cues[i];
                // Limit repeated draw/UI to 3 / 150ms; avoid same variation back-to-back.
                bool isDraw=cue!=null&&(cue.StartsWith("card-draw")||cue=="card-deal"||cue.StartsWith("ui-"));
                if(isDraw)
                {
                    if(Time.unscaledTime-burstWindow>0.15f){burst=0;burstWindow=Time.unscaledTime;}
                    if(burst>=3)continue;
                    burst++;
                }
                if(cue==lastVariation)continue;
                lastVariation=cue;
                Sound(cue);
                float gap=cue!=null&&cue.Length==3&&char.IsDigit(cue[1])?.12f:.07f;
                if(i<cues.Count-1)yield return new WaitForSecondsRealtime(gap);
            }
        }
        private void Schedule()
        {
            if(match.Winner>=0)
            {
                if(!presented){presented=true;StartCoroutine(ShowResult());}
                return;
            }
            if(match.Turn==1&&!ai)StartCoroutine(Enemy());
        }
        private IEnumerator Enemy()
        {
            ai=true;
            while(page==Page.Battle&&match.Turn==1&&match.Winner<0)
            {
                yield return new WaitForSecondsRealtime(.64f);
                while((settings||showRules||presenting)&&page==Page.Battle)yield return null;
                var move=match.ChooseAI(1, tutorialMatch ? 0 : Mathf.Clamp(profile.aiDifficulty, 0, 2));
                bool resolved=move!=null&&ResolveWithMotion(1,move);
                if(resolved)yield return new WaitForSecondsRealtime(move.Kind==ActionKind.Play?1.0f:move.Kind==ActionKind.Attack?.7f:.45f);
                if(!resolved)
                {
                    // Never leave the match stuck on the enemy's turn: fall back to passing.
                    if(match.Turn==1&&match.Winner<0)match.Apply(1,new Move(ActionKind.Pass));
                    break;
                }
            }
            ai=false;
            presenting=false;
            // The last enemy move re-rendered while ai was still set, so redraw now that it is the player's turn.
            if(page==Page.Battle&&match!=null)Render();
            if(match.Winner>=0)Schedule();
        }
        private IEnumerator ShowResult()
        {
            yield return new WaitForSecondsRealtime(.85f);
            while(settings||showRules||presenting)yield return null;
            if(page!=Page.Battle)yield break;
            var before=new System.Collections.Generic.HashSet<string>(owned);
            int sealsNow=match.Players[0].Seals;
            bool won=match.Winner==0;
            if(won)
            {
                result="Victory";
                if(!duel&&!tutorialMatch)
                {
                    bool optionalRun=CampaignMeta.Encounters.Any(e=>e.Id==activeEncounterId&&e.IsOptional);
                    bool firstMainClear=!optionalRun&&chapter==profile.progress;
                    if(firstMainClear)
                    {
                        profile.progress++;LoadOwned();
                        rewardCards=owned.Except(before).OrderByDescending(x=>Catalog.Get(x).Legendary).ThenBy(x=>x).ToArray();
                        newCards=rewardCards.Length;
                    }
                    string encId=activeEncounterId;
                    if(string.IsNullOrEmpty(encId))
                    {
                        var enc=CampaignMeta.MainAt(chapter);
                        encId=enc?.Id??"";
                    }
                    var cleared=CampaignMeta.Encounters.FirstOrDefault(e=>e.Id==encId);
                    if(!string.IsNullOrEmpty(encId)&&!EncounterCleared(encId))
                        ClaimFirstClear(encId,cleared?.Relic??"");
                    Save();
                }
                Sound("victory");
            }
            else
            {
                result=match.Winner==2?"Draw":"Defeat";
                if(match.Winner!=2)UiSound("defeat");
            }
            RecordMatchEnd(won,duel||tutorialMatch,Mathf.Max(0,sealsNow));
            if(PlayerPrefs.GetInt("dawnwager.rated",0)==1)
            {
                ApplyRatedResult(won,(Net.QueueMode)PlayerPrefs.GetInt("dawnwager.lastMode",0),
                    PlayerPrefs.GetFloat("dawnwager.lastOppMmr",1500),PlayerPrefs.GetFloat("dawnwager.lastOppRd",200));
                PlayerPrefs.SetInt("dawnwager.rated",0);
            }
            onlineDuel=null;
            Go(Page.Outcome);
            // Keep tutorialMatch through Outcome CTAs; cleared when leaving Outcome via Go().
        }
    }
}
