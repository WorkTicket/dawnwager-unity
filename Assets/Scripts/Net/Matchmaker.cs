using System;
using System.Collections.Generic;
using System.Linq;

namespace Wolfsun.Net
{
    // QueueMode / tickets are net-facing; Ladder.SeasonState remains in Wolfsun.
    public enum QueueMode { Casual, Ranked }

    public sealed class QueueTicket
    {
        public string PlayerId;
        public QueueMode Mode;
        public double Mmr;
        public double MmrRd;
        public string DeckHash;
        public string Region = "na";
        public DateTime EnqueuedAtUtc;
        public string LastOpponentId;
        public bool AllowAiFill = true;
    }

    public sealed class MatchPair
    {
        public QueueTicket A;
        public QueueTicket B;
        public bool AiFill;
        public double SearchRadiusUsed;
    }

    /// <summary>
    /// Local/in-process matchmaker: MMR proximity with expanding radius (HS-like feel).
    /// Suitable for EA + offline sims; swap transport later without changing pairing rules.
    /// </summary>
    public sealed class Matchmaker
    {
        private readonly List<QueueTicket> queue = new List<QueueTicket>();
        private readonly object gate = new object();

        public int QueuedCount { get { lock (gate) return queue.Count; } }

        public void Enqueue(QueueTicket ticket)
        {
            if (ticket == null || string.IsNullOrEmpty(ticket.PlayerId)) return;
            lock (gate)
            {
                queue.RemoveAll(t => t.PlayerId == ticket.PlayerId && t.Mode == ticket.Mode);
                ticket.EnqueuedAtUtc = DateTime.UtcNow;
                queue.Add(ticket);
            }
        }

        public void Cancel(string playerId, QueueMode mode)
        {
            lock (gate) queue.RemoveAll(t => t.PlayerId == playerId && t.Mode == mode);
        }

        public MatchPair TryPair(DateTime utcNow, double maxWaitSeconds = 45)
        {
            lock (gate)
            {
                for (int i = 0; i < queue.Count; i++)
                {
                    var a = queue[i];
                    double waited = (utcNow - a.EnqueuedAtUtc).TotalSeconds;
                    double radius = SearchRadius(waited);
                    QueueTicket best = null;
                    double bestDist = double.MaxValue;
                    for (int j = 0; j < queue.Count; j++)
                    {
                        if (i == j) continue;
                        var b = queue[j];
                        if (a.Mode != b.Mode) continue;
                        if (a.PlayerId == b.PlayerId) continue;
                        if (!string.IsNullOrEmpty(a.LastOpponentId) && a.LastOpponentId == b.PlayerId && waited < 20) continue;
                        double dist = Math.Abs(a.Mmr - b.Mmr) + Math.Abs(a.MmrRd - b.MmrRd) * 0.15;
                        if (dist <= radius && dist < bestDist)
                        {
                            best = b;
                            bestDist = dist;
                        }
                    }
                    if (best != null)
                    {
                        queue.Remove(a);
                        queue.Remove(best);
                        return new MatchPair { A = a, B = best, AiFill = false, SearchRadiusUsed = radius };
                    }
                    if (a.AllowAiFill && waited >= maxWaitSeconds)
                    {
                        queue.Remove(a);
                        var ai = new QueueTicket
                        {
                            PlayerId = "ai:" + Guid.NewGuid().ToString("N").Substring(0, 8),
                            Mode = a.Mode,
                            Mmr = a.Mmr + (waited % 2 == 0 ? -40 : 40),
                            MmrRd = Math.Max(80, a.MmrRd),
                            DeckHash = "ai",
                            EnqueuedAtUtc = utcNow
                        };
                        return new MatchPair { A = a, B = ai, AiFill = true, SearchRadiusUsed = radius };
                    }
                }
                return null;
            }
        }

        public static double SearchRadius(double waitedSeconds)
        {
            // Start ±50, expand toward ±400, then open further.
            if (waitedSeconds < 5) return 50;
            if (waitedSeconds < 10) return 100;
            if (waitedSeconds < 20) return 200;
            if (waitedSeconds < 35) return 400;
            return 800;
        }

        public IReadOnlyList<QueueTicket> Snapshot()
        {
            lock (gate) return queue.ToList();
        }
    }
}
