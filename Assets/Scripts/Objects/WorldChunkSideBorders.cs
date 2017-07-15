using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldChunkSideBorders {

	public Dictionary<Coord, WorldChunk> sidesChunks = new Dictionary <Coord, WorldChunk>();
	public List<Coord> sidesChunksMeshUpdateDone = new List <Coord>();

	public WorldChunkSideBorders(WorldChunk chunk, WorldChunkSettings setting) {
		this.UpdateSideBorders (chunk, setting);
	}

	public void UpdateSideBorders(WorldChunk chunk, WorldChunkSettings setting) {
		for (int y = chunk.coord.y - 1; y <= chunk.coord.y + 1; y++) {
			for (int x = chunk.coord.x - 1; x <= chunk.coord.x + 1; x++) {
				if (x == chunk.coord.x && y == chunk.coord.y) {
					continue;
				}
				Coord c = new Coord (x, y, setting);
				// Already found, dont copy again an again
				if (!this.sidesChunks.ContainsKey (c)) {
					if (MapEndless.instance.worldChunks.ContainsKey (c)) {
						WorldChunk sideChunk = MapEndless.instance.worldChunks [c];
						if (sideChunk.state >= ChunkStates.Merged) {
							this.sidesChunks [c] = sideChunk;
						}
					}
				}
			}	
		}

		// DEV
		if (chunk.meshObject != null) {
			// Do it after all
			string dev = "";
			for (int y = chunk.coord.y - 1; y <= chunk.coord.y + 1; y++) {
				for (int x = chunk.coord.x - 1; x <= chunk.coord.x + 1; x++) {
					if (x == chunk.coord.x && y == chunk.coord.y) {
						continue;
					}
					Coord c = new Coord (x, y, setting);
					if (this.sidesChunks.ContainsKey (c)) {
						dev += " " + c;
					}
				}
			}
			chunk.meshObject.transform.name = chunk.coord + ((dev != "") ? (" is normalized with " + dev) : "");
		}
	}
}