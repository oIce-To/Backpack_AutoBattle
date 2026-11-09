using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    public enum EconomyIconKind { OriginShard, MineTicket, Blueprint }

    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PrototypeEconomyIcon : MaskableGraphic
    {
        private static readonly string[] Crystal =
        {
            "..........dd..........",
            ".........dhhd.........",
            "........dchhcd........",
            "........dhmmmcd.......",
            ".......dchmmmcd.......",
            "..dddd.dchmmmsd.......",
            "..dhhcdchmmmmsd..ddd..",
            "..dhmmcdmmmmsdp.dhhhd.",
            "..dhmmmcdmmsdpddhmmcd.",
            "..dchmmmcdsdpdchmmmcd.",
            "...dchmmmdpddchmmmcd..",
            "...dschmmdpdchmmscd...",
            "....dschmdpdchmscd....",
            ".....dsddchhdmscd.....",
            ".....ddchhmmcdsd......",
            "..dd.dchmmmscdsdd.....",
            ".dhhcddmmmmscdhhd.....",
            ".dhmmscddmscdhmmcd....",
            "..dsmmscdscdhmmscd....",
            "...dssmscdchmmscd.....",
            "....dpsscdhmmscd......",
            ".....dpscdmsscd.......",
            "......ddspssdd........",
            "........dddd.........."
        };
        private static readonly string[] Ticket =
        {
            "..dddddddddddddddddd..",
            ".dyyyyyyyyyyyyyyyyygd.",
            ".dygggggggggggggggggd.",
            ".dygddddddddddddddggd.",
            ".dygdyyyyyyyyyyyygdgd.",
            "..dgdyggggbgggggygd...",
            "...gdygggbbbggggygg...",
            "..dgdyggbbbbbgggygd...",
            ".dygdygggbbbggggygggd.",
            ".dygdyggggbgggggygggd.",
            ".dygddddddddddddddggd.",
            ".dygggggggggggggggggd.",
            ".dggggggggggggggggggd.",
            "..dddddddddddddddddd.."
        };
        private static readonly string[] Blueprint =
        {
            "....dddddddddddddd....",
            "...dbbbbbbbbbbbbyyd...",
            "...dbhhhhhhhhbbbbyd...",
            "...dbbbbbbbbbbbbbyd...",
            "...dbbbbchhbbbbbbbd...",
            "...dbbbchhhbbbbbbbd...",
            "...dbbchhhbbbbbbbbd...",
            "...dbchhhbbbbbbbbbd...",
            "...dbbhhbbbbbbbbbbd...",
            "...dbbbhbbbbbbbbbbd...",
            "...dbbbbbbbhhhbbbbd...",
            "...dbbbbbbhhhhhbbbd...",
            "...dbbbbbbbhhhbbbbd...",
            "...dbbbbbbbbbbbbbbd...",
            "..dddddddddddddddbd...",
            ".dyyyyyyyyyyyyyyybd...",
            ".dygggggggggggggybd...",
            "..dddddddddddddddd...."
        };
        private EconomyIconKind _kind;
        public PrototypeEconomyIcon() { useLegacyMeshGeneration = false; }
        public void SetKind(EconomyIconKind kind) { _kind = kind; SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            string[] pixels = _kind switch
            {
                EconomyIconKind.OriginShard => Crystal,
                EconomyIconKind.MineTicket => Ticket,
                _ => Blueprint
            };
            int columns = pixels.Max(row => row.Length);
            Rect rect = GetPixelAdjustedRect();
            float size = Mathf.Min(rect.width / columns, rect.height / pixels.Length);
            Vector2 origin = rect.center + new Vector2(-columns, pixels.Length) * size * .5f;
            for (int y = 0; y < pixels.Length; y++)
                for (int x = 0; x < pixels[y].Length; x++)
                {
                    char part = pixels[y][x];
                    if (part == '.') continue;
                    Color tint = part switch
                    {
                        'd' => new Color32(44, 62, 84, 255),
                        's' => new Color32(73, 108, 134, 255),
                        'm' => new Color32(121, 177, 188, 255),
                        'c' => new Color32(165, 245, 239, 255),
                        'h' => new Color32(231, 255, 250, 255),
                        'p' => new Color32(174, 128, 206, 255),
                        'g' => new Color32(208, 157, 68, 255),
                        'y' => new Color32(255, 231, 155, 255),
                        _ => new Color32(73, 136, 152, 255)
                    };
                    float left = origin.x + x * size, top = origin.y - y * size;
                    int i = mesh.currentVertCount;
                    mesh.AddVert(new Vector3(left, top), tint * color, Vector2.zero);
                    mesh.AddVert(new Vector3(left + size, top), tint * color, Vector2.zero);
                    mesh.AddVert(new Vector3(left + size, top - size), tint * color, Vector2.zero);
                    mesh.AddVert(new Vector3(left, top - size), tint * color, Vector2.zero);
                    mesh.AddTriangle(i, i + 1, i + 2); mesh.AddTriangle(i, i + 2, i + 3);
                }
        }
    }
}
