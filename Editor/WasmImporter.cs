using UnityEngine;
using UnityEditor.AssetImporters;
using System.Reflection;
using System.Linq;
using System;

#nullable enable

[ScriptedImporter(1, "wasm")]
public class WasmImporter : ScriptedImporter {
	const string iconPath = "Assets/Editor/Wasm/wasm-icon.png";
	private static Texture2D? iconAsset;

	public override void OnImportAsset(AssetImportContext ctx) {
		if(iconAsset == null) {
			iconAsset = Resources.Load<Texture2D>(iconPath);
		}

		var asset = ScriptableObject.CreateInstance(typeof(WasmAsset)) as WasmAsset;
		ctx.AddObjectToAsset("WasmAsset", asset, iconAsset);
		ctx.SetMainObject(asset);

		var importerTypes = Assembly.GetExecutingAssembly()
			.GetTypes()
			.Where(t => t.IsDefined(typeof(WasmScriptedImporterAttribute)));

		foreach(var importerType in importerTypes) {
			if(!typeof(ScriptedImporter).IsAssignableFrom(importerType)) {
				Debug.LogError(
					$"Wasm scripted importer '{importerType.FullName}' must extend " +
					$"ScriptedImporter"
				);
				continue;
			}

			var importer = (ScriptedImporter)Activator.CreateInstance(importerType);
			importer.OnImportAsset(ctx);
		}
	}
}
