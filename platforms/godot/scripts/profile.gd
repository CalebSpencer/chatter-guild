# res://scripts/profile.gd
class_name PlayerProfile
extends Resource

@export var level: int = 1
@export var total_xp: int = 0

@export var clarity_xp: int = 0
@export var integration_xp: int = 0
@export var depth_xp: int = 0
@export var adaptability_xp: int = 0

# 5-way archetype vector
@export var initiator: float = 0.2
@export var listener: float = 0.2
@export var challenger: float = 0.2
@export var synthesizer: float = 0.2
@export var explorer: float = 0.2

func add_xp(xp: int, c: int, i: int, d: int, a: int) -> void:
	total_xp += xp
	clarity_xp += c
	integration_xp += i
	depth_xp += d
	adaptability_xp += a
	_recompute_level()

static func xp_required_for(target_level: int) -> int:
	return 100 + (target_level - 1) * (target_level - 1) * 60

func _recompute_level() -> void:
	var req := xp_required_for(level + 1)
	while total_xp >= req:
		level += 1
		req = xp_required_for(level + 1)

func blend_archetype(i: float, l: float, c: float, s: float, e: float, alpha: float = 0.15) -> void:
	initiator = lerp(initiator, i, alpha)
	listener = lerp(listener, l, alpha)
	challenger = lerp(challenger, c, alpha)
	synthesizer = lerp(synthesizer, s, alpha)
	explorer = lerp(explorer, e, alpha)
	_normalize_archetype()

func _normalize_archetype() -> void:
	var sum := initiator + listener + challenger + synthesizer + explorer
	if sum <= 0.0001:
		initiator = 0.2
		listener = 0.2
		challenger = 0.2
		synthesizer = 0.2
		explorer = 0.2
		return
	initiator /= sum
	listener /= sum
	challenger /= sum
	synthesizer /= sum
	explorer /= sum