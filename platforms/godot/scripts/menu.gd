# res://scripts/menu.gd
extends Control

func _on_start() -> void:
	get_tree().change_scene_to_file("res://scenes/training_chat.tscn")
