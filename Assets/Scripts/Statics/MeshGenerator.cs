using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator {

	public static MeshData GenerateWorldChunkMesh(WorldChunk chunk, WorldChunkSideBorders borders, WorldChunkSettings setting) {
		int size = setting.scaledSize;
		int meshSize = size + 1; // One line on top/left to create the triangles between chunks
		int borderedSize = meshSize + 2; // One line on every direction to calculate normals of triangle on the border of mesh
		float offset = (meshSize - 1) / -2f;

		MeshData meshData = new MeshData (meshSize);
	
		// To handle normals, the mesh have to generate vertices for 1line on each 8chunks bordered
		int[,] vertexIndicesMap = CreateVertexIndicesMap (borderedSize);

		for (int y=0; y < borderedSize; y++) {
			for (int x = 0; x < borderedSize; x++) {
				
				Coord chunkCoord = new Coord (x-1, y-1, setting); // It's start with [-1;-1] 

				int vertexIndex = vertexIndicesMap [x, y];
				float height = chunk.GetHeightValue(chunkCoord, setting);

				Vector2 uv = new Vector2 ((x-1)/(float)(borderedSize-2), (y-1)/(float)(borderedSize-2));
				Vector3 vertexPosition = new Vector3 (
					offset + uv.x * meshSize,
					height, 
					offset + uv.y * meshSize
				);
				meshData.AddOrUpdateVertex(vertexPosition, uv, vertexIndex);

				if (x <= borderedSize-2 && y <= borderedSize-2) {
					int a = vertexIndicesMap[x, y];
					int b = vertexIndicesMap[x + 1, y];
					int c = vertexIndicesMap[x, y + 1];
					int d = vertexIndicesMap[x + 1, y + 1];

					// Clockwise
			//		meshData.AddTriangle (a, d, c);
			//		meshData.AddTriangle (d, a, b);
					// Counter Clockwise
					meshData.AddTriangle (c, d, a);
					meshData.AddTriangle (b, a, d);
				}
			}
		}

		return meshData;
	}

	public static int[,] CreateVertexIndicesMap(int borderedSize) {
		int vertexIndex = 0;
		int borderVertexIndex = -1;
		int[,] vertexIndicesMap = new int[borderedSize, borderedSize]; // good size ?

		for (int y = 0; y < borderedSize; y++) {
			for (int x = 0; x < borderedSize; x++) {
				// Is bordered
				bool isBordered = (x == 0 || y == 0 || x == borderedSize - 1 || y == borderedSize - 1);

				if (isBordered) {
					vertexIndicesMap [x, y] = borderVertexIndex;
					borderVertexIndex--;
				} else {
					//	int vertexIndex = y * meshSize + x;
					vertexIndicesMap [x, y] = vertexIndex;
					vertexIndex++;
				}
			}
		}
		return vertexIndicesMap;
	}

	// Just after loading the first time, of a chunk on top/right/topRight as been updated. Refresh it
	public static void UpdateChunkMesh(WorldChunk chunk, WorldChunkSettings setting) {
		chunk.chunkBorders.UpdateSideBorders (chunk, setting);

		int size = setting.scaledSize;
		int meshSize = size + 1; // One line on top/left to create the triangles between chunks
		int borderedSize = meshSize + 2; // One line on every direction to calculate normals of triangle on the border of mesh
		float offset = (meshSize - 1) / -2f;

		// @TODO2: Don't do it for ALL the chunk, but only for (not updated) borders coords
		int min_x = 0;
		int min_y = 0;
		int max_x = borderedSize;
		int max_y = borderedSize;
		int border_size = 1; // Has +1 for Top/Right (cause of triangle between chunks)

		// To handle normals, the mesh have to generate vertices for 1line on each 8chunks bordered
		int[,] vertexIndicesMap = CreateVertexIndicesMap (borderedSize);

		foreach (KeyValuePair<Coord, WorldChunk> keyValue in chunk.chunkBorders.sidesChunks) {
			Coord sideCoord = keyValue.Key;
			// Already done: skip
			if (chunk.chunkBorders.sidesChunksMeshUpdateDone.Contains(sideCoord)) {
				continue;
			}
			// Reset before
			min_x = min_y = 0;
			max_x = max_y = borderedSize;
			// Compute max/min border to check. Dont reload the whole chunk, only the border related to side chunk
			// Can be corners (so x and y will affect borders)
			if (sideCoord.x < chunk.coord.x) {
				min_x = 0;
				max_x = border_size;
			}
			if (sideCoord.x > chunk.coord.x) {
				min_x = borderedSize - (border_size + 1);
				max_x = borderedSize;
			}
			if (sideCoord.y < chunk.coord.y) {
				min_y = 0;
				max_y = border_size;
			}
			if (sideCoord.y > chunk.coord.y) {
				min_y = borderedSize - (border_size + 1);
				max_y = borderedSize;
			}
			// Update Vertexes On Range and add the coord as "Already updated the mesh for this side")
			MeshGenerator.UpdateVertexesOnRange(chunk, min_x, max_x, min_y, max_y, vertexIndicesMap, meshSize, borderedSize, offset, setting);
			chunk.chunkBorders.sidesChunksMeshUpdateDone.Add (keyValue.Key);
		}

		Mesh mesh = chunk.meshObject.GetComponent<MeshFilter> ().mesh;
		mesh.vertices = chunk.meshData.vertices;
		mesh.uv = chunk.meshData.uvs;

		// @TODO: can send min/max x/y to update only some normales
		mesh.normals = chunk.meshData.CalculateNormales (true);
	}

	static void UpdateVertexesOnRange(WorldChunk chunk, 
		int min_x, int max_x, int min_y, int max_y, 
		int[,] vertexIndicesMap, int meshSize, int borderedSize, float offset,
		WorldChunkSettings setting) {


		for (int y = min_y; y < max_y; y++) {
			for (int x = min_x; x < max_x; x++) {	

				Coord chunkCoord = new Coord (x - 1, y - 1, setting); // It's start with [-1;-1] 

				int vertexIndex = vertexIndicesMap [x, y];

				// @TODO We already know the concerned sideChunk (it's `keyValue.Value` on `UpdateChunkMesh` loop 
				// (can be pass throw GetHeightValue to avoid search it on the list)
				float height = chunk.GetHeightValue (chunkCoord, setting);

				Vector2 uv = new Vector2 ((x - 1) / (float)(borderedSize - 2), (y - 1) / (float)(borderedSize - 2));
				Vector3 vertexPosition = new Vector3 (
					                         offset + uv.x * meshSize,
					                         height, 
					                         offset + uv.y * meshSize
				                         );
				chunk.meshData.AddOrUpdateVertex (vertexPosition, uv, vertexIndex);	
			}
		}
	}

	public static float GetRealHeight(float noiseHeight, WorldZoneTypes zoneType, WorldChunkSettings setting) {
		float height;

		if (zoneType == WorldZoneTypes.Water) {
			float delta = (noiseHeight) * ( 1 / (setting.water));
			return Mathf.Lerp (0f, .3f, delta); //.29f; 
		} else if (zoneType == WorldZoneTypes.Ground) {
			float delta = (noiseHeight - setting.water) * ( 1 / (setting.mountain-setting.water));
			return Mathf.Lerp (.3f, .5f, delta);
		}
		else /*if (zoneType == WorldZoneTypes.Mountain) */{
			float delta = (noiseHeight - setting.mountain) * ( 1 / (1-setting.mountain));
			return Mathf.Lerp (.5f, 1.5f, delta);
		}
	}
}

