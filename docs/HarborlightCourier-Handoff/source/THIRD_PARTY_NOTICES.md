# Third-party notices

This project contains no imported game assets, plugins, audio tracks, or external font files. The maps, courier, patrol characters, parcels, vision indicators, and square UI are drawn at runtime from original C# geometry. The only standalone image is `harborlight_icon.svg`, an original icon drawn for this project.

The application is built with these separately licensed components:

- **Godot Engine 4.7 Mono and its export templates** — MIT License. This third-party license applies to Godot and its runtime components; it does not determine the license status of this project's original source or assets. License and third-party notices: <https://godotengine.org/license/> and <https://github.com/godotengine/godot/blob/master/COPYRIGHT.txt>.
- **Godot .NET SDK 4.7.0** — Godot's .NET build integration, distributed under the Godot project license.
- **.NET 8 runtime / Android workload** — Microsoft's .NET components; see <https://github.com/dotnet/runtime/blob/main/LICENSE.TXT> and <https://github.com/dotnet/android/blob/main/LICENSE.TXT>. Any bundled runtime keeps its own notices.
- **Godot built-in fallback font** — used through `ThemeDB.FallbackFont`; the font is supplied by the Godot distribution and retains the license noted in Godot's bundled third-party notices. No standalone font file is included in this project.

The local JDK and Android SDK are build tools, not original game assets. No additional NuGet packages are referenced. The license for original project code, the icon, and programmatically drawn elements has not yet been selected by the owner; see `LICENSE_STATUS.md`.
