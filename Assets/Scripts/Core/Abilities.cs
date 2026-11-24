using System.Linq;

namespace Wolfsun
{
    public sealed partial class Match
    {
        private bool Due()=>Prophecies.Any(o=>!o.Spent&&o.Round==Round);
        private void Deploy(Piece u)
        {
            if(u==null)return;
            int p=u.Side,l=u.Lane;
            var allies=Units(p,l).Where(a=>a!=u);
            var across=Units(p,1-l);
            var foes=Units(1-p,l);
            void Hurt(Piece x,int damage){if(x!=null)Damage(new[]{x},_=>damage);}
            switch(u.Card.Id)
            {
                case "A02":Shield(Best(allies.Where(x=>!x.Shield)));break;
                case "A10":var hurt=Best(Units(p).Where(x=>x.Hp<x.Max));if(hurt==null)Heal(p,3);else Heal(hurt,3);break;
                case "A13":Shield(Best(across.Where(x=>!x.Shield)));break;
                case "A17":if(Due()){Draw(p);Shield(u);}break;
                case "A19":Hurt(Best(foes),3);break;
                case "A22":if(across.Any(x=>x.Guard))Shield(u);break;
                case "A23":Shield(u);break;
                case "M01":case "M23":
                    if(Contested>=0)Shift(Best(Units(p,1-Contested).Where(x=>x!=u)));
                    break;
                case "M06":Shift(Best(across));break;
                case "M09":if(!across.Any())Summon("X02",p,1-l);break;
                case "M14":if(across.Any())Heal(p,2);break;
                case "M17":Damage(foes.Where(x=>x.Spent),_=>2);break;
                case "M21":if(Units(p).Count(x=>x.Moved==Round)>=2)foreach(var a in Units(p).Where(a=>a!=u))Buff(a,1,1);break;
                case "J02":case "J06":case "J09":case "J13":Forfeit(p,1);break;
                case "J16":Forfeit(p,2);break;
                case "J21":Hurt(Best(foes),2);break;
                case "J24":Damage(Units(-1,l).Where(x=>x!=u),_=>3);break;
                case "H05":Buff(Best(Units(p).Where(x=>x!=u&&x.Card.Rules.Contains("Farewell:"))),0,1);break;
                case "H13":Summon("X05",p,1-l);break;
                case "H17":var offering=allies.Where(x=>x.Card.Rules.Contains("Farewell:")||x.Card.Faction=='X').OrderBy(x=>x.Attack).FirstOrDefault();
                    if(offering!=null){Kill(offering);Buff(u,1,2);}break;
                case "H19":foreach(var a in Units(p).Where(x=>x.Card.Rules.Contains("Farewell:")))Buff(a,1,1);break;
                case "L01":case "L06":
                    var pile=Players[p].Pile;
                    if(pile.Count<2||(u.Card.Id=="L06"&&!Due()))break;
                    bool secondBetter=Catalog.Get(pile[1]).Cost<Catalog.Get(pile[0]).Cost;
                    if(secondBetter){var hold=pile[0];pile[0]=pile[1];pile[1]=hold;}
                    if(u.Card.Id=="L06"){var bottom=pile[1];pile.RemoveAt(1);pile.Add(bottom);}
                    Say(u.Card.Name+" reads the next draw.");
                    break;
                case "L05":if(1-l==Contested)Shift(u);break;
                case "L10":Summon("X07",p,1-l);break;
                case "L13":if(l!=Contested)Shift(Best(Units(1-p,1-l)));break;
                case "L18":Add(p,"X09");break;
                case "L23":if(MoveOmen(true)||MoveOmen(false))Add(p,"X09");break;
                case "L24":Add(p,"X08");break;
                case "N01":Heal(p,1);break;
                case "N04":Buff(Best(allies),1,0,true);break;
                case "N09":if(Due())Draw(p);break;
                case "N10":Add(p,"X10");break;
                case "N15":Kill(Sites(1-p,l).FirstOrDefault(x=>x.Card.Cost<=3));break;
                case "N21":var traveler=Best(across.Where(x=>x.Hp<x.Max||l==Contested));
                    if(traveler!=null&&Room(p,l)){Shift(traveler);Heal(traveler,2);}break;
                case "N24":if(Prophecies.All(o=>o.Spent))Shield(u);break;
            }
        }

        private void Farewell(Piece u)
        {
            if(u.Mark==Round)for(int i=0;i<u.Marks;i++)Summon("X05",u.Side,u.Lane);
            switch(u.Card.Id)
            {
                case "J01":if(Players[1-u.Side].Seals>0)Heal(u.Side,1);break;
                case "J19":Summon("X04",u.Side,u.Lane);break;
                case "H01":Summon("X05",u.Side,u.Lane);break;
                case "H09":Heal(u.Side,2);break;
                case "H21":Summon("X06",u.Side,u.Lane);break;
                case "H24":Summon("X06",u.Side,0);Summon("X06",u.Side,1);break;
                case "L04":Add(u.Side,"X08");break;
                case "N16":Draw(u.Side);break;
            }
        }

        private void AtEnd(Piece u)
        {
            switch(u.Card.Id)
            {
                case "A08":if(Units(u.Side,u.Lane).Any(x=>x.Shield))Heal(u.Side,1);break;
                case "A15":if(Units(u.Side,u.Lane).Count(x=>x.Guard)>=2)Players[u.Side].Bonus[u.Lane]+=2;break;
                case "M19":if(Players[u.Side].Moved[u.Lane]>0)Summon("X02",u.Side,u.Lane);break;
                case "M24":
                    if(Contested==u.Lane&&Units(u.Side,1-u.Lane).Any()){Summon("X02",u.Side,1-u.Lane);Heal(u.Side,2);}
                    break;
                case "J10":if(Players[u.Side].Seals<Players[1-u.Side].Seals)Heal(u.Side,1);break;
                case "N13":if(Units(u.Side,u.Lane).Count()>=2)Heal(u.Side,1);break;
            }
        }

        private void Omenbound(Piece u)
        {
            switch(u.Card.Id)
            {
                case "A21":foreach(var a in Units(u.Side,u.Lane).Where(x=>x!=u))Heal(a,2);break;
                case "A24":Shield(Summon("X01",u.Side,1-u.Lane));break;
                case "J12":Buff(u,2,1);break;
                case "J15":if(!Units(u.Side,u.Lane).Any())Summon("X03",u.Side,u.Lane);break;
                case "J24":HitBastion(1-u.Side,2);break;
                case "H15":if(Players[u.Side].OmenDead>0)Draw(u.Side);break;
                case "H16":if(Players[u.Side].OmenDead>0)Summon("X06",u.Side,u.Lane);break;
                case "H23":
                    int lane=Contested>=0?Contested:u.Lane;
                    Revive(u.Side,Room(u.Side,lane)?lane:1-lane,4,true);break;
                case "L12":Add(u.Side,"X09");break;
                case "L15":Summon("X07",u.Side,u.Lane);break;
                case "L22":
                    var target=Best(Units(1-u.Side,Contested).Where(x=>Room(x.Side,1-x.Lane)));
                    if(target!=null){Shift(target);target.Spent=true;}break;
                case "L24":Buff(Best(Units(1-u.Side,Contested)), -3,0,true);break;
            }
        }
    }
}
