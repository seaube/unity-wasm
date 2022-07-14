using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(WasmImporter))]
public class WasmImporterEditor : ScriptedImporterEditor {
	public override void OnInspectorGUI() {
		base.ApplyRevertGUI();
	}
}
