// <copyright file="TFQMRTest.cs" company="Math.NET">
// Math.NET Numerics, part of the Math.NET Project
// http://numerics.mathdotnet.com
// http://github.com/mathnet/mathnet-numerics
// http://mathnetnumerics.codeplex.com
//
// Copyright (c) 2009-2013 Math.NET
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
// </copyright>

namespace MathNet.Numerics.UnitTests.LinearAlgebraTests.Complex32.Solvers.Iterative
{
    using System;
    using LinearAlgebra.Complex32;
    using LinearAlgebra.Complex32.Solvers;
    using LinearAlgebra.Complex32.Solvers.Iterative;
    using LinearAlgebra.Complex32.Solvers.StopCriterium;
    using LinearAlgebra.Generic.Solvers.Status;
    using NUnit.Framework;

    /// <summary>
    /// Tests of Transpose Free Quasi-Minimal Residual iterative matrix solver.
    /// </summary>
    [TestFixture]
    public class TFQMRTest
    {
        /// <summary>
        /// Convergence boundary.
        /// </summary>
        const float ConvergenceBoundary = 1e-5f;

        /// <summary>
        /// Maximum iterations.
        /// </summary>
        const int MaximumIterations = 1000;

        /// <summary>
        /// Solve wide matrix throws <c>ArgumentException</c>.
        /// </summary>
        [Test]
        public void SolveWideMatrixThrowsArgumentException()
        {
            var matrix = new SparseMatrix(2, 3);
            Vector input = new DenseVector(2);

            var solver = new TFQMR();
            Assert.Throws<ArgumentException>(() => solver.Solve(matrix, input));
        }

        /// <summary>
        /// Solve long matrix throws <c>ArgumentException</c>.
        /// </summary>
        [Test]
        public void SolveLongMatrixThrowsArgumentException()
        {
            var matrix = new SparseMatrix(3, 2);
            Vector input = new DenseVector(3);

            var solver = new TFQMR();
            Assert.Throws<ArgumentException>(() => solver.Solve(matrix, input));
        }

        /// <summary>
        /// Solve unit matrix and back multiply.
        /// </summary>
        [Test]
        public void SolveUnitMatrixAndBackMultiply()
        {
            // Create the identity matrix
            Matrix matrix = SparseMatrix.Identity(100);

            // Create the y vector
            Vector y = DenseVector.Create(matrix.RowCount, i => 1);

            // Create an iteration monitor which will keep track of iterative convergence
            var monitor = new Iterator(new IIterationStopCriterium[]
                {
                    new IterationCountStopCriterium(MaximumIterations),
                    new ResidualStopCriterium(ConvergenceBoundary),
                    new DivergenceStopCriterium(),
                    new FailureStopCriterium()
                });

            var solver = new TFQMR(monitor);

            // Solve equation Ax = y
            var x = solver.Solve(matrix, y);

            // Now compare the results
            Assert.IsNotNull(x, "#02");
            Assert.AreEqual(y.Count, x.Count, "#03");

            // Back multiply the vector
            var z = matrix.Multiply(x);

            // Check that the solution converged
            Assert.IsTrue(monitor.Status is CalculationConverged, "#04");

            // Now compare the vectors
            for (var i = 0; i < y.Count; i++)
            {
                Assert.IsTrue((y[i] - z[i]).Magnitude.IsSmaller(ConvergenceBoundary, 1), "#05-" + i);
            }
        }

        /// <summary>
        /// Solve scaled unit matrix and back multiply.
        /// </summary>
        [Test]
        public void SolveScaledUnitMatrixAndBackMultiply()
        {
            // Create the identity matrix
            Matrix matrix = SparseMatrix.Identity(100);

            // Scale it with a funny number
            matrix.Multiply((float) Math.PI, matrix);

            // Create the y vector
            Vector y = DenseVector.Create(matrix.RowCount, i => 1);

            // Create an iteration monitor which will keep track of iterative convergence
            var monitor = new Iterator(new IIterationStopCriterium[]
                {
                    new IterationCountStopCriterium(MaximumIterations),
                    new ResidualStopCriterium(ConvergenceBoundary),
                    new DivergenceStopCriterium(),
                    new FailureStopCriterium()
                });
            var solver = new TFQMR(monitor);

            // Solve equation Ax = y
            var x = solver.Solve(matrix, y);

            // Now compare the results
            Assert.IsNotNull(x, "#02");
            Assert.AreEqual(y.Count, x.Count, "#03");

            // Back multiply the vector
            var z = matrix.Multiply(x);

            // Check that the solution converged
            Assert.IsTrue(monitor.Status is CalculationConverged, "#04");

            // Now compare the vectors
            for (var i = 0; i < y.Count; i++)
            {
                Assert.IsTrue((y[i] - z[i]).Magnitude.IsSmaller(ConvergenceBoundary, 1), "#05-" + i);
            }
        }

