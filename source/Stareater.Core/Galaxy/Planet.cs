﻿ 

using Ikadn.Ikon.Types;
using Stareater.Utils.Collections;
using System;
using System.Linq;
using Stareater.GameData;

namespace Stareater.Galaxy 
{
	public partial class Planet 
	{
		public StarData Star { get; private set; }
		public int Position { get; private set; }
		public PlanetType Type { get; private set; }
		public double Size { get; private set; }

		public Planet(StarData star, int position, PlanetType type, double size) 
		{
			this.Star = star;
			this.Position = position;
			this.Type = type;
			this.Size = size;
 
			 
		} 


		private Planet(IkonComposite rawData, ObjectDeindexer deindexer) 
		{
			var starSave = rawData[StarKey];
			this.Star = deindexer.Get<StarData>(starSave.To<int>());

			var positionSave = rawData[PositionKey];
			this.Position = positionSave.To<int>();

			var typeSave = rawData[TypeKey];
			this.Type = (PlanetType)Enum.Parse(typeof(PlanetType), (string)typeSave.Tag);

			var sizeSave = rawData[SizeKey];
			this.Size = sizeSave.To<double>();
 
			 
		}

		internal Planet Copy(GalaxyRemap galaxyRemap) 
		{
			return new Planet(galaxyRemap.Stars[this.Star], this.Position, this.Type, this.Size);
 
		} 
 

		#region Saving
		public IkonComposite Save(ObjectIndexer indexer) 
		{
			var data = new IkonComposite(TableTag);
			data.Add(StarKey, new IkonInteger(indexer.IndexOf(this.Star)));

			data.Add(PositionKey, new IkonInteger(this.Position));

			data.Add(TypeKey, new IkonComposite(this.Type.ToString()));

			data.Add(SizeKey, new IkonFloat(this.Size));
			return data;
 
		}

		public static Planet Load(IkonComposite rawData, ObjectDeindexer deindexer)
		{
			var loadedData = new Planet(rawData, deindexer);
			deindexer.Add(loadedData);
			return loadedData;
		}
 

		private const string TableTag = "Planet";
		private const string StarKey = "star";
		private const string PositionKey = "position";
		private const string TypeKey = "type";
		private const string SizeKey = "size";
 
		#endregion

 
	}
}
