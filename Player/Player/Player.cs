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
		Odinoo odinoo = new Odinoo();

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
		while (true) {
			GameState game = null;
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

			game = new GameState(map, entities);
			string result = odinoo.Think(game);
			Console.WriteLine(result);
			//foreach (var ownedFactory in game.Factories.Where(f => f.Owner == Players.Me).OrderBy(f => f.Production)) {
			//	if (ownedFactory.CyborgCount >= 20 && ownedFactory.Production < 3) {
			//		action.AppendPowerup(ownedFactory.EntityId);
			//	}
			//}	
		}
	}
}
#region Logic
public interface IStrategy {
	GameState ExecuteStrategy(GameState game, ref CommandBuilder action);
}

public class Odinoo {

	public List<IStrategy> strategists;

	private int turn = 0;
	private int myBombsCount = 2;

	public Odinoo() {
		SwitchStrategy(new IStrategy[] { new S_Defender() });
	}

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
			//Console.Error.WriteLine("Attacking Factory " + factory.EntityId);
			action.AppendMove(myBestFactory, factory, factory.CyborgCount + 1);
			//Console.Error.WriteLine("Troops Before: " +troopsAvailable);
			troopsAvailable -= (factory.CyborgCount + 1);
			//Console.Error.WriteLine("Troops Afterwards: " + troopsAvailable);
			if (troopsAvailable <= 0) {
				break;
			}
		}
	}

	public void SwitchStrategy(IEnumerable<IStrategy> newStrategy) {
		this.strategists = newStrategy.ToList();
	}

	public void AddStrategy(IStrategy strategy) {
		this.strategists.Add(strategy);
	}

	public string Think(GameState game) {
		turn++;
		CommandBuilder action = new CommandBuilder();

		//game.Factories
		//	.Where(f1 => f1.Owner != Players.Neutral).ToList()
		//	.ForEach(f2 => Console.Error.WriteLine("Factory " + f2.EntityId + " Virtual Count is " + f2.GetVirtualCyborgCoung(game)));


		if (turn == 1) {
			Console.Error.WriteLine("Conqueror Mode");

			Conqueror_FirstTurn(game, ref action);
			action.AppendBomb(game.GetFactoriesOf(Players.Me).First(), game.GetFactoriesOf(Players.Opponent).First());
			myBombsCount--;
		} else {
			if (myBombsCount > 0) {
				//Throw the second motherfucking bomb
				Factory secondBombTarget =
					game.Troops
					.Where(t1 => t1.Owner == Players.Opponent)
					.Select(t2 => game.GetFactory(t2.Target))
					.OrderByDescending(f3 => f3.Production)
					.ThenBy(f4 => game.Graph[f4, f4.GetClosestFactory(game, Players.Me)])
					.ThenByDescending(f5 => f5.CyborgCount)
					.FirstOrDefault();

				if (secondBombTarget != null) {
					action.AppendBomb(secondBombTarget.GetClosestFactory(game, Players.Me), secondBombTarget);
					myBombsCount--;
				}
			}

			GameState gameCopy = new GameState(game);
			foreach (var strategy in strategists) {
				gameCopy = strategy.ExecuteStrategy(game, ref action);
			}
		}

		return action.Result;
	}
}

public class S_Defender : IStrategy {

	public GameState ExecuteStrategy(GameState game, ref CommandBuilder action) {
		GameState virtualGameState = new GameState(game);

		foreach(Factory myFact in virtualGameState.GetFactoriesOf(Players.Me)) {
			int myFactVirtualCount = myFact.GetVirtualCyborgCount(virtualGameState);

			if (myFactVirtualCount <= 0) {
				//This factory needs reinforcements
				int reinforcementsNeeded = (myFactVirtualCount * -1) + 5;
				//Get a list of factories ready to support, ordered by proximity
				List<Factory> factoriesReadyToSupport = virtualGameState.GetFactoriesOf(Players.Me)
					.Where(f1 => f1.GetVirtualCyborgCount(virtualGameState) > 0)
					.OrderBy(f2 => virtualGameState.Graph[f2, myFact.EntityId]).ToList();

				while(reinforcementsNeeded > 0 && factoriesReadyToSupport.Count > 0) {
					Factory currFactory = factoriesReadyToSupport[0];
					factoriesReadyToSupport.RemoveAt(0);
					int supportTroopsCount = Math.Min(reinforcementsNeeded, currFactory.GetVirtualCyborgCount(virtualGameState)); //Send your virtual cyborg count, but don't go over reinforcementsNeeded

					action.AppendMove(currFactory, myFact, supportTroopsCount);
					virtualGameState.UpdateGame_Move(currFactory, myFact, supportTroopsCount);
					reinforcementsNeeded -= supportTroopsCount;
				}
			}			
		}

		return virtualGameState;
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
	public Graph Graph { get; private set; }
	public List<Troops> Troops { get; private set; }
	public GameState AdvanceInputless(int noOfTurns) {
		return AdvanceInputless_Listed(noOfTurns).Last();
	}

	public GameState(Graph mapGraph, List<Entity> entities) {
		this.Graph = mapGraph;
		this.Entities = entities;
		this.Factories = entities.Where(e => e is Factory).Cast<Factory>().ToList();
		this.Troops = entities.Where(e => e is Troops).Cast<Troops>().ToList();
	}

	public GameState(GameState game) {
		this.Graph = game.Graph;
		this.Entities = new List<Entity>(game.Entities);
		this.Factories = Entities.Where(e => e is Factory).Cast<Factory>().ToList();
		this.Troops = Entities.Where(e => e is Troops).Cast<Troops>().ToList();
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
	public Factory GetClosestFactory(GameState game, Players owner) {
		return game.Factories.Where(f1 => f1.Owner == owner).OrderBy(f2 => game.Graph[this, f2]).FirstOrDefault();
	}

	public Factory GetClosestFactory(GameState game, List<Factory> factories) {
		return factories.OrderBy(f2 => game.Graph[this, f2]).FirstOrDefault();
	}

	public Factory GetFurthestFactory(GameState game, Players owner) {
		return game.Factories.Where(f1 => f1.Owner == owner).OrderByDescending(f2 => game.Graph[this, f2]).FirstOrDefault();
	}

	public Factory GetFurthestFactory(GameState game, List<Factory> factories) {
		return factories.OrderByDescending(f2 => game.Graph[this, f2]).FirstOrDefault();
	}

	public int GetVirtualCyborgCount(GameState game) {
		return (int)this.Owner * this.CyborgCount + game.Troops
			.Where(t1 => t1.Target == EntityId)
			.Sum(t2 => (t2.Owner == Owner) ? t2.TroopCount : -t2.TroopCount);
	}

	public int GetVirtualCyborgCount(GameState game, int turnsCap) {
		return (int)this.Owner * this.CyborgCount + game.Troops
			.Where(t1 => t1.Target == EntityId)
			.Where(t3 => t3.Eta <= turnsCap)
			.Sum(t2 => (t2.Owner == Owner) ? t2.TroopCount : -t2.TroopCount);
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
	Neutral = 0, Me = 1, Opponent = -1
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

