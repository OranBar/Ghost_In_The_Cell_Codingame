using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

class Player {

	static void Main(string[] args) {
		#region Initialization
		////////////////////////////// Initialization //////////////////////////////////
		string[] inputs;
		int factoryCount = int.Parse(Console.ReadLine()); // the number of factories
		int linkCount = int.Parse(Console.ReadLine()); // the number of links between factories

		Graph map = new Graph(factoryCount);
		Odinoo odinooooooooooooooooooo = new Odinoo();

		for (int i = 0; i < linkCount; i++) {
			inputs = Console.ReadLine().Split(' ');
			int factory1 = int.Parse(inputs[0]);
			int factory2 = int.Parse(inputs[1]);
			int distance = int.Parse(inputs[2]);

			map.AddConnection(factory1, factory2, distance);
		}
		/////////////////////////////////////////////////////////////////////////////////
		#endregion

		// game loop
		int turn = 0;
		GameState prevGameState = null;
		GameState currGameState = null;
		while (true) {
			turn++;
			prevGameState = currGameState;
			currGameState = null;
			List<Entity> entities = new List<Entity>();

			#region Read Game Info
			int entityCount = int.Parse(Console.ReadLine()); // the number of entities (e.g. factories and troops)
			for (int i = 0; i < entityCount; i++) {
				inputs = Console.ReadLine().Split(' ');
				int entityId = int.Parse(inputs[0]);
				string entityType = inputs[1];
				int owner = int.Parse(inputs[2]);
				int arg2 = int.Parse(inputs[3]);  //identifier of the factory from where the troop leaves || number of cyborgs in the factory 
				int arg3 = int.Parse(inputs[4]);  //identifier of the factory targeted by the troop || factory production (between 0 and 3) 
				int arg4 = int.Parse(inputs[5]);  //number of cyborgs in the troop (positive integer)	
				int arg5 = int.Parse(inputs[6]);  //remaining number of turns before the troop arrives (positive integer)

				if (entityType == "FACTORY") {
					Factory factory = new Factory(entityId, owner, arg2, arg3);
					entities.Add(factory);
				}
				if (entityType == "TROOP") {
					Troops troops = new Troops(entityId, owner, (Factory)entities.First(e => e.EntityId == arg2), (Factory)entities.First(e => e.EntityId == arg3), arg4, arg5);
					entities.Add(troops);
				}
				if (entityType == "BOMB") {
					Bomb bomb = new Bomb(entityId, owner, (Factory)entities.First(e => e.EntityId == arg2), (Factory)entities.FirstOrDefault(e => e.EntityId == arg3), arg4);
					entities.Add(bomb);
				}
			}
			#endregion

			currGameState = new GameState(map, entities, turn, prevGameState);
			string result = odinooooooooooooooooooo.PlanNextMove(currGameState);
			Console.WriteLine(result);	
		}
	}
}
#region Logic
public interface IStrategy {
	GameState ExecuteStrategy(GameState game, ref CommandBuilder action);
}

public class Odinoo {

	public List<IStrategy> strategists;

	//private int turn = 0;
	private int myBombsCount = 2;

	public Odinoo() {
		SwitchStrategy(new IStrategy[] {new S_Neutral_Conquerer()});
	}
	 /*
	public void Conqueror_FirstTurn(GameState game, ref CommandBuilder action) {
		Factory myBestFactory = game.GetFactoriesOf(Players.Me).First();
		Console.Error.WriteLine("My best factory " + myBestFactory.EntityId + " has " + myBestFactory.CyborgCount);
		int troopsAvailable = myBestFactory.CyborgCount - 1;
		var factoriesToAttack = game.Factories
			.Where(f1 => f1.Owner == Players.Neutral)
			.Where(f2 => f2.Production >= 1)
			.OrderBy(f3 => game.Graph[myBestFactory, f3])
			.ThenBy(f4 => f4.CyborgCount);

		foreach (var factory in factoriesToAttack) {
			action.AppendMove(myBestFactory, factory, factory.CyborgCount + 1);
			troopsAvailable -= (factory.CyborgCount + 1);
			if (troopsAvailable <= 0) {
				break;
			}
		}
	}
		 */
	public void SwitchStrategy(IEnumerable<IStrategy> newStrategy) {
		this.strategists = newStrategy.ToList();
	}

	public void AddStrategy(IStrategy strategy) {
		this.strategists.Add(strategy);
	}

	public string PlanNextMove(GameState game) {
		//turn++;
		CommandBuilder action = new CommandBuilder();

		if (game.Turn == 1) {
			Console.Error.WriteLine("Conqueror Mode");

			//Conqueror_FirstTurn(game, ref action);
			//GameState gameCopy = new GameState(game);
			//foreach (var strategy in strategists) {
			//	gameCopy = strategy.ExecuteStrategy(game, ref action);
			//}

			Console.Error.WriteLine("Sending First Turn Bomb");
			//action.AppendBomb(game.GetFactoriesOf(Players.Me).First(), game.GetFactoriesOf(Players.Opponent).First());
			//myBombsCount--;

			SwitchStrategy(new IStrategy[] { new S_Defender(), new S_Neutral_Conquerer(), new S_Macro(), new S_Bomber(2)});

		} else {
			//if (myBombsCount > 0) {
			//	//Throw the second motherfucking bomb
			//	Factory secondBombTarget =
			//		game.Troops
			//		.Where(t1 => t1.Owner == Players.Opponent)
			//		.Where(t3 => t3.Target.Owner != Players.Me)
			//		.Select(t2 => game.GetFactory(t2.Target))
			//		.OrderByDescending(f3 => f3.Production)
			//		.ThenByDescending(f4 => f4.GetDistanceToFurthestFactory(game, Players.Me))
			//		.ThenByDescending(f5 => f5.CyborgCount)
			//		.FirstOrDefault();

			//	if (secondBombTarget != null) {
			//		Console.Error.WriteLine("Sending Second Bomb");
			//		action.AppendBomb(secondBombTarget.GetClosestFactory(game, Players.Me), secondBombTarget.EntityId);
			//		myBombsCount--;
			//	}
			//}
		}

		if (game.GetFactoriesOf(Players.Me).Where(f1 => f1.Production != 3).Count() == 0) {
			//Macro Phase finished
			AddStrategy(new S_ShyExpander());
			AddStrategy(new S_VCDistributorFromBackfield());
			AddStrategy(new S_CloseCombatAttacker());

			//AddStrategy(new S_MoveToFrontier());
			//SwitchStrategy(new IStrategy[] { new S_Defender(), new S_Neutral_Conquerer(), new S_Macro(), new S_ShyExpander(), new S_Bomber()/*, new S_MoveToFrontier()*/ });
		}


		GameState gameCopy = new GameState(game);
		foreach (var strategy in strategists) {
			gameCopy = strategy.ExecuteStrategy(game, ref action);
		}

		return action.Result;
	}
}

