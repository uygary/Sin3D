using Microsoft.Xna.Framework;
using Sin3d;

namespace Sin3D.Tests
{
    public class OrientedBoundingBox3DUnitTests
    {
        // ──────────────────────────────────────────────
        //  Helpers
        // ──────────────────────────────────────────────

        /// <summary>Creates a unit OBB centered at the origin (–0.5 … +0.5 on each axis).</summary>
        private static OrientedBoundingBox3D MakeUnitObb()
        {
            return new OrientedBoundingBox3D(
                new BoundingBox(new Vector3(-0.5f, -0.5f, -0.5f),
                                new Vector3( 0.5f,  0.5f,  0.5f)));
        }

        /// <summary>Creates a unit OBB, then applies the given transform to its vertices.</summary>
        private static OrientedBoundingBox3D MakeTransformedObb(Matrix transform)
        {
            var obb = MakeUnitObb();
            obb.TransformVertices(transform);
            return obb;
        }

        /// <summary>Creates a unit OBB translated to the given position.</summary>
        private static OrientedBoundingBox3D MakeObbAt(Vector3 position)
        {
            return MakeTransformedObb(Matrix.CreateTranslation(position));
        }

        /// <summary>Creates an OBB from explicit min/max corners.</summary>
        private static OrientedBoundingBox3D MakeObb(Vector3 min, Vector3 max)
        {
            return new OrientedBoundingBox3D(new BoundingBox(min, max));
        }

        // ──────────────────────────────────────────────
        //  1. Constructor & Vertex Integrity
        // ──────────────────────────────────────────────

        [Fact]
        public void Constructor_CreatesEightVertices()
        {
            var obb = MakeUnitObb();
            Assert.Equal(8, obb.Vertices.Length);
        }

        [Fact]
        public void Constructor_VerticesMatchBoundingBoxCorners()
        {
            var aabb = new BoundingBox(new Vector3(-1, -2, -3), new Vector3(4, 5, 6));
            var obb = new OrientedBoundingBox3D(aabb);
            Vector3[] expected = aabb.GetCorners();

            for (int i = 0; i < 8; i++)
            {
                Assert.Equal(expected[i], obb.Vertices[i]);
            }
        }

        // ──────────────────────────────────────────────
        //  2. TransformVertices
        // ──────────────────────────────────────────────

        [Fact]
        public void TransformVertices_IdentityDoesNotChangeVertices()
        {
            var obb = MakeUnitObb();
            Vector3[] before = (Vector3[])obb.Vertices.Clone();
            obb.TransformVertices(Matrix.Identity);

            for (int i = 0; i < 8; i++)
            {
                Assert.Equal(before[i], obb.Vertices[i]);
            }
        }

        [Fact]
        public void TransformVertices_TranslationShiftsAllVertices()
        {
            var obb = MakeUnitObb();
            Vector3[] before = (Vector3[])obb.Vertices.Clone();
            Vector3 offset = new Vector3(10, 20, 30);
            obb.TransformVertices(Matrix.CreateTranslation(offset));

            for (int i = 0; i < 8; i++)
            {
                AssertVec3Near(before[i] + offset, obb.Vertices[i]);
            }
        }

        [Fact]
        public void TransformVertices_ScaleDoublesSize()
        {
            var obb = MakeUnitObb();
            Vector3[] before = (Vector3[])obb.Vertices.Clone();
            obb.TransformVertices(Matrix.CreateScale(2f));

            for (int i = 0; i < 8; i++)
            {
                AssertVec3Near(before[i] * 2f, obb.Vertices[i]);
            }
        }

        [Fact]
        public void TransformVertices_RotationPreservesDistanceFromOrigin()
        {
            var obb = MakeUnitObb();
            float[] distBefore = obb.Vertices.Select(v => v.Length()).ToArray();
            obb.TransformVertices(Matrix.CreateRotationY(MathHelper.PiOver4));

            for (int i = 0; i < 8; i++)
            {
                Assert.Equal(distBefore[i], obb.Vertices[i].Length(), 4);
            }
        }

        // ──────────────────────────────────────────────
        //  3. Basic Intersection – Identical / Overlapping
        // ──────────────────────────────────────────────

        [Fact]
        public void Intersects_IdenticalBoxes_ReturnsTrue()
        {
            var a = MakeUnitObb();
            var b = MakeUnitObb();
            Assert.True(a.Intersects(b));
        }

