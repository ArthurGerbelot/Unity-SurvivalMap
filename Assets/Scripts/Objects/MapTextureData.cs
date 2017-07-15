using UnityEngine;
using System.Collections;
using System.Linq;

[CreateAssetMenu()]
public class MapTextureData : ScriptableObject {

	const int textureSize = 512;
	const TextureFormat textureFormat = TextureFormat.RGB565;

	public TextureLayer[] layers;

	/*
		this.layers = new TextureLayer[8];
		this.layers [0] = new TextureLayer (Color.blue, 1f, waterHeight * .66f, 0f, 1f);
		this.layers [1] = new TextureLayer (Color.cyan, 1f, waterHeight, 1f, .1f);
		this.layers [2] = new TextureLayer (Color.yellow, 0f, minGroundHeight, 0f, 1f);
		this.layers [3] = new TextureLayer (new Color(218f/255f, .5f, 0f), 0f, minGroundHeight + ((maxGroundHeight - minGroundHeight) * .33f), .1f, 1f);
		this.layers [4] = new TextureLayer (Color.red, 0f, minGroundHeight + ((maxGroundHeight - minGroundHeight) * .66f), .1f, 1f);
		this.layers [5] = new TextureLayer (Color.gray, 0f, maxGroundHeight, .1f, 1f);
		this.layers [6] = new TextureLayer (new Color(.25f,.25f,.25f), 0f, mountainHeight, .1f, 1f);
		this.layers [7] = new TextureLayer (Color.black, 0f, mountainHeight + ((1f - mountainHeight) * .33f), .1f, 1f);
*/

	// Update manualy height to fit with code
	public void UpdateStartHeights(WorldChunkSettings setting) {
		
		float waterHeight = MeshGenerator.GetRealHeight (setting.water, WorldZoneTypes.Water, setting);
		float minGroundHeight = MeshGenerator.GetRealHeight (setting.water, WorldZoneTypes.Ground, setting);
		float maxGroundHeight = MeshGenerator.GetRealHeight (setting.mountain, WorldZoneTypes.Ground, setting);
		float mountainHeight = MeshGenerator.GetRealHeight (setting.mountain, WorldZoneTypes.Mountain, setting);

		this.layers [0].startHeight = 0;
		this.layers [1].startHeight = waterHeight*.66f;
		this.layers [2].startHeight = minGroundHeight; // remove blendStrenght to avoir water on lowest ground
		this.layers [3].startHeight = minGroundHeight + ((maxGroundHeight - minGroundHeight) * .33f);
		this.layers [4].startHeight = minGroundHeight + ((maxGroundHeight - minGroundHeight) * .66f);
		this.layers [5].startHeight = maxGroundHeight;
		this.layers [6].startHeight = mountainHeight;
		this.layers [7].startHeight = mountainHeight + ((1f - mountainHeight) * .33f);

		for (int i = 0; i < 8; i++) {
			this.layers [i].textureScale = 1200/3;
		}

		Debug.Log("water:"+waterHeight+"  ground: "+minGroundHeight+"/"+maxGroundHeight+"  mountain: "+mountainHeight);
	}

	public void ApplyOnMaterial(Material material) {
		//material.SetFloat ("var", value);
		material.SetInt ("baseLayerCount", this.layers.Length);
		material.SetColorArray ("baseColors", this.layers.Select(x => x.color).ToArray());
		material.SetFloatArray ("baseStartHeights", this.layers.Select(x => x.startHeight).ToArray());
		material.SetFloatArray ("baseBlendsUp", this.layers.Select(x => x.blendStrenghtUp).ToArray());
		material.SetFloatArray ("baseBlendsDown", this.layers.Select(x => x.blendStrenghtDown).ToArray());
		material.SetFloatArray ("baseColorsStrenght", this.layers.Select(x => x.colorStrenght).ToArray());
		material.SetFloatArray ("baseTextureScales", this.layers.Select(x => x.textureScale).ToArray());
		Texture2DArray texturesArray = GenerateTextureArray (this.layers.Select (x => x.texture).ToArray ());
		material.SetTexture("baseTextures", texturesArray);
	}

	Texture2DArray GenerateTextureArray(Texture2D[] textures) {
		Texture2DArray textureArray = new Texture2DArray (textureSize, textureSize, textures.Length, textureFormat, true);
		for (int i = 0; i < textures.Length; i++) {
			textureArray.SetPixels (textures[i].GetPixels(), i);
		}
		textureArray.Apply ();
		return textureArray;
	}

	[System.Serializable]
	public class TextureLayer {
		public Texture2D texture;
		public Color color;
		[Range(0,1)]
		public float colorStrenght;
		[Range(0,1)]
		public float startHeight;
		[Range(0,1)]
		public float blendStrenghtUp;
		[Range(0,1)]
		public float blendStrenghtDown;
		public float textureScale;

		public TextureLayer(Color color, float colorStrenght, float startHeight, float blendStrenghtUp, float blendStrenghtDown, float textureScale) {
			this.color = color;
			this.colorStrenght = colorStrenght;
			this.startHeight = startHeight;
			this.blendStrenghtUp = blendStrenghtUp;
			this.blendStrenghtDown = blendStrenghtDown;
			this.textureScale = textureScale;
		}
	}
}