public class S_Defender : IStrategy {

	public Tuple<int, int> bombTargetAndEta;

	public GameState ExecuteStrategy(GameState game, ref CommandBuilder action) {
		GameState virtualGameState = new GameState(game);

		//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//Avoid the first bomb
		if (bombTargetAndEta == null) {
			if (virtualGameState.PrevGameState?.Bombs.Where(b => b.Owner == Players.Opponent).Count() < virtualGameState.Bombs.Where(b => b.Owner == Players.Opponent).Count()) {
				Console.Error.WriteLine("Detected Bomb Launched");
				if (virtualGameState.GetFactoriesOf(Players.Me).Count == 1) {
					Console.Error.WriteLine("I know for sure my opponent is bombing " + virtualGameState.GetFactoriesOf(Players.Me).First().EntityId);
					Console.Error.WriteLine("Bomb ETA is " + virtualGameState.GetFactoriesOf(Players.Me).First().GetDistanceToClosestFactory(virtualGameState, Players.Opponent));
					bombTargetAndEta = Tuple.Create(virtualGameState.GetFactoriesOf(Players.Me).First().EntityId, virtualGameState.GetFactoriesOf(Players.Me).First().GetDistanceToClosestFactory(virtualGameState, Players.Opponent)-1);
				}
			}
		}

		if(bombTargetAndEta != null && bombTargetAndEta.Item2 == virtualGameState.Turn-1) {
			//Evaquate motherfuckers. BOMB IS INCOMING
			Factory factoryToEvacuate = virtualGameState.GetFactory(bombTargetAndEta.Item1);
			Factory closestFactory = factoryToEvacuate.GetClosestFactory(virtualGameState, Players.Me);
			if(closestFactory == null) {
				//This is the short map. We don't have anywhere to run yet.
				closestFactory = factoryToEvacuate.GetClosestFactory(virtualGameState, Players.Neutral);
			}
			action.AppendMove(factoryToEvacuate.EntityId, closestFactory.EntityId, factoryToEvacuate.CyborgCount);
			virtualGameState = virtualGameState.UpdateGame_Move(factoryToEvacuate, closestFactory, factoryToEvacuate.CyborgCount);
		}
		//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


		//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//Reinforce Weak Factories
		HashSet<Factory> myHelpingFactories = new HashSet<Factory>();

		foreach (Factory myFact in virtualGameState.GetFactoriesOf(Players.Me).Where(f1 => f1.Production > 0)) {
			Console.Error.WriteLine("Factory " + myFact.EntityId + " Its VC is " + myFact.GetVirtualCyborgCount(virtualGameState));

			int myFactVirtualCount = myFact.GetVirtualCyborgCount(virtualGameState);
		
			if (myFactVirtualCount <= 0) {
				//This factory needs reinforcements
				//int reinforcementsNeeded = (myFactVirtualCount * -1) + 5;
				int reinforcementsNeeded = (myFact.GetVirtualCyborgCount(virtualGameState, 5) * -1) + 4;
				Console.Error.WriteLine("Factory " + myFact.EntityId + " need reinforcements! Its VC is "+myFactVirtualCount+" and I will send "+ ((myFactVirtualCount * -1) + 5) );
				//Get a list of factories ready to support, ordered by proximity

				while(reinforcementsNeeded > 0) {

					List<Factory> factoriesReadyToSupport = virtualGameState.GetFactoriesOf(Players.Me)
						.Where(f4 => myHelpingFactories.Contains(f4) == false )
						.Where(f1 => f1.GetVirtualCyborgCount(virtualGameState) > 1) 
						.Where(f3 => f3.CyborgCount > 2)
						.Where(f5 => f5.EntityId != myFact.EntityId)
						.OrderBy(f2 => virtualGameState.Graph[f2, myFact.EntityId])
						.ToList();

					if (factoriesReadyToSupport.Contains(myFact)){
						break;
					}

					if(factoriesReadyToSupport.Count == 0) {
						break;
					}

					Factory currFactory = factoriesReadyToSupport[0];
					factoriesReadyToSupport.RemoveAt(0);

					//Console.Error.WriteLine("reinforcementsNeeded " + reinforcementsNeeded);
					//Console.Error.WriteLine("currFactory.GetVirtualCyborgCount(virtualGameState) " + currFactory.GetVirtualCyborgCount(virtualGameState));
					//Console.Error.WriteLine("currFactory.CyborgCount - 1 " + (currFactory.CyborgCount - 1));

					int supportTroopsCount = new int[] {reinforcementsNeeded, currFactory.CyborgCount - 1, currFactory.GetVirtualCyborgCount(virtualGameState)-1 }.Min(); //Send your virtual cyborg count, but don't go over reinforcementsNeeded

					//int supportTroopsCount = Math.Min(reinforcementsNeeded, currFactory.GetVirtualCyborgCount(virtualGameState)); //Send your virtual cyborg count, but don't go over reinforcementsNeeded
									   
					//game.GetFactoriesOf(Players.Me).ForEach(f1=> Console.Error.WriteLine("Factory " + f1.EntityId + " owned by " + f1.Owner));

					Console.Error.WriteLine(string.Format("Reinforcement Move: {0} {1} {2}", currFactory.EntityId, myFact.EntityId, supportTroopsCount));
					action.AppendMove(currFactory, myFact, supportTroopsCount);
					virtualGameState = virtualGameState.UpdateGame_Move(currFactory, myFact, supportTroopsCount);
					reinforcementsNeeded -= supportTroopsCount;

					myHelpingFactories.Add(myFact);
				}
			}			
		}
		//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


		return virtualGameState;
	}

}

public class S_Macro : IStrategy {
	
