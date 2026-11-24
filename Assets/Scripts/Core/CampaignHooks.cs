using System.Linq;

namespace Wolfsun
{
    public sealed partial class Match
    {
        private bool CampaignDiscountAvailable(string key) =>
            Rules != null && !Rules.Fired.Contains(key);

        private int CampaignOrderDiscount(int p, Card c)
        {
            // Peek only — never consume Fired here (legality/preview/AI must not spend once-keys).
            if (p != 0 || Rules == null || Rules == CampaignRules.Empty) return 0;
            int discount = 0;
            if (c.Kind == Kind.Order)
            {
                if (Rules.Has("R1") && CampaignDiscountAvailable("R1-order")) discount++;
                if (Rules.Has("W4") && Rules.AttackedThisRound && CampaignDiscountAvailable("W4-order")) discount++;
                if (Rules.Has("R4") && Rules.SealAwardedThisMatch && CampaignDiscountAvailable("R4-order")) discount++;
            }
            if (c.Kind == Kind.Site && Rules.Has("R6") && Rules.PlayedOrderThisMatch && CampaignDiscountAvailable("R6-site"))
                discount++;
            return discount;
        }

        private void ConsumeCampaignOrderDiscount(int p, Card c)
        {
            if (p != 0 || Rules == null || Rules == CampaignRules.Empty) return;
            if (c.Kind == Kind.Order)
            {
                if (Rules.Has("R1")) Rules.Once("R1-order");
                if (Rules.Has("W4") && Rules.AttackedThisRound) Rules.Once("W4-order");
                if (Rules.Has("R4") && Rules.SealAwardedThisMatch) Rules.Once("R4-order");
            }
            if (c.Kind == Kind.Site && Rules.Has("R6") && Rules.PlayedOrderThisMatch)
                Rules.Once("R6-site");
        }

        private void ApplyCampaignAfterPlay(int p, Card c, Piece placed, int lane)
        {
            if (p != 0 || Rules == null || Rules == CampaignRules.Empty) return;
            if (c.Kind == Kind.Unit && placed != null)
            {
                if (Rules.Has("W1") && Rules.Once("W1")) Buff(placed, 1, 0, true);
                if (Rules.Has("W3") && Contested == lane && Rules.Once("W3")) Buff(placed, 1, 0, true);
                if (c.Guard && Rules.Has("B2") && Rules.Once("B2")) Buff(placed, 0, 1, false);
                if (c.Rush && Rules.Has("W6") && Rules.Once("W6")) Buff(placed, 1, 0, false);
            }
            if (c.Kind == Kind.Site && placed != null)
            {
                if (Rules.Has("R2") && Rules.Once("R2")) Heal(0, 1);
                if (Rules.Has("frost-nail") && Rules.Once("frost-nail"))
                {
                    Players[0].Bonus[placed.Lane] += 1;
                    Say("Frost Nail strengthens " + (placed.Lane == 0 ? "West." : "East."));
                }
                if (Rules.Has("R6")) { /* discount consumed in Cost */ }
            }
            if (c.Kind == Kind.Order) Rules.PlayedOrderThisMatch = true;
        }

        private void ApplyCampaignAfterMarch(int p, Piece u)
        {
            if (p != 0 || u == null || Rules == null || Rules == CampaignRules.Empty) return;
            if (Rules.Has("W2") && Rules.Once("W2")) Buff(u, 1, 0, true);
            if (Rules.Has("ferry-bell") && Rules.Once("ferry-bell")) Heal(0, 2);
        }

        private void ApplyCampaignAfterPower(int p)
        {
            if (p != 0 || Rules == null || Rules == CampaignRules.Empty) return;
            if (Rules.Has("R3") && Rules.Once("R3")) Heal(0, 1);
            if (Rules.Has("charred-coin") && Rules.Once("charred-coin"))
            {
                Players[0].Charge += 1;
                Say("Charred Coin sparks a Charge.");
            }
        }

        private void ApplyCampaignAfterAttack(int p)
        {
            if (p == 0 && Rules != null) Rules.AttackedThisRound = true;
        }

        private void ApplyCampaignAfterSeal(int winner)
        {
            if (Rules == null || Rules == CampaignRules.Empty) return;
            if (winner == 0)
            {
                Rules.SealAwardedThisMatch = true;
                if (Rules.Has("W5") && Rules.Once("W5")) Heal(0, 2);
                if (Rules.Has("R4")) { /* next-order discount via Once key */ }
            }
            else if (winner == 1 && Rules.Has("B4") && Rules.Once("B4")) Heal(0, 2);
        }

        private void ApplyCampaignRoundEnd()
        {
            if (Rules == null || Rules == CampaignRules.Empty) return;
            Rules.AttackedThisRound = false;
            if (Rules.Has("B6") && Players[0].Hp <= 6 && Winner < 0 && Rules.Once("B6"))
                Heal(0, 3);
        }

        private void ApplyCampaignAfterDamage(int side)
        {
            if (side != 0 || Rules == null || Rules == CampaignRules.Empty) return;
            if (Rules.Has("last-lantern") && Players[0].Hp > 0 && Players[0].Hp <= 6 && Rules.Once("last-lantern"))
                Heal(0, 2);
        }

        private void ApplyCampaignAfterDeath(Piece dead)
        {
            if (dead == null || Rules == null || Rules == CampaignRules.Empty) return;
            if (dead.Side == 0 && dead.Card.Kind == Kind.Unit && Rules.Has("B3") && Rules.Once("B3"))
                Heal(0, 2);
        }

        private void ApplyCampaignAfterProphecy()
        {
            if (Rules == null || Rules == CampaignRules.Empty) return;
            if (Rules.Has("R5") && Winner < 0 && Rules.Once("R5")) Draw(0, 1);
        }
    }
}
