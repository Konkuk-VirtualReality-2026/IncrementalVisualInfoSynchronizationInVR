using UnityEngine;
using UnityEditor;

namespace VRAdaptation.Editor
{
    /// <summary>
    /// CQB(근접전투) 훈련용 킬하우스 맵을 씬에 절차적으로 생성한다.
    /// 메뉴: VR Adaptation > Generate CQB Map
    /// </summary>
    public static class CQBMapGenerator
    {
        // ── 전체 치수 (미터) ────────────────────────────────────────────────
        const float WALL_H     = 3.5f;   // 벽 높이
        const float WALL_T     = 0.25f;  // 벽 두께
        const float COVER_H    = 1.1f;   // 엄폐물 높이 (허리~가슴)
        const float DOOR_W     = 1.2f;   // 출입구 폭
        const float DOOR_H     = 2.2f;   // 출입구 높이

        static GameObject s_Root;
        static Material   s_WallMat;
        static Material   s_FloorMat;
        static Material   s_CoverMat;

        // ════════════════════════════════════════════════════════════════════
        [MenuItem("VR Adaptation/Generate CQB Map")]
        public static void Generate()
        {
            // 기존 맵 제거
            GameObject old = GameObject.Find("CQB_Map");
            if (old != null)
            {
                Undo.DestroyObjectImmediate(old);
            }

            s_Root = new GameObject("CQB_Map");
            Undo.RegisterCreatedObjectUndo(s_Root, "Generate CQB Map");

            CreateMaterials();
            BuildMap();

            Debug.Log("[CQBMap] 킬하우스 생성 완료.");
        }

        [MenuItem("VR Adaptation/Remove CQB Map")]
        public static void Remove()
        {
            GameObject map = GameObject.Find("CQB_Map");
            if (map != null) Undo.DestroyObjectImmediate(map);
        }

