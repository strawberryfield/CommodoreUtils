set converter="C:\projects\CommodoreUtils\ImageConverter\bin\Debug\net10.0\ImageConverter.exe"
set image="C:\projects\CommodoreUtils\ImageConverter\c64\test.jpg"

%converter% -p $E000 -c $C000 %image%