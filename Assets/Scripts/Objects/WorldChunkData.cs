using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Object to store the noise data during 
 */
public class WorldChunkData  {
	//public Coord coord;
	public readonly float [] heightMap;
	public readonly float [] regionMap;

	// setting already retrieved when new Chunk are created
	public WorldChunkData(WorldChunk chunk, WorldChunkSettings setting) {
		DevNoiseType devNoiseType = MapEndless.instance.devNoiseType;

		// Get Height Map
		FastNoise fastNoiseGround = setting.fastNoiseGround.fastNoise;
		FastNoise fastNoiseRegion = setting.fastNoiseRegion.fastNoise;
		int length = setting.scaledSize * setting.scaledSize;
		int chunkOffsetX = chunk.coord.x * setting.scaledSize - (setting.scaledSize / 2);
		int chunkOffsetY = chunk.coord.y * setting.scaledSize - (setting.scaledSize / 2);

		this.heightMap = new float[length];
		this.regionMap = new float[length];
		for (int y = 0; y < setting.scaledSize ; y++) {
			for (int x = 0; x < setting.scaledSize; x++) {
				// Test and return only a mountain on [1;1] to [9;1]
				Coord c = new Coord(x,y, setting);

				if (devNoiseType != DevNoiseType.NoDev) {
					// Use dev noise
					this.heightMap [c.idx] = this.getZonesHeightTest (new Coord(c.x + chunkOffsetX, c.y + chunkOffsetY), devNoiseType);
				} else {
					// Use fastNoiseGround
					this.heightMap [c.idx] = (fastNoiseGround.GetNoise (c.x + chunkOffsetX, c.y + chunkOffsetY) + 1f) / 2f; 
				}
				this.regionMap [c.idx] = (fastNoiseRegion.GetNoise (c.x + chunkOffsetX,  c.y + chunkOffsetY) + 1f) / 2f; 
			}
		}
	}


