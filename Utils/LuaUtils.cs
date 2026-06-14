using Engine;
using Game;
using MoonSharp.Interpreter;

namespace EBoyTerminal.Utils {
    public static class LuaUtils {
        public static DynValue NewVector2(Script script, Vector2 vector) {
            Table table = NewTable(script);
            SetNumber(table, "x", vector.X);
            SetNumber(table, "y", vector.Y);
            return DynValue.NewTable(table);
        }

        public static DynValue NewVector3(Script script, Vector3 vector) {
            Table table = NewTable(script);
            SetNumber(table, "x", vector.X);
            SetNumber(table, "y", vector.Y);
            SetNumber(table, "z", vector.Z);
            return DynValue.NewTable(table);
        }

        public static DynValue NewVector4(Script script, Vector4 vector) {
            Table table = NewTable(script);
            SetNumber(table, "x", vector.X);
            SetNumber(table, "y", vector.Y);
            SetNumber(table, "z", vector.Z);
            SetNumber(table, "w", vector.W);
            return DynValue.NewTable(table);
        }

        public static DynValue NewPoint2(Script script, Point2 point) {
            Table table = NewTable(script);
            SetNumber(table, "x", point.X);
            SetNumber(table, "y", point.Y);
            return DynValue.NewTable(table);
        }

        public static DynValue NewPoint3(Script script, Point3 point) {
            Table table = NewTable(script);
            SetNumber(table, "x", point.X);
            SetNumber(table, "y", point.Y);
            SetNumber(table, "z", point.Z);
            return DynValue.NewTable(table);
        }

        public static DynValue NewColor(Script script, Color color) {
            Table table = NewTable(script);
            SetNumber(table, "r", color.R);
            SetNumber(table, "g", color.G);
            SetNumber(table, "b", color.B);
            SetNumber(table, "a", color.A);
            SetNumber(table, "packed", color.PackedValue);
            return DynValue.NewTable(table);
        }

        public static DynValue NewQuaternion(Script script, Quaternion quaternion) {
            Table table = NewTable(script);
            SetNumber(table, "x", quaternion.X);
            SetNumber(table, "y", quaternion.Y);
            SetNumber(table, "z", quaternion.Z);
            SetNumber(table, "w", quaternion.W);
            return DynValue.NewTable(table);
        }

        public static DynValue NewMatrix(Script script, Matrix matrix) {
            Table table = NewTable(script);
            SetNumber(table, "m11", matrix.M11);
            SetNumber(table, "m12", matrix.M12);
            SetNumber(table, "m13", matrix.M13);
            SetNumber(table, "m14", matrix.M14);
            SetNumber(table, "m21", matrix.M21);
            SetNumber(table, "m22", matrix.M22);
            SetNumber(table, "m23", matrix.M23);
            SetNumber(table, "m24", matrix.M24);
            SetNumber(table, "m31", matrix.M31);
            SetNumber(table, "m32", matrix.M32);
            SetNumber(table, "m33", matrix.M33);
            SetNumber(table, "m34", matrix.M34);
            SetNumber(table, "m41", matrix.M41);
            SetNumber(table, "m42", matrix.M42);
            SetNumber(table, "m43", matrix.M43);
            SetNumber(table, "m44", matrix.M44);
            return DynValue.NewTable(table);
        }

        public static DynValue NewRectangle(Script script, Rectangle rectangle) {
            Table table = NewTable(script);
            SetNumber(table, "left", rectangle.Left);
            SetNumber(table, "top", rectangle.Top);
            SetNumber(table, "width", rectangle.Width);
            SetNumber(table, "height", rectangle.Height);
            SetNumber(table, "right", rectangle.Right);
            SetNumber(table, "bottom", rectangle.Bottom);
            return DynValue.NewTable(table);
        }

