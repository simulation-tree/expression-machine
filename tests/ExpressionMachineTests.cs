﻿using Collections.Generic;
using System;
using System.Numerics;
using Unmanaged.Tests;

namespace ExpressionMachine.Tests
{
    public class ExpressionMachineTests : UnmanagedTests
    {
        [Test]
        public void EvaluateComplicatedExpression()
        {
            using Machine operation = new("2 * 5 + (3 - (10 / 2)) * 2");
            Assert.That(operation.Evaluate(), Is.EqualTo(6));
        }

        [Test]
        public void EvaluateSimpleExpression()
        {
            using Machine source = new("4 * 2");
            Assert.That(source.Evaluate(), Is.EqualTo(8));
        }

        [Test]
        public void EvaluateWithVariables()
        {
            using Machine source = new("value * 0.5");
            source.SetVariable("value", 800);
            Assert.That(source.Evaluate(), Is.EqualTo(400));
        }

        [Test]
        public void ChangeSource()
        {
            using Machine reusable = new();
            reusable.SetVariable("value", 1024);
            reusable.SetFunction("do", Do);

            reusable.SetSource("do(100)");
            Assert.That(reusable.Evaluate(), Is.EqualTo(50));

            reusable.SetSource("do(200)");
            Assert.That(reusable.Evaluate(), Is.EqualTo(100));

            static float Do(float value)
            {
                return value * 0.5f;
            }
        }

        [Test]
        public void InvokeFunctionsManually()
        {
            using Machine vm = new();
            vm.SetFunction("do", Do);

            float result = vm.InvokeFunction("do", 20f);
            Assert.That(result, Is.EqualTo(10));

            static float Do(float value)
            {
                return value * 0.5f;
            }
        }

        [Test]
        public void AnchorExample()
        {
            using Machine horizontal = new("width * 0.5");
            horizontal.SetVariable("width", 800);
            horizontal.SetVariable("height", 600);

            using Machine vertical = new("multiply(height) + 50");
            vertical.SetVariable("width", 800);
            vertical.SetVariable("height", 600);
            vertical.SetFunction("multiply", Multiply);

            Assert.That(horizontal.Source.ToString(), Is.EqualTo("width * 0.5"));
            Assert.That(vertical.Source.ToString(), Is.EqualTo("multiply(height) + 50"));
            Assert.That(horizontal.Evaluate(), Is.EqualTo(400));
            Assert.That(vertical.Evaluate(), Is.EqualTo(350));

            static float Multiply(float value)
            {
                return value * 0.5f;
            }
        }

        [Test]
        public void EvaluateWithCustomFunction()
        {
            using Machine expression = new("do(10 * 0.5) + wow()");
            expression.SetFunction("do", Do);
            expression.SetFunction("wow", Wow);
            Assert.That(expression.Evaluate(), Is.EqualTo(11));

            static float Do(float value)
            {
                return value + 1;
            }

            static float Wow(float value)
            {
                return 5;
            }
        }

        [Test]
        public void PrintCircle()
        {
            using Machine vm = new();
            float radius = 4f;
            vm.SetVariable("radius", radius);
            vm.SetFunction("cos", MathF.Cos);
            vm.SetFunction("sin", MathF.Sin);

            using List<Vector2> positions = new();
            for (uint i = 0; i < 360; i++)
            {
                float t = i * MathF.PI / 180;
                vm.SetVariable("t", t);
                vm.SetSource("cos(t) * radius");
                float x = vm.Evaluate();
                vm.SetSource("sin(t) * radius");
                float y = vm.Evaluate();
                positions.Add(new Vector2(x, y));
            }

            using List<Vector2> otherPositions = new();
            for (uint i = 0; i < 360; i++)
            {
                float t = i * MathF.PI / 180;
                float x = MathF.Cos(t) * radius;
                float y = MathF.Sin(t) * radius;
                otherPositions.Add(new Vector2(x, y));
            }

            for (int i = 0; i < 360; i++)
            {
                Assert.That(positions[i], Is.EqualTo(otherPositions[i]));
            }
        }

        [Test]
        public void UseNodes()
        {
            Node a = new(NodeType.Value, 0, 1, default);
            Assert.That(a.Type, Is.EqualTo(NodeType.Value));
            Assert.That((int)a.A, Is.EqualTo(0));
            Assert.That((int)a.B, Is.EqualTo(1));
            Assert.That(a.IsDisposed, Is.False);
            a.Dispose();
            Assert.That(a.IsDisposed, Is.True);
        }

        [Test]
        public void UnsuccessfulCompilation()
        {
            using Machine vm = new();
            bool success = vm.TrySetSource("5 +", out CompilationError error);
            Assert.That(success, Is.False);
            Assert.That(error.type, Is.EqualTo(CompilationError.Type.ExpectedAdditionalToken));
        }
    }
}
