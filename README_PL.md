# Cuphead 3-Player Co-op Mod - Instrukcja Obsługi

Stabilny, dopracowany i w pełni zbalansowany mod umożliwiający rozgrywkę kooperacyjną w 3 osoby na jednym ekranie w Cuphead oraz dodatku The Delicious Last Course.

Projekt tworzony przez blisko dwa lata inżynierii wstecznej silnika gry oraz trasowania kontrolerów Rewired. Przetestowany w boju: ukończona cała kampania od Inkwell Isle I po Diabła oraz dodatek DLC (Szef Solniczak i sekretni bossowie) bez ani jednego błędu krytycznego.

[English documentation (README.md)](README.md)

---

## Główne Możliwości

Cuphead posiada wczesny, niedokończony kod dla dodatkowych slotów graczy, lecz brakuje w nim balansu, trasowania kontrolerów, obsługi ekwipunku Gracza 3 oraz poprawek błędów silnika w multiplayerze. Ten mod uzupełnia te elementy:

* **Rozgrywka w 3 osoby**: Działa lokalnie na jednym komputerze, przez Steam Remote Play Together oraz przez Parsec.
* **Trasowanie kontrolerów**: Gra automatycznie przypisuje pady bez konfliktów w Rewired. Obsługuje 3 pady lub kombinację: 1 klawiatura + 2 pady.
* **Dynamiczne skalowanie obrażeń**: Obrażenia graczy skalują się zależnie od liczby żywych towarzyszy (0.35x dla 3 graczy, 0.50x dla 2 graczy, 1.00x dla 1 gracza). Gdy sojusznik polegnie, pozostali gracze natychmiast zadają większe obrażenia.
* **Tryb Hardcore**: Jeśli poległy gracz ucieknie duszkiem poza ekran bez reanimacji przez parowanie, walka restartuje się automatycznie.
* **Karta osi czasu bossa**: W trybie Hardcore przed restartem na 3 sekundy pojawia się karta osi postępu bossa, pokazująca, jak blisko zwycięstwa była drużyna.
* **Menu w grze [F5]**: Menu w stylu kreskówek lat 30., obsługiwane klawiaturą, padem lub myszką. Umożliwia wybór broni Gracza 3, amuletów (w tym natychmiastowe włączenie Ms. Chalice), modelu bazowego oraz punktów zdrowia (1 do 3 HP na gracza).
* **Statystyki, laury MVP i 187 osiągnięć**: Zlicza obrażenia, parowania, reanimacje, superciosy i zwycięstwa. Przyznaje odznaczenia po walce (Złoty Taran za obrażenia, Anioł Stróż za ratunki, Żelazna Skorupa za przetrwanie).
* **Szybki restart [R] / [L3]**: Przytrzymanie lewego analoga (L3) lub klawisza [R] przez 1 sekundę natychmiast restartuje walkę bez wchodzenia do menu pauzy.
* **Poprawki błędów podstawowej gry**: Usuwa błąd KeyNotFoundException przy losowaniu celu ataku bossa, błędy zamku szachowego (Królewski Skok), respawn przeszkód u Pana Papierska oraz zacięcia przejścia łodzią DLC.

---

## Klawisze Skrótu

| Klawisz | Akcja | Opis |
| :--- | :--- | :--- |
| **[F1]** | Klawiatura dla P1 | Odłącza pady od Gracza 1, rezerwując kontrolery dla P2 i P3. |
| **[F2]** | Wymuś wejście P2 | Wymusza wejście Mugmana na pierwszym wolnym padzie. |
| **[F3]** | Wymuś wejście P3 | Wymusza wejście Gracza 3 na kolejnym padzie. |
| **[F4]** | Auto-naprawa padów | Czyste przypisanie padów: P1=Klawiatura/Pad 1, P2=Pad 2, P3=Pad 3. |
| **[F5]** | Menu konfiguracji | Otwiera menu wyboru ekwipunku, zasad i statystyk w grze. |
| **[F6]** | Przełącznik Hardcore | Włącza lub wyłącza automatyczny restart po ucieczce duszka. |
| **[R] / [L3]** | Szybki restart | Przytrzymaj przez 1 sekundę, aby zrestartować walkę z bossem. |
| **[V]** | Karta wyników | Otwiera podgląd podsumowania ostatniej walki z bossem na mapie świata. |

---

## Konfiguracje Kontrolerów

### Wariant 1: 3 Pady (Lokalnie, Parsec lub Remote Play)
1. Podłącz 3 kontrolery (Xbox, PlayStation lub uniwersalne kontrolery USB/Bluetooth).
2. Uruchom Cuphead.
3. Jeśli sterowanie się pomiesza, naciśnij **[F4]**:
   * Pad 1 -> Gracz 1 (Cuphead / Czerwony)
   * Pad 2 -> Gracz 2 (Mugman / Niebieski)
   * Pad 3 -> Gracz 3 (Ms. Chalice / Zielony)

