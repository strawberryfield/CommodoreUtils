set converter="C:\projects\CommodoreUtils\ImageConverter\bin\Debug\net10.0\ImageConverter.exe"
set image="C:\projects\CommodoreUtils\ImageConverter\c64\test.jpg"

%converter% -p $E000 -c $C000 %image%
%converter% -p $E000 -c $C000 --brightness=0,7 -o "C:\projects\CommodoreUtils\ImageConverter\c64\test07" %image%
%converter% -p $E000 -c $C000 -d -o "C:\projects\CommodoreUtils\ImageConverter\c64\testq" %image%
%converter% -p $E000 -c $C000 -d --brightness=0,7 -o "C:\projects\CommodoreUtils\ImageConverter\c64\testq07" %image%

%converter% -p $E000 -c $C000 -2 --brightness=0,7 -o "C:\projects\CommodoreUtils\ImageConverter\c64\testhr" %image%
%converter% -p $E000 -c $C000 -2 -d --brightness=0,7 -o "C:\projects\CommodoreUtils\ImageConverter\c64\testhrq" %image%