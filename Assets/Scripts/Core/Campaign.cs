using System;
using System.Collections.Generic;
using System.Linq;

namespace Wolfsun
{
    public sealed class Hero
    {
        public readonly string Id, Name, Title, Power, Description;
        private Hero(string id,string name,string title,string power,string desc){Id=id;Name=name;Title=title;Power=power;Description=desc;}
        public static readonly Hero[] All={
            new Hero("A23","Thor","Last Light","Shield of Asgard","Give an allied unit Shield."),
            new Hero("M23","Runa","The Ice Ferrier","Call the Ferry","Summon a 1/2 Ferryhand in either lane."),
            new Hero("H23","Hel","Keeper of the Names","Keep the Names","Heal an allied unit for 3 or your Bastion for 2."),
            new Hero("J23","Fenrir","Chainbreaker","Snap the Chain","Deal 2 damage to an enemy unit."),
            new Hero("L23","Loki","Oathbreaker","Silver Tongue","Give an enemy unit −2 Attack this round.")
        };
        public static Hero Info(string id)=>All.First(h=>h.Id==id);
    }
    public sealed class Chapter
    {
        public readonly string Name,Boss,Hero,Portrait,DeckName,First,Second,Before,After,Unlock;
        public readonly int Health;
        public Chapter(string name,string boss,string hero,string portrait,string deck,string first,string second,int health,string before,string after,string unlock="")
        {Name=name;Boss=boss;Hero=hero;Portrait=portrait;DeckName=deck;First=first;Second=second;Health=health;Before=before;After=after;Unlock=unlock;}
    }
    public static class Campaign
    {
        public static readonly Chapter[] Chapters={
            new Chapter("Fimbulwinter","Runa, Ice Ferrier","M23","M23","No One Left Behind","P01","P02",15,
                "Three winters come without a summer between them. As Midgard freezes, Runa guards the last ferry. Win her trust and bring the stranded families across.",
                "The ferry reaches shore. Above Midgard, the wolves close in on the sun and moon.","M23"),
            new Chapter("Heimdall’s Horn","The Oathguard","A23","A24","The Lantern Guard","P04","P05",19,
                "The bonds of Fenrir tear loose and the sea surges inland. Heimdall sounds Gjallarhorn to warn the gods. The Oathguard hold the road to Bifröst. Earn passage before the bridge breaks.",
                "The road opens. Across the worlds, the last note of the horn is still ringing."),
            new Chapter("The Road to Hel","Hel, Keeper of the Names","H23","H23","The Long Wake","P04","P02",21,
                "Hermóðr once rode this road to plead for Baldr. Hel agreed to release him if all things wept, but one voice refused. Seek a hearing with Hel before you ride to the final battle.",
                "Hel lets the living leave. Baldr will return after the old world falls.","H23"),
            new Chapter("Naglfar Sets Sail","The Mountain Host","J23","J21","Winter on Credit","P01","P03",22,
                "Jörmungandr churns the sea and the ship Naglfar breaks loose. A host of giants comes ashore. Hold the landing long enough to reach Vígríðr.",
                "The landing is won. Beyond the surf, the serpent rises and the giants march inland."),
            new Chapter("Fenrir at Vígríðr","Fenrir, Chainbreaker","J23","J23","Ferry of Giants","P05","P03",23,
                "On Vígríðr, Fenrir swallows Odin. Víðarr steps forward to avenge his father. Hold the wolf’s attention while Víðarr closes in.",
                "Víðarr avenges Odin. The wolf falls, and the plain grows quiet for a breath.","J23"),
            new Chapter("Loki at the Broken Bridge","Loki, Oathbreaker","L23","L23","False Colors","P02","P06",24,
                "Loki has broken free and fights against the gods. Heimdall meets him on the field. Hold the road so the people behind you can escape the fighting.",
                "Heimdall and Loki fall by each other’s hand. The survivors head for Hoddmímir’s wood.","L23"),
            new Chapter("Surtr’s Fire","Surtr, the Coming Fire","J23","J24","The Burial Avalanche","P06","P01",24,
                "Surtr comes from Múspell with a burning sword. Freyr falls and fire takes the world. Hold the last crossing long enough for Líf and Lífþrasir to reach the sheltering wood.",
                "A green earth rises from the sea. Baldr returns, and Líf and Lífþrasir step out into the new daylight.")
        };
        public static int ChapterAtProgress(int progress) => Math.Max(0, Math.Min(progress, Chapters.Length - 1));
        public static bool CanPlayChapter(int chapter, int progress) => chapter >= 0 && chapter < Chapters.Length && chapter <= progress;