	public GameState ExecuteStrategy(GameState game, ref CommandBuilder action) {
		//Use PowerUp
		GameState virtualGameState = new GameState(game);

		if (game.GetFactoriesOf(Players.Neutral).Where(f1 => f1.Production > 0).Count() == 0) {
			foreach (Factory myFact in virtualGameState.GetFactoriesOf(Players.Me)) {
				if (myFact.Production < 3 && myFact.GetVirtualCyborgCount(virtualGameState) - 10 >= 0) {
					if (game.GetProduction(Players.Opponent) - game.GetProduction(Players.Me) - 10 < 15) {
						action.AppendPowerup(myFact.EntityId);
						virtualGameState.GetFactory(myFact).CyborgCount -= 10;
					}
				}
			}
		}
		//Move troops to factories with low count to allow for Powerups in next turn(s)

		List<Factory> factoriesNotAtFullProduction = virtualGameState.GetFactoriesOf(Players.Me).Where(f1 => f1.Production < 3).Where(f2 => f2.GetVirtualCyborgCount(virtualGameState) < 40).ToList();

		foreach (Factory lowCountFactory in factoriesNotAtFullProduction) {
			int supportTroopsCount = 0;

			List<Factory> myFactoriesSorted = virtualGameState.GetFactoriesOf(Players.Me)
				.Where(f3 => f3.Production == 3)
				.Where(f2 => f2.GetVirtualCyborgCount(virtualGameState) >= 2)
				//.OrderByDescending(f1 => f1.GetDistanceToClosestFactory(virtualGameState, Players.Opponent))
				.OrderByDescending(f1 => f1.CyborgCount)
				.ToList();

			foreach (Factory supportingFactory in myFactoriesSorted) {
				Console.Error.WriteLine(string.Format("Macro Move: {0} {1} {2}", supportingFactory.EntityId, lowCountFactory.EntityId, supportingFactory.GetVirtualCyborgCount(virtualGameState) / 2));
				int supportTroopsSent = Math.Min(supportingFactory.GetVirtualCyborgCount(virtualGameState) / 2, 10);

				action.AppendMove(supportingFactory, lowCountFactory, supportTroopsSent);
				virtualGameState = virtualGameState.UpdateGame_Move(supportingFactory, lowCountFactory, supportTroopsSent);
				supportTroopsCount += supportTroopsSent;

				if (supportTroopsCount + lowCountFactory.CyborgCount + 1 >= 11 ) {
					break;
				}
			}

		}

		Factory closestFactToEnemy = game.GetFactoriesOf(Players.Me).OrderBy( f1 => game.GetFactoriesOf(Players.Opponent).Sum(f2 => f1.GetDistanceTo(game, f2)) ).First();
		Console.Error.WriteLine("I think the front factory is " + closestFactToEnemy.EntityId);

		return virtualGameState;
	}  

}

public class S_Neutral_Conquerer : IStrategy {

	public GameState ExecuteStrategy(GameState game, ref CommandBuilder action) {
		GameState virtualGame = new GameState(game);

		var factoriesToAttack = game.Factories
			.Where(f1 => f1.Owner == Players.Neutral)
			.Where(f3 => f3.GetVirtualCyborgCount(virtualGame) <= 0 )
			.Where(f6 => f6.Production > 0)	//Might want to take this out later. We'll see
			.Where(f5 => f5.GetDistanceToClosestFactory(game, Players.Me) <= f5.GetDistanceToClosestFactory(game, Players.Opponent)) //Don't cross the map.
			.OrderByDescending(f2 => f2.Production)
			.ThenBy(f4 => f4.CyborgCount)
			.ToList();

		factoriesToAttack.ForEach(f1 => Console.Error.WriteLine("Factory " + f1.EntityId + " VCC " + f1.GetVirtualCyborgCount(virtualGame) + " CC " + f1.CyborgCount));

		foreach (Factory neutralFactory in factoriesToAttack) {

			int troopsNeeded = neutralFactory.CyborgCount + 1;

			List<Factory> myFactoriesReadyForOffense = virtualGame.GetFactoriesOf(Players.Me)
				.Where(f3 => f3.GetVirtualCyborgCount(virtualGame) > 1)
				.OrderBy(f1 => virtualGame.Graph[f1, neutralFactory])
				.ThenByDescending(f2 => f2.CyborgCount).ToList();


			foreach (Factory currFactory in myFactoriesReadyForOffense) {
				if(currFactory.GetVirtualCyborgCount(virtualGame) > 1) {
				
					int attackTroopsCount = Math.Min(currFactory.GetVirtualCyborgCount(virtualGame)-1, troopsNeeded);
					action.AppendMove(currFactory, neutralFactory, attackTroopsCount);
					virtualGame = virtualGame.UpdateGame_Move(currFactory, neutralFactory, attackTroopsCount);
					troopsNeeded -= attackTroopsCount;
					if(troopsNeeded <= 0) {
						break;
					}
				}
			}																												 
		}

		return virtualGame;
	}

}

public class S_ShyExpander : IStrategy {

	public GameState ExecuteStrategy(GameState game, ref CommandBuilder action) {
		//Console.Error.WriteLine("my CC " + game.GetCyborgCount(Players.Me) + " opponent cc " + game.GetCyborgCount(Players.Opponent));

		if (game.GetFactoriesOf(Players.Me).Where(f1 => f1.Production != 3).Count() == 0
			&& game.GetProduction(Players.Me) <= game.GetProduction(Players.Opponent)
			//&& game.GetCyborgCount(Players.Me) > game.GetCyborgCount(Players.Opponent) 
			)  {

			//TODO this logic needs reviewing
			List<Factory> neutrals = game.GetFactoriesOf(Players.Neutral)
				.OrderBy( neutral => game.GetFactoriesOf(Players.Me).Sum(f3 => neutral.GetDistanceTo(game, f3)) )
				.ToList();

			neutrals.ForEach(f => Console.Error.WriteLine("Factory " + f.EntityId + " has " + game.GetFactoriesOf(Players.Me).Sum(f3 => f.GetDistanceTo(game, f3))) );

			Factory target = neutrals.FirstOrDefault();

			if (target != null) {


				action.AppendMove(target.GetClosestFactory(game, Players.Me), target, 1);
				game.UpdateGame_Move(target.GetClosestFactory(game, Players.Me), target, 1);
			}
		}
		return game;
	}

}

public class S_MoveToFrontier : IStrategy {

	public GameState ExecuteStrategy(GameState game, ref CommandBuilder action) {
		//List<Factory> sortedFactoriesDistance = game.GetFactoriesOf(Players.Me);


		//List<Factory> myFactories = game.GetFactoriesOf(Players.Me).OrderBy(f1 => f1.GetDistanceToClosestFactory(game, Players.Opponent)).ToList();
		int min = game.GetFactoriesOf(Players.Me).Min(f1 => f1.GetDistanceToClosestFactory(game, Players.Opponent));
		List<Factory> myFront = game.GetFactoriesOf(Players.Me).Where(f1 => f1.GetDistanceToClosestFactory(game, Players.Opponent) == min).ToList();
		int frontFactoryCount = myFront.Count();
		int totalFactoryCount = game.GetFactoriesOf(Players.Me).Count();
		int nonFrontFactoryCount = totalFactoryCount - frontFactoryCount;

		int targetFrontVC = (game.GetFactoriesOf(Players.Me).Sum(f => f.GetVirtualCyborgCount(game)) - nonFrontFactoryCount) / frontFactoryCount;

		List<Factory> nonFrontFactories = game.GetFactoriesOf(Players.Me).Where(f1 => f1.GetDistanceToClosestFactory(game, Players.Opponent) != min).ToList();
		foreach (Factory nonFrontFactory in nonFrontFactories) {
			Factory factoryToReinforce = myFront.Where(f => f.GetVirtualCyborgCount(game) < targetFrontVC).FirstOrDefault();
			if(factoryToReinforce == null) {
				break;
			}
			
			if(nonFrontFactory.Production < 3) {
				if(nonFrontFactory.CyborgCount > 10 && nonFrontFactory.GetVirtualCyborgCount(game) > 11) {
					action.AppendMove(nonFrontFactory, factoryToReinforce, nonFrontFactory.GetVirtualCyborgCount(game) - 11);
					game.UpdateGame_Move(nonFrontFactory, factoryToReinforce, nonFrontFactory.GetVirtualCyborgCount(game) - 11);
				}

			}	else {
				if (nonFrontFactory.GetVirtualCyborgCount(game) - 1 > 1) {
					action.AppendMove(nonFrontFactory, factoryToReinforce, nonFrontFactory.GetVirtualCyborgCount(game) - 1);
					game.UpdateGame_Move(nonFrontFactory, factoryToReinforce, nonFrontFactory.GetVirtualCyborgCount(game) - 1);
				}
			}
		}	   

		int averageVC = game.GetFactoriesOf(Players.Me).Sum(f1 => f1.GetVirtualCyborgCount(game)) / game.GetFactoriesOf(Players.Me).Count();


		foreach(Factory myFact in game.GetFactoriesOf(Players.Me)) {
			int vcMissing = averageVC - myFact.GetVirtualCyborgCount(game);
			if(vcMissing > 0) {
				foreach(Factory myOtherFact in game.GetFactoriesOf(Players.Me)) {
					if(myOtherFact.GetVirtualCyborgCount(game) > averageVC) {
						action.AppendMove(myOtherFact, myFact, myOtherFact.GetVirtualCyborgCount(game) - averageVC);
						game.UpdateGame_Move(myOtherFact, myFact, myOtherFact.GetVirtualCyborgCount(game) - averageVC);
					}
				}
			}
		}
		return game;
	}

}

