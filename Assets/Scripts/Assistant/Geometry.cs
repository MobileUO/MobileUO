#region License
// Copyright (C) 2022-2025 Sascha Puligheddu
// 
// This project is a complete reproduction of AssistUO for MobileUO and ClassicUO.
// Developed as a lightweight, native assistant.
// 
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// 
// SPECIAL PERMISSION: Integration with projects under BSD 2-Clause (like ClassicUO)
// is permitted, provided that the integrated result remains publicly accessible 
// and the AGPL-3.0 terms are respected for this specific module.
//
// This program is distributed WITHOUT ANY WARRANTY. 
// See <https://www.gnu.org> for details.
#endregion

using System;

namespace Assistant
{
    public interface IPoint2D
    {
        int X { get; }
        int Y { get; }
    }

    public interface IPoint3D : IPoint2D
    {
        int Z { get; }
    }

    public struct Point2D : IPoint2D
    {
        internal int _X;
        internal int _Y;

        public static readonly Point2D Zero = new Point2D(0, 0);
        public static readonly Point2D MinusOne = new Point2D(-1, -1);

        public Point2D(int x, int y)
        {
            _X = x;
            _Y = y;
        }

        public Point2D(IPoint2D p) : this(p.X, p.Y)
        {
        }

        public int X
        {
            get { return _X; }
            set { _X = value; }
        }

        public int Y
        {
            get { return _Y; }
            set { _Y = value; }
        }

        public override string ToString()
        {
            return string.Format("({0}, {1})", _X, _Y);
        }

        public override bool Equals(object o)
        {
            if (o == null || !(o is IPoint2D)) return false;

            IPoint2D p = (IPoint2D)o;

            return _X == p.X && _Y == p.Y;
        }

        public override int GetHashCode()
        {
            return _X ^ _Y;
        }

        public static bool operator ==(Point2D l, Point2D r)
        {
            return l._X == r._X && l._Y == r._Y;
        }

        public static bool operator !=(Point2D l, Point2D r)
        {
            return l._X != r._X || l._Y != r._Y;
        }

        public static bool operator ==(Point2D l, IPoint2D r)
        {
            return l._X == r.X && l._Y == r.Y;
        }

        public static bool operator !=(Point2D l, IPoint2D r)
        {
            return l._X != r.X || l._Y != r.Y;
        }

        public static bool operator >(Point2D l, Point2D r)
        {
            return l._X > r._X && l._Y > r._Y;
        }

        public static bool operator >(Point2D l, Point3D r)
        {
            return l._X > r._X && l._Y > r._Y;
        }

        public static bool operator >(Point2D l, IPoint2D r)
        {
            return l._X > r.X && l._Y > r.Y;
        }

        public static bool operator <(Point2D l, Point2D r)
        {
            return l._X < r._X && l._Y < r._Y;
        }

        public static bool operator <(Point2D l, Point3D r)
        {
            return l._X < r._X && l._Y < r._Y;
        }

        public static bool operator <(Point2D l, IPoint2D r)
        {
            return l._X < r.X && l._Y < r.Y;
        }

        public static bool operator >=(Point2D l, Point2D r)
        {
            return l._X >= r._X && l._Y >= r._Y;
        }

        public static bool operator >=(Point2D l, Point3D r)
        {
            return l._X >= r._X && l._Y >= r._Y;
        }

        public static bool operator >=(Point2D l, IPoint2D r)
        {
            return l._X >= r.X && l._Y >= r.Y;
        }

        public static bool operator <=(Point2D l, Point2D r)
        {
            return l._X <= r._X && l._Y <= r._Y;
        }

        public static bool operator <=(Point2D l, Point3D r)
        {
            return l._X <= r._X && l._Y <= r._Y;
        }

        public static bool operator <=(Point2D l, IPoint2D r)
        {
            return l._X <= r.X && l._Y <= r.Y;
        }
    }

    public struct Point3D : IPoint3D
    {
        internal int _X;
        internal int _Y;
        internal int _Z;

