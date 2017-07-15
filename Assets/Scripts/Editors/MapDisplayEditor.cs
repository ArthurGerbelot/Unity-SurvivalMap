#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MapDisplay))]
public class MapDisplayEditor : Editor {

	public override void OnInspectorGUI() {
		MapDisplay mapDisplay = (MapDisplay)target;
		if (DrawDefaultInspector ()) {
			mapDisplay.ForceRedrawAll ();
		}
	}

}
#endif
/**/