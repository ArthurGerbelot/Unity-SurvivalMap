using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ChunkStates:int { Created=0, Loaded=1, Computed=2, Merged=3, Meshed=4 }

public class WorldChunk {
	public Coord coord; 
	public GameObject chunkObject;

	public bool isVisible;
	public bool isLoading; // Add a state ? or handle like that with bool?
	public bool isComputing; // Add a state ? or handle like that with bool?
	public bool isMeshing; // Add a state ? or handle like that with bool?
	public float computingTime; // When computing it's the start Time.time, when computed it's the number of second

	public ChunkStates state;
	public ChunkStates requireState; // If it's displayed, or it's loaded for computing next (as border)
	public DisplayModes displayMode;

	public WorldChunkData chunkData;
	public WorldChunkComputed chunkComputed;
	public MeshData meshData;
	public WorldChunkSideBorders chunkBorders; // Top Left information about first line topRight (if merged) to draw mesh between 2 chunks (final value stored) 
	public List<WorldZone> worldZonesRefs = new List<WorldZone> (); 

	public GameObject meshObject;

	#region construct
	public WorldChunk(Coord _coord, GameObject _chunkObject, WorldChunkSettings setting, DisplayModes _displayMode, ChunkStates _requireState) {
		this.coord = _coord;
		this.state = ChunkStates.Created;
		this.displayMode = _displayMode;
		this.isVisible = false;
		this.isLoading = false;
		this.isComputing = false;
		this.isMeshing = false;
		this.requireState = _requireState;

		this.chunkObject = _chunkObject;
		this.chunkObject.transform.parent = setting.parent;
		this.chunkObject.transform.localScale = new Vector3(setting.size, .01f, setting.size);
		this.chunkObject.transform.localPosition = this.coord.ToWorldPosition (setting);
		MapDisplay.instance.UpdateChunkName (this, setting);

		// Request Data
		if (this.requireState >= ChunkStates.Loaded) {
			this.requestData (setting);
		} else {
			MapDisplay.instance.UpdateChunkDisplay (this);
		}
	}
	#endregion

	#region update
	public void UpdateState(WorldChunkSettings setting, ChunkStates requiredState) {
		if (this.requireState < requiredState) {
			this.requireState = requiredState;
			if (this.state == ChunkStates.Created && this.requireState > ChunkStates.Created) {
				if (this.isLoading) {
					return;
				}
				this.requestData (setting);
			} else if (this.state == ChunkStates.Loaded && this.requireState > ChunkStates.Loaded) {
				if (this.isComputing) {
					return;
				}
				this.requestComputed (setting);
			} else if (this.state == ChunkStates.Computed && this.requireState > ChunkStates.Computed) {
				// All zones are already computed by sideChunks but we didn't catch it, try now ?
				//this.TestMerged(setting);
				// Retest now is state still Computed
				if (this.state == ChunkStates.Computed) {
					for (int zone_idx = 0; zone_idx < this.worldZonesRefs.Count; zone_idx++) {
						this.worldZonesRefs [zone_idx].UpdateState (WorldZoneStates.Merged);
					}
				}
				this.MergeZones(setting);
			}
		}
	}
	public void Load() {
		this.Load (MapDisplay.instance.displayMode);
	}
	public void Load(DisplayModes _displayMode) {
		if (_displayMode != this.displayMode) {
			this.isVisible = true;
			this.displayMode = _displayMode;
			MapDisplay.instance.UpdateChunkDisplay (this);
		}
		if (!this.isVisible) {
			this.isVisible = true;
			MapDisplay.instance.UpdateChunkDisplay (this, true);
		}
	}

	public void Unload(WorldChunkSettings setting) {
		this.isVisible = false;
		if (MapDisplay.instance.displayFogOfWar == true) {
			Renderer renderer = this.chunkObject.GetComponent<Renderer> ();
			renderer.material.color = Color.gray;		
		}
		MapDisplay.instance.UpdateChunkName (this, setting);
	}
	#endregion

