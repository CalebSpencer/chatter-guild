# res://scripts/session_report.gd
class_name SessionReport
extends Resource

class TurnRecord:
	var speaker: String
	var text: String
	var role: String
	var raw: int
	var norm: float
	var ip: int
	var oc: int
	var inn: int
	var iq: int
	var rf: int

@export var turns: Array = []
@export var total_ip: int = 0
var sumI := 0.0
var sumL := 0.0
var sumC := 0.0
var sumS := 0.0
var sumE := 0.0

func add_turn(speaker: String, text: String, role: int, r) -> void:
	var t := TurnRecord.new()
	t.speaker = speaker
	t.text = text
	t.role = ScoringEngine.RoleType.keys()[role]
	t.raw = r.raw
	t.norm = r.norm
	t.ip = r.ip
	t.oc = r.b.oc
	t.inn = r.b.inn
	t.iq = r.b.iq
	t.rf = r.b.rf
	turns.append(t)

	total_ip += r.ip
	sumI += r.I; sumL += r.L; sumC += r.C; sumS += r.S; sumE += r.E

func archetype_vector() -> Dictionary:
	var sum := sumI + sumL + sumC + sumS + sumE
	if sum <= 0.0001:
		return {"i":0.2,"l":0.2,"c":0.2,"s":0.2,"e":0.2}
	return {"i":sumI/sum,"l":sumL/sum,"c":sumC/sum,"s":sumS/sum,"e":sumE/sum}