        public static DynValue NewBox(Script script, Box box) {
            Table table = NewTable(script);
            SetNumber(table, "left", box.Left);
            SetNumber(table, "top", box.Top);
            SetNumber(table, "near", box.Near);
            SetNumber(table, "width", box.Width);
            SetNumber(table, "height", box.Height);
            SetNumber(table, "depth", box.Depth);
            SetNumber(table, "right", box.Right);
            SetNumber(table, "bottom", box.Bottom);
            SetNumber(table, "far", box.Far);
            return DynValue.NewTable(table);
        }

        public static DynValue NewBoundingRectangle(Script script, BoundingRectangle rectangle) {
            Table table = NewTable(script);
            table.Set("min", NewVector2(script, rectangle.Min));
            table.Set("max", NewVector2(script, rectangle.Max));
            return DynValue.NewTable(table);
        }

        public static DynValue NewBoundingBox(Script script, BoundingBox box) {
            Table table = NewTable(script);
            table.Set("min", NewVector3(script, box.Min));
            table.Set("max", NewVector3(script, box.Max));
            return DynValue.NewTable(table);
        }

        public static DynValue NewBoundingCircle(Script script, BoundingCircle circle) {
            Table table = NewTable(script);
            table.Set("center", NewVector2(script, circle.Center));
            SetNumber(table, "radius", circle.Radius);
            return DynValue.NewTable(table);
        }

        public static DynValue NewBoundingSphere(Script script, BoundingSphere sphere) {
            Table table = NewTable(script);
            table.Set("center", NewVector3(script, sphere.Center));
            SetNumber(table, "radius", sphere.Radius);
            return DynValue.NewTable(table);
        }

        public static DynValue NewRay2(Script script, Ray2 ray) {
            Table table = NewTable(script);
            table.Set("position", NewVector2(script, ray.Position));
            table.Set("direction", NewVector2(script, ray.Direction));
            return DynValue.NewTable(table);
        }

        public static DynValue NewRay3(Script script, Ray3 ray) {
            Table table = NewTable(script);
            table.Set("position", NewVector3(script, ray.Position));
            table.Set("direction", NewVector3(script, ray.Direction));
            return DynValue.NewTable(table);
        }

        public static DynValue NewLine2(Script script, Line2 line) {
            Table table = NewTable(script);
            table.Set("normal", NewVector2(script, line.Normal));
            SetNumber(table, "d", line.D);
            return DynValue.NewTable(table);
        }

        public static DynValue NewPlane(Script script, Plane plane) {
            Table table = NewTable(script);
            table.Set("normal", NewVector3(script, plane.Normal));
            SetNumber(table, "d", plane.D);
            return DynValue.NewTable(table);
        }

        public static DynValue NewSegment2(Script script, Segment2 segment) {
            Table table = NewTable(script);
            table.Set("start", NewVector2(script, segment.Start));
            table.Set("end", NewVector2(script, segment.End));
            return DynValue.NewTable(table);
        }

        public static DynValue NewSegment3(Script script, Segment3 segment) {
            Table table = NewTable(script);
            table.Set("start", NewVector3(script, segment.Start));
            table.Set("end", NewVector3(script, segment.End));
            return DynValue.NewTable(table);
        }

        public static DynValue NewCellFace(Script script, CellFace cellFace) {
            Table table = NewTable(script);
            SetNumber(table, "x", cellFace.X);
            SetNumber(table, "y", cellFace.Y);
            SetNumber(table, "z", cellFace.Z);
            SetNumber(table, "face", cellFace.Face);
            table.Set("point", NewPoint3(script, cellFace.Point));
            return DynValue.NewTable(table);
        }

        public static DynValue NewBlockPlacementData(Script script, BlockPlacementData data) {
            Table table = NewTable(script);
            SetNumber(table, "value", data.Value);
            table.Set("cellFace", NewCellFace(script, data.CellFace));
            return DynValue.NewTable(table);
        }

        public static DynValue NewTerrainRaycastResult(Script script, TerrainRaycastResult result) {
            Table table = NewTable(script);
            table.Set("ray", NewRay3(script, result.Ray));
            SetNumber(table, "value", result.Value);
            table.Set("cellFace", NewCellFace(script, result.CellFace));
            SetNumber(table, "collisionBoxIndex", result.CollisionBoxIndex);
            SetNumber(table, "distance", result.Distance);
            table.Set("hitPoint", NewVector3(script, result.HitPoint()));
            return DynValue.NewTable(table);
        }

