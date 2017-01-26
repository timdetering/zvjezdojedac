﻿using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Stareater.GLData;
using Stareater.GraphicsEngine;

namespace Stareater.GLData.OrbitShader
{
	class OrbitDrawable : IDrawable
	{
		public VertexArray Vao { get; private set; }
		private readonly int objectIndex;
		private readonly OrbitData objectData;
		
		public OrbitDrawable(VertexArray vao, int objectIndex, OrbitData objectData)
		{
			this.objectIndex = objectIndex;
			this.objectData = objectData;
			this.Vao = vao;
		}
		
		public void Draw(Matrix4 view)
		{
			var program = ShaderLibrary.PlanetOrbit;
			GL.UseProgram(program.ProgramId);
			this.Vao.Bind();
			
			var mvp = this.objectData.LocalTransform * view;
			GL.UniformMatrix4(program.LocalTransformId, false, ref mvp);
			GL.Uniform1(program.ZId, this.objectData.Z);
			GL.Uniform4(program.ColorId, this.objectData.Color);
			GL.Uniform1(program.MinRId, this.objectData.MinRadius);
			GL.Uniform1(program.MaxRId, this.objectData.MaxRadius);
		
			GL.DrawArrays(BeginMode.Triangles, Vao.ObjectStart(this.objectIndex), Vao.ObjectSize(this.objectIndex));
			ShaderLibrary.PrintGlErrors("Draw orbits");
		}
	}
}