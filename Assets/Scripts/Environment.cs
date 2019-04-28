using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu]
public class Environment : ScriptableObject {
	public List<Company> Companies;

	[ContextMenu("Update positions")]
	public void UpdatePositions() {
		foreach ( var company in Companies ) {
			for ( var i = 0; i < company.Positions.Count; i++ ) {
				var pos = company.Positions[i];
				var isSpecific = false;
				foreach ( var req in pos.Requirements ) {
					if ( req.Trait == Trait.Skill ) {
						continue;
					}
					if ( req.Trait == Trait.Talking ) {
						continue;
					}
					isSpecific = true;
					break;
				}
				if ( !isSpecific ) {
					foreach ( var req in pos.Requirements ) {
						req.Value += 5 + i * 5;
					}
				} else {
					pos.Payment += Random.Range(2 + i, (2 + i * 2) * 3) * 5;
				}
			}
		}
	}

	[ContextMenu("Update levels")]
	public void UpdateLevels() {
		var basic = new int[] { 10, 30, 100, 150 };
		foreach ( var company in Companies ) {
			for ( var i = 0; i < company.Positions.Count; i++ ) {
				var pos = company.Positions[i];
				var skillReq = pos.Requirements.Find(r => r.Trait == Trait.Skill);
				if ( skillReq == null ) {
					continue;
				}
				var range = (i + 1) * 10;
				var skillValue = basic[i] + Random.Range(-range, range);
				skillReq.Value = skillValue;
				pos.Payment = (skillValue + Random.Range(0, 10)) * 5;
			}
			var levels = company.Positions.Select(p => p.Requirements.FirstOrDefault(r => r.Trait == Trait.Skill)?.Value);
			Debug.Log(company.Positions.Count + ": " + string.Join(", ", levels));
		}
	}

	[ContextMenu("Stats")]
	public void Stats() {
		var mins = new int[] { int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue };
		var maxs = new int[4];
		foreach ( var company in Companies ) {
			for ( var i = 0; i < company.Positions.Count; i++ ) {
				var pos = company.Positions[i];
				if ( pos.Payment < mins[i] ) {
					mins[i] = pos.Payment;
				}
				if ( pos.Payment > maxs[i] ) {
					maxs[i] = pos.Payment;
				}
			}
		}
		Debug.Log("Min: " + string.Join(", ", mins));
		Debug.Log("Max: " + string.Join(", ", maxs));
	}
}
