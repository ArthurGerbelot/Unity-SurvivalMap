#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MapEndless))]
public class MapEndlessEditor : Editor {

	public override void OnInspectorGUI() {
		MapEndless mapEndless = (MapEndless)target;
		if (DrawDefaultInspector ()) {
			//mapEndless.UpdateChunks ();
		}
	}

}
#endif
/**/