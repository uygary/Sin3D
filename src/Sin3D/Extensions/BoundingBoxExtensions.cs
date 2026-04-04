using Microsoft.Xna.Framework;

namespace Sin3D.Extensions;

public static class BoundingBoxExtensions
{
    extension(in BoundingBox boundingBox)
    {
        /// <summary>
        ///   Fill the first 8 places of an array of <see cref="T:Microsoft.Xna.Framework.Vector3" />
        ///   with the corners of this <see cref="T:Microsoft.Xna.Framework.BoundingBox" />.
        /// </summary>
        /// <param name="corners">The array to fill.</param>
        /// <exception cref="T:System.ArgumentNullException">If <paramref name="corners" /> is <code>null</code>.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        ///   If <paramref name="corners" /> has a length of less than 8.
        /// </exception>
        public void ReadonlyGetCorners(Vector3[] corners)
        {
            if (corners == null)
            {
                throw new ArgumentNullException(nameof(corners));
            }
            if (corners.Length < 8)
            {
                throw new ArgumentOutOfRangeException(nameof(corners), "Not Enought Corners");
            }
            
            corners[0].X = boundingBox.Min.X;
            corners[0].Y = boundingBox.Max.Y;
            corners[0].Z = boundingBox.Max.Z;
            corners[1].X = boundingBox.Max.X;
            corners[1].Y = boundingBox.Max.Y;
            corners[1].Z = boundingBox.Max.Z;
            corners[2].X = boundingBox.Max.X;
            corners[2].Y = boundingBox.Min.Y;
            corners[2].Z = boundingBox.Max.Z;
            corners[3].X = boundingBox.Min.X;
            corners[3].Y = boundingBox.Min.Y;
            corners[3].Z = boundingBox.Max.Z;
            corners[4].X = boundingBox.Min.X;
            corners[4].Y = boundingBox.Max.Y;
            corners[4].Z = boundingBox.Min.Z;
            corners[5].X = boundingBox.Max.X;
            corners[5].Y = boundingBox.Max.Y;
            corners[5].Z = boundingBox.Min.Z;
            corners[6].X = boundingBox.Max.X;
            corners[6].Y = boundingBox.Min.Y;
            corners[6].Z = boundingBox.Min.Z;
            corners[7].X = boundingBox.Min.X;
            corners[7].Y = boundingBox.Min.Y;
            corners[7].Z = boundingBox.Min.Z;
        }
    }
}