using System;

public readonly struct RoomCoord : IEquatable<RoomCoord>
{
	public int X { get; }
	public int Y { get; }

	public RoomCoord(int x, int y)
	{
		X = x;
		Y = y;
	}

	public bool Equals(RoomCoord other) => X == other.X && Y == other.Y;
	public override bool Equals(object? obj) => obj is RoomCoord other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(X, Y);
	public static bool operator ==(RoomCoord left, RoomCoord right) => left.Equals(right);
	public static bool operator !=(RoomCoord left, RoomCoord right) => !left.Equals(right);

	public override string ToString() => $"({X}, {Y})";
}