public class MeshData {
	public Vector3[] vertices; // Can be updated if we receive other chunks
	int[] triangles;
	public Vector2[] uvs;

	Vector3[] borderVertices;
	int[] borderTriangles;

	int triangleIndex = 0;
	int borderTriangleIndex;

	public MeshData(int size) {
		this.vertices = new Vector3[size * size];
		this.uvs = new Vector2[size * size];
		this.triangles = new int[(size - 1) * (size - 1) * 6];

		this.borderVertices = new Vector3[size * 4 + 4];
		this.borderTriangles = new int[24*size];
	}

	public void AddOrUpdateVertex(Vector3 vertexPosition, Vector2 uv, int vertexIndex) {
		if (vertexIndex < 0) {
			this.borderVertices [-vertexIndex - 1] = vertexPosition;
		} else {
			this.vertices [vertexIndex] = vertexPosition;
			this.uvs [vertexIndex] = uv;
		}
	}

	public void AddTriangle(int a, int b, int c) {
		if (a < 0 || b < 0 || c < 0) {
			this.borderTriangles [borderTriangleIndex] = a;
			this.borderTriangles [borderTriangleIndex + 1] = b;
			this.borderTriangles [borderTriangleIndex + 2] = c;
			this.borderTriangleIndex += 3;
		} else {
			this.triangles [triangleIndex] = a;
			this.triangles [triangleIndex + 1] = b;
			this.triangles [triangleIndex + 2] = c;
			this.triangleIndex += 3;
		}
	}

