using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Viewer : MonoBehaviour {
	[Range(1f, 1000f)]
	public float speed;
	[Range(1f, 1000f)]
	public float speedUI;
	[Range(.01f, 10f)]
	public float speedScaleUI;
	[Range(1f, 1000f)]
	public float viewerThreshold;
	float sqrtViewerThreshold;
	[Range(.01f, 10f)]
	public float viewerThresholdScale;

	[Range(1f, 1000f)]
	public float boundSize;
	[Range(0.1f, 1000f)]
	public float scale;
	[Range(0.1f, 50f)]
	public float minScale;
	[Range(0.1f, 1000f)]
	public float maxScale;

	// References objects
	public Transform cameraTransform;
	public Transform cubeTransform;

	public Bounds bound;
	Vector3 last_viewer_position;
	float last_viewer_scale;

	#region singleton
	public static Viewer instance;
	void Awake () {
		if (Viewer.instance == null) {
			Viewer.instance = this;
			/* OnAwake */
			OnAwake ();
		} else {
			Destroy (this.gameObject);
		}
	}
	#endregion

	#region init
	void OnAwake () {
		RefreshSettings ();
		cameraTransform.localPosition = new Vector3 (0f, boundSize * scale * 0.8f, -boundSize * scale * 0.4f);
	}
	void Start() { 
		//transform.position = Vector3.zero;
		last_viewer_position = transform.position;
		last_viewer_scale = scale;

		// This is not working because FastNoise Seed are finished
		OnViewerUpdated ();
	}
	#endregion

	#region update-viewer
	public void RefreshSettings() {
		sqrtViewerThreshold = viewerThreshold * viewerThreshold;
		bound.center = transform.position;
		bound.size = new Vector3 (boundSize * scale, 0, boundSize * scale);
		cubeTransform.localScale = new Vector3 (scale, scale, scale)*.3f;
	}
	void Update() {
		// Update position based on axis
		float horizontal = Input.GetAxis ("Horizontal");
		float vertical = Input.GetAxis ("Vertical");
		if (horizontal != 0 || vertical != 0) {
			transform.position += new Vector3 (horizontal, 0f, vertical) * Time.deltaTime * speed * scale;
		}
		float scrollWheel = Input.GetAxis ("Mouse ScrollWheel");
		if (scrollWheel != 0) {
			scale -= scrollWheel * Time.deltaTime * speed * scale;
			if (scale <= minScale) { scale = minScale; }
			if (scale >= maxScale) { scale = maxScale; }

			cameraTransform.localPosition = new Vector3 (0f, boundSize * scale * 0.8f, -boundSize * scale * 0.4f);
			RefreshSettings ();
		}
		UpdateThreshold ();
	}

	public void UpdatePos(Direction direction) {
		transform.position += Coord.GetDirectionAsCoord(direction).ToVector3() * speedUI * Time.deltaTime * speed * scale;
		UpdateThreshold ();
	}
	// Enum not available from Inspector :/

	public void UpdatePosTop() {
		this.UpdatePos (Direction.Top);
	}
	public void UpdatePosRight() {
		this.UpdatePos (Direction.Right);
	}
	public void UpdatePosBottom() {
		this.UpdatePos (Direction.Bottom);
	}
	public void UpdatePosLeft() {
		this.UpdatePos (Direction.Left);
	}

	public void UpdateScaleUp() {
		scale += speedScaleUI * scale;
		if (scale <= minScale) { scale = minScale; }
		if (scale >= maxScale) { scale = maxScale; }

		cameraTransform.localPosition = new Vector3 (0f, boundSize * scale, 0f);
		RefreshSettings ();

		// @TODO Add Threshold for the scale update 
		UpdateThreshold ();
	}
	public void UpdateScaleDown() {
		scale -= speedScaleUI * scale;
		if (scale <= minScale) { scale = minScale; }
		if (scale >= maxScale) { scale = maxScale; }

		cameraTransform.localPosition = new Vector3 (0f, boundSize * scale, 0f);
		RefreshSettings ();

		// @TODO Add Threshold for the scale update 
		UpdateThreshold ();
	}
	#endregion

	void UpdateThreshold() {
		// Test the position update
		if ((Mathf.Abs(scale - last_viewer_scale) > (viewerThresholdScale * scale))
		|| (transform.position - last_viewer_position).sqrMagnitude > (sqrtViewerThreshold * scale*scale)) {

			last_viewer_position = transform.position;
			last_viewer_scale = scale;
			bound.center = transform.position;

			OnViewerUpdated ();
		}
	}
	void OnViewerUpdated() {
		MapEndless.instance.UpdateChunks ();
	}

	#region dev
	void OnDrawGizmos() {
		Vector3 gizmosPos = new Vector3 (0f, .5f, 0f);
		float y = boundSize * scale / 5;
		Gizmos.color = Color.green;
		Gizmos.DrawLine (bound.max, bound.max + gizmosPos + (Vector3.up * y));
		Gizmos.color = Color.red;
		Gizmos.DrawLine (bound.min, bound.min + gizmosPos + (Vector3.up * y));

		Gizmos.color = Color.white;
		Gizmos.DrawLine (new Vector3 (bound.min.x, 0f, bound.min.z) + gizmosPos, new Vector3 (bound.min.x, 0f, bound.max.z) + gizmosPos);
		Gizmos.DrawLine (new Vector3 (bound.min.x, 0f, bound.max.z) + gizmosPos, new Vector3 (bound.max.x, 0f, bound.max.z) + gizmosPos);
		Gizmos.DrawLine (new Vector3 (bound.max.x, 0f, bound.max.z) + gizmosPos, new Vector3 (bound.max.x, 0f, bound.min.z) + gizmosPos);
		Gizmos.DrawLine (new Vector3 (bound.max.x, 0f, bound.min.z) + gizmosPos, new Vector3 (bound.min.x, 0f, bound.min.z) + gizmosPos);
		Gizmos.DrawLine (new Vector3 (bound.min.x, y, bound.min.z) + gizmosPos, new Vector3 (bound.min.x, y, bound.max.z) + gizmosPos);
		Gizmos.DrawLine (new Vector3 (bound.min.x, y, bound.max.z) + gizmosPos, new Vector3 (bound.max.x, y, bound.max.z) + gizmosPos);
		Gizmos.DrawLine (new Vector3 (bound.max.x, y, bound.max.z) + gizmosPos, new Vector3 (bound.max.x, y, bound.min.z) + gizmosPos);
		Gizmos.DrawLine (new Vector3 (bound.max.x, y, bound.min.z) + gizmosPos, new Vector3 (bound.min.x, y, bound.min.z) + gizmosPos);

		Gizmos.color = Color.gray;
		Gizmos.DrawWireSphere (last_viewer_position, viewerThreshold * scale);
	}
	void OnGUI() {
		Coord boundCoord = new Coord (this.bound.center, MapEngine.instance.worldChunkSetting);
		GUI.Label (new Rect (10f, 10f, 400f, 30f), "Viewer : " + this.bound.center + " " + boundCoord + " / Scale: " + this.scale);
	}
	#endregion
}