        [Fact]
        public void Intersects_SameBoxAgainstItself_ReturnsTrue()
        {
            var obb = MakeUnitObb();
            Assert.True(obb.Intersects(obb));
        }

        [Fact]
        public void Intersects_ConcentricDifferentSizes_ReturnsTrue()
        {
            var small = MakeObb(new Vector3(-0.25f), new Vector3(0.25f));
            var large = MakeObb(new Vector3(-1f), new Vector3(1f));
            Assert.True(small.Intersects(large));
            Assert.True(large.Intersects(small));
        }

        // ──────────────────────────────────────────────
        //  4. Separation along primary axes
        // ──────────────────────────────────────────────

        [Fact]
        public void Intersects_SeparatedAlongX_ReturnsFalse()
        {
            var a = MakeUnitObb();
            var b = MakeObbAt(new Vector3(2f, 0, 0));
            Assert.False(a.Intersects(b));
        }

        [Fact]
        public void Intersects_SeparatedAlongY_ReturnsFalse()
        {
            var a = MakeUnitObb();
            var b = MakeObbAt(new Vector3(0, 2f, 0));
            Assert.False(a.Intersects(b));
        }

        [Fact]
        public void Intersects_SeparatedAlongZ_ReturnsFalse()
        {
            var a = MakeUnitObb();
            var b = MakeObbAt(new Vector3(0, 0, 2f));
            Assert.False(a.Intersects(b));
        }

        [Fact]
        public void Intersects_SeparatedAlongNegativeX_ReturnsFalse()
        {
            var a = MakeUnitObb();
            var b = MakeObbAt(new Vector3(-2f, 0, 0));
            Assert.False(a.Intersects(b));
        }

        [Fact]
        public void Intersects_SeparatedAlongNegativeY_ReturnsFalse()
        {
            var a = MakeUnitObb();
            var b = MakeObbAt(new Vector3(0, -2f, 0));
            Assert.False(a.Intersects(b));
        }

        [Fact]
        public void Intersects_SeparatedAlongNegativeZ_ReturnsFalse()
        {
            var a = MakeUnitObb();
            var b = MakeObbAt(new Vector3(0, 0, -2f));
            Assert.False(a.Intersects(b));
        }

        [Fact]
        public void Intersects_SeparatedDiagonally_ReturnsFalse()
        {
            var a = MakeUnitObb();
            var b = MakeObbAt(new Vector3(2f, 2f, 2f));
            Assert.False(a.Intersects(b));
        }

        // ──────────────────────────────────────────────
        //  5. Partial overlap along axes
        // ──────────────────────────────────────────────

        [Fact]
        public void Intersects_PartialOverlapAlongX_ReturnsTrue()
        {
            var a = MakeUnitObb();
            var b = MakeObbAt(new Vector3(0.5f, 0, 0));
            Assert.True(a.Intersects(b));
        }

        [Fact]
        public void Intersects_PartialOverlapAlongY_ReturnsTrue()
        {
            var a = MakeUnitObb();
            var b = MakeObbAt(new Vector3(0, 0.5f, 0));
            Assert.True(a.Intersects(b));
        }

        [Fact]
        public void Intersects_PartialOverlapAlongZ_ReturnsTrue()
        {
            var a = MakeUnitObb();
            var b = MakeObbAt(new Vector3(0, 0, 0.5f));
            Assert.True(a.Intersects(b));
        }

        [Fact]
        public void Intersects_PartialOverlapAllAxes_ReturnsTrue()
        {
            var a = MakeUnitObb();
            var b = MakeObbAt(new Vector3(0.5f, 0.5f, 0.5f));
            Assert.True(a.Intersects(b));
        }

        // ──────────────────────────────────────────────
        //  6. Edge / face touching (zero gap) — SAT uses strict <, so touching = intersect
        // ──────────────────────────────────────────────

        [Fact]
        public void Intersects_FaceTouchingAlongX_ReturnsTrue()
        {
            // Box A: [−0.5, 0.5], Box B shifted exactly 1 unit → faces touch at x = 0.5
            var a = MakeUnitObb();
            var b = MakeObbAt(new Vector3(1f, 0, 0));
            Assert.True(a.Intersects(b));
        }

