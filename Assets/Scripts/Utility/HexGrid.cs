using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public struct IntVector2
{
    public int x;
    public int y;

    public static IntVector2 Zero = new IntVector2(0, 0);
    public static IntVector2 One = new IntVector2(1, 1);

    public static IntVector2 Left = new IntVector2(-1, 0);
    public static IntVector2 Right = new IntVector2(1, 0);
    public static IntVector2 Up = new IntVector2(0, 1);
    public static IntVector2 Down = new IntVector2(0, -1);

    public IntVector2(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public IntVector2(float x, float y)
        : this((int)x, (int)y)
    {
    }

    public IntVector2(Vector2 point)
        : this(point.x, point.y)
    {
    }

    public IntVector2 Offset(Vector2 offset)
    {
        return this + new IntVector2(offset);
    }

    public static implicit operator Vector2(IntVector2 point)
    {
        return new Vector2(point.x, point.y);
    }

    public static implicit operator Vector3(IntVector2 point)
    {
        return new Vector3(point.x, point.y, 0);
    }

    public static implicit operator IntVector2(Vector2 vector)
    {
        return new IntVector2(vector);
    }

    public static implicit operator IntVector2(Vector3 vector)
    {
        return new IntVector2(vector);
    }

    public IntVector2 Size
    {
        get
        {
            return new IntVector2(Mathf.Abs(x), Mathf.Abs(y));
        }
    }

    public override bool Equals(object obj)
    {
        if (obj is IntVector2)
        {
            return Equals((IntVector2)obj);
        }

        return false;
    }

    public bool Equals(IntVector2 other)
    {
        return other.x == x
            && other.y == y;
    }

    public override int GetHashCode()
    {
        return new { x, y }.GetHashCode();
    }

    public override string ToString()
    {
        return string.Format("IntVector2({0}, {1})", x, y);
    }

    public static bool operator ==(IntVector2 a, IntVector2 b)
    {
        return a.x == b.x && a.y == b.y;
    }

    public static bool operator !=(IntVector2 a, IntVector2 b)
    {
        return a.x != b.x || a.y != b.y;
    }

    public static IntVector2 operator +(IntVector2 a, IntVector2 b)
    {
        a.x += b.x;
        a.y += b.y;

        return a;
    }

    public static IntVector2 operator -(IntVector2 a, IntVector2 b)
    {
        a.x -= b.x;
        a.y -= b.y;

        return a;
    }

    public static IntVector2 operator *(IntVector2 a, int scale)
    {
        a.x *= scale;
        a.y *= scale;

        return a;
    }
}

public static class HexGrid
{
    public static Vector3 xAxis, yAxis, zAxis;

    public static IntVector2[] directions = new IntVector2[]
    {
        new IntVector2(+1,  0),
        new IntVector2(+1, -1),
        new IntVector2( 0, -1),
        new IntVector2(-1,  0),
        new IntVector2(-1, +1),
        new IntVector2( 0, +1),
    };

    static HexGrid()
    {
        xAxis = Quaternion.AngleAxis( 30, Vector3.up) * Vector3.forward;
        yAxis = Quaternion.AngleAxis( 90, Vector3.up) * Vector3.forward;
        zAxis = Quaternion.AngleAxis(150, Vector3.up) * Vector3.forward;
    }

    public static int Distance(IntVector2 a, IntVector2 b)
    {
        IntVector2 c = a - b;

        int x = c.x;
        int y = c.y;
        int z = 0 - c.x - c.y;

        return Mathf.Max(Mathf.Abs(x), Mathf.Abs(y), Mathf.Abs(z));
    }

    public static IntVector2 Rotate(IntVector2 hex, int hexes)
    {
        int x = hex.x;
        int y = hex.y;
        int z = 0 - hex.x - hex.y;

        for (; hexes > 0; hexes -= 1)
        {
            int a = x, b = y, c = z;

            x = -c; y = -a; z = -b;
        }

        for (; hexes < 0; hexes += 1)
        {
            int a = x, b = y, c = z;

            x = -b; y = -c; z = -a;
        }

        hex.x = x;
        hex.y = y;

        return hex;
    }

    public static Vector3 HexToWorld(IntVector2 hex)
    {
        return hex.x * xAxis
             + hex.y * yAxis;
    }

    public static IEnumerable<IntVector2> Neighbours(IntVector2 hex)
    {
        for (int i = 0; i < directions.Length; ++i)
        {
            yield return hex + directions[i];
        }
    }
}

