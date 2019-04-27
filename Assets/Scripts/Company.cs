using System;
using System.Collections.Generic;

[Serializable]
public class Company {
	[Serializable]
	public class Position {
		public string Name;
		public int Payment;
		public List<TraitValue> Preconditions;
		public List<TraitValue> Requirements;
	}
	public string Name;
	public List<Position> Positions;
}