        // ════════════════════════════════════════════════════════════════════
        //  레이아웃 (단위: m, Y=0 기준 바닥)
        //
        //   ┌──────────────────────────────────┐
        //   │  [E] 입구마당         [R3] 기관실 │  Z: 20~28
        //   ├────────┬─────────────┬────────────┤
        //   │ [R1]   │  복도       │   [R2]     │  Z: 10~20
        //   │ 좌측방  ├─────────────┤  우측방    │
        //   │        │             │            │
        //   ├────────┴──┬──────────┴────────────┤
        //   │  [Main]   │   [Cover Zone]        │  Z:  0~10
        //   │  개활지   │   격벽+엄폐물          │
        //   └───────────┴───────────────────────┘
        //   X: 0                              X: 28
        // ════════════════════════════════════════════════════════════════════
        static void BuildMap()
        {
            // 플레이어 스폰 위치 기준: X=14, Z=2, Y=0 (Main 하단 중앙)

            // ── 바닥 & 천장 ────────────────────────────────────────────────
            Box("Floor",   14, -WALL_T * 0.5f, 14, 28, WALL_T, 28, s_FloorMat);
            Box("Ceiling", 14, WALL_H + WALL_T * 0.5f, 14, 28, WALL_T, 28, s_WallMat);

            // ── 외벽 4면 ───────────────────────────────────────────────────
            // 남쪽(Z=0), 북쪽(Z=28), 서쪽(X=0), 동쪽(X=28)
            WallZ("OuterS", 14,  0,  28, WALL_H);
            WallZ("OuterN", 14, 28,  28, WALL_H);
            WallX("OuterW",  0, 14,  28, WALL_H);
            WallX("OuterE", 28, 14,  28, WALL_H);

            // ── [Main] 개활지 (Z:0~10, X:0~14) ───────────────────────────
            // 기둥 4개
            Pillar("Pillar_ML", 3,  5);
            Pillar("Pillar_MR", 11, 5);
            Pillar("Pillar_ML2", 3, 8);
            Pillar("Pillar_MR2", 11, 8);

            // ── [Cover Zone] (Z:0~10, X:14~28) ───────────────────────────
            // 격벽 (가로)
            WallXSegment("Cover_H1", 14, 28, 3,  COVER_H, full: true);
            WallXSegment("Cover_H2", 14, 28, 7,  COVER_H, full: true);
            // 격벽 (세로 칸막이)
            WallZSegment("Cover_V1", 18, 0, 10, COVER_H, full: true);
            WallZSegment("Cover_V2", 23, 0, 10, COVER_H, full: true);

            // 박스 엄폐물
            CoverBox("Box1", 16, 1.5f);
            CoverBox("Box2", 20, 5.0f);
            CoverBox("Box3", 25, 2.0f);
            CoverBox("Box4", 25, 8.5f);

            // ── 구분벽: Main/Cover ↔ R1/R2 (Z=10) ────────────────────────
            // X:0~28 에 출입구 2개 (X=6-7.2, X=20-21.2)
            WallXWithDoors("MidWall", 0, 28, 10, WALL_H,
                new float[] { 6f, 20f });   // 문 왼쪽 엣지 X

            // ── [R1] 좌측방 (Z:10~20, X:0~13) ────────────────────────────
            // 우측 격벽 (X=13), 문 있음 (Z=15)
            WallZWithDoor("R1_RightWall", 13, 10, 20, WALL_H, doorZ: 15f);
            // 내부 엄폐물
            CoverBox("R1_Box1", 3,  14);
            CoverBox("R1_Box2", 9,  17);
            WallXSegment("R1_Cover", 2, 10, 16, COVER_H, full: true);

            // ── [R2] 우측방 (Z:10~20, X:15~28) ───────────────────────────
            // 좌측 격벽 (X=15), 문 있음 (Z=15)
            WallZWithDoor("R2_LeftWall", 15, 10, 20, WALL_H, doorZ: 15f);
            CoverBox("R2_Box1", 18, 13);
            CoverBox("R2_Box2", 24, 17);

            // ── 구분벽: R1/R2 ↔ 입구마당 (Z=20) ──────────────────────────
            WallXWithDoors("UpperMidWall", 0, 28, 20, WALL_H,
                new float[] { 5f, 12f, 21f });

            // ── [E] 입구마당 (Z:20~25, X:0~18) ───────────────────────────
            // 내부 격벽 (X=6, Z:20~25), 문 있음
            WallZWithDoor("Entry_Div", 6, 20, 25, WALL_H, doorZ: 22.5f);
            CoverBox("E_Box1", 3,  23);
            CoverBox("E_Box2", 11, 22);

            // ── [R3] 기관실 (Z:20~28, X:18~28) ───────────────────────────
            // 서쪽 격벽 (X=18)
            WallZSegment("R3_LeftWall", 18, 20, 28, WALL_H, full: true);
            // 내부 엄폐물
            CoverBox("R3_Box1", 21, 24);
            CoverBox("R3_Box2", 25, 26);
            Pillar("R3_Pillar", 21, 22);
        }

        // ════════════════════════════════════════════════════════════════════
        //  헬퍼 메서드
        // ════════════════════════════════════════════════════════════════════

        // 임의 박스 (중심 + 크기)
        static GameObject Box(string name, float cx, float cy, float cz,
                               float sx, float sy, float sz, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(s_Root.transform, false);
            go.transform.position   = new Vector3(cx, cy, cz);
            go.transform.localScale = new Vector3(sx, sy, sz);
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic
                                                     | StaticEditorFlags.OccluderStatic
                                                     | StaticEditorFlags.OccludeeStatic);
            return go;
        }

        // Z 방향 벽 (X 축 수직, 길이=lenX)
        static void WallZ(string name, float cx, float cz, float lenX, float h)
            => Box(name, cx, h * 0.5f, cz, lenX, h, WALL_T, s_WallMat);

        // X 방향 벽 (Z 축 수직, 길이=lenZ)
        static void WallX(string name, float cx, float cz, float lenZ, float h)
            => Box(name, cx, h * 0.5f, cz, WALL_T, h, lenZ, s_WallMat);

        // X 방향 벽 세그먼트 (X: x0~x1, Z 고정)
        static void WallXSegment(string name, float x0, float x1, float z, float h, bool full)
        {
            float len = x1 - x0;
            Box(name, (x0 + x1) * 0.5f, h * 0.5f, z, len, h, WALL_T,
                full ? s_WallMat : s_CoverMat);
        }