public class S_Bomber : IStrategy {

	int bombCount = 2;
	Tuple<Factory, int> lastBombedFactory;

	public S_Bomber(int bombs) {
		this.bombCount = bombs;
	}

	public GameState ExecuteStrategy(GameState game, ref CommandBuilder action) {
		//Console.Error.WriteLine("Bomb count "+ bombCount);
		if(bombCount > 0) {
			//int targetProduction = game.Factories.Max(f => f.Production);
			int targetProduction = 3;
			Factory enemyFactoryMatchingProduction =
				game.GetFactoriesOf(Players.Opponent)
				.Where(f => f.Production == targetProduction)
				.Where(f2 => game.Bombs.Where(b=>b.Target == f2).Count() == 0 )
				.OrderBy(f1 => f1.GetDistanceToClosestFactory(game, Players.Me))
				.FirstOrDefault();

			game.GetFactoriesOf(Players.Opponent)
				.Where(f => f.Production == targetProduction)
				.Where(f2 => game.Bombs.Where(b => b.Target == f2).Count() == 0)
				.OrderBy(f1 => f1.GetDistanceToClosestFactory(game, Players.Me))
				.ToList().ForEach(f3 => Console.Error.WriteLine("POSSIBLE TARGET " + f3.EntityId));


			if(enemyFactoryMatchingProduction != null) {
				if(lastBombedFactory == null || (enemyFactoryMatchingProduction.EntityId != lastBombedFactory.Item1.EntityId || game.Turn > lastBombedFactory.Item2 + 7)) {
					action.AppendBomb(enemyFactoryMatchingProduction.GetClosestFactory(game, Players.Me), enemyFactoryMatchingProduction);
					bombCount--;
					lastBombedFactory = Tuple.Create(enemyFactoryMatchingProduction, game.Turn);
				}
			}
		}		
		return game;
	}

}

public class S_VCDistributorFromBackfield : IStrategy {
	
	public GameState ExecuteStrategy(GameState game, ref CommandBuilder action) {
		if (game.GetFactoriesOf(Players.Me).Where(f1 => f1.Production != 3).Count() != 0) {
			return game;
		}


		List<Factory> factoriesByClosest = game.GetFactoriesOf(Players.Me).OrderBy(f1 => f1.GetDistanceToClosestFactory(game, Players.Opponent)).ToList();
		List<Factory> moreImportantFactories = factoriesByClosest.Take(factoriesByClosest.Count() + 1 / 2).ToList();
		List<Factory> backfieldFactories = factoriesByClosest.Skip(factoriesByClosest.Count() + 1 / 2).ToList();

		int totalVC = game.GetFactoriesOf(Players.Me).Sum(f1 => f1.GetVirtualCyborgCount(game));
		int importantFactoryVC = (totalVC - backfieldFactories.Count()) / moreImportantFactories.Count();

		Console.Error.WriteLine("Total VC " + totalVC + " - importantFactory VC " + importantFactoryVC);

		foreach (Factory backFieldFact in backfieldFactories) {
			Console.Error.WriteLine(backFieldFact.EntityId + " is a backfield factory, VC = " + backFieldFact.GetVirtualCyborgCount(game));

			if (backFieldFact.GetVirtualCyborgCount(game) > 2) {
				
				//int troopsToSend = new int[] { backFieldFact.CyborgCount - 1, backFieldFact.GetVirtualCyborgCount(game) - 1 }.Min();
				int troopsToSend = new int[] { backFieldFact.GetVirtualCyborgCount(game) - Math.Min(importantFactoryVC, backFieldFact.GetVirtualCyborgCount(game) - 1), backFieldFact.CyborgCount - 1 }.Min();

				Factory factoryToHelp = moreImportantFactories
					.Where(f2 => f2.GetVirtualCyborgCount(game) < importantFactoryVC)
					.OrderBy(f1 => f1.GetVirtualCyborgCount(game))
					.FirstOrDefault();

				if(factoryToHelp != null) {
					action.AppendMove(backFieldFact, factoryToHelp, troopsToSend);
					game.UpdateGame_Move(backFieldFact, factoryToHelp, troopsToSend);
					Console.Error.WriteLine(backFieldFact.EntityId + " with VC "+backFieldFact.GetVirtualCyborgCount(game)+" will send "+troopsToSend+" to "+factoryToHelp.EntityId);
				}
			}
		} 


		//int averageVC = totalVC / game.GetFactoriesOf(Players.Me).Count();

		//Console.Error.WriteLine("Total VC "+totalVC+" - Average VC "+ averageVC);
		//Console.Error.WriteLine("My Factory Count " + game.GetFactoriesOf(Players.Me).Count());
		//if (totalVC > game.GetFactoriesOf(Players.Me).Count()) {
		//	foreach(Factory surplusFactory in game.GetFactoriesOf(Players.Me).Where(f1 => f1.Production == 3).Where(f => f.GetVirtualCyborgCount(game) > averageVC)) {
		//		Console.Error.WriteLine(surplusFactory.EntityId + " is a surplus factory, VC = " + surplusFactory.GetVirtualCyborgCount(game));
		//		int troopsToSend = new int[] { surplusFactory.GetVirtualCyborgCount(game) - Math.Min(averageVC, surplusFactory.GetVirtualCyborgCount(game)-1), surplusFactory.CyborgCount - 1 }.Min();

		//		Factory factoryToHelp = game.GetFactoriesOf(Players.Me).Where(f1 => f1.GetVirtualCyborgCount(game) < averageVC).OrderBy(f => f.GetVirtualCyborgCount(game)).FirstOrDefault();

		//		action.AppendMove(surplusFactory, factoryToHelp, troopsToSend);
		//		game.UpdateGame_Move(surplusFactory, factoryToHelp, troopsToSend);
		//		Console.Error.WriteLine();

		//	}
		//}
		return game;
	}

}

