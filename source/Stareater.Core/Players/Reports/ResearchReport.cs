﻿using System;
using Ikadn;
using Ikadn.Ikon.Types;
using Stareater.GameData;
using Stareater.GameLogic;
using Stareater.Utils.Collections;

namespace Stareater.Players.Reports
{
	class ResearchReport : IReport
	{
		public ResearchResult TechProgress { get; private set; }
		
		internal ResearchReport(ResearchResult techProgress)
		{
			this.TechProgress = techProgress;
		}
		
		public Player Owner {
			get {
				return this.TechProgress.Item.Owner;
			}
		}
		
		public void Accept(IReportVisitor visitor)
		{
			visitor.Visit(this);
		}
		
		public IkadnBaseObject Save(ObjectIndexer indexer)
		{
			var data = new IkonComposite(SaveTag);
			data.Add(CountKey, new IkonInteger(this.TechProgress.CompletedCount));
			data.Add(InvestedKey, new IkonFloat(this.TechProgress.InvestedPoints));
			data.Add(LeftoverKey, new IkonFloat(this.TechProgress.LeftoverPoints));
			data.Add(TopicKey, new IkonInteger(indexer.IndexOf(this.TechProgress.Item)));
			
			return data;
		}
		
		public static IReport Load(IkonComposite reportData, ObjectDeindexer deindexer)
		{
			return new ResearchReport(new ResearchResult(
				reportData[CountKey].To<long>(),
				reportData[InvestedKey].To<double>(),
				deindexer.Get<ResearchProgress>(reportData[TopicKey].To<int>()),
				reportData[LeftoverKey].To<double>()
			));
		}
		
		public const string SaveTag = "ResearchReport";
		private const string CountKey = "count";
		private const string InvestedKey = "invested";
		private const string LeftoverKey = "leftover";
		private const string TopicKey = "topic";
	}
}
