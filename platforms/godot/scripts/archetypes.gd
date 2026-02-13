# res://scripts/archetypes.gd
class_name Archetypes
extends Node

enum Core { INITIATOR, LISTENER, CHALLENGER, SYNTHESIZER, EXPLORER }

static func class_name_from(i: float, l: float, c: float, s: float, e: float) -> String:
	var v := [i, l, c, s, e]
	var top1 := 0
	var top2 := 1
	for k in range(v.size()):
		if v[k] > v[top1]:
			top2 = top1
			top1 = k
		elif k != top1 and v[k] > v[top2]:
			top2 = k

	var a := Core.keys()[top1]
	var b := Core.keys()[top2]

	if (a == "INITIATOR" and b == "EXPLORER") or (a == "EXPLORER" and b == "INITIATOR"):
		return "Trailblazer"
	if (a == "LISTENER" and b == "SYNTHESIZER") or (a == "SYNTHESIZER" and b == "LISTENER"):
		return "Harmonizer"
	if (a == "CHALLENGER" and b == "SYNTHESIZER") or (a == "SYNTHESIZER" and b == "CHALLENGER"):
		return "Dialectician"
	if (a == "EXPLORER" and b == "LISTENER") or (a == "LISTENER" and b == "EXPLORER"):
		return "Curious Companion"
	if (a == "INITIATOR" and b == "CHALLENGER") or (a == "CHALLENGER" and b == "INITIATOR"):
		return "Provocateur"

	return a.capitalize()