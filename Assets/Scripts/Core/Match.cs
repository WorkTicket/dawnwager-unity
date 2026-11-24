using System;
using System.Collections.Generic;
using System.Linq;

namespace Wolfsun
{
    public enum Kind { Unit, Order, Site, Prophecy }
    public enum ActionKind { Play, Attack, March, Power, Pass }
    public sealed class Card
    {
        public readonly string Id, Name, Rules;
        public readonly Kind Kind;
        public readonly int Cost, Attack, Health, Unchain;
        public readonly bool Legendary, Guard, Rush;
        public char Faction => Id[0];
        public Card(string id, string name, Kind kind, int cost, int attack, int health, string rules, bool legendary)
        {
            Id=id; Name=name; Kind=kind; Cost=cost; Attack=attack; Health=health; Rules=rules; Legendary=legendary;
            Guard=rules.StartsWith("Guard.", StringComparison.Ordinal);
            Rush=rules.StartsWith("Rush.",StringComparison.Ordinal) || (rules.StartsWith("Unchain",StringComparison.Ordinal) && rules.Contains(" Rush."));
            Unchain=rules.StartsWith("Unchain ",StringComparison.Ordinal) ? rules[8]-'0' : 0;
        }
    }
    public static partial class Catalog
    {
        public static Card Get(string id) => All.First(c=>c.Id==id);
        public static string[] Deck(string source) => source.Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries);
    }
    public sealed class Piece
    {
        public int Key, Side, Lane, Power, Temp, Hp, Max, Born, Moved, Mark, Marks, GuardStrike;
        public bool Guard, Shield, Spent;
        public Card Card;
        public int Attack => Math.Max(0,Power+Temp);
    }
    public sealed class Pilot
    {
        public int Hp=24, MaxHp=24, Charge, Seals, Fatigue;
        public int Dead, OmenDead, PowerRound;
        public string Hero;
        public readonly List<string> Hand=new List<string>(), Pile=new List<string>(), Discard=new List<string>();
        public readonly List<Card> Graves=new List<Card>(), OmenGraves=new List<Card>();
        public readonly int[] Bonus=new int[2], Moved=new int[2];
    }
    public sealed class Prophecy
    {
        public Card Card;
        public int Round;
        public bool Spent, Shifted;
    }
    public sealed class Move
    {
        public readonly ActionKind Kind;
        public readonly int Slot, Lane, Target;
        public Move(ActionKind kind, int slot=-1,int lane=-1,int target=0) { Kind=kind; Slot=slot; Lane=lane; Target=target; }
        public bool Same(Move b)=>b!=null&&Kind==b.Kind&&Slot==b.Slot&&Lane==b.Lane&&Target==b.Target;
    }
    public sealed class Beat
    {
        public readonly string Text, Sound;
        public Beat(string text,string sound) { Text=text; Sound=sound; }
    }

    public sealed partial class Match
    {
        public readonly Pilot[] Players={new Pilot(),new Pilot()};
        public readonly List<Piece> Board=new List<Piece>();
        public readonly List<Beat> Log=new List<Beat>();
        // Presentation can read successful draws without inferring them from hand size.
        internal readonly List<(int Side, string Id, bool Kept)> DrawLog = new List<(int Side, string Id, bool Kept)>();
        public readonly Prophecy[] Prophecies=new Prophecy[2];
        public int Round {get;private set;}
        public int Turn {get;private set;}
        public int Winner {get;private set;}=-1;
        public int Contested => Round<2?-1:Round%2;
        public int Capacity=>Math.Min(10,Round);
        public bool Opening {get;private set;}=true;
        public bool AwaitingSecondPass => passes == 1 && !Opening && Winner < 0;
        private readonly Queue<Action> pending=new Queue<Action>();
        private readonly HashSet<string> once=new HashSet<string>();
        private int nextKey=1, initiative, passes;
        private readonly Random random;
        private bool dimmed, shiftedOmen;
        public readonly CampaignRules Rules;
        public Match(string[] player,string[] opponent,string hero,string enemyHero,string first,string second,int seed,int opponentHealth=24,CampaignRules rules=null)
        {
            Rules=rules??CampaignRules.Empty;
            random=new Random(seed);
            initiative=random.Next(2);
            Players[0].Hero=hero; Players[1].Hero=enemyHero;
            Players[0].MaxHp=Rules.MaxHeroHealth;
            Players[0].Hp=Rules.MaxHeroHealth;
            Players[1].MaxHp=opponentHealth;
            Players[1].Hp=opponentHealth;
            for(int p=0;p<2;p++)
            {
                Players[p].Pile.AddRange(p==0?player:opponent);
                for(int i=Players[p].Pile.Count-1;i>0;i--)
                {
                    int j=random.Next(i+1);
                    string hold=Players[p].Pile[i];
                    Players[p].Pile[i]=Players[p].Pile[j]; Players[p].Pile[j]=hold;
                }
                Draw(p,4);
            }
            Prophecies[0]=new Prophecy{Card=Catalog.Get(first),Round=3};
            Prophecies[1]=new Prophecy{Card=Catalog.Get(second),Round=5};
        }
        private readonly bool[] openingHandChosen=new bool[2];
        public void Keep(IEnumerable<int> replace)
        {
            ChooseOpeningHand(0,replace);
            ChooseOpeningHand(1,Array.Empty<int>());
        }
        public bool Surrender(int side)
        {
            if(side<0||side>1||Winner>=0)return false;
            Opening=false;Winner=1-side;Say("A player conceded.");return true;
        }
        public bool ChooseOpeningHand(int side,IEnumerable<int> replace)
        {
            if(!Opening||side<0||side>1||openingHandChosen[side])return false;
            var p=Players[side];
            var old=new List<string>();
            foreach(int i in replace.Distinct().Where(i=>i>=0&&i<p.Hand.Count).OrderByDescending(i=>i))
            {old.Add(p.Hand[i]);p.Hand.RemoveAt(i);}
            Draw(side,old.Count);
            foreach(string id in old)p.Pile.Insert(random.Next(p.Pile.Count+1),id);
            openingHandChosen[side]=true;
            if(openingHandChosen.All(x=>x)){Opening=false;NextRound();}
            return true;
        }
        public IEnumerable<Piece> Units(int p=-1,int lane=-1)=>Board.Where(u=>u.Card.Kind==Kind.Unit&&(p<0||u.Side==p)&&(lane<0||u.Lane==lane));
        public Piece Find(int key)=>Board.FirstOrDefault(u=>u.Key==key);
        private IEnumerable<Piece> Sites(int p=-1,int lane=-1)=>Board.Where(u=>u.Card.Kind==Kind.Site&&(p<0||u.Side==p)&&(lane<0||u.Lane==lane));
        private bool Room(int p,int lane)=>Units(p,lane).Count()<3;
        private static Piece Best(IEnumerable<Piece> set)=>set.OrderByDescending(x=>x.Attack*2+x.Hp).ThenBy(x=>x.Key).FirstOrDefault();
        private bool First(Piece p,string key)=>once.Add(p.Key+":"+key);
        private IEnumerable<Piece> InInitiative(IEnumerable<Piece> units)=>units.OrderBy(u=>u.Side==initiative?0:1).ThenBy(u=>u.Key);
        private void Say(string words,string cue=null)=>Log.Add(new Beat(words,cue));
        public int Cost(int p,Card c)
        {
            int baseCost = Math.Max(0, c.Cost - Math.Min(c.Unchain, Players[1 - p].Seals));
            return Math.Max(0, baseCost - CampaignOrderDiscount(p, c));
        }
        public int SealPower(int p,int lane)=>Units(p,lane).Sum(x=>Math.Max(0,x.Attack-(dimmed&&x.Spent?1:0)))+Players[p].Bonus[lane];
        private void NextRound()
        {
            Round++;Turn=initiative;passes=0;dimmed=false;once.Clear();
            foreach(var unit in Board){unit.Spent=false;unit.Temp=0;}
            foreach(var p in Players){p.Charge=Capacity;p.Dead=0;Array.Clear(p.Bonus,0,2);Array.Clear(p.Moved,0,2);}
            if(Round>1) DrawBoth();
            Say("Round "+Round+(Contested<0?"":" · "+(Contested==0?"West":"East")+" Beacon"));
        }
        private void DrawBoth()
        {
            TakeDraw(initiative);TakeDraw(1-initiative);Check();
        }
        internal void Draw(int side,int count=1)
        {
            for(int i=0;i<count&&Winner<0;i++){TakeDraw(side);Check();}
        }
        private void TakeDraw(int side)
        {
            var p=Players[side];
            if(p.Pile.Count==0){p.Fatigue++;p.Hp-=p.Fatigue;Say("Fatigue deals "+p.Fatigue+" Bastion damage.");return;}
            var id=p.Pile[0];p.Pile.RemoveAt(0);
            DrawLog.Add((side,id,p.Hand.Count<8));
            Add(side,id);
        }
        private void Add(int side,string id)
        {
            var p=Players[side];
            if(p.Hand.Count<8)p.Hand.Add(id);
            else{p.Discard.Add(id);Say(Catalog.Get(id).Name+" overflows the hand.");}
        }
        private void DiscardWeak(int side)
        {
            var hand=Players[side].Hand;
            if(hand.Count==0)return;
            int index=Enumerable.Range(0,hand.Count).OrderByDescending(i=>Catalog.Get(hand[i]).Cost).ThenByDescending(i=>i).First();
            Players[side].Discard.Add(hand[index]);hand.RemoveAt(index);
        }
        private void Heal(int side,int n)=>Players[side].Hp=Math.Min(Players[side].MaxHp,Players[side].Hp+n);
        private static void Heal(Piece u,int n){if(u!=null)u.Hp=Math.Min(u.Max,u.Hp+n);}
        private static void Buff(Piece u,int a,int h,bool temp=false){if(u==null)return;if(temp)u.Temp+=a;else u.Power+=a;u.Hp+=h;u.Max+=h;}
        private static void Shield(Piece u){if(u!=null)u.Shield=true;}
        private void Check()
        {
            if(Winner>=0)return;
            Winner=Players[0].Hp<=0&&Players[1].Hp<=0?2:Players[0].Hp<=0?1:Players[1].Hp<=0?0:-1;
        }
        private void HitBastion(int side,int damage)
        {
            Players[side].Hp-=damage;
            ApplyCampaignAfterDamage(side);
            Check();
        }
        private void Forfeit(int side,int amount)
        {
            HitBastion(side,amount);
            foreach(var site in Sites(side).Where(x=>x.Card.Id=="J08").ToArray())
                if(First(site,"forfeit")) pending.Enqueue(()=>Buff(Best(Units(side,site.Lane)),1,0,true));
        }
        internal Piece Place(Card card,int p,int lane)
        {
            if(card.Kind==Kind.Unit&&!Room(p,lane))return null;
            if(card.Kind==Kind.Site&&Sites(p,lane).Any())return null;
            var u=new Piece{Card=card,Side=p,Lane=lane,Key=nextKey++,Hp=card.Health,Max=card.Health,Power=card.Attack,
                Guard=card.Guard,Born=Round,Spent=!card.Rush};
            Board.Add(u);
            return u;
        }
        private Piece Summon(string id,int p,int lane)
        {
            var u=Place(Catalog.Get(id),p,lane);
            if(u!=null)Say(u.Card.Name+" enters the crossing.",id);
            return u;
        }
        private void Ready(Piece u)
        {if(u!=null&&(u.Born!=Round||u.Card.Rush))u.Spent=false;}
        private void Recall(Piece u)
        {
            if(u==null||!Board.Remove(u))return;
            if(u.Card.Faction!='X')Add(u.Side,u.Card.Id);
        }
        private void Shift(Piece u)
        {
            if(u==null||!Board.Contains(u)||!Room(u.Side,1-u.Lane))return;
            u.Lane=1-u.Lane;
            RegisterMove(u);
        }
        private void Swap(Piece a,Piece b)
        {
            if(a==null||b==null||a.Side!=b.Side||a.Lane==b.Lane)return;
            a.Lane=1-a.Lane;b.Lane=1-b.Lane;
            RegisterMove(a);RegisterMove(b);
        }
        private void RegisterMove(Piece u)
        {
            int lane=u.Lane,p=u.Side;
            u.Moved=Round;Players[p].Moved[lane]++;
            if(u.Card.Id=="M10"&&First(u,"move"))pending.Enqueue(()=>Buff(u,1,0));
            foreach(var witness in InInitiative(Board.ToArray()))
            {
                var copy=witness;
                if(witness.Side==p&&witness.Lane==lane&&witness!=u)
                {
                    switch(witness.Card.Id)
                    {
                        case "M05": case "N14":
                            if(First(witness,"move"))pending.Enqueue(()=>Heal(u,copy.Card.Id=="N14"?2:1));
                            break;
                        case "M15":if(First(witness,"move"))pending.Enqueue(()=>Buff(u,1,1));break;
                        case "M08":if(First(witness,"move"))pending.Enqueue(()=>Buff(u,1,0,true));break;
                    }
                }
                if(witness!=u&&witness.Side==p&&lane==Contested&&witness.Card.Id=="M23"&&First(witness,"move"))
                    pending.Enqueue(()=>Buff(u,2,0,true));
                if(witness.Side!=p&&witness.Lane!=lane&&witness.Card.Id=="L08"&&First(witness,"move"))
                    pending.Enqueue(()=>Add(copy.Side,"X08"));
            }
        }
        private bool CanRevive(int p,int lane,int ceiling,bool omen)
        {
            return Room(p,lane)&&(omen?Players[p].OmenGraves:Players[p].Graves).Any(c=>c.Cost<=ceiling&&Players[p].Graves.Contains(c));
        }
        private void Revive(int p,int lane,int ceiling,bool omen=false)
        {
            if(!CanRevive(p,lane,ceiling,omen))return;
            var player=Players[p];
            var card=(omen?player.OmenGraves:player.Graves).Where(c=>c.Cost<=ceiling&&player.Graves.Contains(c))
                .OrderByDescending(c=>c.Cost).ThenByDescending(c=>c.Attack+c.Health).First();
            player.Graves.Remove(card);player.Discard.Remove(card.Id);player.OmenGraves.Remove(card);
            Place(card,p,lane);Say(card.Name+" returns from below.",card.Id);
        }
        private List<Piece> Damage(IEnumerable<Piece> targets,Func<Piece,int> hit,bool omen=false)
        {
            var set=InInitiative(targets.Distinct()).ToArray();
            foreach(var u in set)
            {
                int damage=hit(u);
                if(damage<=0||!Board.Contains(u))continue;
                if(u.Shield){u.Shield=false;QueueShield(u);}
                else u.Hp-=damage;
            }
            return RemoveDead(omen);
        }
        private void QueueShield(Piece u)
        {
            Say(u.Card.Name+" loses Shield.","shield-break");
            if(u.Card.Id=="A06")pending.Enqueue(()=>Buff(u,1,1));
            if(u.Card.Id=="A23"&&First(u,"shield"))pending.Enqueue(()=>Ready(u));
            foreach(var witness in Units(u.Side,u.Lane).Where(x=>x!=u&&x.Card.Id=="A09").ToArray())
                if(First(witness,"shield"))pending.Enqueue(()=>Buff(u,1,0));
        }
        private List<Piece> RemoveDead(bool omen=false)
        {
            var lost=InInitiative(Units().Where(x=>x.Hp<=0)).ToList();
            foreach(var u in lost)
            {
                Board.Remove(u);var p=Players[u.Side];p.Dead++;if(omen)p.OmenDead++;
                if(u.Card.Faction!='X'){p.Graves.Add(u.Card);p.Discard.Add(u.Card.Id);if(omen)p.OmenGraves.Add(u.Card);}
                ApplyCampaignAfterDeath(u);
            }
            foreach(var u in lost)
            {
                pending.Enqueue(()=>Farewell(u));
                foreach(var w in InInitiative(Board.Where(x=>x.Side==u.Side&&x.Lane==u.Lane).ToArray()))
                {
                    if(w.Card.Id=="H03"&&First(w,"death"))pending.Enqueue(()=>Heal(w.Side,1));
                    if(w.Card.Id=="H08"&&u.Card.Faction!='X'&&First(w,"death"))pending.Enqueue(()=>Buff(Best(Units(w.Side,w.Lane)),1,1));
                    if(w.Card.Id=="H10"&&First(w,"death"))pending.Enqueue(()=>Buff(w,1,0));
                    if(w.Card.Id=="H24"&&First(w,"death"))pending.Enqueue(()=>Damage(new[]{Best(Units(1-w.Side,w.Lane))}.Where(x=>x!=null),x=>2));
                }
            }
            return lost;
        }
        private void Kill(Piece u)
        {
            if(u==null)return;
            if(u.Card.Kind==Kind.Site){Board.Remove(u);Players[u.Side].Discard.Add(u.Card.Id);}
            else{u.Hp=0;RemoveDead();}
        }
        private void Drain()
        {
            while(pending.Count>0&&Winner<0)pending.Dequeue()();
            if(Winner>=0)pending.Clear();
        }
        private void EndRound()
        {
            foreach(var prophecy in Prophecies.Where(x=>!x.Spent&&x.Round==Round).ToArray())
            {
                ResolveProphecy(prophecy);
                if(Winner>=0)return;
            }
            foreach(var u in InInitiative(Board).ToArray())pending.Enqueue(()=>AtEnd(u));
            Drain();
            if(Winner>=0)return;
            if(Contested>=0)
            {
                int a=SealPower(0,Contested),b=SealPower(1,Contested);
                int winner=a==b?-1:a>b?0:1;
                if(winner>=0&&Units(winner,Contested).Any())
                {
                    Players[winner].Seals++;Say("Player "+(winner+1)+" claimed a Seal.","beacon-claim");
                    ApplyCampaignAfterSeal(winner);
                    if(Players[winner].Seals>=6){Winner=winner;return;}
                }
            }
            ApplyCampaignRoundEnd();
            initiative=1-initiative;NextRound();
        }
        internal void ResolveProphecy(Prophecy prophecy)
        {
            prophecy.Spent=true;
            Say(prophecy.Card.Name+" resolves.","prophecy-reveal");
            Say(prophecy.Card.Name+" speaks.",prophecy.Card.Id);
            ApplyCampaignAfterProphecy();
            foreach(var p in Players){p.OmenGraves.Clear();p.OmenDead=0;}
            switch(prophecy.Card.Id)
            {
                case "P01":Damage(Units(),u=>2,true);break;
                case "P02":foreach(var u in Units())u.Spent=true;dimmed=true;break;
                case "P03":
                    if(Contested>=0)
                    {
                        var selected=new[]{initiative,1-initiative}.Where(p=>Room(p,1-Contested))
                            .Select(p=>Best(Units(p,Contested))).Where(u=>u!=null).ToArray();
                        foreach(var u in selected)u.Lane=1-u.Lane;
                        foreach(var u in selected)RegisterMove(u);
                    }
                    break;
                case "P04":if(Contested>=0){Summon("X05",initiative,Contested);Summon("X05",1-initiative,Contested);}break;
                case "P05":Damage(Units(),u=>u.Lane==Contested?3:1,true);break;
                case "P06":
                    bool[] had={Sites(0).Any(),Sites(1).Any()};
                    foreach(var u in Sites().ToArray())Kill(u);
                    if(!had[initiative])Draw(initiative);
                    if(!had[1-initiative])Draw(1-initiative);
                    break;
            }
            foreach(var witness in InInitiative(Board).ToArray())pending.Enqueue(()=>Omenbound(witness));
            Drain();
        }
        private bool MoveOmen(bool later)
        {
            var omen=Prophecies.Where(x=>!x.Spent).OrderBy(x=>x.Round).FirstOrDefault();
            if(omen==null||omen.Shifted)return false;
            int next=omen.Round+(later?1:-1);
            if(next<Math.Max(2,Round)||next>6)return false;
            omen.Round=next;omen.Shifted=shiftedOmen=true;
            Say(omen.Card.Name+" moved to round "+next+".");return true;
        }
    }
}
