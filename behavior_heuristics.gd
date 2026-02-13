# res://scripts/behavior_heuristics.gd
class_name BehaviorHeuristics
extends Node

class BehaviorVector:
	var oc: int
	var inn: int
	var iq: int
	var rf: int

static func extract(msg: String, partner_last: String, recent_tokens: Dictionary) -> BehaviorVector:
	var m := msg.to_lower()
	var pl := partner_last.to_lower()

	var partner_asked := pl.find("?") >= 0
	var i_asked := m.find("?") >= 0

	var iq := i_asked ? 1 : 0

	var rf := 0
	if m.begins_with("so ") or m.find("it sounds like") >= 0 or m.find("in other words") >= 0 or m.find("to summarize") >= 0:
		rf = 1

	var cue := (m.find("you said") >= 0
		or m.find("you mentioned") >= 0
		or m.find("that makes sense") >= 0
		or m.find("i hear you") >= 0
		or m.find("good point") >= 0)

	var answered := _detect_answer(m)
	var overlap := _overlap_score(m, pl)

	var inn := 0
	if cue:
		inn = 1
	elif partner_asked and answered:
		inn = 1
	elif overlap >= 2 and answered:
		inn = 1

	var oc := 0
	if m.length() >= 60: oc += 1
	if m.find("because") >= 0 or m.find("for example") >= 0 or m.find("in my experience") >= 0: oc += 1
	if _novel_token_count(m, recent_tokens) >= 4: oc += 1

	if partner_asked and answered: oc += 1
	if partner_asked and not answered and i_asked: oc -= 1

	var b := BehaviorVector.new()
	b.oc = _clamp02(oc)
	b.inn = _clamp02(inn)
	b.iq = _clamp02(iq)
	b.rf = _clamp02(rf)
	return b

static func update_recent_tokens(recent: Dictionary, msg: String) -> void:
	if recent.size() > 200:
		recent.clear()
	var m := msg.to_lower()
	for tok in m.split(" "):
		var t := _clean_token(tok)
		if t.length() >= 4:
			recent[t] = true

static func _detect_answer(m: String) -> bool:
	var cues := ["because", "for me", "i think", "i feel", "my favorite", "i prefer", "i like", "i have", "i'm ", "im "]
	for c in cues:
		if m.find(c) >= 0: return true
	for ch in m:
		if ch >= "0" and ch <= "9": return true
	return false

static func _overlap_score(a: String, b: String) -> int:
	if a.is_empty() or b.is_empty(): return 0
	var ta := a.split(" ")
	var tb := b.split(" ")
	var count := 0
	for x0 in ta:
		var x := _clean_token(x0)
		if x.length() < 4: continue
		for y0 in tb:
			var y := _clean_token(y0)
			if x == y and x.length() >= 4:
				count += 1
				break
	return count

static func _novel_token_count(m: String, recent: Dictionary) -> int:
	var novel := 0
	for tok0 in m.split(" "):
		var tok := _clean_token(tok0)
		if tok.length() < 4: continue
		if not recent.has(tok): novel += 1
	return novel

static func _clean_token(s: String) -> String:
	var t := s.strip_edges()
	while t.length() > 0 and not t[0].is_valid_int() and not t[0].is_valid_float() and not t[0].is_subsequence_of("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"):
		t = t.substr(1)
	while t.length() > 0 and not t[t.length()-1].is_valid_int() and not t[t.length()-1].is_valid_float() and not t[t.length()-1].is_subsequence_of("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"):
		t = t.substr(0, t.length()-1)
	# above is a bit hacky; simplest: strip common punctuation
	return t.strip_edges(".,!?;:\"'()[]{}<>")

static func _clamp02(v: int) -> int:
	return clamp(v, 0, 2)