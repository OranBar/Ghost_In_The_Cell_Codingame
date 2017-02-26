using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

class Player {
	static void Main(string[] args) {
		string[] inputs;
		int factoryCount = int.Parse(Console.ReadLine()); // the number of factories
		int linkCount = int.Parse(Console.ReadLine()); // the number of links between factories
		for (int i = 0; i < linkCount; i++) {
			inputs = Console.ReadLine().Split(' ');
			int factory1 = int.Parse(inputs[0]);
			int factory2 = int.Parse(inputs[1]);
			int distance = int.Parse(inputs[2]);
		}

		// game loop
		while (true) {
			int entityCount = int.Parse(Console.ReadLine()); // the number of entities (e.g. factories and troops)
			for (int i = 0; i < entityCount; i++) {
				inputs = Console.ReadLine().Split(' ');
				int entityId = int.Parse(inputs[0]);
				string entityType = inputs[1];
				int arg1 = int.Parse(inputs[2]);
				int arg2 = int.Parse(inputs[3]);
				int arg3 = int.Parse(inputs[4]);
				int arg4 = int.Parse(inputs[5]);
				int arg5 = int.Parse(inputs[6]);
			}

			// Write an action using Console.WriteLine()
			// To debug: Console.Error.WriteLine("Debug messages...");


			// Any valid action, such as "WAIT" or "MOVE source destination cyborgs"
			Console.WriteLine("WAIT");
		}
	}
}

public class Disk : Vector2 {
	public override double X {
		set {
			Console.Error.WriteLine("Can't override X value of Disk, because it is meant to be immutable. Please create a new Disk instead");
		}
	}

	public override double Y {
		set {
			Console.Error.WriteLine("Can't override Y value of Disk, because it is meant to be immutable. Please create a new Disk instead");
		}
	}

	public readonly Vector2 velocity;
	public readonly double radius;

	public Disk(Vector2 position, Vector2 velocity, double radius) : base(position.X, position.Y) {
		this.velocity = velocity;
		this.radius = radius;
	}

	/** <summary>Move the disk by its speed vector</summary> 
     */
	#region Object Class Overrides
	public override int GetHashCode() {
		unchecked {
			return 17 * this.GetHashCode() + 23 * velocity.GetHashCode() + 31 * radius.GetHashCode();
		}
	}

	public override bool Equals(object obj) {
		if (obj == null) {
			return false;
		}
		Disk otherDisk = obj as Disk;
		if (otherDisk == null) {
			return false;
		}
		if (this == otherDisk
			&& velocity == otherDisk.velocity
			&& radius == otherDisk.radius) {
			return true;
		} else {
			return false;
		}
	}
	#endregion

	#region Operator Overloads
	public static bool operator ==(Disk d1, Disk d2) {
		// If both are null, or both are same instance, return true.
		if (System.Object.ReferenceEquals(d1, d2)) {
			return true;
		}

		// If one is null, but not both, return false.
		if (((object)d1 == null) || ((object)d2 == null)) {
			return false;
		}
		return d1.Equals(d2);
	}

	public static bool operator !=(Disk d1, Disk d2) {
		return (d1 == d2) == false;
	}
	#endregion

	#region Disk Methods
	public Disk Move() {
		return new Disk(this + velocity, velocity, radius);
	}

	public Disk AddAcceleration(Vector2 acceleration) {
		return new Disk(this, velocity + acceleration, radius);
	}

	public Disk AccelerateByFactor(double factor) {
		return new Disk(this, velocity * factor, radius);
	}

	/**
     * <summary> Identify if the disks will collide with each other assuming that both
     * disks will remain with a constant velocity. A collision occurs when the two
     * circles touch each other </summary>
     * 
     */
	public bool WillCollide(Disk other) {
		Vector2 toOther = other - this;
		Vector2 relativeSpeed = velocity - (other.velocity);
		if (relativeSpeed.LengthSquared() <= 0) // No relative movement
		{
			return false;
		}
		if (toOther.Dot(relativeSpeed) < 0) // Opposite directions
		{
			return false;
		}
		return Math.Abs(relativeSpeed.Normalize().Orthogonal().Dot(toOther)) <= radius + other.radius;
	}
	#endregion
}

public class Line {
	public double Slope { get; private set; }
	public double Offset { get; private set; }

	private Vector2 pointOnLine = null;

	public Line(Vector2 point1, Vector2 point2) {
		this.Slope = (point2.Y - point1.Y) / (point2.X - point1.X);
		this.pointOnLine = point1;

		this.Offset = pointOnLine.Y - Slope * pointOnLine.X;
	}

	public Line(Vector2 point1, double slope) {
		this.pointOnLine = point1;

		this.Offset = pointOnLine.Y - Slope * pointOnLine.X;
	}