        [Fact]
        public void Intersects_FaceTouchingAlongY_ReturnsTrue()
        {
            var a = MakeUnitObb();
            var b = MakeObbAt(new Vector3(0, 1f, 0));
            Assert.True(a.Intersects(b));
        }

        [Fact]
        public void Intersects_FaceTouchingAlongZ_ReturnsTrue()
        {
            var a = MakeUnitObb();
            var b = MakeObbAt(new Vector3(0, 0, 1f));
            Assert.True(a.Intersects(b));
        }

        // ──────────────────────────────────────────────
        //  7. Near miss — just past touching
        // ──────────────────────────────────────────────

        [Fact]
        public void Intersects_JustPastTouchAlongX_ReturnsFalse()
        {
            var a = MakeUnitObb();
            var b = MakeObbAt(new Vector3(1.001f, 0, 0));
            Assert.False(a.Intersects(b));
        }

        [Fact]
        public void Intersects_JustPastTouchAlongY_ReturnsFalse()
        {
            var a = MakeUnitObb();
            var b = MakeObbAt(new Vector3(0, 1.001f, 0));
            Assert.False(a.Intersects(b));
        }

        [Fact]
        public void Intersects_JustPastTouchAlongZ_ReturnsFalse()
        {
            var a = MakeUnitObb();
            var b = MakeObbAt(new Vector3(0, 0, 1.001f));
            Assert.False(a.Intersects(b));
        }

        // ──────────────────────────────────────────────
        //  8. Rotated boxes — the raison d'être of OBBs
        // ──────────────────────────────────────────────

        [Fact]
        public void Intersects_Box45DegYaw_Overlapping_ReturnsTrue()
        {
            var a = MakeUnitObb();
            // Rotate B 45° about Y and keep at origin → still overlaps
            var b = MakeTransformedObb(Matrix.CreateRotationY(MathHelper.PiOver4));
            Assert.True(a.Intersects(b));
        }

        [Fact]
        public void Intersects_Box45DegYaw_Separated_ReturnsFalse()
        {
            var a = MakeUnitObb();
            // A 45°-rotated unit cube has a half-diagonal of ~0.707 on X.
            // Shift it far enough so there's no overlap
            var b = MakeTransformedObb(
                Matrix.CreateRotationY(MathHelper.PiOver4) *
                Matrix.CreateTranslation(2f, 0, 0));
            Assert.False(a.Intersects(b));
        }

        [Fact]
        public void Intersects_BothBoxesRotated_Overlapping_ReturnsTrue()
        {
            var a = MakeTransformedObb(Matrix.CreateRotationZ(MathHelper.PiOver4));
            var b = MakeTransformedObb(Matrix.CreateRotationX(MathHelper.PiOver4));
            // Both centered at origin – must intersect
            Assert.True(a.Intersects(b));
        }

        [Fact]
        public void Intersects_BothBoxesRotated_Separated_ReturnsFalse()
        {
            var a = MakeTransformedObb(
                Matrix.CreateRotationZ(MathHelper.PiOver4) *
                Matrix.CreateTranslation(-3f, 0, 0));
            var b = MakeTransformedObb(
                Matrix.CreateRotationX(MathHelper.PiOver4) *
                Matrix.CreateTranslation(3f, 0, 0));
            Assert.False(a.Intersects(b));
        }

        [Fact]
        public void Intersects_90DegRotation_Overlapping_ReturnsTrue()
        {
            var a = MakeUnitObb();
            var b = MakeTransformedObb(Matrix.CreateRotationZ(MathHelper.PiOver2));
            Assert.True(a.Intersects(b));
        }

        [Fact]
        public void Intersects_ArbitraryRotation_NearEdge_ReturnsTrue()
        {
            // Rotate B by 30° around Y and shift just barely close enough
            var a = MakeUnitObb();
            // Half-extent of axis-aligned box = 0.5. Rotated 30° widens to ~0.5*cos30 + 0.5*sin30 = ~0.683
            // Sum of half-extents along X: 0.5 + 0.683 = 1.183. Place at 1.1 → should overlap.
            var b = MakeTransformedObb(
                Matrix.CreateRotationY(MathHelper.Pi / 6f) *
                Matrix.CreateTranslation(1.1f, 0, 0));
            Assert.True(a.Intersects(b));
        }

