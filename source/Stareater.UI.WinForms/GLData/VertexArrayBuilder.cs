﻿using System;
using System.Collections.Generic;
using NGenerics.DataStructures.Mathematical;
using OpenTK.Graphics.OpenGL;
using Stareater.GLRenderers;

namespace Stareater.GLData
{
	class VertexArrayBuilder
	{
		private List<float> vertices = new List<float>();
		private List<int> objectStarts = new List<int>();
		private List<int> objectSizes = new List<int>();
	
		private int vertexSize;
		private int objectSize = 0;
		
		public VertexArrayBuilder(int vertexSize)
		{
			this.vertexSize = vertexSize;
		}
		
		public void BeginObject()
		{
			this.objectSize = 0;
			this.objectStarts.Add(this.vertices.Count);
		}
		
		public void EndObject()
		{
			this.objectSizes.Add(this.objectSize);
		}
		
		public VertexArray GenBuffer(IGlProgram forProgram)
		{
			var vao = GL.GenVertexArray();
			GL.BindVertexArray(vao);
			
			var vbo = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
			GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(this.vertices.Count * sizeof (float)), this.vertices.ToArray(), BufferUsageHint.StaticDraw);
			forProgram.SetupAttributes();
			GL.BindVertexArray(0);
			
			return new VertexArray(vao, vbo, this.objectStarts, this.objectSizes);
		}
		
		public void AddPathRect(Vector2D fromPosition, Vector2D toPosition, double width, TextureInfo textureinfo)
		{
			var center = (fromPosition + toPosition) / 2;
			var length = toPosition - fromPosition;
			var direction = new Vector2D(length.X, length.Y);
			direction.Normalize();
			var widthDir = new Vector2D(-direction.Y, direction.X) * width;
			
			this.add(center - length / 2 + widthDir /2);
			this.vertices.Add(textureinfo.TextureCoords[3].X);
			this.vertices.Add(textureinfo.TextureCoords[3].Y);
			
			this.add(center + length / 2 + widthDir /2);
			this.vertices.Add(textureinfo.TextureCoords[2].X);
			this.vertices.Add(textureinfo.TextureCoords[2].Y);
		
			this.add(center + length / 2 - widthDir /2);
			this.vertices.Add(textureinfo.TextureCoords[1].X);
			this.vertices.Add(textureinfo.TextureCoords[1].Y);
		
			
			this.add(center + length / 2 - widthDir /2);
			this.vertices.Add(textureinfo.TextureCoords[1].X);
			this.vertices.Add(textureinfo.TextureCoords[1].Y);
		
			this.add(center - length / 2 - widthDir /2);
			this.vertices.Add(textureinfo.TextureCoords[0].X);
			this.vertices.Add(textureinfo.TextureCoords[0].Y);
		
			this.add(center - length / 2 + widthDir /2);
			this.vertices.Add(textureinfo.TextureCoords[3].X);
			this.vertices.Add(textureinfo.TextureCoords[3].Y);
		
			this.objectSize += 6;
		}
		
		public void AddTexturedRect(Vector2D center, Vector2D width, Vector2D height, Vector2D[] textureCoords)
		{
			this.add(center - width / 2 + height /2);
			this.add(textureCoords[3]);
			
			this.add(center + width / 2 + height /2);
			this.add(textureCoords[2]);
		
			this.add(center + width / 2 - height /2);
			this.add(textureCoords[1]);
		
			
			this.add(center + width / 2 - height /2);
			this.add(textureCoords[1]);
		
			this.add(center - width / 2 - height /2);
			this.add(textureCoords[0]);
		
			this.add(center - width / 2 + height /2);
			this.add(textureCoords[3]);
		
			this.objectSize += 6;
		}
		
		public void AddTexturedRect(float x0, float y0, float w, float h, float tx0, float ty0, float tx1, float ty1)
		{
			this.vertices.Add(x0 - w / 2); 
			this.vertices.Add(y0 + h / 2);
			this.vertices.Add(tx0);
			this.vertices.Add(ty0);
		
			this.vertices.Add(x0 + w / 2); 
			this.vertices.Add(y0 + h / 2);
			this.vertices.Add(tx1);
			this.vertices.Add(ty0);
		
			this.vertices.Add(x0 + w / 2); 
			this.vertices.Add(y0 - h / 2);
			this.vertices.Add(tx1);
			this.vertices.Add(ty1);
		
		
			this.vertices.Add(x0 + w / 2); 
			this.vertices.Add(y0 - h / 2);
			this.vertices.Add(tx1);
			this.vertices.Add(ty1);
		
			this.vertices.Add(x0 - w / 2); 
			this.vertices.Add(y0 - h / 2);
			this.vertices.Add(tx0);
			this.vertices.Add(ty1);
		
			this.vertices.Add(x0 - w / 2); 
			this.vertices.Add(y0 + h / 2);
			this.vertices.Add(tx0);
			this.vertices.Add(ty0);
		
			this.objectSize += 6;
		}
		
		private void add(Vector2D v)
		{
			this.vertices.Add((float)v.X);
			this.vertices.Add((float)v.Y);
		}
	}
}
