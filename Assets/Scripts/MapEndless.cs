using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum DevNoiseType {NoDev, Empty, SmallZoneNoBorder, 
	OneSmallLine, TwoSmallLines, LongHorizontalBar, LongHorizontalBarWithStop, LongVerticalBar, 
	SmallL, L, T, ReversedT, SmallCross, LargeCross,
	TwoLongParallelLines, TwoLongParallelLinesWithOneBigger,
	FakeZone, FakeZoneAndEmpty, DoubleFakeZone, FakeZoneOnTop, SmallGroundMergeMainGround, FakeZoneU, FakeCross, FakeCornerLTopRight,
	Hook, PreCreatedHook, N, LongHook,
	Corner, Corners, CornerSquare, Big,
	MainGroundCorner, FakeZoneCrossCorner,
	CaseA
}

public enum ChunkLevel {World, Region, Human, Detail}

public class MapEndless : MonoBehaviour {

	public GameObject chunkPrefab;
	public ChunkStates stateInBoundChunk;
	public DevNoiseType devNoiseType;

	public int devMaxChunkCoord;

	public Dictionary<Coord, WorldChunk> worldChunks = new Dictionary<Coord, WorldChunk>();
	List<WorldChunk> worldChunksInView = new List<WorldChunk> ();
	public List<WorldZone> worldZones = new List<WorldZone>();
	public WorldZone mainGround;

	#region singleton
	public static MapEndless instance;
	void Awake () {
		if (MapEndless.instance == null) {
			DontDestroyOnLoad (this.gameObject);
			MapEndless.instance = this;
			/* OnAwake */
			OnAwake ();
		} else {
			Destroy (this.gameObject);
		}
	}
	#endregion
	void OnAwake() {
		this.mainGround = new WorldZone (WorldZoneTypes.Ground);
	}

	#region dev
	void OnGUI() {
		WorldChunkSettings setting = MapEngine.instance.worldChunkSetting;
		Coord viewer = new Coord (Viewer.instance.bound.center, setting);
		GUI.color = Color.black;
		GUI.Label (new Rect (10f, 30f, 250f, 30f), viewer + " - " + Viewer.instance.scale + ": " + GetChunkLevel(Viewer.instance.scale));
		float y = 50f;

		if (/* Display all zones ? */ false) {
			for (int i = 0; i < this.worldZones.Count; i++) {
				y = this.OnGUIZone (this.worldZones [i], y);
			}
		} else {
			Bounds viewerBound = Viewer.instance.bound;
			Coord viewerChunkCoord = new Coord (viewerBound.center, setting);
			WorldChunk chunk = this.worldChunks [viewerChunkCoord];
			for (int i = 0; i < chunk.worldZonesRefs.Count; i++) {
				y = this.OnGUIZone (chunk.worldZonesRefs[i], y);
			}
		}
	}

	float OnGUIZone(WorldZone zone, float y) {
		float w = 500f;
		y += 30f;
		GUI.Label (new Rect (10f, y, w, 30f), "Zone [" + zone.randomInt + "]: " + (zone.isMainGround ? "Main " : "") + zone.type.ToString () + " -> " + zone.state.ToString () + ">" + zone.requireZoneState.ToString ());

		if (!zone.isMainGround) {
			y += 15f;
			GUI.Label (new Rect (10f, y, w, 30f), "    On chunks: ");
			for (int c = 0; c < zone.chunks.Count; c++) {
				Coord coord = zone.chunks [c];
				y += 15f;
				string dev = "  " + zone.chunkZones [coord].Count + "x [";
				for (int i = 0; i < zone.chunkZones [coord].Count; i++) {
					dev += (i == 0 ? "" : ", ") + (zone.chunkZones [coord] [i].containAllCoords ? " ALL " : zone.chunkZones [coord] [i].coords.Count.ToString ());
				}
				GUI.Label (new Rect (10f, y, w, 30f), "   -> " + coord + dev + "]");
			}
		}
		if (zone.state == WorldZoneStates.Created) {
			y += 25f;
			GUI.Label (new Rect (10f, y, w, 30f), "    Missings chunks: ");
			List<Coord> missingChunks = zone.GetMissingChunks ();
			for (int c = 0; c < missingChunks.Count; c++) {
				y += 15f;
				GUI.Label (new Rect (10f, y, w, 30f), "   -> " + missingChunks [c]);
			}
		}
		return y;
	}
	#endregion

