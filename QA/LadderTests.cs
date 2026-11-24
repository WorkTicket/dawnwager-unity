using System;
using Wolfsun;

/// <summary>Headless ladder / matchmaker checks. Run: dotnet script or compile into QA harness.</summary>
public static class LadderTests
{
    public static int Run()
    {
        int fails = 0;
        void Check(bool ok, string name) { if (!ok) { Console.WriteLine("FAIL " + name); fails++; } else Console.WriteLine("ok " + name); }

        Check(Ladder.DisplayName(0) == "Ember 10", "ember10");
        Check(Ladder.IsFloor(0), "floor ember10");
        Check(Ladder.MinStarBonusForPeak(0) >= 2, "min bonus");

        var season = new Ladder.SeasonState { StarBonus = 3, AbsoluteRank = 0, Stars = 0 };
        Ladder.ApplyRankedResult(season, true);
        Check(season.Stars == 3 || season.AbsoluteRank > 0, "star bonus win");

        season = new Ladder.SeasonState { AbsoluteRank = 0, Stars = 0 };
        Ladder.ApplyRankedResult(season, false);
        Check(season.AbsoluteRank == 0 && season.Stars == 0, "floor protects");

        double r = 1500, rd = 200;
        Glicko.Update(ref r, ref rd, 1500, 200, true);
        Check(r > 1500, "glicko win raises");

        var mm = new Wolfsun.Net.Matchmaker();
        mm.Enqueue(new Wolfsun.Net.QueueTicket { PlayerId = "a", Mode = Wolfsun.Net.QueueMode.Ranked, Mmr = 1500, MmrRd = 100 });
        mm.Enqueue(new Wolfsun.Net.QueueTicket { PlayerId = "b", Mode = Wolfsun.Net.QueueMode.Ranked, Mmr = 1510, MmrRd = 100 });
        var pair = mm.TryPair(DateTime.UtcNow.AddSeconds(10));
        Check(pair != null && !pair.AiFill, "pair close mmr");

        Console.WriteLine(fails == 0 ? "ALL LADDER TESTS PASSED" : fails + " failures");
        return fails;
    }
}
