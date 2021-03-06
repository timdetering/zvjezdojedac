﻿using System;
using System.Collections.Generic;
using System.Linq;
using NGenerics.DataStructures.Mathematical;
using Stareater.Controllers.Data;
using Stareater.Controllers.Views;
using Stareater.Controllers.Views.Ships;
using Stareater.Galaxy;
using Stareater.GameData;
using Stareater.Players;

namespace Stareater.Controllers
{
	//TODO(later) filter invisible fleets
	public class PlayerController
	{
		public int PlayerIndex { get; private set; }
		private GameController gameController;
		
		internal PlayerController(int playerIndex, GameController gameController)
		{
			this.PlayerIndex = playerIndex;
			this.gameController = gameController;
		}
		
		private MainGame gameInstance
		{
			get { return this.gameController.GameInstance; }
		}

		internal Player PlayerInstance(MainGame game)
		{
			if (this.PlayerIndex < game.MainPlayers.Length)
				return game.MainPlayers[this.PlayerIndex];
			else
				return game.StareaterOrganelles;
		}
		
		public PlayerInfo Info
		{
			get { return new PlayerInfo(this.PlayerInstance(this.gameInstance)); }
		}

		public LibraryController Library 
		{
			get { return new LibraryController(this.gameController); }
		}
		
		#region Turn progression
		public void EndGalaxyPhase()
		{
			this.gameController.EndGalaxyPhase(this);
		}

		public bool IsReadOnly
		{
			get { return this.gameInstance.IsReadOnly; }
		}
		#endregion
			
		#region Map related
		public bool IsStarVisited(StarData star)
		{
			return this.PlayerInstance(this.gameInstance).Intelligence.About(star).IsVisited;
		}
		
		public IEnumerable<ColonyInfo> KnownColonies(StarData star)
		{
			var game = this.gameInstance;
			var starKnowledge = this.PlayerInstance(game).Intelligence.About(star);
			
			foreach(var colony in game.States.Colonies.AtStar[star])
				if (starKnowledge.Planets[colony.Location.Planet].LastVisited != PlanetIntelligence.NeverVisited)
					yield return new ColonyInfo(colony);
		}
		
		public StarSystemController OpenStarSystem(StarData star)
		{
			var game = this.gameInstance;
			return new StarSystemController(game, star, game.IsReadOnly, this.PlayerInstance(game));
		}
		
		public StarSystemController OpenStarSystem(Vector2D position)
		{
			return this.OpenStarSystem(this.gameInstance.States.Stars.At[position]);
		}
				
		public FleetController SelectFleet(FleetInfo fleet)
		{
			var game = this.gameInstance;
			return new FleetController(fleet, game, this.PlayerInstance(game));
		}
		
		public IEnumerable<FleetInfo> Fleets
		{
			get
			{
				var game = this.gameInstance;
				return game.States.Fleets.
					Where(x => x.Owner != this.PlayerInstance(game) || !x.Owner.Orders.ShipOrders.ContainsKey(x.Position)).
					Concat(this.PlayerInstance(game).Orders.ShipOrders.SelectMany(x => x.Value)).
					Select(
						x => new FleetInfo(x, game.Derivates.Of(x.Owner), game.Statics)
					);
			}
		}
		
		public IEnumerable<FleetInfo> FleetsAt(Vector2D position)
		{
			var game = this.gameInstance;
			var player = this.PlayerInstance(game);
			var fleets = game.States.Fleets.At[position].Where(x => x.Owner != player || !x.Owner.Orders.ShipOrders.ContainsKey(x.Position));

			if (player.Orders.ShipOrders.ContainsKey(position))
				fleets = fleets.Concat(player.Orders.ShipOrders[position]);
			
			return fleets.Select(x => new FleetInfo(x, game.Derivates.Of(x.Owner), game.Statics));
		}
		
		public StarData Star(Vector2D position)
		{
			return this.gameInstance.States.Stars.At[position];
		}
		
		public int StarCount
		{
			get 
			{
				return this.gameInstance.States.Stars.Count;
			}
		}
		
		public IEnumerable<StarData> Stars
		{
			get
			{
				return this.gameInstance.States.Stars;
			}
		}

		public IEnumerable<Wormhole> Wormholes
		{
			get
			{
				foreach (var wormhole in this.gameInstance.States.Wormholes)
					yield return wormhole;
			}
		}
		#endregion
		
		#region Stellarises and colonies
		public IEnumerable<StellarisInfo> Stellarises()
		{
			var game = this.gameInstance;
			foreach(var stellaris in game.States.Stellarises.OwnedBy[this.PlayerInstance(game)])
				yield return new StellarisInfo(stellaris, game);
		}
		#endregion
		
		#region Ship designs
		public ShipDesignController NewDesign()
		{
			var game = this.gameInstance;
			return (!game.IsReadOnly) ? new ShipDesignController(game, this.PlayerInstance(game)) : null;
		}
		
		public IEnumerable<DesignInfo> ShipsDesigns()
		{
			var game = this.gameInstance;
			return game.States.Designs.
				OwnedBy[this.PlayerInstance(game)].
				Select(x => new DesignInfo(x, game.Derivates.Of(this.PlayerInstance(game)).DesignStats[x], game.Statics));
		}
		