        public static readonly Point3D Zero = new Point3D(0, 0, 0);
        public static readonly Point3D MinusOne = new Point3D(-1, -1, 0);

        public Point3D(int x, int y, int z)
        {
            _X = x;
            _Y = y;
            _Z = z;
        }

        public Point3D(IPoint3D p) : this(p.X, p.Y, p.Z)
        {
        }

        public Point3D(IPoint2D p, int z) : this(p.X, p.Y, z)
        {
        }

        public int X
        {
            get { return _X; }
            set { _X = value; }
        }

        public int Y
        {
            get { return _Y; }
            set { _Y = value; }
        }

        public int Z
        {
            get { return _Z; }
            set { _Z = value; }
        }

        public override string ToString()
        {
            return string.Format("({0} {1} {2})", _X, _Y, _Z);
        }

        public override bool Equals(object o)
        {
            if (o == null || !(o is IPoint3D)) return false;

            IPoint3D p = (IPoint3D)o;

            return _X == p.X && _Y == p.Y && _Z == p.Z;
        }

        public override int GetHashCode()
        {
            return _X ^ _Y ^ _Z;
        }

        public static Point3D Parse(string value)
        {
            int start = value.IndexOf('(');
            int end = value.IndexOf(',', start + 1);

            string param1 = value.Substring(start + 1, end - (start + 1)).Trim();

            start = end;
            end = value.IndexOf(',', start + 1);

            string param2 = value.Substring(start + 1, end - (start + 1)).Trim();

            start = end;
            end = value.IndexOf(')', start + 1);

            string param3 = value.Substring(start + 1, end - (start + 1)).Trim();

            return new Point3D(Convert.ToInt32(param1), Convert.ToInt32(param2), Convert.ToInt16(param3));
        }

        public static bool operator ==(Point3D l, Point3D r)
        {
            return l._X == r._X && l._Y == r._Y && l._Z == r._Z;
        }

        public static bool operator !=(Point3D l, Point3D r)
        {
            return l._X != r._X || l._Y != r._Y || l._Z != r._Z;
        }

        public static bool operator ==(Point3D l, IPoint3D r)
        {
            return l._X == r.X && l._Y == r.Y && l._Z == r.Z;
        }

        public static bool operator !=(Point3D l, IPoint3D r)
        {
            return l._X != r.X || l._Y != r.Y || l._Z != r.Z;
        }

        public static Point3D operator +(Point3D l, Point3D r)
        {
            return new Point3D(l._X + r._X, l._Y + r._Y, l._Z + r._Z);
        }

        public static Point3D operator -(Point3D l, Point3D r)
        {
            return new Point3D(l._X - r._X, l._Y - r._Y, l._Z - r._Z);
        }
    }

    public struct Line2D
    {
        private Point2D _Start, _End;

        public Line2D(IPoint2D start, IPoint2D end)
        {
            _Start = new Point2D(start);
            _End = new Point2D(end);
            Fix();
        }

        public void Fix()
        {
            if (_Start > _End)
            {
                Point2D temp = _Start;
                _Start = _End;
                _End = temp;
            }
        }

        public Point2D Start
        {
            get { return _Start; }
            set
            {
                _Start = value;
                Fix();
            }
        }

        public Point2D End
        {
            get { return _End; }
            set
            {
                _End = value;
                Fix();
            }
        }

        public double Length
        {
            get
            {
                int run = _End.X - _Start.X;
                int rise = _End.Y - _Start.Y;

                return Math.Sqrt(run * run + rise * rise);
            }
        }

        public override string ToString()
        {
            return string.Format("--{0}->{1}--", _Start, _End);
        }

        public override bool Equals(object o)
        {
            if (o == null || !(o is Line2D)) return false;

            Line2D ln = (Line2D)o;

            return _Start == ln._Start && _End == ln._End;
        }

        public override int GetHashCode()
        {
            return _Start.GetHashCode() ^ (~_End.GetHashCode());
        }

        public static bool operator ==(Line2D l, Line2D r)
        {
            return l._Start == r._Start && l._End == r._End;
        }

        public static bool operator !=(Line2D l, Line2D r)
        {
            return l._Start != r._Start || l._End != r._End;
        }