	// DEV [chunk: 20x20]
	float getZonesHeightTest(Coord c, DevNoiseType devNoiseType) {
		if (devNoiseType == DevNoiseType.Empty) {
			return 0.5f;
		}
		if (devNoiseType == DevNoiseType.SmallZoneNoBorder) {
			if ((c.y >= 0 && c.y <= 2 && c.x >= 0 && c.x <= 2)) {
				return 1f;
			} 
			return 0.5f;
		}
		if (devNoiseType == DevNoiseType.OneSmallLine) {
			if ((c.y > 0 && c.y < 2 && c.x > 7 && c.x < 13)) {
				return 1f;
			} 
			return 0.5f;
		} else if (devNoiseType == DevNoiseType.TwoSmallLines) {
			if ((c.y > 0 && c.y < 2 && c.x > 7 && c.x < 13) || (c.y > 6 && c.y < 8 && c.x > 7 && c.x < 13)) {
				return 1f;
			} 
			return 0.5f;
		} else if (devNoiseType == DevNoiseType.LongHorizontalBar) {
			if ((c.x > -5 && c.x < 35 && c.y > -5 && c.y < 5)) {
				return 1f;
			} 
			return .5f;
		} else if (devNoiseType == DevNoiseType.LongHorizontalBarWithStop) {
			if ((c.x > 5 && c.x < 35 && c.y > -5 && c.y < 5) || (c.x > 37 && c.x < 65 && c.y > -5 && c.y < 5)) {
				return 1f;
			} 
			return .5f;
		} else if (devNoiseType == DevNoiseType.LongVerticalBar) {
			if ((c.x > -5 && c.x < 5 && c.y > -35 && c.y < 35)) {
				return 1f;
			} 
			return .5f;
		} else if (devNoiseType == DevNoiseType.L) {
			if ((c.x > -5 && c.x < 5 && c.y > -5 && c.y < 35) || (c.x > -5 && c.x < 35 && c.y > -5 && c.y < 5)) {
				return 1f;
			} 
			return .5f;
		} else if (devNoiseType == DevNoiseType.SmallL) {
			if ((c.x > -5 && c.x < 5 && c.y > -5 && c.y < 15) || (c.x > -5 && c.x < 15 && c.y > -5 && c.y < 5)) {
				return 1f;
			} 
			return .5f;
		} else if (devNoiseType == DevNoiseType.T) {
			if ((c.x > -15 && c.x < 15 && c.y > 15 && c.y < 25) || (c.x > -5 && c.x < 5 && c.y > -5 && c.y < 25)) {
				return 1f;
			} 
			return .5f;
		} else if (devNoiseType == DevNoiseType.ReversedT) {
			if ((c.x > -35 && c.x < 35 && c.y > -5 && c.y < 5) || (c.x > -5 && c.x < 5 && c.y > -5 && c.y < 45)) {
				return 1f;
			} 
			return .5f;
		} else if (devNoiseType == DevNoiseType.SmallCross) {
			if ((c.x > -25 && c.x < 25 && c.y > -5 && c.y < 5) || (c.x > -5 && c.x < 5 && c.y > -25 && c.y < 25)) {
				return 1f;
			} 
			return .5f;
		} else if (devNoiseType == DevNoiseType.LargeCross) {
			if ((c.x > -55 && c.x < 55 && c.y > -5 && c.y < 5) || (c.x > -5 && c.x < 5 && c.y > -55 && c.y < 55)) {
				return 1f;
			} 
			return .5f;
		} else if (devNoiseType == DevNoiseType.TwoLongParallelLines) {
			if ((c.y > 0 && c.y < 45 && c.x > 0 && c.x < 5) || (c.y > 20 && c.y < 60 && c.x > -6 && c.x < -1)) {
				return 1f;
			} 
			return 0.5f;
		} else if (devNoiseType == DevNoiseType.TwoLongParallelLinesWithOneBigger) {
			if ((c.y > 0 && c.y < 45 && c.x > 0 && c.x < 5) || (c.y > 20 && c.y < 45 && c.x > -6 && c.x < -1)) {
				return 1f;
			} 
			return 0.5f;
		} else if (devNoiseType == DevNoiseType.FakeZone) {
			if ((c.y > 0 && c.y < 10 && c.x > 0 && c.x < 5) || (c.y > 12 && c.y < 15 && c.x > 0 && c.x < 4)) {
				return 1f;
			} 
			return 0.5f;
		} else if (devNoiseType == DevNoiseType.DoubleFakeZone) {
			if ((c.y > 0 && c.y < 10 && c.x > -15 && c.x < -5) || (c.y > 0 && c.y < 10 && c.x > 5 && c.x < 10)) {
				return 1f;
			} 
			return 0.5f;
		} else if (devNoiseType == DevNoiseType.FakeZoneAndEmpty) {
			if (c.y > 0 && c.y < 10 && c.x > 0 && c.x < 5) {
				return 1f;
			} 
			return 0.5f;
		} else if (devNoiseType == DevNoiseType.FakeZoneOnTop) {
			if (c.y > 10 && c.y < 15 && c.x > 0 && c.x < 5) {
				return 1f;
			} 
			return 0.5f;
		} else if (devNoiseType == DevNoiseType.SmallGroundMergeMainGround) {
			if ((c.y > 5 && c.y < 15 && c.x > 0 && c.x < 5) || (c.y > 5 && c.y < 15 && c.x > -7 && c.x < -3) || (c.y > 5 && c.y < 7 && c.x > -7 && c.x < 5)) {
				return 1f;
			} 
			return 0.5f;
		} else if (devNoiseType == DevNoiseType.FakeZoneU) {
			if ((c.y > 0 && c.y < 10 && c.x > 0 && c.x < 5) || (c.y > 0 && c.y < 10 && c.x > -7 && c.x < -3) || (c.y > 0 && c.y < 2 && c.x > -7 && c.x < 5)) {
				return 1f;
			} 
			return 0.5f;
		} else if (devNoiseType == DevNoiseType.FakeCross) {
			if ((c.y > -8 && c.y < 8 && c.x > -11 && c.x < 10) || (c.y > -11 && c.y < 10 && c.x > -8 && c.x < 8)) {
				return 1f;
			} 
			return 0.5f;
		} else if (devNoiseType == DevNoiseType.FakeCornerLTopRight) {
			if ((c.y > 0 && c.y < 20 && c.x > 9 && c.x < 16) || (c.y > 9 && c.y < 16 && c.x > 0 && c.x < 20)) {
				return 1f;
			} 
			return 0.5f;
		} else if (devNoiseType == DevNoiseType.Hook) {
			if ((c.y > 0 && c.y < 15 && c.x > -8 && c.x < -2) || (c.y > 7 && c.y < 15 && c.x > 2 && c.x < 8) || (c.y > 12 && c.y < 15 && c.x > -8 && c.x < 8)) {
				return 1f;
			} 
			return 0.5f;
		} else if (devNoiseType == DevNoiseType.PreCreatedHook) {
			if ((c.y > 8 && c.y < 12 && c.x > -5 && c.x < -3) || (c.y > 20 && c.y < 35 && c.x > -8 && c.x < -2) || (c.y > 27 && c.y < 35 && c.x > 2 && c.x < 8) || (c.y > 32 && c.y < 35 && c.x > -8 && c.x < 8)) {
				return 1f;
			} 
			return 0.5f;
		} else if (devNoiseType == DevNoiseType.N) {
			if ((c.y > 0 && c.y < 15 && c.x > -8 && c.x < -5) || (c.y > 0 && c.y < 15 && c.x > -2 && c.x < 2) || (c.y > 0 && c.y < 15 && c.x > 5 && c.x < 8)
			    || (c.y > 13 && c.y < 15 && c.x > -8 && c.x < 2) || (c.y > 0 && c.y < 2 && c.x > -2 && c.x < 8)) {
				return 1f;
			} 
			return 0.5f;
		} else if (devNoiseType == DevNoiseType.LongHook) {
			if ((c.y > 0 && c.y < 35 && c.x > -8 && c.x < -2) || (c.y > 7 && c.y < 35 && c.x > 2 && c.x < 8) || (c.y > 32 && c.y < 35 && c.x > -8 && c.x < 8)) {
				return 1f;
			} 
			return 0.5f;
		} else if (devNoiseType == DevNoiseType.Corner) {
			if ((c.y > -15 && c.y < -5 && c.x > 5 && c.x < 15)) {
				return 1f;
			} 
			return 0.5f;
		} else if (devNoiseType == DevNoiseType.Corners) {
			if ((c.y > -15 && c.y < -5 && c.x > 5 && c.x < 15) || (c.y > 5 && c.y < 15 && c.x > 5 && c.x < 15) || (c.y > -15 && c.y < -5 && c.x > -15 && c.x < -5) || (c.y > 5 && c.y < 15 && c.x > -15 && c.x < -5)) {
				return 1f;
			} 
			return 0.5f;
		} else if (devNoiseType == DevNoiseType.CornerSquare) {
			if ((c.y >= 0 && c.y <= 20 && (c.x == 0 || c.x == 20)) || (c.x >= 0 && c.x <= 20 && (c.y == 0 || c.y == 20))) {
				return 1f;
			} 
			return 0.5f;
		} else if (devNoiseType == DevNoiseType.Big) {
			if ((c.y > -15 && c.y < 50 && c.x > 0 && c.x < 65)) {
				return 1f;
			} 
			return 0.5f;
		} else if (devNoiseType == DevNoiseType.MainGroundCorner) {
			if ((c.y > -5 && c.y < 10 && c.x > -19 && c.x < -14) || (c.y > -5 && c.y < 5 && c.x > -15 && c.x < 5)
			    || (c.y > -5 && c.y < 19 && c.x > -8 && c.x < 5) || (c.y > 14 && c.y < 19 && c.x > -11 && c.x < 0)) {
				return 1f;
			} 
			return 0.5f;
		} else if (devNoiseType == DevNoiseType.FakeZoneCrossCorner) {
			if ((c.x == -10 && c.y >= 9 && c.y <= 11) || (c.y == 10 && c.x >= -11 && c.x <= -9)) {
				return 1f;
			} 
			return 0.5f;
		} else if (devNoiseType == DevNoiseType.CaseA) {
			if ((c.y > 0 && c.y < 40 && c.x > -10 && c.x < 0) || (c.y > 20 && c.y < 40 && c.x > 3 && c.x < 6)) {
				return 1f;
			} 
			return 0.5f;
		} 


		return 0f;
	}


