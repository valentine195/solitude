# Unity metadata validation

Unity `.meta` files are source files: their GUIDs bind scenes, prefabs, scripts,
and assets together. Commit each asset and folder with its existing metadata.
Never delete or regenerate metadata as a routine repair.

Run the validator from the Unity menu:

`SOLITUDE > Validation > Validate Unity Metadata`

For CI or a local batch check, run the project's Unity version with:

```sh
Unity -batchmode -quit -projectPath /path/to/SOLITUDE \
  -executeMethod SOLITUDE.Editor.UnityMetaValidator.ValidateForCI
```

The command fails when an imported asset or folder lacks metadata, metadata is
orphaned, a Git-versioned asset and its metadata are not versioned together,
or two metadata files under `Assets/` share a GUID.

When metadata is missing, recover it from Git history, another checkout, or a
backup. Generating a replacement GUID is a last resort because every serialized
reference to the old GUID must then be repaired deliberately.
