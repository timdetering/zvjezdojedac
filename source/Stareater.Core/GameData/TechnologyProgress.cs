﻿ 
using System;
using Stareater.Players;

namespace Stareater.GameData 
{
	partial class TechnologyProgress 
	{
		public Player Owner { get; private set; }
		public Technology Topic { get; private set; }
		public int Level { get; private set; }
		public double InvestedPoints { get; private set; }

		public TechnologyProgress(Player owner, Technology topic, int level, double investedPoints) 
		{
			this.Owner = owner;
			this.Topic = topic;
			this.Level = level;
			this.InvestedPoints = investedPoints;
 
		} 


		internal TechnologyProgress Copy(PlayersRemap playersRemap) 
		{
			return new TechnologyProgress(playersRemap.Players[this.Owner], this.Topic, this.Level, this.InvestedPoints);
 
		} 
 
	}
}
