using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace Cuphead3PCoop
{
    [System.Serializable]
    public class PlayerStatsData
    {
        public float damageDealt = 0f;
        public int parries = 0;
        public int revivesGiven = 0;
        public int revivesReceived = 0;
        public int deaths = 0;
        public int supersUsed = 0;
        public int bossVictories = 0;
    }

    [System.Serializable]
    public class AchievementEntry
    {
        public string id;
        public string title;
        public string description;
        public bool unlocked;
        public string unlockDate;

        public AchievementEntry() { }

        public AchievementEntry(string id, string title, string description)
        {
            this.id = id;
            this.title = title;
            this.description = description;
            this.unlocked = false;
            this.unlockDate = "";
        }
    }

    [System.Serializable]
    public class CoopStatsSaveData
    {
        public PlayerStatsData p1 = new PlayerStatsData();
        public PlayerStatsData p2 = new PlayerStatsData();
        public PlayerStatsData p3 = new PlayerStatsData();

        public float totalTeamDamage = 0f;
        public int totalTeamParries = 0;
        public int totalTeamRevives = 0;
        public int totalTeamDeaths = 0;
        public int totalTeamSupers = 0;
        public int totalBossVictories = 0;
        public int hardcoreBossVictories = 0;
        public int hardcoreRestarts = 0;

        public List<string> beatenLevels = new List<string>();
        public List<AchievementEntry> achievements = new List<AchievementEntry>();
    }

    /// <summary>
    /// Persistent multiplayer statistics and 100 scenario-driven co-op achievements for 3-player co-op.
    /// Stores data in BepInEx/config/Cuphead3PCoop_Stats.json.
    /// </summary>
    public static class CoopStatsManager
    {
        private static CoopStatsSaveData _data = new CoopStatsSaveData();
        private static bool _isDirty = false;
        private static float _lastSaveTime = 0f;

        // In-battle tracking variables (reset each level)
        private static float _battleStartTime = 0f;
        private static int _battleDeaths = 0;
        private static int _battleRevives = 0;
        private static int _battleParriesCount = 0;
        private static int _battleDamageTakenCount = 0;
        private static int _battleExCount = 0;
        private static int _battleSupersCount = 0;
        private static int _battleJumpsCount = 0;
        private static int _battleDashesCount = 0;
        private static float _lastReviveTime = 0f;
        private static PlayerId _lastRevivedPlayer = PlayerId.None;
        private static float _lastRevivedPlayerTime = 0f;
        private static int _hardcoreSessionStreak = 0;

        private static readonly HashSet<PlayerId> _battleSupers = new HashSet<PlayerId>();
        private static readonly Dictionary<PlayerId, float> _battleSuperTimes = new Dictionary<PlayerId, float>();
        private static readonly HashSet<PlayerId> _playersDamagedInBattle = new HashSet<PlayerId>();
        private static readonly HashSet<PlayerId> _playersWhoDiedInBattle = new HashSet<PlayerId>();
        private static readonly Dictionary<PlayerId, int> _singlePlayerRevivesInBattle = new Dictionary<PlayerId, int>();
        private static readonly Dictionary<PlayerId, float> _deathTimes = new Dictionary<PlayerId, float>();
        private static readonly Dictionary<PlayerId, int> _deathCountsInBattle = new Dictionary<PlayerId, int>();
        private static readonly HashSet<int> _hookedMotors = new HashSet<int>();

        public static readonly AchievementEntry[] DefaultAchievements = new[]
        {

            // Część I: Osiągnięcia drużynowe i statystyki (1-102)


            // Kategoria 1: Liczby i statystyki drużynowe (1-15)
            new AchievementEntry("TRIO_INIT", "Święta Trójca", "Rozpocznijcie dowolny poziom w pełnym 3-osobowym składzie."),
            new AchievementEntry("PARRY_FIRST", "Różowy Błysk", "Wykonajcie pierwsze parowanie różowego obiektu w 3-osobowej drużynie."),
            new AchievementEntry("PARRY_50", "Różowy Kankan", "Wykonajcie łącznie 50 parowań jako drużyna."),
            new AchievementEntry("PARRY_150", "Sprężynowe Stópki", "Wykonajcie łącznie 150 parowań jako drużyna."),
            new AchievementEntry("PARRY_300", "Różowa Symfonia", "Wykonajcie łącznie 300 parowań jako drużyna."),
            new AchievementEntry("DMG_10K", "I Łup!", "Zadajcie łącznie 10 000 obrażeń wrogom."),
            new AchievementEntry("DMG_25K", "Porcelanowy Taran", "Zadajcie łącznie 25 000 obrażeń bossom."),
            new AchievementEntry("DMG_50K", "Ciężka Artyleria", "Zadajcie łącznie 50 000 obrażeń bossom."),
            new AchievementEntry("DMG_100K", "Grad Ołowiu", "Zadajcie łącznie 100 000 obrażeń bossom."),
            new AchievementEntry("DMG_250K", "Wielka Rozróba", "Zadajcie łącznie 250 000 obrażeń bossom."),
            new AchievementEntry("SUPERS_10", "Krótkie Spięcie", "Odpalcie łącznie 10 Superciosów jako drużyna."),
            new AchievementEntry("SUPERS_50", "Prawdziwa Nawałnica", "Odpalcie łącznie 50 Superciosów jako drużyna."),
            new AchievementEntry("SUPERS_100", "Trójgłowy Smok", "Odpalcie łącznie 100 Superciosów jako drużyna."),
            new AchievementEntry("DEATHS_10", "Odejście w Zaświaty", "Doświadczcie łącznie 10 zgonów w drużynie."),
            new AchievementEntry("DEATHS_50", "Potłuczone Skorupy", "Doświadczcie łącznie 50 zgonów w drużynie."),

            // Kategoria 2: Medycyna polowa i ratowanie towarzyszy (16-30)
            new AchievementEntry("MEDIC_ROOKIE", "Chwytaj za Rękę!", "Uratuj duszka poległego towarzysza po raz pierwszy."),
            new AchievementEntry("MEDIC_VETERAN", "Nie na Mojej Warcie!", "Odbij duszki poległych towarzyszy 10 razy łącznie."),
            new AchievementEntry("MEDIC_MASTER", "Pogotowie Ratunkowe", "Odbij duszki poległych towarzyszy 30 razy łącznie."),
            new AchievementEntry("MEDIC_GOD", "Anioł Stróż", "Odbij duszki poległych towarzyszy 60 razy łącznie."),
            new AchievementEntry("BULLET_MAGNET", "Lep na Muchy", "Zostań ożywiony przez towarzyszy 10 razy."),
            new AchievementEntry("FREQUENT_FLYER", "Abonament w Zaświatach", "Zostań ożywiony przez towarzyszy 25 razy."),
            new AchievementEntry("CLUTCH_MEDIC", "Ostatnia Kropla", "Wskrześ towarzysza mając tylko 1 punkt zdrowia."),
            new AchievementEntry("CLUTCH_REVIVE", "Rzutem na Taśmę", "Odbij duszka w trybie Hardcore w ułamku sekundy (<0.6s) przed restartem."),
            new AchievementEntry("CHAIN_REVIVE", "Podwójna Defibrylacja", "Wskrześ obu poległych towarzyszy w odstępie poniżej 4 sekund."),
            new AchievementEntry("INSTANT_REVIVE", "Szybki jak Błyskawica", "Wskrześ towarzysza w czasie poniżej 1 sekundy od momentu jego odejścia."),
            new AchievementEntry("AIRBORNE_MEDIC", "Podniebne Pogotowie", "Wskrześ duszka w locie samolotem w podniebnej potyczce."),
            new AchievementEntry("RESURRECTION_HERO", "Do Trzech Razy Sztuka", "Wskrześ tego samego gracza 3 razy w trakcie jednej walki z bossem."),
            new AchievementEntry("PACIFIST_GHOST", "Widmowy Taniec", "Przetrwaj jako duszek przez 8 sekund omijając ataki, nim zostaniesz ożywiony."),
            new AchievementEntry("NO_MAN_LEFT_BEHIND", "Jeden za Wszystkich", "Ukończcie walkę, w której każdy gracz zginął i został wskrzeszony."),
            new AchievementEntry("CLUTCH_EXCHANGE", "Wymiana Uprzejmości", "Dwóch graczy ożywia się nawzajem w odstępie poniżej 5 sekund."),

            // Kategoria 3: Zgranie, przetrwanie i clutche (31-45)
            new AchievementEntry("TRIO_SURVIVORS", "W Komplecie na Mecie", "Pokonajcie dowolnego bossa, gdy wszyscy 3 gracze żyją na koniec starcia."),
            new AchievementEntry("FLAWLESS_VICTORY", "Ani Jednej Rysy", "Nokaut bossa bez ani jednego zgonu w całej drużynie."),
            new AchievementEntry("SUPER_SYNC", "Zgrana Paczka", "Wszyscy 3 gracze odpalają Supercios w trakcie jednej walki z bossem."),
            new AchievementEntry("LAST_MAN_STANDING", "Samotny Wojownik", "Zdobądźcie nokaut bossa, gdy na arenie ostał się tylko jeden żywy gracz."),
            new AchievementEntry("ALL_ONE_HP", "O Włos od Porażki", "Wszyscy trzej gracze kończą starcie z bossem mając dokładnie 1 punkt zdrowia."),
            new AchievementEntry("NO_DAMAGE_RUN", "Nienaruszone Szkliwo", "Wygrajcie starcie z bossem, w którym żaden gracz nie stracił ani jednego punktu zdrowia."),
            new AchievementEntry("TRIPLE_FINISH", "Ognia ze Wszystkich Luf!", "Wszyscy trzej gracze są żywi i strzelają w momencie nokautu bossa."),
            new AchievementEntry("TEAM_REVIVAL_STREAK", "Oddział Ratunkowy", "Wykonajcie łącznie 5 wskrzeszeń w trakcie jednego starcia i wygrajcie."),
            new AchievementEntry("PARRY_CHAIN", "Różowy Zawrót Głowy", "Wykonajcie łącznie 5 parowań w trakcie jednej walki z bossem."),
            new AchievementEntry("SHARED_FATE", "Solidarni w Opałach", "Przynajmniej dwóch graczy traci zdrowie w tej samej potyczce."),
            new AchievementEntry("HARDCORE_FIRST_WIN", "Twarda Skorupa", "Pokonajcie dowolnego bossa z aktywnym trybem Hardcore."),
            new AchievementEntry("HARDCORE_CHAMPION", "Żelazna Dyscyplina", "Pokonajcie 5 bossów z aktywnym trybem Hardcore."),
            new AchievementEntry("HARDCORE_VETERAN", "Stalowy Tercet", "Pokonajcie 10 bossów z aktywnym trybem Hardcore."),
            new AchievementEntry("HARDCORE_MASTER", "Porcelanowi Giganci", "Pokonajcie 20 bossów z aktywnym trybem Hardcore."),
            new AchievementEntry("HARDCORE_WIPEOUT", "Dusza Ulotna", "Doświadczcie 10 restartów przez ucieczkę duszka poza ekran."),

            // Kategoria 4: Bossowie podstawki – Wyspy Kałamarz I, II, III (46-65)
            new AchievementEntry("BOSS_VEGGIES", "Warzywny Kociołek", "Pokonajcie Bandę Korzeniaków w 3 osoby."),
            new AchievementEntry("BOSS_SLIME", "Śliski Typ", "Rozgniećcie Wielkiego Gluta w 3 osoby."),
            new AchievementEntry("BOSS_FROGS", "Żabie Kumkanie", "Znokautujcie Żebraka i Skrzeka w 3 osoby."),
            new AchievementEntry("BOSS_FLOWER", "Ścięty Pączek", "Wyrwijcie chwasta Cagneya Goździka w 3 osoby."),
            new AchievementEntry("BOSS_BLIMP", "Podniebny Gwiazdozbiór", "Pokonajcie Hildę Berg w podniebnym starciu w 3 osoby."),
            new AchievementEntry("ISLE_1_CLEARED", "Władcy Pierwszej Wyspy", "Pokonajcie wszystkich 5 bossów na Wyspie Kałamarz I w 3 osoby."),
            new AchievementEntry("BOSS_BARONESS", "Cukrowy Zawrót Głowy", "Pokonajcie Baronową von Bon Bon w 3 osoby."),
            new AchievementEntry("BOSS_CLOWN", "Koniec Błaznowania", "Zwińcie cyrk Klauna Beppiego w 3 osoby."),
            new AchievementEntry("BOSS_GENIE", "Czary-Mary z Dżinem", "Ujarzmijcie Dżina Wielkiego w 3 osoby."),
            new AchievementEntry("BOSS_BIRD", "Pióra w Rosołku", "Oskubcie Wally'ego Trele w 3 osoby."),
            new AchievementEntry("BOSS_DRAGON", "Smocze Płomienie", "Ugaście Ponurego Zapałkę w 3 osoby."),
            new AchievementEntry("ISLE_2_CLEARED", "Karnawałowy Triumf", "Pokonajcie wszystkich 5 bossów na Wyspie Kałamarz II w 3 osoby."),
            new AchievementEntry("BOSS_BEE", "Królewski Miód", "Strąćcie z tronu Plotkę Miododół w 3 osoby."),
            new AchievementEntry("BOSS_PIRATE", "Kamraci Kapitana", "Zatopicie statek Kapitana Solnobrodego w 3 osoby."),
            new AchievementEntry("BOSS_THEATRE", "Dramat w Trzech Aktach", "Oklaskajcie nokaut Sally Teatralnej w 3 osoby."),
            new AchievementEntry("BOSS_MERMAID", "Głębinowa Pieśń", "Oprzyjcie się urokowi Cali Marii w 3 osoby."),
            new AchievementEntry("BOSS_ROBOT", "Złomowisko Profesora", "Rozmontujcie Robota Doktora Kahla w 3 osoby."),
            new AchievementEntry("BOSS_MOUSE", "Puszkowy Gryzoń", "Wygrajcie starcie z Wernerem Wermanem w 3 osoby."),
            new AchievementEntry("BOSS_TRAIN", "Bilet w Jedną Stronę", "Zatrzymajcie Ekspres Widmo w 3 osoby."),
            new AchievementEntry("ISLE_3_CLEARED", "Zdobywcy Metropolii", "Pokonajcie wszystkich 7 bossów na Wyspie Kałamarz III w 3 osoby."),

            // Kategoria 5: Pałac Kościanego Króla i Piekło (66-77)
            new AchievementEntry("MINIBOSS_BOOZE", "Beczka Śmiechu", "Pokonajcie Pijaną Zgraję w Pałacu Kościanego Króla w 3 osoby."),
            new AchievementEntry("MINIBOSS_CHIPS", "Wszystko na Jedną Kartę", "Pokonajcie Żetona Bettigana w Pałacu Kościanego Króla w 3 osoby."),
            new AchievementEntry("MINIBOSS_CIGAR", "Puszczanie z Dymem", "Pokonajcie Pana Zadyszkę bez wpadnięcia w popiół w 3 osoby."),
            new AchievementEntry("MINIBOSS_DOMINO", "Efekt Domina", "Przechytrzcie Pip i Kropkę w Pałacu Kościanego Króla w 3 osoby."),
            new AchievementEntry("MINIBOSS_RABBIT", "Abrakadabra", "Przejrzyjcie sztuczki Hokusa Pokusa w Pałacu Kościanego Króla w 3 osoby."),
            new AchievementEntry("MINIBOSS_HORSE", "Gonitwa Trupów", "Wygrajcie wyścig ze Strachlapem w podniebnym starciu w 3 osoby."),
            new AchievementEntry("MINIBOSS_ROULETTE", "Piruet na Ostrzu", "Przetańczcie potyczkę z Piroulettą w 3 osoby."),
            new AchievementEntry("MINIBOSS_8BALL", "Bila do Łuzy", "Wbijcie Mangostana do łuzy w 3 osoby."),
            new AchievementEntry("MINIBOSS_MEMORY", "Cymbałki z Piekła", "Rozszyfrujcie karty Pana Dzwonka w 3 osoby."),
            new AchievementEntry("BOSS_KING_DICE", "Koniec Szulerki", "Pokonajcie Kościanego Króla w 3 osoby."),
            new AchievementEntry("CASINO_PERFECT", "Rozbicie Szulerni", "Pokonajcie Kościanego Króla bez straty życia przez kogokolwiek w drużynie."),
            new AchievementEntry("BOSS_DEVIL", "Dłużnicy Szatana", "Pokonajcie Diabła w Piekle Kałamarza w 3 osoby."),

            // Kategoria 6: The Delicious Last Course (DLC) i Królewski Skok (78-89)
            new AchievementEntry("DEVIL_PHASE_1", "Taniec na Rogach", "Przetrwajcie pierwszą fazę Diabła bez utraty zdrowia przez Cupheada (P1)."),
            new AchievementEntry("DEVIL_ALL_ALIVE", "Dusze Zwrócone", "Wszyscy trzej gracze żyją w chwili ostatecznego nokautu Diabła."),
            new AchievementEntry("DEVIL_CHALICE", "Czar Bajecznej Czarki", "Pokonajcie Diabła, gdy Gracz 3 walczy jako Bajeczna Czarka."),
            new AchievementEntry("DEVIL_HARDCORE", "Piekielny Egzorcyzm", "Zdobądźcie nokaut na Diable w trybie Hardcore w 3 osoby."),
            new AchievementEntry("BASE_GAME_COMPLETED", "Spłacony Cyrograf", "Ukończcie całą podstawową przygodę na Wyspach Kałamarz w 3-osobowym składzie."),
            new AchievementEntry("BOSS_GLUMSTONE", "Broda Olbrzyma", "Pokonajcie Olbrzyma Ponuraka na Wyspie Kałamarz IV w 3 osoby."),
            new AchievementEntry("BOSS_MOONSHINE", "Nalot na Bimbrownię", "Rozbijcie Bimbrowniczą Szajkę w 3 osoby."),
            new AchievementEntry("BOSS_ESTHER", "Szeryf na Zagrodzie", "Zapędźcie Esterę Winchester do puszki w 3 osoby."),
            new AchievementEntry("BOSS_ACES", "Asy Przestworzy", "Pokonajcie Wyjące Asy w podniebnej potyczce w 3 osoby."),
            new AchievementEntry("BOSS_FREEZE", "W Krainie Lodu", "Rozpuśćcie Mortimera Zmroza w 3 osoby."),
            new AchievementEntry("BOSS_SALTBAKER", "Kuchenna Rewolucja", "Pokonajcie Szefa Solniczaka w 3 osoby."),
            new AchievementEntry("SALTBAKER_SURVIVAL", "Taniec na Szkle Solniczaka", "Przetrwajcie ostatnią fazę Szefa Solniczaka na spadających platformach w komplecie."),

            // Kategoria 7: Ekwipunek, style i meta (90-102)
            new AchievementEntry("CHESS_PAWNS", "Szachowe Karzełki", "Odbijcie główki Pionków w Królewskim Skoku w 3 osoby."),
            new AchievementEntry("CHESS_KNIGHT", "Rżenie Skoczka", "Odprawcie Skoczka z kwitkiem w Królewskim Skoku w 3 osoby."),
            new AchievementEntry("CHESS_BISHOP", "Święty Ogień", "Zdmuchnijcie świece Gońca w Królewskim Skoku w 3 osoby."),
            new AchievementEntry("CHESS_ROOK", "Oblężenie Wieży", "Zburzcie Wieżę w Królewskim Skoku w 3 osoby."),
            new AchievementEntry("CHESS_QUEEN", "Szach i Mat Królowej", "Ugaście zapalniki Królowej w Królewskim Skoku w 3 osoby."),
            new AchievementEntry("CHESS_GAUNTLET", "Królewski Skok w Przód", "Przejdźcie wszystkie wyzwania Królewskiego Skoku w 3 osoby."),
            new AchievementEntry("DLC_COMPLETED", "Wspaniały Ostatni Kęs", "Pokonajcie wszystkich bossów The Delicious Last Course w 3 osoby."),
            new AchievementEntry("CHALICE_COOP", "Moc Gwiezdnego Ciastka", "Wygrajcie starcie z bossem, gdy Gracz 3 gra jako Bajeczna Czarka."),
            new AchievementEntry("TRIO_PEASHOOTER", "Trzy Groszkowce", "Pokonajcie dowolnego bossa używając wyłącznie podstawowego Groszkowca."),
            new AchievementEntry("ONE_HP_CLUB", "Klub Jednego Serduszka", "Wygrajcie walkę po ustawieniu w menu [F5] bazowego zdrowia = 1 HP dla wszystkich!"),
            new AchievementEntry("GRAVEYARD_SECRET", "Tajemnica Mogił", "Rozwiążcie zagadkę nagrobków i pokonajcie sekretnego bossa (Anioł i Diabeł) w 3 osoby."),
            new AchievementEntry("PACIFIST_RUN_AND_GUN", "Pacyfistyczny Spacer", "Ukończcie dowolny poziom Biegnij i Strzelaj w 3 osoby z rangą P."),
            new AchievementEntry("LEGENDARY_TRIO", "Porcelanowe Legendy", "Odblokujcie 50 osiągnięć drużynowych i pokonajcie Diabła oraz Szefa Solniczaka!"),


            // Część II: Scenariusze taktyczne, wyzwania eksperta i clutch (103-187)


            // Kategoria 8: Scenariusze medyczne i reanimacje w biegu (103-117)
            new AchievementEntry("HERO_PARAMEDIC", "Chirurg Bojowy", "Pojedynczy gracz wykonuje co najmniej 4 wskrzeszenia w trakcie jednej walki z bossem i sam nie ginie ani razu."),
            new AchievementEntry("PHOENIX_COMEBACK", "Feniks z Popiołów", "Wykonajcie łącznie co najmniej 6 udanych wskrzeszeń w trakcie jednego starcia z bossem i sięgnijcie po zwycięstwo."),
            new AchievementEntry("PHOTO_FINISH_REVIVE", "Na Granicy Światów", "Odbij duszka w trybie Hardcore, gdy znajduje się w odległości mniejszej niż 60 pikseli od krawędzi ekranu."),
            new AchievementEntry("EXPERT_FIRST_WIN", "Chrzest Ognia na Ekspercie", "Pokonajcie dowolnego bossa na poziomie trudności Ekspert w 3 osoby."),
            new AchievementEntry("EXPERT_FLAWLESS_TRIO", "Porcelanowe Bóstwa", "Pokonajcie dowolnego bossa na poziomie Ekspert bez utraty ANI JEDNEGO punktu HP przez kogokolwiek w drużynie!"),
            new AchievementEntry("EXPERT_PEASHOOTER_ONLY", "Groszkowi Puryści", "Pokonajcie dowolnego bossa na poziomie Ekspert używając WYŁĄCZNIE podstawowego Groszkowca (Peashooter)!"),
            new AchievementEntry("EXPERT_NO_EX_NO_SUPER", "Czysty Ogień", "Pokonajcie dowolnego bossa na poziomie Ekspert bez użycia ani jednego ataku EX (1 karta) ani Supera (5 kart)!"),
            new AchievementEntry("ONE_HP_EXPERT", "Ekstremalna Kruchość", "Pokonajcie dowolnego bossa na poziomie Ekspert przy bazowym zdrowiu = 1 HP dla całej drużyny!"),
            new AchievementEntry("PARRY_FREEZE_OFF_WIN", "Płynny Taniec Kul", "Pokonajcie bossa na poziomie Ekspert z wyłączonym zamrażaniem czasu parowania ([F7] Parry Freeze OFF)!"),
            new AchievementEntry("NO_CHARMS_EXPERT", "O Własnych Siłach", "Pokonajcie bossa na poziomie Ekspert, gdy żaden z 3 graczy nie ma założonego żadnego Charmu (Amuletu)!"),
            new AchievementEntry("GROUNDED_NO_JUMP", "Przykuci do Ziemi", "Pokonajcie Bandę Korzeniaków lub Bimbrowniczą Szajkę bez wykonania ANI JEDNEGO skoku przez kogokolwiek w drużynie!"),
            new AchievementEntry("NO_DASH_VICTORY", "Bez Uników w Powietrzu", "Pokonajcie dowolnego bossa z Wyspy I lub II bez użycia Zrywu (Dash) przez kogokolwiek w drużynie!"),
            new AchievementEntry("SPEED_DEMON_75S", "Ekspresowy Nokaut", "Pokonajcie dowolnego bossa z Wyspy II lub III w czasie poniżej 1 minuty i 15 sekund (75 sekund)!"),
            new AchievementEntry("PARRY_STORM_15", "Różowa Nawałnica", "Zdobądźcie co najmniej 15 udanych parowań w trakcie jednego starcia z bossem jako drużyna!"),
            new AchievementEntry("HARDCORE_TRIO_STREAK", "Żelazny Tercet", "Pokonajcie 3 różnych bossów pod rząd w jednej sesji w trybie Hardcore bez ani jednego restartu!"),

            // Kategoria 9: Ekspert, restrykcje ekwipunku i zasady bojowe (118-138)
            new AchievementEntry("SUPER_SYNC_BURST", "Potrójny Wybuch Mocy", "Wszyscy 3 gracze odpalają Supercios w odstępie poniżej 4 sekund od siebie!"),
            new AchievementEntry("VEGGIES_NO_RADISH", "Oszczędzona Rzodkiewka", "Pokonajcie Bandę Korzeniaków (The Root Pack) nie doprowadzając do zniszczenia małej rzodkiewki (sekretna faza bez płaczu)!"),
            new AchievementEntry("VEGGIES_LEFT_HALF_ONLY", "Okopani Ogrodnicy", "Pokonajcie Bandę Korzeniaków nie przekraczając linii środka areny przez całe starcie!"),
            new AchievementEntry("VEGGIES_EXPERT_WIN", "Kompost na Ekspercie", "Pokonajcie Bandę Korzeniaków na poziomie Ekspert w 3 osoby."),
            new AchievementEntry("SLIME_TOMB_DUCK", "Niski Pokłon dla Gluta", "W finałowej fazie Nagrobka Wielkiego Gluta (Goopy Le Grande) zadajcie nokaut kucając pod nagrobkiem!"),
            new AchievementEntry("SLIME_NO_PUNCH_HIT", "Unik Bokserski", "Pokonajcie fazę 2 Wielkiego Gluta bez otrzymania ani jednego ciosu bokserską rękawicą przez żadnego gracza!"),
            new AchievementEntry("SLIME_EXPERT_WIN", "Śliski Ekspert", "Pokonajcie Wielkiego Gluta na poziomie Ekspert w 3 osoby."),
            new AchievementEntry("FROGS_SLOT_PARRY_CHAIN", "Potrójny Jackpot", "W fazie automatu żab (Ribby and Croaks) wykonajcie 3 parowania wajchy pod rząd bez dotknięcia podłogi między parowaniami!"),
            new AchievementEntry("FROGS_NO_COIN_HIT", "Omijacze Żetonów", "W finałowej fazie żab pokonajcie bossa bez uderzenia przez wysuwane platformy żetonów!"),
            new AchievementEntry("FROGS_EXPERT_WIN", "Żabi Kwartet na Ekspercie", "Pokonajcie Żebraka i Skrzeka na poziomie Ekspert w 3 osoby."),
            new AchievementEntry("FLOWER_NO_FLOATING_PLATFORMS", "Korzenie w Glebie", "Pokonajcie Cagneya Goździka (Floral Fury) stojąc WYŁĄCZNIE na dolnej ziemi (ani razu nie wchodząc na 3 latające platformy)!"),
            new AchievementEntry("FLOWER_PARRY_SEED_STORM", "Nasiona pod Kontrolą", "Sparujcie co najmniej 5 różowych nasion lecących z góry w trakcie jednego starcia z Cagneyem!"),
            new AchievementEntry("FLOWER_EXPERT_WIN", "Ścięty Pączek na Ekspercie", "Pokonajcie Cagneya Goździka na poziomie Ekspert w 3 osoby."),
            new AchievementEntry("BLIMP_NO_CONSTELLATION_DAMAGE", "Astrologiczny Błysk", "Pokonajcie Hildę Berg (Threatenin' Zeppelin) nie otrzymując obrażeń w żadnej z form zodiaku (Byk, Bliźnięta, Strzelec)!"),
            new AchievementEntry("BLIMP_MOON_BLITZ", "Zaćmienie Księżyca", "W finałowej fazie Półksiężyca Hildy Berg znokautujcie ją w czasie poniżej 25 sekund!"),
            new AchievementEntry("ISLE_1_EXPERT_CLEARED", "Władcy Pierwszej Wyspy na Ekspercie", "Pokonajcie wszystkich 5 bossów Wyspy Kałamarz I na poziomie Ekspert w 3 osoby!"),
            new AchievementEntry("BARONESS_NO_MINION_KILL", "Cukrowy Pacyfizm", "Pokonajcie Baronową von Bon Bon nie zabijając ani jednego biegającego żelka na dole areny!"),
            new AchievementEntry("BARONESS_CASTLE_PARRY", "Ucieczka przed Zamkiem", "W finałowej fazie goniącego zamku sparujcie toczącą się miętówkę co najmniej 3 razy jako drużyna!"),
            new AchievementEntry("BARONESS_EXPERT_WIN", "Słodki Rygor na Ekspercie", "Pokonajcie Baronową von Bon Bon na poziomie Ekspert w 3 osoby."),
            new AchievementEntry("CLOWN_NO_COASTER_HIT", "Uważaj na Wagony!", "Pokonajcie Klauna Beppiego (Carnival Kerfuffle), gdy żaden gracz ani razu nie zostanie potrącony przez kolejkę górską!"),

            // Kategoria 10: Scenariusze sytuacyjne poszczególnych bossów (139-187)
            new AchievementEntry("CLOWN_HORSE_PARRY", "Karuzela w Chmurach", "W fazie konia Beppiego sparujcie podkowy co najmniej 4 razy przed przejściem do kolejnej fazy!"),
            new AchievementEntry("CLOWN_EXPERT_WIN", "Koniec Błaznowania na Ekspercie", "Pokonajcie Klauna Beppiego na poziomie Ekspert w 3 osoby."),
            new AchievementEntry("GENIE_PUPPET_BLITZ", "Kukiełka w Strzępach", "W fazie 4 Dżina Wielkiego (Pyramid Peril) zniszczcie marionetkę Cuppeta w czasie poniżej 15 sekund!"),
            new AchievementEntry("GENIE_SARCOPHAGUS_CLEAN", "Grobowy Spokój", "Przejdźcie fazę sarkofagu Dżina bez ani jednego zgonu w drużynie!"),
            new AchievementEntry("GENIE_EXPERT_WIN", "Ujarzmiony Dżin na Ekspercie", "Pokonajcie Dżina Wielkiego na poziomie Ekspert w 3 osoby."),
            new AchievementEntry("BIRD_SON_PARRY_ROTATION", "Gniazdo na Jajkach", "W fazie 3 synalka Wally'ego Trele sparujcie wirujące różowe jajka przez co najmniej 2 różnych graczy!"),
            new AchievementEntry("BIRD_EXPERT_WIN", "Oskubany Wally na Ekspercie", "Pokonajcie Wally'ego Trele na poziomie Ekspert w 3 osoby."),
            new AchievementEntry("DRAGON_NO_SPLIT_FIREBALLS", "Nie Drażnij Ognia!", "W finałowej fazie 3 głów Ponurego Zapałki (Grim Matchstick) nie rozbijcie ani jednej kuli ognia na mniejsze odłamki!"),
            new AchievementEntry("DRAGON_TAIL_DODGE", "Smoczy Ogon", "W fazie 1 smoka uniknijcie uderzenia ogona z dołu bez otrzymania obrażeń przez któregokolwiek gracza!"),
            new AchievementEntry("DRAGON_EXPERT_WIN", "Smoczy Pogromcy na Ekspercie", "Pokonajcie Ponurego Zapałkę na poziomie Ekspert w 3 osoby."),
            new AchievementEntry("ISLE_2_EXPERT_CLEARED", "Mistrzowie Drugiej Wyspy na Ekspercie", "Pokonajcie wszystkich 5 bossów Wyspy Kałamarz II na poziomie Ekspert w 3 osoby!"),
            new AchievementEntry("BEE_NO_HONEY_DIP", "Czyste Buty", "Pokonajcie Królową Pszczół (Rumor Honeybottoms) bez dotknięcia zabójczego miodu na dole ekranu przez kogokolwiek przez całą walkę!"),
            new AchievementEntry("BEE_AIRPLANE_SHRED", "Rojowisko w Dymie", "W finałowej fazie samolotu piły mechanicznej zniszczcie Królową Pszczół w czasie poniżej 20 sekund!"),
            new AchievementEntry("BEE_EXPERT_WIN", "Królewski Miód na Ekspercie", "Pokonajcie Plotkę Miododół na poziomie Ekspert w 3 osoby."),
            new AchievementEntry("PIRATE_NO_WATER_FALL", "Szczury Lądowe", "Pokonajcie Kapitana Solnobrodego (Shootin N' Lootin), gdy żaden gracz ani razu nie wpadnie do morza!"),
            new AchievementEntry("PIRATE_BEAM_JUMP", "Piekielna Paszcza Statku", "W finałowej fazie statku przeskoczcie nad promieniem lasera z paszczy statku wszystkimi trzema graczami jednocześnie!"),
            new AchievementEntry("PIRATE_EXPERT_WIN", "Kamraci na Ekspercie", "Pokonajcie Kapitana Solnobrodego na poziomie Ekspert w 3 osoby."),
            new AchievementEntry("THEATRE_NO_PARASOL_HIT", "Widzowie w Pierwszym Rzędzie", "W finałowej fazie Sally Teatralnej (Dramatic Delirium) pokonajcie ją bez ani jednego trafienia wirującym parasolem!"),
            new AchievementEntry("THEATRE_EXPERT_WIN", "Dramat w Trzech Aktach na Ekspercie", "Pokonajcie Sally Teatralną na poziomie Ekspert w 3 osoby."),
            new AchievementEntry("MERMAID_QUICK_UNSTONE", "Przełamana Klątwa Meduzy", "Uwolnijcie się ze skamieniałości (Stone Gaze) Cali Marii w czasie poniżej 1.2 sekundy przez energiczne ruchy gałką!"),
            new AchievementEntry("MERMAID_EXPERT_WIN", "Głębinowa Pieśń na Ekspercie", "Pokonajcie Calę Marię na poziomie Ekspert w 3 osoby."),
            new AchievementEntry("ROBOT_SIMULTANEOUS_BREAK", "Równoległy Demontaż", "W fazie 1 Robota Doktora Kahla zniszczcie wszystkie 3 części (głowa, pierś, brzuch) w odstępie poniżej 12 sekund od siebie!"),
            new AchievementEntry("ROBOT_EXPERT_WIN", "Złomowisko Kahla na Ekspercie", "Pokonajcie Robota Doktora Kahla na poziomie Ekspert w 3 osoby."),
            new AchievementEntry("MOUSE_CAT_DUCK_JUMP", "Kocia Zasadzka", "W finałowej fazie kota Wernera Wermana (Murine Corps) wszyscy 3 gracze przeskakują nad łapą kota bez otrzymania ciosu!"),
            new AchievementEntry("MOUSE_EXPERT_WIN", "Puszkowy Gryzoń na Ekspercie", "Pokonajcie Wernera Wermana na poziomie Ekspert w 3 osoby."),
            new AchievementEntry("TRAIN_EXPERT_WIN", "Ekspres Widmo na Ekspercie", "Pokonajcie Ekspres Widmo na poziomie Ekspert w 3 osoby."),
            new AchievementEntry("CIGAR_NO_ASH_DAMAGE", "Bez Poparzeń na Cygarze", "Pokonajcie Pana Zadyszkę (Mr. Wheezy) bez ani jednego wpadnięcia w ogień/popiół między popielniczkami!"),
            new AchievementEntry("MEMORY_NO_WRONG_FLIP", "Genialna Pamięć", "W starciu z Panem Dzwonkiem (Mr. Chimes) odkrywajcie wyłącznie pasujące pary kart bez ani jednej pomyłki!"),
            new AchievementEntry("ROULETTE_NO_BALL_HIT", "Piruet nad Ruletką", "Pokonajcie Piroulettę przeskakując po żetonach bez dotknięcia jej sukni ani kulek ruletki!"),
            new AchievementEntry("KING_DICE_15_CARD_PARRIES", "Karciany Wirtuoz Króla", "W trakcie ataku marszu kart Kościanego Króla wykonajcie co najmniej 15 parowań kart bez dotknięcia ziemi!"),
            new AchievementEntry("KING_DICE_NO_DEATHS", "Szulernia bez Strat", "Pokonajcie Kościanego Króla i co najmniej 4 minibossów kasyna bez ani jednego zgonu w drużynie!"),
            new AchievementEntry("KING_DICE_EXPERT_WIN", "Koniec Szulerki na Ekspercie", "Pokonajcie Kościanego Króla na poziomie Ekspert w 3 osoby!"),
            new AchievementEntry("DEVIL_PHASE_1_FLAWLESS", "Piekielny Spokój", "Ukończcie całą fazę 1 Diabła (The Devil) na tronie bez ani jednego trafienia przez ogień, kozły lub pająka!"),
            new AchievementEntry("DEVIL_FINAL_STAND_TRIO", "Ostatnia Platforma w Piekle", "W finałowej fazie z 1 platformą przetrwajcie we trzech przez co najmniej 20 sekund i znokautujcie Diabła!"),
            new AchievementEntry("DEVIL_TEARS_PARRY", "Różowe Łzy Szatana", "Sparujcie spadające fioletowe/różowe łzy Diabła co najmniej 4 razy w trakcie jednej walki!"),
            new AchievementEntry("DEVIL_EXPERT_HARDCORE", "Piekielny Egzorcyzm na Ekspercie", "Pokonajcie Diabła w 3 osoby na poziomie Ekspert z włączonym trybem Hardcore!"),
            new AchievementEntry("DEVIL_EXPERT_WIN", "Pokonany Belzebub", "Pokonajcie Diabła na poziomie Ekspert w 3 osoby!"),
            new AchievementEntry("ISLE_3_EXPERT_CLEARED", "Zdobywcy Metropolii na Ekspercie", "Pokonajcie wszystkich 7 bossów Wyspy Kałamarz III na poziomie Ekspert w 3 osoby!"),
            new AchievementEntry("BASE_GAME_ALL_EXPERT", "Spłacony Cyrograf na Ekspercie", "Pokonajcie wszystkich bossów podstawowej gry na poziomie Ekspert w 3 osoby!"),
            new AchievementEntry("GLUMSTONE_EXPERT_WIN", "Broda Olbrzyma na Ekspercie", "Pokonajcie Olbrzyma Ponuraka (Glumstone The Giant) na poziomie Ekspert w 3 osoby!"),
            new AchievementEntry("MOONSHINE_SNAIL_FINISH", "Fałszywy Nokaut Ślimaka", "W finałowej fazie zaskakującego ślimaka Bimbrowniczej Szajki znokautujcie go w czasie poniżej 3 sekund od pojawienia się!"),
            new AchievementEntry("MOONSHINE_EXPERT_WIN", "Nalot na Bimbrownię na Ekspercie", "Pokonajcie Bimbrowniczą Szajkę na poziomie Ekspert w 3 osoby!"),
            new AchievementEntry("ESTHER_EXPERT_WIN", "Szeryf na Zagrodzie na Ekspercie", "Pokonajcie Esterę Winchester na poziomie Ekspert w 3 osoby!"),
            new AchievementEntry("ACES_GRAVITY_MASTERY", "Mistrzowie Grawitacji", "W starciu z Wyjącymi Asami przetrwajcie obroty ekranu (widok pod kątem 90 i 180 stopni) bez utraty HP przez drużynę!"),
            new AchievementEntry("ACES_EXPERT_WIN", "Asy Przestworzy na Ekspercie", "Pokonajcie Wyjące Asy na poziomie Ekspert w 3 osoby!"),
            new AchievementEntry("FREEZE_EXPERT_WIN", "W Krainie Lodu na Ekspercie", "Pokonajcie Mortimera Zmroza na poziomie Ekspert w 3 osoby!"),
            new AchievementEntry("SALTBAKER_FALLING_GLASS", "Taniec na Szkle Solniczaka", "W finałowej fazie spadających kolumn przetrwajcie we trzech i znokautujcie serce Szefa Solniczaka!"),
            new AchievementEntry("SALTBAKER_EXPERT_WIN", "Kuchenna Rewolucja na Ekspercie", "Pokonajcie Szefa Solniczaka na poziomie Ekspert w 3 osoby!"),
            new AchievementEntry("CHESS_ALL_PERFECT", "Arcymistrzowie Królewskiego Skoku", "Ukończcie wszystkie wyzwania Królewskiego Skoku (Pionki, Skoczek, Goniec, Wieża, Królowa) bez utraty ani jednego serduszka!"),
            new AchievementEntry("GRAVEYARD_SECRET_EXPERT", "Anioł i Diabeł na Ekspercie", "Pokonajcie sekretnego bossa cmentarza na poziomie Ekspert w 3 osoby!"),
        };

        public static CoopStatsSaveData Data => _data;

        public static void Init()
        {
            Load();
        }

        public static string GetFilePath()
        {
            try
            {
                string dir = Paths.ConfigPath;
                if (!string.IsNullOrEmpty(dir))
                {
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                    return Path.Combine(dir, "Cuphead3PCoop_Stats.json");
                }
            }
            catch (Exception) { }

            try
            {
                string dir = Application.persistentDataPath;
                if (!string.IsNullOrEmpty(dir))
                {
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                    return Path.Combine(dir, "Cuphead3PCoop_Stats.json");
                }
            }
            catch (Exception) { }

            string baseDir = Path.Combine(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BepInEx"), "config");
            if (!Directory.Exists(baseDir)) Directory.CreateDirectory(baseDir);
            return Path.Combine(baseDir, "Cuphead3PCoop_Stats.json");
        }

        public static void Load()
        {
            bool fileExisted = false;
            try
            {
                string path = GetFilePath();
                if (File.Exists(path))
                {
                    fileExisted = true;
                    string json = File.ReadAllText(path, Encoding.UTF8);
                    var dict = SimpleJson.Deserialize(json) as Dictionary<string, object>;
                    if (dict != null)
                    {
                        PopulateFromJson(_data, dict);
                    }
                }
            }
            catch (Exception e)
            {
                if (Plugin.Log != null)
                {
                    Plugin.Log.LogWarning("[3P-Coop] Failed to load stats: " + e.Message);
                }
            }

            bool needSave = !fileExisted;

            if (_data == null) { _data = new CoopStatsSaveData(); needSave = true; }
            if (_data.p1 == null) { _data.p1 = new PlayerStatsData(); needSave = true; }
            if (_data.p2 == null) { _data.p2 = new PlayerStatsData(); needSave = true; }
            if (_data.p3 == null) { _data.p3 = new PlayerStatsData(); needSave = true; }
            if (_data.beatenLevels == null) { _data.beatenLevels = new List<string>(); needSave = true; }
            if (_data.achievements == null) { _data.achievements = new List<AchievementEntry>(); needSave = true; }

            // Synchronize achievements strictly with DefaultAchievements (preserving unlocked state, updating descriptions, removing obsolete grinds)
            var syncedList = new List<AchievementEntry>();
            foreach (var def in DefaultAchievements)
            {
                AchievementEntry existing = null;
                for (int i = 0; i < _data.achievements.Count; i++)
                {
                    if (_data.achievements[i].id == def.id)
                    {
                        existing = _data.achievements[i];
                        break;
                    }
                }

                if (existing != null)
                {
                    if (existing.title != def.title || existing.description != def.description)
                    {
                        existing.title = def.title;
                        existing.description = def.description;
                        needSave = true;
                    }
                    syncedList.Add(existing);
                }
                else
                {
                    syncedList.Add(new AchievementEntry(def.id, def.title, def.description));
                    needSave = true;
                }
            }

            if (_data.achievements.Count != syncedList.Count) needSave = true;
            _data.achievements = syncedList;

            if (needSave)
            {
                Save();
            }
        }

        private static void PopulateFromJson(CoopStatsSaveData data, Dictionary<string, object> dict)
        {
            if (data == null || dict == null) return;

            if (dict.ContainsKey("p1") && dict["p1"] is Dictionary<string, object>)
                PopulatePlayerStats(data.p1, (Dictionary<string, object>)dict["p1"]);
            if (dict.ContainsKey("p2") && dict["p2"] is Dictionary<string, object>)
                PopulatePlayerStats(data.p2, (Dictionary<string, object>)dict["p2"]);
            if (dict.ContainsKey("p3") && dict["p3"] is Dictionary<string, object>)
                PopulatePlayerStats(data.p3, (Dictionary<string, object>)dict["p3"]);

            if (dict.ContainsKey("totalTeamDamage")) data.totalTeamDamage = Convert.ToSingle(dict["totalTeamDamage"], CultureInfo.InvariantCulture);
            if (dict.ContainsKey("totalTeamParries")) data.totalTeamParries = Convert.ToInt32(dict["totalTeamParries"]);
            if (dict.ContainsKey("totalTeamRevives")) data.totalTeamRevives = Convert.ToInt32(dict["totalTeamRevives"]);
            if (dict.ContainsKey("totalTeamDeaths")) data.totalTeamDeaths = Convert.ToInt32(dict["totalTeamDeaths"]);
            if (dict.ContainsKey("totalTeamSupers")) data.totalTeamSupers = Convert.ToInt32(dict["totalTeamSupers"]);
            if (dict.ContainsKey("totalBossVictories")) data.totalBossVictories = Convert.ToInt32(dict["totalBossVictories"]);
            if (dict.ContainsKey("hardcoreBossVictories")) data.hardcoreBossVictories = Convert.ToInt32(dict["hardcoreBossVictories"]);
            if (dict.ContainsKey("hardcoreRestarts")) data.hardcoreRestarts = Convert.ToInt32(dict["hardcoreRestarts"]);

            if (dict.ContainsKey("beatenLevels") && dict["beatenLevels"] is List<object>)
            {
                var levels = (List<object>)dict["beatenLevels"];
                data.beatenLevels.Clear();
                for (int i = 0; i < levels.Count; i++)
                {
                    if (levels[i] != null) data.beatenLevels.Add(levels[i].ToString());
                }
            }

            if (dict.ContainsKey("achievements") && dict["achievements"] is List<object>)
            {
                var achList = (List<object>)dict["achievements"];
                data.achievements.Clear();
                for (int i = 0; i < achList.Count; i++)
                {
                    if (achList[i] is Dictionary<string, object>)
                    {
                        var achDict = (Dictionary<string, object>)achList[i];
                        var entry = new AchievementEntry();
                        if (achDict.ContainsKey("id") && achDict["id"] != null) entry.id = achDict["id"].ToString();
                        if (achDict.ContainsKey("title") && achDict["title"] != null) entry.title = achDict["title"].ToString();
                        if (achDict.ContainsKey("description") && achDict["description"] != null) entry.description = achDict["description"].ToString();
                        if (achDict.ContainsKey("unlocked") && achDict["unlocked"] != null) entry.unlocked = Convert.ToBoolean(achDict["unlocked"]);
                        if (achDict.ContainsKey("unlockDate") && achDict["unlockDate"] != null) entry.unlockDate = achDict["unlockDate"].ToString();
                        data.achievements.Add(entry);
                    }
                }
            }
        }

        private static void PopulatePlayerStats(PlayerStatsData st, Dictionary<string, object> dict)
        {
            if (st == null || dict == null) return;
            if (dict.ContainsKey("damageDealt")) st.damageDealt = Convert.ToSingle(dict["damageDealt"], CultureInfo.InvariantCulture);
            if (dict.ContainsKey("parries")) st.parries = Convert.ToInt32(dict["parries"]);
            if (dict.ContainsKey("revivesGiven")) st.revivesGiven = Convert.ToInt32(dict["revivesGiven"]);
            if (dict.ContainsKey("revivesReceived")) st.revivesReceived = Convert.ToInt32(dict["revivesReceived"]);
            if (dict.ContainsKey("deaths")) st.deaths = Convert.ToInt32(dict["deaths"]);
            if (dict.ContainsKey("supersUsed")) st.supersUsed = Convert.ToInt32(dict["supersUsed"]);
            if (dict.ContainsKey("bossVictories")) st.bossVictories = Convert.ToInt32(dict["bossVictories"]);
        }

        public static void Save()
        {
            try
            {
                string path = GetFilePath();
                string json = SimpleJson.Serialize(_data);
                File.WriteAllText(path, json, Encoding.UTF8);
                _isDirty = false;
                _lastSaveTime = Time.unscaledTime;
            }
            catch (Exception e)
            {
                if (Plugin.Log != null)
                {
                    Plugin.Log.LogWarning("[3P-Coop] Failed to save stats: " + e.Message);
                }
            }
        }

        public static void Flush()
        {
            if (_isDirty)
            {
                Save();
            }
        }

        public static void Tick()
        {
            if (_isDirty && Time.unscaledTime - _lastSaveTime > 2.0f)
            {
                Save();
            }

            if (Level.Current == null) return;
            var all = PlayerManager.GetAllPlayers();
            if (all == null) return;

            foreach (var player in all)
            {
                var p = player as LevelPlayerController;
                if (p != null && p.motor != null)
                {
                    int id = p.motor.GetInstanceID();
                    if (!_hookedMotors.Contains(id))
                    {
                        _hookedMotors.Add(id);
                        p.motor.OnJumpEvent += OnPlayerJumped;
                        p.motor.OnDashStartEvent += OnPlayerDashed;
                    }
                }
            }
        }

        private static void OnPlayerJumped()
        {
            _battleJumpsCount++;
        }

        private static void OnPlayerDashed()
        {
            _battleDashesCount++;
        }

        public static PlayerStatsData GetPlayerStats(PlayerId id)
        {
            switch (id)
            {
                case PlayerId.PlayerOne: return _data.p1;
                case PlayerId.PlayerTwo: return _data.p2;
                case PlayerId.PlayerThree: return _data.p3;
                default: return null;
            }
        }

        public static void ResetAllStats()
        {
            _data = new CoopStatsSaveData();
            foreach (var def in DefaultAchievements)
            {
                _data.achievements.Add(new AchievementEntry(def.id, def.title, def.description));
            }
            Save();
            ControllerManager.ShowToast("Statystyki i osiągnięcia zresetowane do zera!");
        }

        public static void AwardAchievement(string id)
        {
            if (_data == null || _data.achievements == null) return;

            AchievementEntry entry = null;
            for (int i = 0; i < _data.achievements.Count; i++)
            {
                if (_data.achievements[i].id == id)
                {
                    entry = _data.achievements[i];
                    break;
                }
            }

            if (entry == null || entry.unlocked) return;

            entry.unlocked = true;
            entry.unlockDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            Save();

            string toast = string.Format("[OSIĄGNIĘCIE] {0} ({1})", entry.title.ToUpper(), entry.description);
            ControllerManager.ShowToast(toast, 5.0f);
            Plugin.Log.LogMessage("[3P-Coop] UNLOCKED ACHIEVEMENT: " + entry.title + " (" + id + ")");

            CheckMetaAchievements();
        }

        private static void CheckMetaAchievements()
        {
            if (_data == null || _data.achievements == null) return;

            int count = 0;
            for (int i = 0; i < _data.achievements.Count; i++)
            {
                if (_data.achievements[i].unlocked) count++;
            }

            if (count >= 60)
            {
                var pData = PlayerData.Data;
                if (pData != null && IsLevelExpert(pData, Levels.Devil) && IsLevelExpert(pData, Levels.Saltbaker))
                {
                    AwardAchievement("LEGENDARY_TRIO");
                }
            }
        }

        public static void OnLevelStart(Level level)
        {
            _battleStartTime = Time.time;
            _battleDeaths = 0;
            _battleRevives = 0;
            _battleParriesCount = 0;
            _battleDamageTakenCount = 0;
            _battleExCount = 0;
            _battleSupersCount = 0;
            _battleJumpsCount = 0;
            _battleDashesCount = 0;
            _lastReviveTime = 0f;
            _lastRevivedPlayer = PlayerId.None;
            _lastRevivedPlayerTime = 0f;

            _battleSupers.Clear();
            _battleSuperTimes.Clear();
            _playersDamagedInBattle.Clear();
            _playersWhoDiedInBattle.Clear();
            _singlePlayerRevivesInBattle.Clear();
            _deathTimes.Clear();
            _deathCountsInBattle.Clear();
            _hookedMotors.Clear();

            int playerCount = 0;
            var all = PlayerManager.GetAllPlayers();
            if (all != null)
            {
                foreach (var p in all)
                {
                    if (p != null) playerCount++;
                }
            }

            if (playerCount >= 3)
            {
                AwardAchievement("TRIO_INIT");
            }
        }

        public static void RecordDamage(PlayerId id, float dmg)
        {
            if (dmg <= 0f) return;

            var st = GetPlayerStats(id);
            if (st != null) st.damageDealt += dmg;

            _data.totalTeamDamage += dmg;
            if (_data.totalTeamDamage >= 10000f) AwardAchievement("DMG_10K");
            if (_data.totalTeamDamage >= 25000f) AwardAchievement("DMG_25K");
            if (_data.totalTeamDamage >= 50000f) AwardAchievement("DMG_50K");
            if (_data.totalTeamDamage >= 100000f) AwardAchievement("DMG_100K");
            if (_data.totalTeamDamage >= 250000f) AwardAchievement("DMG_250K");
            _isDirty = true;
        }

        public static void RecordDamageTaken(PlayerId id)
        {
            _battleDamageTakenCount++;
            if (id != PlayerId.None && id != PlayerId.Any)
            {
                _playersDamagedInBattle.Add(id);
            }
        }

        public static void RecordEx(PlayerId id)
        {
            _battleExCount++;
        }

        public static void RecordParry(PlayerStatsManager stats)
        {
            if (stats == null || stats.basePlayer == null) return;
            PlayerId id = stats.basePlayer.id;

            var st = GetPlayerStats(id);
            if (st != null) st.parries++;

            _data.totalTeamParries++;
            _battleParriesCount++;

            AwardAchievement("PARRY_FIRST");
            if (_data.totalTeamParries >= 50) AwardAchievement("PARRY_50");
            if (_data.totalTeamParries >= 150) AwardAchievement("PARRY_150");
            if (_data.totalTeamParries >= 300) AwardAchievement("PARRY_300");

            if (_battleParriesCount >= 5) AwardAchievement("PARRY_CHAIN");
            if (_battleParriesCount >= 15) AwardAchievement("PARRY_STORM_15");

            if (Level.Current != null)
            {
                if (Level.Current.CurrentLevel == Levels.DicePalaceMain && _battleParriesCount >= 15)
                {
                    AwardAchievement("KING_DICE_15_CARD_PARRIES");
                }
                if (Level.Current.CurrentLevel == Levels.Devil && _battleParriesCount >= 4)
                {
                    AwardAchievement("DEVIL_TEARS_PARRY");
                }
                if (Level.Current.CurrentLevel == Levels.Flower && _battleParriesCount >= 5)
                {
                    AwardAchievement("FLOWER_PARRY_SEED_STORM");
                }
                if (Level.Current.CurrentLevel == Levels.Baroness && _battleParriesCount >= 3)
                {
                    AwardAchievement("BARONESS_CASTLE_PARRY");
                }
                if (Level.Current.CurrentLevel == Levels.Frogs && _battleParriesCount >= 3)
                {
                    AwardAchievement("FROGS_SLOT_PARRY_CHAIN");
                }
            }

            _isDirty = true;
        }

        public static void RecordSuper(PlayerStatsManager stats)
        {
            if (stats == null || stats.basePlayer == null) return;
            PlayerId id = stats.basePlayer.id;

            var st = GetPlayerStats(id);
            if (st != null) st.supersUsed++;

            _data.totalTeamSupers++;
            _battleSupersCount++;
            _battleSupers.Add(id);
            _battleSuperTimes[id] = Time.time;

            if (_data.totalTeamSupers >= 10) AwardAchievement("SUPERS_10");
            if (_data.totalTeamSupers >= 50) AwardAchievement("SUPERS_50");
            if (_data.totalTeamSupers >= 100) AwardAchievement("SUPERS_100");

            if (_battleSupers.Contains(PlayerId.PlayerOne) &&
                _battleSupers.Contains(PlayerId.PlayerTwo) &&
                _battleSupers.Contains(PlayerId.PlayerThree))
            {
                AwardAchievement("SUPER_SYNC");
            }

            if (_battleSuperTimes.ContainsKey(PlayerId.PlayerOne) &&
                _battleSuperTimes.ContainsKey(PlayerId.PlayerTwo) &&
                _battleSuperTimes.ContainsKey(PlayerId.PlayerThree))
            {
                float t1 = _battleSuperTimes[PlayerId.PlayerOne];
                float t2 = _battleSuperTimes[PlayerId.PlayerTwo];
                float t3 = _battleSuperTimes[PlayerId.PlayerThree];
                float spread = Mathf.Max(t1, Mathf.Max(t2, t3)) - Mathf.Min(t1, Mathf.Min(t2, t3));
                if (spread <= 4.0f)
                {
                    AwardAchievement("SUPER_SYNC_BURST");
                }
            }

            _isDirty = true;
        }

        public static void RecordDeath(PlayerId id)
        {
            if (id == PlayerId.None || id == PlayerId.Any) return;

            var st = GetPlayerStats(id);
            if (st != null) st.deaths++;

            _data.totalTeamDeaths++;
            _battleDeaths++;
            _playersDamagedInBattle.Add(id);
            _playersWhoDiedInBattle.Add(id);
            _deathTimes[id] = Time.unscaledTime;
            int dCount;
            _deathCountsInBattle.TryGetValue(id, out dCount);
            _deathCountsInBattle[id] = dCount + 1;

            if (_data.totalTeamDeaths >= 10) AwardAchievement("DEATHS_10");
            if (_data.totalTeamDeaths >= 50) AwardAchievement("DEATHS_50");

            _isDirty = true;
        }

        public static void RecordHardcoreRestart()
        {
            _data.hardcoreRestarts++;
            _hardcoreSessionStreak = 0;
            if (_data.hardcoreRestarts >= 10) AwardAchievement("HARDCORE_WIPEOUT");
            Save();
        }

        public static void RecordRevive(PlayerDeathParrySwitch deathSwitch, AbstractPlayerController rescuer)
        {
            if (deathSwitch == null || rescuer == null) return;

            var deathEffect = deathSwitch.GetComponentInParent<PlayerDeathEffect>() ?? deathSwitch.GetComponent<PlayerDeathEffect>();
            if (deathEffect == null) return;

            PlayerId fallenId = HardcoreGhostWatcher.GetPlayerId(deathEffect);
            PlayerId rescuerId = rescuer.id;
            float now = Time.unscaledTime;

            var rescuerStats = GetPlayerStats(rescuerId);
            if (rescuerStats != null) rescuerStats.revivesGiven++;

            var fallenStats = GetPlayerStats(fallenId);
            if (fallenStats != null) fallenStats.revivesReceived++;

            _data.totalTeamRevives++;
            _battleRevives++;

            AwardAchievement("MEDIC_ROOKIE");
            if (rescuerStats != null)
            {
                if (rescuerStats.revivesGiven >= 10) AwardAchievement("MEDIC_VETERAN");
                if (rescuerStats.revivesGiven >= 30) AwardAchievement("MEDIC_MASTER");
                if (rescuerStats.revivesGiven >= 60) AwardAchievement("MEDIC_GOD");
            }

            if (fallenStats != null)
            {
                if (fallenStats.revivesReceived >= 10) AwardAchievement("BULLET_MAGNET");
                if (fallenStats.revivesReceived >= 25) AwardAchievement("FREQUENT_FLYER");
            }

            int deathCount;
            _deathCountsInBattle.TryGetValue(fallenId, out deathCount);
            if (deathCount >= 3)
            {
                AwardAchievement("RESURRECTION_HERO");
            }

            float dTime;
            if (_deathTimes.TryGetValue(fallenId, out dTime))
            {
                if (now - dTime <= 1.0f) AwardAchievement("INSTANT_REVIVE");
                if (now - dTime >= 8.0f) AwardAchievement("PACIFIST_GHOST");
            }

            int rCount;
            _singlePlayerRevivesInBattle.TryGetValue(rescuerId, out rCount);
            rCount++;
            _singlePlayerRevivesInBattle[rescuerId] = rCount;

            if (rCount >= 4 && !_playersWhoDiedInBattle.Contains(rescuerId))
            {
                AwardAchievement("HERO_PARAMEDIC");
            }

            if (_battleRevives >= 6)
            {
                AwardAchievement("PHOENIX_COMEBACK");
            }

            // Clutch medic (rescuer has 1 HP)
            if (rescuer.stats != null && rescuer.stats.Health == 1)
            {
                AwardAchievement("CLUTCH_MEDIC");
            }

            // Clutch revive in hardcore mode
            if (HardcoreGhostWatcher.IsGhostInClutchWindow(deathEffect))
            {
                AwardAchievement("CLUTCH_REVIVE");
            }

            // Ghost near screen edge in hardcore
            try
            {
                var cam = CupheadLevelCamera.Current != null ? CupheadLevelCamera.Current.GetComponent<UnityEngine.Camera>() : UnityEngine.Camera.main;
                if (cam != null)
                {
                    Vector3 vp = cam.WorldToViewportPoint(deathEffect.transform.position);
                    if (vp.x > 0.88f || vp.x < 0.12f || vp.y > 0.88f || vp.y < 0.12f)
                    {
                        AwardAchievement("PHOTO_FINISH_REVIVE");
                    }
                }
            }
            catch { }

            // Chain revive (<4s between revives)
            if (now - _lastReviveTime < 4.0f && _battleRevives >= 2)
            {
                AwardAchievement("CHAIN_REVIVE");
            }

            // Clutch exchange (<5s mutual revival)
            if (_lastRevivedPlayer == rescuerId && now - _lastRevivedPlayerTime < 5.0f)
            {
                AwardAchievement("CLUTCH_EXCHANGE");
            }

            // Airborne medic
            var level = Level.Current;
            if (level != null && level.playerMode == PlayerMode.Plane)
            {
                AwardAchievement("AIRBORNE_MEDIC");
            }

            _lastReviveTime = now;
            _lastRevivedPlayer = fallenId;
            _lastRevivedPlayerTime = now;

            Save();
        }

        public static void OnLevelWin(Level level)
        {
            if (level == null) return;

            string levelName = level.CurrentLevel.ToString();
            if (!_data.beatenLevels.Contains(levelName))
            {
                _data.beatenLevels.Add(levelName);
            }

            bool isBoss = HardcoreGhostWatcher.IsBossLevel(level);
            bool isExpert = level.mode == Level.Mode.Hard || Level.CurrentMode == Level.Mode.Hard || Level.Difficulty == Level.Mode.Hard;
            float duration = Time.time - _battleStartTime;

            if (isBoss)
            {
                _data.totalBossVictories++;
                if (Plugin.RestartOnGhostLost != null && Plugin.RestartOnGhostLost.Value)
                {
                    _data.hardcoreBossVictories++;
                    AwardAchievement("HARDCORE_FIRST_WIN");
                    if (_data.hardcoreBossVictories >= 5) AwardAchievement("HARDCORE_CHAMPION");
                    if (_data.hardcoreBossVictories >= 10) AwardAchievement("HARDCORE_VETERAN");
                    if (_data.hardcoreBossVictories >= 20) AwardAchievement("HARDCORE_MASTER");
                }
                if (isExpert)
                {
                    AwardAchievement("EXPERT_FIRST_WIN");
                    SyncExpertLevelBeaten(level.CurrentLevel);
                }
            }

            // Check living players
            var allPlayers = PlayerManager.GetAllPlayers();
            int aliveCount = 0;
            int totalExist = 0;
            bool p1Alive = false;
            bool p2Alive = false;
            bool p3Alive = false;
            bool allOneHp = true;

            if (allPlayers != null)
            {
                foreach (var p in allPlayers)
                {
                    if (p != null)
                    {
                        totalExist++;
                        if (!p.IsDead)
                        {
                            aliveCount++;
                            var st = GetPlayerStats(p.id);
                            if (st != null && isBoss) st.bossVictories++;

                            if (p.id == PlayerId.PlayerOne) p1Alive = true;
                            if (p.id == PlayerId.PlayerTwo) p2Alive = true;
                            if (p.id == PlayerId.PlayerThree) p3Alive = true;

                            if (p.stats != null && p.stats.Health != 1) allOneHp = false;
                        }
                    }
                }
            }

            // 1. Team Survival Scenarios
            if (p1Alive && p2Alive && p3Alive)
            {
                AwardAchievement("TRIO_SURVIVORS");
                if (allOneHp) AwardAchievement("ALL_ONE_HP");
                if (_playersWhoDiedInBattle.Contains(PlayerId.PlayerOne) &&
                    _playersWhoDiedInBattle.Contains(PlayerId.PlayerTwo) &&
                    _playersWhoDiedInBattle.Contains(PlayerId.PlayerThree))
                {
                    AwardAchievement("NO_MAN_LEFT_BEHIND");
                }
            }

            if (_battleDeaths == 0 && isBoss)
            {
                AwardAchievement("FLAWLESS_VICTORY");
            }

            if (aliveCount >= 3 && isBoss)
            {
                AwardAchievement("TRIPLE_FINISH");
            }

            if (_battleRevives >= 5)
            {
                AwardAchievement("TEAM_REVIVAL_STREAK");
            }

            if (_playersDamagedInBattle.Count >= 2)
            {
                AwardAchievement("SHARED_FATE");
            }

            // 2. Last man standing
            if (aliveCount == 1 && totalExist >= 3)
            {
                AwardAchievement("LAST_MAN_STANDING");
            }

            // 3. No damage run
            if (_battleDamageTakenCount == 0 && _battleDeaths == 0 && isBoss)
            {
                AwardAchievement("NO_DAMAGE_RUN");
                if (isExpert) AwardAchievement("EXPERT_FLAWLESS_TRIO");
            }

            // 4. Special restrictions
            if (isExpert && _battleExCount == 0 && _battleSupersCount == 0 && isBoss)
            {
                AwardAchievement("EXPERT_NO_EX_NO_SUPER");
            }

            if (HealthConfig.GetBaseHealth(PlayerId.PlayerOne) == 1 &&
                HealthConfig.GetBaseHealth(PlayerId.PlayerTwo) == 1 &&
                HealthConfig.GetBaseHealth(PlayerId.PlayerThree) == 1)
            {
                AwardAchievement("ONE_HP_CLUB");
                if (isExpert) AwardAchievement("ONE_HP_EXPERT");
            }

            if (isExpert && Plugin.ParryTimeFreeze != null && !Plugin.ParryTimeFreeze.Value && isBoss)
            {
                AwardAchievement("PARRY_FREEZE_OFF_WIN");
            }

            if (_battleDashesCount == 0 && isBoss)
            {
                AwardAchievement("NO_DASH_VICTORY");
            }

            if (_battleJumpsCount == 0 && (level.CurrentLevel == Levels.Veggies || level.CurrentLevel == Levels.RumRunners))
            {
                AwardAchievement("GROUNDED_NO_JUMP");
            }

            if (duration > 0f && duration <= 75.0f && isBoss)
            {
                AwardAchievement("SPEED_DEMON_75S");
            }

            // Loadouts check
            CheckLoadoutAchievements(isExpert);

            // Hardcore mode streak
            if (Plugin.RestartOnGhostLost != null && Plugin.RestartOnGhostLost.Value && isBoss)
            {
                _data.hardcoreBossVictories++;
                _hardcoreSessionStreak++;
                if (_hardcoreSessionStreak >= 3) AwardAchievement("HARDCORE_TRIO_STREAK");
                if (level.CurrentLevel == Levels.Devil && isExpert) AwardAchievement("DEVIL_EXPERT_HARDCORE");
            }

            // Specific Boss Achievements
            CheckBossLevelAchievements(level, p1Alive, p2Alive, p3Alive, isExpert);

            // Check Isle completions on Expert
            CheckIsleCompletions();

            Save();
        }

        private static void CheckLoadoutAchievements(bool isExpert)
        {
            try
            {
                var data = PlayerData.Data;
                if (data == null || data.Loadouts == null) return;
                var p1 = data.Loadouts.playerOne;
                var p2 = data.Loadouts.playerTwo;
                var p3 = data.Loadouts.playerThree;

                if (p1 != null && p2 != null && p3 != null)
                {
                    if (p1.primaryWeapon == Weapon.level_weapon_peashot && (p1.secondaryWeapon == Weapon.None || p1.secondaryWeapon == Weapon.level_weapon_peashot) &&
                        p2.primaryWeapon == Weapon.level_weapon_peashot && (p2.secondaryWeapon == Weapon.None || p2.secondaryWeapon == Weapon.level_weapon_peashot) &&
                        p3.primaryWeapon == Weapon.level_weapon_peashot && (p3.secondaryWeapon == Weapon.None || p3.secondaryWeapon == Weapon.level_weapon_peashot))
                    {
                        if (isExpert) AwardAchievement("EXPERT_PEASHOOTER_ONLY");
                    }

                    if (isExpert && p1.charm == Charm.None && p2.charm == Charm.None && p3.charm == Charm.None)
                    {
                        AwardAchievement("NO_CHARMS_EXPERT");
                    }

                    AwardAchievement("TRIO_PEASHOOTER");

                    if (isExpert && p1.charm == Charm.charm_chalice && p2.charm == Charm.charm_chalice && p3.charm == Charm.charm_chalice)
                    {
                        AwardAchievement("TRIPLE_CHALICE_SQUAD");
                    }

                    if (p3.charm == Charm.charm_chalice)
                    {
                        AwardAchievement("CHALICE_COOP");
                    }
                }
            }
            catch (Exception) { }
        }

        private static void CheckBossLevelAchievements(Level level, bool p1Alive, bool p2Alive, bool p3Alive, bool isExpert)
        {
            Levels lvl = level.CurrentLevel;

            // Inkwell Isle I
            if (lvl == Levels.Veggies)
            {
                AwardAchievement("BOSS_VEGGIES");
                if (isExpert) AwardAchievement("VEGGIES_EXPERT_WIN");
                AwardAchievement("VEGGIES_NO_RADISH");
                AwardAchievement("VEGGIES_LEFT_HALF_ONLY");
            }
            else if (lvl == Levels.Slime)
            {
                AwardAchievement("BOSS_SLIME");
                if (isExpert) AwardAchievement("SLIME_EXPERT_WIN");
                AwardAchievement("SLIME_TOMB_DUCK");
                AwardAchievement("SLIME_NO_PUNCH_HIT");
            }
            else if (lvl == Levels.Frogs)
            {
                AwardAchievement("BOSS_FROGS");
                if (isExpert) AwardAchievement("FROGS_EXPERT_WIN");
                AwardAchievement("FROGS_NO_COIN_HIT");
            }
            else if (lvl == Levels.Flower)
            {
                AwardAchievement("BOSS_FLOWER");
                if (isExpert) AwardAchievement("FLOWER_EXPERT_WIN");
                AwardAchievement("FLOWER_NO_FLOATING_PLATFORMS");
            }
            else if (lvl == Levels.FlyingBlimp)
            {
                AwardAchievement("BOSS_BLIMP");
                if (isExpert) AwardAchievement("BLIMP_EXPERT_WIN");
                AwardAchievement("BLIMP_NO_CONSTELLATION_DAMAGE");
                if (Time.time - _battleStartTime <= 90f) AwardAchievement("BLIMP_MOON_BLITZ");
            }

            // Inkwell Isle II
            else if (lvl == Levels.Baroness)
            {
                AwardAchievement("BOSS_BARONESS");
                if (isExpert) AwardAchievement("BARONESS_EXPERT_WIN");
                AwardAchievement("BARONESS_NO_MINION_KILL");
            }
            else if (lvl == Levels.Clown)
            {
                AwardAchievement("BOSS_CLOWN");
                if (isExpert) AwardAchievement("CLOWN_EXPERT_WIN");
                AwardAchievement("CLOWN_NO_COASTER_HIT");
                AwardAchievement("CLOWN_HORSE_PARRY");
            }
            else if (lvl == Levels.FlyingGenie)
            {
                AwardAchievement("BOSS_GENIE");
                if (isExpert) AwardAchievement("GENIE_EXPERT_WIN");
                AwardAchievement("GENIE_PUPPET_BLITZ");
                AwardAchievement("GENIE_SARCOPHAGUS_CLEAN");
            }
            else if (lvl == Levels.FlyingBird)
            {
                AwardAchievement("BOSS_BIRD");
                if (isExpert) AwardAchievement("BIRD_EXPERT_WIN");
                AwardAchievement("BIRD_SON_PARRY_ROTATION");
            }
            else if (lvl == Levels.Dragon)
            {
                AwardAchievement("BOSS_DRAGON");
                if (isExpert) AwardAchievement("DRAGON_EXPERT_WIN");
                AwardAchievement("DRAGON_NO_SPLIT_FIREBALLS");
                AwardAchievement("DRAGON_TAIL_DODGE");
            }

            // Inkwell Isle III
            else if (lvl == Levels.Bee)
            {
                AwardAchievement("BOSS_BEE");
                if (isExpert) AwardAchievement("BEE_EXPERT_WIN");
                AwardAchievement("BEE_NO_HONEY_DIP");
                AwardAchievement("BEE_AIRPLANE_SHRED");
            }
            else if (lvl == Levels.Pirate)
            {
                AwardAchievement("BOSS_PIRATE");
                if (isExpert) AwardAchievement("PIRATE_EXPERT_WIN");
                AwardAchievement("PIRATE_NO_WATER_FALL");
                AwardAchievement("PIRATE_BEAM_JUMP");
            }
            else if (lvl == Levels.SallyStagePlay)
            {
                AwardAchievement("BOSS_THEATRE");
                if (isExpert) AwardAchievement("THEATRE_EXPERT_WIN");
                AwardAchievement("THEATRE_NO_PARASOL_HIT");
            }
            else if (lvl == Levels.FlyingMermaid)
            {
                AwardAchievement("BOSS_MERMAID");
                if (isExpert) AwardAchievement("MERMAID_EXPERT_WIN");
                AwardAchievement("MERMAID_QUICK_UNSTONE");
            }
            else if (lvl == Levels.Robot)
            {
                AwardAchievement("BOSS_ROBOT");
                if (isExpert) AwardAchievement("ROBOT_EXPERT_WIN");
                AwardAchievement("ROBOT_SIMULTANEOUS_BREAK");
            }
            else if (lvl == Levels.Mouse)
            {
                AwardAchievement("BOSS_MOUSE");
                if (isExpert) AwardAchievement("MOUSE_EXPERT_WIN");
                AwardAchievement("MOUSE_CAT_DUCK_JUMP");
            }
            else if (lvl == Levels.Train)
            {
                AwardAchievement("BOSS_TRAIN");
                if (isExpert) AwardAchievement("TRAIN_EXPERT_WIN");
            }

            // Casino & Hell
            else if (lvl == Levels.DicePalaceBooze)
            {
                AwardAchievement("MINIBOSS_BOOZE");
            }
            else if (lvl == Levels.DicePalaceChips)
            {
                AwardAchievement("MINIBOSS_CHIPS");
            }
            else if (lvl == Levels.DicePalaceCigar)
            {
                AwardAchievement("MINIBOSS_CIGAR");
                AwardAchievement("CIGAR_NO_ASH_DAMAGE");
            }
            else if (lvl == Levels.DicePalaceDomino)
            {
                AwardAchievement("MINIBOSS_DOMINO");
            }
            else if (lvl == Levels.DicePalaceRabbit)
            {
                AwardAchievement("MINIBOSS_RABBIT");
            }
            else if (lvl == Levels.DicePalaceFlyingHorse)
            {
                AwardAchievement("MINIBOSS_HORSE");
            }
            else if (lvl == Levels.DicePalaceFlyingMemory)
            {
                AwardAchievement("MINIBOSS_MEMORY");
                AwardAchievement("MEMORY_NO_WRONG_FLIP");
            }
            else if (lvl == Levels.DicePalaceRoulette)
            {
                AwardAchievement("MINIBOSS_ROULETTE");
                AwardAchievement("ROULETTE_NO_BALL_HIT");
            }
            else if (lvl == Levels.DicePalaceEightBall)
            {
                AwardAchievement("MINIBOSS_8BALL");
            }
            else if (lvl == Levels.DicePalaceMain)
            {
                AwardAchievement("BOSS_KING_DICE");
                if (_battleDeaths == 0 && _battleDamageTakenCount == 0) AwardAchievement("CASINO_PERFECT");
                if (isExpert) AwardAchievement("KING_DICE_EXPERT_WIN");
                if (_battleDeaths == 0) AwardAchievement("KING_DICE_NO_DEATHS");
            }
            else if (lvl == Levels.Devil)
            {
                AwardAchievement("BOSS_DEVIL");
                if (isExpert) AwardAchievement("DEVIL_EXPERT_WIN");
                AwardAchievement("DEVIL_PHASE_1_FLAWLESS");
                if (p1Alive && p2Alive && p3Alive) AwardAchievement("DEVIL_FINAL_STAND_TRIO");
                if (p1Alive && p2Alive && p3Alive) AwardAchievement("DEVIL_ALL_ALIVE");
                if (_battleDeaths == 0) AwardAchievement("DEVIL_PHASE_1");
                if (Plugin.RestartOnGhostLost != null && Plugin.RestartOnGhostLost.Value) AwardAchievement("DEVIL_HARDCORE");
                try
                {
                    if (PlayerData.Data != null && PlayerData.Data.Loadouts != null &&
                        PlayerData.Data.Loadouts.playerThree != null &&
                        PlayerData.Data.Loadouts.playerThree.charm == Charm.charm_chalice)
                    {
                        AwardAchievement("DEVIL_CHALICE");
                    }
                }
                catch (Exception) { }
            }

            // DLC
            else if (lvl == Levels.OldMan)
            {
                AwardAchievement("BOSS_GLUMSTONE");
                if (isExpert) AwardAchievement("GLUMSTONE_EXPERT_WIN");
            }
            else if (lvl == Levels.RumRunners)
            {
                AwardAchievement("BOSS_MOONSHINE");
                if (isExpert) AwardAchievement("MOONSHINE_EXPERT_WIN");
                AwardAchievement("MOONSHINE_SNAIL_FINISH");
            }
            else if (lvl == Levels.FlyingCowboy)
            {
                AwardAchievement("BOSS_ESTHER");
                if (isExpert) AwardAchievement("ESTHER_EXPERT_WIN");
            }
            else if (lvl == Levels.Airplane)
            {
                AwardAchievement("BOSS_ACES");
                if (isExpert) AwardAchievement("ACES_EXPERT_WIN");
                AwardAchievement("ACES_GRAVITY_MASTERY");
            }
            else if (lvl == Levels.SnowCult)
            {
                AwardAchievement("BOSS_FREEZE");
                if (isExpert) AwardAchievement("FREEZE_EXPERT_WIN");
            }
            else if (lvl == Levels.Saltbaker)
            {
                AwardAchievement("BOSS_SALTBAKER");
                if (p1Alive && p2Alive && p3Alive) AwardAchievement("SALTBAKER_SURVIVAL");
                if (isExpert) AwardAchievement("SALTBAKER_EXPERT_WIN");
                if (p1Alive && p2Alive && p3Alive) AwardAchievement("SALTBAKER_FALLING_GLASS");
            }
            else if (lvl == Levels.Graveyard)
            {
                AwardAchievement("GRAVEYARD_SECRET");
                if (isExpert) AwardAchievement("GRAVEYARD_SECRET_EXPERT");
            }

            // Chess Gauntlet
            else if (lvl == Levels.ChessPawn)
            {
                AwardAchievement("CHESS_PAWNS");
                if (_battleDamageTakenCount == 0 && _battleDeaths == 0) AwardAchievement("CHESS_ALL_PERFECT");
            }
            else if (lvl == Levels.ChessKnight)
            {
                AwardAchievement("CHESS_KNIGHT");
                if (_battleDamageTakenCount == 0 && _battleDeaths == 0) AwardAchievement("CHESS_ALL_PERFECT");
            }
            else if (lvl == Levels.ChessBishop)
            {
                AwardAchievement("CHESS_BISHOP");
                if (_battleDamageTakenCount == 0 && _battleDeaths == 0) AwardAchievement("CHESS_ALL_PERFECT");
            }
            else if (lvl == Levels.ChessRook)
            {
                AwardAchievement("CHESS_ROOK");
                if (_battleDamageTakenCount == 0 && _battleDeaths == 0) AwardAchievement("CHESS_ALL_PERFECT");
            }
            else if (lvl == Levels.ChessQueen)
            {
                AwardAchievement("CHESS_QUEEN");
                if (_battleDamageTakenCount == 0 && _battleDeaths == 0) AwardAchievement("CHESS_ALL_PERFECT");
            }
            else if (lvl == Levels.ChessCastle)
            {
                AwardAchievement("CHESS_GAUNTLET");
                if (_battleDamageTakenCount == 0 && _battleDeaths == 0) AwardAchievement("CHESS_ALL_PERFECT");
            }

            // Run & Gun Pacifist
            if (level.playerMode == PlayerMode.Level && !HardcoreGhostWatcher.IsBossLevel(level))
            {
                if (Level.ScoringData != null && Level.ScoringData.pacifistRun)
                {
                    AwardAchievement("PACIFIST_RUN_AND_GUN");
                }
            }
        }

        private static void CheckIsleCompletions()
        {
            var pData = PlayerData.Data;
            if (pData == null) return;

            // Isle 1 Cleared
            if (IsLevelBeaten(pData, Levels.Veggies) && IsLevelBeaten(pData, Levels.Slime) &&
                IsLevelBeaten(pData, Levels.Frogs) && IsLevelBeaten(pData, Levels.Flower) &&
                IsLevelBeaten(pData, Levels.FlyingBlimp))
            {
                AwardAchievement("ISLE_1_CLEARED");
            }

            // Isle 2 Cleared
            if (IsLevelBeaten(pData, Levels.Baroness) && IsLevelBeaten(pData, Levels.Clown) &&
                IsLevelBeaten(pData, Levels.FlyingGenie) && IsLevelBeaten(pData, Levels.FlyingBird) &&
                IsLevelBeaten(pData, Levels.Dragon))
            {
                AwardAchievement("ISLE_2_CLEARED");
            }

            // Isle 3 Cleared
            if (IsLevelBeaten(pData, Levels.Bee) && IsLevelBeaten(pData, Levels.Pirate) &&
                IsLevelBeaten(pData, Levels.SallyStagePlay) && IsLevelBeaten(pData, Levels.FlyingMermaid) &&
                IsLevelBeaten(pData, Levels.Robot) && IsLevelBeaten(pData, Levels.Mouse) &&
                IsLevelBeaten(pData, Levels.Train))
            {
                AwardAchievement("ISLE_3_CLEARED");
            }

            // DLC Cleared
            if (IsLevelBeaten(pData, Levels.OldMan) && IsLevelBeaten(pData, Levels.RumRunners) &&
                IsLevelBeaten(pData, Levels.FlyingCowboy) && IsLevelBeaten(pData, Levels.Airplane) &&
                IsLevelBeaten(pData, Levels.SnowCult) && IsLevelBeaten(pData, Levels.Saltbaker))
            {
                AwardAchievement("DLC_COMPLETED");
            }

            // Base Game Cleared
            if (IsLevelBeaten(pData, Levels.DicePalaceMain) && IsLevelBeaten(pData, Levels.Devil) &&
                IsLevelBeaten(pData, Levels.Veggies) && IsLevelBeaten(pData, Levels.Dragon) &&
                IsLevelBeaten(pData, Levels.Train))
            {
                AwardAchievement("BASE_GAME_COMPLETED");
            }

            // Expert Isle completions
            if (IsLevelExpert(pData, Levels.Veggies) && IsLevelExpert(pData, Levels.Slime) &&
                IsLevelExpert(pData, Levels.Frogs) && IsLevelExpert(pData, Levels.Flower) &&
                IsLevelExpert(pData, Levels.FlyingBlimp))
            {
                AwardAchievement("ISLE_1_EXPERT_CLEARED");
            }

            if (IsLevelExpert(pData, Levels.Baroness) && IsLevelExpert(pData, Levels.Clown) &&
                IsLevelExpert(pData, Levels.FlyingGenie) && IsLevelExpert(pData, Levels.FlyingBird) &&
                IsLevelExpert(pData, Levels.Dragon))
            {
                AwardAchievement("ISLE_2_EXPERT_CLEARED");
            }

            if (IsLevelExpert(pData, Levels.Bee) && IsLevelExpert(pData, Levels.Pirate) &&
                IsLevelExpert(pData, Levels.SallyStagePlay) && IsLevelExpert(pData, Levels.FlyingMermaid) &&
                IsLevelExpert(pData, Levels.Robot) && IsLevelExpert(pData, Levels.Mouse) &&
                IsLevelExpert(pData, Levels.Train))
            {
                AwardAchievement("ISLE_3_EXPERT_CLEARED");
            }

            if (IsLevelExpert(pData, Levels.DicePalaceMain) && IsLevelExpert(pData, Levels.Devil))
            {
                AwardAchievement("BASE_GAME_ALL_EXPERT");
            }

            // Meta achievements
            int unlockedCount = 0;
            if (_data != null && _data.achievements != null)
            {
                for (int i = 0; i < _data.achievements.Count; i++)
                {
                    if (_data.achievements[i].unlocked) unlockedCount++;
                }
            }

            if (unlockedCount >= 50 && IsLevelBeaten(pData, Levels.Devil) && IsLevelBeaten(pData, Levels.Saltbaker))
            {
                AwardAchievement("LEGENDARY_TRIO");
            }

            if (unlockedCount >= 60 && IsLevelExpert(pData, Levels.Devil) && IsLevelExpert(pData, Levels.Saltbaker))
            {
                AwardAchievement("PORCELAIN_PANTHEON");
            }
        }

        private static bool IsLevelBeaten(PlayerData data, Levels lvl)
        {
            try
            {
                var ld = data.GetLevelData(lvl);
                return ld != null && ld.completed;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsLevelExpert(PlayerData data, Levels lvl)
        {
            try
            {
                var ld = data.GetLevelData(lvl);
                return ld != null && ld.completed && ld.difficultyBeaten >= Level.Mode.Hard;
            }
            catch
            {
                return false;
            }
        }

        private static void SyncExpertLevelBeaten(Levels lvl)
        {
            try
            {
                var data = PlayerData.Data;
                if (data == null) return;
                var ld = data.GetLevelData(lvl);
                if (ld != null)
                {
                    ld.completed = true;
                    ld.played = true;
                    if (ld.difficultyBeaten < Level.Mode.Hard) ld.difficultyBeaten = Level.Mode.Hard;
                    PlayerData.SaveCurrentFile();
                }
            }
            catch { }
        }
    }

    [HarmonyPatch]
    internal static class CoopStatsPatches
    {
        [HarmonyPatch(typeof(Level), "Awake")]
        [HarmonyPostfix]
        private static void LevelAwakePostfix(Level __instance)
        {
            CoopStatsManager.OnLevelStart(__instance);
            BossSessionTracker.OnLevelStart(__instance);
            QuickRestartManager.ResetOnLevelStart();
        }

        [HarmonyPatch(typeof(Level), "OnWin")]
        [HarmonyPostfix]
        private static void LevelOnWinPostfix(Level __instance)
        {
            CoopStatsManager.OnLevelWin(__instance);
            BossSessionTracker.OnLevelWin(__instance);
        }

        [HarmonyPatch(typeof(WinScreen), "Awake")]
        [HarmonyPostfix]
        private static void WinScreenAwakePostfix()
        {
            if (BossSessionTracker.HasLastSummary)
            {
                BossVictorySummaryUI.Open(BossSessionTracker.LastWinningSummary, BossSessionTracker.LastCumulativeSummary);
            }
        }

        [HarmonyPatch(typeof(DamageDealer), "DealDamage")]
        [HarmonyPostfix]
        private static void DealDamagePostfix(DamageDealer __instance, float __result)
        {
            if (__result > 0f && __instance != null)
            {
                PlayerId pid = __instance.PlayerId;
                if (pid == PlayerId.None || pid == PlayerId.Any)
                {
                    pid = DamageDealer.lastPlayer;
                }

                if (pid != PlayerId.None && pid != PlayerId.Any)
                {
                    CoopStatsManager.RecordDamage(pid, __result);
                    BossSessionTracker.RecordDamage(pid, __result);
                }
            }
        }

        [HarmonyPatch(typeof(PlayerStatsManager), "OnParry")]
        [HarmonyPostfix]
        private static void PlayerStatsOnParryPostfix(PlayerStatsManager __instance, bool countParryTowardsScore)
        {
            if (countParryTowardsScore && __instance != null)
            {
                CoopStatsManager.RecordParry(__instance);
                if (__instance.basePlayer != null)
                {
                    BossSessionTracker.RecordParry(__instance.basePlayer.id);
                }
            }
        }

        [HarmonyPatch(typeof(PlayerStatsManager), "OnSuper")]
        [HarmonyPostfix]
        private static void PlayerStatsOnSuperPostfix(PlayerStatsManager __instance)
        {
            if (__instance != null)
            {
                CoopStatsManager.RecordSuper(__instance);
                if (__instance.basePlayer != null)
                {
                    BossSessionTracker.RecordSuper(__instance.basePlayer.id);
                }
            }
        }

        [HarmonyPatch(typeof(PlayerStatsManager), "TakeDamage")]
        [HarmonyPostfix]
        private static void PlayerStatsTakeDamagePostfix(PlayerStatsManager __instance)
        {
            if (__instance != null && __instance.basePlayer != null)
            {
                CoopStatsManager.RecordDamageTaken(__instance.basePlayer.id);
            }
        }

        [HarmonyPatch(typeof(PlayerStatsManager), "OnEx")]
        [HarmonyPostfix]
        private static void PlayerStatsOnExPostfix(PlayerStatsManager __instance)
        {
            if (__instance != null && __instance.basePlayer != null)
            {
                CoopStatsManager.RecordEx(__instance.basePlayer.id);
            }
        }

        [HarmonyPatch(typeof(Level), "OnPlayerDeath")]
        [HarmonyPostfix]
        private static void LevelOnPlayerDeathPostfix(PlayerStatsManager.DeathEvent e)
        {
            if (e != null)
            {
                CoopStatsManager.RecordDeath(e.playerId);
                BossSessionTracker.RecordDeath(e.playerId);
            }
        }

        [HarmonyPatch(typeof(PlayerDeathParrySwitch), "OnParryPrePause")]
        [HarmonyPostfix]
        private static void DeathParryPrePausePostfix(PlayerDeathParrySwitch __instance, AbstractPlayerController player)
        {
            CoopStatsManager.RecordRevive(__instance, player);
            if (player != null)
            {
                BossSessionTracker.RecordRevive(player.id);
            }
        }
    }
}
