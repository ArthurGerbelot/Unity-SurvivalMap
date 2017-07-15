using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public enum Direction {Top, Left, Bottom, Right}

[System.Serializable]
public class Coord {
	public int x;
	public int y;
	public int idx; // Array[] index of this coord (to be computed only once when required) [-1 if no setting are given, we don't know yet)

	// Direction Update
	[HideInInspector]
	public static Coord Top = new Coord(0,1);
	[HideInInspector] 
	public static Coord Right = new Coord(1,0);
	[HideInInspector]
	public static Coord Bottom = new Coord(0,-1);
	[HideInInspector]
	public static Coord Left = new Coord(-1,0);
	[HideInInspector] // Use this array to quick loop throw all Direction enum 
	public static Direction[] directions = new Direction[] {Direction.Top, Direction.Left, Direction.Bottom, Direction.Right};

	#region construct
	public Coord (int _x, int _y) {
		this.x = _x; 
		this.y = _y;
		this.idx = -1; // We don't konw yet
	}
	public Coord (int _x, int _y, WorldChunkSettings setting) {
		this.x = _x; 
		this.y = _y;
		this.SetIndex (setting);
	}
	// From a World position
	public Coord (Vector3 worldPosition, WorldChunkSettings setting) {
		// Divide by size (not scaled) because we got a world px position
		this.x = Mathf.RoundToInt (worldPosition.x / setting.size) * Coord.Right.x; // * Coord.Right.x to reverse world X and chunk X (if required)
		this.y = Mathf.RoundToInt (worldPosition.z / setting.size) * Coord.Top.y; // * Coord.Top.y to reverse world Y and chunk Y (if required)
	}
	#endregion

	#region override
	public override string ToString () {
		return "[" + this.x + ";" + this.y + "]";
	}
	public static bool operator == (Coord a, Coord b) {
		return (a.x == b.x && a.y == b.y);
	} 
	public static bool operator != (Coord a, Coord b) {
		return (a.x != b.x || a.y != b.y);
	}  
	// Dictionary.ContainKey
	public override int GetHashCode() {
		// Uniq string hashCode
		return (this.x + ";" + this.y).GetHashCode ();
	}
	public override bool Equals(object obj) {
		Coord c = Coord.GetCoord(obj);
		return (c.x == this.x && c.y == this.y); 
	}
	#endregion

	#region static
	public static Coord GetDirectionAsCoord(Direction direction) {
		if (direction == Direction.Top) {
			return Coord.Top;
		} else if (direction == Direction.Right) {
			return Coord.Right;
		} else if (direction == Direction.Bottom) {
			return Coord.Bottom;
		} else { // if (direction == Direction.Left) {
			return Coord.Left;
		}
	}
	public static Direction GetDirectionInverse(Direction direction) {
		if (direction == Direction.Top) {
			return Direction.Bottom;
		} else if (direction == Direction.Right) {
			return Direction.Left;
		} else if (direction == Direction.Bottom) {
			return Direction.Top;
		} else { // if (direction == Direction.Left) {
			return Direction.Right;
		}
	}
	#endregion

	#region object-instance
	public void SetIndex(WorldChunkSettings setting) {
		this.idx = this.y * setting.scaledSize + this.x;
	}
	public Coord GetDirection(Direction direction) {
		Coord directionCoord = Coord.GetDirectionAsCoord (direction);
		return new Coord (this.x + directionCoord.x, this.y + directionCoord.y);
	}
	// Return the single value interesting for matching (only X for Top/Bottom, and Y for Left/Right)
	public int GetBorderValue(Direction direction) {
		return (direction == Direction.Top || direction == Direction.Bottom) ? this.x : this.y;
	}

	// Return the coord next to this one on the `direction` chunk. (this have to be a border coord to work fine)
	public Coord GetCoordOnChunkSide(Direction direction, WorldChunkSettings setting) {
		Coord directionCoord = Coord.GetDirectionAsCoord (direction);
		Coord sideCoord = new Coord (this.x,this.y);
		if (directionCoord.y == 1) {	// Top
			sideCoord.y = 0;
		} else if (directionCoord.x == 1) { // Right
			sideCoord.x = 0;
		} else if (directionCoord.y == -1) { // Bottom
			sideCoord.y = setting.scaledSize - 1;
		} else { // if (directionCoord.x == -1) { // Left
			sideCoord.x = setting.scaledSize - 1;
		}
		return sideCoord;
	}
	public Vector2 ToVector2() {
		return new Vector2 ((float)this.x, (float)this.y);
	}
	public Vector3 ToVector3() {
		return new Vector3 ((float)this.x, 0f, (float)this.y);
	}
	public Vector3 ToWorldPosition(WorldChunkSettings setting) {
		// Reverse if required (from Coord X/Y to Unity X/Y 3D World)
		return new Vector3(
			this.x * setting.size * Coord.Right.x, // * Coord.Right.x to reverse world X and chunk X (if required)
			0f, 
			this.y * setting.size * Coord.Top.y // * Coord.Top.y to reverse world Y and chunk Y (if required)
		);
	}
	public bool IsOnChunkBorder(Direction direction, WorldChunkSettings setting) {
		Coord directionCoord = Coord.GetDirectionAsCoord (direction);
		// Base the search of side with direction coord (if we change static Direction Coord, all will change)
		if (directionCoord.x < 0) { // new Coord(-1,0) - Left
			return (this.x == 0);
		}
		if (directionCoord.x > 0) { // new Coord(1,0) - Right
			return (this.x == setting.scaledSize - 1);
		}
		if (directionCoord.y < 0) { // new Coord(0,-1) - BOTTOM (Y grow UP when chunks go TOP)
			return (this.y == 0);
		}
		if (directionCoord.y > 0) { // new Coord(1,0) - TOP
			return (this.y == setting.scaledSize - 1);
		}
		return false;
	}
	#endregion

	#region transform-to-coord
	/* Transform Vector3/Vector2/T into Coord */
	public static Coord GetCoord(object c) {
		if (c.GetType () == typeof(Vector3)) {
			Vector3 objV3 = (Vector3)c;
			return new Coord((int)objV3.x, (int)objV3.z);
		}
		else if (c.GetType () == typeof(Vector2)) {
			Vector2 objV2 = (Vector2)c;
			return new Coord((int)objV2.x, (int)objV2.y);
		}
		return (Coord)c;
	}
	#endregion

}