	// Get Height map as saved:
	// Size: 50x50  - Scale: 10 -> coord from [0;0] to [5;5]
	// coord can be on a side chunk (ex x:-1 or y:setting.scaledSize), return sidechunk value he's merged
	public float GetHeightValue(Coord coord, WorldChunkSettings setting) {
		if (this.state < ChunkStates.Merged) {
			return 0f;
		}
		// It's on a side chunk, get the chunk and the right coord
		if (coord.x < 0 || coord.y < 0 || coord.x >= setting.scaledSize || coord.y >= setting.scaledSize) {
			Coord chunkCoord = new Coord (this.coord.x, this.coord.y);
			if (coord.x < 0) {
				chunkCoord.x += Coord.Left.x; // x--
			} else if (coord.x >= setting.scaledSize) {
				chunkCoord.x += Coord.Right.x; // x++
			}
			if (coord.y < 0) {
				chunkCoord.y += Coord.Bottom.y; // y--
			} else if (coord.y >= setting.scaledSize) {
				chunkCoord.y += Coord.Top.y; // y++
			}
			// The chunk is loaded AND merged 
			if(this.chunkBorders.sidesChunks.ContainsKey(chunkCoord)) {
				WorldChunk sideChunk = this.chunkBorders.sidesChunks [chunkCoord];
				if (sideChunk.state >= ChunkStates.Merged) {
					if (coord.x < 0) {
						coord.x += setting.scaledSize;
					} else if (coord.x >= setting.scaledSize) {
						coord.x -= setting.scaledSize;
					}
					if (coord.y < 0) {
						coord.y += setting.scaledSize;
					} else if (coord.y >= setting.scaledSize) {
						coord.y -= setting.scaledSize;
					}
					return sideChunk.GetHeightValue (coord, setting);
				}
			} 
			// The chunk isn't loaded.. take the nearest on the same chunk by updating the coord requested
			if (coord.x < 0) {
				coord.x = 0;
			} else if (coord.x >= setting.scaledSize) {
				coord.x = setting.scaledSize - 1;
			}
			if (coord.y < 0) {
				coord.y = 0;
			} else if (coord.y >= setting.scaledSize) {
				coord.y = setting.scaledSize - 1;
			}
		}

		// Reset index, cause x/y may was updated
		coord.SetIndex (setting);

		float noiseHeight = this.chunkData.GetHeightValue (coord);
		WorldZone zone = this.chunkComputed.GetZone (coord).worldZoneRef;
		return MeshGenerator.GetRealHeight (noiseHeight, zone.type, setting);
	}




	#region thread
	public void requestData(WorldChunkSettings setting) {
		this.isLoading = true;
		MapThreading.instance.RequestWorldChunkData (this, setting, OnWorldChunkDataReceived);
	}
	public void OnWorldChunkDataReceived(WorldChunkData _chunkData) {
		WorldChunkSettings setting = MapEngine.instance.worldChunkSetting;
		// Loaded
		this.chunkData = _chunkData;
		this.state = ChunkStates.Loaded;
		this.isLoading = false;

		// @TODO: if the chunk are loaded when it's no more displayed, will not be handled on `worldChunkInView`
		this.isVisible = true;

		// If it's not a border chunk (to compute the next chunk) request for computing
		if (this.requireState >= ChunkStates.Computed) {
			this.requestComputed (setting);
		}

		// Rendering
		MapDisplay.instance.UpdateChunkDisplay (this);

		// Inform MapEndless
		MapEndless.instance.OnWorldChunkThreadReceived (this);
	}

	public void requestComputed(WorldChunkSettings setting) {
		// Request Data
		this.isComputing = true;
		this.computingTime = Time.time;
		MapThreading.instance.RequestWorldChunkComputed(this, setting, OnWorldChunkComputedReceived);
	}
	public void OnWorldChunkComputedReceived(WorldChunkComputed _chunkComputed) {
		// Computed
		this.chunkComputed = _chunkComputed;
		this.state = ChunkStates.Computed;
		this.computingTime = Time.time - this.computingTime;
		this.isComputing = false;

		// Inform MapEndless
		MapEndless.instance.OnWorldChunkThreadReceived (this);

		// Rendering
		MapDisplay.instance.UpdateChunkDisplay (this);
	}
	public void OnChunkMerged() {
		WorldChunkSettings setting = MapEngine.instance.worldChunkSetting;
		this.state = ChunkStates.Merged;
		if (this.requireState < ChunkStates.Merged) { // If all the chunk is merged but wasn't required for (ex: side chunk with not border)
			this.requireState = ChunkStates.Merged;
		}
		MapDisplay.instance.UpdateChunkDisplay (this);
		this.isMeshing = true;
		// Create the first time the chunk borders values
		this.chunkBorders = new WorldChunkSideBorders (this, setting);
		// Request Mesh data
		MapThreading.instance.RequestWorldChunkMeshData (this, this.chunkBorders, setting, OnWorldChunkMeshReceived);
	}
	public void OnWorldChunkMeshReceived(MeshData meshData) {
		this.isMeshing = true;
		this.meshData = meshData;
		this.state = ChunkStates.Meshed;
		if (this.requireState < ChunkStates.Meshed) {
			this.requireState = ChunkStates.Meshed;
		}
		// Create Mesh a first time
		MapEndless.instance.OnWorldChunkThreadReceived (this);
		// Direct Update Mesh after creating (if some chunk as been updated during this thread)

		MeshGenerator.UpdateChunkMesh (this, MapEngine.instance.worldChunkSetting);
	}

