using System.Collections.Generic;
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
}