        public static bool operator >(Line2D l, Line2D r)
        {
            return l._Start > r._Start && l._End > r._End;
        }

        public static bool operator <(Line2D l, Line2D r)
        {
            return l._Start < r._Start && l._End < r._End;
        }

        public static bool operator >=(Line2D l, Line2D r)
        {
            return l._Start >= r._Start && l._End >= r._End;
        }

        public static bool operator <=(Line2D l, Line2D r)
        {
            return l._Start <= r._Start && l._End <= r._End;
        }
    }

    public struct Rectangle2D
    {
        private Point2D _Start;
        private Point2D _End;

        public Rectangle2D(IPoint2D start, IPoint2D end)
        {
            _Start = new Point2D(start);
            _End = new Point2D(end);
        }

        public Rectangle2D(int x, int y, int width, int height)
        {
            _Start = new Point2D(x, y);
            _End = new Point2D(x + width, y + height);
        }

        public void Set(int x, int y, int width, int height)
        {
            _Start = new Point2D(x, y);
            _End = new Point2D(x + width, y + height);
        }

        public static Rectangle2D Parse(string value)
        {
            int start = value.IndexOf('(');
            int end = value.IndexOf(',', start + 1);

            string param1 = value.Substring(start + 1, end - (start + 1)).Trim();

            start = end;
            end = value.IndexOf(',', start + 1);

            string param2 = value.Substring(start + 1, end - (start + 1)).Trim();

            start = end;
            end = value.IndexOf(',', start + 1);

            string param3 = value.Substring(start + 1, end - (start + 1)).Trim();

            start = end;
            end = value.IndexOf(')', start + 1);

            string param4 = value.Substring(start + 1, end - (start + 1)).Trim();

            return new Rectangle2D(Convert.ToInt32(param1), Convert.ToInt32(param2), Convert.ToInt32(param3),
                Convert.ToInt32(param4));
        }

        public Point2D Start
        {
            get { return _Start; }
            set { _Start = value; }
        }

        public Point2D End
        {
            get { return _End; }
            set { _End = value; }
        }

        public int X
        {
            get { return _Start._X; }
            set { _Start._X = value; }
        }

        public int Y
        {
            get { return _Start._Y; }
            set { _Start._Y = value; }
        }

        public int Width
        {
            get { return _End._X - _Start._X; }
            set { _End._X = _Start._X + value; }
        }

        public int Height
        {
            get { return _End._Y - _Start._Y; }
            set { _End._Y = _Start._Y + value; }
        }

        public void MakeHold(Rectangle2D r)
        {
            if (r._Start._X < _Start._X)
                _Start._X = r._Start._X;

            if (r._Start._Y < _Start._Y)
                _Start._Y = r._Start._Y;

            if (r._End._X > _End._X)
                _End._X = r._End._X;

            if (r._End._Y > _End._Y)
                _End._Y = r._End._Y;
        }

        // "test" must be smaller than this rectangle!
        public bool Intersects(Rectangle2D test)
        {
            Point2D e1 = new Point2D(test.Start.X + test.Width, test.Start.Y);
            Point2D e2 = new Point2D(test.Start.X, test.Start.Y + test.Width);

            return Contains(test.Start) || Contains(test.End) || Contains(e1) || Contains(e2);
        }

        public bool Contains(Rectangle2D test)
        {
            Point2D e1 = new Point2D(test.Start.X + test.Width, test.Start.Y);
            Point2D e2 = new Point2D(test.Start.X, test.Start.Y + test.Width);

            return Contains(test.Start) && Contains(test.End) && Contains(e1) && Contains(e2);
        }

        public bool Contains(Point3D p)
        {
            return (_Start._X <= p._X && _Start._Y <= p._Y && _End._X > p._X && _End._Y > p._Y);
            //return ( _Start <= p && _End > p );
        }

        public bool Contains(Point2D p)
        {
            return (_Start._X <= p._X && _Start._Y <= p._Y && _End._X > p._X && _End._Y > p._Y);
            //return ( _Start <= p && _End > p );
        }