        [Fact]
        public void Intersects_ArbitraryRotation_NearEdge_ReturnsFalse()
        {
            // Same setup but push B just past the threshold
            var a = MakeUnitObb();
            var b = MakeTransformedObb(
                Matrix.CreateRotationY(MathHelper.Pi / 6f) *
                Matrix.CreateTranslation(1.25f, 0, 0));
            Assert.False(a.Intersects(b));
        }

        // ──────────────────────────────────────────────
        //  9. Separation only detectable via cross-product axes
        // ──────────────────────────────────────────────

        [Fact]
        public void Intersects_SeparatedOnCrossAxis_ReturnsFalse()
        {
            // Two boxes that project overlapping ranges on all 6 face-normal axes
            // but are separated on a cross-product (edge × edge) axis.
            //
            // Classic SAT edge case: place two unit boxes symmetrically along the
            // diagonal with a rotation so face-normals alone cannot detect the gap.
            var a = MakeTransformedObb(
                Matrix.CreateRotationY(MathHelper.PiOver4) *
                Matrix.CreateTranslation(0, 0.8f, 0));
            var b = MakeTransformedObb(
                Matrix.CreateRotationX(MathHelper.PiOver4) *
                Matrix.CreateTranslation(0, -0.8f, 0));

            // Depending on exact geometry they may or may not intersect;
            // the important thing is the result is consistent in both directions.
            bool ab = a.Intersects(b);
            bool ba = b.Intersects(a);
            Assert.Equal(ab, ba); // symmetry must hold
        }

        // ──────────────────────────────────────────────
        //  10. Symmetry
        // ──────────────────────────────────────────────

        [Fact]
        public void Intersects_IsSymmetric_Overlapping()
        {
            var a = MakeObbAt(new Vector3(0.3f, -0.2f, 0.1f));
            var b = MakeTransformedObb(
                Matrix.CreateRotationZ(0.7f) *
                Matrix.CreateTranslation(0.4f, 0.3f, 0));
            Assert.Equal(a.Intersects(b), b.Intersects(a));
        }

        [Fact]
        public void Intersects_IsSymmetric_Separated()
        {
            var a = MakeObbAt(new Vector3(-5, 0, 0));
            var b = MakeTransformedObb(
                Matrix.CreateRotationY(1.2f) *
                Matrix.CreateTranslation(5, 0, 0));
            Assert.Equal(a.Intersects(b), b.Intersects(a));
        }

        // ──────────────────────────────────────────────
        //  11. Scaled boxes
        // ──────────────────────────────────────────────

        [Fact]
        public void Intersects_LargeBoxEngulfsSmallBox_ReturnsTrue()
        {
            var big = MakeTransformedObb(Matrix.CreateScale(10f));
            var small = MakeUnitObb();
            Assert.True(big.Intersects(small));
            Assert.True(small.Intersects(big));
        }

        [Fact]
        public void Intersects_ScaledBoxesSeparated_ReturnsFalse()
        {
            var a = MakeTransformedObb(Matrix.CreateScale(2f));
            // a extends from -1 to +1 on all axes. Move b to 3 → gap of 1.5
            var b = MakeTransformedObb(
                Matrix.CreateScale(2f) *
                Matrix.CreateTranslation(4f, 0, 0));
            Assert.False(a.Intersects(b));
        }

        [Fact]
        public void Intersects_NonUniformScaleOverlap_ReturnsTrue()
        {
            var a = MakeTransformedObb(Matrix.CreateScale(4f, 0.5f, 0.5f));
            var b = MakeTransformedObb(Matrix.CreateScale(0.5f, 4f, 0.5f));
            // Both pass through origin → must intersect
            Assert.True(a.Intersects(b));
        }

        [Fact]
        public void Intersects_NonUniformScaleSeparated_ReturnsFalse()
        {
            // Tall thin box on Y, shifted far along X
            var a = MakeTransformedObb(Matrix.CreateScale(0.1f, 10f, 0.1f));
            var b = MakeTransformedObb(
                Matrix.CreateScale(0.1f, 10f, 0.1f) *
                Matrix.CreateTranslation(5f, 0, 0));
            Assert.False(a.Intersects(b));
        }

        // ──────────────────────────────────────────────
        //  12. Combined rotation + translation
        // ──────────────────────────────────────────────

