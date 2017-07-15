using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapThreading : MonoBehaviour {

	Queue<MapThreadInfo<WorldChunkData>> chunkDataThreadInQueue = new Queue<MapThreadInfo<WorldChunkData>> ();
	Queue<MapComputingThreadInfo<WorldChunkComputed>> chunkComputedThreadInQueue = new Queue<MapComputingThreadInfo<WorldChunkComputed>> ();
	Queue<MeshDataThreadInfo<MeshData>> chunkMeshThreadInQueue = new Queue<MeshDataThreadInfo<MeshData>> ();

	#region singleton
	public static MapThreading instance;
	void Awake () {
		if (MapThreading.instance == null) {
			DontDestroyOnLoad (this.gameObject);
			MapThreading.instance = this;
		} else {
			Destroy (this.gameObject);
		}
	}
	#endregion

	public void RequestWorldChunkData(WorldChunk chunk, WorldChunkSettings setting, Action<WorldChunkData> callback) {
		ThreadStart threadStart = delegate {
			// Loading 
			WorldChunkData chunkData = new WorldChunkData(chunk, setting);

			lock (chunkDataThreadInQueue) {
				chunkDataThreadInQueue.Enqueue(new MapThreadInfo<WorldChunkData>(callback, chunkData));
			}
			// End Loading..
		};
		new Thread (threadStart).Start ();
	}

	public void RequestWorldChunkComputed(WorldChunk chunk, WorldChunkSettings setting, Action<WorldChunkComputed> callback) {
		ThreadStart threadStart = delegate {
			// Loading 
			WorldChunkComputed chunkComputed = new WorldChunkComputed(chunk, setting);
			lock (chunkComputedThreadInQueue) {
				chunkComputedThreadInQueue.Enqueue(new MapComputingThreadInfo<WorldChunkComputed>(callback, chunkComputed));
			}
			// End Loading..
		};
		new Thread (threadStart).Start ();
	}

	public void RequestWorldChunkMeshData(WorldChunk chunk, WorldChunkSideBorders sideBorder, WorldChunkSettings setting, Action<MeshData> callback) {
		ThreadStart threadStart = delegate {
			// Loading 
			MeshData meshData = MeshGenerator.GenerateWorldChunkMesh(chunk, sideBorder, setting);
			lock (chunkMeshThreadInQueue) {
				chunkMeshThreadInQueue.Enqueue(new MeshDataThreadInfo<MeshData>(callback, meshData));
			}
			// End Loading..
		};
		new Thread (threadStart).Start ();
	}

	void Update() {
		if (chunkDataThreadInQueue.Count > 0) {
			for (int i = 0; i < chunkDataThreadInQueue.Count; i++) {
				MapThreadInfo<WorldChunkData> threadInfo = chunkDataThreadInQueue.Dequeue ();
				threadInfo.callback (threadInfo.parameter);
			}
		}
		if (chunkComputedThreadInQueue.Count > 0) {
			for (int i = 0; i < chunkComputedThreadInQueue.Count; i++) {
				MapComputingThreadInfo<WorldChunkComputed> threadInfo = chunkComputedThreadInQueue.Dequeue ();
				threadInfo.callback (threadInfo.parameter);
			}
		}
		if (chunkMeshThreadInQueue.Count > 0) {
			for (int i = 0; i < chunkMeshThreadInQueue.Count; i++) {
				MeshDataThreadInfo<MeshData> threadInfo = chunkMeshThreadInQueue.Dequeue ();
				threadInfo.callback (threadInfo.parameter);
			}
		}
	}

	struct MapThreadInfo<Time> {
		public readonly Action<Time> callback;
		public readonly Time parameter;

		public MapThreadInfo(Action<Time> callback, Time parameter) {
			this.callback = callback;
			this.parameter = parameter;
		}
	}
	struct MapComputingThreadInfo<Time> {
		public readonly Action<Time> callback;
		public readonly Time parameter;

		public MapComputingThreadInfo(Action<Time> callback, Time parameter) {
			this.callback = callback;
			this.parameter = parameter;
		}
	}
	struct MeshDataThreadInfo<Time> {
		public readonly Action<Time> callback;
		public readonly Time parameter;

		public MeshDataThreadInfo(Action<Time> callback, Time parameter) {
			this.callback = callback;
			this.parameter = parameter;
		}
	}
}