	#endregion

	// All the Merging zone should be done using chunk and chunkZone (who have a ref to zone) and not WorldZone
	// Because if we have to merge {0:0 A} & {0:0 B} with {0:1 C} ( or {0:0 A} with {0:1 B} & {0:1 C}) 
	// The world zone will be deleted during a loop on it (to merge all connected)
	// With chunkZone, even if his ref have been updated to another (ex A > C), the chunkZone still exist and can continue to merge next 
	#region merge-zone

	// This chunk has been loaded (or state updated) and will try to merge with side chunk.
	// Use chunk.chunkZone instead of WorldZone during merge to avoir deleting during loop
	// WorldChunk.MergeZones is used to know witch directions have to be loaded/merged with this one. (I need to be merged, or my side chunk need me)
	public void MergeZones(WorldChunkSettings setting) {
		//return;
		if (this.state >= ChunkStates.Merged || this.state < ChunkStates.Computed) {
			return; // Done
		}
		bool isCurrentCheckIsMerging = (this.requireState >= ChunkStates.Merged);

		for (int e = 0; e < Coord.directions.Length; e++) {
			Direction direction = Coord.directions [e];
			Coord sideChunkCoord = this.coord.GetDirection (direction);

			this.MergeIfLoadedSideChunk (direction, setting);
		}

		this.TestZonesCompletedAndSeeMore (setting);
		this.TestMerged (setting);
	}

	// this.MergeZones found than `this` have to be merged on `direction`
	//  Because this require state is merged or sideChunk have zone look for this chunk
	void MergeIfLoadedSideChunk(Direction direction, WorldChunkSettings setting) {
		Coord sideChunkCoord = this.coord.GetDirection (direction);
	
		if (MapEndless.instance.worldChunks.ContainsKey (sideChunkCoord)) {
			// Get the chunk who need to be merged with
			WorldChunk sideChunk = MapEndless.instance.worldChunks [sideChunkCoord];
			// The side chunk is loaded, but not computed. Do it! (merging need Computed chunk)
			if (sideChunk.state < ChunkStates.Computed) {
				// Ask to be at least computed
				sideChunk.UpdateState (setting, ChunkStates.Computed);
			} 
			else {
				this.MergeSideChunk (direction, sideChunk, setting);
				sideChunk.MergeSideChunk (Coord.GetDirectionInverse (direction), this, setting);

				// Dev test ? will also check if the zone is completed (fakeZone)
				sideChunk.TestZonesCompletedAndSeeMore (setting);
			}
		}
	}

