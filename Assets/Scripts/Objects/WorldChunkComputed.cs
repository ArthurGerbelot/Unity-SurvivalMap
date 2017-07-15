using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldChunkComputed {

	public List<WorldChunkZone> zones;
	//public WorldChunkZone mainGround;

	bool isCompleted; 
	List<Coord> availableCoords = new List<Coord> (); // To find the first coord on the new zone to compute

	// Test for dev
	public int countTest = 0;

	#region start-computing
	public WorldChunkComputed(WorldChunk _chunk, WorldChunkSettings setting) {
		// Init attribute
		this.isCompleted = false;
		this.zones = new List<WorldChunkZone>();

		// List all coord found next to found zones, an easy way to know here to start a new zone
		this.availableCoords.Add(new Coord(0,0)); // Init first zone at [0;0] (all chunk have a 00)

		// Loop to find all zones on the chunk
		this.FindNewZone (_chunk, setting);
		this.isCompleted = true; // Before Clean zone (ex: Ground -> MainGround)
		this.CleanZones (_chunk, setting);
	}
	#endregion

	#region compute
	void FindNewZone(WorldChunk _chunk, WorldChunkSettings setting) {
		// Coord found
		if (availableCoords.Count > 0) {
			Coord firstNotInZone = availableCoords [0];
			// Create a new zone based on this coord
			WorldChunkZone zone = new WorldChunkZone (_chunk.chunkData.GetZoneType(firstNotInZone, setting));
			this.zones.Add (zone);
			this.UpdateCursor (_chunk, setting, zone, firstNotInZone, new Coord(0,0));
			// Zone computed, check for next
			this.FindNewZone (_chunk, setting);
		}
	}

	void UpdateCursor(WorldChunk _chunk, WorldChunkSettings setting, WorldChunkZone zone, Coord coord, Coord lastCoordDirection) {
		this.countTest++;
		if (coord.x < 0 || coord.y < 0 || coord.x >= setting.scaledSize || coord.y >= setting.scaledSize) {
			// Is out of chunk!
			return;
		}
		// First already contains on the current zone (ex: [0;0] -> [0;1] -> [0;0] will append offen)
		if (zone.coords.Contains (coord)) {
			return;
		}
		// Test zone type (get the type based on heightMap)
		WorldZoneTypes coordType = _chunk.chunkData.GetZoneType(coord, setting);
		if (coordType != zone.type) {
			// It's not the same region (add it only one time)
			if (!this.HasZone(coord) && !this.availableCoords.Contains (coord)) {
				this.availableCoords.Add (coord);
			}
			return;
		}

		// It's a new on the same zone, add
		zone.AddCoord(coord, setting);
		// If the coord is on the free coord list (for future next list)
		if (this.availableCoords.Contains (coord)) {
			this.availableCoords.Remove (coord);
		}

		// Test all sides (but avoid returning on same than previous)
		if (lastCoordDirection != Coord.Top) UpdateCursor(_chunk, setting, zone, coord.GetDirection(Direction.Top), Coord.Bottom);
		if (lastCoordDirection != Coord.Bottom) UpdateCursor(_chunk, setting, zone, coord.GetDirection(Direction.Bottom), Coord.Top);
		if (lastCoordDirection != Coord.Left) UpdateCursor(_chunk, setting, zone, coord.GetDirection(Direction.Left), Coord.Right);
		if (lastCoordDirection != Coord.Right) UpdateCursor(_chunk, setting, zone, coord.GetDirection(Direction.Right), Coord.Left);
		return;
	}

	// When computing, return if this coord is already on a zone
	bool HasZone(Coord coord) {
		// If chunk is completed, all coord have a zone
		if (this.isCompleted) {
			return true;
		}
		// Other case check if it's contained on a zone
		for (int idx = 0; idx < zones.Count; idx++) {
			if (zones [idx].coords.Contains (coord)) {
				return true;
			}
		}
		return false;
	}

	void CleanZones(WorldChunk _chunk, WorldChunkSettings setting) {
		// Whatever zone it is, it's unique on this chunk. remove all coords and set boolean `containAllCoords`
		if (this.zones.Count == 1) {
			this.zones [0].coords.Clear();
			this.zones [0].containAllCoords = true;
		}
		// Handle MissingChunks foreach chubkZone based on borders
		for (int e = 0; e < Coord.directions.Length; e++) {
			Coord sideCoord = _chunk.coord.GetDirection (Coord.directions [e]);
			for (int idx = this.zones.Count - 1; idx >= 0; idx--) {
				if (this.zones [idx].isDirectionChunkBorder [Coord.directions [e]]) {
					this.zones [idx].missingChunks.Add (sideCoord);
				}
			}			
		}
	}
	#endregion

	#region world-chunk-zone
	public WorldChunkZone GetZone(Coord coord) {
		// No need to loop, it's the only one
		if (this.zones [0].containAllCoords) {
			return this.zones [0];
		}
		// Loop to find the coorect one
		for (int idx = 0; idx < zones.Count; idx++) {
			if (zones [idx].Contains (coord)) {
				return zones[idx];
			}
		}
		// Should never append, but can return nothing
		Debug.Log("Hummmmmmmmmmmmmmmmm ?!!!!! Should never append !!");
		return this.zones [0];/*MapEndless.instance.mainGround; */
	}

	public class WorldChunkZone {
		public WorldZoneTypes type;

		// On computing push all coord on coords, after that remove border coord to others List<>
		public bool containAllCoords;
		public List<Coord> coords = new List<Coord>();
		public WorldZone worldZoneRef; // Keep a ref to the WorldZone refered to this WorldChunkZone

		// Border
		public Dictionary<Direction, bool> isDirectionChunkBorder = new Dictionary<Direction, bool> (); // Is on border for a direction
		public Dictionary<Direction, List<int>> directionChunkBorderCoords = new Dictionary<Direction, List<int>> (); // List all coord next to direction chunk
		public List<Coord> missingChunks = new List<Coord> (); // missing chunk next to complete

		// Default Constructor
		public WorldChunkZone(WorldZoneTypes _type) {
			this.type = _type;

			this.containAllCoords = false;
			this.worldZoneRef = null;

			for (int e=0; e < Coord.directions.Length; e++) {
				isDirectionChunkBorder[Coord.directions[e]] = false;
				directionChunkBorderCoords[Coord.directions[e]] = new List<int>();
			}
		}

		public void AddCoord(Coord coord, WorldChunkSettings setting) { 
			this.coords.Add (coord);

			// Test chunks sides for each directions
			for (int e = 0; e < Coord.directions.Length; e++) {
				if (coord.IsOnChunkBorder (Coord.directions[e], setting)) {
					isDirectionChunkBorder [Coord.directions [e]] = true;
					directionChunkBorderCoords [Coord.directions [e]].Add (coord.GetBorderValue(Coord.directions [e]));  // Every coord who are not really bordered will be removed on future
				}
			}
		}

		public bool IsOnChunkBorder() {
			for (int e = 0; e < Coord.directions.Length; e++) {
				if (this.isDirectionChunkBorder [Coord.directions [e]]) {
					return true;
				}
			}
			return false;
		}
		public bool Contains(Coord coord) {
			// Contain all coords or on the list
			return (this.containAllCoords || this.coords.Contains (coord));
		}

		// when Zone is create,ask if create new, of direct merge to main ground
		public bool isMainGround(WorldChunkSettings setting) {
			return (this.type == WorldZoneTypes.Ground && (this.containAllCoords || this.coords.Count >= setting.worldGroundZoneMinSize));
		}
	}
	#endregion
}