	#region chunk
	public void CreateChunk (Coord coord) {
		this.CreateChunk (coord, ChunkStates.Computed);
	}
	public void CreateChunk (Coord coord, ChunkStates state) {
		if (devMaxChunkCoord > 0) {
			if (Math.Abs (coord.x) > devMaxChunkCoord || Math.Abs (coord.y) > devMaxChunkCoord) {
				Debug.Log ("Avoid creation because " + coord + " is over DEV " + devMaxChunkCoord);
				return;
			}
		}

		GameObject chunkObject = Instantiate (chunkPrefab) as GameObject;
		WorldChunk newWorldChunk = new WorldChunk (coord, chunkObject, MapEngine.instance.worldChunkSetting, MapDisplay.instance.displayMode, state);
		this.worldChunks.Add (coord, newWorldChunk);
	}

	// When we receive the order (from viewer only?) to update the map based on position
	public void UpdateChunks() {
		// Compute Bound 
		Bounds viewerBound = Viewer.instance.bound;
		WorldChunkSettings setting = MapEngine.instance.worldChunkSetting;

		ChunkLevel chunkLevel = this.GetChunkLevel (Viewer.instance.scale);

		// Get the bound min/max [@TODO: Wy don't reverse TOP or RIGHT so min/max already are boundMin and boundMax !! But actually it's for dev]
		// Later (when Coord.Top/Left.. are sure uncomment min/max and stop compute them) 
		Coord boundMin /*min*/ = new Coord (viewerBound.min, setting);
		Coord boundMax /*max*/ = new Coord (viewerBound.max, setting);
		// Cause Coord.Top & Coord.Right can be reverse, check min/max
		Coord min = new Coord (
			(boundMin.x < boundMax.x) ? boundMin.x : boundMax.x,
			(boundMin.y < boundMax.y) ? boundMin.y : boundMax.y
		);
		Coord max = new Coord (
			(boundMin.x > boundMax.x) ? boundMin.x : boundMax.x,
			(boundMin.y > boundMax.y) ? boundMin.y : boundMax.y
		);

		// Hide chunk you are not on bound
		if (this.worldChunksInView.Count > 0) {
			for (int idx = 0; idx < this.worldChunksInView.Count; idx++) {
				WorldChunk chunk = this.worldChunksInView [idx];
				// Not In bound, Unload
				if (chunk.coord.x < min.x || chunk.coord.y < min.y || chunk.coord.x > max.x || chunk.coord.y > max.y) {
					this.worldChunksInView [idx].Unload(setting);
				}
			}
		}
		this.worldChunksInView.Clear ();

		//  Regenerate new
		for (int y = min.y; y <= max.y; y++) {
			for (int x = min.x; x <= max.x; x++) {
				Coord c = new Coord (x, y);
				if (this.worldChunks.ContainsKey (c)) {
					// If chunk is here, but need more
					if (this.worldChunks [c].state < this.stateInBoundChunk) {
						this.worldChunks [c].UpdateState (setting, this.stateInBoundChunk);
					}
					this.worldChunks [c].Load(MapDisplay.instance.displayMode);
				} else {
					this.CreateChunk (c, this.stateInBoundChunk);
				}
				// Visible Chunk
				this.worldChunksInView.Add (this.worldChunks [c]);
			}
		}
		setting.parent.gameObject.name = "Map Chunks [" + this.worldChunksInView.Count + "/" + this.worldChunks.Count + "] [zones: " + worldZones.Count + "]";		
	}
	public ChunkLevel GetChunkLevel(float scale) {
		if (scale > 250) {
			return ChunkLevel.World;
		}
		if (scale > 50) {
			return ChunkLevel.Region;
		}
		if (scale > 10) {
			return ChunkLevel.Human;
		}
		return ChunkLevel.Detail;
	}
	#endregion

	#region treaded-chunk
	// When data are received from chunk
	public void OnWorldChunkThreadReceived(WorldChunk _chunk) {
		// Get the info that a chunk is Loaded. (in case it's not on bound)
		this.worldChunksInView.Add (_chunk);
		WorldChunkSettings setting = MapEngine.instance.worldChunkSetting;

		if (_chunk.state == ChunkStates.Computed) {
			this.CreateWorldZones (_chunk, setting);
			// Will be updated after TryMergeZone
		}
		if (_chunk.state == ChunkStates.Meshed) {
			this.CreateMeshAndUpdateSideMeshs (_chunk, setting);
		}
	}
	#endregion