public class S_CloseCombatAttacker : IStrategy {

	public GameState ExecuteStrategy(GameState game, ref CommandBuilder action) {
		List<Factory> sortedTargetFactories = game.GetFactoriesOf(Players.Opponent)
			.Where(f3 => f3.Production > 0)
			.Where(f1 => f1.GetDistanceToClosestFactory(game, Players.Me) <= 3)
			.OrderByDescending(f2 => f2.GetVirtualCyborgCount(game)).ToList();

		if(sortedTargetFactories.Count == 0) {
			return game;
		}

		Factory targetFactory = sortedTargetFactories.First();

		if (targetFactory == null) {
			return game;
		}

		Factory attackFactory = game.GetFactoriesOf(Players.Me)
			.Where(f3 => f3.GetVirtualCyborgCount(game) > 5)
			.OrderBy(f1 => f1.GetDistanceTo(game, targetFactory))
			.ThenBy(f2 => f2.GetVirtualCyborgCount(game))
			.FirstOrDefault();

		Console.Error.WriteLine("I really want to build an attack from " + attackFactory?.EntityId + " to " + targetFactory?.EntityId);

		//Attack 
		if (attackFactory != null && attackFactory.CyborgCount + 6 > targetFactory.CyborgCount) {
			if(attackFactory.Production > 0 || game.GetFactoriesOf(Players.Me).Where(f1 => f1.Production != 3).Count() != 0) {
				action.AppendMove(attackFactory, targetFactory, attackFactory.CyborgCount - 5);
				game.UpdateGame_Move(attackFactory, targetFactory, attackFactory.CyborgCount - 5);
			}
		}				
		//OR Give me all your power




		//List<Factory> myFactoriesReadyToAttack = game.GetFactoriesOf(Players.Me).Where(f1 => f1.GetVirtualCyborgCount(game) > )
		return game;

	}

}

public class S_VCDistributor_Equal : IStrategy {

	public GameState ExecuteStrategy(GameState game, ref CommandBuilder action) {
		if (game.GetFactoriesOf(Players.Me).Where(f1 => f1.Production != 3).Count() != 0) {
			return game;
		}

		int totalVC = game.GetFactoriesOf(Players.Me).Sum(f1 => f1.GetVirtualCyborgCount(game));
		int averageVC = totalVC / game.GetFactoriesOf(Players.Me).Count();

		Console.Error.WriteLine("Total VC " + totalVC + " - Average VC " + averageVC);
		Console.Error.WriteLine("My Factory Count " + game.GetFactoriesOf(Players.Me).Count());
		if (totalVC > game.GetFactoriesOf(Players.Me).Count()) {
			foreach (Factory surplusFactory in game.GetFactoriesOf(Players.Me).Where(f1 => f1.Production == 3).Where(f => f.GetVirtualCyborgCount(game) > averageVC)) {
				Console.Error.WriteLine(surplusFactory.EntityId + " is a surplus factory, VC = " + surplusFactory.GetVirtualCyborgCount(game));
				int troopsToSend = new int[] { surplusFactory.GetVirtualCyborgCount(game) - Math.Min(averageVC, surplusFactory.GetVirtualCyborgCount(game) - 1), surplusFactory.CyborgCount - 1 }.Min();

				Factory factoryToHelp = game.GetFactoriesOf(Players.Me).Where(f1 => f1.GetVirtualCyborgCount(game) < averageVC).OrderBy(f => f.GetVirtualCyborgCount(game)).FirstOrDefault();

				action.AppendMove(surplusFactory, factoryToHelp, troopsToSend);
				game.UpdateGame_Move(surplusFactory, factoryToHelp, troopsToSend);
				Console.Error.WriteLine();

			}
		}
		return game;
	}

}

public class S_Swarmer : IStrategy {

	public int myBombsCount = 1;

	public GameState ExecuteStrategy(GameState game, ref CommandBuilder action) {
		if(game.GetProduction(Players.Me) > game.GetProduction(Players.Opponent)) {
			return game;
		}

		if(game.GetFactoriesOf(Players.Me).Where(f1 => f1.Production != 3).Count() != 0) {
			Console.Error.WriteLine("Not swarming yet");
			return game;
		}

		if(myBombsCount > 0) {
			Console.Error.WriteLine("Looking for target for second bomb");
			Factory secondBombTarget = game.GetFactoriesOf(Players.Opponent)
				.OrderByDescending(f2 => f2.CyborgCount)
				.ThenBy(f1 => f1.GetDistanceToClosestFactory(game, Players.Me)).FirstOrDefault();

			if (secondBombTarget != null) {
				Console.Error.WriteLine("Sending Second Bomb");
				action.AppendBomb(secondBombTarget.GetClosestFactory(game, Players.Me), secondBombTarget.EntityId);
				myBombsCount--;
				secondBombTarget.GetClosestFactory(game, Players.Me).CyborgCount = 0;
			}
		}


		GameState virtualGame = new GameState(game);

		List<Factory> sortedEnemyFactory = virtualGame.GetFactoriesOf(Players.Opponent)
			.OrderBy(f1 => f1.GetVirtualCyborgCount(virtualGame) + f1.GetDistanceToClosestFactory(virtualGame, Players.Me) * f1.Production)
			.ToList();

		sortedEnemyFactory.ForEach(f1 => Console.Error.WriteLine(
			string.Format("Factory {0}, strength {1}", 
			f1.EntityId, f1.GetVirtualCyborgCount(virtualGame) + f1.GetDistanceToClosestFactory(virtualGame, Players.Me) * f1.Production )));

		Factory weakestEnemyFactory = virtualGame.GetFactoriesOf(Players.Opponent)
			.Where(f3 => f3.Production > 0)
			.OrderByDescending(f1 => f1.GetVirtualCyborgCount(virtualGame) + f1.GetDistanceToClosestFactory(virtualGame, Players.Me) * f1.Production)
			.FirstOrDefault();

		List<Factory> factoriesReadyToAttack = virtualGame.GetFactoriesOf(Players.Me).Where(f1 => f1.GetVirtualCyborgCount(virtualGame) > 10).ToList();

		if(weakestEnemyFactory != null) {
			//Let's conquer this motherfucker
			foreach(Factory currFactory in factoriesReadyToAttack) {
				action.AppendMove(currFactory, weakestEnemyFactory, 5);
				virtualGame = virtualGame.UpdateGame_Move(currFactory, weakestEnemyFactory, 5);
			}
		}

		return virtualGame;
	}

}


