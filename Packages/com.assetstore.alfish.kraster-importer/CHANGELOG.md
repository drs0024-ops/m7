# Changelog

## 0.4.2 - 2025-02-02
ℹ️ **This update is strongly recommended if you're on Windows and use Krita animations.**
### Fixed
- Generation: Fixed a bug when generating animation frames in Windows. Patterns for frame numbers (e.g. `<frame00>` in the default animation pattern) should no longer cause an error.
- Generation: Log error when using an invalid path parameter (e.g. `<frame>`).

## 0.4.1 - 2025-10-08
### Added
- Thumbnails: **New icons for Source Image assets based on file type** (OpenRaster Image, Krita Image, Krita Animation).
- Preferences: Option to choose file type icon (new default) or image miniature (to keep old behavior if desired) as asset icon.
- Manual Image Converter: Add buttons to set the default image or animation patterns, so you can easily **use the manual converter to render multiple animations** as well.
- Manual Image Converter: Add generation mode field (set to automatic by default), so you can now also force internal or external mode on the advanced converter tool.
### Fixed
- Thumbnails: Fix static previews, which replace the big thumbnail in the project view and the icon in the inspector. These **previews are now loaded on demand again**, as intended, instead of being serialized like a big icon and occupying extra space.
- Generation: Fix krita script so a non-animated image only generates 1 frame (instead of krita's default 100 frames) when treated as an animation.
- Manual Image Converter: Correctly refresh asset database after generating animation frames.
- Importer, Source Image Preview: Avoid multiple unused mipmap calculations when resampling.
### Improved
- Manual Image Converter: Allow **faster conversions to PNG** by using internal mode. This means the manual converter too can now extract the PNG when appropriate instead of calling Krita Runner.
- Manual Image Converter: Add context object on manual conversion logs so the source image asset is pinged when selecting the log entry.
- Thumbnails: For Source Image and imported textures, their **16px asset icons are now resampled via a faster implementation** using Blit (which only supports point and linear filters and don't care about outline artifacts when alpha is transparency) instead of the advanced filters (implemented via compute shaders or CPU parallelism).

## 0.4.0 - 2025-09-10
⚠️ **THIS IS A MAJOR BREAKING CHANGE! DO NOT UPGRADE EXISTING PROJECTS WITHOUT A BACKUP!**  
Because of a necessary namespace change, **your previous .kra/.krz/.ora IMPORT SETTINGS WILL BE LOST if you just upgrade**!  
If you still want to upgrade on an existing project, follow the v0.4.0 [Upgrade Guide] to avoid losing most import settings!

[Upgrade Guide]: https://bitbucket.org/alfish/kraster-importer-for-unity/wiki/Upgrading.md

### Changed
- Package: The namespace of the code classes had to change to conform to an update in Unity's Submission Guidelines. **This BREAKING CHANGE will cause all import settings of Krita/.ora images from previous versions to be lost**. Files will reset to default import settings (usually that means nothing is imported). See the link above to try to work around this.
- Generation: Separate name patterns for generate image and generate animation. **This BREAKING CHANGE will cause the previous path pattern info to be lost and images regenerated at the new default path** pattern.
- Generation: Now **using kritarunner as the external CLI program** to handle Krita files (enable Krita again in Preferences). It's also used to handle ORA files as a fallback if a specific OpenRaster program (e.g. LazPaint) is not enabled.
- Generation: Animations delete detected frame files in the output path before regeneration. This also means if you reduce the amount of frames, the extra frame files will be deleted.
- Importer: Alpha Is Transparency flag now uses a **color dilation filter** to avoid outline artifacts from fully transparent pixel RGB in bilinear/trilinear filter modes.
- Importer: RGBA8 option now actually **uses RGBA instead of ARGB** format for the imported texture.
- Presets: Defaults have changed for some fields in the presets.
- Package: Minimum Unity version is now 2022.3.
### Added
- Importer: **Spritesheet mode** for importing regular grid-based sprite sheets.
- Importer: **Mipmap limits** feature.
- Importer: **Max size** feature with various **resizing filters**: Point, Box, Bilinear, Bicubic (Hermite, B-spline, Catmull-Rom, Mitchell-Netravali). It respects global override to max texture size on (re)importation.
- Preferences: **Option to ask for exportation settings** every time a file is converted.
- Preferences: Filter option for how Source Image previews are resized.
- Preferences: Allow a (slow) CPU resizing option, just in case the GPU compute shaders have issues.
### Fixed
- 🥳🎉 Generation: Finally **FIXED THE FREEZING ISSUE when Krita is open during importations** by using kritarunner instead of Krita CLI. This means you need to set it up again in Preferences. This also means Snap and AppImage Krita installations are not supported on Linux, so use preferably your distro's installation instead, or Flatpak. A separate second installation is no longer required.
- Generation: Image **conversion no longer changes based on last used exportation settings in Krita**. Instead, recommended parameters are always applied by default.
- Compatibility: Fix texture importation bug in Unity 6 when using certain platforms, caused by a compatibility code not being updated for a Unity 6 change in internals.
- Source Image Preview: Limit to 12 previews max to avoid freezing editor on massive multi-selecting.
- Source Image Preview: Use in-memory cache instead of saving PNG bytes in imported object, to save space.
### Improved
- Generation: Optional progress dialog when rendering animation frames.
- Generation: Timeout for Krita Runner is now a single field for both images and animations; on animations it's multiplied by the number of frames.
- Preferences: Warning dialog if user enables Krita using the wrong executable.
- Thumbnails: Use Bilinear filter to improve how Source Image assets appear in the editor.

## 0.3.0 - 2024-03-18
### Changed
- Minimum Unity version is now 2021.3.
- SourceImage previews are now independent of the imported texture and the icon, but they take a few seconds to load after reimportation.
### Added
- Manual Image Converter: quick conversions (to KRA, EXR, ORA, PNG) on the same folder when right-clicking an image asset, including on formats that Unity can't read but Krita can.
- Manual Image Converter: advanced batch conversion tool with full flexibility.
- Disclaimer for the user that even external Krita exportations can still be incorrect in some cases, like when the image has non-paint layers.
### Fixed
- Select RGTC format correctly when DXTC_RGTC is selected for Android.
- Krita timeout now terminates it correctly when under flatpak-spawn.
- Fixed non-square SourceImage icons issue on Unity 2023 by forcing them into a 16x16 px icon.
- Preferences: browse button was not setting text field when text is focused.
- Clarify errors telling how to enable Krita in Preferences.
- Small bugs on edge cases like zero-sized textures and unexpected color format names.

## 0.2.0 - 2023-03-09
### Changed
- Direct importer uses generic cross-platform compression formats like in the built-in texture importer. Avoid bad cross-platform formats as default.
### Added
- Automatic texture formats, auto-detect opaque image to remove alpha channel.
- Respect texture subtarget from player settings and build overrides. (Unity 2020.3 ~ 2022.2; Android, iOS, WebGL)
- Texture compression respects asset import overrides. (Unity 2021.2)
- Add R-only and RG-only texture channels, and their respective compression formats.
- Uncompressed fallbacks for unsupported sizes and mipmaps, with warnings.
### Improved
- Smarter property drawer UI for wrapMode axes.

## 0.1.0 - 2023-02-08
### Added
- Package meta-info.
- Source Image meta-data asset, and its icon.
- Importer for KRA, KRZ and ORA source images. Property types.
- User preferences to enable external Krita converter.
- Presets for common import settings.
- Code for detecting meta-info from Krita, ORA and PNG images.
- Code for inferring ICC color profiles.
- Editors for the importer, preview and preferences. Property drawers.
- Smart warnings for detecting and displaying probable mistakes from users.
- Documentation.
