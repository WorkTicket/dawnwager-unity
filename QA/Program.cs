using System;
using System.Collections.Generic;
using System.Linq;
using Wolfsun;

static class Program
{
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);}
    static Match Create(string first="P01",string second="P02",int seed=37,int health=24,string hero="A23",string foe="M23",string[] player=null,string[] enemy=null)
        =>new Match(player??Campaign.Starter,enemy??Campaign.EnemyDeck(0),hero,foe,first,second,seed,health);
    static void PassRound(Match game)
    {
        Check(game.Apply(game.Turn,new Move(ActionKind.Pass)),"First pass is legal");
        Check(game.Apply(game.Turn,new Move(ActionKind.Pass)),"Second pass is legal");
    }
    static void Main()
    {
        foreach(var size in new[]{(1920,1080),(2560,1440),(1920,1200),(1280,960),(3440,1440),(3840,1080)})
        {
            var crop=ViewportLayout.ArtworkCrop(size.Item1,size.Item2,1672,941);
            Check(crop.Width>0&&crop.Height>0&&crop.Width<=1&&crop.Height<=1,"viewport crop is valid");
            Check(Math.Abs(1672*crop.Width/(941*crop.Height)-(float)size.Item1/size.Item2)<.001f,
                "edge-to-edge art keeps pixel proportions");
            Check(crop.X>=0&&crop.Y>=0&&crop.X+crop.Width<=1.001f&&crop.Y+crop.Height<=1.001f,
                "viewport does not sample outside the artwork");
        }
        var cold=ViewportLayout.ArtworkCrop(0,0,1672,941);
        Check(cold.Width==1&&cold.Height==1,"pre-layout viewport returns full UV");
        Console.WriteLine("PASS full-bleed aspect crop at standard, tall, and ultrawide sizes.");

        Check(Campaign.ChapterAtProgress(0)==0&&Campaign.CanPlayChapter(0,0),"fresh save can enter chapter one");
        Check(!Campaign.CanPlayChapter(1,0)&&Campaign.CanPlayChapter(1,1),"boss win unlocks next chapter");
        Check(Campaign.ChapterAtProgress(Campaign.Chapters.Length)==Campaign.Chapters.Length-1,"completed campaign opens final chapter");
        Check(!Campaign.CanPlayChapter(-1,0)&&!Campaign.CanPlayChapter(Campaign.Chapters.Length,Campaign.Chapters.Length),"out-of-range chapters rejected");
        Console.WriteLine("PASS campaign chapter routing for fresh, unlocked, and complete profiles.");
        Check(Catalog.All.Count(c=>c.Faction!='P'&&c.Faction!='X')==144,"144 deckbuildable cards");
        Check(Catalog.All.Select(c=>c.Id).Distinct().Count()==Catalog.All.Length,"unique IDs");
        Check(Catalog.All.All(c=>!string.IsNullOrWhiteSpace(RulesText.Describe(c))),"every card has inspection text");
        Check(RulesText.Terms(Catalog.Get("A03")).Any(t=>t.Key=="Shield"),"Shield keyword in inspection");
        Check(RulesText.Describe(Catalog.Get("P03")).Contains("strongest"),"Tide inspection describes automatic selection");
        Check(RulesText.Describe(Catalog.Get("L06")).Contains("cheaper"),"Rumor inspection describes automatic deck ordering");
        var passState=Create();
        Check(!passState.AwaitingSecondPass,"opening hand has no pass");
        passState.Keep(Array.Empty<int>());
        Check(passState.Apply(passState.Turn,new Move(ActionKind.Pass))&&passState.AwaitingSecondPass,"next pass ends round");
        Check(passState.Apply(passState.Turn,new Move(ActionKind.Pass))&&!passState.AwaitingSecondPass,"new round clears pass notice");
        Console.WriteLine("PASS inspection text, keyword help and live pass notice.");
        var drawTest=Create();
        drawTest.Keep(Array.Empty<int>());
        int drawOffset=drawTest.DrawLog.Count;
        int[] remaining=drawTest.Players.Select(player=>player.Pile.Count).ToArray();
        PassRound(drawTest);
        var draws=drawTest.DrawLog.Skip(drawOffset).ToArray();
        Check(draws.Length==2&&draws.All(draw=>draw.Kept),"one kept draw per side");
        Check(draws.All(draw=>drawTest.Players[draw.Side].Hand.Last()==draw.Id),"displayed draw agrees with the hand");
        Check(draws.All(draw=>drawTest.Players[draw.Side].Pile.Count==remaining[draw.Side]-1),"one card removed per visible draw");
        var full=drawTest.Players[0];
        while(full.Hand.Count<8)full.Hand.Add("A01");
        drawTest.Draw(0);
        Check(full.Hand.Count==8&&!drawTest.DrawLog.Last().Kept&&full.Pile.Count==remaining[0]-2,"full hand draws from deck without adding a card");
        Console.WriteLine("PASS draw events, pile counts, kept cards, and full-hand overflow.");
        var owned=new HashSet<string>(Campaign.Starter);
        Check(Campaign.LegalDeck(Campaign.Starter,owned),"starter valid");
        Check(owned.Count==15,"only 15 cards collected on first launch");
        Check(!Campaign.LegalDeck(Enumerable.Repeat("A01",30),owned),"copy limits");
        Check(!Campaign.LegalDeck(Campaign.EnemyDeck(0),owned),"locked cards rejected");
        for(int i=0;i<Campaign.Chapters.Length;i++)
        {
            var d=Campaign.EnemyDeck(i);
            Check(Campaign.LegalDeck(d,Catalog.All.Select(c=>c.Id)),"legal boss deck "+i);
            int before=owned.Count;owned.UnionWith(Campaign.Rewards(i));
            Check(owned.Count>before,"each win earns cards "+i);
            int after=owned.Count;owned.UnionWith(Campaign.Rewards(i));
            Check(after==owned.Count,"replayed reward is idempotent");
        }
        Check(owned.Count==144,"all cards obtainable");

        for (int mask=0; mask<16; mask++)
        {
            var mulligan=Create();
            var original=mulligan.Players[0].Hand.ToArray();
            var slots=Enumerable.Range(0,4).Where(slot=>(mask&(1<<slot))!=0).ToArray();
            var kept=original.Where((_,slot)=>!slots.Contains(slot));
            int startDraw=mulligan.DrawLog.Count;
            mulligan.Keep(slots);
            var replacements=mulligan.DrawLog.Skip(startDraw).Where(draw=>draw.Side==0).Select(draw=>draw.Id);
            Check(kept.Concat(replacements).SequenceEqual(mulligan.Players[0].Hand),"mulligan flight order follows final hand");
            Check(mulligan.Round==1&&mulligan.Players[0].Pile.Count==26,"mulligan resolves exactly once and preserves pile size");
        }
        Console.WriteLine("PASS all 16 opening-hand keep and replacement patterns.");
        var demo=Create();demo.Keep(new[]{0,2});Check(demo.Players[0].Hand.Count==4&&demo.Players[0].Pile.Count==26,"mulligan count");
        int turn=demo.Turn;
        Check(!demo.Apply(1-turn,new Move(ActionKind.Pass)),"reject wrong turn");
        Check(demo.Round==1&&demo.Capacity==1,"round one resource");
        PassRound(demo);Check(demo.Round==2&&demo.Contested==0,"West round two");
        PassRound(demo);Check(demo.Round==3&&demo.Contested==1,"East round three");
        var keeper=demo.Place(Catalog.Get("A06"),0,1);keeper.Shield=true;
        demo.Place(Catalog.Get("H01"),1,1);
        demo.Place(Catalog.Get("A01"),1,1);
        demo.ResolveProphecy(demo.Prophecies[0]);
        Check(keeper.Hp==3&&keeper.Attack==3&&!keeper.Shield,"Ringkeeper grows after shield break");
        Check(demo.Units(1,1).Any(x=>x.Card.Id=="X05"),"Candle Bearer creates soul");
        Check(demo.Units(1,1).Single(x=>x.Card.Id=="A01").Hp==1,"simultaneous damage applied");
        Console.WriteLine("PASS scripted Ringkeeper vs Candle Bearer Fimbulwinter sequence.");

        var fog=Create("P02");fog.Keep(Array.Empty<int>());PassRound(fog);PassRound(fog);
        var guard=fog.Place(Catalog.Get("A01"),0,1);
        fog.ResolveProphecy(fog.Prophecies[0]);
        Check(guard.Spent&&fog.SealPower(0,1)==0,"Sun Goes Out exhausts and dims");
        var sea=Create("P03");sea.Keep(new int[0]);PassRound(sea);PassRound(sea);
        var sailor=sea.Place(Catalog.Get("M06"),0,1);
        sea.ResolveProphecy(sea.Prophecies[0]);Check(sailor.Lane==0,"Sea moves unit");
        var dead=Create("P04");dead.Keep(new int[0]);PassRound(dead);PassRound(dead);
        dead.ResolveProphecy(dead.Prophecies[0]);Check(dead.Units(-1,1).Count()==2,"Dead Knock adds two souls");
        var bridge=Create("P05");bridge.Keep(new int[0]);PassRound(bridge);PassRound(bridge);
        var sturdy=bridge.Place(Catalog.Get("A01"),0,0);bridge.Place(Catalog.Get("A01"),1,1);
        bridge.ResolveProphecy(bridge.Prophecies[0]);
        Check(sturdy.Hp==2&&!bridge.Units(1,1).Any(),"Bridge damage differs by lane");
        var gate=Create("P06");gate.Keep(new int[0]);PassRound(gate);PassRound(gate);
        gate.Place(Catalog.Get("A08"),0,0);
        var initial=gate.Players.Select(p=>p.Hand.Count).ToArray();
        gate.ResolveProphecy(gate.Prophecies[0]);
        Check(!gate.Board.Any()&&gate.Players[0].Hand.Count==initial[0]&&gate.Players[1].Hand.Count==initial[1]+1,"Gates Burn destroys sites and draws for eligible side");
        Console.WriteLine("PASS dedicated resolution checks for all six prophecies.");

        var seals=Create("P06","P04");seals.Keep(new int[0]);seals.Place(Catalog.Get("J23"),0,0);seals.Place(Catalog.Get("J23"),0,1);
        while(seals.Winner<0&&seals.Round<=8)PassRound(seals);
        Check(seals.Winner==0&&seals.Players[0].Seals==6&&seals.Round==7,"six Seals end game in round seven");
        var bastion=Create(health:2);bastion.Keep(new int[0]);var attacker=bastion.Place(Catalog.Get("J23"),0,0);
        PassRound(bastion);
        if(bastion.Turn==1)Check(bastion.Apply(1,new Move(ActionKind.Pass)),"enemy passes");
        Check(bastion.Apply(0,new Move(ActionKind.Attack,attacker.Key,0))&&bastion.Winner==0,"Bastion damage ends game");
        Console.WriteLine("PASS both victory routes, deck rules and reward gating.");

        int exercised=0;
        foreach(var c in Catalog.All.Where(x=>x.Kind!=Kind.Prophecy&&!(x.Faction=='X'&&x.Kind==Kind.Unit)))
        {
            var match=Create("P06","P04",hero:"A23",foe:"H23");
            match.Keep(new int[0]);
            for(int n=0;n<7;n++)PassRound(match);
            if(match.Turn==1)match.Apply(1,new Move(ActionKind.Pass));
            match.Players[0].Hand.Clear();match.Players[0].Hand.Add(c.Id);
            var ally=match.Place(Catalog.Get("A01"),0,0);
            var secondAlly=match.Place(Catalog.Get("A05"),0,1);
            var foe=match.Place(Catalog.Get("J21"),1,0);
            var secondFoe=match.Place(Catalog.Get("H01"),1,1);
            var site=match.Place(Catalog.Get("H03"),1,1);
            foe.GuardStrike=match.Round;foe.Shield=true;secondFoe.Spent=true;
            match.Players[0].Graves.Add(Catalog.Get("H01"));
            match.Players[0].Discard.Add("H01");
            Move play=match.Legal(0).FirstOrDefault(x=>x.Kind==ActionKind.Play);
            Check(play!=null&&match.Apply(0,play),"play path for "+c.Id);
            exercised++;
        }
        Check(exercised==147,"all hand-playable cards");
        foreach(var hero in Hero.All)
        {
            var match=Create(hero:hero.Id);
            match.Keep(new int[0]);PassRound(match);
            if(match.Turn==1)match.Apply(1,new Move(ActionKind.Pass));
            var ally=match.Place(Catalog.Get("A01"),0,0);ally.Hp=1;
            match.Place(Catalog.Get("J18"),1,0);
            var power=match.Legal(0).FirstOrDefault(x=>x.Kind==ActionKind.Power);
            Check(power!=null&&match.Apply(0,power),"hero power "+hero.Id);
            Check(match.Players[0].Charge==0,"charge spent once");
        }
        Console.WriteLine("PASS every hand-playable card path and all five hero powers.");
        int runs=0,wins=0; var past=new HashSet<string>(Campaign.Starter);
        
        for(int i=0;i<Campaign.Chapters.Length;i++)
        {
            var c=Campaign.Chapters[i];int chapterWins=0;
            for(int seed=0;seed<24;seed++)
            {
                var ally=i>=5?"J23":i>=3?"H23":"A23";
                var saveDeck=Campaign.Recommend(past,ally);
                Check(Campaign.LegalDeck(saveDeck,past),"suggested deck legal");
                var match=Create(c.First,c.Second,233+seed,c.Health,ally,c.Hero,saveDeck,Campaign.EnemyDeck(i));
                match.Keep(match.Players[0].Hand.Select((id,slot)=>(id,slot)).Where(x=>Catalog.Get(x.id).Cost>3).Select(x=>x.slot));
                int actions=0;
                while(match.Winner<0&&actions++<1600)
                {
                    int p=match.Turn;var move=match.ChooseAI(p);
                    Check(move!=null&&match.Apply(p,move),"AI legal move");
                    Check(match.Board.GroupBy(x=>(x.Side,x.Lane,x.Card.Kind)).All(g=>g.Count()<=(g.Key.Kind==Kind.Site?1:3)),"slot capacity");
                    Check(match.Players.All(x=>x.Hand.Count<=8&&x.Charge>=0),"hand and Charge invariants");
                    Check(match.Units().All(x=>x.Hp>0&&x.Hp<=x.Max),"health invariant");
                }
                Check(match.Winner>=0,"AI match terminates");
                if(match.Winner==0){chapterWins++;wins++;}
                runs++;
            }
            Console.WriteLine("Chapter "+(i+1)+" · "+chapterWins+"/24 wins by progression-deck AI.");
            Check(chapterWins>0,"chapter is winnable with available rewards");
            past.UnionWith(Campaign.Rewards(i));
        }
        Console.WriteLine($"PASS {runs} completed campaign simulations ({wins} wins by progression-deck AI).");

        // Campaign RPG hooks — MaxHp, empty rules for casual, discount peek does not consume.
        var boosted = new CampaignRules(new[] { "B1", "B5" }, Array.Empty<string>(), "C01");
        Check(boosted.MaxHeroHealth == 29, "Bastion match-start health bonuses stack");
        var campMatch = new Match(Campaign.Starter, Campaign.EnemyDeck(0), "A23", "M23", "P01", "P02", 11, 24, boosted);
        Check(campMatch.Players[0].MaxHp == 29 && campMatch.Players[0].Hp == 29, "campaign MaxHp applied at match start");
        var casual = new Match(Campaign.Starter, Campaign.EnemyDeck(0), "A23", "M23", "P01", "P02", 12, 24, CampaignRules.Empty);
        Check(casual.Players[0].MaxHp == 24 && casual.Players[0].Hp == 24, "casual match ignores campaign rules");
        Check(CampaignMeta.Encounters.Length >= 10, "encounters CSV/fallback loaded");
        Check(CampaignMeta.Passives.Length == 18, "18 skill nodes");
        Check(CampaignMeta.Relics.Length == 4, "four test relics");
        var r1 = new CampaignRules(new[] { "R1" }, Array.Empty<string>(), "C01");
        var costPeek = new Match(Campaign.Starter, Campaign.EnemyDeck(0), "A23", "M23", "P01", "P02", 13, 24, r1);
        costPeek.Keep(Array.Empty<int>());
        costPeek.Players[0].Hand.Clear();
        costPeek.Players[0].Hand.Add("A03");
        costPeek.Players[0].Charge = 10;
        var order = Catalog.Get("A03");
        int firstCost = costPeek.Cost(0, order);
        int secondCost = costPeek.Cost(0, order);
        Check(firstCost == secondCost, "Cost preview does not consume campaign discount");
        Check(firstCost == Math.Max(0, order.Cost - 1), "R1 discounts first Order by 1 in preview");
        Console.WriteLine("PASS campaign MaxHp, casual isolation, and non-consuming Cost previews.");
    }
}