        public static Vector2 DecodeVector2(this Table table) {
            float x = RequiredFloat(table, "x");
            float y = RequiredFloat(table, "y");
            return new Vector2(x, y);
        }

        public static Vector3 DecodeVector3(this Table table) {
            float x = RequiredFloat(table, "x");
            float y = RequiredFloat(table, "y");
            float z = RequiredFloat(table, "z");
            return new Vector3(x, y, z);
        }

        public static Vector4 DecodeVector4(this Table table) {
            float x = RequiredFloat(table, "x");
            float y = RequiredFloat(table, "y");
            float z = RequiredFloat(table, "z");
            float w = RequiredFloat(table, "w");
            return new Vector4(x, y, z, w);
        }

        public static Point2 DecodePoint2(this Table table) {
            int x = RequiredInt(table, "x");
            int y = RequiredInt(table, "y");
            return new Point2(x, y);
        }

        public static Point3 DecodePoint3(this Table table) {
            int x = RequiredInt(table, "x");
            int y = RequiredInt(table, "y");
            int z = RequiredInt(table, "z");
            return new Point3(x, y, z);
        }

        public static Color DecodeColor(this Table table) {
            DynValue packed = table.Get("packed");
            if (packed.Type == DataType.Number) {
                return new Color((uint)packed.Number);
            }
            int r = RequiredInt(table, "r");
            int g = RequiredInt(table, "g");
            int b = RequiredInt(table, "b");
            int a = OptionalInt(table, "a", 255);
            return new Color(r, g, b, a);
        }

        public static Quaternion DecodeQuaternion(this Table table) {
            float x = RequiredFloat(table, "x");
            float y = RequiredFloat(table, "y");
            float z = RequiredFloat(table, "z");
            float w = RequiredFloat(table, "w");
            return new Quaternion(x, y, z, w);
        }

        public static Matrix DecodeMatrix(this Table table) =>
            new(
                RequiredFloat(table, "m11"),
                RequiredFloat(table, "m12"),
                RequiredFloat(table, "m13"),
                RequiredFloat(table, "m14"),
                RequiredFloat(table, "m21"),
                RequiredFloat(table, "m22"),
                RequiredFloat(table, "m23"),
                RequiredFloat(table, "m24"),
                RequiredFloat(table, "m31"),
                RequiredFloat(table, "m32"),
                RequiredFloat(table, "m33"),
                RequiredFloat(table, "m34"),
                RequiredFloat(table, "m41"),
                RequiredFloat(table, "m42"),
                RequiredFloat(table, "m43"),
                RequiredFloat(table, "m44"));

        public static Rectangle DecodeRectangle(this Table table) {
            int left = RequiredInt(table, "left");
            int top = RequiredInt(table, "top");
            int width = RequiredInt(table, "width");
            int height = RequiredInt(table, "height");
            return new Rectangle(left, top, width, height);
        }

        public static Box DecodeBox(this Table table) {
            int left = RequiredInt(table, "left");
            int top = RequiredInt(table, "top");
            int near = RequiredInt(table, "near");
            int width = RequiredInt(table, "width");
            int height = RequiredInt(table, "height");
            int depth = RequiredInt(table, "depth");
            return new Box(left, top, near, width, height, depth);
        }

        public static BoundingRectangle DecodeBoundingRectangle(this Table table) {
            Vector2 min = RequiredTable(table, "min").DecodeVector2();
            Vector2 max = RequiredTable(table, "max").DecodeVector2();
            return new BoundingRectangle(min, max);
        }

        public static BoundingBox DecodeBoundingBox(this Table table) {
            Vector3 min = RequiredTable(table, "min").DecodeVector3();
            Vector3 max = RequiredTable(table, "max").DecodeVector3();
            return new BoundingBox(min, max);
        }