	// Merge all zone from this chunk and all sideChunk
	// Will be called one time and another time reversed (to found fakezone and ground zone who need to be merged with MainGround)
	void MergeSideChunk(Direction direction, WorldChunk sideChunk, WorldChunkSettings setting) {
		Direction inverseDirection = Coord.GetDirectionInverse (direction);

		for (int chunkZone_idx = this.chunkComputed.zones.Count - 1; chunkZone_idx >= 0; chunkZone_idx--) {
			WorldChunkComputed.WorldChunkZone chunkZone = this.chunkComputed.zones [chunkZone_idx];

			if (chunkZone.worldZoneRef.isMainGround) {
				continue; // Don't merge MainGround into another 
			}

			// Found a zone who need to be merged on direction (and it's not already done ? we repass here when reverse)
			if (chunkZone.isDirectionChunkBorder [direction] && chunkZone.missingChunks.Contains (sideChunk.coord)) {
				bool machingFound = false;

				for (int sideChunkZone_idx = sideChunk.chunkComputed.zones.Count - 1; sideChunkZone_idx >= 0; sideChunkZone_idx--) {

					WorldChunkComputed.WorldChunkZone sideChunkZone = sideChunk.chunkComputed.zones [sideChunkZone_idx];

					if (!sideChunkZone.isDirectionChunkBorder [inverseDirection]) {
						continue;
					}
					if (!sideChunkZone.missingChunks.Contains(this.coord)) {
						continue;
					}	
					if (sideChunkZone.type != chunkZone.type) {
						continue;
					}

					if (this.MergeIfMatchingCoords (chunkZone, direction, sideChunk, sideChunkZone, setting)) {
						machingFound = true;
					}
				}

				// IT's a fake bordered zone ! Some coord are on the limit of the chunk but no one on the next
				if (!machingFound && !chunkZone.worldZoneRef.isMainGround) {
					chunkZone.missingChunks.Remove (sideChunk.coord);
					chunkZone.isDirectionChunkBorder [direction] = false;
					chunkZone.directionChunkBorderCoords [direction].Clear ();
				
					/* done by sideChunk.SeeMore()
					 * if (chunkZone.worldZoneRef.GetMissingChunks ().Count == 0) {
						chunkZone.worldZoneRef.OnMergeCompleted ();
					}*/
				}
			}	
		}
	}
	bool MergeIfMatchingCoords(WorldChunkComputed.WorldChunkZone chunkZone, Direction direction, WorldChunk sideChunk, WorldChunkComputed.WorldChunkZone sideChunkZone, WorldChunkSettings setting) {
		Direction inverseDirection = Coord.GetDirectionInverse (direction);
		List<int> matchingCoords = new List<int> ();

		for (int coord_idx = 0; coord_idx < chunkZone.directionChunkBorderCoords [direction].Count; coord_idx++) {
			// Get coord next to this one on the side Chunk
		/*	Coord sideCoord = chunkZone.directionChunkBorderCoords [direction] [coord_idx].GetCoordOnChunkSide (direction, setting);

			if (sideChunkZone.directionChunkBorderCoords [inverseDirection].Contains (sideCoord)) {
				matchingCoords.Add ((direction == Direction.Top || direction == Direction.Bottom) ? sideCoord.x : sideCoord.y);
			}
			*/
			// Only X or Y
			int coord_value = chunkZone.directionChunkBorderCoords [direction] [coord_idx];
			if (sideChunkZone.directionChunkBorderCoords [inverseDirection].Contains (coord_value)) {
				matchingCoords.Add (coord_value);
			}
		}

		// Have no matchink coords between both zones
		if (matchingCoords.Count > 0) {
			this.RemoveAndMergeIntoSideZone (chunkZone, direction, sideChunk, sideChunkZone, matchingCoords, setting);
			return true;
		} 
		return false;
	}

