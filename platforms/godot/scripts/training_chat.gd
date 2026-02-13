# res://scripts/training_chat.gd
extends Control

@onready var header: Label = $VBoxContainer/HeaderLabel
@onready var footer: Label = $VBoxContainer/FooterLabel
@onready var messages: VBoxContainer = $VBoxContainer/ScrollContainer/MessagesBox
@onready var input: LineEdit = $VBoxContainer/HBoxContainer/Input
@onready var send_btn: Button = $VBoxContainer/HBoxContainer/SendButton
@onready var end_btn: Button = $VBoxContainer/EndButton

var profile: PlayerProfile
var report: SessionReport
var recent_tokens := {}
var last_player := ""
var last_ai := ""

var player_name := "Tyler"
var ai_name := "CoachBot"

var player_role := ScoringEngine.RoleType.INITIATOR
var ai_role := ScoringEngine.RoleType.LISTENER

var llm: LLMConnector
var conversation_history: Array = []
var _waiting_for_reply := false
var _last_player_ip: int = 0

func _ready() -> void:
	profile = SaveLoad.load_or_create()
	report = SessionReport.new()

	llm = LLMConnector.new()
	add_child(llm)
	llm.reply_received.connect(_on_ai_reply)

	var archetype_class := Archetypes.class_name_from(profile.initiator, profile.listener, profile.challenger, profile.synthesizer, profile.explorer)
	header.text = "Level %d • Class: %s" % [profile.level, archetype_class]
	footer.text = "Training Mode"

	_add_system("Training Mode: have a conversation. End anytime to see your report.")
	_add_system("Roles: You=%s  AI=%s" % [ScoringEngine.RoleType.keys()[player_role], ScoringEngine.RoleType.keys()[ai_role]])

	send_btn.pressed.connect(_on_send)
	end_btn.pressed.connect(_on_end)
	input.text_submitted.connect(func(_t): _on_send())

func _on_send() -> void:
	var text := input.text.strip_edges()
	if text.is_empty() or _waiting_for_reply:
		return
	input.text = ""

	_add_message(player_name, text)

	var r1 = ScoringEngine.score_turn(player_role, text, last_ai, recent_tokens)
	report.add_turn(player_name, text, player_role, r1)
	BehaviorHeuristics.update_recent_tokens(recent_tokens, text)
	last_player = text
	_last_player_ip = r1.ip

	conversation_history.append({"role": "user", "content": text})

	_waiting_for_reply = true
	send_btn.disabled = true
	input.editable = false
	footer.text = "AI is thinking..."

	llm.request_reply(ai_role, last_player, conversation_history)

func _on_ai_reply(reply: String) -> void:
	_waiting_for_reply = false
	send_btn.disabled = false
	input.editable = true

	_add_message(ai_name, reply)

	conversation_history.append({"role": "assistant", "content": reply})

	var r2 = ScoringEngine.score_turn(ai_role, reply, last_player, recent_tokens)
	report.add_turn(ai_name, reply, ai_role, r2)
	BehaviorHeuristics.update_recent_tokens(recent_tokens, reply)
	last_ai = reply

	footer.text = "Session IP: %d  •  You +%d IP" % [report.total_ip, _last_player_ip]
	input.grab_focus()

func _on_end() -> void:
	var v := report.archetype_vector()
	var xp := report.total_ip

	profile.add_xp(xp, xp/4, xp/4, xp/4, xp/4)
	profile.blend_archetype(v["i"], v["l"], v["c"], v["s"], v["e"], 0.15)
	SaveLoad.save(profile)

	var cache := get_node("/root/SessionCache")
	cache.last_report = report

	get_tree().change_scene_to_file("res://scenes/report.tscn")

func _add_message(who: String, text: String) -> void:
	var l := Label.new()
	l.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	l.text = "%s: %s" % [who, text]
	messages.add_child(l)

func _add_system(text: String) -> void:
	var l := Label.new()
	l.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	l.text = "[%s]" % text
	messages.add_child(l)
