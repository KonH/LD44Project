using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class DecisionTree : ScriptableObject {
	[Serializable]
	public class Category {
		public string Name;
		public List<Decision> Decisions;
	}

	[Serializable]
	public class Decision {
		public string Name;
		public DecisionId Id;
		public double Days;
		public List<TraitValue> Min;
		public List<TraitValue> Max;
		public List<TraitValue> Changes;
	}

	public List<Category> Categories;
}