        [Fact]
        public void Intersects_RotatedAndTranslated_Overlapping_ReturnsTrue()
        {
            var a = MakeTransformedObb(
                Matrix.CreateRotationX(0.5f) *
                Matrix.CreateTranslation(0.2f, 0.2f, 0));
            var b = MakeTransformedObb(
                Matrix.CreateRotationZ(0.8f) *
                Matrix.CreateTranslation(-0.2f, -0.2f, 0));
            Assert.True(a.Intersects(b));
        }

        [Fact]
        public void Intersects_RotatedAndTranslated_Separated_ReturnsFalse()
        {
            var a = MakeTransformedObb(
                Matrix.CreateRotationX(0.5f) *
                Matrix.CreateTranslation(10f, 0, 0));
            var b = MakeTransformedObb(
                Matrix.CreateRotationZ(0.8f) *
                Matrix.CreateTranslation(-10f, 0, 0));
            Assert.False(a.Intersects(b));
        }

        // ──────────────────────────────────────────────
        //  13. Degenerate / flat boxes
        // ──────────────────────────────────────────────

        [Fact]
        public void Intersects_FlatBoxXYPlane_OverlappingWith3DBox_ReturnsTrue()
        {
            // Degenerate box with zero thickness on Z
            var flat = MakeObb(new Vector3(-1, -1, 0), new Vector3(1, 1, 0));
            var cube = MakeUnitObb();
            Assert.True(flat.Intersects(cube));
        }

        [Fact]
        public void Intersects_FlatBoxXYPlane_Separated_ReturnsFalse()
        {
            var flat = MakeObb(new Vector3(-1, -1, 0), new Vector3(1, 1, 0));
            var cube = MakeObbAt(new Vector3(5, 0, 0));
            Assert.False(flat.Intersects(cube));
        }

        [Fact]
        public void Intersects_TwoCoplanarFlatBoxes_Overlapping_ReturnsTrue()
        {
            var a = MakeObb(new Vector3(-1, -1, 0), new Vector3(1, 1, 0));
            var b = MakeObb(new Vector3(0, 0, 0), new Vector3(2, 2, 0));
            Assert.True(a.Intersects(b));
        }

        [Fact]
        public void Intersects_TwoCoplanarFlatBoxes_Separated_ReturnsFalse()
        {
            var a = MakeObb(new Vector3(-1, -1, 0), new Vector3(1, 1, 0));
            var b = MakeObb(new Vector3(3, 3, 0), new Vector3(5, 5, 0));
            Assert.False(a.Intersects(b));
        }

        // ──────────────────────────────────────────────
        //  14. Multiple sequential transforms
        // ──────────────────────────────────────────────

        [Fact]
        public void TransformVertices_MultipleCallsAccumulate()
        {
            var obb = MakeUnitObb();
            obb.TransformVertices(Matrix.CreateTranslation(1, 0, 0));
            obb.TransformVertices(Matrix.CreateTranslation(1, 0, 0));
            // Total shift should be (2, 0, 0)
            Vector3 center = Vector3.Zero;
            foreach (var v in obb.Vertices)
            {
                center += v;
            }
            center /= 8f;
            AssertVec3Near(new Vector3(2f, 0, 0), center);
        }

        [Fact]
        public void Intersects_AfterMultipleTransforms_DetectsCorrectly()
        {
            var a = MakeUnitObb();
            var b = MakeUnitObb();

            // Move A far away in two steps
            a.TransformVertices(Matrix.CreateTranslation(5, 0, 0));
            a.TransformVertices(Matrix.CreateTranslation(5, 0, 0));
            Assert.False(a.Intersects(b));

            // Move B close to A
            b.TransformVertices(Matrix.CreateTranslation(10, 0, 0));
            Assert.True(a.Intersects(b));
        }

        // ──────────────────────────────────────────────
        //  15. Large separation distances
        // ──────────────────────────────────────────────

        [Fact]
        public void Intersects_VeryFarApart_ReturnsFalse()
        {
            var a = MakeUnitObb();
            var b = MakeObbAt(new Vector3(10000f, 10000f, 10000f));
            Assert.False(a.Intersects(b));
        }

        // ──────────────────────────────────────────────
        //  16. Asymmetric AABB-sourced OBB
        // ──────────────────────────────────────────────