	public double GetY(double x) {
		return Slope * x + Offset;
	}

	public double GetX(double y) {
		return (y - Offset) / Slope;
	}

	public Vector2 GetIntersection(Line other) {
		//TODO:
		return null;
	}

}

/** Vector2 Class
 * 
 * Author: Oran Bar
 */
[Serializable]
public class Vector2 : IEquatable<Vector2> {
	#region Static Variables
	public static double COMPARISON_TOLERANCE = 0.0000001;

	private readonly static Vector2 zeroVector = new Vector2(0);
	private readonly static Vector2 unitVector = new Vector2(1);

	public static Vector2 Zero {
		get { return zeroVector; }
		private set { }
	}
	public static Vector2 One {
		get { return unitVector; }
		private set { }
	}
	#endregion

	public virtual double X { get; set; }
	public virtual double Y { get; set; }

	public Vector2(double val) {
		this.X = val;
		this.Y = val;
	}

	public Vector2(double x, double y) {
		this.X = x;
		this.Y = y;
	}

	public Vector2(Vector2 v) {
		this.X = v.X;
		this.Y = v.Y;
	}

	#region Operators
	public static Vector2 operator +(Vector2 v1, Vector2 v2) {
		return new Vector2(v1.X + v2.X, v1.Y + v2.Y);
	}

	public static Vector2 operator -(Vector2 v1, Vector2 v2) {
		return new Vector2(v1.X - v2.X, v1.Y - v2.Y);
	}

	public static Vector2 operator *(Vector2 v1, double mult) {
		return new Vector2(v1.X * mult, v1.Y * mult);
	}

	public static bool operator ==(Vector2 a, Vector2 b) {
		// If both are null, or both are same instance, return true.
		if (System.Object.ReferenceEquals(a, b)) {
			return true;
		}

		// If one is null, but not both, return false.
		if (((object)a == null) || ((object)b == null)) {
			return false;
		}

		// Return true if the fields match:
		return a.Equals(b);
	}

	public static bool operator !=(Vector2 a, Vector2 b) {
		return (a == b) == false;
	}
	#endregion

	#region Object Class Overrides
	public override bool Equals(object obj) {
		if (obj == null) {
			return false;
		}
		return Equals(obj as Vector2);
	}

	public bool Equals(Vector2 other) {
		if ((object)other == null) {
			return false;
		}
		if (Math.Abs(X - other.X) > COMPARISON_TOLERANCE) {
			return false;
		}
		if (Math.Abs(Y - other.Y) > COMPARISON_TOLERANCE) {
			return false;
		}
		return true;

	}


	public override int GetHashCode() {
		unchecked {
			return 17 * X.GetHashCode() + 23 * Y.GetHashCode();
		}
	}


	public override string ToString() {
		return String.Format("[{0}, {1}] ", X, Y);
	}
	#endregion

	#region Vector2 Methods
	public static double Distance(Vector2 v1, Vector2 v2) {
		return Math.Sqrt(Math.Pow(v1.X - v2.X, 2) + Math.Pow(v1.Y - v2.Y, 2));
	}

	public static double DistanceSquared(Vector2 v1, Vector2 v2) {
		return Math.Pow(v1.X - v2.X, 2) + Math.Pow(v1.Y - v2.Y, 2);
	}

	public double Distance(Vector2 other) {
		return Vector2.Distance(this, other);
	}

	public double DistanceSquared(Vector2 other) {
		return Vector2.DistanceSquared(this, other);
	}

	public Vector2 Closest(params Vector2[] vectors) {
		return vectors.ToList().OrderBy(v1 => this.DistanceSquared(v1)).First();
	}

	public double Length() {
		return Math.Sqrt(X * X + Y * Y);
	}

	public double LengthSquared() {
		return X * X + Y * Y;
	}

	public Vector2 Normalize() {
		double length = LengthSquared();
		return new Vector2(X / length, Y / length);
	}

	public double Dot(Vector2 v) {
		return X * v.X + Y * v.Y;
	}

	public double Cross(Vector2 v) {
		return X * v.Y + Y * v.X;
	}

	public Vector2 Orthogonal() {
		return new Vector2(-Y, X);
	}

	//TODO: test
	public double AngleTo(Vector2 v) {
		return this.Dot(v) / (this.Length() + v.Length());
	}

	public Vector2 ScalarProjectionOn(Vector2 v) {
		return v.Normalize() * this.Dot(v);
	}

	public double AngleInDegree() {
		return AngleInRadians() * (180.0 / Math.PI);
	}

	public double AngleInRadians() {
		return Math.Atan2(Y, X);
	}
	#endregion
}