public class S_IDontEvenKnow : IStrategy {

	public GameState ExecuteStrategy(GameState game, ref CommandBuilder action) {
		Console.Error.WriteLine("Standard Mode");
		//Bomb Command
		if (game.GetFactoriesOf(Players.Me).Count() <= 0) {
			return game;
		}

		var bombTarget = game.GetFactoriesOf(Players.Opponent)
			.Where(f1 => f1.CyborgCount >= 10 && f1.Production == 3)
			.OrderBy(f2 => game.Graph[f2.GetClosestFactory(game, Players.Me), f2])
			.FirstOrDefault();

		if (bombTarget != null) {
			action.AppendBomb(bombTarget.GetClosestFactory(game, Players.Me), bombTarget);
		}

		//Increase Commands
		foreach (var ownedFactory in game.Factories.Where(f => f.Owner == Players.Me).OrderBy(f => f.Production)) {
			if (ownedFactory.CyborgCount >= 20 && ownedFactory.Production < 3) {
				action.AppendPowerup(ownedFactory.EntityId);
			}
		}

		// Move Commands
		// Tuple represents Factory and its Strengh. Lower strength means easier to conquer
		List<Tuple<Factory, float>> sortedTargets = game.GetUnownedFactories(Players.Me)
			.Where(f3 => f3.Production > 0)
			.Select(f1 => Tuple.Create(f1, f1.CyborgCount / (f1.Production + float.Epsilon)))
			.OrderBy(f2 => f2.Item2)
			//.ThenBy(f3 => game.Graph[f3.Item1, f3.Item1.GetClosestFactory(game, Players.Me)])
			.ToList();

		Console.Error.WriteLine("sortedTargets");
		sortedTargets.ForEach(t => Console.Error.WriteLine(t.Item1.EntityId + " " + t.Item2));


		//If there is no factory to conquer, just wait. We're doing good
		if (sortedTargets.Count == 0) {
			return game;
		}

		float minStrength = sortedTargets.Min(f1 => f1.Item2);

		Factory target = sortedTargets
			.Where(f1 => f1.Item2 == minStrength)
			.OrderBy(f2 => f2.Item1.CyborgCount)
			.First().Item1;

		Factory myBestFactory = game.GetFactoriesOf(Players.Me).OrderByDescending(f1 => f1.CyborgCount).First();
		Console.Error.WriteLine("myBestFactory.EntityId " + myBestFactory.EntityId);
		Console.Error.WriteLine("target.EntityId" + target.EntityId);
		action.AppendMove(myBestFactory.EntityId, target.EntityId, Math.Min(Math.Max(0, myBestFactory.CyborgCount - 1), 4));

		return game;
	}
}

public class S_MacroPowerful : IStrategy {

	private GameState game;

	public GameState ExecuteStrategy(GameState game, ref CommandBuilder action) {
		this.game = game;
		bool shouldTryMacro = false;
		action = PowerfulStep(action, out shouldTryMacro);
		if (shouldTryMacro) {
			action = MacroStep(action);
		}

		return game;
	}

	public CommandBuilder MacroStep(CommandBuilder action) {
		return action;
	}

	public CommandBuilder PowerfulStep(CommandBuilder action, out bool result) {
		List<Factory> enemyFactories = game.GetFactoriesOf(Players.Opponent).OrderBy(f1 => f1.CyborgCount).ToList();

		foreach (Factory enemyFact in enemyFactories) {
			int enemyTroops = enemyFact.CyborgCount;
			List<Factory> myFactories = game.GetFactoriesOf(Players.Me).OrderByDescending(f1 => f1.CyborgCount).ToList();
			int attackCount = 0;

			for (int i = 0; i < myFactories.Count; i++) {
				List<Factory> myAttackingFactories = myFactories.Take(i + 1).ToList();
				attackCount = myFactories.Take(i + 1).Sum(f1 => f1.CyborgCount - 1);
				int increasedTroopsToAccountForDistance = enemyTroops + enemyFact.Production * game.Graph[enemyFact, enemyFact.GetFurthestFactory(game, myAttackingFactories)];
				if (attackCount >= increasedTroopsToAccountForDistance) {
					//Let's go
					CommandBuilder attackCmd = new CommandBuilder();
					for (int x = 0; x < i; x++) {
						Factory myFact = myFactories[x];
						int attackTroopCount = myFactories[x].CyborgCount - 1;
						attackCmd.AppendMove(myFact, enemyFact, attackTroopCount);
						increasedTroopsToAccountForDistance -= attackTroopCount;
						if (increasedTroopsToAccountForDistance <= 0) {
							result = true; //Success
							Console.Error.WriteLine("I'm really doing this ");
							return action.AppendActions(attackCmd);
						}
					}
				}
			}
		}
		result = false;
		Console.Error.WriteLine("I'm really doing this 2");
		return action;
	}
}


public class S_Powerful : IStrategy {

	public GameState ExecuteStrategy(GameState game, ref CommandBuilder action) {
		List<Factory> myFactories = game.GetFactoriesOf(Players.Me).OrderByDescending(f1 => f1.CyborgCount).ToList();
		Factory myBestFactory = myFactories.First();


		var factoriesToAttack = game.Factories
			.Where(f1 => f1.Owner != Players.Me)
			.Where(f2 => f2.Production >= 1)
			.OrderBy(f4 => f4.CyborgCount)
			.ThenBy(f3 => game.Graph[myFactories.First(), f3]);

		var enemyFactory = factoriesToAttack.First();
		//foreach (var enemyFactory in factoriesToAttack) {
		List<Factory> myFactoriesCopy = new List<Factory>(myFactories);
		//Factory currFactory = myFactoriesCopy.First();
		//myFactoriesCopy.RemoveAt(0);
		//int troopsAvailable = currFactory.CyborgCount /2;

		Console.Error.WriteLine("Should I attack Factory " + enemyFactory.EntityId);

		int incomingTroopsToFactory = game.Troops.Where(t1 => t1.Target == enemyFactory).Aggregate(0, (agg, t2) => agg + (int)t2.Owner * t2.TroopCount);
		Console.Error.WriteLine("Incoming Trops will alter " + incomingTroopsToFactory);

		int enemyFactoryCC = enemyFactory.CyborgCount + game.Graph[enemyFactory, myBestFactory] * enemyFactory.Production + incomingTroopsToFactory + 5;
		Console.Error.WriteLine("Estimated Troops to Conquer Factory " + enemyFactory + " is " + enemyFactoryCC);

		int troopsAvailable = 0;
		CommandBuilder conquerFactoryAction = new CommandBuilder();
		while (troopsAvailable < enemyFactoryCC) {
			if (myFactoriesCopy.Count > 0) {
				conquerFactoryAction.Clear();
				break;
			}

			var myFactory = myFactoriesCopy.First();
			myFactoriesCopy.RemoveAt(0);
			troopsAvailable += myFactory.CyborgCount / 3 * 2;
			conquerFactoryAction.AppendMove(myFactory, enemyFactory, myFactory.CyborgCount / 3 * 2);
		}

		action = action.AppendActions(conquerFactoryAction);
		return game;


		//action.AppendMove(myFactory, enemyFactory, enemyFactory.CyborgCount + 1);
		//Console.Error.WriteLine("Troops Before: " + troopsAvailable);
		//troopsAvailable -= (enemyFactory.CyborgCount + 1);
		//Console.Error.WriteLine("Troops Afterwards: " + troopsAvailable);
		//if (troopsAvailable <= 0) {
		//	break;
		//}
		//}
	}
}
#endregion
#region Game Data Structures
public class GameState {