        public static BoundingCircle DecodeBoundingCircle(this Table table) {
            Vector2 center = RequiredTable(table, "center").DecodeVector2();
            float radius = RequiredFloat(table, "radius");
            return new BoundingCircle(center, radius);
        }

        public static BoundingSphere DecodeBoundingSphere(this Table table) {
            Vector3 center = RequiredTable(table, "center").DecodeVector3();
            float radius = RequiredFloat(table, "radius");
            return new BoundingSphere(center, radius);
        }

        public static Ray2 DecodeRay2(this Table table) {
            Vector2 position = RequiredTable(table, "position").DecodeVector2();
            Vector2 direction = RequiredTable(table, "direction").DecodeVector2();
            return new Ray2(position, direction);
        }

        public static Ray3 DecodeRay3(this Table table) {
            Vector3 position = RequiredTable(table, "position").DecodeVector3();
            Vector3 direction = RequiredTable(table, "direction").DecodeVector3();
            return new Ray3(position, direction);
        }

        public static Line2 DecodeLine2(this Table table) {
            Vector2 normal = RequiredTable(table, "normal").DecodeVector2();
            float d = RequiredFloat(table, "d");
            return new Line2(normal, d);
        }

        public static Plane DecodePlane(this Table table) {
            Vector3 normal = RequiredTable(table, "normal").DecodeVector3();
            float d = RequiredFloat(table, "d");
            return new Plane(normal, d);
        }

        public static Segment2 DecodeSegment2(this Table table) {
            Vector2 start = RequiredTable(table, "start").DecodeVector2();
            Vector2 end = RequiredTable(table, "end").DecodeVector2();
            return new Segment2(start, end);
        }

        public static Segment3 DecodeSegment3(this Table table) {
            Vector3 start = RequiredTable(table, "start").DecodeVector3();
            Vector3 end = RequiredTable(table, "end").DecodeVector3();
            return new Segment3(start, end);
        }

        public static CellFace DecodeCellFace(this Table table) {
            DynValue point = table.Get("point");
            if (point.Type == DataType.Table) {
                return new CellFace(point.Table.DecodePoint3(), RequiredInt(table, "face"));
            }
            int x = RequiredInt(table, "x");
            int y = RequiredInt(table, "y");
            int z = RequiredInt(table, "z");
            int face = RequiredInt(table, "face");
            return new CellFace(x, y, z, face);
        }

        public static BlockPlacementData DecodeBlockPlacementData(this Table table) =>
            new() {
                Value = RequiredInt(table, "value"),
                CellFace = RequiredTable(table, "cellFace").DecodeCellFace()
            };

        public static TerrainRaycastResult DecodeTerrainRaycastResult(this Table table) =>
            new() {
                Ray = RequiredTable(table, "ray").DecodeRay3(),
                Value = RequiredInt(table, "value"),
                CellFace = RequiredTable(table, "cellFace").DecodeCellFace(),
                CollisionBoxIndex = RequiredInt(table, "collisionBoxIndex"),
                Distance = RequiredFloat(table, "distance")
            };

        static Table NewTable(Script script) => new(script);

        static void SetNumber(Table table, string key, double value) {
            table.Set(key, DynValue.NewNumber(value));
        }

        static float RequiredFloat(Table table, string key) => (float)RequiredNumber(table, key);

        static int RequiredInt(Table table, string key) => (int)RequiredNumber(table, key);

        static int OptionalInt(Table table, string key, int defaultValue) {
            DynValue value = table.Get(key);
            return value.Type == DataType.Nil || value.Type == DataType.Void ? defaultValue : (int)ReadNumber(value, key);
        }

        static double RequiredNumber(Table table, string key) => ReadNumber(table.Get(key), key);

        static double ReadNumber(DynValue value, string key) {
            if (value.Type != DataType.Number) {
                throw new ScriptRuntimeException($"field '{key}' must be a number");
            }
            return value.Number;
        }

        static Table RequiredTable(Table table, string key) {
            DynValue value = table.Get(key);
            if (value.Type != DataType.Table) {
                throw new ScriptRuntimeException($"field '{key}' must be a table");
            }
            return value.Table;
        }
    }
}