        // Z 방향 벽 세그먼트 (Z: z0~z1, X 고정)
        static void WallZSegment(string name, float x, float z0, float z1, float h, bool full)
        {
            float len = z1 - z0;
            Box(name, x, h * 0.5f, (z0 + z1) * 0.5f, WALL_T, h, len,
                full ? s_WallMat : s_CoverMat);
        }

        // X 방향 벽 + 문 여러 개 (문 폭 DOOR_W)
        static void WallXWithDoors(string name, float x0, float x1, float z,
                                   float h, float[] doorLeftEdges)
        {
            // 문들을 X 좌표 오름차순 정렬
            System.Array.Sort(doorLeftEdges);

            float cur = x0;
            int idx = 0;
            foreach (float dLeft in doorLeftEdges)
            {
                float dRight = dLeft + DOOR_W;
                // 문 왼쪽 벽 세그먼트
                if (dLeft > cur)
                {
                    float len = dLeft - cur;
                    Box($"{name}_Seg{idx}a", (cur + dLeft) * 0.5f, h * 0.5f, z,
                        len, h, WALL_T, s_WallMat);
                }
                // 문 위쪽 린텔
                float lintelH = h - DOOR_H;
                if (lintelH > 0.01f)
                {
                    Box($"{name}_Lintel{idx}", (dLeft + dRight) * 0.5f,
                        DOOR_H + lintelH * 0.5f, z,
                        DOOR_W, lintelH, WALL_T, s_WallMat);
                }
                cur = dRight;
                idx++;
            }
            // 마지막 세그먼트
            if (cur < x1)
            {
                float len = x1 - cur;
                Box($"{name}_SegLast", (cur + x1) * 0.5f, h * 0.5f, z,
                    len, h, WALL_T, s_WallMat);
            }
        }

        // Z 방향 벽 + 문 1개 (doorZ: 문 하단 중심 Z)
        static void WallZWithDoor(string name, float x, float z0, float z1,
                                  float h, float doorZ)
        {
            float dBot = doorZ - DOOR_W * 0.5f;
            float dTop = doorZ + DOOR_W * 0.5f;

            // 아래 세그먼트
            if (dBot > z0)
            {
                float len = dBot - z0;
                Box($"{name}_Bot", x, h * 0.5f, (z0 + dBot) * 0.5f,
                    WALL_T, h, len, s_WallMat);
            }
            // 린텔
            float lintelH = h - DOOR_H;
            if (lintelH > 0.01f)
            {
                Box($"{name}_Lintel", x, DOOR_H + lintelH * 0.5f, doorZ,
                    WALL_T, lintelH, DOOR_W, s_WallMat);
            }
            // 위 세그먼트
            if (dTop < z1)
            {
                float len = z1 - dTop;
                Box($"{name}_Top", x, h * 0.5f, (dTop + z1) * 0.5f,
                    WALL_T, h, len, s_WallMat);
            }
        }

        // 기둥 (50cm x 50cm)
        static void Pillar(string name, float x, float z)
            => Box(name, x, WALL_H * 0.5f, z, 0.5f, WALL_H, 0.5f, s_WallMat);

        // 엄폐물 박스 (60cm 정육면체)
        static void CoverBox(string name, float x, float z)
            => Box(name, x, COVER_H * 0.5f, z, 0.7f, COVER_H, 0.7f, s_CoverMat);

        // ════════════════════════════════════════════════════════════════════
        //  머티리얼 생성
        // ════════════════════════════════════════════════════════════════════
        static void CreateMaterials()
        {
            s_WallMat  = MakeMat("CQB_Wall",  new Color(0.55f, 0.52f, 0.48f));  // 콘크리트 회색
            s_FloorMat = MakeMat("CQB_Floor", new Color(0.35f, 0.33f, 0.30f));  // 어두운 바닥
            s_CoverMat = MakeMat("CQB_Cover", new Color(0.40f, 0.38f, 0.35f));  // 엄폐물
        }

        static Material MakeMat(string matName, Color col)
        {
            Shader urp = Shader.Find("Universal Render Pipeline/Lit");
            if (urp == null) urp = Shader.Find("Standard");
            var mat = new Material(urp) { name = matName, color = col };
            return mat;
        }
    }
}
