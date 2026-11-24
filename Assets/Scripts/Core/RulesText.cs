using System.Collections.Generic;
using System.Linq;

namespace Wolfsun
{
    public static class RulesText
    {
        public static readonly IReadOnlyDictionary<string, string> Keywords = new Dictionary<string, string>
        {
            ["Deploy"] = "Happens immediately when you play this unit.",
            ["Guard"] = "Units attacking this lane must attack a Guard first.",
            ["Rush"] = "This unit enters ready. It can attack units immediately.",
            ["Shield"] = "Blocks the next instance of damage to this unit.",
            ["Farewell"] = "Happens when this unit dies.",
            ["Omenbound"] = "Happens immediately after a prophecy resolves.",
            ["Forfeit"] = "Your Bastion loses the stated amount of health.",
            ["Unchain"] = "Costs 1 less Charge per enemy Seal, up to the stated discount.",
            ["Exhaust"] = "An exhausted unit can't strike or march until it is readied.",
            ["Recall"] = "Return the unit to its owner’s hand. Generated units disappear instead.",
            ["Revive"] = "Return an eligible fallen unit to the board at full health."
        };

        // The rules engine resolves these secondary choices automatically.
        private static readonly Dictionary<string, string> Clarified = new Dictionary<string, string>
        {
            ["P03"] = "For each side with room in the other lane, move its strongest unit from the contested lane there. Both moves happen together.",
            ["A07"] = "Choose a lane. Give your two highest-Attack units there +1 Health. If both get Health, give the first one Shield.",
            ["A10"] = "Deploy: Heal your strongest wounded unit 3. If none is wounded, heal your Bastion 3.",
            ["M01"] = "Deploy: Move your strongest other unit from the quiet lane to the contested lane, if there is room.",
            ["M06"] = "Deploy: Move your strongest unit from the other lane here, if there is room.",
            ["M11"] = "Choose a lane. Move up to two of your highest-Attack units from there to the other lane, as space allows.",
            ["M16"] = "Choose one of your units. Swap it with your strongest unit in the other lane. Give both +1 Attack this round and draw a card.",
            ["M23"] = "Deploy: Move your strongest other unit into the contested lane if able. The first time each round another ally moves into the contested lane, give it +2 Attack this round.",
            ["J07"] = "Forfeit 2. Draw two cards, then discard your highest-cost card.",
            ["H11"] = "Choose a lane. Revive your highest-cost eligible unit there, up to a printed cost of 2.",
            ["H17"] = "Deploy: If possible, destroy your lowest-Attack ally here with Farewell or one that was generated. Gain +1/+2 if you do.",
            ["H18"] = "Choose a lane. Revive your highest-cost eligible unit there, up to a printed cost of 4.",
            ["H22"] = "Revive your highest-cost fallen unit up to cost 5 in each lane, West first. Skip a lane if it is full or no eligible card remains.",
            ["H23"] = "Omenbound: Revive your highest-cost unit up to cost 4 that died to this prophecy. Use the contested lane if there is room, otherwise the other lane.",
            ["L01"] = "Deploy: Put the cheaper of the next two cards in your deck on top.",
            ["L05"] = "Deploy: Move this unit to the other lane if that lane is contested and has room.",
            ["L06"] = "Deploy: When a prophecy is due, keep the cheaper of your next two cards on top and put the other on the bottom.",
            ["L11"] = "Choose whether to move the next prophecy one round earlier or later. If its timing allows, move it. Then draw a card.",
            ["L13"] = "Deploy: If this lane is quiet and the enemy has room here, move their strongest unit from the other lane here.",
            ["L14"] = "Choose a lane. Exhaust enemies there, then move the strongest of them to the other lane if there is room.",
            ["L19"] = "Draw three cards, then discard your highest-cost card. If a prophecy has been moved this match, heal your Bastion 2.",
            ["L21"] = "Choose an enemy unit. Swap it with the strongest enemy in the other lane. Both lose 2 Attack this round.",
            ["L22"] = "Omenbound: Move the strongest eligible enemy unit out of the contested lane and exhaust it.",
            ["L23"] = "Deploy: Try to delay the next prophecy by one round. If you cannot delay it, try to move it one round earlier. Add a Cinder Rune if it shifts.",
            ["N15"] = "Deploy: Destroy an enemy Site here that has a printed cost of 3 or less, if able.",
            ["N21"] = "Deploy: Move your strongest eligible ally from the other lane here and heal it 2. Wounded allies are eligible, or any ally if this lane is contested."
        };

        public static string Describe(Card card)
        {
            if (Clarified.TryGetValue(card.Id, out var text)) return text;
            return card.Rules.Length == 0 ? "No special ability." : card.Rules;
        }

        public static IEnumerable<KeyValuePair<string, string>> Terms(Card card)
        {
            var rules = Describe(card);
            return Keywords.Where(keyword => rules.IndexOf(keyword.Key, System.StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }
}
