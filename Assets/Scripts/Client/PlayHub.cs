using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using QueueMode = Wolfsun.Net.QueueMode;
using MatchPair = Wolfsun.Net.MatchPair;
using QueueTicket = Wolfsun.Net.QueueTicket;
using AuthoritativeDuel = Wolfsun.Net.AuthoritativeDuel;
using Matchmaker = Wolfsun.Net.Matchmaker;

namespace Wolfsun
{
    public sealed partial class Game
    {
        private static readonly string[] CardBackCatalog =
        {
            "Foundry/CardBack",
            "Foundry/CardBack-Ember",
            "Foundry/CardBack-Frost",
            "Foundry/CardBack-Legend"
        };

        private void PlayHub() { }

        private void DrawRankStars(Transform parent, float x, float y, int filled, int total) { }

        private void BuyCardBack(string path = null) { }

        private void BeginQueue(QueueMode mode) { }

        private void CancelQueue()
        {
            queueSearching = false;
            queueStatus = "";
        }

        private IEnumerator QueuePump()
        {
            yield break;
        }

        private void StartRankedSim(MatchPair pair) { }

        private void ShowMatchFound(MatchPair pair) { }

        private void Career() { }
    }
}