	// Get Height map as saved:
	// Size: 50x50  - Scale: 10 -> coord from [0;0] to [5;5]
	public float GetHeightValue(Coord coord, WorldChunkSettings setting) {
		return this.GetHeightValue (coord, coord.y * setting.scaledSize + coord.x);
	}
	public float GetRegionValue(Coord coord, WorldChunkSettings setting) {
		return this.GetRegionValue (coord, coord.y * setting.scaledSize + coord.x);
	}
	public float GetHeightValue(Coord coord, int idx) {
		return this.heightMap [idx];
	}
	public float GetRegionValue(Coord coord, int idx) {
		return this.regionMap [idx];
	}
	// When Coord.idx already have been computed, don't recalculate it
	public float GetHeightValue(Coord coord) {
		return this.heightMap [coord.idx];
	}
	public float GetRegionValue(Coord coord) {
		return this.regionMap [coord.idx];
	}

	// Get Scaled data (real in game coord)
	// Size: 50x50  - Scale: 10 -> coord from [0;0] to [50;50]
	public float GetHeightScaledValue(Coord coord, WorldChunkSettings setting) {
		//int sizeScaled = setting.size / setting.scale;
		Coord scaledCoord = new Coord (
			                    coord.x / setting.scaledSize,
			                    coord.y / setting.scaledSize,
			                    setting);
		return this.GetHeightValue(scaledCoord);
	}
	public float GetRegionScaledValue(Coord coord, WorldChunkSettings setting) {
		Coord scaledCoord = new Coord (
			coord.x / setting.scaledSize,
			coord.y / setting.scaledSize,
			setting);
		return this.GetRegionValue(scaledCoord);
	}

	// This function have to be here because return is based on HeightMap ! And have NO considaration on computed zones
	// Will be called to create a new zone from this coord
	public WorldZoneTypes GetZoneType(Coord coord, WorldChunkSettings setting) {
		float heightValue = this.GetHeightValue (coord, setting);
		if (heightValue < setting.water) {
			return WorldZoneTypes.Water;
		} else if (heightValue > setting.mountain) {
			return WorldZoneTypes.Mountain;
		}
		return WorldZoneTypes.Ground;
	}



}