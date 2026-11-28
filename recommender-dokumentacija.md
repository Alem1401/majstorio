# Majstorio: sistem preporuke

## 1. Svrha

Sistem preporuke korisniku mobilne aplikacije rangira majstore prema njegovim interesima i uz svakog prikazuje razlog preporuke. Radi unutar API-ja, bez vanjskog AI servisa i bez unaprijed treniranog modela.

## 2. Algoritam

Koriste se dva poznata pristupa (hibridni sistem s prebacivanjem):

| Situacija | Algoritam |
|---|---|
| Korisnik ima historiju | **Content-based filtering** s **kosinusnom sličnošću** |
| Korisnik nema historiju (cold start) | **Popularity-based** rangiranje |

## 3. Ulazni podaci

| Podatak | Tabela | Kada se upisuje | Težina |
|---|---|---|---|
| Pretraga po kategoriji i/ili gradu, zadnjih 30 dana | `SearchLog` | prva stranica pretrage s barem jednim filterom | 1 |
| Zahtjev korisnika koji nije otkazan | `ServiceRequest` | kreiranje zahtjeva | 1 |
| Završen zahtjev korisnika | `ServiceRequest` | kada se posao završi | 2 |
| Kategorije aktivnih usluga majstora | `TradespersonService` | profil majstora | |
| Grad majstora | `TradespersonProfile` | profil majstora | |
| Ocjene majstora | `Review` | korisnik ocijeni završen posao | |
| Završeni poslovi majstora | `Appointment` | majstor završi posao | |

## 4. Content-based filtering

### 4.1 Vektor korisnika

Iz historije se grade dva vektora: po kategorijama i po gradovima. Svaka komponenta je zbir težina zapisa za tu kategoriju, odnosno grad.

### 4.2 Vektor majstora

Binarni vektor: 1 za svaku kategoriju u kojoj majstor ima aktivnu uslugu, odnosno 1 za njegov grad.

### 4.3 Kosinusna sličnost

```
cos(u, m) = (u · m) / (|u| × |m|)
```

Vrijednost je između 0 (nema poklapanja) i 1 (potpuno poklapanje). Majstor koji pokriva više kategorija ima duži vektor, pa specijalista za traženu kategoriju dobija veću sličnost od majstora koji radi sve.

### 4.4 Ukupni skor

```
skor = 0,5 × cos(kategorije) + 0,3 × cos(grad) + 0,2 × (prosječna ocjena / 5)
```

Težine odgovaraju prijavi rada: kategorija 50%, grad 30%, normalizovana prosječna ocjena 20%. Majstor bez ocjena za taj dio dobija 0.

### 4.5 Primjer

Korisnik je 4 puta tražio vodoinstalatera i 2 puta električara, svih 6 puta u Mostaru.

- vektor kategorija: vodoinstalater 4, električar 2; dužina √(4² + 2²) = 4,47
- vektor gradova: Mostar 6; dužina 6

| Majstor (Mostar, ocjena 4,5) | Kategorija | Grad | Ocjena | Skor |
|---|---|---|---|---|
| Vodoinstalater | 0,5 × 0,894 = 0,447 | 0,3 × 1 = 0,300 | 0,2 × 0,9 = 0,180 | **0,927** |
| Električar | 0,5 × 0,447 = 0,224 | 0,300 | 0,180 | **0,704** |

## 5. Popularity-based rangiranje (bez historije)

```
skor = ln(1 + završeni poslovi) / ln(1 + najveći broj među kandidatima) × (prosječna ocjena / 5)
```

Logaritam sprječava da jedan veoma zauzet majstor potisne sve ostale. Kada je odabrana kategorija, broje se samo poslovi završeni u toj kategoriji. Kod jednakog skora odlučuju veća ocjena pa manji ID.

## 6. Kandidati

Preporučuju se samo odobreni i aktivni profili. Filteri pretrage (ime, grad, kategorija, najmanja ocjena, dostupnost) prvo suze listu, a algoritam zatim rangira preostale. Majstor vidi i vlastiti profil u rezultatima pretrage, ali ga početna stranica ne preporučuje njemu samom.

## 7. Objašnjenje preporuke

API vraća polje `recommendationReasons`, poredano po doprinosu skoru. Razlog se navodi samo ako je signal zaista doprinio.

| Vrijednost | Tekst u aplikaciji |
|---|---|
| `Category` | kategorija koju tražite |
| `City` | grad koji tražite |
| `Rating` | ocjena 4.2, odnosno visoka ocjena 4.8 (od 4,5) |
| `Popularity` | popularan po završenim poslovima |
| (nema razloga) | dostupan majstor |

## 8. Pravila za historiju pretraga

- Lista bez filtera (početna stranica) se ne upisuje, jer ne govori ništa o interesima.
- Pretraga ne utiče na vlastito rangiranje: isti filteri kategorije i grada iz zadnjih 10 minuta se preskaču. Tako druga stranica ima isti redoslijed kao prva, a novi korisnik pri prvoj pretrazi dobija rangiranje po popularnosti.
- Pretrage administratora se ne upisuju, a administrator dobija običnu listu sortiranu po ocjeni.

## 9. Implementacija

| Fajl | Uloga |
|---|---|
| `Majstorio.Services/Recommendations/ContentBasedRecommender.cs` | kosinusna sličnost, skor, popularnost, razlozi |
| `Majstorio.Services/Services/TradespersonProfileService.cs` | historija korisnika, kandidati, paginacija, upis u `SearchLog` |
| `GET /TradespersonProfiles` | endpoint (isti kao pretraga) |
| `UI/majstorio_mobile/lib/widgets/tradesperson_card.dart` | prikaz razloga |
| `UI/majstorio_mobile/lib/screens/home_screen.dart` | sekcija „Preporučeno za vas“ |
| `Majstorio.Services.Tests/RecommenderTests.cs` | testovi |

## 10. Ograničenja

- Skor se računa u memoriji pri svakom zahtjevu, što odgovara obimu seminarskog rada. Za veliki broj majstora trebalo bi keširanje.
- Težine su zadane prijavom i nisu kalibrirane na stvarnim podacima.