	// We found two WorldChunkZone who match ! 
	// Remove the WorldZone related to this and merge into sideWorldZone
	void RemoveAndMergeIntoSideZone(WorldChunkComputed.WorldChunkZone chunkZone, Direction direction, WorldChunk sideChunk, WorldChunkComputed.WorldChunkZone sideChunkZone, List<int> matchingCoords, WorldChunkSettings setting) {
		Direction inverseDirection = Coord.GetDirectionInverse (direction);
		// Get ref everytime ! 
		// (If A is merged to B, and try after to be merged with C, take ref to {AmergedB} instead of {deletedA})
		WorldZone zone = chunkZone.worldZoneRef;
		WorldZone sideZone = sideChunkZone.worldZoneRef;
		/*
		if (zone.state > sideZone.state) {
			sideZone.state = zone.state;
		}*/
		if (zone.requireZoneState > sideZone.requireZoneState) {
			sideZone.requireZoneState = zone.requireZoneState;
		}

		// Loop all chunk who are on the zone to be remove
		for (int chunk_idx = zone.chunks.Count - 1; chunk_idx >= 0; chunk_idx--) {
			Coord zoneCoord = zone.chunks [chunk_idx];

			// This chunk didn't exist on sideZone
			if (!sideZone.chunks.Contains (zoneCoord)) {
				sideZone.chunks.Add (zoneCoord);
				sideZone.chunkZones.Add (zoneCoord, new List<WorldChunkComputed.WorldChunkZone> ());
			}

			// Remove missing chunk on both zone:
			chunkZone.missingChunks.Remove(sideChunk.coord);
		//	sideChunkZone.missingChunks.Remove(this.coord);

			// Update matching coords 
			chunkZone.directionChunkBorderCoords[direction] = matchingCoords;
		//	sideChunkZone.directionChunkBorderCoords[inverseDirection] = matchingCoords;

			// 
			if (zone.chunkZones.ContainsKey (zoneCoord)) {
				for (int zoneChunkZone_idx = zone.chunkZones [zoneCoord].Count - 1; zoneChunkZone_idx >= 0; zoneChunkZone_idx--) {
					WorldChunkComputed.WorldChunkZone zoneChunkZone = zone.chunkZones [zoneCoord] [zoneChunkZone_idx];
					// In case of corners, the diagonal chunk will be merge to both sides (who are shared by diagonals) and a loop is created
					// Check it's not the same
					if (zoneChunkZone.worldZoneRef != sideChunkZone.worldZoneRef) {

						sideZone.chunkZones [zoneCoord].Add (zoneChunkZone); 
						zoneChunkZone.worldZoneRef = sideChunkZone.worldZoneRef;
					}
				}
			}

			// Update all refs (to the new zone) on every chunk who contain this removed zone
			for (int chunkCoord_idx = zone.chunks.Count -1; chunkCoord_idx >= 0; chunkCoord_idx--) {
				MapEndless.instance.worldChunks [zone.chunks [chunkCoord_idx]].worldZonesRefs.Remove (zone);
				if (!MapEndless.instance.worldChunks [zone.chunks [chunkCoord_idx]].worldZonesRefs.Contains (sideZone)) {
					MapEndless.instance.worldChunks [zone.chunks [chunkCoord_idx]].worldZonesRefs.Add (sideZone);
				}
			}

			this.worldZonesRefs.Remove (zone);
			if (!this.worldZonesRefs.Contains(sideZone)) {
				this.worldZonesRefs.Add (sideZone);
			}
			MapEndless.instance.worldZones.Remove (zone);

			zone.isDeleted = true;
		}
	}
	void TestZonesCompletedAndSeeMore(WorldChunkSettings setting) {
		// Loop all zones to check if completed
		for (int zone_idx = this.worldZonesRefs.Count - 1; zone_idx >= 0; zone_idx--) {
			if (this.worldZonesRefs [zone_idx].isMainGround) {
				continue;
			}
			WorldZone sideZone = this.worldZonesRefs [zone_idx];

			if (sideZone.GetMissingChunks().Count == 0) {
				sideZone.OnMergeCompleted ();
			} else {
				// See more ?!
				if (sideZone.requireZoneState >= WorldZoneStates.Merged) {
					
					// BY ALL MISSNIG ?
					// Don't use by Direction because can create a missing chunk is one was already loaded (see bug-SeeMore-not-per-direction-but-missing.png)
					List<Coord> missingCoords = sideZone.GetMissingChunks ();
					for (int missing_idx = 0; missing_idx < missingCoords.Count; missing_idx++) {

						if (!MapEndless.instance.worldChunks.ContainsKey (missingCoords [missing_idx])) {
							MapEndless.instance.CreateChunk (missingCoords [missing_idx], ChunkStates.Computed);
						} else if (MapEndless.instance.worldChunks [missingCoords [missing_idx]].state < ChunkStates.Computed) {
							MapEndless.instance.worldChunks [missingCoords [missing_idx]].UpdateState (setting, ChunkStates.Computed);
						}
					}
					/**/

							// BY CHUNK DIRECTION
					/*
					for (int e = 0; e < Coord.directions.Length; e++) {
						Direction seeMoreDirection = Coord.directions [e];
						Coord seeMoreCoord = this.coord.GetDirection (seeMoreDirection);

						if (sideZone.chunks.Contains (seeMoreCoord)) {
						//	continue; // skip if already on this zone
						}

						// If it's not the direction we come from
						// And the zone who was used to merge, is also bordered on another chunk   
						for (int idx = this.worldZonesRefs [zone_idx].chunkZones[this.coord].Count - 1; idx >= 0; idx--) {
							if (this.worldZonesRefs [zone_idx].chunkZones[this.coord][idx].missingChunks.Contains(seeMoreCoord)) {
								// Already exist
								if (MapEndless.instance.worldChunks.ContainsKey(seeMoreCoord)) {
									continue;
								}

								MapEndless.instance.CreateChunk (seeMoreCoord, ChunkStates.Computed);
							}
						}
					}
					/**/
				}
			}
		}
	}

	public void TestMerged(WorldChunkSettings setting) {
			if (this.state >= ChunkStates.Merged) {
			return; //done
		}

		bool allZonesMerged = true;
		bool onlySmallGroundRemaining = true;

		for (int idx=0; idx < this.worldZonesRefs.Count; idx++) {
			WorldZone zone = this.worldZonesRefs [idx];
			if (zone.state < WorldZoneStates.Merged) {
				allZonesMerged = false;

				if (zone.type != WorldZoneTypes.Ground) {
				//	onlySmallGroundRemaining = false;
				}
			}
		}

		if (allZonesMerged) {
			this.OnChunkMerged ();
		} 
	}
	#endregion
}
