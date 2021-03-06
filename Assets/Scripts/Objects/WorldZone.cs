using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WorldZoneTypes {Region, Water, Ground, Mountain}
public enum WorldZoneStates:int {Created=0, Merged=1}

public class WorldZone {
	public WorldZoneTypes type;
	public bool isMainGround;

	public WorldZoneStates state;
	public WorldZoneStates requireZoneState;
	public bool isDeleted;
	public List<Coord> chunks = new List<Coord> (); // chunk where the zone is on it

	// Link to all chunkzone for each chunk (a single zone can be at 2 separated places on a single chunk)
	public Dictionary<Coord, List<WorldChunkComputed.WorldChunkZone>> chunkZones = new Dictionary<Coord, List<WorldChunkComputed.WorldChunkZone>>();

//	public List<Coord> missingChunks = new List<Coord> (); // missing chunk next to complete
//	public List<Coord> computedChunks = new List<Coord> (); // list of computed chunk (create a bug when X chunks with same zone have X who have fakeZone with single chunk: missing will be added after 
	public int randomInt;
	public Color randomColor;

	// Main Ground
	public WorldZone(WorldZoneTypes type) {
		this.type = type;
		this.isMainGround = true; // Main Ground !
		this.isDeleted = false;

		this.state = WorldZoneStates.Merged;
		this.requireZoneState = WorldZoneStates.Merged;

		this.randomInt = 0;
		this.randomColor = new Color (1f, 245f/255f, 0f); // yellow
	}

	// World Zone created on a chunk, and expended after
	public WorldZone(WorldChunk chunk, WorldChunkComputed.WorldChunkZone zone) {

		this.randomInt = (int)Random.Range (1f, 100f);
		this.randomColor = new Color ( Random.Range (0f, 1f), Random.Range (0f, 1f),  Random.Range (0f, 1f));
		this.type = zone.type;
		this.isMainGround = false;
		this.isDeleted = false;

		if (zone.IsOnChunkBorder()) {
			this.state = WorldZoneStates.Created;
			this.requireZoneState = (chunk.requireState >= ChunkStates.Merged) ? WorldZoneStates.Merged : WorldZoneStates.Created;
			/*
			for (int e = 0; e < Coord.directions.Length; e++) {
				Direction direction = Coord.directions [e];
				Coord directionCoord = chunk.coord.GetDirection (direction);
				if (zone.isDirectionChunkBorder [direction]) {
					this.missingChunks.Add (directionCoord);
				}
			}*/
		} else {
			this.state = WorldZoneStates.Merged;
			this.requireZoneState = WorldZoneStates.Merged;
		}
		// Have chunk border
		this.chunks.Add (chunk.coord);

		// ref to this zone
		this.chunkZones[chunk.coord] = new List<WorldChunkComputed.WorldChunkZone>();
		this.chunkZones [chunk.coord].Add (zone); // Add a ref to it
	}


	// Result of all region zone-merging
	public void OnMergeCompleted() {
		this.state = WorldZoneStates.Merged;
		this.requireZoneState = WorldZoneStates.Merged;

		if (this.isMainGround) {
			return; // stop
		}
		for (int chunk_idx = 0; chunk_idx < this.chunks.Count; chunk_idx++) {
			WorldChunk chunk = MapEndless.instance.worldChunks [this.chunks [chunk_idx]];
			MapDisplay.instance.UpdateChunkDisplay (chunk); // Reload display for all chunk on this zone
			chunk.TestMerged(MapEngine.instance.worldChunkSetting);
		}
	}

	// This is called for already existing zone but not completely merge
	public void UpdateState(WorldZoneStates state) {
		if (this.requireZoneState < WorldZoneStates.Merged) {
			this.requireZoneState = WorldZoneStates.Merged;

			/*for (int missingChunk_idx = 0; missingChunk_idx < this.missingChunks.Count; missingChunk_idx++) {
				MapEndless.instance.MergeOrCreateZones(this.missingChunks[missingChunk_idx]);
			}*/

		}
	}

	public List<Coord> GetMissingChunks() {
		List<Coord> coords = new List<Coord> ();
		for (int idx_chunk = 0; idx_chunk < this.chunks.Count; idx_chunk++) {
			for (int idx_zone = 0; idx_zone < this.chunkZones [this.chunks [idx_chunk]].Count; idx_zone++) {
				WorldChunkComputed.WorldChunkZone chunkZone = this.chunkZones [this.chunks [idx_chunk]] [idx_zone];

				for (int idx_direction = 0; idx_direction < chunkZone.missingChunks.Count; idx_direction++) {
					if (!coords.Contains(chunkZone.missingChunks [idx_direction])) {
						coords.Add (chunkZone.missingChunks [idx_direction]);
					}
				}
			}					
		}
		return coords;
	}
}
