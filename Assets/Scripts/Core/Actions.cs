using System;
using System.Collections.Generic;
using System.Linq;

namespace Wolfsun
{
    public sealed partial class Match
    {
        private IEnumerable<Move> PlayTargets(int p,int slot,Card c)
        {
            int e=1-p;
            Move Route(int lane,int key=0)=>new Move(ActionKind.Play,slot,lane,key);
            if(c.Kind==Kind.Unit||c.Kind==Kind.Site)
            {
                for(int lane=0;lane<2;lane++)
                    if(c.Kind==Kind.Unit?Room(p,lane):!Sites(p,lane).Any())yield return Route(lane);
                yield break;
            }
            IEnumerable<Piece> goals=null;
            var allies=Units(p);var enemies=Units(e);
            switch(c.Id)
            {
                case "A03":case "A04":case "J04":case "H02":case "H07":case "L16":goals=allies;break;
                case "N03":yield return Route(-1);goals=allies;break;
                case "A12":case "J03":case "J17":case "H12":case "L02":case "L17":case "X08":case "X09":goals=enemies;break;
                case "A16":goals=enemies.Where(u=>u.GuardStrike==Round);break;
                case "M03":case "X10":goals=allies.Where(u=>Room(p,1-u.Lane)&&u.Card.Cost<=(c.Id=="M03"?2:3));break;
                case "M07":case "N07":goals=allies.Where(u=>Room(p,1-u.Lane));break;
                case "M16":goals=allies.Where(u=>allies.Any(x=>x.Lane!=u.Lane));break;
                case "L21":goals=enemies.Where(u=>enemies.Any(x=>x.Lane!=u.Lane));break;
                case "J11":goals=enemies.Concat(Sites());break;
                case "N17":goals=enemies.Concat(Sites(e));break;
                case "L03":goals=enemies.Where(u=>u.Shield);break;
                case "L07":goals=Units().Where(u=>Room(u.Side,1-u.Lane));break;
                case "L09":goals=Units().Where(u=>u.Card.Cost<=3);break;
                case "L20":goals=enemies.Where(u=>u.Attack<=3);break;
                case "N08":goals=enemies.Where(u=>u.Spent);break;
                case "N23":goals=enemies.Where(u=>u.Attack>=5);break;
                case "L11":
                    yield return Route(0);yield return Route(1);yield break;
                case "H11":case "H18":
                    for(int l=0;l<2;l++)if(CanRevive(p,l,c.Id=="H11"?2:4,false))yield return Route(l);
                    yield break;
                case "A07":case "A11":case "A14":case "A18":case "M04":case "M11":case "M13":case "M18":case "M22":
                case "J14":case "J20":case "H14":case "H20":case "L14":case "N12":case "N18":
                    yield return Route(0);yield return Route(1);yield break;
            }
            if(goals!=null)
                foreach(var u in goals)yield return Route(u.Lane,u.Key);
            else yield return Route(-1);
        }
        public List<Move> Legal(int p)
        {
            var moves=new List<Move>();
            if(Opening||Winner>=0||Turn!=p)return moves;
            var pilot=Players[p];
            for(int i=0;i<pilot.Hand.Count;i++)
            {
                var c=Catalog.Get(pilot.Hand[i]);
                if(Cost(p,c)<=pilot.Charge)moves.AddRange(PlayTargets(p,i,c));
            }
            foreach(var u in Units(p).Where(u=>!u.Spent))
            {
                if(Room(p,1-u.Lane))moves.Add(new Move(ActionKind.March,u.Key,1-u.Lane));
                var enemies=Units(1-p,u.Lane).ToArray();
                if(enemies.Length==0&&u.Born!=Round)moves.Add(new Move(ActionKind.Attack,u.Key,u.Lane));
                else
                    foreach(var e in enemies.Where(x=>x.Guard||!enemies.Any(y=>y.Guard)))
                        moves.Add(new Move(ActionKind.Attack,u.Key,u.Lane,e.Key));
            }
            if(pilot.Charge>=2&&pilot.PowerRound!=Round)
            {
                switch(pilot.Hero)
                {
                    case "A23":case "H23":
                        foreach(var u in Units(p).Where(u=>pilot.Hero=="A23"?!u.Shield:u.Hp<u.Max))moves.Add(new Move(ActionKind.Power,-1,u.Lane,u.Key));
                        if(pilot.Hero=="H23"&&pilot.Hp<pilot.MaxHp)moves.Add(new Move(ActionKind.Power));
                        break;
                    case "M23":
                        for(int l=0;l<2;l++)if(Room(p,l))moves.Add(new Move(ActionKind.Power,-1,l));
                        break;
                    case "J23":case "L23":
                        foreach(var u in Units(1-p))moves.Add(new Move(ActionKind.Power,-1,u.Lane,u.Key));
                        break;
                }
            }
            moves.Add(new Move(ActionKind.Pass));
            return moves;
        }
        public bool Apply(int p,Move m)
        {
            if(!Legal(p).Any(x=>x.Same(m)))return false;
            if(m.Kind==ActionKind.Pass)
            {
                Say(p==0?"You pass.":"Enemy passes.","pass-turn");
                passes++; if(passes==2)EndRound(); else Turn=1-p;
                return true;
            }
            passes=0;
            var self=Players[p];var target=Find(m.Target);var u=Find(m.Slot);
            var attackFrom=u;
            switch(m.Kind)
            {
                case ActionKind.Play:
                    var c=Catalog.Get(self.Hand[m.Slot]);
                    int charged=Cost(p,c);
                    self.Charge-=charged;self.Hand.RemoveAt(m.Slot);
                    ConsumeCampaignOrderDiscount(p,c);
                    Say(c.Name+" played.",c.Id);
                    Piece placed=null;
                    if(c.Kind==Kind.Order)
                    {
                        Order(c,p,m.Lane,target);
                        self.Discard.Add(c.Id);
                        if(c.Cost==1)
                            foreach(var unit in Units(p).Where(x=>x.Card.Id=="L18").ToArray())
                                if(First(unit,"order"))pending.Enqueue(()=>Buff(unit,1,0,true));
                    }
                    else
                    {
                        placed=Place(c,p,m.Lane);
                        Deploy(placed);
                    }
                    ApplyCampaignAfterPlay(p,c,placed,m.Lane);
                    break;
                case ActionKind.March:
                    u.Spent=true;Shift(u);Say(u.Card.Name+" marches to "+(u.Lane==0?"West.":"East."));
                    ApplyCampaignAfterMarch(p,u);
                    break;
                case ActionKind.Attack:
                    u.Spent=true;
                    if(target==null){HitBastion(1-p,u.Attack);Say(u.Card.Name+" struck the Bastion.","unit-hit");}
                    else
                    {
                        if(target.Guard)u.GuardStrike=Round;
                        int a=u.Attack,b=target.Attack;
                        var lost=Damage(new[]{u,target},x=>x==u?b:a);
                        if(attackFrom.Card.Id=="J23"&&lost.Contains(target))pending.Enqueue(()=>Heal(p,2));
                        Say(u.Card.Name+" attacked "+target.Card.Name+".","unit-hit");
                    }
                    ApplyCampaignAfterAttack(p);
                    break;
                case ActionKind.Power:
                    self.Charge-=2;self.PowerRound=Round;
                    Say(Hero.Info(self.Hero).Power+" invoked.","card-select");
                    switch(self.Hero)
                    {
                        case "A23":Shield(target);break;
                        case "M23":Summon("X02",p,m.Lane);break;
                        case "J23":Damage(new[]{target},x=>2);break;
                        case "H23":if(target==null)Heal(p,2);else Heal(target,3);break;
                        case "L23":Buff(target,-2,0,true);break;
                    }
                    ApplyCampaignAfterPower(p);
                    break;
            }
            Drain();if(Winner<0)Turn=1-p;
            return true;
        }
        /// <summary>difficulty: 0 Easy, 1 Normal, 2 Ranked-sim Hard.</summary>
        public Move ChooseAI(int p,int difficulty=1)
        {
            var moves=Legal(p);
            if(moves.Count==0)return null;
            if(difficulty<=0)
            {
                var soft=moves.Where(m=>m.Kind!=ActionKind.Pass).ToList();
                if(soft.Count==0)return moves[0];
                return soft[random.Next(soft.Count)];
            }
            float Score(Move m)
            {
                var t=Find(m.Target);
                if(m.Kind==ActionKind.Pass)return .1f;
                if(m.Kind==ActionKind.Attack)
                {
                    var u=Find(m.Slot);
                    if(t==null)return Players[1-p].Hp<=u.Attack?1000:2.2f+u.Attack*.81f;
                    return 1+Math.Min(t.Hp,u.Attack)*.48f+(u.Attack>=t.Hp&&!t.Shield?3f:0)
                        -(t.Attack>=u.Hp&&!u.Shield?2.9f:0)+(t.Shield&&u.Attack>0?1.5f:0);
                }
                if(m.Kind==ActionKind.March)
                {
                    if(m.Lane!=Contested)return -.6f;
                    var u=Find(m.Slot);
                    bool turnSeal=SealPower(p,m.Lane)<=SealPower(1-p,m.Lane)&&SealPower(p,m.Lane)+u.Attack>SealPower(1-p,m.Lane);
                    return turnSeal?4.5f+u.Attack*.3f:-.15f;
                }
                if(m.Kind==ActionKind.Power)
                {
                    if(Players[p].Hero=="J23")return t!=null&&!t.Shield&&t.Hp<=2?5.5f:1.4f;
                    if(Players[p].Hero=="A23")return t!=null&&t.Lane==Contested?2f:.75f;
                    if(Players[p].Hero=="H23")return (t==null?Players[p].Hp<21:t.Max-t.Hp>=2)?2.0f:.4f;
                    if(Players[p].Hero=="L23")return t!=null&&t.Lane==Contested&&t.Attack>0?1.8f:-.5f;
                    return 2.5f+(m.Lane==Contested?.6f:0);
                }
                var c=Catalog.Get(Players[p].Hand[m.Slot]);
                if(c.Kind==Kind.Unit)
                {
                    var now=SealPower(p,m.Lane);var other=SealPower(1-p,m.Lane);
                    return 2.0f+c.Attack*.56f+c.Health*.24f+(c.Guard&&Players[p].Hp<10?1.2f:0)
                        +(m.Lane==Contested&&now<=other&&now+c.Attack>other?2.0f:0)
                        +(m.Lane==1-Contested&&!Units(p,m.Lane).Any()?.62f:0f)
                        -(Units(p,m.Lane).Count()*.6f);
                }
                if(c.Kind==Kind.Site)return 1.42f+(m.Lane==Contested?.7f:0);
                if(t!=null&&t.Side!=p)return 2.5f+t.Attack*.5f+(t.Lane==Contested?.55f:0);
                if(t!=null)return (t.Card.Kind==Kind.Unit?1.4f:1.1f)+(t.Lane==Contested?.45f:0);
                return 1.4f+(m.Lane==Contested?.35f:0);
            }
            var ranked=moves.OrderByDescending(Score).ToList();
            if(difficulty>=2)return ranked[0];
            // Normal: top-3 with light noise so practice isn't a wall.
            int pick=Math.Min(ranked.Count,3);
            return ranked[random.Next(pick)];
        }
        private void Order(Card c,int p,int lane,Piece t)
        {
            var allies=Units(p,lane);var foes=Units(1-p,lane);var w=Players[p];var e=Players[1-p];
            void Hurt(Piece x,int damage){if(x!=null)Damage(new[]{x},_=>damage);}
            switch(c.Id)
            {
                case "A03":Shield(t);break;
                case "A04":Buff(t,2,0,true);if(t.Shield)Heal(t,1);break;
                case "A07":var guards=allies.OrderByDescending(x=>x.Attack).Take(2).ToArray();foreach(var u in guards)Buff(u,0,1);if(guards.Length==2)Shield(guards[0]);break;
                case "A11":foreach(var u in allies){u.Guard=true;Heal(u,2);}break;
                case "A12":Hurt(t,Units(p,t.Lane).Any(x=>x.Shield)?4:3);break;
                case "A14":Summon("X01",p,lane);Summon("X01",p,1-lane);break;
                case "A16":Kill(t);break;
                case "A18":foreach(var u in allies)Shield(u);Ready(Best(allies.Where(u=>u.Spent&&(u.Born!=Round||u.Card.Rush))));break;
                case "A20":Forfeit(p,2);if(Winner<0){Draw(p,2);Shield(Best(Units(p).Where(u=>!u.Shield)));}break;
                case "M03":case "X10":Shift(t);break;
                case "M04":Summon("X02",p,lane);break;
                case "M07":Shift(t);if(t.Lane==Contested)Draw(p);break;
                case "M11":foreach(var a in allies.OrderByDescending(u=>u.Attack).Take(2).ToArray())Shift(a);break;
                case "M12":Buff(Best(Units(p,0)),1,1);Buff(Best(Units(p,1)),1,1);break;
                case "M13":Summon("X02",p,lane);Summon("X02",p,lane);
                    foreach(var u in allies.Where(x=>x.Card.Id=="X02"))Buff(u,1,0,true);break;
                case "M16":var partner=Best(Units(p,1-lane));Swap(t,partner);Buff(t,1,0,true);Buff(partner,1,0,true);Draw(p);break;
                case "M18":foreach(var u in foes)Buff(u,-2,0,true);var exhaust=Best(foes.Where(x=>!x.Spent));if(exhaust!=null)exhaust.Spent=true;break;
                case "M20":Summon("X02",p,0);Summon("X02",p,1);foreach(var u in Units(p).Where(x=>x.Moved==Round))Buff(u,1,1);break;
                case "M22":foreach(var u in allies)Buff(u,0,2);if(lane==Contested)w.Bonus[lane]+=3;break;
                case "J03":Forfeit(p,1);if(Winner<0)Hurt(t,2);break;
                case "J04":Buff(t,2,0,true);if(t.Card.Cost>=5)Shield(t);break;
                case "J07":Forfeit(p,2);if(Winner<0){Draw(p,2);DiscardWeak(p);}break;
                case "J11":case "N17":if(t.Card.Kind==Kind.Site)Kill(t);else Hurt(t,3);break;
                case "J14":Forfeit(p,2);if(Winner<0)Damage(foes,u=>2);break;
                case "J17":Hurt(t,5);if(e.Seals>0)HitBastion(1-p,1);break;
                case "J20":if(Damage(Units(-1,lane),u=>3).Any(x=>x.Side==p))Summon("X03",p,lane);break;
                case "J22":w.Hp-=3;e.Hp-=3;Check();if(Winner<0)Buff(Best(Units(p)),2,0);break;
                case "H02":if(t.Mark!=Round){t.Mark=Round;t.Marks=0;}t.Marks++;break;
                case "H04":Heal(p,w.Dead>0?4:2);break;
                case "H07":Kill(t);Draw(p);Summon("X05",p,lane);break;
                case "H11":Revive(p,lane,2);break;
                case "H12":Hurt(t,w.Dead>0?4:2);break;
                case "H14":Summon("X05",p,lane);Summon("X05",p,lane);
                    foreach(var u in allies.Where(x=>x.Card.Id=="X05"))Buff(u,1,1);break;
                case "H18":Revive(p,lane,4);break;
                case "H20":if(Damage(Units(-1,lane),u=>3).Count>=2)Draw(p);break;
                case "H22":Revive(p,0,5);Revive(p,1,5);break;
                case "L02":Buff(t,-2,0,true);break;
                case "L03":t.Shield=false;Draw(p);break;
                case "L07":Shift(t);break;
                case "L09":Recall(t);break;
                case "L11":MoveOmen(lane==1);Draw(p);break;
                case "L14":foreach(var u in foes)u.Spent=true;Shift(Best(foes));break;
                case "L16":Recall(t);Summon("X07",p,lane);Add(p,"X09");break;
                case "L17":Recall(t);if(Due())Buff(Best(Units(p)),2,0,true);break;
                case "L19":Draw(p,3);DiscardWeak(p);if(shiftedOmen)Heal(p,2);break;
                case "L20":Kill(t);Draw(p);break;
                case "L21":var opposing=Best(Units(1-p,1-lane));Swap(t,opposing);Buff(t,-2,0,true);Buff(opposing,-2,0,true);break;
                case "N03":if(t==null)Heal(p,2);else Heal(t,2);break;
                case "N07":bool fought=t.Lane==Contested;Shift(t);if(fought)Heal(t,2);break;
                case "N08":Hurt(t,2);break;
                case "N12":Summon("X11",p,lane);Draw(p);break;
                case "N18":Damage(Units(-1,lane).Where(u=>u.Spent),u=>2);break;
                case "N20":Heal(p,5);Draw(p);break;
                case "N23":Kill(t);break;
                case "X08":Buff(t,-1,0,true);break;
                case "X09":Hurt(t,1);break;
            }
        }
    }
}
