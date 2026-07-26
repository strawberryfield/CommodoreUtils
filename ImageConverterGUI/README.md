Casasoft.ImageConverterGUI
===========================

Interfaccia grafica Avalonia (Windows/Linux/macOS) per il convertitore di immagini C64,
basata sulle librerie esistenti `Commodore`, `CommodoreImages`, `Helpers` e sui controlli
`Casasoft.Avalonia.Controls` (repository `Contemporary_CDV`).

Cosa fa
-------
- Carica un'immagine in **qualsiasi formato supportato da ImageMagick** (PNG, JPG, BMP,
  GIF, TIFF, WEBP, ...), di qualunque dimensione: il ridimensionamento a 160x200 (multicolor)
  o 320x200 (hires) è gestito internamente dal convertitore (`C64BitmapConverterBase.DownscaleImage`),
  quindi non è necessario pre-scalare l'immagine come richiesto invece dal tool a riga di comando.
- Permette di scegliere:
  - **Multicolor** (160x200, 4 colori per cella) o **Hires** (320x200, 2 colori per cella)
  - Dithering Floyd-Steinberg on/off
  - Bias di luminosità (0.0-1.0) e a quale fase applicarlo (quantizzazione / selezione / entrambi)
  - Indirizzi di memoria C64 (bitmap, schermo, sfondo — quest'ultimo solo in multicolor)
- Mostra l'**anteprima** (320x200, come apparirebbe su un vero C64) accanto ai parametri.
- Permette di **salvare l'anteprima** (PNG/JPG/BMP) e di **salvare i file Commodore 64**
  (`.bm.prg`, `.sc.prg`, e in multicolor anche `.co.prg`/`.bg.prg`), con la stessa
  convenzione di nomi usata da `ImageConverter`.

Struttura dei file
-------------------
```
ImageConverterGUI/
├── ImageConverterGUI.csproj
├── app.manifest
├── Program.cs                  # bootstrap Avalonia
├── App.axaml / App.axaml.cs    # tema, apertura MainWindow
└── Views/
    ├── MainWindow.axaml        # layout: pannello parametri a sinistra, anteprima a destra
    └── MainWindow.axaml.cs     # logica: caricamento, conversione, salvataggi
```

Prerequisiti e passi di integrazione
-------------------------------------
1. **Riferimento a Casasoft.Avalonia.Controls**: il progetto è nella solution separata
   `Contemporary_CDV`. Apri `ImageConverterGUI.csproj` e correggi il percorso relativo
   nel `<ProjectReference>` in base a dove hai clonato quella repository, es.:
   ```xml
   <ProjectReference Include="..\..\Contemporary_CDV\Casasoft.Avalonia.Controls\Casasoft.Avalonia.Controls.csproj" />
   ```
   In alternativa, se preferisci non dipendere da un secondo repository in fase di build,
   pacchettizza `Casasoft.Avalonia.Controls` come NuGet locale e referenzialo con
   `<PackageReference>`.

2. **Nota sulle quantum depth di Magick.NET**: `CommodoreImages` (e quindi questo progetto)
   usa `Magick.NET-Q8-AnyCPU`, mentre `Casasoft.Avalonia.Controls` dichiara una dipendenza da
   `Magick.NET-Q16-AnyCPU` (usata solo da `ImageViewer.SetImage(MagickImage)`). Questo progetto
   **non chiama mai** quel metodo: converte invece l'immagine in un `Avalonia.Media.Imaging.Bitmap`
   (via round-trip PNG in memoria, vedi `ToAvaloniaBitmap` in `MainWindow.axaml.cs`) e lo assegna
   a `ImageViewer.Source`, che è tipizzato `Bitmap?` e non ha nulla a che fare con ImageMagick.
   Questo evita conflitti di tipo tra i due package, ma se in futuro aggiungi altro codice che
   passa direttamente `MagickImage` tra i due progetti, dovrai allineare entrambi sulla stessa
   quantum depth (consigliato: Q8, per coerenza con `CommodoreImages`/`ImageConverter`).

3. **Aggiungere il progetto alla solution**:
   ```bash
   dotnet sln CommodoreUtils.sln add ImageConverterGUI/ImageConverterGUI.csproj
   ```

4. **Build/Run**:
   ```bash
   dotnet build -c Release ImageConverterGUI/ImageConverterGUI.csproj
   dotnet run --project ImageConverterGUI/ImageConverterGUI.csproj
   ```

Note implementative
--------------------
- La selezione hires/multicolor usa `IC64BitmapConverter<IC64BitmapData>` esattamente come
  `ImageConverter/Program.cs`, sfruttando la covarianza dell'interfaccia (`out TData`).
- Gli indirizzi (`$E000`, `$C000`, `$D021`) sono validati con `CommandLineHelpers.GetIntParameter`,
  la stessa utility già usata dal tool a riga di comando, quindi accettano sia `$XXXX` che `0xXXXX`
  che notazione decimale.
- La Color RAM è sempre salvata a `$D800` (fisso), come nel tool CLI.
- Il file `.bg.prg` (colore di sfondo, un solo byte) viene generato solo in modalità multicolor,
  perché in hires non esiste un registro di sfondo condiviso (vedi `C64HiresData`).