### Wariant 2: 1 Klawiatura + 2 Pady
1. Gracz 1 gra na klawiaturze.
2. Naciśnij **[F1]** na klawiaturze. Pojawi się powiadomienie: `Cuphead (P1) na KLAWIATURZE. Pady zwolnione dla P2 i P3!`.
3. Gracze 2 i 3 wciskają dowolny przycisk na padach (lub wciskasz [F2] i [F3]).
4. Gracz 2 steruje Mugmanem, a Gracz 3 trzecią postacią.

---

## Menu w Grze [F5]

Naciśnij **[F5]** w dowolnym momencie (w trakcie poziomu lub na mapie świata):

### Zakładka 1: Ekwipunek Gracza 3 i Zdrowie
* **Broń A i Broń B**: Groszkowiec, Rozrzutnik, Namierzacz, Miotacz, Ładowacz, Zawracacz, Trzaskacz, Zbiegacz, Skręcacz.
* **Supercios**: Super I (Wiązka), Super II (Niewrażliwość), Super III (Wielki Duszek).
* **Amulet**: Bomba Dymna, Serce, Kawa, Cukier P., Podwójne Serce, Astralne Ciastko (Ms. Chalice), Osełka, Pierścień Serca, Zniszczony / Przeklęty / Boski Relikt.
* **Przełącznik Ms. Chalice**: Błyskawiczna zmiana postaci na Bajeczną Czarkę.
* **Model bazowy P3**: Wybór animacji Cupheada lub Mugmana, gdy gracz nie gra jako Czarka.
* **Bazowe punkty zdrowia**: Ustawienie bazowego HP (1, 2 lub 3 HP) niezależnie dla każdego gracza lub jednym przyciskiem dla całej drużyny.
* **Kopiuj z P1**: Błyskawiczne skopiowanie wyposażenia Gracza 1 do Gracza 3.

### Zakładka 2: Zasady Rozgrywki i Modyfikatory
* **Mnożniki obrażeń**:
  * *Łatwy*: 2 graczy = 0.75x, 3 graczy = 0.50x.
  * *Normalny (Domyślny)*: 2 graczy = 0.50x, 3 graczy = 0.35x.
  * *Trudny*: 2 graczy = 0.35x, 3 graczy = 0.25x.
* **Zatrzymanie czasu przy parowaniu**: Włączenie lub wyłączenie mikro-pauzy przy parowaniu różowych obiektów.
* **Tryb Hardcore**: Włączenie lub wyłączenie restartu walki po ucieczce duszka.
* **Czas karty osi postępu**: Regulacja czasu wyświetlania osi bossa przed restartem (w sekundach).
* **Czas przytrzymania szybkiego restartu**: Regulacja czasu przytrzymania L3 / [R] (od 0.4s do 3.0s).

### Zakładka 3: Statystyki i 187 Osiągnięć
* Bieżące zliczanie obrażeń, parowań, reanimacji, zgonów i pokonanych bossów.
* 187 autorskich osiągnięć zgodnych z polskim tłumaczeniem gry.
* Otwarcie karty podsumowania ostatniej walki oraz reset zapisanych statystyk.

---

## Instalacja

1. Zainstaluj **BepInEx 5.4.x (x64)** w głównym folderze gry (obok pliku `Cuphead.exe`).
2. Pobierz najnowszą wersję z zakładki [Releases](https://github.com/Meloon33/Cuphead-3-Player-Coop/releases).
3. Wypakuj plik `Cuphead3PCoop.dll` do katalogu `Cuphead/BepInEx/plugins/`.
4. Uruchom grę `Cuphead.exe`.

---

## Kompilacja ze Źródeł

Projekt kompiluje się kompilatorem C# Roslyn (`csc.exe`) bez menedżerów pakietów:

1. Sklonuj repozytorium:
   ```bash
   git clone https://github.com/Meloon33/Cuphead-3-Player-Coop.git
   ```
2. Uruchom skrypt `src/build.bat`. Jeśli Cuphead znajduje się w niestandardowej ścieżce, ustaw zmienną:
   ```cmd
   set CUPHEAD_GAME_DIR=C:\Sciezka\Do\Cuphead
   build.bat
   ```
3. Skompilowana biblioteka zostanie zapisana w `src/Cuphead3PCoop.dll` oraz `release/`.

---

## Licencja

Projekt objęty licencją [MIT](LICENSE).  
Cuphead jest zarejestrowanym znakiem towarowym Studio MDHR Entertainment Inc. Ten mod jest niezależnym projektem fanowskim i nie jest powiązany ze Studio MDHR.