        public bool Contains(IPoint2D p)
        {
            return (_Start <= p && _End > p);
        }

        public override string ToString()
        {
            return string.Format("({0}, {1})+({2}, {3})", X, Y, Width, Height);
        }
    }

    public struct Rectangle3D
    {
        private Point3D _Start;
        private Point3D _End;

        public Rectangle3D(IPoint3D start, IPoint3D end)
        {
            _Start = new Point3D(start);
            _End = new Point3D(end);
        }

        public Rectangle3D(int x, int y, int z, int width, int height, int depth)
        {
            _Start = new Point3D(x, y, z);
            _End = new Point3D(x + width, y + height, z + depth);
        }

        public void Set(int x, int y, int z, int width, int height, int depth)
        {
            _Start = new Point3D(x, y, z);
            _End = new Point3D(x + width, y + height, z + depth);
        }

        public Point3D Start
        {
            get { return _Start; }
            set { _Start = value; }
        }

        public Point3D End
        {
            get { return _End; }
            set { _End = value; }
        }

        public int X
        {
            get { return _Start._X; }
            set { _Start._X = value; }
        }

        public int Y
        {
            get { return _Start._Y; }
            set { _Start._Y = value; }
        }

        public int Z
        {
            get { return _Start._Z; }
            set { _Start._Z = value; }
        }

        public int Width
        {
            get { return _End._X - _Start._X; }
            set { _End._X = _Start._X + value; }
        }

        public int Height
        {
            get { return _End._Y - _Start._Y; }
            set { _End._Y = _Start._Y + value; }
        }

        public int Depth
        {
            get { return _End._Z - _Start._Z; }
            set { _End._Z = _Start._Z + value; }
        }

        public void MakeHold(Rectangle3D r)
        {
            if (r._Start._X < _Start._X)
                _Start._X = r._Start._X;

            if (r._Start._Y < _Start._Y)
                _Start._Y = r._Start._Y;

            if (r._Start._Z < _Start._Z)
                _Start._Z = r._Start._Z;

            if (r._End._X > _End._X)
                _End._X = r._End._X;

            if (r._End._Y > _End._Y)
                _End._Y = r._End._Y;

            if (r._End._Z > _End._Z)
                _End._Z = r._End._Z;
        }

        // "test" must be smaller than this rectangle!
        public bool Intersects(Rectangle3D test)
        {
            Point3D e1 = new Point3D(test.Start.X + test.Width, test.Start.Y, test.Start.Z);
            Point3D e2 = new Point3D(test.Start.X, test.Start.Y + test.Width, test.Start.Z);
            Point3D e3 = new Point3D(test.Start.X, test.Start.Y, test.Start.Z + test.Depth);

            return Contains(test.Start) || Contains(test.End) || Contains(e1) || Contains(e2) || Contains(e3);
        }

        public bool Contains(Rectangle3D test)
        {
            Point3D e1 = new Point3D(test.Start.X + test.Width, test.Start.Y, test.Start.Z);
            Point3D e2 = new Point3D(test.Start.X, test.Start.Y + test.Width, test.Start.Z);
            Point3D e3 = new Point3D(test.Start.X, test.Start.Y, test.Start.Z + test.Depth);

            return Contains(test.Start) && Contains(test.End) && Contains(e1) && Contains(e2) && Contains(e3);
        }

        public bool Contains(Point3D p)
        {
            return (p._X >= _Start._X)
                && (p._X < _End._X)
                && (p._Y >= _Start._Y)
                && (p._Y < _End._Y)
                && (p._Z >= _Start._Z)
                && (p._Z < _End._Z);
        }

        public bool Contains(IPoint3D p)
        {
            return (p.X >= _Start._X)
                && (p.X < _End._X)
                && (p.Y >= _Start._Y)
                && (p.Y < _End._Y)
                && (p.Z >= _Start._Z)
                && (p.Z < _End._Z);
        }

        public override string ToString()
        {
            return string.Format("({0}, {1})+({2}, {3})", X, Y, Width, Height);
        }
    }
}
