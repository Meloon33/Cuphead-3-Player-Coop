using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cuphead3PCoop
{
    [System.Serializable]
    public class PlayerBossRunStats
    {
        public float damageDealt = 0f;
        public int parries = 0;
        public int revivesGiven = 0;
        public int deaths = 0;
        public int supersUsed = 0;
    }

    [System.Serializable]
    public class BossRunTrophy
    {
        public string icon;
        public string title;
        public string playerName;
        public string description;
        public string colorHex;

        public BossRunTrophy() { }

        public BossRunTrophy(string icon, string title, string playerName, string description, string colorHex)
        {
            this.icon = icon;
            this.title = title;
            this.playerName = playerName;
            this.description = description;
            this.colorHex = colorHex;
        }
    }

    [System.Serializable]
    public class BossRunSummary
    {
        public string levelKey;
        public string bossName;
        public int totalAttempts = 1;
        public int attemptNumber = 1;
        public bool isWinningAttemptOnly = false;
        public float totalTeamDamage = 0f;
        public int totalTeamParries = 0;
        public int totalTeamRevives = 0;
        public int totalTeamDeaths = 0;
        public int totalTeamSupers = 0;
        public string completedDate = "";

        public PlayerBossRunStats p1 = new PlayerBossRunStats();
        public PlayerBossRunStats p2 = new PlayerBossRunStats();
        public PlayerBossRunStats p3 = new PlayerBossRunStats();

        public List<BossRunTrophy> trophies = new List<BossRunTrophy>();
    }

    /// <summary>
    /// Tracks both:
    /// 1. Single winning attempt stats (last successful round).
    /// 2. Cumulative multiplayer stats across all attempts/restarts for the boss.
    /// Generates two distinct scorecards after victory.
    /// </summary>
    public static class BossSessionTracker
    {
        private static bool _hasActiveRun = false;
        private static string _currentLevelKey = "";
        private static int _attemptsCount = 1;

        // Cumulative across all attempts in this boss session
        private static PlayerBossRunStats _p1Run = new PlayerBossRunStats();
        private static PlayerBossRunStats _p2Run = new PlayerBossRunStats();
        private static PlayerBossRunStats _p3Run = new PlayerBossRunStats();

        // Single current attempt stats
        private static PlayerBossRunStats _p1Attempt = new PlayerBossRunStats();
        private static PlayerBossRunStats _p2Attempt = new PlayerBossRunStats();
        private static PlayerBossRunStats _p3Attempt = new PlayerBossRunStats();

        public static BossRunSummary LastWinningSummary { get; private set; } = null;
        public static BossRunSummary LastCumulativeSummary { get; private set; } = null;
        public static BossRunSummary LastSummary => LastWinningSummary ?? LastCumulativeSummary;
        public static bool HasLastSummary => (LastWinningSummary != null || LastCumulativeSummary != null);
        public static int CurrentAttempts => _attemptsCount;
        public static bool HasActiveRun => _hasActiveRun;

        public static void OnLevelStart(Level level)
        {
            if (level == null) return;

            bool isBoss = HardcoreGhostWatcher.IsBossLevel(level);
            if (!isBoss)
            {
                _hasActiveRun = false;
                return;
            }

            string key = level.CurrentLevel.ToString();

            if (_hasActiveRun && _currentLevelKey == key)
            {
                // Restart / retry of the SAME boss level: increment attempt counter, keep accumulated stats
                _attemptsCount++;
                _p1Attempt = new PlayerBossRunStats();
                _p2Attempt = new PlayerBossRunStats();
                _p3Attempt = new PlayerBossRunStats();

                if (Plugin.Log != null)
                {
                    Plugin.Log.LogInfo(string.Format("[3P-Coop] Boss Run attempt #{0} for {1}. Stats preserved.", _attemptsCount, key));
                }

                ControllerManager.ShowToast(string.Format("Wspólne podejście #{0} do walki z bossem!", _attemptsCount), 2.5f);
            }
            else
            {
                // New boss fight session (entered fresh from map or another stage)
                _hasActiveRun = true;
                _currentLevelKey = key;
                _attemptsCount = 1;
                _p1Run = new PlayerBossRunStats();
                _p2Run = new PlayerBossRunStats();
                _p3Run = new PlayerBossRunStats();
                _p1Attempt = new PlayerBossRunStats();
                _p2Attempt = new PlayerBossRunStats();
                _p3Attempt = new PlayerBossRunStats();

                if (Plugin.Log != null)
                {
                    Plugin.Log.LogInfo("[3P-Coop] Started new boss session run for " + key);
                }
            }
        }

        public static void RecordDamage(PlayerId id, float amount)
        {
            if (!_hasActiveRun || amount <= 0f) return;
            var stRun = GetRunStats(id);
            if (stRun != null) stRun.damageDealt += amount;
            var stAtt = GetAttemptStats(id);
            if (stAtt != null) stAtt.damageDealt += amount;
        }

        public static void RecordParry(PlayerId id)
        {
            if (!_hasActiveRun) return;
            var stRun = GetRunStats(id);
            if (stRun != null) stRun.parries++;
            var stAtt = GetAttemptStats(id);
            if (stAtt != null) stAtt.parries++;
        }

        public static void RecordSuper(PlayerId id)
        {
            if (!_hasActiveRun) return;
            var stRun = GetRunStats(id);
            if (stRun != null) stRun.supersUsed++;
            var stAtt = GetAttemptStats(id);
            if (stAtt != null) stAtt.supersUsed++;
        }

        public static void RecordDeath(PlayerId id)
        {
            if (!_hasActiveRun) return;
            var stRun = GetRunStats(id);
            if (stRun != null) stRun.deaths++;
            var stAtt = GetAttemptStats(id);
            if (stAtt != null) stAtt.deaths++;
        }

        public static void RecordRevive(PlayerId rescuerId)
        {
            if (!_hasActiveRun) return;
            var stRun = GetRunStats(rescuerId);
            if (stRun != null) stRun.revivesGiven++;
            var stAtt = GetAttemptStats(rescuerId);
            if (stAtt != null) stAtt.revivesGiven++;
        }

        private static PlayerBossRunStats GetRunStats(PlayerId id)
        {
            switch (id)
            {
                case PlayerId.PlayerOne: return _p1Run;
                case PlayerId.PlayerTwo: return _p2Run;
                case PlayerId.PlayerThree: return _p3Run;
                default: return null;
            }
        }

        private static PlayerBossRunStats GetAttemptStats(PlayerId id)
        {
            switch (id)
            {
                case PlayerId.PlayerOne: return _p1Attempt;
                case PlayerId.PlayerTwo: return _p2Attempt;
                case PlayerId.PlayerThree: return _p3Attempt;
                default: return null;
            }
        }

        public static void OnLevelWin(Level level)
        {
            if (level == null) return;
            bool isBoss = HardcoreGhostWatcher.IsBossLevel(level);
            if (!isBoss) return;

            string key = level.CurrentLevel.ToString();
            string bossName = GetBossDisplayName(level.CurrentLevel);
            string dateStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            int totalAtt = Mathf.Max(1, _attemptsCount);

            // 1. Single winning attempt summary
            var winning = new BossRunSummary();
            winning.levelKey = key;
            winning.bossName = bossName;
            winning.totalAttempts = 1;
            winning.attemptNumber = totalAtt;
            winning.isWinningAttemptOnly = true;
            winning.completedDate = dateStr;
            CopyStats(_p1Attempt, winning.p1);
            CopyStats(_p2Attempt, winning.p2);
            CopyStats(_p3Attempt, winning.p3);
            ComputeTotals(winning);
            GenerateTrophies(winning);

            // 2. Cumulative all attempts summary
            var cumulative = new BossRunSummary();
            cumulative.levelKey = key;
            cumulative.bossName = bossName;
            cumulative.totalAttempts = totalAtt;
            cumulative.attemptNumber = totalAtt;
            cumulative.isWinningAttemptOnly = false;
            cumulative.completedDate = dateStr;
            CopyStats(_p1Run, cumulative.p1);
            CopyStats(_p2Run, cumulative.p2);
            CopyStats(_p3Run, cumulative.p3);
            ComputeTotals(cumulative);
            GenerateTrophies(cumulative);

            LastWinningSummary = winning;
            LastCumulativeSummary = cumulative;
            _hasActiveRun = false;

            if (Plugin.Log != null)
            {
                Plugin.Log.LogInfo(string.Format("[3P-Coop] Boss victory summaries generated for {0} (Won on attempt #{1}, total attempts {2}).", bossName, totalAtt, totalAtt));
            }

            BossVictorySummaryUI.Open(winning, cumulative);
        }

        private static void CopyStats(PlayerBossRunStats src, PlayerBossRunStats dst)
        {
            dst.damageDealt = src.damageDealt;
            dst.parries = src.parries;
            dst.revivesGiven = src.revivesGiven;
            dst.deaths = src.deaths;
            dst.supersUsed = src.supersUsed;
        }

        private static void ComputeTotals(BossRunSummary s)
        {
            s.totalTeamDamage = s.p1.damageDealt + s.p2.damageDealt + s.p3.damageDealt;
            s.totalTeamParries = s.p1.parries + s.p2.parries + s.p3.parries;
            s.totalTeamRevives = s.p1.revivesGiven + s.p2.revivesGiven + s.p3.revivesGiven;
            s.totalTeamDeaths = s.p1.deaths + s.p2.deaths + s.p3.deaths;
            s.totalTeamSupers = s.p1.supersUsed + s.p2.supersUsed + s.p3.supersUsed;
        }

        private static void GenerateTrophies(BossRunSummary s)
        {
            s.trophies.Clear();

            bool p1Exists = false;
            bool p2Exists = false;
            bool p3Exists = false;

            try
            {
                p1Exists = PlayerManager.DoesPlayerExist(PlayerId.PlayerOne);
                p2Exists = PlayerManager.DoesPlayerExist(PlayerId.PlayerTwo);
                p3Exists = PlayerManager.DoesPlayerExist(PlayerId.PlayerThree);
            }
            catch
            {
                // Standalone test or transition context
            }

            // Fallback: Check recorded activity in this run
            if (!p1Exists) p1Exists = true;
            if (!p2Exists && (s.p2.damageDealt > 0f || s.p2.parries > 0 || s.p2.deaths > 0 || s.p2.revivesGiven > 0 || s.p2.supersUsed > 0))
                p2Exists = true;
            if (!p3Exists && (s.p3.damageDealt > 0f || s.p3.parries > 0 || s.p3.deaths > 0 || s.p3.revivesGiven > 0 || s.p3.supersUsed > 0))
                p3Exists = true;

            try
            {
                if (!p2Exists && Extras.GetPlayer(PlayerId.PlayerTwo) != null)
                    p2Exists = true;
                if (!p3Exists && Extras.GetPlayer(PlayerId.PlayerThree) != null)
                    p3Exists = true;
            }
            catch { }

            // Default for 3P coop mod if no players flagged
            if (!p2Exists && !p3Exists)
            {
                p2Exists = true;
                p3Exists = true;
            }

            var players = new List<Tuple<PlayerId, string, string, PlayerBossRunStats>>();
            if (p1Exists) players.Add(new Tuple<PlayerId, string, string, PlayerBossRunStats>(PlayerId.PlayerOne, "Cuphead", "#FF6666", s.p1));
            if (p2Exists) players.Add(new Tuple<PlayerId, string, string, PlayerBossRunStats>(PlayerId.PlayerTwo, "Mugman", "#55A0FF", s.p2));
            if (p3Exists) players.Add(new Tuple<PlayerId, string, string, PlayerBossRunStats>(PlayerId.PlayerThree, "Ms. Chalice", "#5CD864", s.p3));

            if (players.Count == 0) return;

            var awardedPlayers = new HashSet<PlayerId>();

            // 1. Złoty Taran (Najwięcej Obrażeń)
            Tuple<PlayerId, string, string, PlayerBossRunStats> topDmg = players[0];
            foreach (var p in players)
            {
                if (p.Item4.damageDealt > topDmg.Item4.damageDealt) topDmg = p;
            }
            if (topDmg.Item4.damageDealt > 0f)
            {
                float pct = (s.totalTeamDamage > 0f) ? (topDmg.Item4.damageDealt / s.totalTeamDamage) : 0f;
                string desc = string.Format("Główna siła ognia: {0:N0} obrażeń ({1:P0} wkładu drużyny)!", topDmg.Item4.damageDealt, pct);
                s.trophies.Add(new BossRunTrophy("[SIŁA OGNIA]", "ZŁOTY TARAN", topDmg.Item2, desc, topDmg.Item3));
                awardedPlayers.Add(topDmg.Item1);
            }

            // 2. Anioł Stróż (Order Medyka - Najwięcej Ożywień)
            Tuple<PlayerId, string, string, PlayerBossRunStats> topRevive = null;
            foreach (var p in players)
            {
                if (p.Item4.revivesGiven > 0)
                {
                    if (topRevive == null || p.Item4.revivesGiven > topRevive.Item4.revivesGiven)
                        topRevive = p;
                }
            }
            if (topRevive != null)
            {
                string desc = string.Format("Niezawodna pomoc medyczna: {0} uratowanych towarzyszy w ferworze walki!", topRevive.Item4.revivesGiven);
                s.trophies.Add(new BossRunTrophy("[RATOWNIK]", "ANIOŁ STRÓŻ", topRevive.Item2, desc, topRevive.Item3));
                awardedPlayers.Add(topRevive.Item1);
            }

            // 3. Żelazna Skorupa (Mistrz Przetrwania - Najmniej potknięć)
            Tuple<PlayerId, string, string, PlayerBossRunStats> bestSurvivor = players[0];
            foreach (var p in players)
            {
                if (p.Item4.deaths < bestSurvivor.Item4.deaths) bestSurvivor = p;
            }
            if (bestSurvivor != null)
            {
                string desc = s.isWinningAttemptOnly
                    ? ((bestSurvivor.Item4.deaths == 0) ? "Bezbłędny taniec kul – 0 zgonów w decydującej rundzie!" : "Mistrzowskie uniki i nienaruszona porcelana w zwycięskiej walce!")
                    : ((bestSurvivor.Item4.deaths == 0) ? string.Format("Bezbłędny taniec kul – 0 zgonów w trakcie {0} podejść!", s.totalAttempts) : string.Format("Mistrzowskie uniki i nienaruszona porcelana przez {0} podejść!", s.totalAttempts));
                s.trophies.Add(new BossRunTrophy("[OBRONA]", "ŻELAZNA SKORUPA", bestSurvivor.Item2, desc, bestSurvivor.Item3));
                awardedPlayers.Add(bestSurvivor.Item1);
            }

            // 4. Różowy Wirtuoz (Najwięcej Parowań)
            Tuple<PlayerId, string, string, PlayerBossRunStats> topParry = null;
            foreach (var p in players)
            {
                if (p.Item4.parries > 0)
                {
                    if (topParry == null || p.Item4.parries > topParry.Item4.parries)
                        topParry = p;
                }
            }
            if (topParry != null && (!awardedPlayers.Contains(topParry.Item1) || players.Count <= 2))
            {
                string desc = string.Format("Aż {0} zwinnych parowań różowych obiektów i pocisków!", topParry.Item4.parries);
                s.trophies.Add(new BossRunTrophy("[PAROWANIE]", "RÓŻOWY WIRTUOZ", topParry.Item2, desc, topParry.Item3));
                awardedPlayers.Add(topParry.Item1);
            }

            // 5. Nawałnica Mocy (Najwięcej Superów)
            Tuple<PlayerId, string, string, PlayerBossRunStats> topSupers = null;
            foreach (var p in players)
            {
                if (p.Item4.supersUsed > 0)
                {
                    if (topSupers == null || p.Item4.supersUsed > topSupers.Item4.supersUsed)
                        topSupers = p;
                }
            }
            if (topSupers != null && !awardedPlayers.Contains(topSupers.Item1))
            {
                string desc = string.Format("Wyprowadził {0} potężnych Superciosów prosto w bossa!", topSupers.Item4.supersUsed);
                s.trophies.Add(new BossRunTrophy("[SUPERCIOS]", "NAWAŁNICA MOCY", topSupers.Item2, desc, topSupers.Item3));
                awardedPlayers.Add(topSupers.Item1);
            }

            // 6. Gwarancja nagrody dla każdego gracza: Niezłomny Wojownik / Skrzydłowy
            foreach (var p in players)
            {
                if (!awardedPlayers.Contains(p.Item1))
                {
                    string desc = string.Format("Zacięta walka ramię w ramię przez {0} podejść i wspólny nokaut!", s.totalAttempts);
                    s.trophies.Add(new BossRunTrophy("[SKRZYDŁOWY]", "NIEZŁOMNY WOJOWNIK", p.Item2, desc, p.Item3));
                    awardedPlayers.Add(p.Item1);
                }
            }
        }

        public static string GetBossDisplayName(Levels lvl)
        {
            switch (lvl)
            {
                // Wyspa Kałamarz I
                case Levels.Veggies: return "Banda Korzeniaków (Root Pack)";
                case Levels.Slime: return "Wielki Glut (Goopy Le Grande)";
                case Levels.Frogs: return "Żebrak i Skrzek (Ribby & Croaks)";
                case Levels.Flower: return "Cagney Goździk";
                case Levels.FlyingBlimp: return "Hilda Berg";

                // Wyspa Kałamarz II
                case Levels.Baroness: return "Baronowa von Bon Bon";
                case Levels.Clown: return "Klaun Beppi";
                case Levels.FlyingGenie: return "Dżin Djimmi";
                case Levels.FlyingBird: return "Ptak Wally Warbles";
                case Levels.Dragon: return "Smok Grim Matchstick";

                // Wyspa Kałamarz III
                case Levels.Bee: return "Królowa Pszczół Rumor Honeybottoms";
                case Levels.Pirate: return "Kapitan Brineybeard";
                case Levels.SallyStagePlay: return "Sally Teatrzyk";
                case Levels.Mouse: return "Szczur Werner Werman";
                case Levels.Robot: return "Robot Dr. Kahla";
                case Levels.FlyingMermaid: return "Syrena Cala Maria";
                case Levels.Train: return "Widmowy Ekspres (Phantom Express)";

                // Finał
                case Levels.DicePalaceMain: return "Kasyno King Dice'a";
                case Levels.Devil: return "Sam Diabeł (The Devil)";

                // DLC The Delicious Last Course
                case Levels.OldMan: return "Glumstone Olbrzym";
                case Levels.RumRunners: return "Bimbrownicy (Moonshine Mob)";
                case Levels.FlyingCowboy: return "Esther Winchester";
                case Levels.Airplane: return "Asy Przestworzy (Howling Aces)";
                case Levels.SnowCult: return "Mortimer Bałwan (Mortimer Freeze)";
                case Levels.Saltbaker: return "Szef Kuchni Solniczak (Chef Saltbaker)";

                // Królewski Skok
                case Levels.ChessPawn: return "Pionki (Królewski Skok)";
                case Levels.ChessKnight: return "Skoczek (Królewski Skok)";
                case Levels.ChessBishop: return "Goniec (Królewski Skok)";
                case Levels.ChessRook: return "Wieża (Królewski Skok)";
                case Levels.ChessQueen: return "Królowa (Królewski Skok)";
                case Levels.ChessCastle: return "Turniej Królewskiego Skoku";

                // Cmentarz
                case Levels.Graveyard: return "Sekretny Cmentarz (Anioł i Diabeł)";

                default: return lvl.ToString();
            }
        }
    }

    public class Tuple<T1, T2, T3, T4>
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;
        public Tuple(T1 item1, T2 item2, T3 item3, T4 item4)
        {
            Item1 = item1; Item2 = item2; Item3 = item3; Item4 = item4;
        }
    }
}