	public List<Entity> Entities { get; private set; }
	public List<Factory> Factories { get; private set; }
	public List<Bomb> Bombs { get; private set; }
	public Graph Graph { get; private set; }
	public List<Troops> Troops { get; private set; }

	public int Turn { get; private set; }
	public GameState PrevGameState { get; set; }

	public GameState AdvanceInputless(int noOfTurns) {
		return AdvanceInputless_Listed(noOfTurns).Last();
	}

	public GameState(Graph mapGraph, List<Entity> entities, int turn, GameState prevGameState) {
		this.Graph = mapGraph;
		this.Entities = entities;
		this.Factories = entities.Where(e => e is Factory).Cast<Factory>().ToList();
		this.Troops = entities.Where(e => e is Troops).Cast<Troops>().ToList();
		this.Bombs = entities.Where(e => e is Bomb).Cast<Bomb>().ToList();
		this.Turn = turn;
		this.PrevGameState = prevGameState;
	}

	public GameState(GameState game) {
		this.Graph = game.Graph;
		this.Entities = new List<Entity>(game.Entities);
		this.Factories = Entities.Where(e => e is Factory).Cast<Factory>().ToList();
		this.Troops = Entities.Where(e => e is Troops).Cast<Troops>().ToList();
		this.Bombs = Entities.Where(e => e is Bomb).Cast<Bomb>().ToList();
		this.Turn = game.Turn;
		this.PrevGameState = game.PrevGameState;
	}

	public List<GameState> AdvanceInputless_Listed(int noOfTurns) {
		List<GameState> result = new List<GameState>();
		for (int i = 0; i < noOfTurns; i++) {
			result.Add(AdvanceInputlessStep());
		}
		return result;

	}

	public List<Factory> GetFactoriesOf(Players owner) {
		return Factories.Where(f => f.Owner == owner).ToList();
	}

	public Factory GetFactory(int factoryId) {
		return Factories.First(f => f.EntityId == factoryId);
	}
	public int GetProduction(Players owner) {
		return Factories.Where(f1 => f1.Owner == owner).Select(f2 => f2.Production).Sum();
	}

	public List<Factory> GetUnownedFactories(Players owner) {
		return Factories.Where(f => f.Owner != owner).ToList();
	}


	private GameState AdvanceInputlessStep() {

		//Move existing troops and bombs
		//Execute user orders
		//Produce new cyborgs in all factories
		//Solve battles
		//Make the bombs explode
		//Check end conditions


		return null;
	}

	public GameState UpdateGame_Move(Factory startFactory, Factory myFact, int supportTroopsCount) {
		GameState resultGame = new GameState(this);

		Factory resultStartFact = new Factory(resultGame.GetFactory(startFactory));
		resultStartFact.CyborgCount -= supportTroopsCount;
		//Make the factory Switch-a-roo
		resultGame.Factories.Remove(startFactory);
		resultGame.Factories.Add(resultStartFact);

		int maxEntityId = this.Entities.Max(e => e.EntityId);
		Troops troops = new Troops(maxEntityId+1, (int)resultStartFact.Owner, resultStartFact, myFact, supportTroopsCount, Graph[resultStartFact, myFact]);
		resultGame.Troops.Add(troops);

		return resultGame;
	}

	public int GetCyborgCount(Players owner) {
		int troopsCount = Troops.Where(t2 => t2.Owner == owner).Sum(t1 => t1.TroopCount);
		int factoriesCount = Factories.Where(t2 => t2.Owner == owner).Sum(f => f.CyborgCount);
		return troopsCount + factoriesCount;
	}
}

public class Graph {

	private int[][] adjacencyMatrix;
	private int noOfNodes;

	public Graph(int noOfNodes) {
		this.noOfNodes = noOfNodes;
		adjacencyMatrix = new int[noOfNodes][];
		for (int i = 0; i < adjacencyMatrix.Length; i++) { adjacencyMatrix[i] = new int[noOfNodes]; }
	}

	public int this[int x, int y] {
		get { return adjacencyMatrix[x][y]; }
		private set { adjacencyMatrix[x][y] = adjacencyMatrix[y][x] = value; }
	}
	public void AddConnection(int node1, int node2, int cost) {
		Debug.Assert(node1 < noOfNodes);
		Debug.Assert(node2 < noOfNodes);
		Debug.Assert(cost > 0);

		adjacencyMatrix[node1][node2] = adjacencyMatrix[node2][node1] = cost;
	}
}

public class Factory : Entity {
	private Factory factory;

	public Factory(int entityId, int owner, int cyborgCount, int production) : base(entityId, owner) {
		Debug.Assert(production <= 3);
		Debug.Assert(cyborgCount >= 0);

		this.CyborgCount = cyborgCount;
		this.Production = production;
	}

	public Factory(Factory factory) : base(factory.EntityId, (int) factory.Owner) {
		Debug.Assert(factory.Production <= 3);
		Debug.Assert(factory.CyborgCount >= 0);

		this.CyborgCount = factory.CyborgCount;
		this.Production = factory.Production;
	}

	public int CyborgCount { get; set; }
	public int Production { get; private set; }
	public Factory GetClosestFactory(GameState game, Players ownerFilter) {
		return game.Factories.Where(f1 => f1.Owner == ownerFilter).Where(f3 => f3 != this).OrderBy(f2 => game.Graph[this, f2]).FirstOrDefault();
	}

	public int GetDistanceTo(GameState game, Factory f) {
		return game.Graph[this, f.EntityId];
	}

	public int GetDistanceToClosestFactory(GameState game, Players ownerFilter) {
		if(GetClosestFactory(game, ownerFilter) == null) {
			return int.MaxValue;
		}
		return GetDistanceTo(game, GetClosestFactory(game, ownerFilter));
	}

	public int GetDistanceToFurthestFactory(GameState game, Players ownerFilter) {
		return GetDistanceTo(game, GetFurthestFactory(game, ownerFilter));
	}

	public Factory GetClosestFactory(GameState game, List<Factory> factories) {
		return factories.OrderBy(f2 => game.Graph[this, f2]).FirstOrDefault();
	}

