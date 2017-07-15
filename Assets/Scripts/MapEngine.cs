using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapEngine : MonoBehaviour {
	public int seed;

	public WorldChunkSettings worldChunkSetting;

	#region singleton
	public static MapEngine instance;
	void Awake () {
		if (MapEngine.instance == null) {
			DontDestroyOnLoad (this.gameObject);
			MapEngine.instance = this;
			/* OnAwake */
			OnAwake ();
		} else {
			Destroy (this.gameObject);
		}
	}
	#endregion

	void OnAwake() {
		if (seed == 0) {
			seed = Random.Range (int.MaxValue/2, int.MaxValue);
		}
		worldChunkSetting.fastNoiseGround.seed = seed;
		worldChunkSetting.fastNoiseRegion.seed = seed;
		worldChunkSetting.fastNoiseGround.SaveSettings (); 
		worldChunkSetting.fastNoiseRegion.SaveSettings ();
		    
		if (((worldChunkSetting.size / worldChunkSetting.scaledSize) % 1f) != 0) {
			Debug.Log ("setting size (" + worldChunkSetting.size + ") have to be a multiple of (" + worldChunkSetting.scaledSize + ")");
		}
	}
}

#region structs
[System.Serializable]
public struct WorldChunkSettings {
	// heightMap width and height are `size/scale` 
	public int size; // Size is x (int!) time smaller and have to be `impair` (odd?even?) 
	public int scaledSize; // How many coord are computed on the noise chunk 

	public float water;
	public float mountain;

	public int worldZoneMinSize; // Mountain || Water
	public int worldGroundZoneMinSize; // MainGround ? 

	public FastNoiseUnity fastNoiseGround;
	public FastNoiseUnity fastNoiseRegion;
	public Transform parent;
	public Transform meshParent;
}
#endregion