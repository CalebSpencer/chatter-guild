# res://scripts/scoring_engine.gd
class_name ScoringEngine
extends Node

enum RoleType { INITIATOR, LISTENER, CHALLENGER, SYNTHESIZER, EXPLORER }

class ScoringResult:
	var b
	var raw: int
	var norm: float
	var ip: int
	var clarity_xp: int
	var integration_xp: int
	var depth_xp: int
	var adaptability_xp: int
	var I: float
	var L: float
	var C: float
	var S: float
	var E: float

static var _count := { RoleType.INITIATOR: 0, RoleType.LISTENER: 0, RoleType.CHALLENGER: 0, RoleType.SYNTHESIZER: 0, RoleType.EXPLORER: 0 }
static var _sum_raw := { RoleType.INITIATOR: 0, RoleType.LISTENER: 0, RoleType.CHALLENGER: 0, RoleType.SYNTHESIZER: 0, RoleType.EXPLORER: 0 }

static func _weights(role: int) -> Array:
	# [OC, IN, IQ, RF]
	match role:
		RoleType.INITIATOR: return [3,1,2,1]
		RoleType.LISTENER: return [0,3,2,3]
		RoleType.CHALLENGER: return [2,1,3,1]
		RoleType.SYNTHESIZER: return [1,3,1,3]
		RoleType.EXPLORER: return [2,1,3,1]
		_: return [1,1,1,1]

static func score_turn(role: int, msg: String, partner_last: String, recent_tokens: Dictionary) -> ScoringResult:
	var b = BehaviorHeuristics.extract(msg, partner_last, recent_tokens)
	var w := _weights(role)

	var raw := b.oc * w[0] + b.inn * w[1] + b.iq * w[2] + b.rf * w[3]

	_count[role] += 1
	_sum_raw[role] += raw

	var expected := float(_sum_raw[role]) / max(1, _count[role])
	expected = max(1.0, expected)

	var norm := clamp(float(raw) / expected, 0.0, 2.5)

	var clarity := int(round(3.0 * (b.oc + b.iq)))
	var integration := int(round(4.0 * b.inn))
	var depth := int(round(4.0 * b.rf))
	var adaptability := int(round(3.0 * (b.inn + b.iq))) # placeholder

	var ip := clamp(int(round(10.0 * norm)), 0, 30)

	# archetype evidence
	var I := (role == RoleType.INITIATOR ? 1.0 : 0.0) + 0.5 * b.oc + 0.25 * b.iq
	var L := (role == RoleType.LISTENER ? 1.0 : 0.0) + 0.6 * b.inn + 0.4 * b.rf
	var C := (role == RoleType.CHALLENGER ? 1.0 : 0.0) + 0.6 * b.iq + 0.2 * b.oc
	var S := (role == RoleType.SYNTHESIZER ? 1.0 : 0.0) + 0.7 * b.rf + 0.4 * b.inn
	var E := (role == RoleType.EXPLORER ? 1.0 : 0.0) + 0.7 * b.iq + 0.2 * b.oc

	var r := ScoringResult.new()
	r.b = b
	r.raw = raw
	r.norm = norm
	r.ip = ip
	r.clarity_xp = clarity
	r.integration_xp = integration
	r.depth_xp = depth
	r.adaptability_xp = adaptability
	r.I = I; r.L = L; r.C = C; r.S = S; r.E = E
	return r