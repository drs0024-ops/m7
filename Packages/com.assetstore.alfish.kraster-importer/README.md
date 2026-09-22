KRaster Importer
===

This plugin makes it easier for you (or your artists) to use free software, such as [Krita], for drawing/editing multi-layer images. It also supports [OpenRaster], a standard interchange format used by multiple image editors, most of them free.

Now, you no longer need to manually export .png files from the image editor every time you make changes to the source image. By letting this process be handled in Unity, you can simplify your workflow and save your precious time!  
It can do both manual and automatic conversions conveniently from Unity.

This is a hi-quality product:
- **Intuitive**. The familiar interface is very easy to learn and convenient to use.
- **Efficient workflow**. Import multiple files at once. Use patterns and presets to be efficient even if you want to organize your files in a different way.
- **Smart warnings** to detect edge cases and guide you on how to proceed.
- **Fully documented** online (wiki) and offline (tooltips).
- **Thoroughly tested**. Check the documentation to solve issues or report bugs.
- **Editor-only**. It doesn't need to add any runtime code to your builds.
- **Offline code**. No user data is collected, so your privacy is respected.

Check the [Documentation] for details.


Setup
---

Firstly, make sure to have a Krita installation for the conversions, then **go to Preferences and enable Krita Runner** after confirming the path to its executable. Although simple PNG-based importations still work without Krita, this is **required to import animations and for the manual converter** (i.e., handling all image formats), and highly recommended to get the best compatibility with advanced Krita features.
- On Windows and macOS, you can just [download Krita]. You can use the free installer.
- On Linux, Snap and AppImage installations are not supported. Use preferably the one [from your distro] (e.g. `sudo apt install krita` on Ubuntu) or, if not possible, the one [from Flathub].


Manual Conversions
---

This plugin can make Krita convert between most image formats directly from Unity. Simply right-click assets to quickly generate images next to them, or use the advanced tool to make batch conversions with full flexibility. It's very useful to quickly make Krita files for editing, or to convert formats unsupported by Unity that Krita can open (like WebP and JPEG XL) to a format like PNG or EXR for importation.


Automatic Importations
---

Any Krita (.kra/.krz) and OpenRaster (.ora) files in your project will be recognized as assets and show their preview. Simply multi-select assets and pick an importation method in the inspector, then click *Apply*. Whenever you edit any of those files externally, it will be reimported automatically!

The recommended method is *Generate File*, which allows to use all features of Unity's built-in texture importer. You can set a specific path/name pattern to where the images will be regenerated whenever a source image changes.  
For animated files, you can make it use Krita Runner to generate images for each frame and even group those under the same folder.

If you prefer to import textures and sprites directly as sub-assets, you also have a few options using the *Import Image* method, which doesn't require Krita.  
You can even import grid-based spritesheets directly. This is great for when you don't need to edit sprites one-by-one with Unity's Sprite Editor, e.g. in tilesets.

If you want to automate even more and your project's files are well-organized, you can use presets (some are included). By understanding how path patterns work and using Unity's Preset Manager you can even achieve zero-click path-based importation with full flexibility. Just add an image to the project and all importations will be done with the correct settings for where you put the files.


Technical details
---

Note that the way assets are imported or generated could change in a future version. Check the changelog for breaking changes when updating.

This plugin works in two modes. The internal mode doesn't need Krita and is enough for simpler projects. With Krita installed, you can enable the external mode too, which will be automatically used when appropriate.

**Internal PNG** mode (default):
- Animation is not supported in this mode.
- HDR is not supported in this mode. Only PNG files can be generated.
- Only the flattened image will be read, so this merged image has to be available inside the file (KRZ[*] files don't have it). If it's missing, the thumbnail is used as a fallback, but it's typically sized and cropped differently from the original image. So you should not import KRZ files if you didn't enable Krita.
- Appropriate only for images on the sRGB color space (i.e., it needs both sRGB TRC and sRGB gamut). For linear images and any other color spaces, the external mode is highly recommended.

**External program** mode (users can enable it in preferences):
- Enabling a Krita installation in this mode is required to export animation frames and for the manual image converter tool. It's highly recommended for KRZ and so KRA files can be handled properly on advanced cases (like HDR and non-sRGB color spaces).
- On Linux, you should prefer your distro's installation of Krita over Flatpak, specially if also using Unity Hub in a Flatpak, because bypassing the inter-Flatpak isolation is less efficient.
- In auto-importations, it's only used as an alternative for the *Generate File* method. It's currently not used in direct sub-asset importations, thumbnails or previews.
- Batch importation with Krita Runner is a bit slower, so the plugin will only use this mode when it detects that it's necessary or would be more accurate than the internal PNG.
- Some files, specially those with non-paint layers (e.g. vector layers, fill layers, etc.) might not always export correctly. In this case, you can try enabling the option to ask for exportation settings and see if that fixes it. If it's still incorrect, try exporting from Krita itself to see if it's a Krita bug. You can force internal mode if what you need is just a PNG file.

This plugin is officially compatible[**] with at least:
- KRA, KRZ[*]: Krita v5.2.9+; only installations with `kritarunner`, meaning Snap and AppImage are currently not supported
- ORA: OpenRaster v0.0.2~v0.0.6, Baseline Intent

[*]. If you want to avoid .krz (Krita Archival Image) files for static images (e.g. if you don't have Krita installed), ask your artists to re-save as .kra instead (enabling the "compress .kra files more" setting in Krita if desired).

[**]. Note that, although compatibility is expected to be good, no testing is done in any specific image editors, other than the specified version of Krita. Other versions or apps might not necessarily be supported. Same applies to any extra features/extensions that might be added by these apps to the formats. Note also that paid installations of Krita (i.e. Microsoft/Mac/Steam/Epic stores) were not tested.


[Krita]: https://krita.org/
[OpenRaster]: https://en.wikipedia.org/wiki/OpenRaster
[Documentation]: https://bitbucket.org/alfish/kraster-importer-for-unity/wiki/
[download Krita]: https://krita.org/en/download/
[from your distro]: https://docs.krita.org/en/user_manual/getting_started/installation.html#linux
[from Flathub]: https://flathub.org/apps/org.kde.krita
