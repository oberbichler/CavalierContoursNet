using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace CavalierContours.Core
{
    /// <summary>
    /// Basic geometric and angle math helpers used throughout the library.
    /// </summary>
    public static class BaseMath
    {
        /// <summary>
        /// Returns the (min, max) values from <paramref name="v1"/> and <paramref name="v2"/>.
        /// </summary>
        /// <typeparam name="T">Floating point type being compared.</typeparam>
        /// <param name="v1">First value.</param>
        /// <param name="v2">Second value.</param>
        /// <returns>A tuple with the smaller value first and the larger value second.</returns>
        /// <remarks>
        /// Ordering follows Rust's <c>if v1 &lt; v2 { (v1, v2) } else { (v2, v1) }</c>, including
        /// its <c>NaN</c> behaviour: any comparison involving <c>NaN</c> is false, so the else
        /// branch is taken and the arguments come back swapped.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (T Min, T Max) MinMax<T>(T v1, T v2) where T : struct, IFloatingPointIeee754<T>
        {
            // Matches Rust `if v1 < v2 { (v1, v2) } else { (v2, v1) }`, including the NaN
            // behaviour: every comparison involving NaN is false, so the else branch is taken.
            // CompareTo would order NaN below everything and give the opposite result.
            return v1 < v2 ? (v1, v2) : (v2, v1);
        }

        /// <summary>
        /// Normalize radians to be between <c>0</c> and <c>2PI</c>, e.g. <c>-PI/4</c> becomes
        /// <c>7PI/4</c> and <c>5PI</c> becomes <c>PI</c>.
        /// </summary>
        /// <typeparam name="T">Floating point type of the angle.</typeparam>
        /// <param name="angle">The angle in radians to normalize.</param>
        /// <returns>The normalized angle in radians.</returns>
        /// <remarks>
        /// Anything already between <c>0</c> and <c>2PI</c> inclusive is left unchanged.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T NormalizeRadians<T>(T angle) where T : struct, IFloatingPointIeee754<T>
        {
            T tau = T.Tau;
            if (angle >= T.Zero && angle <= tau)
            {
                return angle;
            }
            return angle - T.Floor(angle / tau) * tau;
        }

        /// <summary>
        /// Returns the smaller difference between two angles.
        /// </summary>
        /// <typeparam name="T">Floating point type of the angles.</typeparam>
        /// <param name="angle1">The angle measured from, in radians.</param>
        /// <param name="angle2">The angle measured to, in radians.</param>
        /// <returns>
        /// The signed difference; negative if
        /// <c>NormalizeRadians(angle2 - angle1) &gt; PI</c>. See
        /// <see cref="NormalizeRadians{T}(T)"/> for more information.
        /// </returns>
        /// <remarks>
        /// When the two angles differ by exactly <c>PI</c> the result is positive regardless of
        /// argument order; use <see cref="DeltaAngleSigned{T}(T, T, bool)"/> to force a polarity.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T DeltaAngle<T>(T angle1, T angle2) where T : struct, IFloatingPointIeee754<T>
        {
            T diff = NormalizeRadians(angle2 - angle1);
            if (diff > T.Pi)
            {
                diff -= T.Tau;
            }
            return diff;
        }

        /// <summary>
        /// Returns the smaller difference between two angles and applies the sign given.
        /// </summary>
        /// <typeparam name="T">Floating point type of the angles.</typeparam>
        /// <param name="angle1">The angle measured from, in radians.</param>
        /// <param name="angle2">The angle measured to, in radians.</param>
        /// <param name="negative">
        /// If <see langword="true"/> the result is always negative, otherwise always positive.
        /// </param>
        /// <returns>The magnitude of <see cref="DeltaAngle{T}(T, T)"/> with the sign applied.</returns>
        /// <remarks>
        /// This is useful for ensuring a particular polarity for edge cases: if
        /// <paramref name="angle1"/> is 0 and <paramref name="angle2"/> is <c>PI</c> then the delta
        /// angle could be considered positive or negative, while
        /// <see cref="DeltaAngle{T}(T, T)"/> always returns positive.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T DeltaAngleSigned<T>(T angle1, T angle2, bool negative) where T : struct, IFloatingPointIeee754<T>
        {
            T diff = DeltaAngle(angle1, angle2);
            return negative ? -T.Abs(diff) : T.Abs(diff);
        }

        /// <summary>
        /// Tests if <paramref name="testAngle"/> is between a <paramref name="startAngle"/> and
        /// <paramref name="endAngle"/>.
        /// </summary>
        /// <typeparam name="T">Floating point type of the angles.</typeparam>
        /// <param name="testAngle">The angle to test, in radians.</param>
        /// <param name="startAngle">The start of the sweep, in radians.</param>
        /// <param name="endAngle">The end of the sweep, in radians.</param>
        /// <param name="epsilon">Tolerance making the test inclusive of the boundaries.</param>
        /// <returns><see langword="true"/> if the test angle lies within the sweep.</returns>
        /// <remarks>
        /// The test assumes a counter clockwise sweep from <paramref name="startAngle"/> to
        /// <paramref name="endAngle"/>. Going from <c>PI</c> to <c>PI/2</c> counter clockwise
        /// therefore sweeps across <c>0</c>. See <see cref="AngleIsBetween{T}(T, T, T)"/> to use
        /// the default fuzzy epsilon.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AngleIsBetweenEps<T>(T testAngle, T startAngle, T endAngle, T epsilon) where T : struct, IFloatingPointIeee754<T>
        {
            T endSweep = NormalizeRadians(endAngle - startAngle);
            T midSweep = NormalizeRadians(testAngle - startAngle);
            return midSweep < endSweep + epsilon;
        }

        /// <summary>
        /// Same as <see cref="AngleIsBetweenEps{T}(T, T, T, T)"/> using
        /// <see cref="Fuzzy{T}.Epsilon"/>.
        /// </summary>
        /// <typeparam name="T">Floating point type of the angles.</typeparam>
        /// <param name="testAngle">The angle to test, in radians.</param>
        /// <param name="startAngle">The start of the sweep, in radians.</param>
        /// <param name="endAngle">The end of the sweep, in radians.</param>
        /// <returns><see langword="true"/> if the test angle lies within the sweep.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AngleIsBetween<T>(T testAngle, T startAngle, T endAngle) where T : struct, IFloatingPointIeee754<T>
        {
            return AngleIsBetweenEps(testAngle, startAngle, endAngle, Fuzzy<T>.Epsilon);
        }

        /// <summary>
        /// Tests if <paramref name="testAngle"/> is within the <paramref name="sweepAngle"/>
        /// starting at <paramref name="startAngle"/>.
        /// </summary>
        /// <typeparam name="T">Floating point type of the angles.</typeparam>
        /// <param name="testAngle">The angle to test, in radians.</param>
        /// <param name="startAngle">The start of the sweep, in radians.</param>
        /// <param name="sweepAngle">
        /// The swept angle in radians; if positive the sweep is counter clockwise, otherwise it is
        /// clockwise.
        /// </param>
        /// <param name="epsilon">Tolerance controlling the fuzzy inclusion.</param>
        /// <returns><see langword="true"/> if the test angle lies within the sweep.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AngleIsWithinSweepEps<T>(T testAngle, T startAngle, T sweepAngle, T epsilon) where T : struct, IFloatingPointIeee754<T>
        {
            T endAngle = startAngle + sweepAngle;
            if (sweepAngle < T.Zero)
            {
                return AngleIsBetweenEps(testAngle, endAngle, startAngle, epsilon);
            }
            return AngleIsBetweenEps(testAngle, startAngle, endAngle, epsilon);
        }

        /// <summary>
        /// Same as <see cref="AngleIsWithinSweepEps{T}(T, T, T, T)"/> using
        /// <see cref="Fuzzy{T}.Epsilon"/>.
        /// </summary>
        /// <typeparam name="T">Floating point type of the angles.</typeparam>
        /// <param name="testAngle">The angle to test, in radians.</param>
        /// <param name="startAngle">The start of the sweep, in radians.</param>
        /// <param name="sweepAngle">
        /// The swept angle in radians; if positive the sweep is counter clockwise, otherwise it is
        /// clockwise.
        /// </param>
        /// <returns><see langword="true"/> if the test angle lies within the sweep.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AngleIsWithinSweep<T>(T testAngle, T startAngle, T sweepAngle) where T : struct, IFloatingPointIeee754<T>
        {
            return AngleIsWithinSweepEps(testAngle, startAngle, sweepAngle, Fuzzy<T>.Epsilon);
        }

        /// <summary>
        /// Returns the solutions to the quadratic equation
        /// <c>(-b +/- sqrt(b * b - 4 * a * c)) / (2 * a)</c>, with
        /// <paramref name="sqrtDiscriminant"/> defined as <c>sqrt(b * b - 4 * a * c)</c>.
        /// </summary>
        /// <typeparam name="T">Floating point type of the coefficients.</typeparam>
        /// <param name="a">The quadratic coefficient.</param>
        /// <param name="b">The linear coefficient.</param>
        /// <param name="c">The constant coefficient.</param>
        /// <param name="sqrtDiscriminant">The precomputed square root of the discriminant.</param>
        /// <returns>The two solutions to the quadratic equation.</returns>
        /// <remarks>
        /// The purpose of this function is to minimize error in the process of finding the
        /// solutions. Choosing the addition or subtraction branch based on the sign of
        /// <paramref name="b"/> avoids taking the difference of two floating point values that are
        /// very near each other in value; the second solution is then recovered from the product of
        /// the roots as <c>(c / a) / sol1</c>. In Debug builds the supplied
        /// <paramref name="sqrtDiscriminant"/> is asserted to match the coefficients.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (T, T) QuadraticSolutions<T>(T a, T b, T c, T sqrtDiscriminant) where T : struct, IFloatingPointIeee754<T>
        {
            T four = T.CreateChecked(4);
            T two = T.CreateChecked(2);
            Debug.Assert(
                T.Sqrt((b * b) - (four * a * c)).FuzzyEq(sqrtDiscriminant),
                "discriminant is not valid");
            
            T denom = two * a;
            T sol1 = b < T.Zero ? (-b + sqrtDiscriminant) / denom : (-b - sqrtDiscriminant) / denom;
            T sol2 = (c / a) / sol1;
            return (sol1, sol2);
        }

        /// <summary>
        /// Distance squared between the points <paramref name="p0"/> and <paramref name="p1"/>.
        /// </summary>
        /// <typeparam name="T">Floating point type of the point components.</typeparam>
        /// <param name="p0">First point.</param>
        /// <param name="p1">Second point.</param>
        /// <returns>The squared euclidean distance between the points.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T DistSquared<T>(Vector2<T> p0, Vector2<T> p1) where T : struct, IFloatingPointIeee754<T>
        {
            Vector2<T> d = p0 - p1;
            return d.Dot(d);
        }

        /// <summary>
        /// Angle of the direction vector described by <paramref name="p0"/> to
        /// <paramref name="p1"/>.
        /// </summary>
        /// <typeparam name="T">Floating point type of the point components.</typeparam>
        /// <param name="p0">Start point of the direction vector.</param>
        /// <param name="p1">End point of the direction vector.</param>
        /// <returns>The angle in radians as returned by <c>atan2</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Angle<T>(Vector2<T> p0, Vector2<T> p1) where T : struct, IFloatingPointIeee754<T>
        {
            return T.Atan2(p1.Y - p0.Y, p1.X - p0.X);
        }

        /// <summary>
        /// Midpoint of a line segment defined by <paramref name="p0"/> to <paramref name="p1"/>.
        /// </summary>
        /// <typeparam name="T">Floating point type of the point components.</typeparam>
        /// <param name="p0">Start point of the segment.</param>
        /// <param name="p1">End point of the segment.</param>
        /// <returns>The midpoint of the segment.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2<T> Midpoint<T>(Vector2<T> p0, Vector2<T> p1) where T : struct, IFloatingPointIeee754<T>
        {
            T two = T.CreateChecked(2);
            return new Vector2<T>((p0.X + p1.X) / two, (p0.Y + p1.Y) / two);
        }

        /// <summary>
        /// Returns the point on the circle with <paramref name="radius"/>,
        /// <paramref name="center"/>, and polar <paramref name="angle"/> in radians given.
        /// </summary>
        /// <typeparam name="T">Floating point type of the point components.</typeparam>
        /// <param name="radius">Radius of the circle.</param>
        /// <param name="center">Center of the circle.</param>
        /// <param name="angle">Polar angle in radians.</param>
        /// <returns>The point on the circle.</returns>
        /// <remarks>
        /// Sine and cosine are evaluated with separate <c>T.Sin</c> and <c>T.Cos</c> calls rather
        /// than <c>T.SinCos</c>. Rust's <c>f64::sin_cos</c> calls <c>sin</c> and <c>cos</c>
        /// individually, while <c>Math.SinCos</c> dispatches to the libc <c>sincos</c>, which
        /// differs by 1 ulp for roughly 1 in 700 arguments and would break bit-exactness against
        /// the Rust implementation.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2<T> PointOnCircle<T>(T radius, Vector2<T> center, T angle) where T : struct, IFloatingPointIeee754<T>
        {
            // Separate Sin and Cos, not T.SinCos: Rust's f64::sin_cos calls sin and cos
            // individually while Math.SinCos goes to libc sincos, which differs by 1 ulp for
            // roughly 1 in 700 arguments.
            T s = T.Sin(angle);
            T c = T.Cos(angle);
            return new Vector2<T>(center.X + radius * c, center.Y + radius * s);
        }

        /// <summary>
        /// Returns the point on the line segment going from <paramref name="p0"/> to
        /// <paramref name="p1"/> at parametric value <paramref name="t"/>.
        /// </summary>
        /// <typeparam name="T">Floating point type of the point components.</typeparam>
        /// <param name="p0">Start point of the segment.</param>
        /// <param name="p1">End point of the segment.</param>
        /// <param name="t">Parametric value, <c>0</c> at <paramref name="p0"/> and <c>1</c> at
        /// <paramref name="p1"/>.</param>
        /// <returns><c>p0 + t * (p1 - p0)</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2<T> PointFromParametric<T>(Vector2<T> p0, Vector2<T> p1, T t) where T : struct, IFloatingPointIeee754<T>
        {
            return p0 + (p1 - p0).Scale(t);
        }

        /// <summary>
        /// Returns the parametric value on the line segment going from <paramref name="p0"/> to
        /// <paramref name="p1"/> at the <paramref name="point"/> given.
        /// </summary>
        /// <typeparam name="T">Floating point type of the point components.</typeparam>
        /// <param name="p0">Start point of the segment.</param>
        /// <param name="p1">End point of the segment.</param>
        /// <param name="point">The point on the line to convert to a parametric value.</param>
        /// <param name="epsilon">
        /// Positional tolerance used by the Debug assertion that <paramref name="point"/> lies on
        /// the line.
        /// </param>
        /// <returns>The parametric value of <paramref name="point"/> along the segment.</returns>
        /// <remarks>
        /// This function assumes the <paramref name="point"/> is on the line, and properly handles
        /// the cases of vertical and horizontal lines by using the component with the largest
        /// difference in the calculation. That also avoids adding error caused by a small
        /// denominator.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ParametricFromPoint<T>(Vector2<T> p0, Vector2<T> p1, Vector2<T> point, T epsilon) where T : struct, IFloatingPointIeee754<T>
        {
            T xDiff = p1.X - p0.X;
            T yDiff = p1.Y - p0.Y;

            Debug.Assert(
                ((xDiff * (p0.Y - point.Y) - (p0.X - point.X) * yDiff) / T.Sqrt(xDiff * xDiff + yDiff * yDiff))
                .FuzzyEqZero(epsilon),
                "point does not lie on the line defined by p0 to p1"
            );

            if (T.Abs(xDiff) < T.Abs(yDiff))
            {
                return (point.Y - p0.Y) / yDiff;
            }
            else
            {
                return (point.X - p0.X) / xDiff;
            }
        }

        /// <summary>
        /// Returns the closest point on the line segment from <paramref name="p0"/> to
        /// <paramref name="p1"/> to the <paramref name="point"/> given.
        /// </summary>
        /// <typeparam name="T">Floating point type of the point components.</typeparam>
        /// <param name="p0">Start point of the segment.</param>
        /// <param name="p1">End point of the segment.</param>
        /// <param name="point">The point to find the closest point to.</param>
        /// <returns>
        /// The closest point, clamped to the segment end points when the projection falls outside
        /// of it.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2<T> LineSegClosestPoint<T>(Vector2<T> p0, Vector2<T> p1, Vector2<T> point) where T : struct, IFloatingPointIeee754<T>
        {
            Vector2<T> v = p1 - p0;
            Vector2<T> w = point - p0;
            T c1 = w.Dot(v);
            if (c1 < Fuzzy<T>.Epsilon)
            {
                return p0;
            }

            T c2 = v.LengthSquared();
            if (c2 < c1 + Fuzzy<T>.Epsilon)
            {
                return p1;
            }

            T b = c1 / c2;
            return p0 + v.Scale(b);
        }

        /// <summary>
        /// Helper function to avoid repeating code for the left and right side checks.
        /// </summary>
        /// <typeparam name="T">Floating point type of the point components.</typeparam>
        /// <param name="p0">Start point of the direction vector.</param>
        /// <param name="p1">End point of the direction vector.</param>
        /// <param name="point">The point being tested.</param>
        /// <returns>
        /// The perpendicular dot product of the direction vector <c>p1 - p0</c> with
        /// <c>point - p0</c>; positive when the point is left of the direction vector.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T PerpDotTestValue<T>(Vector2<T> p0, Vector2<T> p1, Vector2<T> point) where T : struct, IFloatingPointIeee754<T>
        {
            return (p1.X - p0.X) * (point.Y - p0.Y) - (p1.Y - p0.Y) * (point.X - p0.X);
        }

        /// <summary>
        /// Returns <see langword="true"/> if <paramref name="point"/> is left of a direction
        /// vector, where the direction vector is defined as <c>p1 - p0</c>.
        /// </summary>
        /// <typeparam name="T">Floating point type of the point components.</typeparam>
        /// <param name="p0">Start point of the direction vector.</param>
        /// <param name="p1">End point of the direction vector.</param>
        /// <param name="point">The point being tested.</param>
        /// <returns><see langword="true"/> if the point is strictly left of the direction vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsLeft<T>(Vector2<T> p0, Vector2<T> p1, Vector2<T> point) where T : struct, IFloatingPointIeee754<T>
        {
            return PerpDotTestValue(p0, p1, point) > T.Zero;
        }

        /// <summary>
        /// Same as <see cref="IsLeft{T}(Vector2{T}, Vector2{T}, Vector2{T})"/> but uses the
        /// <c>&gt;=</c> operator rather than <c>&gt;</c> for boundary inclusion.
        /// </summary>
        /// <typeparam name="T">Floating point type of the point components.</typeparam>
        /// <param name="p0">Start point of the direction vector.</param>
        /// <param name="p1">End point of the direction vector.</param>
        /// <param name="point">The point being tested.</param>
        /// <returns>
        /// <see langword="true"/> if the point is left of, or exactly on, the direction vector.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsLeftOrEqual<T>(Vector2<T> p0, Vector2<T> p1, Vector2<T> point) where T : struct, IFloatingPointIeee754<T>
        {
            return PerpDotTestValue(p0, p1, point) >= T.Zero;
        }

        /// <summary>
        /// Returns <see langword="true"/> if <paramref name="point"/> is left of, or fuzzy
        /// coincident with, the direction vector defined by <c>p1 - p0</c>.
        /// </summary>
        /// <typeparam name="T">Floating point type of the point components.</typeparam>
        /// <param name="p0">Start point of the direction vector.</param>
        /// <param name="p1">End point of the direction vector.</param>
        /// <param name="point">The point being tested.</param>
        /// <param name="epsilon">Controls the fuzzy compare, must be positive.</param>
        /// <returns>
        /// <see langword="true"/> if the point is left of or fuzzy coincident with the direction
        /// vector.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsLeftOrCoincidentEps<T>(Vector2<T> p0, Vector2<T> p1, Vector2<T> point, T epsilon) where T : struct, IFloatingPointIeee754<T>
        {
            Debug.Assert(epsilon > T.Zero);
            return PerpDotTestValue(p0, p1, point) > -epsilon;
        }

        /// <summary>
        /// Same as
        /// <see cref="IsLeftOrCoincidentEps{T}(Vector2{T}, Vector2{T}, Vector2{T}, T)"/> using
        /// <see cref="Fuzzy{T}.Epsilon"/>.
        /// </summary>
        /// <typeparam name="T">Floating point type of the point components.</typeparam>
        /// <param name="p0">Start point of the direction vector.</param>
        /// <param name="p1">End point of the direction vector.</param>
        /// <param name="point">The point being tested.</param>
        /// <returns>
        /// <see langword="true"/> if the point is left of or fuzzy coincident with the direction
        /// vector.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsLeftOrCoincident<T>(Vector2<T> p0, Vector2<T> p1, Vector2<T> point) where T : struct, IFloatingPointIeee754<T>
        {
            return IsLeftOrCoincidentEps(p0, p1, point, Fuzzy<T>.Epsilon);
        }

        /// <summary>
        /// Returns <see langword="true"/> if <paramref name="point"/> is right of, or fuzzy
        /// coincident with, the direction vector defined by <c>p1 - p0</c>.
        /// </summary>
        /// <typeparam name="T">Floating point type of the point components.</typeparam>
        /// <param name="p0">Start point of the direction vector.</param>
        /// <param name="p1">End point of the direction vector.</param>
        /// <param name="point">The point being tested.</param>
        /// <param name="epsilon">Controls the fuzzy compare, must be positive.</param>
        /// <returns>
        /// <see langword="true"/> if the point is right of or fuzzy coincident with the direction
        /// vector.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsRightOrCoincidentEps<T>(Vector2<T> p0, Vector2<T> p1, Vector2<T> point, T epsilon) where T : struct, IFloatingPointIeee754<T>
        {
            Debug.Assert(epsilon > T.Zero);
            return PerpDotTestValue(p0, p1, point) < epsilon;
        }

        /// <summary>
        /// Same as
        /// <see cref="IsRightOrCoincidentEps{T}(Vector2{T}, Vector2{T}, Vector2{T}, T)"/> using
        /// <see cref="Fuzzy{T}.Epsilon"/>.
        /// </summary>
        /// <typeparam name="T">Floating point type of the point components.</typeparam>
        /// <param name="p0">Start point of the direction vector.</param>
        /// <param name="p1">End point of the direction vector.</param>
        /// <param name="point">The point being tested.</param>
        /// <returns>
        /// <see langword="true"/> if the point is right of or fuzzy coincident with the direction
        /// vector.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsRightOrCoincident<T>(Vector2<T> p0, Vector2<T> p1, Vector2<T> point) where T : struct, IFloatingPointIeee754<T>
        {
            return IsRightOrCoincidentEps(p0, p1, point, Fuzzy<T>.Epsilon);
        }

        /// <summary>
        /// Test if a <paramref name="point"/> is within an arc sweep angle region.
        /// </summary>
        /// <typeparam name="T">Floating point type of the point components.</typeparam>
        /// <param name="center">Center of the arc, and apex of the sweep region.</param>
        /// <param name="arcStart">Start point of the arc.</param>
        /// <param name="arcEnd">End point of the arc.</param>
        /// <param name="isClockwise">Arc direction, <see langword="true"/> if clockwise.</param>
        /// <param name="point">The point being tested.</param>
        /// <param name="epsilon">
        /// Positional tolerance used for fuzzy comparing against the sweep boundaries, must be
        /// positive.
        /// </param>
        /// <returns><see langword="true"/> if the point lies within the sweep region.</returns>
        /// <remarks>
        /// <para>
        /// The angle region is defined as if the arc had infinite radius projected outward in a
        /// cone, so radial distance from <paramref name="center"/> does not matter. The check is
        /// fuzzy inclusive of both boundaries, and points within <paramref name="epsilon"/> of the
        /// apex are always inside.
        /// </para>
        /// <para>
        /// The boundary test scales <paramref name="epsilon"/> by the length of the boundary ray so
        /// that the cross product comparison behaves as a position-based tolerance. Without the
        /// scaling the effective angular tolerance would be
        /// <c>eps / (R * |pointVector|)</c>: near zero for large radii and unbounded for small
        /// ones.
        /// </para>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool PointWithinArcSweep<T>(Vector2<T> center, Vector2<T> arcStart, Vector2<T> arcEnd, bool isClockwise, Vector2<T> point, T epsilon) where T : struct, IFloatingPointIeee754<T>
        {
            Debug.Assert(epsilon > T.Zero);

            // The center is the sweep region's apex, so include points within the position
            // tolerance of it.
            Vector2<T> pointVector = point - center;
            if (pointVector.LengthSquared() < epsilon * epsilon)
            {
                return true;
            }

            // Construct the sweep boundary vectors and determine which side of each one the
            // point lies on.
            Vector2<T> startVector = arcStart - center;
            Vector2<T> endVector = arcEnd - center;
            T startCross = startVector.PerpDot(pointVector);
            T endCross = endVector.PerpDot(pointVector);

            // First test the whole sweep region without tolerance.
            bool exactlyWithinSweep = isClockwise
                ? startCross <= T.Zero && endCross >= T.Zero
                : startCross >= T.Zero && endCross <= T.Zero;
            if (exactlyWithinSweep)
            {
                return true;
            }

            // Then give fuzzy inclusion around each forward boundary ray. Scaling epsilon by the
            // ray length makes the cross product comparison a position-based tolerance. Without
            // it the effective angular tolerance would be eps/(R*|pointVector|): near zero for
            // large radii and unbounded for small ones.
            return FuzzyOnRay(startVector, startCross) || FuzzyOnRay(endVector, endCross);

            bool FuzzyOnRay(Vector2<T> ray, T cross)
                => ray.Dot(pointVector) >= T.Zero && T.Abs(cross) < epsilon * ray.Length();
        }

        /// <summary>
        /// Returns the bulge for the given arc <paramref name="angle"/>, by definition
        /// <c>bulge = tan(arc_sweep_angle / 4)</c>.
        /// </summary>
        /// <typeparam name="T">Floating point type of the angle.</typeparam>
        /// <param name="angle">The arc sweep angle in radians.</param>
        /// <returns>The bulge value.</returns>
        /// <remarks>
        /// If <paramref name="angle"/> is negative then the bulge returned will be negative
        /// (clockwise arc).
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T BulgeFromAngle<T>(T angle) where T : struct, IFloatingPointIeee754<T>
        {
            T four = T.CreateChecked(4);
            return T.Tan(angle / four);
        }

        /// <summary>
        /// Returns the arc sweep angle for the given <paramref name="bulge"/>, by definition
        /// <c>arc_sweep_angle = 4 * atan(bulge)</c>.
        /// </summary>
        /// <typeparam name="T">Floating point type of the bulge.</typeparam>
        /// <param name="bulge">The bulge value.</param>
        /// <returns>The arc sweep angle in radians.</returns>
        /// <remarks>
        /// If <paramref name="bulge"/> is negative then the angle returned will be negative
        /// (clockwise arc).
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T AngleFromBulge<T>(T bulge) where T : struct, IFloatingPointIeee754<T>
        {
            T four = T.CreateChecked(4);
            return four * T.Atan(bulge);
        }
    }
}