		public void DisbandDesign(DesignInfo design)
		{
			var game = this.gameInstance;
			if (game.IsReadOnly)
				return;

			this.PlayerInstance(game).Orders.RefitOrders[design.Data] = null;
		}

		public bool IsMarkedForRemoval(DesignInfo design)
		{
			var player = this.PlayerInstance(this.gameInstance);
			return player.Orders.RefitOrders.ContainsKey(design.Data) && player.Orders.RefitOrders[design.Data] == null;
		}
		
		public void KeepDesign(DesignInfo design)
		{
			var game = this.gameInstance;
			if (game.IsReadOnly)
				return;

			this.PlayerInstance(game).Orders.RefitOrders.Remove(design.Data);
		}
		
		public IEnumerable<DesignInfo> RefitCandidates(DesignInfo design)
		{
			var game = this.gameInstance;
			var playerProc = game.Derivates.Of(this.PlayerInstance(game));
			
			return playerProc.RefitCosts[design.Data].
				Where(x => !x.Key.IsObsolete && !x.Key.IsVirtual).
				Select(x => new DesignInfo(x.Key, playerProc.DesignStats[x.Key], game.Statics));
		}
		
		public double RefitCost(DesignInfo design, DesignInfo refitWith)
		{
			var game = this.gameInstance;
			return game.Derivates.Of(this.PlayerInstance(game)).RefitCosts[design.Data][refitWith.Data];
		}
		
		public void RefitDesign(DesignInfo design, DesignInfo refitWith)
		{
			var game = this.gameInstance;
			if (game.IsReadOnly)
				return;
			
			var player = this.PlayerInstance(game);

			//TODO(v0.6) check refit compatibility, if designs are for same hull
			if (!refitWith.Constructable || player.Orders.RefitOrders.ContainsKey(refitWith.Data))
				return;

			player.Orders.RefitOrders[design.Data] = refitWith.Data;
		}
		
		public DesignInfo RefittingWith(DesignInfo design)
		{
			var game = this.gameInstance;
			var player = this.PlayerInstance(game);

			if (game.IsReadOnly || !player.Orders.RefitOrders.ContainsKey(design.Data))
				return null;
			
			var targetDesign = player.Orders.RefitOrders[design.Data];
			
			return targetDesign != null ?
				new DesignInfo(targetDesign, game.Derivates.Of(targetDesign.Owner).DesignStats[targetDesign], game.Statics) :
				null;
		}
		
		public long ShipCount(DesignInfo design)
		{
			var game = this.gameInstance;
			return game.States.Fleets.
				OwnedBy[this.PlayerInstance(game)].
				SelectMany(x => x.Ships).
				Where(x => x.Design == design.Data).
				Aggregate(0L, (sum, x) => sum + x.Quantity);
		}
		#endregion
		
		#region Colonization related
		public IEnumerable<ColonizationController> ColonizationProjects()
		{
			var game = this.gameInstance;
			var player = this.PlayerInstance(game);
			var planets = new HashSet<Planet>();
			planets.UnionWith(game.States.ColonizationProjects.OwnedBy[player].Select(x => x.Destination));
			planets.UnionWith(player.Orders.ColonizationOrders.Keys);
			
			foreach(var planet in planets)
				yield return new ColonizationController(game, planet, game.IsReadOnly, player);
		}
		
		public IEnumerable<FleetInfo> EnrouteColonizers(Planet destination)
		{
			var game = this.gameInstance;
			var finder = new ColonizerFinder(destination);
			
			foreach(var fleet in game.States.Fleets.Where(x => x.Owner == this.PlayerInstance(game)))
				if (finder.Check(fleet))
					yield return new FleetInfo(fleet, game.Derivates.Of(fleet.Owner), game.Statics);
		}
		#endregion
		
		#region Development related
		public IEnumerable<DevelopmentTopicInfo> DevelopmentTopics()
		{
			var game = this.gameInstance;
			var player = this.PlayerInstance(game);
			var playerTechs = game.Derivates.Of(player).DevelopmentOrder(game.States.DevelopmentAdvances, game.States.ResearchAdvances, game.Statics);

			if (game.Derivates.Of(player).DevelopmentPlan == null)
				game.Derivates.Of(player).CalculateDevelopment(
					game.Statics,
					game.States,
					game.Derivates.Colonies.OwnedBy[player]
				);

			var investments = game.Derivates.Of(player).DevelopmentPlan.ToDictionary(x => x.Item);
			
			foreach(var techProgress in playerTechs)
				if (investments.ContainsKey(techProgress))
					yield return new DevelopmentTopicInfo(techProgress, investments[techProgress]);
				else
					yield return new DevelopmentTopicInfo(techProgress);
			
		}
		