        /// <summary>
        /// Solve poisson matrix and back multiply.
        /// </summary>
        [Test]
        public void SolvePoissonMatrixAndBackMultiply()
        {
            // Create the matrix
            var matrix = new SparseMatrix(25);

            // Assemble the matrix. We assume we're solving the Poisson equation
            // on a rectangular 5 x 5 grid
            const int GridSize = 5;

            // The pattern is:
            // 0 .... 0 -1 0 0 0 0 0 0 0 0 -1 4 -1 0 0 0 0 0 0 0 0 -1 0 0 ... 0
            for (var i = 0; i < matrix.RowCount; i++)
            {
                // Insert the first set of -1's
                if (i > (GridSize - 1))
                {
                    matrix[i, i - GridSize] = -1;
                }

                // Insert the second set of -1's
                if (i > 0)
                {
                    matrix[i, i - 1] = -1;
                }

                // Insert the centerline values
                matrix[i, i] = 4;

                // Insert the first trailing set of -1's
                if (i < matrix.RowCount - 1)
                {
                    matrix[i, i + 1] = -1;
                }

                // Insert the second trailing set of -1's
                if (i < matrix.RowCount - GridSize)
                {
                    matrix[i, i + GridSize] = -1;
                }
            }

            // Create the y vector
            Vector y = DenseVector.Create(matrix.RowCount, i => 1);

            // Create an iteration monitor which will keep track of iterative convergence
            var monitor = new Iterator(new IIterationStopCriterium[]
                {
                    new IterationCountStopCriterium(MaximumIterations),
                    new ResidualStopCriterium(ConvergenceBoundary),
                    new DivergenceStopCriterium(),
                    new FailureStopCriterium()
                });
            var solver = new TFQMR(monitor);

            // Solve equation Ax = y
            var x = solver.Solve(matrix, y);

            // Now compare the results
            Assert.IsNotNull(x, "#02");
            Assert.AreEqual(y.Count, x.Count, "#03");

            // Back multiply the vector
            var z = matrix.Multiply(x);

            // Check that the solution converged
            Assert.IsTrue(monitor.Status is CalculationConverged, "#04");

            // Now compare the vectors
            for (var i = 0; i < y.Count; i++)
            {
                Assert.IsTrue((y[i] - z[i]).Magnitude.IsSmaller(1e-4f, 1), "#05-" + i);
            }
        }

        /// <summary>
        /// Can solve for a random vector.
        /// </summary>
        /// <param name="order">Matrix order.</param>
        [TestCase(4)]
        public void CanSolveForRandomVector(int order)
        {
            for (var iteration = 5; iteration > 3; iteration--)
            {
                var matrixA = MatrixLoader.GenerateRandomDenseMatrix(order, order);
                var vectorb = MatrixLoader.GenerateRandomDenseVector(order);

                var monitor = new Iterator(new IIterationStopCriterium[]
                    {
                        new IterationCountStopCriterium(1000),
                        new ResidualStopCriterium((float) Math.Pow(1.0/10.0, iteration)),
                    });
                var solver = new TFQMR(monitor);

                var resultx = solver.Solve(matrixA, vectorb);

                if (!(monitor.Status is CalculationConverged))
                {
                    // Solution was not found, try again downgrading convergence boundary
                    continue;
                }

                Assert.AreEqual(matrixA.ColumnCount, resultx.Count);
                var matrixBReconstruct = matrixA*resultx;

                // Check the reconstruction.
                for (var i = 0; i < order; i++)
                {
                    Assert.AreEqual(vectorb[i].Real, matrixBReconstruct[i].Real, (float) Math.Pow(1.0/10.0, iteration - 3));
                    Assert.AreEqual(vectorb[i].Imaginary, matrixBReconstruct[i].Imaginary, (float) Math.Pow(1.0/10.0, iteration - 3));
                }

                return;
            }

            Assert.Fail("Solution was not found in 3 tries");
        }

        /// <summary>
        /// Can solve for random matrix.
        /// </summary>
        /// <param name="order">Matrix order.</param>
        [TestCase(4)]
        public void CanSolveForRandomMatrix(int order)
        {
            for (var iteration = 5; iteration > 3; iteration--)
            {
                var matrixA = MatrixLoader.GenerateRandomDenseMatrix(order, order);
                var matrixB = MatrixLoader.GenerateRandomDenseMatrix(order, order);

                var monitor = new Iterator(new IIterationStopCriterium[]
                    {
                        new IterationCountStopCriterium(1000),
                        new ResidualStopCriterium((float) Math.Pow(1.0/10.0, iteration))
                    });
                var solver = new TFQMR(monitor);
                var matrixX = solver.Solve(matrixA, matrixB);

                if (!(monitor.Status is CalculationConverged))
                {
                    // Solution was not found, try again downgrading convergence boundary
                    continue;
                }

                // The solution X row dimension is equal to the column dimension of A
                Assert.AreEqual(matrixA.ColumnCount, matrixX.RowCount);

                // The solution X has the same number of columns as B
                Assert.AreEqual(matrixB.ColumnCount, matrixX.ColumnCount);

                var matrixBReconstruct = matrixA*matrixX;

                // Check the reconstruction.
                for (var i = 0; i < matrixB.RowCount; i++)
                {
                    for (var j = 0; j < matrixB.ColumnCount; j++)
                    {
                        Assert.AreEqual(matrixB[i, j].Real, matrixBReconstruct[i, j].Real, (float) Math.Pow(1.0/10.0, iteration - 3));
                        Assert.AreEqual(matrixB[i, j].Imaginary, matrixBReconstruct[i, j].Imaginary, (float) Math.Pow(1.0/10.0, iteration - 3));
                    }
                }

                return;
            }

            Assert.Fail("Solution was not found in 3 tries");
        }
    }
}