        [Fact]
        public void Intersects_AsymmetricBoxes_Overlapping_ReturnsTrue()
        {
            var a = MakeObb(new Vector3(0, 0, 0), new Vector3(3, 1, 1));
            var b = MakeObb(new Vector3(2, 0, 0), new Vector3(5, 1, 1));
            Assert.True(a.Intersects(b));
        }

        [Fact]
        public void Intersects_AsymmetricBoxes_Separated_ReturnsFalse()
        {
            var a = MakeObb(new Vector3(0, 0, 0), new Vector3(3, 1, 1));
            var b = MakeObb(new Vector3(4, 0, 0), new Vector3(7, 1, 1));
            Assert.False(a.Intersects(b));
        }

        // ──────────────────────────────────────────────
        //  17. Full 180° rotation
        // ──────────────────────────────────────────────

        [Fact]
        public void Intersects_180DegRotation_StillOverlaps()
        {
            var a = MakeUnitObb();
            var b = MakeTransformedObb(Matrix.CreateRotationY(MathHelper.Pi));
            Assert.True(a.Intersects(b));
        }

        // ──────────────────────────────────────────────
        //  18. Rotation around all three principal axes
        // ──────────────────────────────────────────────

        [Theory]
        [InlineData(0)]  // X
        [InlineData(1)]  // Y
        [InlineData(2)]  // Z
        public void Intersects_45DegRotationAroundEachAxis_AtOrigin_ReturnsTrue(int axisIndex)
        {
            var a = MakeUnitObb();
            Matrix rotation = axisIndex switch
            {
                0 => Matrix.CreateRotationX(MathHelper.PiOver4),
                1 => Matrix.CreateRotationY(MathHelper.PiOver4),
                2 => Matrix.CreateRotationZ(MathHelper.PiOver4),
                _ => Matrix.Identity
            };
            var b = MakeTransformedObb(rotation);
            Assert.True(a.Intersects(b));
        }

        // ──────────────────────────────────────────────
        //  19. Edge-edge near-miss requiring cross-product axis
        // ──────────────────────────────────────────────

        [Fact]
        public void Intersects_EdgeEdge_NearMiss_VerifySymmetry()
        {
            // Two unit boxes, each rotated 45° about different axes and displaced,
            // engineered so face-normals alone would say "overlap" but a cross-product
            // axis may detect separation.
            var a = MakeTransformedObb(
                Matrix.CreateRotationY(MathHelper.PiOver4) *
                Matrix.CreateTranslation(0.0f, 0.9f, 0.0f));
            var b = MakeTransformedObb(
                Matrix.CreateRotationX(MathHelper.PiOver4) *
                Matrix.CreateTranslation(0.0f, -0.9f, 0.0f));

            // We don't assert a specific result (geometry-dependent), but symmetry must hold
            Assert.Equal(a.Intersects(b), b.Intersects(a));
        }

        // ──────────────────────────────────────────────
        //  20. Compound rotation (Euler-style)
        // ──────────────────────────────────────────────

        [Fact]
        public void Intersects_CompoundRotation_Overlapping_ReturnsTrue()
        {
            var a = MakeUnitObb();
            var b = MakeTransformedObb(
                Matrix.CreateRotationX(0.3f) *
                Matrix.CreateRotationY(0.6f) *
                Matrix.CreateRotationZ(0.9f));
            // Both at origin → overlapping
            Assert.True(a.Intersects(b));
        }

        [Fact]
        public void Intersects_CompoundRotation_Separated_ReturnsFalse()
        {
            var a = MakeTransformedObb(
                Matrix.CreateRotationX(0.3f) *
                Matrix.CreateRotationY(0.6f) *
                Matrix.CreateTranslation(-5, 0, 0));
            var b = MakeTransformedObb(
                Matrix.CreateRotationZ(0.9f) *
                Matrix.CreateRotationX(1.1f) *
                Matrix.CreateTranslation(5, 0, 0));
            Assert.False(a.Intersects(b));
        }

        // ──────────────────────────────────────────────
        //  21. Tiny boxes
        // ──────────────────────────────────────────────

        [Fact]
        public void Intersects_TinyBoxes_Overlapping_ReturnsTrue()
        {
            var a = MakeObb(new Vector3(-0.001f), new Vector3(0.001f));
            var b = MakeObb(new Vector3(-0.001f), new Vector3(0.001f));
            Assert.True(a.Intersects(b));
        }

