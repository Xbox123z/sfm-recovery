# Godot Android build-template installer note

Source: [Godot 4.7-stable `editor/export/export_template_manager.cpp`](https://github.com/godotengine/godot/blob/4.7-stable/editor/export/export_template_manager.cpp) (read-only via GitHub API).

- `get_android_source_zip()` (around lines 1393–1403) first checks the preset property `gradle_build/android_source_template`; if empty, it uses the export-template directory plus `GODOT_VERSION_FULL_CONFIG` and `android_source.zip`.
- `get_android_template_identifier()` (around lines 1405–1414) stores either the custom source zip path plus its MD5, or the default Godot template config identifier.
- `install_android_template_from_file()` (around lines 1430–1457) installs the zip into the project's `res://android` tree, writes the identifier to `res://android/.build_version`, creates `res://android/build/.gdignore`, then extracts the source zip.
- This explains why simply unzipping `android_source.zip` into `android/build` is insufficient: the installer metadata at `android/.build_version` is also expected.
