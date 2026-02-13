# res://scripts/report.gd
extends Control

@onready var title: Label = $VBoxContainer/Title
@onready var body: RichTextLabel = $VBoxContainer/Body
@onready var back: Button = $VBoxContainer/BackButton

func _ready() -> void:
	back.pressed.connect(func(): get_tree().change_scene_to_file("res://scenes/menu.tscn"))

	var cache := get_node("/root/SessionCache") as SessionCache
	var report := cache.last_report
	var profile := SaveLoad.load_or_create()

	if report == null:
		title.text = "No session report found."
		body.text = ""
		return

	var v := report.archetype_vector()
	var class_name := Archetypes.class_name_from(profile.initiator, profile.listener, profile.challenger, profile.synthesizer, profile.explorer)

	title.text = "Session Report • +%d IP" % report.total_ip

	body.text = ""
	body.append_text("Level: %d\n" % profile.level)
	body.append_text("Class: %s\n\n" % class_name)
	body.append_text("Archetype Evidence (this session):\n")
	body.append_text("Initiator:   %.2f\n" % v["i"])
	body.append_text("Listener:    %.2f\n" % v["l"])
	body.append_text("Challenger:  %.2f\n" % v["c"])
	body.append_text("Synthesizer: %.2f\n" % v["s"])
	body.append_text("Explorer:    %.2f\n\n" % v["e"])
	body.append_text("Turns: %d\n" % report.turns.size())
	body.append_text("\n(Next: add a scroll list of turns + OC/IN/IQ/RF + IP.)\n")