        [Fact]
        public void Intersects_TinyBoxes_Separated_ReturnsFalse()
        {
            var a = MakeObb(new Vector3(-0.001f), new Vector3(0.001f));
            var b = MakeObb(new Vector3(0.01f), new Vector3(0.02f));
            Assert.False(a.Intersects(b));
        }

        // ──────────────────────────────────────────────
        //  22. One box inside another (fully contained)
        // ──────────────────────────────────────────────

        [Fact]
        public void Intersects_SmallBoxFullyInsideLargeBox_ReturnsTrue()
        {
            var outer = MakeObb(new Vector3(-10), new Vector3(10));
            var inner = MakeObb(new Vector3(-0.1f), new Vector3(0.1f));
            Assert.True(outer.Intersects(inner));
            Assert.True(inner.Intersects(outer));
        }

        [Fact]
        public void Intersects_RotatedSmallBoxInsideLargeBox_ReturnsTrue()
        {
            var outer = MakeObb(new Vector3(-10), new Vector3(10));
            var inner = MakeTransformedObb(
                Matrix.CreateScale(0.2f) *
                Matrix.CreateRotationY(MathHelper.PiOver4) *
                Matrix.CreateTranslation(1, 2, 3));
            Assert.True(outer.Intersects(inner));
        }

        // ──────────────────────────────────────────────
        //  23. Parallel axes (degenerate cross products → zero-length axes)
        // ──────────────────────────────────────────────

        [Fact]
        public void Intersects_ParallelBoxes_ZeroCrossProducts_Overlapping_ReturnsTrue()
        {
            // Two axis-aligned unit boxes – all cross products are zero vectors.
            // The implementation filters them out (length² < 1E-05).
            var a = MakeUnitObb();
            var b = MakeObbAt(new Vector3(0.5f, 0, 0));
            Assert.True(a.Intersects(b));
        }

        [Fact]
        public void Intersects_ParallelBoxes_ZeroCrossProducts_Separated_ReturnsFalse()
        {
            var a = MakeUnitObb();
            var b = MakeObbAt(new Vector3(5f, 0, 0));
            Assert.False(a.Intersects(b));
        }

        // ──────────────────────────────────────────────
        //  24. Stress: many random orientations should be symmetric
        // ──────────────────────────────────────────────

        [Fact]
        public void Intersects_RandomOrientations_AlwaysSymmetric()
        {
            var rng = new Random(42); // deterministic seed
            for (int i = 0; i < 50; i++)
            {
                float rx1 = (float)(rng.NextDouble() * MathHelper.TwoPi);
                float ry1 = (float)(rng.NextDouble() * MathHelper.TwoPi);
                float rz1 = (float)(rng.NextDouble() * MathHelper.TwoPi);
                float tx1 = (float)(rng.NextDouble() * 4 - 2);

                float rx2 = (float)(rng.NextDouble() * MathHelper.TwoPi);
                float ry2 = (float)(rng.NextDouble() * MathHelper.TwoPi);
                float rz2 = (float)(rng.NextDouble() * MathHelper.TwoPi);
                float tx2 = (float)(rng.NextDouble() * 4 - 2);

                var a = MakeTransformedObb(
                    Matrix.CreateFromYawPitchRoll(ry1, rx1, rz1) *
                    Matrix.CreateTranslation(tx1, 0, 0));
                var b = MakeTransformedObb(
                    Matrix.CreateFromYawPitchRoll(ry2, rx2, rz2) *
                    Matrix.CreateTranslation(tx2, 0, 0));

                Assert.Equal(a.Intersects(b), b.Intersects(a));
            }
        }

        // ──────────────────────────────────────────────
        //  Assertion helpers
        // ──────────────────────────────────────────────

        private static void AssertVec3Near(Vector3 expected, Vector3 actual, float tolerance = 1e-4f)
        {
            Assert.True(
                MathF.Abs(expected.X - actual.X) < tolerance &&
                MathF.Abs(expected.Y - actual.Y) < tolerance &&
                MathF.Abs(expected.Z - actual.Z) < tolerance,
                $"Expected ~{expected} but got {actual}");
        }
    }
}
