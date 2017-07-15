using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TextureGenerator {
	public static Texture2D TextureFromColorMap(Color[] colorMap, int chunkSize) {
		Texture2D texture = new Texture2D (chunkSize, chunkSize);
		texture.filterMode = FilterMode.Point;
		texture.wrapMode = TextureWrapMode.Clamp;
		texture.SetPixels (colorMap);
		texture.Apply ();
		return texture;
	}
	public static Texture2D TextureFromCoords(WorldChunkSettings setting) {
		Color[] colourMap = new Color[setting.scaledSize*setting.scaledSize];

		for (int y = 0; y < setting.scaledSize; y++) {
			for (int x = 0; x < setting.scaledSize; x++) {
				Coord c = new Coord (x, y, setting);
				if (x == 0 || y == 0 || x == setting.scaledSize - 1 || y == setting.scaledSize - 1) {
					colourMap [y * setting.scaledSize + x] = Color.white;
				}
				else if ((x == 1 || y == 1) && x != 0 && y != 0) {
					if (x == 1 && y == 1) { // Height Unity World 3D - Null in 2D
						colourMap [c.idx] = Color.green; 
					} else if (x == 1) { // Unity World 3D is Z:blue
						colourMap [c.idx] = Color.blue * (y / (float)setting.scaledSize);
					} else if (y == 1) { // Unity World 3D is X:red
						colourMap [c.idx] = Color.red * (x / (float)setting.scaledSize);
					} 
				}
				else {
					colourMap [y * setting.scaledSize + x] = Color.gray;
				}
			}
		}
		return TextureFromColorMap (colourMap, setting.scaledSize);
	}
	public static Texture2D TextureFromHeightMap(WorldChunk chunk, WorldChunkSettings setting, bool useRegionMap) {
		//int lenght = chunkSize * chunkSize;
		Color[] colourMap = new Color[setting.scaledSize*setting.scaledSize];

		for (int y = 0; y < setting.scaledSize; y++) {
			for (int x = 0; x < setting.scaledSize; x++) {
				Coord c = new Coord (x, y, setting);
				float height;
				if (useRegionMap) {
					height = chunk.chunkData.GetRegionValue(c);
				} else {
					height = chunk.chunkData.GetHeightValue(c);
				}
				colourMap [c.idx] = Color.Lerp (Color.black, Color.white, height);
			}
		}
		return TextureFromColorMap (colourMap, setting.scaledSize);
	}

	public static Texture2D WorldGroundColoredTexture(WorldChunk chunk, WorldChunkSettings setting) {
		Color blue = new Color (0f, 189f/255f, 1f);
		Color yellow = new Color (1f, 245f/255f, 0f);
		Color red = new Color (1f, 57f/255f, 0f);
		Color black = new Color (0f, 0f, 0f);

		float water = setting.water; // min
		float mountain = setting.mountain; // max
		Color[] colourMap = new Color[setting.scaledSize*setting.scaledSize];

		for (int y = 0; y < setting.scaledSize; y++) {
			for (int x = 0; x < setting.scaledSize; x++) {
				Coord c = new Coord (x, y, setting);
				float height = chunk.chunkData.GetHeightValue (c);
				// idx: y * size + x
				if (height < water) {
					colourMap [c.idx] = blue;
				} else if (height > mountain) {
					colourMap [c.idx] = black;
				} else {
					float delta = (height - water) * ( 1 / (mountain-water));
					colourMap [c.idx] = Color.Lerp(yellow, red, delta);
				}
			}
		}
		return TextureGenerator.TextureFromColorMap (colourMap, setting.scaledSize);
	}
	public static Texture2D WorldColoredTexture(WorldChunk chunk, WorldChunkSettings setting) {
		Color blue = new Color (0f, 189f/255f, 1f);
		Color yellow = new Color (1f, 245f/255f, 0f);
		Color red = new Color (1f, 57f/255f, 0f);
		Color black = new Color (0f, 0f, 0f);

		float water = setting.water; // min
		float mountain = setting.mountain; // max
		Color[] colourMap = new Color[setting.scaledSize*setting.scaledSize];

		for (int y = 0; y < setting.scaledSize; y++) {
			for (int x = 0; x < setting.scaledSize; x++) {
				Coord c = new Coord (x, y, setting);
				float height = chunk.chunkData.GetHeightValue (c);
				// idx: y * size + x
				if (height < water) {
					colourMap [c.idx] = blue;
				} else if (height > mountain) {
					colourMap [c.idx] = black;
				} else {
					float delta = (height - water) * ( 1 / (mountain-water));
					colourMap [c.idx] = Color.Lerp(yellow, red, delta) * chunk.chunkData.GetRegionValue (c);
				}
			}
		}
		return TextureGenerator.TextureFromColorMap (colourMap, setting.scaledSize);
	}

	public static Texture2D WorldChunkStatesTexture(WorldChunk chunk, WorldChunkSettings setting) {
		Color loaded = new Color (.2f, .2f, 1f);
		Color computed = new Color (.2f, 1f, .2f);
		Color merging = new Color (1f, .2f, .2f);
		Color merged = new Color (1f, .9f, .2f);

		Color groundMin = new Color (.25f, .25f, .25f);
		Color groundMax = new Color (.75f, .75f, .75f);

		Color[] colourMap = new Color[setting.scaledSize*setting.scaledSize];
		for (int y = 0; y < setting.scaledSize; y++) {
			for (int x = 0; x < setting.scaledSize; x++) {
				Coord c = new Coord (x, y, setting);
				float height = chunk.chunkData.GetHeightValue (c);
				if (chunk.state == ChunkStates.Loaded) {
					colourMap [y * setting.scaledSize + x] = Color.Lerp (Color.black, Color.white, height) *  loaded;
				} else if (chunk.state >= ChunkStates.Computed) {
					// idx: y * size + x
					Color state = computed;
					if (chunk.state == ChunkStates.Computed && chunk.requireState > ChunkStates.Computed) {
						state = merging;
					} else if (chunk.state >= ChunkStates.Merged) {
						state = merged;
					}

					if (height < setting.water) {
						colourMap [y * setting.scaledSize + x] = Color.white * state;
					} else if (height > setting.mountain) {
						colourMap [y * setting.scaledSize + x] = Color.black * state;
					} else {
						float delta = (height - setting.water) * ( 1 / (setting.mountain-setting.water));
						colourMap [y * setting.scaledSize + x] = Color.Lerp(groundMax, groundMin, delta) * state;
					}
				} 
			}
		}
		return TextureGenerator.TextureFromColorMap (colourMap, setting.scaledSize);	
	}

	public static Texture2D WorldChunksZoneStatesTexture(WorldChunk chunk, WorldChunkSettings setting) {
		Color blue = new Color (0f, 189f/255f, 1f);
		Color yellow = new Color (1f, 245f/255f, 0f);
		Color black = new Color (.2f, .2f, .2f);

		Color loaded = new Color (.2f, .2f, 1f);
		Color computed = new Color (.2f, 1f, .2f);
		Color merging = new Color (1f, .2f, .2f);
		Color merged = new Color (1f, .9f, .2f);

		Color groundMin = new Color (.25f, .25f, .25f);
		Color groundMax = new Color (.75f, .75f, .75f);

		Color[] colourMap = new Color[setting.scaledSize * setting.scaledSize];
		bool[] isOnZone = new bool[setting.scaledSize * setting.scaledSize];

		for (int idx = 0; idx < chunk.worldZonesRefs.Count; idx++) {
			// Only display Completed
			//if (chunk.worldZonesRefs [idx].isCompleted) {
			// Be sure this Wolrd zone contain this chunk
			if (chunk.worldZonesRefs [idx].chunkZones.ContainsKey (chunk.coord)) {
				for (int chunk_zone_idx = 0; chunk_zone_idx < chunk.worldZonesRefs [idx].chunkZones[chunk.coord].Count; chunk_zone_idx++) {
					WorldChunkComputed.WorldChunkZone zone = chunk.worldZonesRefs [idx].chunkZones[chunk.coord] [chunk_zone_idx];
					foreach (Coord c in zone.coords) {
						
						isOnZone [c.y * setting.scaledSize + c.x] = true;

						if (zone.type == WorldZoneTypes.Ground) {
							colourMap [c.y * setting.scaledSize + c.x] = yellow;
						} else {
							colourMap [c.y * setting.scaledSize + c.x] = (zone.type == WorldZoneTypes.Water) ? blue : black;
						}
						
						if (zone.worldZoneRef.requireZoneState == WorldZoneStates.Created) {
							colourMap [c.y * setting.scaledSize + c.x] *= computed;
						}
						else if (zone.worldZoneRef.requireZoneState == WorldZoneStates.Merged && zone.worldZoneRef.state == WorldZoneStates.Created) {
							colourMap [c.y * setting.scaledSize + c.x] *= merging;
						}
						else if (zone.worldZoneRef.state >= WorldZoneStates.Merged) {
							colourMap [c.y * setting.scaledSize + c.x] *= merged;
						}
					}
				}
			}
			//}
		}
		for (int y = 0; y < setting.scaledSize; y++) {
			for (int x = 0; x < setting.scaledSize; x++) {
				if (!isOnZone [y * setting.scaledSize + x]) {
					Coord c = new Coord (x, y);
					float height = chunk.chunkData.GetHeightValue (c, setting);
					float delta = (height - setting.water) * (1 / (setting.mountain - setting.water));
					colourMap [y * setting.scaledSize + x] = Color.Lerp (groundMax, groundMin, delta);
				}
			}
		}
		return TextureGenerator.TextureFromColorMap (colourMap, setting.scaledSize);
	}

	public static Texture2D WorldZonesMergingTexture(WorldChunk chunk, WorldChunkSettings setting) {
		Color groundMin = new Color (.25f, .25f, .25f);
		Color groundMax = new Color (.75f, .75f, .75f);

		Color[] colourMap = new Color[setting.scaledSize * setting.scaledSize];
		bool[] isOnZone = new bool[setting.scaledSize * setting.scaledSize];

		for (int idx = 0; idx < chunk.worldZonesRefs.Count; idx++) {
			// Only display Completed
			//if (chunk.worldZonesRefs [idx].isCompleted) {
				// Be sure this Wolrd zone contain this chunk
			if (chunk.worldZonesRefs [idx].chunkZones.ContainsKey (chunk.coord)) {
				for (int chunk_zone_idx = 0; chunk_zone_idx < chunk.worldZonesRefs [idx].chunkZones[chunk.coord].Count; chunk_zone_idx++) {
					WorldChunkComputed.WorldChunkZone zone = chunk.worldZonesRefs [idx].chunkZones[chunk.coord] [chunk_zone_idx];
					foreach (Coord c in zone.coords) {
						if (zone.type == WorldZoneTypes.Water || zone.type == WorldZoneTypes.Mountain) {
							isOnZone [c.y * setting.scaledSize + c.x] = true;
							colourMap [c.y * setting.scaledSize + c.x] = zone.worldZoneRef.randomColor;
						}
					}
				}
			}
			//}
		}
		for (int y = 0; y < setting.scaledSize; y++) {
			for (int x = 0; x < setting.scaledSize; x++) {
				if (!isOnZone [y * setting.scaledSize + x]) {
					Coord c = new Coord (x, y);
					float height = chunk.chunkData.GetHeightValue (c, setting);
					float delta = (height - setting.water) * (1 / (setting.mountain - setting.water));
					colourMap [y * setting.scaledSize + x] = Color.Lerp (groundMax, groundMin, delta);
				}
			}
		}
		return TextureGenerator.TextureFromColorMap (colourMap, setting.scaledSize);
	}


	public static Texture2D WorldMergingTexture(WorldChunk chunk, WorldChunkSettings setting) {
		Color blue = new Color (0f, 189f/255f, 1f);
		Color yellow = new Color (1f, 245f/255f, 0f);
		Color red = new Color (1f, 57f/255f, 0f);
		Color black = new Color (0f, 0f, 0f);
		Color groundMin = new Color (.25f, .25f, .25f);
		Color groundMax = new Color (.75f, .75f, .75f);

		float water = setting.water; // min
		float mountain = setting.mountain; // max
		Color[] colourMap = new Color[setting.scaledSize*setting.scaledSize];
		bool[] isOnZone = new bool[setting.scaledSize * setting.scaledSize];

		for (int idx = 0; idx < chunk.worldZonesRefs.Count; idx++) {
			if (chunk.worldZonesRefs [idx].chunkZones.ContainsKey (chunk.coord)) {
				for (int chunk_zone_idx = 0; chunk_zone_idx < chunk.worldZonesRefs [idx].chunkZones[chunk.coord].Count; chunk_zone_idx++) {
					WorldChunkComputed.WorldChunkZone zone = chunk.worldZonesRefs [idx].chunkZones[chunk.coord] [chunk_zone_idx];

					if (!zone.worldZoneRef.isMainGround && zone.worldZoneRef.state >= WorldZoneStates.Merged) {

						if (zone.containAllCoords) {
							for (int y = 0; y < setting.scaledSize; y++) {
								for (int x = 0; x < setting.scaledSize; x++) {
									Coord c = new Coord (x, y, setting);
									isOnZone [c.y * setting.scaledSize + c.x] = true;
									if (zone.type == WorldZoneTypes.Ground) {
										float height = chunk.chunkData.GetHeightValue (c, setting);
										float delta = (height - water) * (1 / (mountain - water));
										colourMap [c.y * setting.scaledSize + c.x] = Color.Lerp (yellow, red, delta) / 1.5f;
									} else {
										colourMap [c.y * setting.scaledSize + c.x] = (zone.type == WorldZoneTypes.Water) ? blue : black;
									}
								}
							}
						} else {
							foreach (Coord c in zone.coords) {
								
								isOnZone [c.y * setting.scaledSize + c.x] = true;

								if (zone.type == WorldZoneTypes.Ground) {
									float height = chunk.chunkData.GetHeightValue (c, setting);
									float delta = (height - water) * (1 / (mountain - water));
									colourMap [c.y * setting.scaledSize + c.x] = Color.Lerp (yellow, red, delta) / 1.5f;
								} else {
									colourMap [c.y * setting.scaledSize + c.x] = (zone.type == WorldZoneTypes.Water) ? blue : black;
								}
							}
						}
					}
				}
			}
		}

		for (int y = 0; y < setting.scaledSize; y++) {
			for (int x = 0; x < setting.scaledSize; x++) {
				Coord c = new Coord (x, y, setting);


				// DEV unicolor
		//	colourMap [c.idx] = blue;
		//	continue;
				// end DEV unicolor


				if (isOnZone [c.y * setting.scaledSize + c.x]) {
					continue;
				}
				float height = chunk.chunkData.GetHeightValue (c);
				float delta = (height - water) * ( 1 / (mountain-water));
				
				colourMap [c.idx] = (chunk.state >= ChunkStates.Merged) 
					? Color.Lerp(yellow, red, delta)
					: Color.Lerp(groundMax, groundMin, delta);
			}
		}
		return TextureGenerator.TextureFromColorMap (colourMap, setting.scaledSize);
	}


}
