{renumber}
{step:10}
	REM ---------------------------------------------
	REM  CASASOFT HIRES BITMAP VIEWER
	REM  loads the .BM.PRG .SC.PRG files generated
	REM  by ImageConverter (-2/--hires) and shows
	REM  the image on the VIC-II
	REM  press ENTER to return to the original
	REM  configuration
	REM ---------------------------------------------
{nice:100}	
	IF LD=0 THEN GOSUB {:init}: LD=1: LOAD F$+".BM",DV,1
	IF LD=1 THEN PRINT ".";: LD=LD+1: LOAD F$+".SC",DV,1
	REM --- SAVE ORIGINAL VIC/CIA CONFIGURATION ---
	OB=PEEK(53265):OM=PEEK(53270):OP=PEEK(53272)
	OC=PEEK(56576):OF=PEEK(53280):OG=PEEK(53281)
	REM --- VIC BANK 3 ($C000-$FFFF) ---
	POKE 56576,PEEK(56576) AND 252
	REM --- SCREEN AT $C000 (OFFSET 0) BITMAP AT $E000 (OFFSET $2000) ---
	POKE 53272,8
	REM --- ENABLE BITMAP MODE (BMM BIT OF $D011) ---
	POKE 53265,PEEK(53265) OR 32
	REM --- DISABLE MULTICOLOR MODE (MCM BIT OF $D016) ---
	REM --- (in hires mode all colors are in SCREEN RAM) ---
	POKE 53270,PEEK(53270) AND 239
	REM --- BORDER (CHANGE AS DESIRED) ---
	POKE 53280,0
	REM --- WAIT FOR ENTER ---
{:wait}	
	GET K$:IF K$<>CHR$(13) THEN {:wait}
	REM --- RESTORE ORIGINAL CONFIGURATION ---
	POKE 53265,OB:POKE 53270,OM:POKE 53272,OP
	POKE 56576,OC:POKE 53280,OF:POKE 53281,OG
	PRINT CHR$(147)
	END
{nice:1000}
	REM ---------------------------------------------
	REM --- INIT
{:init}
	DV=8:REM disk device number
	PRINT CHR$(147)
	PRINT "CASASOFT HIRES VIEWER"
	PRINT
	INPUT "IMAGE NAME (NO EXTENSION)";F$
	PRINT
	PRINT "LOADING.";
	RETURN
