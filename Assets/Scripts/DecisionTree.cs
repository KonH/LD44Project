using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Random = System.Random;

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
		public bool Scaled;
	}

	public List<Category> Categories;

	[ContextMenu("SetupPrices")]
	public void SetupPrices() {
		var basePrice = 25;
		var randomCoeff = 5;
		var pricePerPoint = 150;
		var category = Categories.Find(p => p.Name == "Study");
		foreach ( var decision in category.Decisions ) {
			var priceTrait = decision.Changes.Find(t => t.Trait == Trait.Money);
			if ( priceTrait == null ) {
				continue;
			}
			if ( decision.Min.Count == 0 ) {
				continue;
			}
			var maxTrait = decision.Min.Max(r => r.Value);
			if ( maxTrait == 0 ) {
				continue;
			}
			priceTrait.Value = -(basePrice + (maxTrait + UnityEngine.Random.Range(0, randomCoeff)) * pricePerPoint);
		}
	}
}