        /// <summary>Practice skirmish modifiers for EA depth without new story art.</summary>
        public static readonly (string Name, int Health, string First, string Second, string Deck)[] Skirmishes =
        {
            ("Ferry Clash", 18, "P01", "P04", "No One Left Behind"),
            ("Horn Echo", 20, "P04", "P05", "The Lantern Guard"),
            ("Ash Tide", 22, "P06", "P03", "Winter on Credit"),
            ("Broken Oath", 24, "P02", "P06", "False Colors"),
            ("Last Ember", 21, "P05", "P01", "The Burial Avalanche")
        };
        private static readonly string[] Pools={"MN","AN","HN","JN","JMN","LN","AHJMLN"};
        public static string[] Starter => Catalog.Deck("A01 A01 A02 A02 A03 A03 A05 A05 A06 A06 A09 A09 A10 A10 A11 A11 A12 A12 A14 A14 A19 A19 N01 N01 N03 N03 N06 N06 N11 N11");
        public static IEnumerable<string> Rewards(int chapter)
        {
            if(chapter==Chapters.Length-1)return Catalog.All.Where(c=>c.Faction!='X'&&c.Faction!='P').Select(c=>c.Id);
            var pool=Pools[chapter];int ceiling=chapter<2?4:chapter<4?5:chapter<6?8:10;
            var award=Catalog.All.Where(c=>pool.Contains(c.Faction)&&c.Cost<=ceiling).Select(c=>c.Id).ToList();
            if(Chapters[chapter].Unlock.Length>0)award.Add(Chapters[chapter].Unlock);
            return award;
        }
        public static string[] EnemyDeck(int chapter)=>Catalog.Deck(Catalog.ThemedDecks[Chapters[chapter].DeckName]);
        public static string[] Recommend(IEnumerable<string> unlocked,string hero)
        {
            var owned=new HashSet<string>(unlocked);
            char home=hero[0];
            var cards=Catalog.All.Where(c=>owned.Contains(c.Id)).ToArray();
            if(cards.Length<=15)return Starter;
            char secondary=cards.Where(c=>c.Faction!=home&&c.Faction!='N')
                .GroupBy(c=>c.Faction).OrderByDescending(g=>g.Sum(c=>c.Attack+c.Health)).Select(g=>g.Key).FirstOrDefault();
            var result=new List<string>();
            float Quality(Card card)
            {
                float identity=card.Faction==home?.6f:0;
                if(card.Kind!=Kind.Unit)return card.Rules.Contains("Deal ")?2.5f:1f;
                return card.Attack*1.2f+card.Health*.68f-card.Cost*1.1f+identity
                    +(card.Rush?1.5f:0)+(card.Guard?1.0f:0)+(card.Unchain>0?1.5f:0)+(card.Legendary?1.8f:0);
            }
            var pool=cards.Where(c=>c.Faction==home||c.Faction==secondary||c.Faction=='N').ToArray();
            void Take(Func<Card,bool> filter,int count)
            {
                foreach(var card in pool.Where(filter).OrderByDescending(Quality).ThenBy(c=>c.Id))
                    while(count>0&&result.Count<30&&result.Count(x=>x==card.Id)<(card.Legendary?1:2))
                    {result.Add(card.Id);count--;}
            }
            Take(c=>c.Kind==Kind.Unit&&c.Cost<=2,12);
            Take(c=>c.Kind==Kind.Unit&&c.Cost>=3&&c.Cost<=4,8);
            Take(c=>c.Kind==Kind.Unit&&c.Cost>=5,6);
            Take(c=>c.Kind==Kind.Order&&c.Rules.Contains("Deal "),4);
            Take(c=>c.Kind==Kind.Unit,30);
            Take(c=>true,30);
            return LegalDeck(result,owned)?result.ToArray():Starter;
        }

        public static bool LegalDeck(IEnumerable<string> sequence,IEnumerable<string> unlocked)
        {
            if(sequence==null||unlocked==null)return false;
            var deck=sequence.ToArray();var owned=new HashSet<string>(unlocked);
            if(deck.Length!=30||deck.Any(id=>!owned.Contains(id)||!Catalog.All.Any(c=>c.Id==id&&c.Faction!='P'&&c.Faction!='X')))return false;
            return deck.Select(id=>Catalog.Get(id).Faction).Where(f=>f!='N').Distinct().Count()<=2
                &&deck.GroupBy(x=>x).All(g=>g.Count()<=(Catalog.Get(g.Key).Legendary?1:2));
        }
    }
}
