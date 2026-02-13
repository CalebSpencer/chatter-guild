# res://scripts/save_load.gd
class_name SaveLoad
extends Node

static func _path_file() -> String:
	return "user://player_profile.json"

static func load_or_create() -> PlayerProfile:
	var path := _path_file()
	if FileAccess.file_exists(path):
		var f := FileAccess.open(path, FileAccess.READ)
		var json_text := f.get_as_text()
		f.close()

		var data = JSON.parse_string(json_text)
		if typeof(data) == TYPE_DICTIONARY:
			var p := PlayerProfile.new()
			# defensive reads
			p.level = int(data.get("level", 1))
			p.total_xp = int(data.get("total_xp", 0))
			p.clarity_xp = int(data.get("clarity_xp", 0))
			p.integration_xp = int(data.get("integration_xp", 0))
			p.depth_xp = int(data.get("depth_xp", 0))
			p.adaptability_xp = int(data.get("adaptability_xp", 0))
			p.initiator = float(data.get("initiator", 0.2))
			p.listener = float(data.get("listener", 0.2))
			p.challenger = float(data.get("challenger", 0.2))
			p.synthesizer = float(data.get("synthesizer", 0.2))
			p.explorer = float(data.get("explorer", 0.2))
			p._normalize_archetype()
			return p

	return PlayerProfile.new()

static func save(profile: PlayerProfile) -> void:
	var data := {
		"level": profile.level,
		"total_xp": profile.total_xp,
		"clarity_xp": profile.clarity_xp,
		"integration_xp": profile.integration_xp,
		"depth_xp": profile.depth_xp,
		"adaptability_xp": profile.adaptability_xp,
		"initiator": profile.initiator,
		"listener": profile.listener,
		"challenger": profile.challenger,
		"synthesizer": profile.synthesizer,
		"explorer": profile.explorer,
	}
	var f := FileAccess.open(_path_file(), FileAccess.WRITE)
	f.store_string(JSON.stringify(data, "\t"))
	f.close()