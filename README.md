# Sistem za kontrolu robotske ruke

Ovo je WCF klijent–server aplikacija za upravljanje robotskom rukom. Više klijenata može istovremeno slati komande, uz jasno definisane dozvole, prioritete i determinističko redosledno izvršavanje. Server čuva stanje, serijalizuje obradu komandi i obaveštava sve klijente o promenama.

## Ključne karakteristike

- Više tipova klijenata sa različitim dozvolama (kretanje i/ili rotacija).
- Prioritetno zakazivanje komandi: najpre po prioritetu klijenta, zatim po vremenu prijema, pa po internom sekvencijalnom brojaču.
- Sigurna komunikacija: komande klijenta stižu šifrovane (RSA).
- Obrada bez konflikta: izmene stanja robotske ruke se vrše pod zaključanom robotskom rukom, kako ne bi došlo do data-race.
- Ažuriranja u realnom vremenu: server šalje obaveštenja svim registrovanim klijentima o novom stanju.

## Tipovi klijenata i dozvole

- Client1
  - Dozvole: LEFT, RIGHT, UP, DOWN, ROTATE
  - Prioritet: najviši
- Client2
  - Dozvole: LEFT, RIGHT, UP, DOWN
  - Prioritet: isti kao Client3
- Client3
  - Dozvole: ROTATE
  - Prioritet: isti kao Client2

Napomena: Klijenti istog prioriteta se raspoređuju po vremenu prijema komande, a u slučaju identičnog vremena koristi se interni sekvencijalni broj zahtevа radi stabilnog poređenja.

## Komunikacija i protokol

- WCF dvosmerna komunikacija (duplex): klijent poziva servis (komande, registracija), a servis šalje povratne notifikacije klijentima o stanju.
- Komande se šalju kao šifrovani tekst (RSA). Server vrši dešifrovanje pre obrade.
- Operacije servisa:
  - `RegisterClient(clientName)`: registruje klijenta i otvara callback kanal.
  - `UnregisterClient(clientName)`: odjavljuje klijenta i uklanja njegov callback.
  - `ExecuteCommand(clientName, encryptedCommand)`: prijem, validacija i zakazivanje komande.
  - `GetCurrentState()`: sinhroni uvid u trenutno stanje ruke.

## Kontrola sesije (`SessionManager`)

- Evidentira aktivne klijente po tipu klijenta (“Client1”, “Client2”, “Client3”) u `ConcurrentDictionary`.
- Dozvoljava registraciju samo ako tip klijenta nije već aktivan (jedan aktivan klijent po tipu).
- Gašenjem klijenta uklanja se iz rečnika aktivnih sesija.

## Red čekanja komandi i zakazivanje

- Svaka pristigla komanda se čuva kao instanca `CommandRequest` klase, gde se beleži: tip klijenta, tip komande, vreme prijema, sekvencijalni broj i rezultat izvršavanja komande.
- Red čekanja je implementiran pomoću `SortedSet` klase, gde se sortiranje vrši po:
  1) prioritetu klijenta, 
  2) vremenu pristizanja poruke,
  3) internom brojaču (garantuje stabilan i jedinstven poredak).
- Dodavanje u red signalizira pozadinskom procesoru da je dostupna nova komanda.

## Pozadinska obrada i bezbedan pristup stanju

- Pozadinska nit blokira dok ne stigne sledeća komanda, zatim je preuzima po gore navedenom poretku.
- Izvršenje komande se obavlja pod zaključavanjem stanja robotske ruke, tako da u jednom trenutku samo jedna komanda menja stanje.
- Validacija:
  - Provera dozvola klijenta za konkretnu komandu.
  - Odbijanje ako je komanda nedozvoljena za taj tip klijenta ili ako je van dozvoljenih granica.
- Logovanje:
  - Svaka obrada se upisuje u bazu (komanda, rezultat, nova pozicija i ugao).
- Rezultat:
  - Klijent koji je poslao komandu dobija rezultat preko TaskCompletionSource mehanizma (poruka + trenutno stanje).

## Povratne informacije klijentima

- Nakon svake obrade, servis emitije aktuelno stanje svim registrovanim klijentima preko callback kanala.
- Ako se isporuka ne uspe, callback se tretira kao nevažeći i uklanja.

## Stanje robotske ruke i ograničenja

- Mreža: 5x5.
- Rotacija: 0°, 90°, 180°, 270° (korak od 90°).
- Kretanje:
  - LEFT/RIGHT: pomeranje za jedan korak horizontalno.
  - UP/DOWN: pomeranje za dva koraka vertikalno.
- Komande van granica se odbijaju.

## Dostupne komande

- `LEFT` — pomeri ulevo (1 korak)
- `RIGHT` — pomeri udesno (1 korak)
- `UP` — pomeri nagore (2 koraka)
- `DOWN` — pomeri nadole (2 koraka)
- `ROTATE` — rotacija za 90° u smeru kazaljke na satu
- `EXIT` — zatvaranje klijentske aplikacije

## Bezbednost

- RSA enkripcija za komande u dolaznom smeru.
- Dozvole po tipu klijenta se striktno primenjuju pre izvršenja.
- Obrada se odvija pod zaključavanjem stanja kako bi se sprečila konkurentna korupcija podataka.

## Obrada grešaka

- Nevalidne ili nedozvoljene komande: vraća se poruka o grešci, bez promene stanja.
- Izlazak van granica mreže: komanda se odbija i vraća se odgovarajuća poruka.
- Problemi u komunikaciji sa klijentom: neuspešni callback-ovi se uklanjaju, servis nastavlja sa radom.
- Greške baze: hvataju se i loguju, bez prekidanja obrade komandi.

## Autori

- Patrik Barši SV7/2022
- David Makan SV33/2022
- Dušan Komadinović SV65/2022