	public Vector3[] CalculateNormales(bool isUpdate) {
		Vector3[] vertexNormals = new Vector3[vertices.Length];

		int triangleCount = this.triangles.Length / 3;
		for (int i = 0; i < triangleCount; i++) {
			int normalTriangleIndex = i * 3;
			int vertexIdxA = triangles [normalTriangleIndex];
			int vertexIdxB = triangles [normalTriangleIndex + 1];
			int vertexIdxC = triangles [normalTriangleIndex + 2];

			Vector3 triangleNormal = this.SurfaceNormalFromIdxs (vertexIdxA, vertexIdxB, vertexIdxC);
			vertexNormals [vertexIdxA] += triangleNormal;
			vertexNormals [vertexIdxB] += triangleNormal;
			vertexNormals [vertexIdxC] += triangleNormal;
		}


		int borderTriangleCount = this.borderTriangles.Length / 3;
		for (int i = 0; i < borderTriangleCount; i++) {
			int normalTriangleIndex = i * 3;
			int vertexIdxA = this.borderTriangles [normalTriangleIndex];
			int vertexIdxB = this.borderTriangles [normalTriangleIndex + 1];
			int vertexIdxC = this.borderTriangles [normalTriangleIndex + 2];

			Vector3 triangleNormal = this.SurfaceNormalFromIdxs (vertexIdxA, vertexIdxB, vertexIdxC);
			if (vertexIdxA >= 0) {
				vertexNormals [vertexIdxA] += triangleNormal;
			}
			if (vertexIdxB >= 0) {
				vertexNormals [vertexIdxB] += triangleNormal;
			}
			if (vertexIdxC >= 0) {
				vertexNormals [vertexIdxC] += triangleNormal;
			}
		}

		for (int i = 0; i < vertexNormals.Length; i++) {
			vertexNormals [i].Normalize();
		}
		return vertexNormals;
	}

	Vector3 SurfaceNormalFromIdxs(int idxA, int idxB, int idxC) {
		Vector3 pointA = (idxA < 0) ? this.borderVertices[-idxA-1] : this.vertices [idxA];
		Vector3 pointB = (idxB < 0) ? this.borderVertices[-idxB-1] : this.vertices [idxB];
		Vector3 pointC = (idxC < 0) ? this.borderVertices[-idxC-1] : this.vertices [idxC];

		Vector3 sideAB = pointB - pointA;
		Vector3 sideAC = pointC - pointA;
		return Vector3.Cross (sideAB, sideAC).normalized;
	}

	public Mesh CreateMesh() {
		Mesh mesh = new Mesh ();
		mesh.vertices = this.vertices;
		mesh.triangles = this.triangles;
		mesh.uv = this.uvs;
		mesh.normals = this.CalculateNormales (false);
		return mesh;
	}
}
