using Engine;
using Engine.Graphics;
using Game;
using SCIENEW;
using SCIENEW.VoltNet;
using EBoyTerminal.Electric;
using EBoyTerminal.VoltNet;

namespace EBoyTerminal {
    /// <summary>
    /// 月之终端方块。正面（<see cref="GetFacing"/>）为屏幕无接线；其余五向为宿主 <see cref="ElectricConnectorDirection"/> 相对 IO。
    /// </summary>
    public class MoonTerminalBlock : CubeBlock, IVoltDevice, IElectricElementBlock, IRotatableDevice, IElectricDrillRemovable {
        public static int Index = 550;

        const int SlotTop = 0;
        const int SlotSideBack = 1;
        const int SlotBottom = 2;
        const int SlotFront = 3;

        public override string GetCategory(int value) => IEConstants.BlockCategory.Devices;

        public override bool IsInteractive(SubsystemTerrain subsystemTerrain, int value) => true;

        public override int GetTextureSlotCount(int value) => 16;

        public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z) {
            generator.GenerateCubeVertices(this, value, x, y, z, Color.White, geometry.GetGeometry(EBoyTerminalLoader.BlockTexture).OpaqueSubsetsByFace);
            for (int face = 0; face < 6; face++) {
                if (face == 4) {
                    continue;
                }
                generator.GenerateWireVertices(value, x, y, z, face, 0.35f, Vector2.Zero, geometry.SubsetOpaque);
            }
        }

        public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData) {
            BlocksManager.DrawCubeBlock(primitivesRenderer, value, new Vector3(size), ref matrix, color, color, environmentData, EBoyTerminalLoader.BlockTexture);
        }

        public override int GetFaceTextureSlot(int face, int value) {
            if (face == 4) {
                return SlotTop;
            }
            if (face == 5) {
                return SlotBottom;
            }
            int facing = GetFacing(value);
            if (face == facing) {
                return SlotFront;
            }
            return SlotSideBack;
        }

        public HashSet<int> GetFaceMask(int value) {
            HashSet<int> faces = [0, 1, 2, 3, 4, 5];
            faces.Remove(GetFacing(value));
            return faces;
        }

        public VoltElement? GetVoltElement(SubsystemVoltNet subsystemVoltNet, SubsystemTerrain subsystemTerrain, Point3 position, int blockValue)
            => new MoonTerminalVoltElement(subsystemVoltNet, subsystemTerrain, position, blockValue);

        public float GetStandardPL(int value) => 6;

        public float GetStandardUL(int value) => 1;

        public ElectricElement CreateElectricElement(SubsystemElectricity subsystemElectricity, int value, int x, int y, int z) {
            return new MoonTerminalElectricElement(subsystemElectricity, new CellFace(x, y, z, 4), GetDirection(value));
        }

        public ElectricConnectorType? GetConnectorType(SubsystemTerrain terrain, int value, int face, int connectorFace, int x, int y, int z) => ElectricConnectorType.InputOutput;

        public int GetConnectionMask(int value) => int.MaxValue;

        public static int GetFacing(int value) => Terrain.ExtractData(value) & 3;

        public static int GetDirection(int value) => CellFace.OppositeFace(GetFacing(value));

        public int GetNextDirection(int value, bool reverse = false) {
            int facing = GetFacing(value);
            facing = reverse ? (facing + 3) % 4 : (facing + 1) % 4;
            return Terrain.ReplaceData(value, (Terrain.ExtractData(value) & ~3) | facing);
        }

        public override BlockPlacementData GetPlacementValue(SubsystemTerrain subsystemTerrain,
            ComponentMiner componentMiner,
            int value,
            TerrainRaycastResult raycastResult) {
            Vector3 forward = Matrix.CreateFromQuaternion(componentMiner.ComponentCreature.ComponentCreatureModel.EyeRotation).Forward;
            float num = Vector3.Dot(forward, Vector3.UnitZ);
            float num2 = Vector3.Dot(forward, Vector3.UnitX);
            float num3 = Vector3.Dot(forward, -Vector3.UnitZ);
            float num4 = Vector3.Dot(forward, -Vector3.UnitX);
            int data = num == MathUtils.Max(num, num2, num3, num4) ? 2 :
                num2 == MathUtils.Max(num2, num3, num4) ? 3 :
                num3 == Math.Max(num3, num4) ? 0 : 1;
            BlockPlacementData result = default;
            result.Value = Terrain.ReplaceData(Terrain.ReplaceContents(BlockIndex), data);
            result.CellFace = raycastResult.CellFace;
            return result;
        }
    }
}