		public IEnumerable<DevelopmentTopicInfo> ReorderDevelopmentTopics(IEnumerable<string> idCodeOrder)
		{
			var game = this.gameInstance;
			if (game.IsReadOnly)
				return DevelopmentTopics();

			var modelQueue = this.PlayerInstance(game).Orders.DevelopmentQueue;
			modelQueue.Clear();
			
			int i = 0;
			foreach (var idCode in idCodeOrder) {
				modelQueue.Add(idCode, i);
				i++;
			}

			game.Derivates.Of(this.PlayerInstance(game)).InvalidateDevelopment();
			return DevelopmentTopics();
		}
		
		public DevelopmentFocusInfo[] DevelopmentFocusOptions()
		{
			return this.gameInstance.Statics.DevelopmentFocusOptions.Select(x => new DevelopmentFocusInfo(x)).ToArray();
		}
		
		public int DevelopmentFocusIndex 
		{ 
			get
			{
				return this.PlayerInstance(this.gameInstance).Orders.DevelopmentFocusIndex;
			}
			
			set
			{
				var game = this.gameInstance;
				if (game.IsReadOnly)
					return;
				
				var player = this.PlayerInstance(game);

				if (value >= 0 && value < game.Statics.DevelopmentFocusOptions.Count)
					player.Orders.DevelopmentFocusIndex = value;

				game.Derivates.Of(player).InvalidateDevelopment();
			}
		}
		
		public double DevelopmentPoints 
		{ 
			get
			{
				var game = this.gameInstance;
				
				return game.Derivates.Colonies.OwnedBy[this.PlayerInstance(game)].Sum(x => x.Development);
			}
		}
		#endregion
		
		#region Research related
		public IEnumerable<ResearchTopicInfo> ResearchTopics()
		{
			var game = this.gameInstance;
			var player = this.PlayerInstance(game);
			var playerTechs = game.Derivates.Of(player).ResearchOrder(game.States.ResearchAdvances);

			if (game.Derivates.Of(player).ResearchPlan == null)
				game.Derivates.Of(player).CalculateResearch(
					game.Statics,
					game.States,
					game.Derivates.Colonies.OwnedBy[player]
				);

			var investments = game.Derivates.Of(player).ResearchPlan.ToDictionary(x => x.Item);
			
			foreach(var techProgress in playerTechs)
				if (investments.ContainsKey(techProgress))
					yield return new ResearchTopicInfo(techProgress, investments[techProgress], game.Statics.DevelopmentTopics);
				else
					yield return new ResearchTopicInfo(techProgress, game.Statics.DevelopmentTopics);
		}
		
		public int ResearchFocus
		{
			get 
			{
				var game = this.gameInstance;
				string focused = this.PlayerInstance(game).Orders.ResearchFocus;
				var playerTechs = game.Derivates.Of(this.PlayerInstance(game)).ResearchOrder(game.States.ResearchAdvances).ToList();
				
				for (int i = 0; i < playerTechs.Count; i++)
					if (playerTechs[i].Topic.IdCode == focused)
						return i;
				
				return 0; //TODO(later) think of some smarter default research
			}
			
			set
			{
				var game = this.gameInstance;
				if (game.IsReadOnly)
					return;

				var playerTechs = game.Derivates.Of(this.PlayerInstance(game)).ResearchOrder(game.States.ResearchAdvances).ToList();
				if (value >= 0 && value < playerTechs.Count) 
				{
					this.PlayerInstance(game).Orders.ResearchFocus = playerTechs[value].Topic.IdCode;
					game.Derivates.Of(this.PlayerInstance(game)).InvalidateResearch();
				}
			}
		}
		#endregion
		
		#region Report related
		public IEnumerable<IReportInfo> Reports
		{
			get 
			{
				var game = this.gameInstance;
				var wrapper = new ReportWrapper();

				foreach (var report in game.States.Reports.Of[this.PlayerInstance(game)])
					yield return wrapper.Wrap(report);
			}
		}
		#endregion
		
		#region Diplomacy related
		public IEnumerable<ContactInfo> DiplomaticContacts()
		{
			var game = this.gameInstance;
			var treaties = game.States.Treaties.Of[this.PlayerInstance(game)];
			
			foreach(var player in game.MainPlayers)
				if (player != this.PlayerInstance(game))
					yield return new ContactInfo(player, treaties.Where(x => x.Party1 == player || x.Party2 == player));
		}

		public bool IsAudienceRequested(ContactInfo contact)
		{
			var game = this.gameInstance;
			var contactIndex = Array.IndexOf(game.MainPlayers, contact.PlayerData);
			
			return this.PlayerInstance(game).Orders.AudienceRequests.Contains(contactIndex);
		}
		
		public void RequestAudience(ContactInfo contact)
		{
			var game = this.gameInstance;
			if (game.IsReadOnly)
				return;
			
			var contactIndex = Array.IndexOf(game.MainPlayers, contact.PlayerData);
			this.PlayerInstance(game).Orders.AudienceRequests.Add(contactIndex);
		}
		
		public void CancelAudience(ContactInfo contact)
		{
			var game = this.gameInstance;
			if (game.IsReadOnly)
				return;
			
			var contactIndex = Array.IndexOf(game.MainPlayers, contact.PlayerData);
			this.PlayerInstance(game).Orders.AudienceRequests.Remove(contactIndex);
		}
		#endregion
	}
}
