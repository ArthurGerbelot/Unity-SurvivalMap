using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum DisplayModes { ChunkCoords, WorldGroudNoise, WorldRegionNoise, WorldGroundColored, WorldColored, WorldChunksStates, WorldChunksZoneStates, WorldZonesMerging, WorldMerged }

public class MapDisplay : MonoBehaviour {

	public DisplayModes displayMode;
	public Material chunkMeshMaterial;
	public bool displayFogOfWar;
	public bool displayChunkTexture;
	public MapTextureData mapTextureData;

	#region singleton
	public static MapDisplay instance;

	void Awake () {
		if (MapDisplay.instance == null) {
			DontDestroyOnLoad (this.gameObject);
			MapDisplay.instance = this;
			/* OnAwake */
			OnAwake ();
		} else {
			Destroy (this.gameObject);
		}
	}
	#endregion

	void OnAwake() {
		this.UpdateMapShader ();
	}

	public void UpdateMapShader() {
		WorldChunkSettings setting = MapEngine.instance.worldChunkSetting;
		this.mapTextureData.UpdateStartHeights (setting);
		this.mapTextureData.ApplyOnMaterial (this.chunkMeshMaterial);
	}
	
	public void ForceRedrawAll () {
		foreach (KeyValuePair<Coord, WorldChunk> chunkPair in MapEndless.instance.worldChunks) {
			chunkPair.Value.displayMode = this.displayMode;
			this.UpdateChunkDisplay (chunkPair.Value);
		}
	}
	public void UpdateChunkDisplay(WorldChunk chunk) {
		this.UpdateChunkDisplay (chunk, false);
	}

	public void UpdateChunkDisplay(WorldChunk chunk, bool avoidTextureRendering) {
		WorldChunkSettings setting = MapEngine.instance.worldChunkSetting;
		this.UpdateChunkName (chunk, setting);
		Renderer renderer = chunk.chunkObject.GetComponent<Renderer> ();

		if (chunk.isVisible) {
			renderer.material.color = Color.white;
		}

		if (displayChunkTexture && !avoidTextureRendering) {
			// Need more than Creation
			if (chunk.state > ChunkStates.Created) {
				if (chunk.displayMode == DisplayModes.ChunkCoords) {
					renderer.material.mainTexture = TextureGenerator.TextureFromCoords (setting);
				} else if (chunk.displayMode == DisplayModes.WorldGroudNoise) {
					renderer.material.mainTexture = TextureGenerator.TextureFromHeightMap (chunk, setting, false);
				} else if (chunk.displayMode == DisplayModes.WorldRegionNoise) {
					renderer.material.mainTexture = TextureGenerator.TextureFromHeightMap (chunk, setting, true);
				} else if (chunk.displayMode == DisplayModes.WorldGroundColored) {
					renderer.material.mainTexture = TextureGenerator.WorldGroundColoredTexture (chunk, setting);
				} else if (chunk.displayMode == DisplayModes.WorldColored) {
					renderer.material.mainTexture = TextureGenerator.WorldColoredTexture (chunk, setting);
				} else if (chunk.displayMode == DisplayModes.WorldChunksStates) {
					renderer.material.mainTexture = TextureGenerator.WorldChunkStatesTexture (chunk, setting);
				} else if (chunk.displayMode == DisplayModes.WorldChunksZoneStates) {
					renderer.material.mainTexture = TextureGenerator.WorldChunksZoneStatesTexture (chunk, setting);
				} else if (chunk.displayMode == DisplayModes.WorldZonesMerging) {
					renderer.material.mainTexture = TextureGenerator.WorldZonesMergingTexture (chunk, setting);
				} else if (chunk.displayMode == DisplayModes.WorldMerged) {
					renderer.material.mainTexture = TextureGenerator.WorldMergingTexture (chunk, setting);
				}

			} else {
				renderer.material.mainTexture = TextureGenerator.TextureFromCoords (setting);
			}

			// Setup Texture Scale/Offset
			renderer.material.SetTextureScale("_MainTex", new Vector2(
				Coord.Left.x,
				Coord.Bottom.y
			));
			renderer.material.SetTextureOffset ("_MainTex", new Vector2 (
				(Coord.Left.x) == -1 ? 1 : 0, 
				(Coord.Bottom.y) == -1 ? 1 : 0
			));

			/* 
			// In case we don't care about flipping World Coords X/Y and Unity World 3D X/Y the good code is:
			renderer.material.SetTextureScale("_MainTex", new Vector2(-1,-1));
			renderer.material.SetTextureOffset ("_MainTex", new Vector2 (1, 1)); // Because of scale [-1;-1]
			*/
		}	
	}

	public void UpdateChunkName(WorldChunk chunk, WorldChunkSettings setting) {
		chunk.chunkObject.name = (chunk.isVisible ? "[x]" : "[ ]") + " " + chunk.coord;
		if (chunk.state == ChunkStates.Created) { 
			if (chunk.requireState > ChunkStates.Created) {
				chunk.chunkObject.name += " Loading... ";
			} else {
				chunk.chunkObject.name += " Created";
			}
		} else if (chunk.state == ChunkStates.Loaded) { 
			if (chunk.isComputing) {
				chunk.chunkObject.name += " Computing...";
			} else {
				chunk.chunkObject.name += " Noise Loaded";
			}
		} else if (chunk.state == ChunkStates.Computed) { 
			chunk.chunkObject.name += " Computed ";
		} else if (chunk.state == ChunkStates.Merged) { 
			chunk.chunkObject.name += " Merged ";
		}
		chunk.chunkObject.name += " (req: " + chunk.requireState + ") ";
		if (chunk.state >= ChunkStates.Computed) {
			chunk.chunkObject.name += " [zone: " + chunk.chunkComputed.zones.Count + " | " + Math.Round (chunk.computingTime, 2) + "sec | tests: " + chunk.chunkComputed.countTest + "/" + (setting.scaledSize * setting.scaledSize) + "]";
		}
	}
}


