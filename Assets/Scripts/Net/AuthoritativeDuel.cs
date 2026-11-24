using System;
using System.Collections.Generic;

namespace Wolfsun.Net
{
    /// <summary>
    /// Authoritative duel host: clients submit Move intents; host validates via Match.Legal/Apply.
    /// Online transport can wrap this; local ranked-sim AI fill uses the same path.
    /// </summary>
    public sealed class AuthoritativeDuel
    {
        public readonly Wolfsun.Match Match;
        public readonly string MatchId;
        public readonly string[] PlayerIds;
        public readonly bool AiFill;
        public readonly QueueMode Mode;
        public readonly List<string> IntentLog = new List<string>();
        public bool Finished => Match.Winner >= 0;

        public AuthoritativeDuel(string matchId, string[] playerIds, string[] deck0, string[] deck1,
            string hero0, string hero1, string prophecy0, string prophecy1, int seed, bool aiFill, QueueMode mode, int enemyHealth = 24)
        {
            MatchId = matchId;
            PlayerIds = playerIds;
            AiFill = aiFill;
            Mode = mode;
            Match = new Wolfsun.Match(deck0, deck1, hero0, hero1, prophecy0, prophecy1, seed, enemyHealth);
        }

        public bool Submit(int side, Wolfsun.Move move)
        {
            if (Finished || side < 0 || side > 1) return false;
            if (Match.Opening) return false;
            if (Match.Turn != side) return false;
            if (!Match.Legal(side).Exists(m => m.Same(move))) return false;
            bool ok = Match.Apply(side, move);
            if (ok) IntentLog.Add(side + ":" + move.Kind + ":" + move.Slot + ":" + move.Lane + ":" + move.Target);
            return ok;
        }

        public bool KeepOpening(int side, IEnumerable<int> replace) => Match.ChooseOpeningHand(side, replace);

        public bool Concede(int side) => Match.Surrender(side);

        public static AuthoritativeDuel FromPair(MatchPair pair, string[] deckA, string[] deckB, string heroA, string heroB, int seed)
        {
            var rng = new Random(seed);
            int first = rng.Next(1, 7), second;
            do { second = rng.Next(1, 7); } while (second == first);
            return new AuthoritativeDuel(
                Guid.NewGuid().ToString("N"),
                new[] { pair.A.PlayerId, pair.B.PlayerId },
                deckA, deckB, heroA, heroB,
                "P0" + first, "P0" + second, seed, pair.AiFill, pair.A.Mode,
                pair.AiFill ? 22 : 24);
        }
    }
}