	public Factory GetFurthestFactory(GameState game, Players owner) {
		return game.Factories.Where(f1 => f1.Owner == owner).Where(f3 => f3 != this).OrderByDescending(f2 => game.Graph[this, f2]).FirstOrDefault();
	}

	public Factory GetFurthestFactory(GameState game, List<Factory> factories) {
		return factories.OrderByDescending(f2 => game.Graph[this, f2]).FirstOrDefault();
	}

	public int GetVirtualCyborgCount(GameState game) {
		if(this.Owner == Players.Neutral) {
			//int temp = game.Troops
			//	.Where(t1 => t1.Target == EntityId)
			//	.Sum(t2 => (t2.Owner == Players.Me) ? t2.TroopCount : -t2.TroopCount);

			//return (Math.Abs(temp) - this.CyborgCount);
			
			return (int)Players.Opponent * this.CyborgCount + game.Troops
				.Where(t1 => t1.Target == EntityId)
				.Sum(t2 => (t2.Owner == Players.Me) ? t2.TroopCount : -t2.TroopCount);
		}

		return (int)this.Owner * this.CyborgCount + game.Troops
			.Where(t1 => t1.Target == EntityId)
			.Sum(t2 => (t2.Owner == Owner) ? t2.TroopCount : -t2.TroopCount);
	}

	public int GetVirtualCyborgCount(GameState game, int turnsCap) {
		if (this.Owner == Players.Neutral) {
			//int temp = game.Troops
			//	.Where(t1 => t1.Target == EntityId)
			//	.Sum(t2 => (t2.Owner == Players.Me) ? t2.TroopCount : -t2.TroopCount);

			//return (Math.Abs(temp) - this.CyborgCount);

			return (int)Players.Opponent * this.CyborgCount + game.Troops
				.Where(t1 => t1.Target == EntityId)
				.Where(t3 => t3.Eta <= turnsCap)
				.Sum(t2 => (t2.Owner == Players.Me) ? t2.TroopCount : -t2.TroopCount);
		}

		return (int)this.Owner * this.CyborgCount + game.Troops
			.Where(t1 => t1.Target == EntityId)
			.Where(t3 => t3.Eta <= turnsCap)
			.Sum(t2 => (t2.Owner == Owner) ? t2.TroopCount : -t2.TroopCount);
	}

	public List<Troops> GetIncomingTroops(GameState game, Players owner) {
		return game.Troops.Where(t1 => t1.Target == this && t1.Owner == owner).ToList();											
	}

	public List<Troops> GetAllIncomingTroops(GameState game) {
		return GetIncomingTroops(game, Players.Me).Concat(GetIncomingTroops(game, Players.Opponent)).ToList();
	}

	

	//public PredictedState FuturePredictedState(GameState game) {
	//	int enemyIncomingTroops = game.Troops.Where(t1 => t1.Owner == Players.Opponent).Where(t2 => t2.Target == this.EntityId).Select(t3 => t3.TroopCount).Sum();
	//	int alliedIncomingTroops = game.Troops.Where(t1 => t1.Owner == Players.Me).Where(t2 => t2.Target == this.EntityId).Select(t3 => t3.TroopCount).Sum();

	//	if (Owner == Players.Neutral) {
	//		if (CyborgCount > enemyIncomingTroops + alliedIncomingTroops) {
	//			return new PredictedState(Players.Neutral, CyborgCount - enemyIncomingTroops - alliedIncomingTroops);
	//		} 
	//	}

	//	int maxEta = GetMaxIncomingTroopEta(game);

	//	for (int i=0; i<maxEta; i++) {

	//	}

	//	return 0;
	//}

	//public List<Troops> GetLastTroopsIncoming(GameState game) {
	//	return game.Troops.Where(t=>t.Eta == GetMaxIncomingTroopEta(game)).ToList();
	//}

	//public int GetMaxIncomingTroopEta(GameState game) {
	//	return GetIncomingTroops(game).Max(t => t.Eta);
	//}

	//public List<Troops> GetIncomingTroops(GameState game) {
	//	return game.Troops.Where(t => t.Target == this.EntityId).ToList();
	//}

}

public enum Players {
	Opponent = -1, Neutral = 0, Me = 1
}
public struct PredictedState {

	public int cyborgCount;
	public Players owner;
	public PredictedState(Players owner, int cyborgCount) {
		this.owner = owner;
		this.cyborgCount = cyborgCount;
	}

}

public class Bomb : Entity {

	public Bomb(int entityId, int owner, Factory start, Factory target, int eta) : base(entityId, owner) {
		this.Start = start;
		this.Target = target;
		this.Eta = eta;
	}

	public int Eta { get; private set; }
	public Factory Start { get; private set; }
	public Factory Target { get; private set; }
}

public class CommandBuilder {

	private string _result = "";
	public string Result {
		get { return "WAIT" + _result; }
	}

	//public void AppendMove(Factory start, Factory target, int count) {
	//	Result += string.Format("MOVE {0} {1} {2};", start.EntityId, target.EntityId, count); 		
	//}

	public CommandBuilder AppendActions(CommandBuilder actions) {
		CommandBuilder resultCommandBuilder = new CommandBuilder();

		if (actions._result != "") {
			resultCommandBuilder._result = this._result + actions._result;
		}

		return resultCommandBuilder;
	}

	public void AppendBomb(int startId, int targetId) {
		_result += string.Format(";BOMB {0} {1}", startId, targetId);
	}

	public void AppendMove(int startId, int targetId, int count) {
		//Console.Error.WriteLine(string.Format(" MOVE {0} {1} {2} ", startId, targetId, count));
		//Console.Error.WriteLine(Environment.StackTrace);

		if (count <= 0) {
			Console.Error.WriteLine(string.Format("ERROR: MOVE {0} {1} {2} is invalid", startId, targetId, count));
			Console.Error.WriteLine(Environment.StackTrace);
			return;
		}
		//Debug.Assert(count > 0);

		_result += string.Format(";MOVE {0} {1} {2}", startId, targetId, count);
	}

	public void AppendPowerup(int factoryId) {
		_result += ";INC " + factoryId;
	}

	public void Clear() {
		_result = "";
	}
}

public abstract class Entity {

	public Entity(int entityId, int owner) {
		this.EntityId = entityId;
		this.Owner = (Players)owner;
	}

	public int EntityId { get; private set; }
	public Players Owner { get; private set; }

	public static implicit operator int(Entity value) {
		return value.EntityId;
	}
}
public class Troops : Entity {

	public Troops(int entityId, int owner, Factory start, Factory target, int troopCount, int eta) : base(entityId, owner) {
		this.Start = start;
		this.Target = target;
		this.TroopCount = troopCount;
		this.Eta = eta;
	}

	public int Eta { get; private set; }
	public Factory Start { get; private set; }
	public Factory Target { get; private set; }
	public int TroopCount { get; private set; }
}
#endregion

