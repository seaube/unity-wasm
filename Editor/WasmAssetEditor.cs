using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Diagnostics;

#nullable enable

[System.Serializable]
public class WasmInfo {
	public static void Load
		( string                   wasmAssetPath
		, System.Action<WasmInfo>  successCallback
		, System.Action            errorCallback
		, Object?                  context = null
		)
	{
		WasmInfo? wasmInfo = null;
		var progressId = Progress.Start(
			$"Collecting WASM info",
			wasmAssetPath,
			Progress.Options.Indefinite
		);

		var proc = new Process();
		proc.StartInfo.FileName = "wasm-info";
		proc.StartInfo.CreateNoWindow = true;
		proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
		proc.StartInfo.Arguments = wasmAssetPath;
		proc.StartInfo.UseShellExecute = false;
		proc.StartInfo.RedirectStandardOutput = true;
		proc.StartInfo.RedirectStandardError = true;
		proc.EnableRaisingEvents = true;

		var jsonStr = "";
		proc.OutputDataReceived += (_, ev) => {
			jsonStr += ev.Data;
		};

		proc.ErrorDataReceived += (_, ev) => {
			if(!string.IsNullOrWhiteSpace(ev.Data)) {
				UnityEngine.Debug.LogError(ev.Data, context);
			}
		};

		proc.Exited += (_, _) => {
			try {
				if(proc.ExitCode == 0) {
					wasmInfo = JsonUtility.FromJson<WasmInfo>(jsonStr);
					Progress.Finish(progressId, Progress.Status.Succeeded);
					successCallback(wasmInfo);
				} else {
					UnityEngine.Debug.LogError(
						$"wasm-info exited with error code {proc.ExitCode}",
						context
					);
					Progress.Finish(progressId, Progress.Status.Failed);
					errorCallback();
				}
			} finally {
				Progress.Finish(progressId, Progress.Status.Failed);
			}
		};

		proc.Start();
		proc.BeginOutputReadLine();
		proc.BeginErrorReadLine();
	}

	[System.Serializable]
	public class ExternInfo {
		public string name = "";
		public string type = "";
		
		/// only set when type == WASM_EXTERN_FUNC
		public List<string>? @params;
		/// only set when type == WASM_EXTERN_FUNC
		public List<string>? results;
	}

	public List<ExternInfo> imports = new();
	public List<ExternInfo> exports = new();
}

[CustomEditor(typeof(WasmAsset))]
public class WasmAssetEditor : Editor {
	static bool importsFoldout = true;
	static bool exportsFoldout = true;

	bool loadingWasmInfo = false;
	bool hasDrawnAfterLoad = false;
	WasmInfo? wasmInfo;

	private void LoadWasmInfoIfNeeded
		( string assetPath
		)
	{
		if(loadingWasmInfo) return;
		if(wasmInfo != null) return;

		loadingWasmInfo = true;
		wasmInfo = null;

		WasmInfo.Load(
			wasmAssetPath: assetPath,
			successCallback: wasmInfo => {
				this.wasmInfo = wasmInfo;
				loadingWasmInfo = false;
			},
			errorCallback: () => {
				loadingWasmInfo = false;
			},
			context: target
		);
	}

	private void ExternInfoGUI
		( WasmInfo.ExternInfo info
		)
	{
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.LabelField(info.name);
		EditorGUILayout.Space();
		EditorGUILayout.LabelField(info.type, EditorStyles.selectionRect);
		EditorGUILayout.EndHorizontal();
	}

	public override void OnInspectorGUI() {
		var wasmAsset = target as WasmAsset;
		var assetPath = AssetDatabase.GetAssetPath(wasmAsset);

		LoadWasmInfoIfNeeded(assetPath);

		if(loadingWasmInfo) {
			EditorGUILayout.LabelField("Loading ...");
			return;
		}

		if(wasmInfo == null) {
			EditorGUILayout.LabelField("Could not load wasm info");
			return;
		}

		hasDrawnAfterLoad = true;

		importsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(
			foldout: importsFoldout,
			content: "Imports"
		);
		foreach(var import in wasmInfo.imports) {
			ExternInfoGUI(import);
		}
		EditorGUILayout.EndFoldoutHeaderGroup();

		exportsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(
			foldout: exportsFoldout,
			content: "Exports"
		);
		foreach(var export in wasmInfo.exports) {
			ExternInfoGUI(export);
		}
		EditorGUILayout.EndFoldoutHeaderGroup();
	}

	public override bool RequiresConstantRepaint() {
		return !hasDrawnAfterLoad;
	}
}
