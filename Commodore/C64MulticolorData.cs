namespace Casasoft.Commodore;

/// <summary>
/// Represents the multicolor graphics data for a Commodore 64 display.
/// This class manages the three separate memory areas required for C64 multicolor bitmap graphics:
/// bitmap data (character definitions), color RAM (per-cell color information), and screen RAM (character codes).
/// </summary>
public class C64MulticolorData
{
    /// <summary>
    /// Gets or sets the bitmap data containing character patterns.
    /// Size: 8000 bytes (40 columns × 25 rows × 8 bytes per character cell).
    /// Each byte represents 8 pixels in a character definition.
    /// </summary>
    public byte[] BitmapData { get; set; } = new byte[8000];    // 40x25x8 = 8KB

    /// <summary>
    /// Gets or sets the color RAM containing color information for each screen cell.
    /// Size: 1000 bytes (40 columns × 25 rows).
    /// Each byte specifies the color pair for the corresponding character cell.
    /// </summary>
    public byte[] ColorRam { get; set; } = new byte[1000];       // 40x25 = 1KB

    /// <summary>
    /// Gets or sets the screen RAM containing character codes for each screen cell.
    /// Size: 1000 bytes (40 columns × 25 rows).
    /// Each byte represents which character pattern to display at that screen position.
    /// </summary>
    public byte[] ScreenRam { get; set; } = new byte[1000];       // 40x25 = 1KB

    /// <summary>
    /// Saves the bitmap data, color RAM, and screen RAM to separate binary files.
    /// </summary>
    /// <param name="basePath">The base file path (without extension) for saving the binary files.
    /// Three files will be created: {basePath}_bitmap.bin, {basePath}_colorram.bin, and {basePath}_screenram.bin</param>
    public void SaveToFile(string basePath)
    {
        File.WriteAllBytes($"{basePath}_bitmap.bin", this.BitmapData);
        File.WriteAllBytes($"{basePath}_colorram.bin", this.ColorRam);
        File.WriteAllBytes($"{basePath}_screenram.bin", this.ScreenRam);
        Console.WriteLine($"Saved files with base path: {basePath}");
    }
}

