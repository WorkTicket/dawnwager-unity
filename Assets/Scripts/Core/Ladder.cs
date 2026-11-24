using System;

namespace Wolfsun
{
    public enum LadderMode { Casual, Ranked }

    /// <summary>
    /// Beacon Ladder — Hearthstone-parity presentation (public Blizzard ranked FAQ shape):
    /// leagues × 10 ranks, 3 stars/rank, floors at 10 and 5, star bonus, win streak to Mythic 5.
    /// Hidden MMR is separate (see Glicko).
    /// </summary>
    public static class Ladder
    {
        public static readonly string[] Leagues = { "Ember", "Rime", "Brass", "Iron", "Mythic" };
        public const int RanksPerLeague = 10;
        public const int StarsPerRank = 3;
        public const int LegendOrdinal = 50; // absolute index past Mythic 1
        public const double DefaultMmr = 1500;
        public const double DefaultRd = 350;

        // Absolute 0 = Ember 10 (bottom), 49 = Mythic 1, 50 = Dawn Legend.
        public static int AbsoluteRank(int league, int rankInLeague)
        {
            league = Math.Clamp(league, 0, Leagues.Length - 1);
            rankInLeague = Math.Clamp(rankInLeague, 1, RanksPerLeague);
            return league * RanksPerLeague + (RanksPerLeague - rankInLeague);
        }

        public static void FromAbsolute(int absolute, out int league, out int rankInLeague)
        {
            absolute = Math.Clamp(absolute, 0, LegendOrdinal);
            if (absolute >= LegendOrdinal) { league = Leagues.Length - 1; rankInLeague = 1; return; }
            league = absolute / RanksPerLeague;
            rankInLeague = RanksPerLeague - (absolute % RanksPerLeague);
        }

        public static bool IsFloor(int absolute)
        {
            if (absolute >= LegendOrdinal) return true;
            FromAbsolute(absolute, out _, out int rank);
            return rank == 10 || rank == 5;
        }

        public static string DisplayName(int absolute, int legendPlace = 0)
        {
            if (absolute >= LegendOrdinal)
                return legendPlace > 0 ? "Dawn Legend #" + legendPlace : "Dawn Legend";
            FromAbsolute(absolute, out int league, out int rank);
            return Leagues[league] + " " + rank;
        }

        public static int MinStarBonusForPeak(int peakAbsolute)
        {
            // Public HS table shape (Bronze10→Legend), remapped to our absolute ranks.
            if (peakAbsolute >= LegendOrdinal) return 10;
            FromAbsolute(peakAbsolute, out int league, out int rank);
            int floorTier = league * 2 + (rank <= 5 ? 1 : 0);
            return Math.Max(2, Math.Min(10, 2 + floorTier));
        }

        public static int StarsForWin(int starBonus, int winStreak, int absoluteRank)
        {
            int bonus = Math.Max(1, starBonus);
            bool streak = winStreak >= 2 && absoluteRank < AbsoluteRank(4, 5); // until Mythic 5
            return bonus * (streak ? 2 : 1);
        }

        public sealed class SeasonState
        {
            public int AbsoluteRank;
            public int Stars;
            public int StarBonus = 2;
            public int WinStreak;
            public int PeakAbsolute;
            public int SeasonYear;
            public int SeasonMonth;
            public double RankedMmr = DefaultMmr;
            public double RankedRd = DefaultRd;
            public double CasualMmr = DefaultMmr;
            public double CasualRd = DefaultRd;
            public int LegendPlace;
            public int RankedGames;
            public int ApprenticeGamesLeft = 5;
        }

        public static void EnsureSeason(SeasonState state, DateTime utcNow)
        {
            if (state.SeasonYear == utcNow.Year && state.SeasonMonth == utcNow.Month) return;
            int peak = Math.Max(state.PeakAbsolute, state.AbsoluteRank);
            state.StarBonus = MinStarBonusForPeak(peak);
            // Soft MMR floor recovery (anti-tank) from last peak.
            double floor = DefaultMmr + peak * 8;
            if (state.RankedMmr < floor) state.RankedMmr = floor;
            state.AbsoluteRank = 0;
            state.Stars = 0;
            state.WinStreak = 0;
            state.LegendPlace = 0;
            state.SeasonYear = utcNow.Year;
            state.SeasonMonth = utcNow.Month;
            state.PeakAbsolute = peak;
        }

        public static void ApplyRankedResult(SeasonState state, bool won)
        {
            if (won)
            {
                state.WinStreak++;
                int gain = StarsForWin(state.StarBonus, state.WinStreak, state.AbsoluteRank);
                AddStars(state, gain);
            }
            else
            {
                state.WinStreak = 0;
                if (state.AbsoluteRank >= LegendOrdinal)
                {
                    // Legend: stars don't matter; place handled externally via MMR board.
                    return;
                }
                if (IsFloor(state.AbsoluteRank) && state.Stars == 0) return;
                if (state.Stars > 0) state.Stars--;
                else if (state.AbsoluteRank > 0)
                {
                    state.AbsoluteRank--;
                    state.Stars = StarsPerRank - 1;
                }
            }
            state.PeakAbsolute = Math.Max(state.PeakAbsolute, state.AbsoluteRank);
            state.RankedGames++;
            if (state.ApprenticeGamesLeft > 0) state.ApprenticeGamesLeft--;
        }

        private static void AddStars(SeasonState state, int gain)
        {
            if (state.AbsoluteRank >= LegendOrdinal) return;
            state.Stars += gain;
            while (state.Stars >= StarsPerRank && state.AbsoluteRank < LegendOrdinal - 1)
            {
                state.Stars -= StarsPerRank;
                state.AbsoluteRank++;
                if (IsFloor(state.AbsoluteRank) && state.StarBonus > 1)
                    state.StarBonus--;
            }
            if (state.AbsoluteRank == LegendOrdinal - 1 && state.Stars >= StarsPerRank)
            {
                state.Stars = 0;
                state.AbsoluteRank = LegendOrdinal;
            }
        }
    }

    /// <summary>Glicko-1 style update (public rating math; not Blizzard proprietary).</summary>
    public static class Glicko
    {
        private const double Q = 0.0057565; // ln(10)/400
        public static void Update(ref double rating, ref double rd, double oppRating, double oppRd, bool won)
        {
            double g = 1.0 / Math.Sqrt(1.0 + 3.0 * Q * Q * oppRd * oppRd / (Math.PI * Math.PI));
            double e = 1.0 / (1.0 + Math.Pow(10, -g * (rating - oppRating) / 400.0));
            double d2 = 1.0 / (Q * Q * g * g * e * (1 - e));
            double score = won ? 1.0 : 0.0;
            rating = rating + Q / (1.0 / (rd * rd) + 1.0 / d2) * g * (score - e);
            rd = Math.Sqrt(1.0 / (1.0 / (rd * rd) + 1.0 / d2));
            rd = Math.Min(350, Math.Max(45, rd));
            rating = Math.Max(600, Math.Min(3000, rating));
        }
    }
}