	#region zones
	public void CreateWorldZones(WorldChunk chunk, WorldChunkSettings setting) {
		// First create, AND AFTER ALL CREATED try to merge (or a chunk can think he's all merged but some zones are still not created)
		for (int idx = 0; idx < chunk.chunkComputed.zones.Count; idx++) {
			WorldChunkComputed.WorldChunkZone zone = chunk.chunkComputed.zones [idx];

			// It's enought to be directly integrated to mainGround
			if (zone.isMainGround(setting)) { 
				if (!this.mainGround.chunks.Contains (chunk.coord)) {
					this.mainGround.chunks.Add (chunk.coord);
				}
				if (!this.mainGround.chunkZones.ContainsKey(chunk.coord)) {
					this.mainGround.chunkZones[chunk.coord] = new List<WorldChunkComputed.WorldChunkZone>();
				}
				this.mainGround.chunkZones [chunk.coord].Add (zone); // Add a ref to it

				zone.worldZoneRef = this.mainGround;
				if (!chunk.worldZonesRefs.Contains (this.mainGround)) {
					chunk.worldZonesRefs.Add (this.mainGround);
				}
			} else {
				// Create a new zone 
				// If this zone already created on another side it's will be merged when all chunks are computed
				WorldZone worldZone = new WorldZone (chunk, zone);
				this.worldZones.Add (worldZone);
				zone.worldZoneRef = worldZone;
				chunk.worldZonesRefs.Add (worldZone);
			}				
		}
	
		chunk.MergeZones(setting);
	}
	#endregion

	#region meshs
	void CreateMeshAndUpdateSideMeshs(WorldChunk chunk, WorldChunkSettings setting) {
		//GameObject meshObjectContainer = new GameObject();
		chunk.meshObject = new GameObject();
		MeshRenderer renderer = chunk.meshObject.AddComponent<MeshRenderer> ();
		MeshFilter filter = chunk.meshObject.AddComponent<MeshFilter> ();

		filter.mesh = chunk.meshData.CreateMesh ();
		renderer.material = MapDisplay.instance.chunkMeshMaterial;
		renderer.material.mainTexture = TextureGenerator.WorldMergingTexture (chunk, setting);
		// @TODO: Why do I have to do that ? Texture are 1coord too big for mesh, but are good for chunk (on dev cube)
		float textureScale = 1f + (1f / setting.scaledSize);
		renderer.material.mainTextureScale = new Vector2 (textureScale, textureScale);

	/*	DisplayMeshFilterNormals displayMeshFilterNormals = chunk.meshObject.AddComponent<DisplayMeshFilterNormals> ();
		displayMeshFilterNormals.normalLength = setting.size / 10f;
		displayMeshFilterNormals.normalColor = Color.black;*/
		/*
		meshObjectContainer.transform.name = chunk.coord.ToString();
		meshObjectContainer.transform.parent = setting.meshParent;
		meshObjectContainer.transform.localPosition = chunk.coord.ToWorldPosition (setting);
*/
		//	chunk.meshObject.transform.parent = meshObjectContainer.transform;
		chunk.meshObject.transform.parent = setting.meshParent;
		chunk.meshObject.transform.name = chunk.coord.ToString();

		chunk.meshObject.transform.localPosition = chunk.coord.ToWorldPosition (setting);//Vector3.zero;
		// Why ? setting.scaledSize
		chunk.meshObject.transform.localScale = new Vector3(setting.size / setting.scaledSize, setting.size, setting.size / setting.scaledSize);


		// If any sides chunks are loaded, update it
		for (int y = chunk.coord.y - 1; y <= chunk.coord.y + 1; y++) {
			for (int x = chunk.coord.x - 1; x <= chunk.coord.x + 1; x++) {
				if (x == chunk.coord.x && y == chunk.coord.y) {
					continue;
				}
				Coord sideCoord = new Coord (x, y);
				if (chunk.chunkBorders.sidesChunks.ContainsKey (sideCoord )) {
					WorldChunk sideChunk = chunk.chunkBorders.sidesChunks [sideCoord];
					if (sideChunk.state >= ChunkStates.Meshed) {
						MeshGenerator.UpdateChunkMesh (sideChunk, setting);
					}
				}
			}
		}
	}
	#endregion

}